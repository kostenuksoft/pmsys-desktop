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

public class ScheduleRepository : BaseRepository<Schedule>, IScheduleRepository
{
    public ScheduleRepository(IDatabaseContext context, ILogger logger)
        : base(context, "schedules", logger)
    {
    }

    public async Task<Schedule?> GetActiveScheduleAsync(string entityId, EntityType entityType, int dayOfWeek)
    {
        try
        {
            var now = DateTime.UtcNow;

            var filterBuilder = Builders<Schedule>.Filter;

            var filter = filterBuilder.And(
                filterBuilder.Eq(s => s.EntityId, entityId),
                filterBuilder.Eq(s => s.EntityType, entityType),
                filterBuilder.Eq(s => s.DayOfWeek, dayOfWeek),
                filterBuilder.Lte(s => s.EffectiveFrom, now),
                filterBuilder.Or(
                    filterBuilder.Eq(s => s.EffectiveUntil, null),
                    filterBuilder.Gt(s => s.EffectiveUntil, now)
                )
            );

            return await Collection.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting active schedule for entity {EntityId}", entityId);
            throw;
        }
    }
}