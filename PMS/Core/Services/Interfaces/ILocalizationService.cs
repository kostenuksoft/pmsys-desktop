using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace PMS.Core.Services.Interfaces;

public interface ILocalizationService : INotifyPropertyChanged
{
    string CurrentLanguage { get; }
    IReadOnlyList<string> AvailableLanguages { get; }

    string GetString(string key);
    string GetString(string key, params object[] args);
    bool SetLanguage(string language);
    event EventHandler<string>? LanguageChanged;
}