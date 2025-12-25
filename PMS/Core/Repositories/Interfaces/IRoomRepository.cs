using System.Collections.Generic;
using System.Threading.Tasks;
using PMS.Core.Enums.General;
using PMS.Core.Models;

namespace PMS.Core.Repositories.Interfaces;

public interface IRoomRepository : IBaseRepository<Room>
{
    Task<IEnumerable<Room>> GetAvailableRoomsAsync();
    Task<(IEnumerable<Room> rooms, long totalCount)> GetPagedRoomsAsync(
        int page,
        int pageSize,
        string? searchText = null,
        RoomType? roomType = null,
        bool? isActive = null);
}