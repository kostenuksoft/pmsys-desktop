using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData;
using PMS.Core.Enums.General;
using PMS.Core.Models;
using PMS.Core.Models.Common;
using PMS.Core.Repositories.Interfaces;
using PMS.Core.Services.Interfaces;
using ReactiveUI;
using Serilog;
using Avalonia.Controls.Notifications;
using PMS.Core.Services;

namespace PMS.ViewModels
{
 
    public class UserManagementViewModel : PageViewModelBase, IParameterizedViewModel
    {
        #region Fields

        private readonly IUserRepository _userRepository;
        private readonly IKeysRepository _keysRepository;
        private readonly IGuestRequestRepository _guestRequestRepository;
        private readonly ISecurityService _securityService;
        private readonly ISessionService _sessionService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly ILogger _logger;

        private readonly SourceCache<User, string> _usersCache = new(x => x.Id);
        private readonly SourceCache<GuestRequest, string> _requestsCache = new(x => x.Id);
        private readonly ReadOnlyObservableCollection<User> _users;
        private readonly ReadOnlyObservableCollection<GuestRequest> _guestRequests;

        private string _userSearchText = string.Empty;
        private UserRole? _roleFilter;
        private bool? _statusFilter;

        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _pageSize;
        private long _totalUsers;
        private long _activeUsers;

        private User? _selectedUser;
        private GuestRequest? _selectedRequest;

        private int _guestRequestsCount;
        private bool _hasNewRequests;

        private bool _isUserDetailsVisible;
        private bool _showPassword;

        private string _editLogin = string.Empty;
        private string _editPassword = string.Empty;
        private string _editFullName = string.Empty;
        private UserRole _editRole = UserRole.Guest;
        private bool _editIsActive = true;
        private AccessRights _editAccessRights = new();

        #endregion

        #region Constructor

        public UserManagementViewModel(
            IUserRepository userRepository,
            IKeysRepository keysRepository,
            IGuestRequestRepository guestRequestRepository,
            ISecurityService securityService,
            ISessionService sessionService,
            INavigationService navigationService,
            IDialogService dialogService,
            ILogger logger)
        {
            _userRepository = userRepository;
            _keysRepository = keysRepository;
            _guestRequestRepository = guestRequestRepository;
            _securityService = securityService;
            _sessionService = sessionService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            _logger = logger;

            _pageSize = App.ApplicationSettings.PageSize;

            var userFilter = this.WhenAnyValue(
                    x => x.UserSearchText,
                    x => x.RoleFilter,
                    x => x.StatusFilter)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .Select(_ => CreateUserPredicate());

            _usersCache.Connect()
                .Filter(userFilter)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _users)
                .Subscribe();

            _requestsCache.Connect()
                .Filter(r => r.Status == RequestStatus.Pending)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _guestRequests)
                .Subscribe();

            _requestsCache.Connect()
                .Filter(r => r.Status == RequestStatus.Pending)
                .Subscribe(_ =>
                {
                    GuestRequestsCount = _requestsCache.Items.Count(r => r.Status == RequestStatus.Pending);
                    HasNewRequests = GuestRequestsCount > 0;
                });

            InitializeCommands();

            this.WhenAnyValue(x => x.SelectedUser)
                .Subscribe(LoadUserDetails);

            if (HasNewRequests)
            {
                _dialogService.ShowNotification(
                    "Нові заявки",
                    $"У вас {GuestRequestsCount} нових заявок від гостей",
                    NotificationPosition.BottomRight);
            }
        }

        private void InitializeCommands()
        {
            AddUserCommand = ReactiveCommand.CreateFromTask(AddUserAsync);
            EditUserCommand = ReactiveCommand.CreateFromTask<User>(EditUserAsync);
            DeleteUserCommand = ReactiveCommand.CreateFromTask<User>(DeleteUserAsync);
            RefreshCommand = ReactiveCommand.CreateFromTask(LoadCurrentPageAsync);

            ViewRequestsCommand = ReactiveCommand.CreateFromTask(ViewGuestRequestsAsync);
            ApproveRequestCommand = ReactiveCommand.CreateFromTask<GuestRequest>(ApproveRequestAsync);
            RejectRequestCommand = ReactiveCommand.CreateFromTask<GuestRequest>(req => RejectRequestAsync(req, null));

            LockAccountCommand = ReactiveCommand.CreateFromTask<User>(LockAccountAsync);
            UnlockAccountCommand = ReactiveCommand.CreateFromTask<User>(UnlockAccountAsync);
            ResetPasswordCommand = ReactiveCommand.CreateFromTask<User>(ResetPasswordAsync);

            PrevPageCommand = ReactiveCommand.CreateFromTask(
                LoadPreviousPageAsync,
                this.WhenAnyValue(x => x.CurrentPage, page => page > 1));

            NextPageCommand = ReactiveCommand.CreateFromTask(
                LoadNextPageAsync,
                this.WhenAnyValue(
                    x => x.CurrentPage,
                    x => x.TotalPages,
                    (current, total) => current < total));

            ClearFiltersCommand = ReactiveCommand.Create(ClearFilters);
        }

        #endregion

        #region Properties

        public override string TabHeader => "Користувачі";
        public override string? TabIconSource => "Admin";

        public ReadOnlyObservableCollection<User> Users => _users;
        public ReadOnlyObservableCollection<GuestRequest> GuestRequests => _guestRequests;

        public string UserSearchText
        {
            get => _userSearchText;
            set => SetAndRiseProperty(ref _userSearchText, value);
        }

        public UserRole? RoleFilter
        {
            get => _roleFilter;
            set => SetAndRiseProperty(ref _roleFilter, value);
        }

        public bool? StatusFilter
        {
            get => _statusFilter;
            set => SetAndRiseProperty(ref _statusFilter, value);
        }

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetAndRiseProperty(ref _currentPage, value))
                {
                    _ = LoadCurrentPageAsync();
                }
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set => SetAndRiseProperty(ref _totalPages, value);
        }

        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (SetAndRiseProperty(ref _pageSize, value))
                {
                    CurrentPage = 1;
                }
            }
        }

        public long TotalUsers
        {
            get => _totalUsers;
            set => SetAndRiseProperty(ref _totalUsers, value);
        }

        public long ActiveUsers
        {
            get => _activeUsers;
            set => SetAndRiseProperty(ref _activeUsers, value);
        }

        public string PaginationText => $"{(CurrentPage - 1) * PageSize + 1}-{Math.Min(CurrentPage * PageSize, TotalUsers)} з {TotalUsers}";

        public User? SelectedUser
        {
            get => _selectedUser;
            set => SetAndRiseProperty(ref _selectedUser, value);
        }


        public int GuestRequestsCount
        {
            get => _guestRequestsCount;
            set => SetAndRiseProperty(ref _guestRequestsCount, value);
        }

        public bool HasNewRequests
        {
            get => _hasNewRequests;
            set => SetAndRiseProperty(ref _hasNewRequests, value);
        }

        public bool IsUserDetailsVisible
        {
            get => _isUserDetailsVisible;
            set => SetAndRiseProperty(ref _isUserDetailsVisible, value);
        }

        public string EditLogin
        {
            get => _editLogin;
            set => SetAndRiseProperty(ref _editLogin, value);
        }

        public string EditPassword
        {
            get => _editPassword;
            set => SetAndRiseProperty(ref _editPassword, value);
        }

        public string EditFullName
        {
            get => _editFullName;
            set => SetAndRiseProperty(ref _editFullName, value);
        }

        public UserRole EditRole
        {
            get => _editRole;
            set => SetAndRiseProperty(ref _editRole, value);
        }

        public bool EditIsActive
        {
            get => _editIsActive;
            set => SetAndRiseProperty(ref _editIsActive, value);
        }

        public AccessRights EditAccessRights
        {
            get => _editAccessRights;
            set => SetAndRiseProperty(ref _editAccessRights, value);
        }

        #endregion

        #region Commands

        public ICommand AddUserCommand { get; private set; }
        public ICommand EditUserCommand { get; private set; }
        public ICommand DeleteUserCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand ViewRequestsCommand { get; private set; }
        public ICommand ApproveRequestCommand { get; private set; }
        public ICommand RejectRequestCommand { get; private set; }
        public ICommand LockAccountCommand { get; private set; }
        public ICommand UnlockAccountCommand { get; private set; }
        public ICommand ResetPasswordCommand { get; private set; }
        public ICommand PrevPageCommand { get; private set; }
        public ICommand NextPageCommand { get; private set; }
        public ICommand ClearFiltersCommand { get; private set; }

        #endregion

        #region Initialization

    
        public override async void Initialize()
        {
            await InitializeAsync(null);
        }

   
        public void Initialize(object parameter)
        {
            _ = InitializeAsync(parameter);
        }

      
        public override async Task InitializeAsync(object? parameter)
        {
            try
            {
                IsBusy = true;
                BusyMessage = "Завантаження користувачів...";

                _logger.Information("Initializing UserManagementViewModel");

                if (!_sessionService.HasPermission("manage_users"))
                {
                    await _dialogService.ShowWarningAsync(
                        "Недостатньо прав",
                        "У вас немає прав для управління користувачами");
                    _logger.Warning("User {UserId} attempted to access user management without permission",
                        _sessionService.CurrentUser?.Id);
                    return;
                }

                await Task.WhenAll(
                    LoadCurrentPageAsync(),
                    LoadGuestRequestsAsync()
                );

                if (parameter is IDictionary<string, object> parameters)
                {
                    if (parameters.TryGetValue("Tab", out var tab) && tab is string tabName)
                    {
                        if (tabName == "access_rights")
                        {
                            _logger.Information("Switching to access rights tab");
                        }
                    }

                    if (parameters.TryGetValue("UserId", out var userId) && userId is string userIdStr)
                    {
                   
                        var user = _usersCache.Items.FirstOrDefault(u => u.Id == userIdStr);
                        if (user != null)
                        {
                            SelectedUser = user;
                            _logger.Information("Selected user {UserId} from parameters", userIdStr);
                        }
                    }
                }

                _logger.Information("UserManagementViewModel initialized successfully");

            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error initializing UserManagementViewModel");
                await _dialogService.ShowErrorAsync(
                    "Помилка ініціалізації",
                    "Не вдалося завантажити дані користувачів",
                    ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Data Loading

        private async Task LoadCurrentPageAsync()
        {
            try
            {
                IsBusy = true;
                BusyMessage = "Завантаження користувачів...";

                _logger.Information("Loading users page {Page} with page size {PageSize}",
                    CurrentPage, PageSize);

                var filter = BuildUserFilter();

                TotalUsers = await _userRepository.CountAsync(filter);
                TotalPages = (int)Math.Ceiling((double)TotalUsers / PageSize);

                var users = await _userRepository.GetPagedAsync(
                    CurrentPage,
                    PageSize, filter);

                _usersCache.Clear();
                var enumerable = users.ToList();
                _usersCache.AddOrUpdate(enumerable);

                ActiveUsers = await _userRepository.CountAsync(u => u.IsActive);

                _logger.Information("Loaded {Count} users for page {Page}",
                    enumerable.Count(), CurrentPage);

                this.RaisePropertyChanged(nameof(PaginationText));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error loading users page {Page}", CurrentPage);
                await _dialogService.ShowErrorAsync(
                    "Помилка завантаження",
                    "Не вдалося завантажити список користувачів",
                    ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadGuestRequestsAsync()
        {
            try
            {
                _logger.Information("Loading guest requests");

                var requests = await _guestRequestRepository.GetPendingRequestsAsync();
                _requestsCache.Clear();
                var guestRequests = requests.ToList();
                _requestsCache.AddOrUpdate(guestRequests);

                _logger.Information("Loaded {Count} pending guest requests", guestRequests.Count());
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error loading guest requests");
            }
        }

        private async Task ViewGuestRequestsAsync()
        {
            try
            {
                _logger.Information("Opening guest requests view");

                await LoadGuestRequestsAsync();

                var guestRequestsView = new Views.Controls.GuestRequestsView
                {
                    DataContext = this
                };

                await _dialogService.ShowCustomDialogAsync<object>(
                    "Заявки гостей",
                    guestRequestsView,
                    primaryButton: null, 
                    cancelButton: "Закрити");

                _logger.Information("Guest requests view closed");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error showing guest requests view");
                await _dialogService.ShowErrorAsync(
                    "Помилка",
                    "Не вдалося відкрити заявки гостей",
                    ex);
            }
        }

        private async Task LoadPreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
            }
        }

        private async Task LoadNextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
            }
        }

        #endregion

        #region User CRUD Operations

        private async Task AddUserAsync()
        {
            try
            {
                _logger.Information("Adding new user");

                var dialogViewModel = new Dialogs.UserEditDialogViewModel(null);

                var result = await _dialogService.ShowViewModelDialogAsync<
                    Dialogs.UserEditDialogViewModel,
                    User>(dialogViewModel);

                if (result == null)
                {
                    _logger.Information("User creation cancelled");
                    return;
                }

                IsBusy = true;
                BusyMessage = "Створення користувача...";

                var existingUser = await _userRepository.GetByLoginAsync(result.Login);
                if (existingUser != null)
                {
                    await _dialogService.ShowWarningAsync(
                        "Користувач існує",
                        $"Користувач з логіном '{result.Login}' вже існує в системі.");
                    return;
                }

                var passwordHash = _securityService.HashPassword(result.PasswordHash);
                result.PasswordHash = passwordHash;
                result.CreatedDate = DateTime.UtcNow;
                result.ModifiedDate = DateTime.UtcNow;

                await _userRepository.CreateAsync(result);
                _logger.Information("Created user {UserId} - {Login}", result.Id, result.Login);

                var keys = new Keys
                {
                    Login = result.Login,
                    PasswordHash = passwordHash,
                    AccessRights = new KeyAccessRights
                    {
                        Role = result.Role,
                        DatabaseAccess = result.Role switch
                        {
                            UserRole.Administrator => DatabaseAccessLevel.Full,
                            UserRole.Operator => DatabaseAccessLevel.ReadWrite,
                            UserRole.Authorized => DatabaseAccessLevel.ReadOnly,
                            _ => DatabaseAccessLevel.None
                        },
                        SpecificPermissions = BuildSpecificPermissions(result.AccessRights)
                    },
                    AccountLocked = false,
                    FailedLoginAttempts = 0,
                    LastPasswordChange = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };

                await _keysRepository.CreateAsync(keys);
                _logger.Information("Created keys for user {Login}", result.Login);

                _usersCache.AddOrUpdate(result);

                await _dialogService.ShowSuccessAsync(
                    $"Користувача '{result.FullName}' успішно створено");

                _dialogService.ShowNotification(
                    "Користувача створено",
                    $"Користувача '{result.FullName}' ({result.Login}) додано до системи",
                    NotificationPosition.BottomRight,
                    NotificationSeverity.Success);

                await LoadCurrentPageAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error creating user");
                await _dialogService.ShowErrorAsync(
                    "Помилка створення",
                    "Не вдалося створити користувача",
                    ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task EditUserAsync(User user)
        {
            try
            {
                if (user == null)
                {
                    await _dialogService.ShowWarningAsync(
                        "Користувача не обрано",
                        "Будь ласка, оберіть користувача для редагування");
                    return;
                }

                _logger.Information("Editing user {UserId} - {Login}", user.Id, user.Login);

                var dialogViewModel = new Dialogs.UserEditDialogViewModel(user);

                var result = await _dialogService.ShowViewModelDialogAsync<
                    Dialogs.UserEditDialogViewModel,
                    User>(dialogViewModel);

                if (result == null)
                {
                    _logger.Information("User editing cancelled");
                    return;
                }

                IsBusy = true;
                BusyMessage = "Збереження змін...";

                result.ModifiedDate = DateTime.UtcNow;
                await _userRepository.UpdateAsync(u => u.Id == result.Id, result);
                _logger.Information("Updated user {UserId} - {Login}", result.Id, result.Login);

                var keys = await _keysRepository.GetByLoginAsync(result.Login);
                if (keys != null)
                {
                    keys.AccessRights.Role = result.Role;
                    keys.AccessRights.DatabaseAccess = result.Role switch
                    {
                        UserRole.Administrator => DatabaseAccessLevel.Full,
                        UserRole.Operator => DatabaseAccessLevel.ReadWrite,
                        UserRole.Authorized => DatabaseAccessLevel.ReadOnly,
                        _ => DatabaseAccessLevel.None
                    };
                    keys.AccessRights.SpecificPermissions = BuildSpecificPermissions(result.AccessRights);
                    keys.ModifiedDate = DateTime.UtcNow;

                    await _keysRepository.UpdateAsync(k => k.Id == keys.Id, keys);
                    _logger.Information("Updated keys for user {Login}", result.Login);
                }

                _usersCache.AddOrUpdate(result);

                await _dialogService.ShowSuccessAsync(
                    $"Дані користувача '{result.FullName}' успішно оновлено");

                _dialogService.ShowNotification(
                    "Користувача оновлено",
                    $"Зміни для '{result.FullName}' збережено",
                    NotificationPosition.BottomRight,
                    NotificationSeverity.Success);

                await LoadCurrentPageAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error editing user {UserId}", user?.Id);
                await _dialogService.ShowErrorAsync(
                    "Помилка редагування",
                    "Не вдалося зберегти зміни користувача",
                    ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DeleteUserAsync(User user)
        {
            try
            {
                if (user == null)
                {
                    await _dialogService.ShowWarningAsync(
                        "Користувача не обрано",
                        "Будь ласка, оберіть користувача для видалення");
                    return;
                }

                if (user.Id == _sessionService.CurrentUser?.Id)
                {
                    await _dialogService.ShowWarningAsync(
                        "Неможлива операція",
                        "Ви не можете видалити свій власний обліковий запис");
                    return;
                }

                if (user.Role == UserRole.Administrator && user.Login == "admin")
                {
                    await _dialogService.ShowWarningAsync(
                        "Неможлива операція",
                        "Неможливо видалити головний адміністраторський обліковий запис");
                    return;
                }

                _logger.Information("Attempting to delete user {UserId} - {Login}", user.Id, user.Login);

                var confirmed = await _dialogService.ShowConfirmAsync(
                    "Підтвердження видалення",
                    $"Ви впевнені, що хочете видалити користувача '{user.FullName}' ({user.Login})?\n\n" +
                    $"Ця дія незворотна і призведе до видалення всіх пов'язаних даних.");

                if (!confirmed)
                {
                    _logger.Information("User deletion cancelled by user");
                    return;
                }

                IsBusy = true;
                BusyMessage = "Видалення користувача...";

                var keys = await _keysRepository.GetByLoginAsync(user.Login);
                if (keys != null)
                {
                    await _keysRepository.DeleteByIdAsync(keys.Id);
                    _logger.Information("Deleted keys for user {Login}", user.Login);
                }

                await _userRepository.DeleteByIdAsync(user.Id);
                _logger.Information("Deleted user {UserId} - {Login}", user.Id, user.Login);

                _usersCache.Remove(user);

                if (SelectedUser?.Id == user.Id)
                {
                    SelectedUser = null;
                    IsUserDetailsVisible = false;
                }

                await _dialogService.ShowSuccessAsync(
                    $"Користувача '{user.FullName}' успішно видалено");

                _dialogService.ShowNotification(
                    "Користувача видалено",
                    $"Користувача '{user.FullName}' ({user.Login}) видалено з системи",
                    NotificationPosition.BottomRight,
                    NotificationSeverity.Success);

                await LoadCurrentPageAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error deleting user {UserId}", user?.Id);
                await _dialogService.ShowErrorAsync(
                    "Помилка видалення",
                    $"Не вдалося видалити користувача '{user?.FullName}'.\n\n" +
                    "Можливо, цей користувач пов'язаний з іншими записами в системі.",
                    ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Guest Requests

        private async Task ApproveRequestAsync(GuestRequest request)
        {
            try
            {
                if (request == null) return;

                _logger.Information("Approving guest request {RequestId}", request.Id);

                var confirmed = await _dialogService.ShowConfirmAsync(
                    "Підтвердження схвалення",
                    $"Ви впевнені, що хочете схвалити заявку від:\n\n" +
                    $"ПІБ: {request.FullName}\n" +
                    $"Логін: {request.Login}\n" +
                    $"Email: {request.Email}\n" +
                    $"Організація: {request.Organization}\n\n" +
                    "Користувачу буде надано права Авторизованого користувача.");

                if (!confirmed)
                {
                    _logger.Information("Guest request approval cancelled");
                    return;
                }

                IsBusy = true;
                BusyMessage = "Схвалення заявки...";

                var existingUser = await _userRepository.GetByLoginAsync(request.Login);
                if (existingUser != null)
                {
                    await _dialogService.ShowWarningAsync(
                        "Користувач існує",
                        $"Користувач з логіном '{request.Login}' вже існує в системі.\n\n" +
                        "Заявка буде відхилена.");

                    await RejectRequestAsync(request, "Користувач з таким логіном вже існує");
                    return;
                }

                var existingRequestByLogin = await _userRepository.GetByLoginAsync(request.Login);
                if (existingRequestByLogin != null)
                {
                    await _dialogService.ShowWarningAsync(
                        "Логін зайнятий",
                        $"Користувач з логіном '{request.Login}' вже існує в системі.");
                    return;
                }

                var existingRequest = await _guestRequestRepository.GetByEmailAsync(request.Email);
                if (existingRequest != null && existingRequest.Id != request.Id && 
                    existingRequest.Status == RequestStatus.Pending)
                {
                    await _dialogService.ShowWarningAsync(
                        "Заявка існує",
                        "Для цього email вже існує інша активна заявка.");
                    return;
                }

                var newPassword = _securityService.GenerateSecureToken()[..6];
                var passwordHash = _securityService.HashPassword(newPassword);

                var newUser = new User
                {
                    Id = request.GuestUserId,
                    Login = request.Login,
                    FullName = request.FullName,
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    Phone = request.MobilePhone ?? string.Empty,
                    Role = UserRole.Authorized,
                    IsActive = true,
                    AccessRights = new AccessRights
                    {
                        ViewData = true,
                        EditData = false,
                        DeleteData = false,
                        RunAggregations = true,
                        SaveResults = true,
                        ManageUsers = false
                    },
                    LastLogin = DateTime.UnixEpoch,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };

                await _userRepository.CreateAsync(newUser);
                _logger.Information("Created user {UserId} from guest request", newUser.Id);

                var keys = new Keys
                {
                    Login = newUser.Login,
                    PasswordHash = passwordHash,
                    AccessRights = new KeyAccessRights
                    {
                        Role = UserRole.Authorized,
                        DatabaseAccess = DatabaseAccessLevel.ReadOnly,
                        SpecificPermissions = new List<string>
                        {
                            "view_all_data",
                            "run_aggregations",
                            "export_data"
                        }
                    },
                    AccountLocked = false,
                    FailedLoginAttempts = 0,
                    LastPasswordChange = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };

                await _keysRepository.CreateAsync(keys);
                _logger.Information("Created keys for user {Login}", newUser.Login);

                var adminResponse = $"Заявка схвалена. Надано права авторизованого користувача.\n" +
                                   $"Ваш новий пароль: {newPassword}\n" +
                                   $"Рекомендуємо змінити його після першого входу.";

                await _guestRequestRepository.ApproveRequestAsync(
                    request.Id,
                    _sessionService.CurrentUser!.Id,
                    adminResponse);

                _logger.Information("Approved guest request {RequestId}", request.Id);

                _requestsCache.Remove(request);

                await LoadCurrentPageAsync();

                await _dialogService.ShowSuccessAsync(
                    $"Заявку від '{request.FullName}' успішно схвалено!\n\n" +
                    $"Пароль для користувача: {newPassword}");

                _dialogService.ShowNotification(
                    "Заявку схвалено",
                    $"Користувача '{request.FullName}' додано до системи",
                    NotificationPosition.BottomRight,
                    NotificationSeverity.Success);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error approving guest request {RequestId}", request?.Id);
                await _dialogService.ShowErrorAsync(
                    "Помилка схвалення",
                    "Не вдалося схвалити заявку. Спробуйте пізніше.",
                    ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RejectRequestAsync(GuestRequest request, string? reason = null)
        {
            try
            {
                if (request == null) return;

                _logger.Information("Rejecting guest request {RequestId}", request.Id);

                if (reason == null)
                {
                    reason = await _dialogService.ShowInputAsync(
                        "Відхилення заявки",
                        "Вкажіть причину відхилення (необов'язково):");
                }

                var confirmed = await _dialogService.ShowConfirmAsync(
                    "Підтвердження відхилення",
                    $"Ви впевнені, що хочете відхилити заявку від:\n\n" +
                    $"{request.FullName} (логін: {request.Login})?");

                if (!confirmed)
                {
                    _logger.Information("Guest request rejection cancelled");
                    return;
                }

                IsBusy = true;
                BusyMessage = "Відхилення заявки...";

                var adminResponse = string.IsNullOrWhiteSpace(reason)
                    ? "Заявка відхилена адміністратором."
                    : $"Заявка відхилена. Причина: {reason}";

                await _guestRequestRepository.RejectRequestAsync(
                    request.Id,
                    _sessionService.CurrentUser!.Id,
                    adminResponse);

                _logger.Information("Rejected guest request {RequestId}", request.Id);

                _requestsCache.Remove(request);

                await _dialogService.ShowSuccessAsync("Заявку відхилено");

                _dialogService.ShowNotification(
                    "Заявку відхилено",
                    $"Заявку від '{request.FullName}' відхилено",
                    NotificationPosition.BottomRight);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error rejecting guest request {RequestId}", request?.Id);
                await _dialogService.ShowErrorAsync(
                    "Помилка відхилення",
                    "Не вдалося відхилити заявку",
                    ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Security Operations

        private async Task LockAccountAsync(User user)
        {
            try
            {
                if (user == null)
                {
                    await _dialogService.ShowWarningAsync(
                        "Користувача не обрано",
                        "Будь ласка, оберіть користувача для блокування");
                    return;
                }

                if (user.Id == _sessionService.CurrentUser?.Id)
                {
                    await _dialogService.ShowWarningAsync(
                        "Неможлива операція",
                        "Ви не можете заблокувати свій власний обліковий запис");
                    return;
                }

                if (user.Role == UserRole.Administrator && user.Login == "admin")
                {
                    await _dialogService.ShowWarningAsync(
                        "Неможлива операція",
                        "Неможливо заблокувати головний адміністраторський обліковий запис");
                    return;
                }

                _logger.Information("Locking account for user {UserId} - {Login}", user.Id, user.Login);

                var confirmed = await _dialogService.ShowConfirmAsync(
                    "Блокування облікового запису",
                    $"Заблокувати обліковий запис для '{user.FullName}' ({user.Login})?\n" +
                    "Користувач не зможе увійти в систему до розблокування.");

                if (!confirmed)
                {
                    _logger.Information("Account lock cancelled");
                    return;
                }

                IsBusy = true;
                BusyMessage = "Блокування...";

                var keys = await _keysRepository.GetByLoginAsync(user.Login);
                if (keys == null)
                {
                    await _dialogService.ShowWarningAsync(
                        "Помилка",
                        "Не знайдено облікових даних для цього користувача");
                    return;
                }

                if (keys.AccountLocked)
                {
                    await _dialogService.ShowInfoAsync(
                        "Інформація",
                        "Обліковий запис вже заблокований");
                    return;
                }

                keys.AccountLocked = true;
                keys.ModifiedDate = DateTime.UtcNow;
                user.IsActive = false;
                await _keysRepository.UpdateAsync(k => k.Id == keys.Id, keys);
                await _userRepository.UpdateAsync(u => u.Id == user.Id, user);

                _logger.Information("Locked account for user {Login}", user.Login);

                await _dialogService.ShowSuccessAsync(
                    $"Обліковий запис '{user.FullName}' заблоковано");

                _dialogService.ShowNotification(
                    "Обліковий запис заблоковано",
                    $"Користувач '{user.FullName}' не може увійти в систему",
                    NotificationPosition.BottomRight,
                    NotificationSeverity.Warning);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error locking account for user {UserId}", user?.Id);
                await _dialogService.ShowErrorAsync(
                    "Помилка блокування",
                    "Не вдалося заблокувати обліковий запис",
                    ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task UnlockAccountAsync(User user)
        {
            try
            {
                if (user == null)
                {
                    await _dialogService.ShowWarningAsync(
                        "Користувача не обрано",
                        "Будь ласка, оберіть користувача для розблокування");
                    return;
                }

                _logger.Information("Unlocking account for user {UserId} - {Login}", user.Id, user.Login);

                var confirmed = await _dialogService.ShowConfirmAsync(
                    "Розблокування облікового запису",
                    $"Розблокувати обліковий запис для '{user.FullName}' ({user.Login})?\n\n" +
                    "Лічильник невдалих спроб входу буде скинуто.");

                if (!confirmed)
                {
                    _logger.Information("Account unlock cancelled");
                    return;
                }

                IsBusy = true;
                BusyMessage = "Розблокування...";

                var keys = await _keysRepository.GetByLoginAsync(user.Login);

                if (keys == null)
                {
                    await _dialogService.ShowWarningAsync(
                        "Помилка",
                        "Не знайдено облікових даних для цього користувача");
                    return;
                }

                if (!keys.AccountLocked)
                {
                    await _dialogService.ShowInfoAsync(
                        "Інформація",
                        "Обліковий запис не заблокований");
                    return;
                }

                user.IsActive = true;
                await _keysRepository.UnlockAccountAsync(keys.Id);
                await _userRepository.UpdateAsync(usr => usr.Id == user.Id, user);

                _logger.Information("Unlocked account for user {Login}", user.Login);

                await _dialogService.ShowSuccessAsync(
                    $"Обліковий запис '{user.FullName}' розблоковано");

                _dialogService.ShowNotification(
                    "Обліковий запис розблоковано",
                    $"Користувач '{user.FullName}' може увійти в систему",
                    NotificationPosition.BottomRight,
                    NotificationSeverity.Success);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error unlocking account for user {UserId}", user?.Id);
                await _dialogService.ShowErrorAsync(
                    "Помилка розблокування",
                    "Не вдалося розблокувати обліковий запис",
                    ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ResetPasswordAsync(User user)
        {
            try
            {
                if (user == null)
                {
                    await _dialogService.ShowWarningAsync(
                        "Користувача не обрано",
                        "Будь ласка, оберіть користувача для скидання паролю");
                    return;
                }

                _logger.Information("Resetting password for user {UserId} - {Login}", user.Id, user.Login);

                
                var newPassword = await _dialogService.ShowInputAsync("Новий пароль",
                    $"ВВедіть новий пароль для користувача: {user.Login}");

                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    return;
                }

                var confirmed = await _dialogService.ShowConfirmAsync(
                    "Скидання паролю",
                    $"Скинути пароль для '{user.FullName}' ({user.Login})?\n" +
                    $"Новий згенерований пароль: {newPassword}\n" +
                    "Користувач зможе увійти за допомогою цього паролю.");

                if (!confirmed)
                {
                    _logger.Information("Password reset cancelled");
                    return;
                }

                IsBusy = true;
                BusyMessage = "Скидання паролю...";

                var passwordHash = _securityService.HashPassword(newPassword);

                var keys = await _keysRepository.GetByLoginAsync(user.Login);
                if (keys == null)
                {
                    await _dialogService.ShowWarningAsync(
                        "Помилка",
                        "Не знайдено облікових даних для цього користувача");
                    return;
                }

                await _keysRepository.UpdatePasswordAsync(keys.Id, passwordHash);
                await _userRepository.UpdatePasswordAsync(user.Id, passwordHash);
                _logger.Information("Reset password for user {Login}", user.Login);

                await _dialogService.ShowSuccessAsync(
                    $"Пароль для '{user.FullName}' успішно скинуто!\n\n" +
                    $"Новий пароль: {newPassword}\n)");

                var contactThroughGuestRequest = await _guestRequestRepository.GetByLoginAsync(user.Login);

                if (contactThroughGuestRequest == null) {
                    _logger.Warning("No guest request found for user {Login} during password reset", user.Login);
                    return;
                }

                await _guestRequestRepository.ApproveRequestAsync(contactThroughGuestRequest.Id,
                    _sessionService.CurrentUser?.Id!, $"Ваш новий пароль: {newPassword}");

                _dialogService.ShowNotification(
                    "Пароль скинуто",
                    $"Новий пароль встановлено для '{user.FullName}'",
                    NotificationPosition.BottomRight,
                    NotificationSeverity.Success);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error resetting password for user {UserId}", user?.Id);
                await _dialogService.ShowErrorAsync(
                    "Помилка скидання паролю",
                    "Не вдалося скинути пароль користувача",
                    ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Helpers

        private Func<User, bool> CreateUserPredicate()
        {
            return user =>
            {
                if (!string.IsNullOrWhiteSpace(UserSearchText))
                {
                    var search = UserSearchText.ToLower();
                    var matchesSearch = user.Login.ToLower().Contains(search) ||
                                       user.FullName.ToLower().Contains(search) ||
                                       (user.Email?.ToLower().Contains(search) ?? false);

                    if (!matchesSearch) return false;
                }

                if (RoleFilter.HasValue && user.Role != RoleFilter.Value)
                    return false;

                if (StatusFilter.HasValue && user.IsActive != StatusFilter.Value)
                    return false;

                return true;
            };
        }

        private System.Linq.Expressions.Expression<Func<User, bool>> BuildUserFilter()
        {
            return u => true; 
        }

        private void LoadUserDetails(User? user)
        {
            if (user != null)
            {
                EditLogin = user.Login;
                EditPassword = string.Empty;
                EditFullName = user.FullName;
                EditRole = user.Role;
                EditIsActive = user.IsActive;
                EditAccessRights = user.AccessRights ?? new AccessRights();
                IsUserDetailsVisible = true;

                _logger.Debug("Loaded details for user {UserId}", user.Id);
            }
            else
            {
                ClearEditFields();
                IsUserDetailsVisible = false;
            }
        }

        private void ClearEditFields()
        {
            EditLogin = string.Empty;
            EditPassword = string.Empty;
            EditFullName = string.Empty;
            EditRole = UserRole.Guest;
            EditIsActive = true;
            EditAccessRights = new AccessRights();
        }

        private void ClearFilters()
        {
            UserSearchText = string.Empty;
            RoleFilter = null;
            StatusFilter = null;

            _dialogService.ShowNotification(
                "Фільтри очищено",
                "Всі фільтри скинуто",
                NotificationPosition.BottomRight);

            _logger.Information("Filters cleared");
        }

        private List<string> BuildSpecificPermissions(AccessRights accessRights)
        {
            var permissions = new List<string>();

            if (accessRights.ViewData)
                permissions.Add("view_all_data");

            if (accessRights.EditData)
            {
                permissions.Add("create_users");
                permissions.Add("modify_users");
                permissions.Add("manage_schedules");
                permissions.Add("issue_certificates");
            }

            if (accessRights.DeleteData)
            {
                permissions.Add("delete_all_data");

            }

            if (accessRights.RunAggregations)
            {
                permissions.Add("run_aggregations");

            }

            if (accessRights.SaveResults)
            {
                permissions.Add("export_data");
            }


            return permissions;
        }

        #endregion

        #region Cleanup

        public override void CleanUp()
        {
            _usersCache.Dispose();
            _requestsCache.Dispose();
            base.CleanUp();

            _logger.Information("UserManagementViewModel cleaned up");
        }

        #endregion
    }
}