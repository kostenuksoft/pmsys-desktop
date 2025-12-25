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

public class GuestRequestRepository : BaseRepository<GuestRequest>, IGuestRequestRepository
{
    public GuestRequestRepository(IDatabaseContext context, ILogger logger)
        : base(context, "guest_requests", logger)
    {
    }

    public async Task<IEnumerable<GuestRequest>> GetPendingRequestsAsync()
    {
        return await Collection
            .Find(r => r.Status == RequestStatus.Pending)
            .SortByDescending(r => r.RequestDate)
            .ToListAsync();
    }

 

    public async Task<bool> ApproveRequestAsync(string requestId, string adminId, string response)
    {
        var update = Builders<GuestRequest>.Update
            .Set(r => r.Status, RequestStatus.Approved)
            .Set(r => r.AdminResponse, response)
            .Set(r => r.ProcessedDate, DateTime.UtcNow)
            .Set(r => r.ProcessedBy, adminId)
            .Set(r => r.ModifiedDate, DateTime.UtcNow);

        var result = await Collection.UpdateOneAsync(r => r.Id == requestId, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> RejectRequestAsync(string requestId, string adminId, string response)
    {
        var update = Builders<GuestRequest>.Update
            .Set(r => r.Status, RequestStatus.Rejected)
            .Set(r => r.AdminResponse, response)
            .Set(r => r.ProcessedDate, DateTime.UtcNow)
            .Set(r => r.ProcessedBy, adminId)
            .Set(r => r.ModifiedDate, DateTime.UtcNow);

        var result = await Collection.UpdateOneAsync(r => r.Id == requestId, update);
        return result.ModifiedCount > 0;
    }

    public async Task<GuestRequest?> GetByRequestCodeAsync(string requestCode)
    {
        return await Collection
            .Find(r => r.RequestCode == requestCode).FirstOrDefaultAsync();
    }

    public async Task<GuestRequest?> GetByEmailAsync(string email)
    {
        return await Collection
            .Find(r => r.Email == email)
            .FirstOrDefaultAsync();
    }

    public async Task<GuestRequest?> GetByLoginAsync(string login)
    {
        return await Collection
            .Find(r => r.Login == login)
            .FirstOrDefaultAsync();
    }
}