using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using PMS.Core.Database;
using PMS.Core.Enums.General;
using PMS.Core.Models;
using PMS.Core.Repositories.Interfaces;
using Serilog;

namespace PMS.Core.Repositories;

public class PatientRepository : BaseRepository<Patient>, IPatientRepository
{
    private readonly IDatabaseContext _context;

    public PatientRepository(IDatabaseContext context, ILogger logger)
        : base(context, "patients", logger)
    {
        _context = context;
    }

   

    public async Task<(List<Patient> Patients, long TotalCount)> GetPagedPatientsAsync(
        int page,
        int pageSize,
        string? searchText = null,
        bool? isActive = null,
        string? assignedDoctorId = null,
        HealthStatus? healthStatus = null)
    {
        try
        {
            var filterBuilder = Builders<Patient>.Filter;
            var filters = new List<FilterDefinition<Patient>>();

            if (isActive.HasValue)
            {
                filters.Add(filterBuilder.Eq(p => p.IsActive, isActive.Value));
            }

            if (!string.IsNullOrWhiteSpace(assignedDoctorId))
            {
                filters.Add(filterBuilder.Eq(p => p.AssignedDoctorId, assignedDoctorId));
            }

            if (healthStatus.HasValue)
            {
                filters.Add(filterBuilder.Eq(p => p.HealthStatus, healthStatus.Value));
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var searchFilter = filterBuilder.Or(
                    filterBuilder.Regex(p => p.FullName, new BsonRegularExpression(searchText, "i")),
                    filterBuilder.Regex(p => p.MedicalRecordNumber, new BsonRegularExpression(searchText, "i")),
                    filterBuilder.Regex(p => p.Phone, new BsonRegularExpression(searchText, "i"))
                );
                filters.Add(searchFilter);
            }

            var finalFilter = filters.Count > 0
                ? filterBuilder.And(filters)
                : filterBuilder.Empty;

            var totalCount = await Collection.CountDocumentsAsync(finalFilter);

            var skip = (page - 1) * pageSize;
            var patients = await Collection
                .Find(finalFilter)
                .Sort(Builders<Patient>.Sort.Ascending(p => p.FullName))
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();

            await PopulateDoctorNamesAsync(patients);

            Logger.Information(
                "Retrieved page {Page} with {Count} patients (Total: {Total})",
                page, patients.Count, totalCount);

            return (patients, totalCount);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting paged patients");
            throw;
        }
    }

  
    public async Task<(List<Patient> Patients, long TotalCount)> SearchPatientsAsync(
        string searchText,
        int page,
        int pageSize)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return (new List<Patient>(), 0);
            }

            var filterBuilder = Builders<Patient>.Filter;

            var searchFilter = filterBuilder.Or(
                filterBuilder.Regex(p => p.FullName, new BsonRegularExpression(searchText, "i")),
                filterBuilder.Regex(p => p.MedicalRecordNumber, new BsonRegularExpression(searchText, "i")),
                filterBuilder.Regex(p => p.Phone, new BsonRegularExpression(searchText, "i")),
                filterBuilder.Regex(p => p.Email, new BsonRegularExpression(searchText, "i"))
            );

            var totalCount = await Collection.CountDocumentsAsync(searchFilter);

            var skip = (page - 1) * pageSize;
            var patients = await Collection
                .Find(searchFilter)
                .Sort(Builders<Patient>.Sort.Ascending(p => p.FullName))
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();

            await PopulateDoctorNamesAsync(patients);

            Logger.Information(
                "Search '{SearchText}' returned {Count} patients (Total: {Total})",
                searchText, patients.Count, totalCount);

            return (patients, totalCount);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error searching patients");
            throw;
        }
    }


  
    private async Task PopulateDoctorNamesAsync(List<Patient> patients)
    {
        try
        {
            var doctorIds = patients
                .Where(p => !string.IsNullOrWhiteSpace(p.AssignedDoctorId))
                .Select(p => p.AssignedDoctorId)
                .Distinct()
                .ToList();

            if (!doctorIds.Any())
            {
                return;
            }

            var doctorsCollection = _context.GetCollection<Doctor>("doctors");

            var objectIds = doctorIds.Select(id => id).ToList();

            var doctors = await doctorsCollection
                .Find(d => objectIds.Contains(d.Id))
                .ToListAsync();

            var doctorLookup = doctors.ToDictionary(d => d.Id, d => d.FullName);

            foreach (var patient in patients)
            {
                if (!string.IsNullOrWhiteSpace(patient.AssignedDoctorId) &&
                    doctorLookup.TryGetValue(patient.AssignedDoctorId, out var doctorName))
                {
                    patient.AssignedDoctorName = doctorName;
                }
                else
                {
                    patient.AssignedDoctorName = "Не призначено";
                }
            }

            Logger.Debug("Populated doctor names for {Count} patients", patients.Count);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error populating doctor names");
            foreach (var patient in patients)
            {
                if (string.IsNullOrWhiteSpace(patient.AssignedDoctorName))
                {
                    patient.AssignedDoctorName = "Не призначено";
                }
            }
        }
    }
}