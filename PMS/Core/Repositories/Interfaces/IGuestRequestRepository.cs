using System.Collections.Generic;
using System.Threading.Tasks;
using PMS.Core.Models;

namespace PMS.Core.Repositories.Interfaces;

public interface IGuestRequestRepository : IBaseRepository<GuestRequest>
{
    Task<IEnumerable<GuestRequest>> GetPendingRequestsAsync();
    Task<bool> ApproveRequestAsync(string requestId, string adminId, string response);
    Task<bool> RejectRequestAsync(string requestId, string adminId, string response);

    Task<GuestRequest?> GetByRequestCodeAsync(string requestCode);
    Task<GuestRequest?> GetByEmailAsync(string email);
    Task<GuestRequest?> GetByLoginAsync(string login);
}