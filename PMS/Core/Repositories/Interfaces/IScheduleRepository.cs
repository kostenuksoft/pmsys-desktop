using System.Collections.Generic;
using System.Threading.Tasks;
using PMS.Core.Enums.General;
using PMS.Core.Models;

namespace PMS.Core.Repositories.Interfaces;

public interface IScheduleRepository : IBaseRepository<Schedule>
{
    Task<Schedule?> GetActiveScheduleAsync(string entityId, EntityType entityType, int dayOfWeek);
}