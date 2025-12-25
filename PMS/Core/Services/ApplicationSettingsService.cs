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
    public class ApplicationSettingsService : IApplicationSettingsService
    {
        private readonly ILogger _logger;
        private const string ConfigPath = "application.ini";

        public ApplicationSettingsService(ILogger logger)
        {
            _logger = logger;
        }

        public ApplicationSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    _logger.Warning("Application configuration file not found, using defaults");
                    return new ApplicationSettings();
                }

                var iniFile = new IniFileService(ConfigPath);

                var settings = new ApplicationSettings
                {
                    DefaultLanguage = iniFile.ReadValue("Localization", "DefaultLanguage", "uk-UA"),
                    DateFormat = iniFile.ReadValue("Localization", "DateFormat", "dd.MM.yyyy"),
                    TimeFormat = iniFile.ReadValue("Localization", "TimeFormat", "HH:mm"),
                    PageSize = int.Parse(iniFile.ReadValue("UI", "PageSize", "50")),
                    MaxExportRows = int.Parse(iniFile.ReadValue("Export", "MaxExportRows", "10000")),
                    AskWhenQuitting = bool.Parse(iniFile.ReadValue("UI", "AskWhenQuitting", "true"))
                };

                _logger.Information("Application configuration loaded successfully");
                return settings;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load application configuration");
                return new ApplicationSettings();
            }
        }

        public bool SaveSettings(ApplicationSettings settings)
        {
            try
            {
                var iniFile = new IniFileService(ConfigPath, loadImmediately: false);

                iniFile.WriteValue("Localization", "DefaultLanguage", settings.DefaultLanguage);
                iniFile.WriteValue("Localization", "DateFormat", settings.DateFormat);
                iniFile.WriteValue("Localization", "TimeFormat", settings.TimeFormat);

                iniFile.WriteValue("UI", "PageSize", settings.PageSize.ToString());
                iniFile.WriteValue("UI", "AskWhenQuitting", settings.AskWhenQuitting.ToString());

                iniFile.WriteValue("Export", "MaxExportRows", settings.MaxExportRows.ToString());

                iniFile.Save();

                _logger.Information("Application configuration saved successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to save application configuration");
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

        public ValidationResult ValidateSettings(ApplicationSettings settings, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(settings.DateFormat))
                {
                    errors.Add("Не вказано формат дати.");
                }

                if (string.IsNullOrWhiteSpace(settings.TimeFormat))
                {
                    errors.Add("Не вказано формат часу.");
                }

                if (settings.PageSize is < 10 or > 500)
                {
                    errors.Add("Розмір сторінки має бути від 10 до 500.");
                }

                if (settings.MaxExportRows is < 100 or > 100000)
                {
                    errors.Add("Максимальна кількість рядків для експорту має бути від 100 до 100000.");
                }

                try
                { 
                    DateTime.Now.ToString(settings.DateFormat);
                }
                catch
                {
                    errors.Add($"Невірний формат дати: {settings.DateFormat}");
                }

                try
                { 
                    DateTime.Now.ToString(settings.TimeFormat);
                }
                catch
                {
                    errors.Add($"Невірний формат часу: {settings.TimeFormat}");
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
                errorMessage = $"Помилка перевірки конфігурації програми: {ex.Message}";
                _logger.Error(ex, "Application configuration validation error");
                return ValidationResult.Corrupted;
            }
        }

        public ApplicationSettings ResetToDefaults()
        {
            _logger.Information("Resetting application configuration to defaults");
            return new ApplicationSettings();
        }
    }
}