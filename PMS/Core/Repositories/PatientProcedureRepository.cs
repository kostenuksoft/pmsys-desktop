using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using PMS.Core.Database;
using PMS.Core.Enums.General;
using PMS.Core.Models;
using PMS.Core.Repositories.Interfaces;
using Serilog;

namespace PMS.Core.Repositories;


public class PatientProcedureRepository : BaseRepository<PatientProcedure>, IPatientProcedureRepository
{
    public PatientProcedureRepository(IDatabaseContext context, ILogger logger)
        : base(context, "patient_procedures", logger)
    {
    }

    public async Task<(List<PatientProcedure> Procedures, long TotalCount)> GetPagedPatientProceduresAsync(
        int page,
        int pageSize,
        string? searchText = null,
        string? patientId = null,
        string? procedureId = null,
        string? prescribedById = null,
        ProcedureStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var filters = new List<FilterDefinition<PatientProcedure>>();

            if (!string.IsNullOrWhiteSpace(patientId))
            {
                filters.Add(FilterBuilder.Eq(p => p.PatientId, patientId));
            }

            if (!string.IsNullOrWhiteSpace(procedureId))
            {
                filters.Add(FilterBuilder.Eq(p => p.ProcedureId, procedureId));
            }

            if (!string.IsNullOrWhiteSpace(prescribedById))
            {
                filters.Add(FilterBuilder.Eq(p => p.PrescribedBy, prescribedById));
            }

            if (status.HasValue)
            {
                filters.Add(FilterBuilder.Eq(p => p.Status, status.Value));
            }

            if (startDate.HasValue)
            {
                filters.Add(FilterBuilder.Gte(p => p.PrescribedDate, startDate.Value));
            }

            if (endDate.HasValue)
            {
                filters.Add(FilterBuilder.Lte(p => p.PrescribedDate, endDate.Value));
            }

            var finalFilter = filters.Count > 0
                ? FilterBuilder.And(filters)
                : FilterBuilder.Empty;

            var totalCount = await Collection.CountDocumentsAsync(finalFilter);

            var skip = (page - 1) * pageSize;
            var procedures = await Collection
                .Find(finalFilter)
                .Sort(Builders<PatientProcedure>.Sort.Descending(p => p.PrescribedDate))
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();

            Logger.Information(
                "Retrieved page {Page} with {Count} patient procedures (Total: {Total})",
                page, procedures.Count, totalCount);

            return (procedures, totalCount);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting paged patient procedures");
            throw;
        }
    }


    public async Task<long> CountByStatusAsync(ProcedureStatus status)
    {
        try
        {
            var filter = FilterBuilder.Eq(p => p.Status, status);
            return await Collection.CountDocumentsAsync(filter);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error counting procedures by status {Status}", status);
            throw;
        }
    }

  
}