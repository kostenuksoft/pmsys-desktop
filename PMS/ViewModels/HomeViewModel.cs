using ReactiveUI;
using System;
using System.Windows.Input;
using TechTaskView = PMS.Views.Window.TechTaskView;
using UserRoleE = PMS.Core.Enums.General.UserRole;
using PMS.Core.Services.Interfaces;

namespace PMS.ViewModels;

public class HomeViewModel : PageViewModelBase
{
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;
    private readonly ITabService _tabService;
    private readonly IWindowService _windowService;
    private readonly IDialogService _dialogService;

    private string _greeting = string.Empty;
    private string _userName = string.Empty;
    private string _userRole = string.Empty;
    private string _lastLoginDate = string.Empty;
    private string _sessionDuration = string.Empty;
    private bool _canViewData;
    private bool _canEditData;
    private bool _canDeleteData;
    private bool _canRunAggregations;
    private bool _canManageUsers;
    private bool _canIssueСertificates;

    public HomeViewModel(
        ISessionService sessionService,
        INavigationService navigationService, 
        ITabService tabService, 
        IWindowService windowService, 
        IDialogService dialogService)
    {
        _sessionService = sessionService;
        _navigationService = navigationService;
        _tabService = tabService;
        _windowService = windowService;
        _dialogService = dialogService;

        NavigateToDoctorsCommand = ReactiveCommand.Create(() =>
            _navigationService.NavigateTo<DoctorsViewModel>());

        NavigateToPatientsCommand = ReactiveCommand.Create(() =>
            _navigationService.NavigateTo<PatientsViewModel>()
        );

        NavigateToAppointmentsCommand = ReactiveCommand.Create(() =>
            _navigationService.NavigateTo<AppointmentFormViewModel>());

        NavigateToHomeVisitsCommand = ReactiveCommand.Create(() =>
            _navigationService.NavigateTo<HomeVisitFormViewModel>());

        NavigateToReportsCommand = ReactiveCommand.Create(() =>
            _navigationService.NavigateTo<AggregationsViewModel>());

        NavigateToUsersCommand = ReactiveCommand.Create(() =>
            _navigationService.NavigateTo<UserManagementViewModel>());

        RefreshCommand = ReactiveCommand.Create(UpdateUserInfo);

        BringTechTaskWindow = ReactiveCommand.Create(ShowTaskWnd);

        UpdateUserInfo();
        UpdateGreeting();
    }


    public string Greeting
    {
        get => _greeting;
        set => SetAndRiseProperty(ref _greeting, value);
    }

    public string UserName
    {
        get => _userName;
        set => SetAndRiseProperty(ref _userName, value);
    }

    public string UserRole
    {
        get => _userRole;
        set => SetAndRiseProperty(ref _userRole, value);
    }

    public string LastLoginDate
    {
        get => _lastLoginDate;
        set => SetAndRiseProperty(ref _lastLoginDate, value);
    }

    public string SessionDuration
    {
        get => _sessionDuration;
        set => SetAndRiseProperty(ref _sessionDuration, value);
    }

    public bool CanViewData
    {
        get => _canViewData;
        set => SetAndRiseProperty(ref _canViewData, value);
    }

    public bool CanEditData
    {
        get => _canEditData;
        set => SetAndRiseProperty(ref _canEditData, value);
    }

    public bool CanDeleteData
    {
        get => _canDeleteData;
        set => SetAndRiseProperty(ref _canDeleteData, value);
    }

    public bool CanRunAggregations
    {
        get => _canRunAggregations;
        set => SetAndRiseProperty(ref _canRunAggregations, value);
    }

    public bool CanManageUsers
    {
        get => _canManageUsers;
        set => SetAndRiseProperty(ref _canManageUsers, value);
    }

    public bool CanIssueСertificates
    {
        get => _canIssueСertificates;
        set => SetAndRiseProperty(ref _canIssueСertificates, value);
    }

    public bool ShowDoctorsCard => CanViewData;
    public bool ShowPatientsCard => CanViewData;
    public bool ShowAppointmentsCard => CanEditData;
    public bool ShowHomeVisitsCard => CanEditData;
    public bool ShowReportsCard => CanRunAggregations;
    public bool ShowUsersCard => CanManageUsers;

    public ICommand NavigateToDoctorsCommand { get; }
    public ICommand NavigateToPatientsCommand { get; }
    public ICommand NavigateToAppointmentsCommand { get; }
    public ICommand NavigateToHomeVisitsCommand { get; }
    public ICommand NavigateToReportsCommand { get; }
    public ICommand NavigateToUsersCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand BringTechTaskWindow { get; }

    private void UpdateUserInfo()
    {
        var user = _sessionService.CurrentUser;
        if (user != null)
        {
            UserName = user.FullName;
            UserRole = GetRoleDisplayName(user.Role);
            LastLoginDate = user.LastLogin?.ToString("dd.MM.yyyy HH:mm:ss") ?? "Перший вхід";

            var sessionTime = DateTime.UtcNow - _sessionService.LoginTime;
            SessionDuration = $"{(int)sessionTime.TotalHours} год. {sessionTime.Minutes} хв. {sessionTime.Seconds} сек.";
            CanViewData = _sessionService.HasPermission("view_data");
            CanEditData = _sessionService.HasPermission("edit_data");
            CanDeleteData = _sessionService.HasPermission("delete_data");
            CanRunAggregations = _sessionService.HasPermission("run_aggregations");
            CanManageUsers = _sessionService.HasPermission("manage_users");
            CanIssueСertificates = _sessionService.HasPermission("issue_certificates");

        }
    }

    private void ShowTaskWnd()
    {
        _windowService.Show<TechTaskView>();
    }

    private void UpdateGreeting()
    {
        var hour = DateTime.Now.Hour;
        var timeGreeting = hour switch
        {
            < 6 => "Доброї ночі",
            < 12 => "Доброго ранку",
            < 18 => "Добрий день",
            _ => "Добрий вечір"
        };

        var user = _sessionService.CurrentUser;
        if (user != null)
        {
            Greeting = $"{timeGreeting}, {user.FullName.Split(' ')[1] + " " + user.FullName.Split(' ')[2]}!";
        }
    }


    private string GetRoleDisplayName(UserRoleE role)
    {
        return role switch
        {
            UserRoleE.Administrator => "Адміністратор системи",
            UserRoleE.Operator => "Оператор",
            UserRoleE.Authorized => "Авторизований користувач",
            UserRoleE.Guest => "Гість",
            _ => "Невідома роль"
        };
    }

    public override void Initialize()
    {
        base.Initialize();
        UpdateUserInfo();
        UpdateGreeting();
    }
}