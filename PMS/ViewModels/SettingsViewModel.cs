using Avalonia.Controls;
using PMS.Core.Services;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls.Notifications;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using PMS.Core.Enums.General;
using PMS.Core.Enums.Window;
using PMS.Core.Models.Common;
using PMS.Views.Window;
using ILogger = Serilog.ILogger;
using PMS.Core.Services.Interfaces;

namespace PMS.ViewModels;

public class SettingsViewModel : PageViewModelBase
{
    private readonly IDatabaseSettingsService _dbSettingsService;
    private readonly ILoggingSettingsService _loggingSettingsService;
    private readonly IApplicationSettingsService _applicationSettingsService;
    private readonly ILogger _logger;
    private readonly IDialogService _dialogService;
    private readonly IEncryptionService _encryptionService;
    private readonly ILocalizationService _localization;
    private readonly IWindowService _windowService;

    private DatabaseSettings? _config;
    private LoggingSettings _loggingSettings;
    private ApplicationSettings _applicationSettings;

    private string? _host = "localhost";
    private int _port = 27017;
    private string? _databaseName = "polyclinic_db";
    private bool _useAuthentication;
    private string? _username = string.Empty;
    private string? _password = string.Empty;
    private string? _authDatabase = "admin";
    private bool _showPassword;
    private char _passwordChar = '•';
    private int _connectionTimeout = 30;

    private string _logFilePath = "Logs/PMS-.log";
    private string _logRollingInterval = "Day";
    private int _retainedFileCountLimit = 30;

    private LanguageOption _selectedDefaultLanguage;
    private int _pageSize = 50;
    private int _maxExportRows = 10000;

    private bool _canContinue;
    private readonly Subject<bool> _credentialStateSubject = new();

    private readonly SettingsMementoCaretaker _mementoCaretaker = new();


    private record ConnectionValues(
        string? Host,
        int Port,
        string? DatabaseName,
        bool UseAuthentication,
        string? AuthDatabase
    );

    private ConnectionValues? _validatedValues;


    public SettingsViewModel(
        IDatabaseSettingsService configService,
        ILoggingSettingsService loggingSettingsService,
        IApplicationSettingsService applicationSettingsService,
        IDialogService dialogService,
        IEncryptionService encryptionService,
        ILocalizationService localizationService,
        ILogger logger,
        IWindowService windowService)
    {
        _dbSettingsService = configService;
        _loggingSettingsService = loggingSettingsService;
        _applicationSettingsService = applicationSettingsService;
        _logger = logger;
        _dialogService = dialogService;
        _encryptionService = encryptionService;
        _localization = localizationService;
        _windowService = windowService;

        Mode = WindowMode.Standalone;

        InitializeLocalizedStrings();
        InitializeCommands();
        InitializeLanguages();
        LoadSettings();
        SetupReactiveBindings();

        ShowInfoBar("Для початку слід:\n"
                    + " 1. Для роботи з системою слід заповнити налаштування бази даних (які надаються адміністратором установи).\n"
                    + " 2. Натиснути на кнопку 'Тест з'єднання', очікувати результату.\n"
                    + " 3. Якщо у вас існує обліковий запис у системі, можете скористатись секцією 'Авторизація', інакше 'Продовжити'.",
            "Початок роботи"
        );
    }



    #region Properties

    public string? Host
    {
        get => _host;
        set => SetAndRiseProperty(ref _host, value);
    }

    public int Port
    {
        get => _port;
        set => SetAndRiseProperty(ref _port, value);
    }

    public string? DatabaseName
    {
        get => _databaseName;
        set => SetAndRiseProperty(ref _databaseName, value);
    }

    public bool UseAuthentication
    {
        get => _useAuthentication;
        set => SetAndRiseProperty(ref _useAuthentication, value);
    }

    public string? Username
    {
        get => _username;
        set => SetAndRiseProperty(ref _username, value);
    }

    public string? Password
    {
        get => _password;
        set => SetAndRiseProperty(ref _password, value);
    }

    public string? AuthDatabase
    {
        get => _authDatabase;
        set => SetAndRiseProperty(ref _authDatabase, value);
    }

    public bool ShowPassword
    {
        get => _showPassword;
        set => SetAndRiseProperty(ref _showPassword, value);
    }

    public char PasswordChar
    {
        get => _passwordChar;
        set => SetAndRiseProperty(ref _passwordChar, value);
    }

    public int ConnectionTimeout
    {
        get => _connectionTimeout;
        set => SetAndRiseProperty(ref _connectionTimeout, value);
    }

    public string LogFilePath
    {
        get => _logFilePath;
        set => SetAndRiseProperty(ref _logFilePath, value);
    }

    public string LogRollingInterval
    {
        get => _logRollingInterval;
        set => SetAndRiseProperty(ref _logRollingInterval, value);
    }

    public int RetainedFileCountLimit
    {
        get => _retainedFileCountLimit;
        set => SetAndRiseProperty(ref _retainedFileCountLimit, value);
    }

    public ObservableCollection<LanguageOption> AvailableLanguages { get; } = [];

    public LanguageOption SelectedDefaultLanguage
    {
        get => _selectedDefaultLanguage;
        set => SetAndRiseProperty(ref _selectedDefaultLanguage, value);
    }

    public int PageSize
    {
        get => _pageSize;
        set => SetAndRiseProperty(ref _pageSize, value);
    }

    public int MaxExportRows
    {
        get => _maxExportRows;
        set => SetAndRiseProperty(ref _maxExportRows, value);
    }

    public bool CanContinue
    {
        get => _canContinue;
        set => SetAndRiseProperty(ref _canContinue, value);
    }


    public Func<Task>? OnExitRequested { get; set; }

    #endregion

    #region Localized Strings

    public LocalizedString ConfigTitle { get; private set; }
    public LocalizedString ConfigDescription { get; private set; }

    public LocalizedString DatabaseSectionTitle { get; private set; }
    public LocalizedString DatabaseSectionDescription { get; private set; }
    public LocalizedString DatabaseServerTitle { get; private set; }
    public LocalizedString DatabaseServerDescription { get; private set; }
    public LocalizedString AuthenticationTitle { get; private set; }
    public LocalizedString AuthenticationDescription { get; private set; }
    public LocalizedString ConnectionPoolTitle { get; private set; }
    public LocalizedString ConnectionPoolDescription { get; private set; }

    public LocalizedString LoggingSectionTitle { get; private set; }
    public LocalizedString LoggingSectionDescription { get; private set; }
    public LocalizedString LogFileTitle { get; private set; }
    public LocalizedString LogFileDescription { get; private set; }

    public LocalizedString ApplicationSectionTitle { get; private set; }
    public LocalizedString ApplicationSectionDescription { get; private set; }
    public LocalizedString LocalizationTitle { get; private set; }
    public LocalizedString LocalizationDescription { get; private set; }
    public LocalizedString DataManagementTitle { get; private set; }
    public LocalizedString DataManagementDescription { get; private set; }

    public LocalizedString HostLabel { get; private set; }
    public LocalizedString PortLabel { get; private set; }
    public LocalizedString DatabaseNameLabel { get; private set; }
    public LocalizedString UsernameLabel { get; private set; }
    public LocalizedString PasswordLabel { get; private set; }
    public LocalizedString AuthDatabaseLabel { get; private set; }
    public LocalizedString ConnectionTimeoutLabel { get; private set; }

    public LocalizedString LogFilePathLabel { get; private set; }
    public LocalizedString RollingIntervalLabel { get; private set; }
    public LocalizedString RetainedFilesLabel { get; private set; }
    public LocalizedString DefaultLanguageLabel { get; private set; }
    public LocalizedString PageSizeLabel { get; private set; }
    public LocalizedString MaxExportRowsLabel { get; private set; }

    public LocalizedString UsernamePlaceholder { get; private set; }
    public LocalizedString PasswordPlaceholder { get; private set; }
    public LocalizedString SecondsLabel { get; private set; }
    public LocalizedString FilesLabel { get; private set; }
    public LocalizedString RecordsLabel { get; private set; }

    public LocalizedString RollingIntervalDay { get; private set; }
    public LocalizedString RollingIntervalHour { get; private set; }
    public LocalizedString RollingIntervalWeek { get; private set; }
    public LocalizedString RollingIntervalMonth { get; private set; }


    public ObservableCollection<LocalizedString> RollingIntervalsCollection { get; set; }


    public LocalizedString ExitButtonLabel { get; private set; }
    public LocalizedString TestConnectionButtonLabel { get; private set; }
    public LocalizedString ContinueButtonLabel { get; private set; }

    public LocalizedString AuthTitle { get; private set; }
    public LocalizedString AuthDescription { get; private set; }
    public LocalizedString AuthLoginButton { get; private set; }
    public LocalizedString OtherSettingsTitle { get; private set; }
    public LocalizedString WindowTitle { get; private set; }

    #endregion

    #region Commands

    public ICommand TestConnectionCommand { get; private set; }
    public ICommand ContinueCommand { get; private set; }

    public ICommand ExitCommand { get; private set; }
    public ICommand BrowseLogPathCommand { get; private set; }
    public ICommand CancelCommand { get; private set; }
    public ICommand ShowLoginWindowCommand { get; private set; }

    public ICommand ChangeLanguageCommand { get; private set; }

    public ICommand SaveCommand { get; private set; }
    public ICommand DeleteCommand { get; private set; }
    public ICommand SetDefaultCommand { get; private set; }

    private async Task ExecuteSave(SettingsGroup group)
    {
        switch (group)
        {
            case SettingsGroup.Database:
                await SaveDatabaseSettings(); break;
            case SettingsGroup.Logging:
                await SaveLoggingSettings(); break;
            case SettingsGroup.Application:
                await SaveApplicationSettings(); break;
            default:
                throw new ArgumentOutOfRangeException(nameof(group), group, null);
        }
    }

    private async Task ExecuteDelete(SettingsGroup group)
    {
        switch (group)
        {
            case SettingsGroup.Database:
                await DeleteDatabaseSettings();
                break;
            case SettingsGroup.Logging:
                await DeleteLoggingSettings();
                break;
            case SettingsGroup.Application:
                await DeleteApplicationSettings();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(group), group, null);
        }
    }

    private async Task ExecuteToDefault(SettingsGroup group)
    {
        switch (group)
        {
            case SettingsGroup.Database:
                await SetDatabaseSettingsToDefault();
                break;
            case SettingsGroup.Logging:
                await SetLoggingSettingsToDefault();
                break;
            case SettingsGroup.Application:
                await SetApplicationSettingsToDefault();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(group), group, null);
        }
    }

    #endregion

    #region Initialization Methods

    private void InitializeLocalizedStrings()
    {
        ConfigTitle = new LocalizedString(_localization, "config.title");
        ConfigDescription = new LocalizedString(_localization, "config.description");

        DatabaseSectionTitle = new LocalizedString(_localization, "config.database.section.title");
        DatabaseSectionDescription = new LocalizedString(_localization, "config.database.section.description");
        DatabaseServerTitle = new LocalizedString(_localization, "config.database.server.title");
        DatabaseServerDescription = new LocalizedString(_localization, "config.database.server.description");
        AuthenticationTitle = new LocalizedString(_localization, "config.database.auth.title");
        AuthenticationDescription = new LocalizedString(_localization, "config.database.auth.description");
        ConnectionPoolTitle = new LocalizedString(_localization, "config.database.pool.title");
        ConnectionPoolDescription = new LocalizedString(_localization, "config.database.pool.description");

        LoggingSectionTitle = new LocalizedString(_localization, "config.logging.section.title");
        LoggingSectionDescription = new LocalizedString(_localization, "config.logging.section.description");
        LogFileTitle = new LocalizedString(_localization, "config.logging.file.title");
        LogFileDescription = new LocalizedString(_localization, "config.logging.file.description");

        ApplicationSectionTitle = new LocalizedString(_localization, "config.application.section.title");
        ApplicationSectionDescription = new LocalizedString(_localization, "config.application.section.description");
        LocalizationTitle = new LocalizedString(_localization, "config.application.localization.title");
        LocalizationDescription = new LocalizedString(_localization, "config.application.localization.description");
        DataManagementTitle = new LocalizedString(_localization, "config.application.data.title");
        DataManagementDescription = new LocalizedString(_localization, "config.application.data.description");

        HostLabel = new LocalizedString(_localization, "config.database.host.label");
        PortLabel = new LocalizedString(_localization, "config.database.port.label");
        DatabaseNameLabel = new LocalizedString(_localization, "config.database.name.label");
        UsernameLabel = new LocalizedString(_localization, "config.database.username.label");
        PasswordLabel = new LocalizedString(_localization, "config.database.password.label");
        AuthDatabaseLabel = new LocalizedString(_localization, "config.database.authdb.label");
        ConnectionTimeoutLabel = new LocalizedString(_localization, "config.database.timeout.label");

        LogFilePathLabel = new LocalizedString(_localization, "config.logging.filepath.label");
        RollingIntervalLabel = new LocalizedString(_localization, "config.logging.interval.label");
        RetainedFilesLabel = new LocalizedString(_localization, "config.logging.retained.label");
        DefaultLanguageLabel = new LocalizedString(_localization, "config.application.language.label");
        PageSizeLabel = new LocalizedString(_localization, "config.application.pagesize.label");
        MaxExportRowsLabel = new LocalizedString(_localization, "config.application.exportrows.label");

        UsernamePlaceholder = new LocalizedString(_localization, "config.database.username.placeholder");
        PasswordPlaceholder = new LocalizedString(_localization, "config.database.password.placeholder");
        SecondsLabel = new LocalizedString(_localization, "config.common.seconds");
        FilesLabel = new LocalizedString(_localization, "config.common.files");
        RecordsLabel = new LocalizedString(_localization, "config.common.records");

        RollingIntervalDay = new LocalizedString(_localization, "config.logging.interval.day");
        RollingIntervalHour = new LocalizedString(_localization, "config.logging.interval.hour");
        RollingIntervalWeek = new LocalizedString(_localization, "config.logging.interval.week");
        RollingIntervalMonth = new LocalizedString(_localization, "config.logging.interval.month");

        RollingIntervalsCollection =
        [
            RollingIntervalHour,
            RollingIntervalDay,
            RollingIntervalWeek,
            RollingIntervalMonth
        ];

        ExitButtonLabel = new LocalizedString(_localization, "config.button.exit");
        TestConnectionButtonLabel = new LocalizedString(_localization, "config.button.testconnection");
        ContinueButtonLabel = new LocalizedString(_localization, "config.button.continue");

        AuthTitle = new LocalizedString(_localization, "config.auth.title");
        AuthDescription = new LocalizedString(_localization, "config.auth.description");
        AuthLoginButton = new LocalizedString(_localization, "config.auth.login");
        OtherSettingsTitle = new LocalizedString(_localization, "config.other.title");
        WindowTitle = new LocalizedString(_localization, "config.window.title");
    }


    private async Task SaveDatabaseSettings()
    {
        try
        {
            _config = BuildConfigFromUi();
            if (!_config.UseAuthentication)
            {
                var requestedAuth = await _dialogService.ShowCustomDialogAsync<bool>("Налаштування",
                    DialogService.CreateMessageContent(
                        "Ви не використовуєте автенфікацію.\nВикористати її та продовжити ?",
                        Symbol.Permissions, Brushes.Orange),
                    "Так",
                    "Ні");
                if (requestedAuth)
                {
                    UseAuthentication = true;
                    _config.UseAuthentication = true;
                }
            }


            var result = _dbSettingsService.ValidateSettings(_config, out string msg);
            if (result != ValidationResult.Valid)
            {
                _dialogService.ShowNotification(
                    "Помилка збереження налаштувань бази даних",
                    msg,
                    NotificationPosition.TopCenter,
                    NotificationSeverity.Error,
                    TimeSpan.FromMilliseconds(-1).Seconds
                );
                _canContinue = false;
                return;
            }

            if (_dbSettingsService.Exists())
            {
                var overrideConfirmed = await _dialogService.ShowCustomDialogAsync<bool>(
                    "Налаштування",
                    DialogService.CreateMessageContent(
                        "Перезаписати існуючі налаштування ?",
                        Symbol.Sync, Brushes.Orange),
                    "Так",
                    "Ні");

                if (!overrideConfirmed)
                {
                    return;
                }
            }

            if (_dbSettingsService.SaveSettings(_config))
            {
                _logger.Information("Database settings saved successfully");
                await _dialogService.ShowSuccessAsync("Збережено!");
                _canContinue = true;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception occurred while saving database settings");
            await _dialogService.ShowErrorAsync("Помилка збереження",
                "Не вдалось зберегти налатшування, виникла несподівана помилка.");
            _canContinue = false;
        }
    }

    private async Task SaveLoggingSettings()
    {
        try
        {
            _loggingSettings.FilePath = LogFilePath;
            _loggingSettings.RollingInterval = LogRollingInterval;
            _loggingSettings.RetainedFileCountLimit = RetainedFileCountLimit;

            var validationResult = _loggingSettingsService.ValidateSettings(_loggingSettings, out string errorMessage);

            if (validationResult != ValidationResult.Valid)
            {
                _logger.Warning("Logging settings validation failed: {Error}", errorMessage);
                _dialogService.ShowNotification(
                    "Помилка збереження налаштувань журналювання",
                    errorMessage,
                    NotificationPosition.TopCenter,
                    NotificationSeverity.Error,
                    TimeSpan.FromMilliseconds(-1).Seconds
                );
                return;
            }

            if (_loggingSettingsService.Exists())
            {
                var overrideConfirmed = await _dialogService.ShowCustomDialogAsync<bool>(
                    "Налаштування",
                    DialogService.CreateMessageContent(
                        "Перезаписати існуючі налаштування журналювання?",
                        Symbol.Sync,
                        Brushes.Orange),
                    "Так",
                    "Ні");

                if (!overrideConfirmed)
                {
                    _logger.Information("User cancelled logging settings override");
                    return;
                }
            }

            if (_loggingSettingsService.SaveSettings(_loggingSettings))
            {
                _logger.Information("Logging settings saved successfully");
                await _dialogService.ShowSuccessAsync("Налаштування журналювання збережено!");
            }
            else
            {
                _logger.Error("Failed to save logging settings");
                await _dialogService.ShowErrorAsync(
                    "Помилка збереження",
                    "Не вдалося зберегти налаштування логування.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception occurred while saving logging settings");
            await _dialogService.ShowErrorAsync(
                "Помилка збереження",
                "Не вдалося зберегти налаштування логування.");
        }
    }

    private async Task SaveApplicationSettings()
    {
        try
        {
            _applicationSettings.DefaultLanguage = SelectedDefaultLanguage.DisplayName;
            _applicationSettings.PageSize = PageSize;
            _applicationSettings.MaxExportRows = MaxExportRows;

            var validationResult =
                _applicationSettingsService.ValidateSettings(_applicationSettings, out string errorMessage);

            if (validationResult != ValidationResult.Valid)
            {
                _logger.Warning("Application settings validation failed: {Error}", errorMessage);
                _dialogService.ShowNotification(
                    "Помилка збереження",
                    errorMessage,
                    NotificationPosition.TopCenter,
                    NotificationSeverity.Error,
                    TimeSpan.FromMilliseconds(-1).Seconds
                );
                return;
            }

            if (_applicationSettingsService.Exists())
            {
                var overrideConfirmed = await _dialogService.ShowCustomDialogAsync<bool>(
                    "Налаштування",
                    DialogService.CreateMessageContent(
                        "Перезаписати існуючі налаштування програми?",
                        Symbol.Sync,
                        Brushes.Orange),
                    "Так",
                    "Ні");

                if (!overrideConfirmed)
                {
                    _logger.Information("User cancelled application settings override");
                    return;
                }
            }

            if (_applicationSettingsService.SaveSettings(_applicationSettings))
            {
                _logger.Information("Application settings saved successfully");
                await _dialogService.ShowSuccessAsync("Налаштування програми збережено!");
            }
            else
            {
                _logger.Error("Failed to save application settings");
                await _dialogService.ShowErrorAsync(
                    "Помилка збереження",
                    "Не вдалося зберегти налаштування програми.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception occurred while saving application settings");
            await _dialogService.ShowErrorAsync(
                "Помилка збереження",
                "Не вдалося зберегти налаштування програми.");
        }
    }

    private async Task DeleteDatabaseSettings()
    {
        try
        {
            if (!_dbSettingsService.Exists())
            {
                await _dialogService.ShowInfoAsync(
                    "Інформація",
                    "Файл налаштувань бази даних не існує.");
                return;
            }

            var confirmed = await _dialogService.ShowCustomDialogAsync<bool>(
                "Підтвердження видалення",
                DialogService.CreateMessageContent(
                    "Ви впевнені, що хочете видалити налаштування бази даних?\n\nЦю дію неможливо скасувати.",
                    Symbol.Delete,
                    Brushes.PaleVioletRed),
                "Видалити",
                "Скасувати");

            if (!confirmed)
            {
                _logger.Information("User cancelled database settings deletion");
                return;
            }

            _dbSettingsService.Delete();
            await _dialogService.ShowSuccessAsync("Налаштування бази даних видалено!");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete database settings");
            await _dialogService.ShowErrorAsync(
                "Помилка видалення",
                $"Не вдалося видалити налаштування: {ex.Message}");
        }
    }

    private async Task DeleteLoggingSettings()
    {
        try
        {
            if (!_loggingSettingsService.Exists())
            {
                await _dialogService.ShowInfoAsync(
                    "Інформація",
                    "Файл налаштувань логування не існує.");
                return;
            }

            var confirmed = await _dialogService.ShowCustomDialogAsync<bool>(
                "Підтвердження видалення",
                DialogService.CreateMessageContent(
                    "Ви впевнені, що хочете видалити налаштування логування?\nТипові налаштування будуть використані при наступному запуску.",
                    Symbol.Delete,
                    Brushes.PaleVioletRed),
                "Видалити",
                "Скасувати");

            if (!confirmed)
            {
                _logger.Information("User cancelled logging settings deletion");
                return;
            }

            _loggingSettingsService.Delete();
            _logger.Information("Logging settings file deleted successfully");
            await _dialogService.ShowSuccessAsync("Налаштування логування видалено!");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete logging settings");
            await _dialogService.ShowErrorAsync(
                "Помилка видалення",
                $"Не вдалося видалити налаштування: {ex.Message}");
        }
    }

    private async Task DeleteApplicationSettings()
    {
        try
        {
            if (!_applicationSettingsService.Exists())
            {
                await _dialogService.ShowInfoAsync(
                    "Інформація",
                    "Файл налаштувань програми не існує.");
                return;
            }

            var confirmed = await _dialogService.ShowCustomDialogAsync<bool>(
                "Підтвердження видалення",
                DialogService.CreateMessageContent(
                    "Ви впевнені, що хочете видалити налаштування програми?\nТипові налаштування будуть використані при наступному запуску.",
                    Symbol.Delete,
                    Brushes.PaleVioletRed),
                "Видалити",
                "Скасувати");

            if (!confirmed)
            {
                _logger.Information("User cancelled application settings deletion");
                return;
            }

            _applicationSettingsService.Delete();
            await _dialogService.ShowSuccessAsync("Налаштування програми видалено!");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete application settings");
            await _dialogService.ShowErrorAsync(
                "Помилка видалення",
                $"Не вдалося видалити налаштування: {ex.Message}");
        }
    }

    private async Task SetDatabaseSettingsToDefault()
    {
        try
        {
            if (_mementoCaretaker.DatabaseMemento != null)
            {
                var memento = _mementoCaretaker.DatabaseMemento;
                Host = memento.Host;
                Port = memento.Port;
                DatabaseName = memento.DatabaseName;
                UseAuthentication = memento.UseAuthentication;
                Username = memento.Username;
                Password = memento.Password;
                AuthDatabase = memento.AuthDatabase;
                ConnectionTimeout = memento.ConnectionTimeout;

                _logger.Information("Database settings restored from memento");
                _dialogService.ShowNotification(
                    "Відновлено",
                    "Налаштування бази даних відновлено до попередніх значень",
                    NotificationPosition.TopCenter);

                _mementoCaretaker.ClearDatabaseMemento();
            }
            else
            {
                var memento = new DatabaseSettingsMemento(
                    Host,
                    Port,
                    DatabaseName,
                    UseAuthentication,
                    Username,
                    Password,
                    AuthDatabase,
                    ConnectionTimeout
                );
                _mementoCaretaker.SaveDatabaseState(memento);

                var defaults = _dbSettingsService.ResetToDefaults();
                Host = defaults.Host;
                Port = defaults.Port;
                DatabaseName = defaults.DatabaseName;
                UseAuthentication = defaults.UseAuthentication;
                Username = defaults.Username;
                Password = defaults.Password;
                AuthDatabase = defaults.AuthDatabase;
                ConnectionTimeout = defaults.ConnectionTimeout;

                _logger.Information("Database settings reset to defaults, previous state saved");
                _dialogService.ShowNotification(
                    "Скинуто до типових",
                    "Налаштування бази даних скинуто до типових значень",
                    NotificationPosition.TopCenter);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to toggle database settings");
        }
    }

    private async Task SetLoggingSettingsToDefault()
    {
        try
        {
            if (_mementoCaretaker.LoggingMemento != null)
            {
                var memento = _mementoCaretaker.LoggingMemento;
                LogFilePath = memento.LogFilePath;
                LogRollingInterval = memento.LogRollingInterval;
                RetainedFileCountLimit = memento.RetainedFileCountLimit;

                _logger.Information("Logging settings restored from memento");
                _dialogService.ShowNotification(
                    "Відновлено",
                    "Налаштування логування відновлено до попередніх значень",
                    NotificationPosition.TopCenter);

                _mementoCaretaker.ClearLoggingMemento();
            }
            else
            {
                var memento = new LoggingSettingsMemento(
                    LogFilePath,
                    LogRollingInterval,
                    RetainedFileCountLimit
                );
                _mementoCaretaker.SaveLoggingState(memento);

                var defaults = _loggingSettingsService.ResetToDefaults();
                LogFilePath = defaults.FilePath;
                LogRollingInterval = defaults.RollingInterval;
                RetainedFileCountLimit = defaults.RetainedFileCountLimit;

                _logger.Information("Logging settings reset to defaults, previous state saved");
                _dialogService.ShowNotification(
                    "Скинуто до типових",
                    "Налаштування логування скинуто до типових значень",
                    NotificationPosition.TopCenter);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to toggle logging settings");
        }
    }

    private async Task SetApplicationSettingsToDefault()
    {
        try
        {
            if (_mementoCaretaker.ApplicationMemento != null)
            {
                var memento = _mementoCaretaker.ApplicationMemento;
                SelectedDefaultLanguage = memento.SelectedDefaultLanguage;
                PageSize = memento.PageSize;
                MaxExportRows = memento.MaxExportRows;

                _logger.Information("Application settings restored from memento");
                _dialogService.ShowNotification(
                    "Відновлено",
                    "Налаштування програми відновлено до попередніх значень",
                    NotificationPosition.TopCenter);

                _mementoCaretaker.ClearApplicationMemento();
            }
            else
            {
                var memento = new ApplicationSettingsMemento(
                    SelectedDefaultLanguage,
                    PageSize,
                    MaxExportRows
                );
                _mementoCaretaker.SaveApplicationState(memento);

                var defaults = _applicationSettingsService.ResetToDefaults();
                SelectedDefaultLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == defaults.DefaultLanguage) ??
                                          AvailableLanguages.First();
                PageSize = defaults.PageSize;
                MaxExportRows = defaults.MaxExportRows;

                _logger.Information("Application settings reset to defaults, previous state saved");
                _dialogService.ShowNotification(
                    "Скинуто до типових",
                    "Налаштування програми скинуто до типових значень",
                    NotificationPosition.TopCenter);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to toggle application settings");
        }
    }

    private void InitializeCommands()
    {
        ExitCommand = ReactiveCommand.CreateFromTask(Exit);
        TestConnectionCommand = ReactiveCommand.CreateFromTask(TestConnectionAsync);
        SaveCommand = ReactiveCommand.CreateFromTask<SettingsGroup>(ExecuteSave);
        DeleteCommand = ReactiveCommand.CreateFromTask<SettingsGroup>(ExecuteDelete);
        SetDefaultCommand = ReactiveCommand.CreateFromTask<SettingsGroup>(ExecuteToDefault);
        CancelCommand = ReactiveCommand.Create(Cancel);
        BrowseLogPathCommand = ReactiveCommand.CreateFromTask(BrowseLogPathAsync);
        ShowLoginWindowCommand = ReactiveCommand.CreateFromTask(ShowLogin, this.WhenAnyValue(x => x.CanContinue));


        ChangeLanguageCommand = ReactiveCommand.Create(() =>
        {
            var languageCode = !string.IsNullOrEmpty(SelectedDefaultLanguage.Code)
                ? SelectedDefaultLanguage.Code
                : SelectedDefaultLanguage.DisplayName;
            _localization.SetLanguage(languageCode);
        });

        ContinueCommand = ReactiveCommand.Create(Continue);
    }

    private void InitializeLanguages()
    {
        if (AvailableLanguages.Count > 2)
        {
            AvailableLanguages.Clear();
        }

        foreach (var l in _localization.AvailableLanguages)
        {
            AvailableLanguages.Add(new LanguageOption(l, l));
        }


        SelectedDefaultLanguage = new LanguageOption("", _localization.CurrentLanguage);
    }

    private void SetupReactiveBindings()
    {
        this.WhenAnyValue(x => x.UseAuthentication)
            .Subscribe(useAuth =>
            {
                if (_config is null) return;

                if (useAuth)
                {
                    Username = _encryptionService.Decrypt(_config.Username);
                    Password = _encryptionService.Decrypt(_config.Password);
                    return;
                }

                Username = string.Empty;
                Password = string.Empty;
            });

        this.WhenAnyValue(x => x.ShowPassword)
            .Subscribe(show => PasswordChar = show ? '\0' : '•');
    }


    public static async Task<bool> ShowSettingsWindowAsync()
    {
        Window? wnd;

        var tcs = new TaskCompletionSource<bool>();
        var wndService = App.GetService<IWindowService>();
        var settingsVm = App.GetService<SettingsViewModel>();

        var a = App.GetService<IWindowService>();
        var b = a.GetAll();
        var c = b.Count();

        var settingsWindow = App.GetService<IWindowService>().Get<SettingsWindow>();

        if (settingsWindow == null)
        {
            wndService.Register("PMS_SETTINGS", new SettingsWindow
            {
                DataContext = App.GetService<SettingsViewModel>()
            });

            wnd = wndService.Get<SettingsWindow>();

            if(wnd != null)
            {
                wnd.Closed += (sender, args) =>
                {
                    tcs.TrySetResult(settingsVm.CanContinue);
                };
            }
        }

        if (settingsWindow != null)
        {
            settingsWindow.Closed += (sender, args) =>
            {
                tcs.TrySetResult(settingsVm.CanContinue);
            };
        }

        wndService.Show<SettingsWindow>();

        return await tcs.Task;
    }

    #endregion

    #region Configuration Methods

    private void LoadSettings()
    {
        try
        {
            if (!_dbSettingsService.Exists())
            {
                ShowWarningBar($"Файл налаштувань бази даних відсутній. Використовуються значення за замовчуванням.");
                CreateDefaultSettings();
                return;
            }

            _config = _dbSettingsService.LoadSettings();
            var validationResult = _dbSettingsService.ValidateSettings(_config, out var error);


            if (validationResult == ValidationResult.Invalid)
            {
                ShowWarningBar($"Неповні або некоректні налаштування: {error}");
                Message = error;
            }

            if (validationResult == ValidationResult.Corrupted)
            {
                ShowErrorBar($"Файл налаштувань пошкодженою: {error}");
                Message = error;
                return;
            }

            LoadDatabaseSettings(_config);

            LoadLoggingSettings();

            LoadApplicationSettings();

            ShowSuccessBar("Конфігурація завантажена успішно");

        }
        catch (Exception ex)
        {
            ShowErrorBar($"Помилка завантаження конфігурації: {ex.Message}");
        }
    }

    private void LoadDatabaseSettings(DatabaseSettings config)
    {
        Host = config.Host;
        Port = config.Port;
        DatabaseName = config.DatabaseName;
        UseAuthentication = config.UseAuthentication;
        Username = config.Username;
        Password = config.Password;
        AuthDatabase = config.AuthDatabase;
        ConnectionTimeout = config.ConnectionTimeout;
    }

    private void LoadLoggingSettings()
    {
        _loggingSettings = new LoggingSettings();
        LogFilePath = _loggingSettings.FilePath;
        LogRollingInterval = _loggingSettings.RollingInterval;
        RetainedFileCountLimit = _loggingSettings.RetainedFileCountLimit;
    }

    private void LoadApplicationSettings()
    {
        _applicationSettings = new ApplicationSettings();
        PageSize = _applicationSettings.PageSize;
        MaxExportRows = _applicationSettings.MaxExportRows;

        var langOption = AvailableLanguages.FirstOrDefault(l => l.DisplayName == _applicationSettings.DefaultLanguage);
        if (langOption != null)
        {
            SelectedDefaultLanguage = langOption;
        }
    }

    private void CreateDefaultSettings()
    {
        LoadLoggingSettings();
        LoadApplicationSettings();
    }

    private DatabaseSettings BuildConfigFromUi()
    {
        return new DatabaseSettings
        {
            Host = Host,
            Port = Port,
            DatabaseName = DatabaseName,
            UseAuthentication = UseAuthentication,
            Username = Username,
            Password = Password,
            AuthDatabase = AuthDatabase,
            ConnectionTimeout = ConnectionTimeout,
        };
    }

    #endregion

    #region Command Handlers

    private async Task TestConnectionAsync()
    {
        try
        {
            CanContinue = false;
            ShowInfoBar("Початок тестування з'єднання...");
            _dialogService.ShowNotification(
                TestConnectionButtonLabel.Value,
                "Початок тестування з'єднання..",
                NotificationPosition.BottomRight,
                durationSeconds: 1
            );
            await Task.Delay(1000);
            var config = BuildConfigFromUi();
            if (_dbSettingsService.ValidateSettings(config, out string eMsg) != ValidationResult.Valid)
            {
                _dialogService.ShowNotification(
                    TestConnectionButtonLabel.Value,
                    eMsg,
                    NotificationPosition.TopCenter,
                    NotificationSeverity.Error,
                    TimeSpan.FromMilliseconds(-1).Seconds
                );
                ShowErrorBar($"Недійсні налаштування: {eMsg}");
                return;
            }

            var msg = $"Підключення до {config.Host}:{config.Port}/{config.DatabaseName}...";
            ShowInfoBar(msg);
            _logger.Information("Test conection: {Message}", msg);
            var success = await _dbSettingsService.TestConnectionAsync(config);

            if (success)
            {
                ShowSuccessBar("З'єднання з базою даних встановлено успішно.");
                await _dialogService.ShowSuccessAsync("Тест з'єднання пройшов успішно.");
                _credentialStateSubject.OnNext(true);
                CanContinue = true;
                _validatedValues = new ConnectionValues(Host, Port, DatabaseName, UseAuthentication, AuthDatabase);

                if (!_dbSettingsService.Exists())
                {
                    var saveConfirmed =
                        await _dialogService.ShowConfirmAsync("Налаштування", "Зберегти поточні налаштування ?");

                    if (saveConfirmed)
                    {
                        if (_dbSettingsService.SaveSettings(config))
                            await _dialogService.ShowSuccessAsync("Збережено!");
                    }
                }
                else
                {
                    var overrideConfirmed = await _dialogService.ShowCustomDialogAsync<bool>("Налаштування",
                        DialogService.CreateMessageContent(
                            "Перезаписати існуючі налаштування ?",
                            Symbol.Sync, Brushes.Orange),
                        "Так",
                        "Ні");

                    if (overrideConfirmed)
                    {
                        if (_dbSettingsService.SaveSettings(config))
                            await _dialogService.ShowSuccessAsync("Збережено!");
                    }
                }
            }
            else
            {
                ShowErrorBar(
                    "Не вдалося підключитися до бази даних.\nПеревірте актуальність і правильність налаштувань.");
                await _dialogService.ShowErrorAsync(TestConnectionButtonLabel.Value,
                    "Не вдалося підключитися до бази даних.\nПеревірте актуальність і правильність налаштувань.");
                CanContinue = false;
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            ShowErrorBar("Невідома помилка тестування з'єднання.");
            await _dialogService.ShowErrorAsync(TestConnectionButtonLabel.Value,
                "Не вдалося підключитися до бази даних.", ex);
            _logger.Error("Test conection error: {msg}", ex.Message);
            CanContinue = false;
            HasError = true;
        }
    }

    private async Task BrowseLogPathAsync()
    {
        try
        {
            var result = await _dialogService.ShowFolderDialogAsync(
                "Виберіть місце для файлів логування", _windowService.Get<SettingsWindow>(), "./Log");

            if (!string.IsNullOrEmpty(result))
            {
                LogFilePath = result;
            }
        }
        catch (Exception ex)
        {
            ShowErrorBar($"Помилка вибору файлу: {ex.Message}");
        }
    }

    private async void Continue()
    {
        try
        {
            var config = BuildConfigFromUi();
            _dbSettingsService.SaveSettings(config);
            await SaveLoggingSettings();
            await SaveApplicationSettings();
            _windowService.Hide<SettingsWindow>();
            await AuthViewModel.ShowAuthWindowAsync();
        }
        catch (Exception ex)
        {
            ShowErrorBar($"Помилка збереження конфігурації: {ex.Message}");
        }
    }

    private void Cancel()
    {
        _windowService.Close<SettingsWindow>();
    }

    private async Task Exit()
    {
        var decision =
            await _dialogService.ShowConfirmAsync(
                "Вихід", "Точно бажаєте вийти ?");
        if (decision) App.Exit();
    }

    private async Task ShowLogin()
    {
        try
        {
            var loginViewModel = App.GetService<LoginViewModel>();
            var settingsWnd = _windowService.Get<SettingsWindow>();

            if (settingsWnd == null)
            {
                return;
            }

            var loginWnd = _windowService.ShowDialog<LoginWindow>(settingsWnd, loginViewModel);

            loginViewModel.OnScopedLoginRequested = () =>
            {
                loginWnd.Hide();
                settingsWnd.Hide();
                _windowService.Show<MainWindow>(App.GetService<MainWindowViewModel>());
                return Task.CompletedTask;
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
    }

    #endregion
}