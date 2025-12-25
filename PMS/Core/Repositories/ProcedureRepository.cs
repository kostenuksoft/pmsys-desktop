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

public class ProcedureRepository : BaseRepository<Procedure>, IProcedureRepository
{
    public ProcedureRepository(IDatabaseContext context, ILogger logger)
        : base(context, "procedures", logger)
    {
    }


    public async Task<(IEnumerable<Procedure> procedures, long totalCount)> GetPagedProceduresAsync(
        int page,
        int pageSize,
        string? searchText = null,
        ProcedureType? procedureType = null,
        bool? isActive = null)
    {
        try
        {
            var filterBuilder = Builders<Procedure>.Filter;
            var filters = new List<FilterDefinition<Procedure>>();

            if (isActive.HasValue)
            {
                filters.Add(filterBuilder.Eq(p => p.IsActive, isActive.Value));
            }

            if (procedureType.HasValue)
            {
                filters.Add(filterBuilder.Eq(p => p.ProcedureType, procedureType.Value));
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var searchFilters = new List<FilterDefinition<Procedure>>
                {
                    filterBuilder.Regex(p => p.Name, new BsonRegularExpression(searchText, "i")),
                    filterBuilder.Regex(p => p.ProcedureCode, new BsonRegularExpression(searchText, "i")),
                    filterBuilder.Regex(p => p.Description, new BsonRegularExpression(searchText, "i"))
                };

                var ukrainianToEnumMap = new Dictionary<string, ProcedureType>(StringComparer.OrdinalIgnoreCase)
                {
                    { "діагностична", ProcedureType.Diagnostic },
                    { "діагност", ProcedureType.Diagnostic },
                    { "терапевтична", ProcedureType.Therapeutic },
                    { "терапія", ProcedureType.Therapeutic },
                    { "фізіотерапія", ProcedureType.PhysicalTherapy },
                    { "фізіотерап", ProcedureType.PhysicalTherapy },
                    { "лабораторне", ProcedureType.Laboratory },
                    { "лабораторія", ProcedureType.Laboratory },
                    { "лаб", ProcedureType.Laboratory },
                    { "візуалізація", ProcedureType.Imaging },
                    { "імідж", ProcedureType.Imaging },
                    { "вакцинація", ProcedureType.Vaccination },
                    { "вакцина", ProcedureType.Vaccination },
                    { "профілактична", ProcedureType.Preventive },
                    { "профілактик", ProcedureType.Preventive },
                    { "реабілітація", ProcedureType.Rehabilitation },
                    { "реабіліт", ProcedureType.Rehabilitation },
                    { "невідкладна", ProcedureType.Emergency },
                    { "екстрена", ProcedureType.Emergency }
                };

                var matchingTypes = Enum.GetValues<ProcedureType>()
                    .Where(pt => pt.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var kvp in ukrainianToEnumMap)
                {
                    if (kvp.Key.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!matchingTypes.Contains(kvp.Value))
                        {
                            matchingTypes.Add(kvp.Value);
                        }
                    }
                }

                if (matchingTypes.Any())
                {
                    var typeFilter = filterBuilder.In(p => p.ProcedureType, matchingTypes);
                    searchFilters.Add(typeFilter);
                }

                var searchFilter = filterBuilder.Or(searchFilters);
                filters.Add(searchFilter);
            }

            var finalFilter = filters.Count > 0
                ? filterBuilder.And(filters)
                : filterBuilder.Empty;

            var totalCount = await Collection.CountDocumentsAsync(finalFilter);

            var skip = (page - 1) * pageSize;
            var procedures = await Collection
                .Find(finalFilter)
                .Sort(Builders<Procedure>.Sort.Ascending(p => p.ProcedureType).Ascending(p => p.Name))
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();

            Logger.Information(
                "Retrieved page {Page} with {Count} procedures (Total: {Total})",
                page, procedures.Count, totalCount);

            return (procedures, totalCount);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting paged procedures");
            throw;
        }
    }
}