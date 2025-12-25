using Avalonia.Media.Imaging;
using ReactiveUI;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace PMS.ViewModels.Dialogs;

public class AboutInfoViewModel : BaseViewModel
{
    private string _appName = string.Empty;
    private string _version = string.Empty;
    private string? _copyright;
    private string? _description;
    private string? _gitHub;
    private string? _avalonia;
    private string? _fluentAvaloniaDocs;
    private string? _license;
    private Bitmap? _logo;
    private Dictionary<string, string> _additionalInfo;

    public string AppName
    {
        get => _appName;
        set => SetAndRiseProperty(ref _appName, value);
    }

    public string Version
    {
        get => _version;
        set => SetAndRiseProperty(ref _version, value);
    }

    public string? Copyright
    {
        get => _copyright;
        set => SetAndRiseProperty(ref _copyright, value);
    }

    public string? Description
    {
        get => _description;
        set => SetAndRiseProperty(ref _description, value);
    }

    public string? GitHub
    {
        get => _gitHub;
        set => SetAndRiseProperty(ref _gitHub, value);
    }

    public string? Avalonia
    {
        get => _avalonia;
        set => SetAndRiseProperty(ref _avalonia, value);
    }

    public string? FluentAvaloniaDocs
    {
        get => _fluentAvaloniaDocs;
        set => SetAndRiseProperty(ref _fluentAvaloniaDocs, value);
    }

    public string? License
    {
        get => _license;
        set => SetAndRiseProperty(ref _license, value);
    }

    public Bitmap? Logo
    {
        get => _logo;
        set => SetAndRiseProperty(ref _logo, value);
    }

    public Dictionary<string, string> AdditionalInfo
    {
        get => _additionalInfo;
        set => SetAndRiseProperty(ref _additionalInfo, value);
    }

    private readonly ObservableAsPropertyHelper<bool> _hasAdditionalInfo;
    public bool HasAdditionalInfo => _hasAdditionalInfo.Value;

    public AboutInfoViewModel()
    {
        _hasAdditionalInfo = this
            .WhenAnyValue(x => x.AdditionalInfo)
            .Select(info => info?.Count > 0)
            .ToProperty(this, x => x.HasAdditionalInfo);



        InitializeDefaultValues();
    }

    private void InitializeDefaultValues()
    {
        AppName = ApplicationTitle;
        Version = ApplicationVersion;
        Description = 
            """
            This system is provided as-is under the MIT License, allowing free use, modification, and distribution for both commercial and non-commercial purposes.
            """;
        Copyright = "";
        GitHub = "https://github.com/kostenuksoft";
        Avalonia = "https://avaloniaui.net";
        FluentAvaloniaDocs = "https://github.com/amwx/FluentAvalonia";
        License = "MIT License";
    }
}