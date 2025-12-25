using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using PMS.Core.Enums.General;
using PMS.Core.Enums.Window;
using PMS.Core.Models.Common;
using PMS.Core.Services.Interfaces;
using PMS.ViewModels.Tabs.Interfaces;
using PMS.Views.Window;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PMS.ViewModels
{
    public class MainWindowViewModel : PageViewModelBase
    {
        private readonly ISessionService _sessionService;
        private readonly IAuthenticationService _authService;
        private readonly IDialogService _dialogService;
        private readonly IViewLocator _viewLocator;
        private readonly INavigationService _navigationService;
        private readonly ITabService _tabService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IWindowService _windowService;

        private ObservableCollection<TabItemModel> _tabs = [];
        private TabItemModel? _selectedTab;
        private Control? _currentView;
        private object? _currentViewContent;

        private string _userName = string.Empty;
        private string _roleName = string.Empty;
        private string _dateTimeText = string.Empty;
        private bool _isAuthenticated;

        private readonly List<TabTypeOption> _availableTabTypes = new()
        {
            new TabTypeOption(typeof(PatientsViewModel), "Пацієнти", "People"),
            new TabTypeOption(typeof(DoctorsViewModel), "Лікарі", "Contact2"),
            new TabTypeOption(typeof(AppointmentFormViewModel), "Запис на прийом", "Calendar"),
            new TabTypeOption(typeof(ScheduleViewModel), "Розклад", "CalendarWeek"),
            new TabTypeOption(typeof(HomeVisitFormViewModel), "Виклик додому", "Vehicle"),
            new TabTypeOption(typeof(CertificateFormViewModel), "Довідки", "Document"),
            new TabTypeOption(typeof(AggregationsViewModel), "Агрегації", "Code"),
            new TabTypeOption(typeof(UserManagementViewModel), "Користувачі", "Admin"),
            new TabTypeOption(typeof(RoomViewModel), "Кабінети", "Building"),
            new TabTypeOption(typeof(ProcedureViewModel), "Процедури", "Medical"),
            new TabTypeOption(typeof(ProcedureViewModel), "Процедури пацієнтів", "Medical")
        };

        

        private readonly CompositeDisposable _disposables = new();

        private string _activeKeyCombination = "Очікую комбінації...";

        public string ActiveKeyCombination
        {
            get => _activeKeyCombination;
            set => SetAndRiseProperty(ref _activeKeyCombination, value);
        }

        private string _memoryUsage = "";

        public string MemoryUsage
        {
            get => _memoryUsage;
            set => SetAndRiseProperty(ref _memoryUsage, value);
        }

        private void UpdateMemoryUsage()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var memoryMb = process.PrivateMemorySize64 / (1024 * 1024);
                MemoryUsage = $"Пам'ять (pm64): {memoryMb} MB";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating memory usage: {ex.Message}");
            }
        }

        public void UpdateKeyCombination(string combination)
        {
            ActiveKeyCombination = combination;
            Task.Delay(2500).ContinueWith(_ =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    ActiveKeyCombination = "Очікування комбінацій...";
                });
            });
        }

        public MainWindowViewModel(
            INavigationService navigationService,
            ISessionService sessionService,
            IAuthenticationService authService,
            IDialogService dialogService,
            IViewLocator viewLocator,
            ITabService tabService,
            IServiceProvider serviceProvider,
            IWindowService windowService)
        {
            _navigationService = navigationService;
            _sessionService = sessionService;
            _authService = authService;
            _dialogService = dialogService;
            _viewLocator = viewLocator;
            _tabService = tabService;
            _serviceProvider = serviceProvider;
            _windowService = windowService;

            Mode = WindowMode.Standalone;
            
            InitializeCommands();
            InitializeTabService();
            InitializeNavigation();
            InitializeSession();

            Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(1))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => UpdateDateTime())
                .DisposeWith(_disposables);

            Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(1))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => UpdateMemoryUsage())
                .DisposeWith(_disposables);

            _ = InitializeDefaultViewAsync();
        }

        #region Properties

        public ObservableCollection<TabItemModel> Tabs
        {
            get => _tabs;
            set => SetAndRiseProperty(ref _tabs, value);
        }

        public TabItemModel? SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (SetAndRiseProperty(ref _selectedTab, value))
                {
                    UpdateCurrentViewContent();
                }
            }
        }

        public object? CurrentViewContent
        {
            get => _currentViewContent;
            set => SetAndRiseProperty(ref _currentViewContent, value);
        }

        public Control? CurrentView
        {
            get => _currentView;
            set => SetAndRiseProperty(ref _currentView, value);
        }

        public string UserName
        {
            get => _userName;
            set => SetAndRiseProperty(ref _userName, value);
        }

        public string RoleName
        {
            get => _roleName;
            set => SetAndRiseProperty(ref _roleName, value);
        }

        public string DateTimeText
        {
            get => _dateTimeText;
            set => SetAndRiseProperty(ref _dateTimeText, value);
        }

        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set => SetAndRiseProperty(ref _isAuthenticated, value);
        }

        public bool IsAdminMenuVisible => _sessionService.UserRole == UserRole.Administrator;

        #endregion

        #region Commands - Tab Management

        public ReactiveCommand<Unit, Unit> AddTabCommand { get; private set; }
        public ReactiveCommand<TabItemModel, Unit> CloseTabCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> CloseAllTabsCommand { get; private set; }
        public ReactiveCommand<TabItemModel, Unit> CloseOtherTabsCommand { get; private set; }
        public ReactiveCommand<TabItemModel, Unit> DuplicateTabCommand { get; private set; }

        public Func<Task<bool>>? OnLogoutRequested { get; set; }

        #endregion

        #region Commands - Navigation

        public ICommand ExitCommand { get; private set; }
        public ICommand EndSessionCommand { get; private set; }
        public ICommand RestartAppCommand { get; private set; }
        public ICommand ShowDoctorsCommand { get; private set; }
        public ICommand ShowPatientsCommand { get; private set; }
        public ICommand ShowRoomsCommand { get; private set; }
        
        public ICommand ShowExaminationsCommand { get; private set; }
        public ICommand ShowProceduresCommand { get; private set; }
        public ICommand ShowPatientProceduresCommand { get; private set; }
        public ICommand ShowAppointmentCommand { get; private set; }
        public ICommand ShowHomeVisitCommand { get; private set; }
        public ICommand ShowCertificatesCommand { get; private set; }
        public ICommand ShowDoctorScheduleCommand { get; private set; }
        public ICommand ShowRoomScheduleCommand { get; private set; }
        public ICommand ShowReceptionStatsCommand { get; private set; }
        public ICommand ShowDiseaseAnalysisCommand { get; private set; }
        public ICommand ShowAggregationsCommand { get; private set; }
        public ICommand ShowUsersCommand { get; private set; }
        public ICommand ShowAccessRightsCommand { get; private set; }
        public ICommand QuickAppointmentCommand { get; private set; }
        public ICommand QuickPatientSearchCommand { get; private set; }
        public ICommand QuickDoctorScheduleCommand { get; private set; }
        public ICommand QuickCertificateCommand { get; private set; }
        public ICommand ShowAboutCommand { get; private set; }
        public ICommand ShowHomeCommand { get; private set; }
        public ICommand ShowLanguagesCommand { get; private set; }
        public ICommand ShowSettingsCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            AddTabCommand = ReactiveCommand.CreateFromTask(AddNewTabAsync);
            CloseTabCommand = ReactiveCommand.CreateFromTask<TabItemModel>(CloseTabAsync);
            CloseAllTabsCommand = ReactiveCommand.CreateFromTask(CloseAllTabsAsync);
            CloseOtherTabsCommand = ReactiveCommand.CreateFromTask<TabItemModel>(CloseOtherTabsAsync);
            DuplicateTabCommand = ReactiveCommand.CreateFromTask<TabItemModel>(DuplicateTabAsync);

            ExitCommand = ReactiveCommand.Create(Exit);
            EndSessionCommand = ReactiveCommand.Create(EndSession);
            RestartAppCommand = ReactiveCommand.Create(RestartApp);

            ShowDoctorsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await OpenInTabAsync<DoctorsViewModel>("Лікарі");
                _navigationService.NavigateTo<DoctorsViewModel>();
            });

            ShowPatientsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await OpenInTabAsync<PatientsViewModel>("Пацієнти");
                _navigationService.NavigateTo<PatientsViewModel>();
            });

            ShowAppointmentCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await OpenInTabAsync<AppointmentFormViewModel>("Прийом");
            });

            ShowHomeVisitCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await OpenInTabAsync<HomeVisitFormViewModel>("Виклик додому");
                _navigationService.NavigateTo<HomeVisitFormViewModel>();
            });

            ShowCertificatesCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await OpenInTabAsync<CertificateFormViewModel>("Довідки");
                _navigationService.NavigateTo<CertificateFormViewModel>();
            });

            ShowDoctorScheduleCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await OpenInTabAsync<ScheduleViewModel>("Розклад лікарів");
                _navigationService.NavigateTo<ScheduleViewModel>(new { ViewType = "doctor" });
            });

            ShowRoomScheduleCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await OpenInTabAsync<ScheduleViewModel>("Розклад кабінетів");
                _navigationService.NavigateTo<ScheduleViewModel>(new { ViewType = "room" });
            });

            ShowReceptionStatsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await OpenInTabAsync<AggregationsViewModel>("Статистика прийомів");
                _navigationService.NavigateTo<AggregationsViewModel>(new { QueryType = "reception_stats" });
            });

            ShowDiseaseAnalysisCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await OpenInTabAsync<AggregationsViewModel>("Аналіз захворювань");
                _navigationService.NavigateTo<AggregationsViewModel>(new { QueryType = "disease_analysis" });
            });

            ShowAggregationsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await OpenInTabAsync<AggregationsViewModel>("Агрегації MongoDB");
                _navigationService.NavigateTo<AggregationsViewModel>();
            });

            ShowUsersCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await OpenInTabAsync<UserManagementViewModel>("Користувачі");
                _navigationService.NavigateTo<UserManagementViewModel>();
            });

            ShowAccessRightsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await OpenInTabAsync<UserManagementViewModel>("Права доступу");
                _navigationService.NavigateTo<UserManagementViewModel>(new { Tab = "access_rights" });
            });

            QuickAppointmentCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                _navigationService.NavigateTo<AppointmentFormViewModel>();
                await Task.CompletedTask;
            });

            QuickPatientSearchCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                _navigationService.NavigateTo<PatientsViewModel>(new { AutoFocusSearch = true });
                await Task.CompletedTask;
            });

            QuickDoctorScheduleCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                _navigationService.NavigateTo<ScheduleViewModel>(new { ViewType = "doctor" });
                await Task.CompletedTask;
            });

            QuickCertificateCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                _navigationService.NavigateTo<CertificateFormViewModel>();
                await Task.CompletedTask;
            });

            ShowHomeCommand = ReactiveCommand.Create(async () =>
            {
                _navigationService.NavigateTo<HomeViewModel>();
                await Task.Delay(300);
            });

            ShowRoomsCommand = ReactiveCommand.Create(async () =>
            {
                await OpenInTabAsync<RoomViewModel>("Кімнати");
                _navigationService.NavigateTo<RoomViewModel>();
            });

            ShowExaminationsCommand = ReactiveCommand.Create(async () =>
            {
                await OpenInTabAsync<ExaminationsViewModel>("Обстеження");
            });

            ShowAboutCommand = ReactiveCommand.CreateFromTask(ShowAboutAsync);

            ShowSettingsCommand = ReactiveCommand.CreateFromTask(ShowSettingsWindow);

            ShowProceduresCommand = ReactiveCommand.Create(async () =>
            {
                await OpenInTabAsync<ProcedureViewModel>("Процедури");
                _navigationService.NavigateTo<ProcedureViewModel>();
            });

            ShowPatientProceduresCommand = ReactiveCommand.Create(async () =>
            {
                await OpenInTabAsync<PatientProceduresViewModel>("Процедури пацієнтів");
            });

            CollapseInfoBarCommand = ReactiveCommand.Create(() =>
            {
                if (IsInfoBarVisible)
                {
                    IsInfoBarVisible = !IsInfoBarVisible;
                    return;
                }
                
                IsInfoBarVisible = true;
            });
        }

        private void InitializeTabService()
        {
            _tabs = new ObservableCollection<TabItemModel>(_tabService.Tabs);

            _tabService.TabOpened += OnTabOpened;
            _tabService.TabClosed += OnTabClosed;
            _tabService.TabSelected += OnTabSelected;
        }

        private void InitializeNavigation()
        {
            _navigationService.CurrentViewChanged += OnNavigationChanged;
        }

        private void InitializeSession()
        {
            _sessionService.SessionChanged += OnSessionChanged;

            UpdateSessionInfo();
            UpdateDateTime();
        }

        public async Task InitializeDefaultViewAsync()
        {
            if (!_tabs.Any())
            {
                _navigationService.NavigateTo<HomeViewModel>();
                UpdateCurrentViewFromNavigation();
            }

            await Task.CompletedTask;
        }

        private async Task ShowSettingsWindow()
        {

            var settingsVm = App.GetService<SettingsViewModel>();

            settingsVm.Mode = WindowMode.Dialog;
            settingsVm.CanContinue = true;

            var wnd = new SettingsWindow
            {
                DataContext = settingsVm
            };
            _windowService.Register("PMS_INTERNAL_SETTINGS_WND", wnd);

            settingsVm.ShowInfoBar("Щоб окремі налаштування вступили в силу необхіно перезавантажити застосунок.", "Примітка");

            if (_windowService.MainWindow != null)
            {
                await wnd.ShowDialog(_windowService.MainWindow);
            }
           

            await Task.CompletedTask;
        }

        #endregion

        public static async Task ShowMainWindow()
        {

            Window? wnd;
            var wndService = App.GetService<IWindowService>();
            var tcs = new TaskCompletionSource<bool>();

            var viewModel = App.GetService<MainWindowViewModel>();
            var mainWindow = wndService.Get<MainWindow>();

            if (mainWindow == null)
            {
                wndService.Register("PMS_MAIN", new MainWindow
                {
                    DataContext = viewModel
                });

                wnd = wndService.Get<MainWindow>();


                if (wnd != null)
                {
                    wnd.Closed += (_, _) =>
                    {
                        tcs.TrySetResult(true);
                    };

                    wndService.SetMainWindow(wnd);

                }
            }

            if (mainWindow != null)
            {
                mainWindow.Closed += (_, _) =>
                {

                    tcs.TrySetResult(true);
                };

                wndService.SetMainWindow(mainWindow);
            }


            await tcs.Task;
        }

        #region Tab Management

        private async Task<TabItemModel?> OpenInTabAsync<TViewModel>(string header) where TViewModel : BaseViewModel
        {
            try
            {
                var existingTab = _tabs.FirstOrDefault(t => t.Content?.GetType() == typeof(TViewModel));
                if (existingTab != null)
                {
                    _tabService.SelectTab(existingTab);
                    return existingTab;
                }

                var viewModel = _serviceProvider.GetRequiredService<TViewModel>();

                if (viewModel is IInitializableViewModel initializable)
                {
                    await initializable.InitializeAsync(null);
                }

                var icon = GetIconForViewModel(typeof(TViewModel));

                var tab = await _tabService.OpenTabAsync(viewModel, header, icon);

                return tab;
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("Помилка", $"Не вдалося відкрити вкладку.", ex);
                return null;
            }
        }

        private async Task AddNewTabAsync()
        {
            var selectedTypes = await _dialogService.ShowMultiSelectionDialogAsync(
                "Відкрити вкладки",
                "Виберіть типи вкладок для відкриття:",
                _availableTabTypes,
                option => option.DisplayName
            );

            if (selectedTypes == null || selectedTypes.Length == 0)
            {
                return;

            }
            
            foreach (var tabOption in selectedTypes)
            {
                await _tabService.OpenTabAsync(tabOption.ViewModelType, tabOption.DisplayName, tabOption.Icon);
            }
        }

        private async Task CloseTabAsync(TabItemModel? tab)
        {
            if (tab == null) return;

            if (tab.HasChanges)
            {
                var result = await _dialogService.ShowConfirmAsync(
                    "Незбережені зміни",
                    $"Вкладка '{tab.Header}' має незбережені зміни. Закрити без збереження?");

                if (!result) return;
            }

            await DisposeTabResourcesAsync(tab);
            await _tabService.CloseTabAsync(tab);

            if (!_tabs.Any())
            {
                await InitializeDefaultViewAsync();
            }
        }

        private async Task CloseAllTabsAsync()
        {
            var tabsWithChanges = _tabs.Where(t => t.HasChanges).ToList();
            if (tabsWithChanges.Any())
            {
                var result = await _dialogService.ShowConfirmAsync(
                    "Незбережені зміни",
                    $"{tabsWithChanges.Count} вкладок мають незбережені зміни. Закрити без збереження?");

                if (!result) return;
            }

            var tabsToClose = _tabs.ToList();
            foreach (var tab in tabsToClose)
            {
                await DisposeTabResourcesAsync(tab);
            }

            await _tabService.CloseAllTabsAsync();
            await InitializeDefaultViewAsync();
        }

        private async Task CloseOtherTabsAsync(TabItemModel? tab)
        {
            if (tab == null) return;

            var otherTabs = _tabs.Where(t => t != tab).ToList();
            var tabsWithChanges = otherTabs.Where(t => t.HasChanges).ToList();

            if (tabsWithChanges.Any())
            {
                var result = await _dialogService.ShowConfirmAsync(
                    "Незбережені зміни",
                    $"{tabsWithChanges.Count} вкладок мають незбережені зміни. Закрити без збереження?");

                if (!result) return;
            }

            foreach (var otherTab in otherTabs)
            {
                await DisposeTabResourcesAsync(otherTab);
            }

            await _tabService.CloseOtherTabsAsync(tab);
        }

        private async Task DisposeTabResourcesAsync(TabItemModel tab)
        {
            try
            {
                if (tab.Content is IDisposable disposableViewModel)
                {
                    disposableViewModel.Dispose();
                }

                if (tab.Content is BaseViewModel baseViewModel)
                {
                    baseViewModel.CleanUp();
                }

                tab.Content = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disposing tab resources: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        private async Task DuplicateTabAsync(TabItemModel? tab)
        {
            if (tab?.Content == null) return;

            var viewModelType = tab.Content.GetType();

            if (_serviceProvider.GetRequiredService(viewModelType) is BaseViewModel newViewModel)
            {
                var baseHeader = GetBaseHeaderName(tab.DisplayHeader);

                var existingTabCount = Tabs.Count(t => t.Content?.GetType() == viewModelType);

                await _tabService.OpenTabAsync(newViewModel, $"{baseHeader}:{existingTabCount + 1}", tab.IconSource);
            }
        }

        private string GetBaseHeaderName(string displayHeader)
        {
            var colonIndex = displayHeader.LastIndexOf(':');
            if (colonIndex > 0 && int.TryParse(displayHeader.Substring(colonIndex + 1), out _))
            {
                return displayHeader.Substring(0, colonIndex);
            }
            return displayHeader;
        }

        private void UpdateCurrentViewContent()
        {
            if (CurrentView is IDisposable disposableCurrentView)
            {
                disposableCurrentView.Dispose();
            }

            if (CurrentView is { } currentControl)
            {
                if (currentControl.Parent is ContentPresenter presenter)
                {
                    presenter.Content = null;
                }
                currentControl.DataContext = null;
            }

            if (_selectedTab?.Content != null)
            {
                CurrentViewContent = _selectedTab.Content;

                if (_selectedTab.Content is BaseViewModel vm)
                {
                    var view = _viewLocator.CreateView(vm);
                    view.DataContext = vm;
                    CurrentView = view;
                }
            }
            else if (_navigationService.CurrentView != null)
            {
                UpdateCurrentViewFromNavigation();
            }
            else
            {
                CurrentViewContent = null;
                CurrentView = null;
            }
        }

        private void UpdateCurrentViewFromNavigation()
        {
            if (CurrentView is IDisposable disposableView)
            {
                disposableView.Dispose();
            }

            if (CurrentView is { } control)
            {
                if (control.Parent is ContentPresenter presenter)
                {
                    presenter.Content = null;
                }
                control.DataContext = null;
            }

            var viewModel = _navigationService.CurrentView;
            CurrentViewContent = viewModel;

            var view = _viewLocator.CreateView(viewModel);
            view.DataContext = viewModel;
            CurrentView = view;
        }

        private string GetIconForViewModel(Type viewModelType)
        {
            return viewModelType.Name switch
            {
                nameof(PatientsViewModel) => "People",
                nameof(DoctorsViewModel) => "Contact2",
                nameof(AppointmentFormViewModel) => "Calendar",
                nameof(ScheduleViewModel) => "CalendarWeek",
                nameof(UserManagementViewModel) => "Admin",
                nameof(HomeVisitFormViewModel) => "Vehicle",
                nameof(CertificateFormViewModel) => "Document",
                nameof(AggregationsViewModel) => "Database",
                _ => "Document"
            };
        }

        #endregion

        #region Event Handlers

        private void OnTabOpened(object? sender, TabEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!_tabs.Contains(e.Tab))
                {
                    _tabs.Add(e.Tab);
                }

                SelectedTab = e.Tab;
            }, DispatcherPriority.Loaded);
        }

        private void OnTabClosed(object? sender, TabEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _tabs.Remove(e.Tab);
            });
        }

        private void OnTabSelected(object? sender, TabEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                SelectedTab = e.Tab;
            });
        }

        private void OnNavigationChanged(object? sender, BaseViewModel viewModel)
        {
            if (!_tabs.Any())
            {
                UpdateCurrentViewFromNavigation();
            }
        }

        private void OnSessionChanged(object? sender, SessionEventArgs e)
        {
            UpdateSessionInfo();
        }

        #endregion

        #region Helper Methods

        private async void Exit()
        {
            string message = HasUnsavedChanges
                ? "Є незбережені зміни.\nВи впевнені, що хочете вийти?"
                : "Ви впевнені, що хочете вийти?";

            var result = await _dialogService.ShowConfirmAsync("Вихід", message);
            if (result)
            {
                _sessionService.EndSession();
                App.Exit();
            }
        }

        private async void RestartApp()
        {
            string message = HasUnsavedChanges
                ? "Є незбережені зміни.\nВи впевнені, що хочете перезапустити додаток?"
                : "Ви впевнені, що хочете перезапустити додаток?";

            var result = await _dialogService.ShowConfirmAsync("Перезапуск", message);
            if (result)
            {
                App.Restart(250);
            }
        }

        private async void EndSession()
        {
            var result = await _dialogService.ShowConfirmAsync(
                "Вихід", "Ви дійсно хочете завершити поточний сеанс?");

            if (result)
            {
                _sessionService.EndSession();
                await CloseAllTabsAsync();

                var mWnd = _windowService.Get<MainWindow>();
                mWnd?.Hide();

                var loginSuccess = await AuthViewModel.ShowAuthWindowAsync();

                mWnd?.ForceClose();
                
                if (loginSuccess)
                {
                    await ShowMainWindow();
                }
                else
                {
                    App.Exit();
                }
            }
        }

        private void UpdateSessionInfo()
        {
            IsAuthenticated = _sessionService.IsAuthenticated;

            if (IsAuthenticated && _sessionService.CurrentUser != null)
            {
                UserName = _sessionService.CurrentUser.FullName;
            }
            else
            {
                UserName = "Не автентифікований";
            }

            RoleName = GetRoleDisplayName(_sessionService.UserRole);
        }

        private void UpdateDateTime()
        {
            DateTimeText = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
        }

        private string GetRoleDisplayName(UserRole role)
        {
            return role switch
            {
                UserRole.Administrator => "Адміністратор",
                UserRole.Operator => "Оператор",
                UserRole.Authorized => "Авторизований",
                UserRole.Guest => "Гість",
                _ => "Невідома роль"
            };
        }

        private async Task ShowAboutAsync()
        {
            await _dialogService.ShowAboutDialogAsync();
        }

        #endregion

        #region Cleanup

        public override void CleanUp()
        {
            try
            {
                var tabsToDispose = _tabs.ToList();
                foreach (var tab in tabsToDispose)
                {
                    _ = DisposeTabResourcesAsync(tab);
                }
                _tabs.Clear();

                _navigationService.Clear();

                CurrentView = null;
                CurrentViewContent = null;
                SelectedTab = null;

                _disposables?.Dispose();

                _navigationService.CurrentViewChanged -= OnNavigationChanged;
                _sessionService.SessionChanged -= OnSessionChanged;
                _tabService.TabOpened -= OnTabOpened;
                _tabService.TabClosed -= OnTabClosed;
                _tabService.TabSelected -= OnTabSelected;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CleanUp: {ex.Message}");
            }
            finally
            {
                base.CleanUp();
            }
        }

        #endregion
    }
}