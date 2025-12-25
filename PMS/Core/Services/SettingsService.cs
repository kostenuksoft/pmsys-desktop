using System;
using System.Collections.Generic;
using System.IO;
using PMS.Core.Models.Common;
using PMS.Core.Services.Interfaces;

namespace PMS.Core.Services;

public interface ISettingsService1
{
    T GetSettings<T>();
    T GetValue<T>(string section, string key, T defaultValue); 
    void SaveValue(string section, string key, string? value);
}

public class SettingsService1 : ISettingsService1
{
    private readonly IIniFileService _iniFile;

    public SettingsService1()
    {
        _iniFile = new IniFileService();
        CreateDefaultApplicationSettings();
        CreateDefaultLoggingSettings();
    }

    public T GetValue<T>(string section, string key, T defaultValue)
    {
        var stringValue = _iniFile.ReadValue(section, key, defaultValue?.ToString() ?? string.Empty);

        if (string.IsNullOrEmpty(stringValue))
            return defaultValue;

        try
        {
            if (typeof(T) == typeof(bool))   return (T)(object)bool.Parse(stringValue);
            if (typeof(T) == typeof(int))    return (T)(object)int.Parse(stringValue);
            if (typeof(T) == typeof(string)) return (T)(object)stringValue;

            return (T)Convert.ChangeType(stringValue, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    public void SaveValue(string section, string key, string? value)
    {
        _iniFile.WriteValue(section, key, value);
        _iniFile.Save();
    }

    public T GetSettings<T>()
    {
        return typeof(T).Name switch
        {
            nameof(ApplicationSettings) => (T)(object)new ApplicationSettings
            {
                DefaultLanguage = GetValue("Application", "DefaultLanguageCode", "uk-UA"),
                DateFormat = GetValue("Application", "DateFormat", "dd.MM.yyyy"),
                TimeFormat = GetValue("Application", "TimeFormat", "HH:mm"),
                PageSize = GetValue("Application", "PageSize", 50),
                MaxExportRows = GetValue("Application", "MaxExportRows", 10000)
            },
            nameof(DatabaseSettings) => (T)(object)new LoggingSettings
            {
                FilePath = GetValue("Logging", "FilePath", "Log/pms-{Date}.log"),
                RollingInterval = GetValue("Logging", "RollingInterval", "Hour"),
                RetainedFileCountLimit = GetValue("Logging", "RetainedFileCountLimit", 30)
            },
            _ => throw new ArgumentException($"Settings type {typeof(T).Name} is not supported")
        };
    }

    private void CreateDefaultSettings(string fileName)
    {
        if (File.Exists(fileName) || !DefaultConfigs.TryGetValue(fileName, out var config))
        {
            return;
        }

        _iniFile.SetFilePath(fileName);

        foreach (var (sectionName, keyValues) in config)
        {
            foreach (var (key, value) in keyValues)
            {
                _iniFile.WriteValue(sectionName, key, value);
            }
        }

        _iniFile.Save();
    }

    private void CreateDefaultLoggingSettings(string path = "logset.ini") => CreateDefaultSettings(path);
    private void CreateDefaultApplicationSettings(string path = "appset.ini") => CreateDefaultSettings(path);


    private static readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> DefaultConfigs = new()
    {
        ["logset.ini"] = new Dictionary<string, Dictionary<string, string>>
        {
            ["Logging"] = new()
            {
                ["FilePath"] = "Logs/PMS-.log",
                ["RollingInterval"] = "Day",
                ["RetainedFileCountLimit"] = "15",
                ["ConsoleEnabled"] = "true"
            }
        },
        ["appset.ini"] = new Dictionary<string, Dictionary<string, string>>
        {
            ["Application"] = new()
            {
                ["DefaultLanguage"] = "uk-UA",
                ["DateFormat"] = "dd.MM.yyyy",
                ["TimeFormat"] = "HH:mm",
                ["PageSize"] = "50",
                ["MaxExportRows"] = "10000"
            },
            ["UI"] = new()
            {
                ["Theme"] = "Light",
                ["ConfirmOnExit"] = "true"
            },
            ["Performance"] = new()
            {
                ["LazyLoadingEnabled"] = "true",
                ["BatchSize"] = "100"
            }
        }
    };

}