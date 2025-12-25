using MongoDB.Bson;
using MongoDB.Driver;
using PMS.Core.Database;
using PMS.Core.Models;
using PMS.Core.Models.DTO;
using PMS.Core.Repositories.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PMS.Core.Repositories;

public class DoctorRepository : BaseRepository<Doctor>, IDoctorRepository
{
    public DoctorRepository(IDatabaseContext context, ILogger logger)
        : base(context, "doctors", logger)
    {
    }

    public async Task<Doctor?> GetByEmployeeNumberAsync(string employeeNumber)
    {
        return await Collection.Find(d => d.EmployeeNumber == employeeNumber).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Doctor>> GetBySpecialtyAsync(string specialtyId)
    {
        return await Collection.Find(d => d.SpecialtyId == specialtyId && d.IsActive).ToListAsync();
    }

    public async Task<IEnumerable<Doctor>> GetDistrictDoctorsAsync()
    {
        return await Collection.Find(d => d.IsDistrictDoctor && d.IsActive).ToListAsync();
    }


    public async Task<(IEnumerable<DoctorWithDetails> doctors, long totalCount)> GetPagedDoctorsWithDetailsAsync(
        int page,
        int pageSize,
        string? searchText = null,
        bool? isActive = null,
        string? specialtyId = null,
        bool? isDistrictDoctor = null)
    {
        try
        {
            var filterBuilder = Builders<Doctor>.Filter;
            var filters = new List<FilterDefinition<Doctor>>();

            if (isActive.HasValue)
            {
                filters.Add(filterBuilder.Eq(d => d.IsActive, isActive.Value));
            }

            if (!string.IsNullOrWhiteSpace(specialtyId))
            {
                filters.Add(filterBuilder.Eq(d => d.SpecialtyId, specialtyId));
            }

            if (isDistrictDoctor.HasValue)
            {
                filters.Add(filterBuilder.Eq(d => d.IsDistrictDoctor, isDistrictDoctor.Value));
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var searchFilter = filterBuilder.Or(
                    filterBuilder.Regex(d => d.FullName, new BsonRegularExpression(searchText, "i")),
                    filterBuilder.Regex(d => d.EmployeeNumber, new BsonRegularExpression(searchText, "i")),
                    filterBuilder.Regex(d => d.Phone, new BsonRegularExpression(searchText, "i"))
                );
                filters.Add(searchFilter);
            }

            var finalFilter = filters.Count > 0
                ? filterBuilder.And(filters)
                : filterBuilder.Empty;

            var totalCount = await Collection.CountDocumentsAsync(finalFilter);

            var skip = (page - 1) * pageSize;
            var doctors = await Collection
                .Find(finalFilter)
                .Sort(Builders<Doctor>.Sort.Ascending(d => d.FullName))
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();

            var doctorDetails = await PopulateDoctorDetailsAsync(doctors);

            Logger.Information(
                "Retrieved page {Page} with {Count} doctors (Total: {Total})",
                page, doctorDetails.Count, totalCount);

            return (doctorDetails, totalCount);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting paged doctors with details");
            throw;
        }
    }

    private async Task<List<DoctorWithDetails>> PopulateDoctorDetailsAsync(List<Doctor> doctors)
    {
        var doctorDetails = new List<DoctorWithDetails>();

        foreach (var doctor in doctors)
        {
            var details = new DoctorWithDetails
            {
                Id = doctor.Id,
                EmployeeNumber = doctor.EmployeeNumber,
                FullName = doctor.FullName,
                BirthDate = doctor.BirthDate,
                SpecialtyId = doctor.SpecialtyId,
                Category = doctor.Category.ToString().ToLower(),
                ExperienceYears = doctor.ExperienceYears,
                HireDate = doctor.HireDate,
                Phone = doctor.Phone,
                Email = doctor.Email,
                RoomId = doctor.RoomId,
                IsDistrictDoctor = doctor.IsDistrictDoctor,
                DistrictArea = doctor.DistrictArea,
                IsActive = doctor.IsActive
            };

            try
            {
                var specialtiesCollection = Collection.Database.GetCollection<Specialty>("specialties");
                var specialty = await specialtiesCollection
                    .Find(Builders<Specialty>.Filter.Eq(s => s.Id, doctor.SpecialtyId))
                    .FirstOrDefaultAsync();

                if (specialty != null)
                {
                    details.SpecialtyName = specialty.Name;
                    details.SpecialtyCode = specialty.Code;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Could not load specialty for doctor {DoctorId}", doctor.Id);
            }

            if (!string.IsNullOrEmpty(doctor.RoomId))
            {
                try
                {
                    var roomsCollection = Collection.Database.GetCollection<Room>("rooms");
                    var room = await roomsCollection
                        .Find(Builders<Room>.Filter.Eq(r => r.Id, doctor.RoomId))
                        .FirstOrDefaultAsync();

                    if (room != null)
                    {
                        details.RoomNumber = room.RoomNumber;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "Could not load room for doctor {DoctorId}", doctor.Id);
                }
            }

            doctorDetails.Add(details);
        }

        return doctorDetails;
    }

    public async Task<(List<Doctor> Doctors, long TotalCount)> SearchDoctorsAsync(
        string searchText,
        int page,
        int pageSize)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return (new List<Doctor>(), 0);
            }

            var filterBuilder = Builders<Doctor>.Filter;

            var searchFilter = filterBuilder.Or(
                filterBuilder.Regex(d => d.FullName, new BsonRegularExpression(searchText, "i")),
                filterBuilder.Regex(d => d.EmployeeNumber, new BsonRegularExpression(searchText, "i")),
                filterBuilder.Regex(d => d.Phone, new BsonRegularExpression(searchText, "i"))
            );

            var activeFilter = filterBuilder.Eq(d => d.IsActive, true);
            var finalFilter = filterBuilder.And(searchFilter, activeFilter);

            var totalCount = await Collection.CountDocumentsAsync(finalFilter);

            var skip = (page - 1) * pageSize;
            var doctors = await Collection
                .Find(finalFilter)
                .Sort(Builders<Doctor>.Sort.Ascending(d => d.FullName))
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();

            Logger.Information("Search '{SearchText}' found {Count} of {Total} doctors",
                searchText, doctors.Count, totalCount);

            return (doctors, totalCount);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error searching doctors with text: {SearchText}", searchText);
            throw;
        }
    }

}



