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


public class RoomRepository : BaseRepository<Room>, IRoomRepository
{
    public RoomRepository(IDatabaseContext context, ILogger logger)
        : base(context, "rooms", logger)
    {
    }

    public async Task<IEnumerable<Room>> GetByTypeAsync(RoomType type)
    {
        return await Collection.Find(r => r.RoomType == type && r.IsActive).ToListAsync();
    }

    public async Task<IEnumerable<Room>> GetAvailableRoomsAsync()
    {
        return await Collection.Find(r => r.IsActive).ToListAsync();
    }


    public async Task<(IEnumerable<Room> rooms, long totalCount)> GetPagedRoomsAsync(
        int page,
        int pageSize,
        string? searchText = null,
        RoomType? roomType = null,
        bool? isActive = null)
    {
        try
        {
            var filterBuilder = Builders<Room>.Filter;
            var filters = new List<FilterDefinition<Room>>();

            if (isActive.HasValue)
            {
                filters.Add(filterBuilder.Eq(r => r.IsActive, isActive.Value));
            }

            if (roomType.HasValue)
            {
                filters.Add(filterBuilder.Eq(r => r.RoomType, roomType.Value));
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var searchFilters = new List<FilterDefinition<Room>>
                {
                    filterBuilder.Regex(r => r.RoomNumber, new BsonRegularExpression(searchText, "i"))
                };

                var ukrainianToEnumMap = new Dictionary<string, RoomType>(StringComparer.OrdinalIgnoreCase)
                {
                    { "кабінет лікаря", RoomType.DoctorOffice },
                    { "лікар", RoomType.DoctorOffice },
                    { "процедурний", RoomType.Procedure },
                    { "фізіотерапія", RoomType.PhysicalTherapy },
                    { "фізіотерапевтичний", RoomType.PhysicalTherapy },
                    { "узд", RoomType.Ultrasound },
                    { "ультразвук", RoomType.Ultrasound },
                    { "лабораторія", RoomType.Laboratory },
                    { "лабораторний", RoomType.Laboratory },
                    { "реєстратура", RoomType.Reception },
                    { "адміністративний", RoomType.Administrative },
                    { "адмін", RoomType.Administrative }
                };

                var matchingRoomTypes = Enum.GetValues<RoomType>()
                    .Where(rt => rt.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var kvp in ukrainianToEnumMap)
                {
                    if (kvp.Key.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!matchingRoomTypes.Contains(kvp.Value))
                        {
                            matchingRoomTypes.Add(kvp.Value);
                        }
                    }
                }

                if (matchingRoomTypes.Any())
                {
                    var roomTypeFilter = filterBuilder.In(r => r.RoomType, matchingRoomTypes);
                    searchFilters.Add(roomTypeFilter);
                }

                var searchFilter = filterBuilder.Or(searchFilters);
                filters.Add(searchFilter);
            }

            var finalFilter = filters.Count > 0
                ? filterBuilder.And(filters)
                : filterBuilder.Empty;

            var totalCount = await Collection.CountDocumentsAsync(finalFilter);

            var skip = (page - 1) * pageSize;
            var rooms = await Collection
                .Find(finalFilter)
                .Sort(Builders<Room>.Sort.Ascending(r => r.Floor).Ascending(r => r.RoomNumber))
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();

            Logger.Information(
                "Retrieved page {Page} with {Count} rooms (Total: {Total})",
                page, rooms.Count, totalCount);

            return (rooms, totalCount);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting paged rooms");
            throw;
        }
    }
}