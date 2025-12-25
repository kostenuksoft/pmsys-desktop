using System.Collections.Generic;
using System.Threading.Tasks;
using PMS.Core.Enums.General;
using PMS.Core.Models;

namespace PMS.Core.Repositories.Interfaces;


public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetByLoginAsync(string login);
    Task<User?> GetByLoginOrEmailAsync(string loginOrEmail);
    Task<bool> UpdatePasswordAsync(string id, string newPasswordHash);
    Task<bool> UpdateLastLoginAsync(string userId);
}