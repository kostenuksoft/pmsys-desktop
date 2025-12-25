using System;
using PMS.ViewModels;

namespace PMS.Core.Services.Interfaces;

public interface INavigationService
{
    BaseViewModel CurrentView { get; }
    TViewModel NavigateTo<TViewModel>() where TViewModel : BaseViewModel;
    TViewModel NavigateTo<TViewModel>(object parameter) where TViewModel : BaseViewModel;
    void NavigateBack();
    bool CanNavigateBack { get; }
    void ClearHistory();
    void Clear(); 
    event EventHandler<BaseViewModel>? CurrentViewChanged;
}