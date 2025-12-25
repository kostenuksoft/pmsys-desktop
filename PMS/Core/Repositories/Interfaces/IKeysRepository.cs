using System;
using System.Threading.Tasks;
using PMS.Core.Models;

namespace PMS.Core.Repositories.Interfaces;


public interface IKeysRepository : IBaseRepository<Keys>
{
    
    Task<Keys?> GetByLoginAsync(string login);
    Task<bool> UpdatePasswordAsync(string keysId, string passwordHash);
    Task<bool> UpdatePasswordWithExpiryAsync(string keysId, string passwordHash, DateTime? expiresAt);
    Task<bool> UnlockAccountAsync(string keysId);
    Task<bool> ResetFailedAttemptsAsync(string keysId);
    Task<bool> IncrementFailedLoginAttemptsAsync(string keysId, int maxAttempts);
    Task<bool> ExistsByLoginAsync(string login);
    Task<bool> UpdateLastLoginAsync(string keysId);
}