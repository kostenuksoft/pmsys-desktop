using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PMS.Core.Enums.General;
using PMS.Core.Models.Common;
using PMS.Core.Services.Interfaces;
using Serilog;

namespace PMS.Core.Services
{
    public class LoggingSettingsService : ILoggingSettingsService
    {
        private readonly ILogger _logger;
        private const string ConfigPath = "logging.ini";

        public LoggingSettingsService(ILogger logger)
        {
            _logger = logger;
        }

        public LoggingSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    _logger.Warning("Logging configuration file not found, using defaults");
                    return new LoggingSettings();
                }

                var iniFile = new IniFileService(ConfigPath);

                var settings = new LoggingSettings
                {
                    FilePath = iniFile.ReadValue("Logging", "FilePath", "Logs/PMS-{Date}.log"),
                    RollingInterval = iniFile.ReadValue("Logging", "RollingInterval", "Day"),
                    RetainedFileCountLimit = int.Parse(iniFile.ReadValue("Logging", "RetainedFileCountLimit", "30") ?? "30")
                };

                _logger.Information("Logging configuration loaded successfully");
                return settings;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load logging configuration");
                return new LoggingSettings();
            }
        }

        public bool SaveSettings(LoggingSettings settings)
        {
            try
            {
                var iniFile = new IniFileService(ConfigPath, loadImmediately: false);

                iniFile.WriteValue("Logging", "FilePath", settings.FilePath);
                iniFile.WriteValue("Logging", "RollingInterval", settings.RollingInterval);
                iniFile.WriteValue("Logging", "RetainedFileCountLimit", settings.RetainedFileCountLimit.ToString());

                iniFile.Save();

                _logger.Information("Logging configuration saved successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to save logging configuration");
                return false;
            }
        }

        public bool Exists()
        {
            return File.Exists(ConfigPath);
        }

        public void Delete()
        {
            File.Delete(ConfigPath);
        }

        public ValidationResult ValidateSettings(LoggingSettings settings, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(settings.FilePath))
                {
                    errors.Add("Не вказано шлях до файлу логів.");
                }

                var validIntervals = new[] { "Infinite", "Year", "Month", "Day", "Hour", "Minute" };
                if (!validIntervals.Contains(settings.RollingInterval, StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"Невірний інтервал ротації логів. Допустимі значення: {string.Join(", ", validIntervals)}");
                }

                if (settings.RetainedFileCountLimit < 1)
                {
                    errors.Add("Кількість збережених файлів має бути не менше 1.");
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
                errorMessage = $"Помилка перевірки конфігурації логування: {ex.Message}";
                _logger.Error(ex, "Logging configuration validation error");
                return ValidationResult.Corrupted;
            }
        }

        public LoggingSettings ResetToDefaults()
        {
            _logger.Information("Resetting logging configuration to defaults");
            return new LoggingSettings();
        }
    }
}