using FluentAvalonia.UI.Controls;
using PMS.ViewModels.Tabs.Interfaces;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;

namespace PMS.ViewModels;

public abstract class PageViewModelBase : 
    BaseViewModel,
    ITabViewModel, 
    IInitializableViewModel
{
    public string Title
    {
        get => _title;
        set => SetAndRiseProperty(ref _title, value);
    }

    public string Message
    {
        get => _message;
        set => SetAndRiseProperty(ref _message, value);
    }

    public InfoBarSeverity MessageSeverity
    {
        get => _severity;
        set => SetAndRiseProperty(ref _severity, value);
    }

    public bool HasError
    {
        get => _hasError;
        set => SetAndRiseProperty(ref _hasError, value);
    }

    public bool IsInfoBarVisible
    {
        get => _isInfobarVisible;
        set => SetAndRiseProperty(ref _isInfobarVisible, value);
    }


    public bool IsBusy
    {
        get => _isBusy;
        set => SetAndRiseProperty(ref _isBusy, value);
    }

    public string BusyMessage
    {
        get => _busyMessage;
        set => SetAndRiseProperty(ref _busyMessage, value);
    }

    private bool _isBusy;
    private string _busyMessage = string.Empty;

    private string _title = string.Empty;
    private string _message = string.Empty;
    private bool   _hasError;
    private bool _isInfobarVisible;
    private InfoBarSeverity _severity = InfoBarSeverity.Informational;

    protected PageViewModelBase()
    {
        CollapseInfoBarCommand = ReactiveCommand.Create(async () =>
        {
            IsInfoBarVisible = false;
            await Task.Delay(10);
            IsInfoBarVisible = true;
        });
    }

    public override void Initialize()
    {
        IsInfoBarVisible = false;
    }

    public override void CleanUp()
    {
        Message = string.Empty;
        HasError = false;
        IsInfoBarVisible = false;
    }

    public virtual string TabHeader => GetType().Name.Replace("ViewModel", "");
    public virtual string? TabIconSource => null;
    public virtual bool CanClose => !HasUnsavedChanges;

    public virtual Task InitializeAsync(object? parameter)
    {
        return Task.CompletedTask;
    }

    protected void MarkAsModified()
    {
        HasUnsavedChanges = true;
    }

    protected void MarkAsSaved()
    {
        HasUnsavedChanges = false;
    }

    public void ClearErrorState()
    {
        HasError = false;
    }

    public void ShowInfoBar(string message, string title = "Інформація")
    {
        Title = title;
        Message = message;
        MessageSeverity = InfoBarSeverity.Informational;
        IsInfoBarVisible = true;
    }

    public void ShowWarningBar(string message, string title = "Увага")
    {
        Title = title;
        Message = message;
        MessageSeverity = InfoBarSeverity.Warning;
        IsInfoBarVisible = true;
    }

    public void ShowErrorBar(string message, string title = "Помилка")
    {
        Title = title;
        MessageSeverity = InfoBarSeverity.Error;
        Message = message;
        HasError = true;
        IsInfoBarVisible = true;
    }

    public void ShowSuccessBar(string message, string title = "Успіх")
    {
        Title = title;
        Message = message;
        MessageSeverity = InfoBarSeverity.Success;
        IsInfoBarVisible = true;
    }

    public ICommand CollapseInfoBarCommand { get; set; }

}