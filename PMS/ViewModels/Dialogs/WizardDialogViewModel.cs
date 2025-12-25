using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using PMS.Core.Models.Common;
using ReactiveUI;

namespace PMS.ViewModels.Dialogs;

public class WizardDialogViewModel : BaseDialogViewModel<Dictionary<string, object>>
{
    private readonly WizardPage[] _pages;
    private int _currentPageIndex;
    private readonly Dictionary<string, object> _data = new();

    public WizardPage CurrentPage => _pages[_currentPageIndex];
    public int CurrentPageIndex
    {
        get => _currentPageIndex;
        set
        {
            _currentPageIndex = value;
        }
    }

    public int PageCount { get; set; }

    public bool CanGoBack => _currentPageIndex > 0;
    public bool CanGoNext => _currentPageIndex < _pages.Length - 1;
    public bool IsLastPage => _currentPageIndex == _pages.Length - 1;
    public string NextButtonText => IsLastPage ? "Finish" : CurrentPage.NextButtonText ?? "Next";

    public ICommand GoBackCommand { get; }
    public ICommand GoNextCommand { get; }
    public ICommand SkipCommand { get; }

    public WizardDialogViewModel(string title, WizardPage[] pages)
    {
        Title = title;
        _pages = pages;
        PageCount = pages.Length;
        GoBackCommand = ReactiveCommand.Create(GoBack,
            this.WhenAnyValue(x => x.CanGoBack));

        GoNextCommand = ReactiveCommand.CreateFromTask(GoNextAsync);

        SkipCommand = ReactiveCommand.Create(Skip, this.WhenAnyValue(x => x.CurrentPage.AllowSkip));
    }

    private void GoBack()
    {
        if (CanGoBack) CurrentPageIndex--;
    }

    private async Task GoNextAsync()
    {
        if (CurrentPage.ValidateAsync != null)
        {
            var isValid = await CurrentPage.ValidateAsync(_data);
            if (!isValid)
                return;
        }

        if (IsLastPage)
        {
            await OnPrimaryCommandAsync();
        }
        else
        {
            CurrentPageIndex++;
        }
    }

    private void Skip()
    {
        if (CurrentPage.AllowSkip && CanGoNext)
            CurrentPageIndex++;
    }

    protected override Dictionary<string, object> GetResult() => _data;
}