using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using PMS.Core.Database;
using PMS.Core.Models;
using PMS.Core.Repositories.Interfaces;
using Serilog;

namespace PMS.Core.Repositories;


public class SpecialtyRepository : BaseRepository<Specialty>, ISpecialtyRepository
{
    public SpecialtyRepository(IDatabaseContext context, ILogger logger)
        : base(context, "specialties", logger)
    {
    }

    public async Task<IEnumerable<Specialty>> GetAllOrderedAsync()
    {
        try
        {
            var specialties = await Collection
                .Find(s => true)
                .SortBy(s => s.Name)
                .ToListAsync();

            Logger.Information("Retrieved {Count} specialties ordered by name", specialties.Count);
            return specialties;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting all ordered specialties");
            throw;
        }
    }

}