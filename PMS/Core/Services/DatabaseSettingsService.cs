using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using PMS.Core.Database;
using PMS.Core.Services.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using PMS.Core.Enums.General;
using PMS.Core.Models.Common;

namespace PMS.Core.Services
{
    public class DatabaseSettingsService : IDatabaseSettingsService
    {
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger _logger;

        private const string ConfigPath = "dbcfg.ini";
        private const string TempPath = "dbcfg.tmp";

        public DatabaseSettingsService(IEncryptionService encryptionService, ILogger logger)
        {
            _encryptionService = encryptionService;
            _logger = logger;
        }

        public DatabaseSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    _logger.Warning("Database configuration file not found");
                    return ResetToDefaults();
                }

                _encryptionService.DecryptFile(ConfigPath, TempPath);

                var iniFile = new IniFileService(TempPath);

                var portStr = iniFile.ReadValue("Database", "Port", "27017");
                var timeoutStr = iniFile.ReadValue("Connection", "Timeout", "30");
                var useAuthStr = iniFile.ReadValue("Authentication", "UseAuthentication", "true");

                if (!int.TryParse(portStr, out var port) || port <= 0 || port > 65535)
                {
                    _logger.Warning("Invalid port value: {Port}, using default 27017", portStr);
                    port = 27017;
                }

                if (!int.TryParse(timeoutStr, out var timeout) || timeout <= 0)
                {
                    _logger.Warning("Invalid timeout value: {Timeout}, using default 30", timeoutStr);
                    timeout = 30;
                }

                if (!bool.TryParse(useAuthStr, out var useAuth))
                {
                    _logger.Warning("Invalid UseAuthentication value: {Value}, using default true", useAuthStr);
                    useAuth = true;
                }

                var config = new DatabaseSettings
                {
                    Host = iniFile.ReadValue("Database", "Host", "localhost"),
                    Port = port,
                    DatabaseName = iniFile.ReadValue("Database", "DatabaseName"),

                    UseAuthentication = useAuth,
                    Username = iniFile.ReadValue("Authentication", "Username"),
                    Password = iniFile.ReadValue("Authentication", "Password"),
                    AuthDatabase = iniFile.ReadValue("Authentication", "AuthDatabase"),

                    ConnectionTimeout = timeout,
                };

                if (File.Exists(TempPath))
                {
                    File.Delete(TempPath);
                }

                _logger.Information("Database configuration loaded successfully");
                return config;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load database configuration");

                if (File.Exists(TempPath)) File.Delete(TempPath);

                return new DatabaseSettings();
            }
        }

        public bool SaveSettings(DatabaseSettings config)
        {
            try
            {
                var iniFile = new IniFileService(TempPath);

                iniFile.WriteValue("Database", "Host", config.Host);
                iniFile.WriteValue("Database", "Port", config.Port.ToString());
                iniFile.WriteValue("Database", "DatabaseName", config.DatabaseName);

                iniFile.WriteValue("Authentication", "UseAuthentication", config.UseAuthentication.ToString());
                iniFile.WriteValue("Authentication", "Username", _encryptionService.Encrypt(config.Username));
                iniFile.WriteValue("Authentication", "Password", _encryptionService.Encrypt(config.Password));
                iniFile.WriteValue("Authentication", "AuthDatabase", config.AuthDatabase);

                iniFile.WriteValue("Connection", "Timeout", config.ConnectionTimeout.ToString());

                iniFile.Save();
                _encryptionService.EncryptFile(TempPath, ConfigPath);

                if (File.Exists(TempPath))
                    File.Delete(TempPath);

                _logger.Information("Database configuration saved successfully");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to save database configuration");

                if (File.Exists(TempPath))
                    File.Delete(TempPath);

                return false;
            }

            return true;
        }

        public bool Exists()
        {
            return File.Exists(ConfigPath);
        }

        public void Delete()
        {
            File.Delete(ConfigPath);
        }

        public ValidationResult ValidateSettings(DatabaseSettings settings, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(settings.Host))
                {
                    errors.Add("Не вказано адресу сервера.");
                }

                if (settings.Port is <= 0 or > 65535)
                {
                    errors.Add("Невірний порт сервера (має бути від 1 до 65535)");
                }

                if (string.IsNullOrWhiteSpace(settings.DatabaseName))
                {
                    errors.Add("Не вказано назву бази даних.");
                }

                if (settings.UseAuthentication)
                {
                    if (string.IsNullOrWhiteSpace(settings.Username))
                    {
                        errors.Add("Не вказано ім'я користувача.");
                    }

                    if (string.IsNullOrWhiteSpace(settings.Password))
                    {
                        errors.Add("Не вказано пароль.");
                    }

                    if (string.IsNullOrWhiteSpace(settings.AuthDatabase))
                    {
                        errors.Add("Не вказано авторизаційну базу даних.");
                    }
                }
                

                if (errors.Any())
                {
                    errorMessage = string.Join(Environment.NewLine, errors);
                    return ValidationResult.Invalid;
                }

                return ValidationResult.Valid;
            }
            catch (Exception ex)
            {
                errorMessage = $"Помилка читання конфігурації: {ex.Message}";
                _logger.Error(ex, "Configuration validation error");
                return ValidationResult.Corrupted;
            }
        }

        public DatabaseSettings ResetToDefaults()
        {
            _logger.Information("Resetting database configuration to defaults");
            return new DatabaseSettings();
        }

        public async Task<bool> TestConnectionAsync(DatabaseSettings dbSettings, 
            string? encryptedUsername = "",
            string? encryptedPassword = "")
        {
            string? tempUsername = null;
            string? tempPassword = null;
            DatabaseSettings? tempSettings = null;
            string? connectionString = null;

            try
            {
                if (dbSettings.UseAuthentication && !string.IsNullOrEmpty(encryptedUsername) && !string.IsNullOrEmpty(encryptedPassword))
                {
                    tempUsername = _encryptionService.Decrypt(encryptedUsername);
                    tempPassword = _encryptionService.Decrypt(encryptedPassword);
                }

                tempSettings = new DatabaseSettings
                {
                    Host = dbSettings.Host,
                    Port = dbSettings.Port,
                    DatabaseName = dbSettings.DatabaseName,
                    UseAuthentication = dbSettings.UseAuthentication,
                    Username = tempUsername ?? dbSettings.Username,
                    Password = tempPassword ?? dbSettings.Password,
                    AuthDatabase = dbSettings.AuthDatabase,
                };

                connectionString = BuildConnectionString(tempSettings);

                tempSettings.Username = null;
                tempSettings.Password = null;

                var mongoSettings = MongoClientSettings.FromConnectionString(connectionString);
                mongoSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);

                connectionString = null;

                var client = new MongoClient(mongoSettings);

                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                {
                    var databaseNames = await client.ListDatabaseNamesAsync(cts.Token);
                    var dbExists = await databaseNames.ToListAsync(cts.Token);

                    if (!dbExists.Contains(dbSettings.DatabaseName))
                    {
                        _logger.Warning("Database '{DatabaseName}' does not exist", dbSettings.DatabaseName);
                        return false;
                    }
                }

                _logger.Information("Database connection test successful");
                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.Warning("Database connection test timed out");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Database connection test failed");
                return false;
            }
            finally
            {
                ClearRef(ref tempUsername);
                ClearRef(ref tempPassword);
                ClearRef(ref connectionString);

                if (tempSettings != null)
                {
                    tempSettings.Username = null;
                    tempSettings.Password = null;
                    tempSettings = null;
                }
            }
        }

        private static void ClearRef(ref string? sensitive)
        {
            if (string.IsNullOrEmpty(sensitive)) return;
            sensitive = null;
        }

        public string? BuildConnectionString(DatabaseSettings config)
        {
            if (config.UseAuthentication && !string.IsNullOrEmpty(config.Username) && !string.IsNullOrEmpty(config.Password))
            {
                var username = Uri.EscapeDataString(config.Username);
                var password = Uri.EscapeDataString(config.Password);
                return $"mongodb://{username}:{password}@{config.Host}:{config.Port}/{config.DatabaseName}?authSource={config.AuthDatabase}";
            }
            
            return $"mongodb://{config.Host}:{config.Port}/{config.DatabaseName}";
        }


        public static bool ValidateDatabaseSettings(out string errorMessage, DatabaseSettings settings)
        {
            try
            {
                var dbConfigService = App.GetService<IDatabaseSettingsService>();
                var validationResult = dbConfigService.ValidateSettings(settings, out var errorMsg);

                if (validationResult != ValidationResult.Valid)
                {
                    Log.Warning("Configuration validation failed: {ErrorMsg}", errorMsg);
                    errorMessage = errorMsg;
                    return false;
                }

                errorMessage = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Configuration validation error");
                errorMessage = ex.Message;
                return false;
            }
        }
    }

   
}