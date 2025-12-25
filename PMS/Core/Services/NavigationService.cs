using System;
using System.Collections.Generic;
using PMS.Core.Services.Interfaces;
using PMS.ViewModels;
using ReactiveUI;
using Serilog;

namespace PMS.Core.Services;

public class NavigationService : ReactiveObject, INavigationService
{
    private readonly Stack<BaseViewModel> _navigationStack = new();
    private readonly IServiceProvider _serviceProvider;
    private BaseViewModel? _currentView;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public BaseViewModel CurrentView
    {
        get => _currentView;
        private set
        {
            if (_currentView != value)
            {
                _currentView?.CleanUp();
                this.RaiseAndSetIfChanged(ref _currentView, value);
                _currentView?.Initialize();
                CurrentViewChanged?.Invoke(this, _currentView);
            }
        }
    }

    public bool CanNavigateBack => _navigationStack.Count > 1;

    public event EventHandler<BaseViewModel>? CurrentViewChanged;

    public TViewModel NavigateTo<TViewModel>() where TViewModel : BaseViewModel
    {
        return NavigateTo<TViewModel>(new object());
    }

    public TViewModel NavigateTo<TViewModel>(object parameter) where TViewModel : BaseViewModel
    {
        try
        {

            var viewModel = (TViewModel)_serviceProvider.GetService(typeof(TViewModel))!;

            if (viewModel == null)
                throw new InvalidOperationException($"ViewModel {typeof(TViewModel).Name} not registered in DI container");

            if (viewModel is IParameterizedViewModel parameterized && parameter != null)
            {
                parameterized.Initialize(parameter);
            }

            if (_currentView != null)
            {
                _navigationStack.Push(_currentView);
            }

            CurrentView = viewModel;
            return viewModel;
        }
        catch(Exception ex)
        {
            throw new Exception("BAD!");
        }

           
    }

    public void Clear()
    {
        while (_navigationStack.Count > 0)
        {
            var vm = _navigationStack.Pop();
            vm?.CleanUp();
        }

        _currentView?.CleanUp();
        _currentView = null;
    }

    public void NavigateBack()
    {
        CurrentView = _navigationStack.Peek();
        if (CanNavigateBack)
        {
            _navigationStack.Pop();
            CurrentView = _navigationStack.Peek();
        }
    }

    public void ClearHistory()
    {
        _navigationStack.Clear();
    }
}

public interface IParameterizedViewModel
{
    void Initialize(object parameter);
}