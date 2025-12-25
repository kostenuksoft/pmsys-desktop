using System.Collections.Generic;

namespace PMS.Core.Services.Interfaces;

public interface IIniFileService
{
    void Load();

    void Save();

    void Delete();

    bool Exists();

    void SetFilePath(string newPath);

    void WriteValue(string section, string key, string? value);

    string? ReadValue(string section, string key, string? defaultValue = "");

    bool SectionExists(string section);

    bool KeyExists(string section, string key);

    void DeleteKey(string section, string key);

    void DeleteSection(string section);

    Dictionary<string, Dictionary<string, string?>> ReadAllSections();
     
}