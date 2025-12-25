using PMS.Core.Models.Common;
using PMS.Core.Services.Interfaces;
using ReactiveUI;

namespace PMS.ViewModels;

public class IRWindowViewModel : BaseViewModel
{
    private readonly ILocalizationService _localization;

    #region Localized Strings

    public LocalizedString WindowTitle { get; private set; }
    public LocalizedString Message { get; private set; }
    public LocalizedString Hint { get; private set; }
    public LocalizedString OkButton { get; private set; }

    #endregion

    public IRWindowViewModel(ILocalizationService localization)
    {
        _localization = localization;
        InitializeLocalizedStrings();
    }

    private void InitializeLocalizedStrings()
    {
        WindowTitle = new LocalizedString(_localization, "irwindow.title");
        Message = new LocalizedString(_localization, "irwindow.message");
        Hint = new LocalizedString(_localization, "irwindow.hint");
        OkButton = new LocalizedString(_localization, "irwindow.okbutton");
    }
}
