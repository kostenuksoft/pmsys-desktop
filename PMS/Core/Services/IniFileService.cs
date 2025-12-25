using PMS.Core.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PMS.Core.Services
{
    public class IniFileService : IIniFileService
    {
        private readonly Dictionary<string, Dictionary<string, string?>> _data;
        private readonly object _lock = new();

        private string _filePath;

        public IniFileService(string filePath = "", bool loadImmediately = true)
        {
            _filePath = filePath;
            _data = new Dictionary<string, Dictionary<string, string?>>(StringComparer.OrdinalIgnoreCase);
            if(loadImmediately) Load();
        }

        public void Load()
        {
            lock (_lock)
            {
                if (_data.Count > 0)
                {
                    _data.Clear();
                }

                if (!File.Exists(_filePath))
                {
                    return;
                }

                string currentSection = string.Empty;

                try
                {
                    foreach (var line in File.ReadAllLines(_filePath, Encoding.UTF8))
                    {
                        var trimmedLine = line.Trim();

                        if (string.IsNullOrWhiteSpace(trimmedLine) ||
                            trimmedLine.StartsWith(";") ||
                            trimmedLine.StartsWith("#"))
                            continue;

                        if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                        {
                            currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                            if (!_data.ContainsKey(currentSection))
                                _data[currentSection] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                        }
                        else if (trimmedLine.Contains("="))
                        {
                            var index = trimmedLine.IndexOf('=');
                            var key = trimmedLine.Substring(0, index).Trim();
                            var value = trimmedLine.Substring(index + 1).Trim();

                            if (!string.IsNullOrEmpty(currentSection) && !string.IsNullOrEmpty(key))
                            {
                                _data[currentSection][key] = value;
                            }
                        }
                    }
                }
                catch
                {
                    _data.Clear();
                }
            }
        }

        public void Save()
        {
            lock (_lock)
            {
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var sb = new StringBuilder();

                sb.AppendLine("; Polyclinic Management System Settings File");
                sb.AppendLine($"; Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine();

                foreach (var section in _data.OrderBy(s => s.Key))
                {
                    sb.AppendLine($"[{section.Key}]");

                    foreach (var kvp in section.Value.OrderBy(k => k.Key))
                    {
                        sb.AppendLine($"{kvp.Key}={kvp.Value}");
                    }

                    sb.AppendLine();
                }

                File.WriteAllText(_filePath, sb.ToString(), Encoding.UTF8);
            }
        }

        public void Delete()
        {
            File.Delete(_filePath);
        }

        public void SetFilePath(string newPath)
        {
            _filePath = newPath;
        }



        public bool Exists()
        {
            return File.Exists(_filePath);
        }

        public void WriteValue(string section, string key, string? value)
        {
            lock (_lock)
            {
                if (!_data.ContainsKey(section))
                    _data[section] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

                _data[section][key] = value;
            }
        }

        public string? ReadValue(string section, string key, string? defaultValue = "")
        {
            lock (_lock)
            {
                if (_data.ContainsKey(section) && _data[section].ContainsKey(key))
                    return _data[section][key];

                return defaultValue;
            }
        }

        public Dictionary<string, Dictionary<string, string?>> ReadAllSections()
        {
            lock (_lock)
            {
                var result = new Dictionary<string, Dictionary<string, string?>>(StringComparer.OrdinalIgnoreCase);
                foreach (var section in _data)
                {
                    result[section.Key] = new Dictionary<string, string?>(section.Value, StringComparer.OrdinalIgnoreCase);
                }
                return result;
            }
        }

        public bool SectionExists(string section)
        {
            lock (_lock)
            {
                return _data.ContainsKey(section);
            }
        }

        public bool KeyExists(string section, string key)
        {
            lock (_lock)
            {
                return _data.ContainsKey(section) && _data[section].ContainsKey(key);
            }
        }

        public void DeleteKey(string section, string key)
        {
            lock (_lock)
            {
                if (_data.TryGetValue(section, out var value))
                {
                    value.Remove(key);
                }
            }
        }

        public void DeleteSection(string section)
        {
            lock (_lock)
            {
                _data.Remove(section);
            }
        }



       
    }
}