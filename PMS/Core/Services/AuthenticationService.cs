using DocumentFormat.OpenXml.Spreadsheet;
using PMS.Core.Enums.General;
using PMS.Core.Models;
using PMS.Core.Models.Common;
using PMS.Core.Repositories.Interfaces;
using PMS.Core.Services.Interfaces;
using PMS.Core.Settings;
using Serilog;
using System;
using System.Threading.Tasks;

namespace PMS.Core.Services;


public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IKeysRepository _keysRepository;
    private readonly ISecurityService _securityService;
    private readonly ILogger _logger;

    public AuthenticationService(
        IUserRepository userRepository,
        IKeysRepository keysRepository,
        ISecurityService securityService,
        ILogger logger)
    {
        _userRepository = userRepository;
        _keysRepository = keysRepository;
        _securityService = securityService;
        _logger = logger;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(string? login, string? password)
    {
        try
        {
            var user = await _userRepository.GetByLoginOrEmailAsync(login ?? string.Empty);

            if (user == null)
            {
                _logger.Warning("Login attempt failed: User {Login} not found", login);
                return new AuthenticationResult
                {
                    Success = false,
                    Message = "Користувача не існує у базі.\nПеревірте введені дані."
                };
            }

            var keys = await _keysRepository.GetByLoginAsync(user.Login);

            if (keys == null)
            {
                _logger.Warning("Login attempt failed: Keys not found for user {Login}", login);
                return new AuthenticationResult
                {
                    Success = false,
                    Message = "Користувача не існує у базі.\nПеревірте введені дані."
                };
            }

            if (keys.AccountLocked)
            {
                _logger.Warning("Login attempt failed: Account {Login} is locked", login);
                return new AuthenticationResult
                {
                    Success = false,
                    Message = "Обліковий запис заблоковано. Зверніться до адміністратора"
                };
            }

            if (!_securityService.VerifyPassword(password, keys.PasswordHash))
            {
                await _keysRepository.IncrementFailedLoginAttemptsAsync(
                    keys.Id,
                    SecuritySettings.MaxFailedLoginAttempts);

                _logger.Warning("Login attempt failed: Invalid password for user {Login}", login);
                return new AuthenticationResult
                {
                    Success = false,
                    Message = "Невірний логін або пароль"
                };
            }

            if (keys.FailedLoginAttempts > 0)
            {
                await _keysRepository.ResetFailedAttemptsAsync(keys.Id);
            }

            if (!user.IsActive)
            {
                _logger.Warning("Login attempt failed: User {Login} is inactive", login);
                return new AuthenticationResult
                {
                    Success = false,
                    Message = "Обліковий запис неактивний"
                };
            }

            bool passwordExpired = _securityService.IsPasswordExpired(
                keys.LastPasswordChange,
                keys.PasswordExpires.HasValue
                    ? (int?)(keys.PasswordExpires.Value - keys.LastPasswordChange).TotalDays
                    : null
            );

            await _userRepository.UpdateLastLoginAsync(user.Id);
            await _keysRepository.UpdateLastLoginAsync(keys.Id);

            var token = _securityService.GenerateSecureToken();

            _logger.Information("User {Login} successfully authenticated", login);

            return new AuthenticationResult
            {
                Success = true,
                Message = "Успішна автентифікація",
                Token = token,
                Role = user.Role,
                UserId = user.Id,
                FullName = user.FullName,
                RequiresPasswordChange = passwordExpired
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Authentication error for user {Login}", login);
            return new AuthenticationResult
            {
                Success = false,
                Message = "Помилка автентифікації. Спробуйте пізніше"
            };
        }
    }

    public async Task<bool> ChangePasswordAsync(string userId, PasswordUpdateRequest request)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.Warning("Password change failed: User {UserId} not found", userId);
                return false;
            }

            var keys = await _keysRepository.GetByLoginAsync(user.Login);
            if (keys == null)
            {
                _logger.Warning("Password change failed: Keys not found for user {Login}", user.Login);
                return false;
            }

            if (!_securityService.VerifyPassword(request.CurrentPassword, keys.PasswordHash))
            {
                _logger.Warning("Password change failed: Invalid current password for user {Login}", user.Login);
                return false;
            }

            if (!_securityService.ValidatePasswordStrength(request.NewPassword, out string errorMessage))
            {
                _logger.Warning("Password change failed: {ErrorMessage} for user {Login}",
                    errorMessage, user.Login);
                return false;
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                _logger.Warning("Password change failed: Passwords don't match for user {Login}", user.Login);
                return false;
            }

            var newHash = _securityService.HashPassword(request.NewPassword);

            DateTime? expiresAt = null;
            if (SecuritySettings.RememberMeDuration > 0)
            {
                expiresAt = DateTime.UtcNow.AddDays(SecuritySettings.RememberMeDuration * 6);
            }

            var success = await _keysRepository.UpdatePasswordWithExpiryAsync(keys.Id, newHash, expiresAt);

            if (success)
            {
                _logger.Information("Password successfully changed for user {Login}", user.Login);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error changing password for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> UnlockAccountAsync(string login)
    {
        try
        {
            var keys = await _keysRepository.GetByLoginAsync(login);
            if (keys == null)
            {
                _logger.Warning("Unlock account failed: Keys not found for login {Login}", login);
                return false;
            }

            var success = await _keysRepository.UnlockAccountAsync(keys.Id);

            if (success)
            {
                _logger.Information("Account unlocked for login {Login}", login);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error unlocking account {Login}", login);
            return false;
        }
    }

    public async Task<User?> GetCurrentUserByIdAsync(string userId)
    {
        try
        {
            return await _userRepository.GetByIdAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting current user {UserId}", userId);
            return null;
        }
    }

    public async Task<User?> GetCurrentUserByLoginAsync(string login)
    {
        try
        {
            return await _userRepository.GetByLoginAsync(login);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting current user {login}", login);
            return null;
        }
    }

    public async Task<Keys?> GetCurrentKeysAsync(string? login)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(login))
                return null;

            return await _keysRepository.GetByLoginAsync(login);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting current keys for login {Login}", login);
            return null;
        }
    }

    public async Task UpdateLastLoginAsync(string userId)
    {
        try
        {
            await _userRepository.UpdateLastLoginAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error updating last login for user {UserId}", userId);
        }
    }

    public async Task<bool> CreateInitialAdminAsync()
    {
        try
        {
            var adminExists = await _keysRepository.ExistsByLoginAsync("admin");
            if (adminExists)
            {
                _logger.Information("Admin account already exists");
                return false;
            }

            var adminPassword = "ko5757";
            var passwordHash = _securityService.HashPassword(adminPassword);

            var adminKeys = new Keys
            {
                Login = "admin",
                PasswordHash = passwordHash,
                AccessRights = new KeyAccessRights
                {
                    DatabaseAccess = DatabaseAccessLevel.Full,
                    Role = UserRole.Administrator,
                    SpecificPermissions =
                    [
                        "create_users", "modify_users", "delete_users",
                        "view_all_data", "modify_all_data", "delete_all_data",
                        "run_aggregations", "export_data", "manage_schedules",
                        "issue_certificates"
                    ]
                },
                LastPasswordChange = DateTime.UtcNow,
                PasswordExpires = DateTime.UtcNow.AddDays(1),
                AccountLocked = false,
                FailedLoginAttempts = 0
            };

            await _keysRepository.CreateAsync(adminKeys);

            var adminUser = new User
            {
                Login = "admin",
                PasswordHash = passwordHash,
                Role = UserRole.Administrator,
                FullName = "Адміністратор",
                Email = "admin@polyclinic.local",
                IsActive = true,
                AccessRights = new AccessRights
                {
                    ViewData = true,
                    EditData = true,
                    DeleteData = true,
                    RunAggregations = true,
                    SaveResults = true,
                    ManageUsers = true
                }
            };

            await _userRepository.CreateAsync(adminUser);

            _logger.Information("Initial admin account created successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error creating initial admin account");
            return false;
        }
    }
}