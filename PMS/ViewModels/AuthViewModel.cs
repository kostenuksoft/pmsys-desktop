using Avalonia.Controls;
using PMS.Core.Models.Common;
using PMS.Core.Services;
using PMS.Core.Services.Interfaces;
using PMS.Views.Window;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PMS.ViewModels;

public class AuthViewModel : BaseViewModel
{
    private readonly ILocalizationService _localization;
    private readonly INavigationService _navigationService;
    private readonly IWindowService _windowService;
    private readonly IViewLocator _viewLocator;
   

    private LanguageOption _selectedDefaultLanguage;

    private Control _currentView;
    private BaseViewModel _currentViewModel;
    private string _selectedNavigationItem = "Login";

    public AuthViewModel(
        ILocalizationService localization,
        INavigationService navigationService,
        IWindowService windowService,
        IViewLocator viewLocator)
    {
        _viewLocator = viewLocator;
        _localization = localization;
        _windowService = windowService;
        _navigationService = navigationService;

        var loginViewModel = _navigationService.NavigateTo<LoginViewModel>();
        loginViewModel.IsInfoBarVisible = false;
        CurrentViewModel = loginViewModel;
        
        CurrentView = _viewLocator.CreateView(CurrentViewModel);

        Log.Information("Setting OnNavigateToRegistrationRequested action for LoginViewModel instance: {InstanceId}", loginViewModel.GetHashCode());
        loginViewModel.OnNavigateToRegistrationRequested = () =>
        {
            Log.Information("OnNavigateToRegistrationRequested action invoked! Navigating to RegistrationViewModel...");
            CurrentViewModel = _navigationService.NavigateTo<RegistrationViewModel>();
            CurrentView = _viewLocator.CreateView(CurrentViewModel);
            SelectedNavigationItem = "Registration";
            Log.Information("Successfully navigated to RegistrationViewModel");
        };

        ShowLoginCommand = ReactiveCommand.Create(() =>
        {
            var vm = _navigationService.NavigateTo<LoginViewModel>();
            CurrentViewModel = vm;
            CurrentView = _viewLocator.CreateView(CurrentViewModel);
            SelectedNavigationItem = "Login";

            Log.Information("Setting OnNavigateToRegistrationRequested action for new LoginViewModel instance: {InstanceId}", vm.GetHashCode());
            vm.OnNavigateToRegistrationRequested = () =>
            {
                Log.Information("OnNavigateToRegistrationRequested action invoked from ShowLoginCommand! Navigating to RegistrationViewModel...");
                CurrentViewModel = _navigationService.NavigateTo<RegistrationViewModel>();
                CurrentView = _viewLocator.CreateView(CurrentViewModel);
                SelectedNavigationItem = "Registration";
                Log.Information("Successfully navigated to RegistrationViewModel from ShowLoginCommand");
            };
        });
        ShowRegistrationCommand = ReactiveCommand.Create(() =>
        {
            CurrentViewModel = _navigationService.NavigateTo<RegistrationViewModel>();
            CurrentView = _viewLocator.CreateView(CurrentViewModel);
            SelectedNavigationItem = "Registration";
        });
        ShowStatusCheckCommand = ReactiveCommand.Create(() =>
        {
            CurrentViewModel = _navigationService.NavigateTo<StatusCheckViewModel>();
            CurrentView = _viewLocator.CreateView(CurrentViewModel);
            SelectedNavigationItem = "StatusCheck";
        });

        ReturnBackCommand = ReactiveCommand.CreateFromTask(ShowSettingsWnd);

        SelectedDefaultLanguage = new LanguageOption(localization.CurrentLanguage, localization.CurrentLanguage);

        foreach(var l in localization.AvailableLanguages)
        {
            AvailableLanguages.Add(new LanguageOption(l, l));
        }

        ChangeLanguageCommand = ReactiveCommand.Create(() =>
        {
            localization.SetLanguage(SelectedDefaultLanguage.DisplayName);
        });

    }

    private async Task ShowSettingsWnd()
    {
        _windowService.Hide<AuthWindow>();
         await SettingsViewModel.ShowSettingsWindowAsync();
    }

    public static async Task<bool> ShowAuthWindowAsync()
    {
        Window? wnd;
        var windowService = App.GetService<IWindowService>();
        var tcs = new TaskCompletionSource<bool>();
        var wndService = App.GetService<IWindowService>();

        var viewModel = App.GetService<AuthViewModel>();
        var loginViewModel = App.GetService<LoginViewModel>();
        var authWindow = windowService.Get<AuthWindow>();

        if (authWindow == null)
        {
            wndService.Register("PMS_AUTH", new AuthWindow
            {
                DataContext = App.GetService<AuthViewModel>()
            });

            wnd = wndService.Get<AuthWindow>();

            if (wnd != null)
            {
                wnd.Closed += (_, _) =>
                {
                    tcs.TrySetResult(loginViewModel.IsAuthenticated);
                };
            }
        }

        if (authWindow != null)
        {
            authWindow.Closed += (_, _) =>
            {
                tcs.TrySetResult(loginViewModel.IsAuthenticated);
            };
        }

        windowService.Show<AuthWindow>(viewModel);

        return await tcs.Task;
    }

    public static async Task<bool> TryAutoLoginAsync()
    {
        try
        {
            var rememberMeService = App.GetService<IRememberMeService>();

            if (!rememberMeService.HasSavedCredentials()) return false;

            var credentials = rememberMeService.TryLoadCredentials();

            if (credentials is not { IsValid: true })
            {
                rememberMeService.ClearCredentials();
                return false;
            }

            Log.Information("Attempt to auto-login for user: {CredentialsUsername} ...", credentials.Username);

            var authService = App.GetService<IAuthenticationService>();
            var sessionService = App.GetService<ISessionService>();

            var authResult = await authService.AuthenticateAsync(
                credentials.Username,
                credentials.Password);

            if (!authResult.Success)
            {
                Log.Warning("Auto-login failed: invalid credentials.");
                rememberMeService.ClearCredentials();
                return false;
            }

            var user = await authService.GetCurrentUserByIdAsync(authResult.UserId);
            if (user == null)
            {
                Log.Warning("Auto-login failed: user not found.");
                rememberMeService.ClearCredentials();
                return false;
            }

            sessionService.StartSession(user, authResult.Token);
            await authService.UpdateLastLoginAsync(authResult.UserId);

            Log.Information("Auto-login successful for user: {UserLogin}", user.Login);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Auto-login error");
            return false;
        }
    }



    public ObservableCollection<LanguageOption> AvailableLanguages { get; } = [];

    public LanguageOption SelectedDefaultLanguage
    {
        get => _selectedDefaultLanguage;
        set => SetAndRiseProperty(ref _selectedDefaultLanguage, value);
    }

    public string SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set => SetAndRiseProperty(ref _selectedNavigationItem, value);
    }

    public BaseViewModel CurrentViewModel
    {
        get => _currentViewModel;
        set => SetAndRiseProperty(ref _currentViewModel, value);
    }

    public Control CurrentView
    {
        get => _currentView;
        set => SetAndRiseProperty(ref _currentView, value);
    }


    public ICommand ShowLoginCommand { get; }
    public ICommand ShowRegistrationCommand { get; }
    public ICommand ShowStatusCheckCommand { get; }
    public ICommand ReturnBackCommand { get; }

    public ICommand ChangeLanguageCommand { get; }


}