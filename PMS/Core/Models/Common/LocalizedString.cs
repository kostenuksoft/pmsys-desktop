using PMS.Core.Services.Interfaces;
using System;
using System.ComponentModel;

namespace PMS.Core.Models.Common;

public class LocalizedString : INotifyPropertyChanged
{
    private readonly ILocalizationService _localizationService;
    private readonly string _key;
    private readonly object[] _args;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Value => _args.Length > 0 ? _localizationService.GetString(_key, _args) : _localizationService.GetString(_key);

    public LocalizedString(ILocalizationService localizationService, string key, params object[] args)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _key = key ?? throw new ArgumentNullException(nameof(key));
        _args = args;
        _localizationService.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, string newLanguage)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
    }
}