using System.Threading.Tasks;
using PMS.Core.Models;

namespace PMS.Core.Services.Interfaces;


public interface IAuthenticationService
{
    
    Task<AuthenticationResult> AuthenticateAsync(string? login, string? password);
    Task<bool> ChangePasswordAsync(string userId, PasswordUpdateRequest request);
    Task<bool> UnlockAccountAsync(string login);
    Task<User?> GetCurrentUserByIdAsync(string userId);
    Task<User?> GetCurrentUserByLoginAsync(string login);
    Task UpdateLastLoginAsync(string userId);
    Task<Keys?> GetCurrentKeysAsync(string? login);
    Task<bool> CreateInitialAdminAsync();
}