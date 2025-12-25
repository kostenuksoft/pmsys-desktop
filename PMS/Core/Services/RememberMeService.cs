using Avalonia.Media;
using PMS.Core.Services.Interfaces;
using Serilog;
using System;
using System.IO;

namespace PMS.Core.Services
{
    public interface IRememberMeService
    {
        void SaveCredentials(string? username, string? password, int daysToRemember);
        RememberedCredentials? TryLoadCredentials();
        void ClearCredentials();
        bool HasSavedCredentials();
    }

    public class RememberMeService : IRememberMeService
    {
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger _logger;
        private readonly IIniFileService _iniFile;

        private const string ConfigPath = "remmecfg.ini";
        private const string TempPath = "remmecfg.tmp";

        public RememberMeService(
            IEncryptionService encryptionService,
            ILogger logger, 
            IIniFileService iniFileService)
        {
            _encryptionService = encryptionService;
            _logger = logger;
            _iniFile = iniFileService;
            _iniFile.SetFilePath(ConfigPath);
        }

        public void SaveCredentials(string? username, string? password, int daysToRemember)
        {

            try
            {
                _iniFile.SetFilePath(TempPath);
                var expirationDate = DateTime.UtcNow.AddDays(daysToRemember);
                _iniFile.WriteValue("Credentials", "Username", _encryptionService.Encrypt(username));
                _iniFile.WriteValue("Credentials", "Password", _encryptionService.Encrypt(password));
                _iniFile.WriteValue("Credentials", "ExpirationDate", expirationDate.ToString("O"));
                _iniFile.WriteValue("Credentials", "CreatedDate", DateTime.UtcNow.ToString("O"));
                _iniFile.Save();
                _encryptionService.EncryptFile(TempPath, ConfigPath);

                _logger.Information(
                    "Remember Me credentials saved for user {Username}, expires on {ExpirationDate:yyyy-MM-dd}",
                    username, expirationDate);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to save Remember Me credentials.");
            }
            finally
            {
                _iniFile.SetFilePath(TempPath);
                if (_iniFile.Exists())
                {
                    _iniFile.Delete();
                }
                _iniFile.SetFilePath(ConfigPath);
            }
        }

        public RememberedCredentials? TryLoadCredentials()
        {
            try
            {
                if (!_iniFile.Exists())
                {
                    _logger.Debug("No saved credentials found");
                    return null;
                }

                _encryptionService.DecryptFile(ConfigPath, TempPath);

                _iniFile.SetFilePath(TempPath);
                _iniFile.Load();
                var encryptedUsername = _iniFile.ReadValue("Credentials", "Username");
                var encryptedPassword = _iniFile.ReadValue("Credentials", "Password");
                var expirationDateStr = _iniFile.ReadValue("Credentials", "ExpirationDate");
                var createdDateStr = _iniFile.ReadValue("Credentials", "CreatedDate");

                if (string.IsNullOrEmpty(encryptedUsername) ||
                    string.IsNullOrEmpty(encryptedPassword) ||
                    string.IsNullOrEmpty(expirationDateStr))
                {
                    _logger.Warning("Invalid or incomplete saved credentials");
                    ClearCredentials();
                    return null;
                }

                if (!DateTime.TryParse(expirationDateStr, out var expirationDate))
                {
                    _logger.Warning("Invalid expiration date in saved credentials");
                    ClearCredentials();
                    return null;
                }

                DateTime.TryParse(createdDateStr, out var createdDate);

                if (DateTime.UtcNow > expirationDate)
                {
                    _logger.Information("Saved credentials have expired");
                    ClearCredentials();
                    return null;
                }

                var username = _encryptionService.Decrypt(encryptedUsername);
                var password = _encryptionService.Decrypt(encryptedPassword);


                _logger.Information(
                    $"Loaded saved credentials for user {username}, expires on {expirationDate:yyyy-MM-dd}");

                return new RememberedCredentials
                {
                    Username = username,
                    Password = password,
                    ExpirationDate = expirationDate,
                    CreatedDate = createdDate,
                    IsValid = !string.IsNullOrEmpty(username) &&
                              !string.IsNullOrEmpty(password) &&
                              DateTime.UtcNow <= expirationDate
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load saved credentials");
                ClearCredentials();
                return null;
            }
            finally
            {
                if (_iniFile.Exists())
                {
                    _iniFile.Delete();
                }
                _iniFile.SetFilePath(ConfigPath);
            }
        }

        public void ClearCredentials()
        {
            try
            {
                _iniFile.SetFilePath(ConfigPath);
                if (_iniFile.Exists())
                {
                    _iniFile.Delete();
                    _logger.Information("Saved credentials cleared");
                }

                _iniFile.SetFilePath(TempPath);
                if (_iniFile.Exists())
                {
                    _iniFile.Delete();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to clear saved credentials");
            }
            finally
            {
                _iniFile.SetFilePath(ConfigPath);
            }
        }

        public bool HasSavedCredentials()
        {
            return File.Exists(ConfigPath);
        }

    }

    public class RememberedCredentials
    {
        public string? Username { get; set; } = string.Empty;
        public string? Password { get; set; } = string.Empty;
        public DateTime ExpirationDate { get; set; }
        public DateTime CreatedDate { get; set; }

        public bool IsValid { get; set; }

        public int DaysUntilExpiration => (int)(ExpirationDate - DateTime.UtcNow).TotalDays;
    }
}