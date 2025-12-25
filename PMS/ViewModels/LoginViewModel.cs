using PMS.Core.Enums.Window;
using PMS.Core.Models;
using PMS.Core.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Windows.Input;
using PMS.Core.Database;
using PMS.Core.Enums.General;
using PMS.Views.Window;
using ILoggerService = Serilog.ILogger;
using PMS.Core.Models.Common;
using PMS.Core.Services.Interfaces;

namespace PMS.ViewModels
{
    public class LoginViewModel : PageViewModelBase
    {
        private readonly IDatabaseContext _databaseContext;
        private readonly IAuthenticationService _authService;
        private readonly ISessionService _sessionService;
        private readonly IDialogService _dialogService;
        private readonly IRememberMeService _rememberMeService;
        private readonly ILocalizationService _localization;
        private readonly ILoggerService _loggerService;
        private readonly ISecurityService _securityService;
        private readonly IWindowService _windowService;

        private bool _isAuthenticated;

        public bool IsAuthenticated 
        { 
            get => _isAuthenticated; 
            set => SetAndRiseProperty(ref _isAuthenticated, value);
        }

        private readonly Dictionary<string, List<string>> _errors = new();

        private string? _username = string.Empty;
        private string? _password = string.Empty;
        private bool _rememberMe;
        private bool _showPassword;
        private bool _forgotPasswordVisible;
        private string _allPasswords = string.Empty;

        public LoginViewModel(
            IDatabaseContext databaseContext,
            IAuthenticationService authService,
            ISessionService sessionService,
            ILoggerService loggerService,

            IDialogService dialogService,
            IRememberMeService rememberMeService,
            ILocalizationService localizationService,
            ISecurityService securityService,
            IWindowService windowService
            )
        {

            _databaseContext = databaseContext;
            _dialogService = dialogService;
            _loggerService = loggerService;
            _authService = authService;
            _sessionService = sessionService;
            _rememberMeService = rememberMeService;
            _localization = localizationService;
            _securityService = securityService;
            _windowService = windowService;

            Mode = WindowMode.Standalone;


            InitializeLocalizedStrings();
            TryLoadCredentials();

            LoginCommand = ReactiveCommand.CreateFromTask(LoginAsync,
                this.WhenAnyValue(
                    x => x.Username,
                    x => x.Password,
                    (user, pass) =>
                        !string.IsNullOrWhiteSpace(user) &&
                        !string.IsNullOrWhiteSpace(pass)));

            ForgotPasswordCommand = ReactiveCommand.CreateFromTask(ShowForgotPasswordAsync);
            ShowPasswordCommand = ReactiveCommand.Create<bool>(show => ShowPassword = show);
            ChangePasswordCommand = ReactiveCommand.CreateFromTask(ChangePasswordAsync);
            NavigateToRegistrationCommand = ReactiveCommand.CreateFromTask(NavigateToRegistrationAsync);
        }

        #region Localized Strings

        public LocalizedString WindowTitle { get; private set; }
        public LocalizedString LoginHeader { get; private set; }
        public LocalizedString LoginSubtitle { get; private set; }
        public LocalizedString UsernameWatermark { get; private set; }
        public LocalizedString PasswordWatermark { get; private set; }
        public LocalizedString RememberMeTooltip { get; private set; }
        public LocalizedString ShowPasswordTooltip { get; private set; }
        public LocalizedString ForgotPasswordLink { get; private set; }
        public LocalizedString NoAccountLink { get; private set; }
        public LocalizedString NoAccountText { get; private set; }
        public LocalizedString RegistrationLink { get; private set; }
        public LocalizedString BackButton { get; private set; }
        public LocalizedString LoginButton { get; private set; }
        public LocalizedString ChangePasswordButton { get; private set; }

        public LocalizedString AuthenticatingMessage { get; private set; }
        public LocalizedString CredentialsSavedMessage { get; private set; }
        public LocalizedString CredentialsSaveErrorMessage { get; private set; }
        public LocalizedString OverwriteCredentialsTitle { get; private set; }
        public LocalizedString OverwriteCredentialsMessage { get; private set; }
        public LocalizedString PasswordChangeRequiredMessage { get; private set; }
        public LocalizedString UserDataErrorMessage { get; private set; }
        public LocalizedString ConnectionErrorMessage { get; private set; }
        public LocalizedString EnterUsernameForPasswordResetMessage { get; private set; }
        public LocalizedString SearchingAccountMessage { get; private set; }
        public LocalizedString AccountNotFoundMessage { get; private set; }
        public LocalizedString PasswordInDatabaseTitle { get; private set; }
        public LocalizedString UserNotInDatabaseMessage { get; private set; }
        public LocalizedString AdminPasswordChangeBlockedMessage { get; private set; }
        public LocalizedString AdminNoNoPasswordChangeMessage { get; private set; }
        public LocalizedString AccountLockedMessage { get; private set; }
        public LocalizedString PasswordsMismatchMessage { get; private set; }
        public LocalizedString SavedPasswordExpiryMessage { get; private set; }

        public LocalizedString ChangePasswordTitle { get; private set; }
        public LocalizedString ChangePasswordHeader { get; private set; }
        public LocalizedString CurrentPasswordPlaceholder { get; private set; }
        public LocalizedString NewPasswordPlaceholder { get; private set; }
        public LocalizedString ConfirmPasswordPlaceholder { get; private set; }
        public LocalizedString ChangePasswordPrimaryButton { get; private set; }
        public LocalizedString ChangePasswordSecondaryButton { get; private set; }


        public LocalizedString EmailRequiredMessage { get; private set; }
        public LocalizedString EmailFormatMessage { get; private set; }

        #endregion

        #region Properties

        [Required(ErrorMessage = "Вкажіть дійсний логін або електронну адресу.")]
        public string? Username
        {
            get => _username;
            set => SetAndRiseProperty(ref _username, value);
        }

        [Required(ErrorMessage = "Вкажіть ваш дійсний пароль.")]
        public string? Password
        {
            get => _password;
            set => SetAndRiseProperty(ref _password, value);
        }

        public bool RememberMe
        {
            get => _rememberMe;
            set => SetAndRiseProperty(ref _rememberMe, value);
        }

        public bool ShowPassword
        {
            get => _showPassword;
            set => SetAndRiseProperty(ref _showPassword, value);
        }

        public bool ForgotPasswordVisible
        {
            get => _forgotPasswordVisible;
            set => SetAndRiseProperty(ref _forgotPasswordVisible, value);
        }

        public string AllPasswords
        {
            get => _allPasswords;
            set => SetAndRiseProperty(ref _allPasswords, value);
        }

        #endregion

        #region Commands

        public ICommand LoginCommand { get; private set; }
        public ICommand ForgotPasswordCommand { get; private set; }
        public ICommand ChangePasswordCommand { get; private set; }
        public ICommand ShowPasswordCommand { get; private set; }
        public ICommand NavigateToRegistrationCommand { get; private set; }
      
        #endregion

        #region Events

        public Func<Task>? OnScopedLoginRequested { get; set; }
        public Action? OnNavigateToRegistrationRequested { get; set; }

        #endregion

        #region Initialization

        private void InitializeLocalizedStrings()
        {
            WindowTitle = new LocalizedString(_localization, "login.window.title");
            LoginHeader = new LocalizedString(_localization, "login.header");
            LoginSubtitle = new LocalizedString(_localization, "login.subtitle");
            UsernameWatermark = new LocalizedString(_localization, "login.username.watermark");
            PasswordWatermark = new LocalizedString(_localization, "login.password.watermark");
            RememberMeTooltip = new LocalizedString(_localization, "login.rememberme.tooltip");
            ShowPasswordTooltip = new LocalizedString(_localization, "login.showpassword.tooltip");
            ForgotPasswordLink = new LocalizedString(_localization, "login.forgotpassword.link");
            NoAccountLink = new LocalizedString(_localization, "login.noaccount.link");
            NoAccountText = new LocalizedString(_localization, "login.noaccount.text");
            RegistrationLink = new LocalizedString(_localization, "login.registration.link");
            BackButton = new LocalizedString(_localization, "login.button.back");
            LoginButton = new LocalizedString(_localization, "login.button.login");
            ChangePasswordButton = new LocalizedString(_localization, "login.button.changepassword");

            AuthenticatingMessage = new LocalizedString(_localization, "login.message.authenticating");
            CredentialsSavedMessage = new LocalizedString(_localization, "login.message.credentialssaved");
            CredentialsSaveErrorMessage = new LocalizedString(_localization, "login.message.credentialssaveerror");
            OverwriteCredentialsTitle = new LocalizedString(_localization, "login.dialog.overwritecredentials.title");
            OverwriteCredentialsMessage = new LocalizedString(_localization, "login.dialog.overwritecredentials.message");
            PasswordChangeRequiredMessage = new LocalizedString(_localization, "login.message.passwordchangerequired");
            UserDataErrorMessage = new LocalizedString(_localization, "login.message.userdataerror");
            ConnectionErrorMessage = new LocalizedString(_localization, "login.message.connectionerror");
            EnterUsernameForPasswordResetMessage = new LocalizedString(_localization, "login.message.enterusernameforpasswordreset");
            SearchingAccountMessage = new LocalizedString(_localization, "login.message.searchingaccount");
            AccountNotFoundMessage = new LocalizedString(_localization, "login.message.accountnotfound");
            PasswordInDatabaseTitle = new LocalizedString(_localization, "login.dialog.passwordindatabase.title");
            UserNotInDatabaseMessage = new LocalizedString(_localization, "login.message.usernotindatabase");
            AdminPasswordChangeBlockedMessage = new LocalizedString(_localization, "login.message.adminpasswordchangeblocked");
            AdminNoNoPasswordChangeMessage = new LocalizedString(_localization, "login.message.adminpasswordchangenono");
            AccountLockedMessage = new LocalizedString(_localization, "login.message.accountlocked");
            PasswordsMismatchMessage = new LocalizedString(_localization, "login.message.passwordsmismatch");
            SavedPasswordExpiryMessage = new LocalizedString(_localization, "login.message.savedpasswordexpiry");

            ChangePasswordTitle = new LocalizedString(_localization, "login.changepassword.title");
            ChangePasswordHeader = new LocalizedString(_localization, "login.changepassword.header");
            CurrentPasswordPlaceholder = new LocalizedString(_localization, "login.changepassword.currentpassword.placeholder");
            NewPasswordPlaceholder = new LocalizedString(_localization, "login.changepassword.newpassword.placeholder");
            ConfirmPasswordPlaceholder = new LocalizedString(_localization, "login.changepassword.confirmpassword.placeholder");
            ChangePasswordPrimaryButton = new LocalizedString(_localization, "login.changepassword.button.primary");
            ChangePasswordSecondaryButton = new LocalizedString(_localization, "login.changepassword.button.secondary");


            EmailRequiredMessage = new LocalizedString(_localization, "login.validation.email.required");
            EmailFormatMessage = new LocalizedString(_localization, "login.validation.email.format");
        }
        #endregion

        #region Command Handlers

        private async Task<bool> LoginAsync()
        {
            try
            {

                ShowInfoBar(AuthenticatingMessage.Value);

                var result = await _authService.AuthenticateAsync(Username, Password);

                if (result.Success)
                {
                    if (RememberMe)
                    {
                        if (!_rememberMeService.HasSavedCredentials())
                        {
                            try
                            {
                                _rememberMeService.SaveCredentials(Username, Password, SecuritySettings.RememberMeDuration);
                                await _dialogService.ShowSuccessAsync(CredentialsSavedMessage.Value);
                            }
                            catch (Exception ex)
                            {
                                _loggerService.Error(ex, "Failed to save remember me credentials.");
                                await _dialogService.ShowErrorAsync("Помилка",
                                    CredentialsSaveErrorMessage.Value);
                            }
                        }
                        else
                        {
                            var overrideData = await _dialogService.ShowConfirmAsync(
                                OverwriteCredentialsTitle.Value,
                                OverwriteCredentialsMessage.Value);
                            try
                            {
                                if (overrideData)
                                {
                                    _rememberMeService.ClearCredentials();
                                    _rememberMeService.SaveCredentials(Username, Password,
                                        SecuritySettings.RememberMeDuration);
                                    await _dialogService.ShowSuccessAsync(CredentialsSavedMessage.Value);
                                }
                            }
                            catch (Exception ex)
                            {
                                _loggerService.Error(ex, "Failed to save remember me credentials.");
                                await _dialogService.ShowErrorAsync("Помилка",
                                    CredentialsSaveErrorMessage.Value);
                            }
                        }
                    }

                    var user = await _authService.GetCurrentUserByIdAsync(result.UserId);

                    if (user != null)
                    {
                        _sessionService.StartSession(user, result.Token);
                        await _authService.UpdateLastLoginAsync(result.UserId);
                        
                        IsAuthenticated = true;

                        if (result.RequiresPasswordChange)
                        {
                            ShowErrorBar(PasswordChangeRequiredMessage.Value);
                            return true;
                        }

                        OnScopedLoginRequested?.Invoke();

                        if (OnScopedLoginRequested == null)
                        {
                            _windowService.Hide<AuthWindow>();
                            await MainWindowViewModel.ShowMainWindow();
                        }
                    }
                    else
                    {
                        ShowErrorBar(UserDataErrorMessage.Value);
                        return false;
                    }
                }
                else
                {
                    ShowErrorBar(result.Message);
                    return false;
                }
            }
            catch (Exception ex)
            {
                ShowErrorBar(ConnectionErrorMessage.Value);
                _loggerService.Error("Auth error:{ExMessage}", ex.Message);
                return false;
            }
            finally
            {
                await Task.Delay(5000);
                ForgotPasswordVisible = false;
                CleanUp();
            }

            return true;
        }

        private async Task NavigateToRegistrationAsync()
        {
            try
            {
                if (OnNavigateToRegistrationRequested == null)
                {
                    await _dialogService.ShowWarningAsync(
                        "Помилка навігації",
                        "Не вдалося перейти до реєстрації. Action не встановлено.");
                    return;
                }

                OnNavigateToRegistrationRequested.Invoke();
            }
            catch (Exception ex)
            {
                _loggerService.Error(ex, "Error navigating to registration");
                await _dialogService.ShowErrorAsync(
                    "Помилка",
                    "Не вдалося перейти до реєстрації",
                    ex);
            }
        }
        
        private async Task ShowForgotPasswordAsync()
        {
            ForgotPasswordVisible = false;
            
            if (string.IsNullOrWhiteSpace(Username))
            {
                ShowErrorBar(EnterUsernameForPasswordResetMessage.Value);
                return;
            }

            try
            {
                ShowInfoBar(SearchingAccountMessage.Value);
                
                await Task.Delay(250);

                var userResult = await _databaseContext.FindOneAsync<User>(
                    user => user.Login == Username || user.Email == Username, "users");


                var keysResult = await _databaseContext.FindOneAsync<Keys>(
                    keys => keys.Login == Username);

                if (keysResult == null || userResult == null) {
                    throw new KeyNotFoundException(AccountNotFoundMessage.Value);
                }

                AllPasswords = keysResult.PasswordHash;
                ShowInfoBar(AllPasswords, PasswordInDatabaseTitle.Value);
                ForgotPasswordVisible = true;
            }
            catch (Exception ex)
            {
                ShowErrorBar($"{ex.Message}");
            }
        }

        private async Task ChangePasswordAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Username))
                {
                    ShowErrorBar("Введіть логін");
                    await _dialogService.ShowWarningAsync(
                        "Логін не вказано",
                        "Будь ласка, введіть ваш логін або email для зміни паролю");
                    return;
                }

                var keys = await _authService.GetCurrentKeysAsync(Username);
                var user = await _authService.GetCurrentUserByLoginAsync(Username);

                if (keys == null || user == null)
                {
                    ShowErrorBar(UserNotInDatabaseMessage.Value);
                    await _dialogService.ShowErrorAsync(
                        "Користувача не знайдено",
                        "Не вдалося знайти користувача в базі даних");
                    return;
                }

                if (keys.AccountLocked)
                {
                    ShowErrorBar(AccountLockedMessage.Value);
                    await _dialogService.ShowErrorAsync(
                        "Обліковий запис заблоковано",
                        "Ваш обліковий запис заблоковано. Зверніться до адміністратора системи.");
                    return;
                }

                var currentPassword = await _dialogService.ShowInputAsync(
                    "Зміна паролю - Крок 1/3",
                    "Введіть ваш поточний пароль для підтвердження:");

                if (string.IsNullOrWhiteSpace(currentPassword))
                {
                    _dialogService.ShowNotification(
                        "Скасовано",
                        "Зміна паролю скасована",
                        Avalonia.Controls.Notifications.NotificationPosition.BottomRight,
                        NotificationSeverity.Information,
                        3);
                    return;
                }

                var authResult = await _authService.AuthenticateAsync(Username, currentPassword);
                if (!authResult.Success)
                {
                    ShowErrorBar("Невірний поточний пароль");
                    await _dialogService.ShowErrorAsync(
                        "Помилка автентифікації",
                        "Введений поточний пароль невірний. \nСпробуйте ще раз.");
                    return;
                }

                var newPassword = await _dialogService.ShowInputAsync(
                    "Зміна паролю - Крок 2/3",
                    "Введіть новий пароль (мінімум 8 символів):\n" +
                     "Вимоги:\n" + " 1. Хоча б одна велика літера\n" 
                    + " 2. Хоча б один унікальний сивмвол.\n" + " 3. Мінімум 8 символів."
                );

                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    _dialogService.ShowNotification(
                        "Скасовано",
                        "Зміна паролю скасована",
                        Avalonia.Controls.Notifications.NotificationPosition.BottomRight,
                        NotificationSeverity.Information,
                        3);
                    return;
                }

                if (!_securityService.ValidatePasswordStrength(newPassword, out string validationError))
                {
                    ShowErrorBar(validationError);
                    await _dialogService.ShowErrorAsync(
                        "Слабкий пароль",
                        validationError);
                    return;
                }

                var confirmPassword = await _dialogService.ShowInputAsync(
                    "Зміна паролю - Крок 3/3",
                    "Підтвердіть новий пароль:");

                if (newPassword != confirmPassword)
                {
                    ShowErrorBar(PasswordsMismatchMessage.Value);
                    await _dialogService.ShowErrorAsync(
                        "Паролі не співпадають",
                        "Введені паролі не співпадають. Спробуйте ще раз.");
                    return;
                }

                ShowInfoBar("Зміна паролю...");

                var passwordRequest = new PasswordUpdateRequest
                {
                    CurrentPassword = currentPassword,
                    NewPassword = newPassword,
                    ConfirmPassword = confirmPassword
                };

                var changeSuccessful = await _authService.ChangePasswordAsync(user.Id, passwordRequest);

                if (changeSuccessful)
                {
                    ShowSuccessBar("Пароль успішно змінено!");

                    await _dialogService.ShowSuccessAsync(
                        "Пароль успішно змінено!\n\n" +
                        "Тепер ви можете увійти в систему з новим паролем.");

                    _dialogService.ShowNotification(
                        "Пароль змінено",
                        "Ваш пароль успішно оновлено",
                        Avalonia.Controls.Notifications.NotificationPosition.BottomRight,
                        NotificationSeverity.Success,
                        6);

                    Password = string.Empty;
                    ForgotPasswordVisible = false;

                    _loggerService.Information("Password changed successfully for user {Username}", Username);
                }
                else
                {
                    ShowErrorBar("Не вдалося змінити пароль");
                    await _dialogService.ShowErrorAsync(
                        "Помилка зміни паролю",
                        "Не вдалося змінити пароль. Спробуйте пізніше або зверніться до адміністратора.");
                }
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка: {ex.Message}");
                _loggerService.Error(ex, "Error changing password for user {Username}", Username);

                await _dialogService.ShowErrorAsync(
                    "Помилка",
                    "Сталася помилка при зміні паролю",
                    ex);
            }
            finally
            {
                ForgotPasswordVisible = false;
            }
        }

        private void TryLoadCredentials()
        {
            try
            {

                var savedCredentials = _rememberMeService.TryLoadCredentials();

                if (savedCredentials is { IsValid: true })
                {
                    Username = savedCredentials.Username;
                    Password = savedCredentials.Password;
                    RememberMe = true;

                    var daysLeft = savedCredentials.DaysUntilExpiration;
                    ShowWarningBar(SavedPasswordExpiryMessage.Value.Replace("{0}", daysLeft.ToString()));
                }
            }
            catch (Exception ex)
            {
                _loggerService.Error(ex, "Failed to load saved credentials");
            }
        }

        #endregion
    }
}