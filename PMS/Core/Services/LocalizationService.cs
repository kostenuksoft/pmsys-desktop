using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using PMS.Core.Services.Interfaces;
using Serilog;
using Serilog.Core;

namespace PMS.Core.Services;

public class LocalizationService : ILocalizationService
{
    private readonly string _languagesDirectory;
    private Dictionary<string, string> _currentTranslations;
    private string _currentLanguage;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<string>? LanguageChanged;

    private readonly List<string> _availableLanguages;
    private readonly IIniFileService _iniFile;


    private const string LanguagesDirectory = "Languages";

    public string CurrentLanguage
    {
        get => _currentLanguage;
        private set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                OnPropertyChanged(nameof(CurrentLanguage));
                LanguageChanged?.Invoke(this, value);
            }
        }
    }

    public IReadOnlyList<string> AvailableLanguages => _availableLanguages.AsReadOnly();

    public LocalizationService
    (
        string? defaultLanguage = null)
    {
        if (string.IsNullOrEmpty(defaultLanguage))
        {
            var culture = System.Globalization.CultureInfo.CurrentUICulture.Name;
            defaultLanguage = culture switch
            {
                "uk-UA" or "uk" => "uk-UA",
                _ => "en-US" 
            };
        }

        _currentLanguage = defaultLanguage;
        _languagesDirectory = LanguagesDirectory;
        _currentTranslations = new Dictionary<string, string>();

        _iniFile = new IniFileService(Path.Combine(LanguagesDirectory, $"{defaultLanguage}.lang"));

        CreateLangDir();
        CreateEnglishLangFile();
        CreateUkrainianLangFile();
        _availableLanguages = [];
        LoadAvailableLanguages();

        SetLanguage(defaultLanguage);
    }

    private void CreateLangDir()
    {
        if (!Directory.Exists(_languagesDirectory))
        {
            Directory.CreateDirectory(_languagesDirectory);
        }
    }

    private void CreateEnglishLangFile()
    {
        _iniFile.SetFilePath(Path.Combine(LanguagesDirectory, "en-US.lang"));

        _iniFile.WriteValue("IRWindow", "title", "Application already running");
        _iniFile.WriteValue("IRWindow", "message", "An instance of this application is already running.");
        _iniFile.WriteValue("IRWindow", "hint", "Please check the taskbar or system tray.");
        _iniFile.WriteValue("IRWindow", "okbutton", "OK");

        _iniFile.WriteValue("App", "language", "Language");
        _iniFile.WriteValue("App", "title", "PMSys");
        _iniFile.WriteValue("App", "version", "Version 1.0.0a");

        _iniFile.WriteValue("Config", "description", "A series of settings regarding the database, records, and application");
        _iniFile.WriteValue("Config", "title", "Settings");

        _iniFile.WriteValue("Config.Application", "exportrows.label", "Max rows for export");
        _iniFile.WriteValue("Config.Application", "language.label", "Default language");
        _iniFile.WriteValue("Config.Application", "pagesize.label", "Page size");

        _iniFile.WriteValue("Config.Application.Data", "description", "Data display and export settings");
        _iniFile.WriteValue("Config.Application.Data", "title", "Data Management");

        _iniFile.WriteValue("Config.Application.Localization", "description", "Interface language and regional settings");
        _iniFile.WriteValue("Config.Application.Localization", "title", "Localization");

        _iniFile.WriteValue("Config.Application.Section", "description", "General application operation and user interface parameters");
        _iniFile.WriteValue("Config.Application.Section", "title", "Application Settings");

        _iniFile.WriteValue("Config.Button", "continue", "Continue");
        _iniFile.WriteValue("Config.Button", "exit", "Exit");
        _iniFile.WriteValue("Config.Button", "testconnection", "Test Connection");
        _iniFile.WriteValue("Config.Button", "testmongo", "Test MongoDB");

        _iniFile.WriteValue("Config.Common", "files", "files");
        _iniFile.WriteValue("Config.Common", "records", "records");
        _iniFile.WriteValue("Config.Common", "seconds", "seconds");

        _iniFile.WriteValue("Config.Database", "authdb.label", "Authentication Database");
        _iniFile.WriteValue("Config.Database", "host.label", "Server Address");
        _iniFile.WriteValue("Config.Database", "name.label", "Database Name");
        _iniFile.WriteValue("Config.Database", "password.label", "Password");
        _iniFile.WriteValue("Config.Database", "password.placeholder", "Enter password");
        _iniFile.WriteValue("Config.Database", "port.label", "Port");
        _iniFile.WriteValue("Config.Database", "timeout.label", "Connection Timeout");
        _iniFile.WriteValue("Config.Database", "username.label", "User");
        _iniFile.WriteValue("Config.Database", "username.placeholder", "MongoDB Username");

        _iniFile.WriteValue("Config.Database.Auth", "description", "Authentication settings for database access");
        _iniFile.WriteValue("Config.Database.Auth", "title", "Authentication");

        _iniFile.WriteValue("Config.Database.Section", "description", "MongoDB connection settings and parameters");
        _iniFile.WriteValue("Config.Database.Section", "title", "Database Configuration");

        _iniFile.WriteValue("Config.Database.Server", "description", "Basic database server connection settings");
        _iniFile.WriteValue("Config.Database.Server", "title", "Server Parameters");

        _iniFile.WriteValue("Config.Logging", "filepath.label", "Log file path");
        _iniFile.WriteValue("Config.Logging", "interval.hour", "Hourly");
        _iniFile.WriteValue("Config.Logging", "interval.day", "Daily");
        _iniFile.WriteValue("Config.Logging", "interval.label", "Rotation Interval");
        _iniFile.WriteValue("Config.Logging", "interval.month", "Monthly");
        _iniFile.WriteValue("Config.Logging", "interval.week", "Weekly");
        _iniFile.WriteValue("Config.Logging", "retained.label", "Files to retain");

        _iniFile.WriteValue("Config.Logging.File", "description", "Log file location and rotation settings");
        _iniFile.WriteValue("Config.Logging.File", "title", "Log Files");

        _iniFile.WriteValue("Config.Logging.Section", "description", "Logging system configuration and file retention");
        _iniFile.WriteValue("Config.Logging.Section", "title", "Logging Settings");

        _iniFile.WriteValue("Config.Window", "title", "PMSys - Settings");

        _iniFile.WriteValue("Config.Auth", "title", "Authorization");
        _iniFile.WriteValue("Config.Auth", "description", "You can log in to the system here.");
        _iniFile.WriteValue("Config.Auth", "login", "Log In");

        _iniFile.WriteValue("Config.Other", "title", "Other Settings");

        _iniFile.WriteValue("Login.Window", "title", "System Login - Polyclinic");
        _iniFile.WriteValue("Login", "header", "Log In");
        _iniFile.WriteValue("Login", "subtitle", "Enter your credentials");
        _iniFile.WriteValue("Login.Username", "watermark", "Enter username or email");
        _iniFile.WriteValue("Login.Password", "watermark", "Enter password");
        _iniFile.WriteValue("Login.Rememberme", "tooltip", "Remember me");
        _iniFile.WriteValue("Login.Showpassword", "tooltip", "Show password");
        _iniFile.WriteValue("Login.Forgotpassword", "link", "Forgot password?");
        _iniFile.WriteValue("Login.Noaccount", "link", "Login as guest");
        _iniFile.WriteValue("Login.Noaccount", "text", "Don't have an account yet?");
        _iniFile.WriteValue("Login.Registration", "link", "Registration");

        _iniFile.WriteValue("Login.Button", "back", "Back");
        _iniFile.WriteValue("Login.Button", "login", "Log In");
        _iniFile.WriteValue("Login.Button", "changepassword", "Change");

        _iniFile.WriteValue("Login.Message", "authenticating", "Authenticating...");
        _iniFile.WriteValue("Login.Message", "credentialssaved", "Credentials saved!");
        _iniFile.WriteValue("Login.Message", "credentialssaveerror", "Failed to save authentication data");
        _iniFile.WriteValue("Login.Message", "passwordchangerequired", "Password change required on first login");
        _iniFile.WriteValue("Login.Message", "userdataerror", "Error retrieving user data");
        _iniFile.WriteValue("Login.Message", "connectionerror", "An unknown error occurred while connecting");
        _iniFile.WriteValue("Login.Message", "enterusernameforpasswordreset", "Enter username for password reset");
        _iniFile.WriteValue("Login.Message", "searchingaccount", "Searching for account...");
        _iniFile.WriteValue("Login.Message", "accountnotfound", "This account does not exist in the database");
        _iniFile.WriteValue("Login.Message", "usernotindatabase", "User not found in database");
        _iniFile.WriteValue("Login.Message", "adminpasswordchangeblocked", "Changing administrator passwords is forbidden.\nThis incident will be reported.");
        _iniFile.WriteValue("Login.Message", "adminpasswordchangenono", "I said no, you can't.");
        _iniFile.WriteValue("Login.Message", "accountlocked", "Account locked. Please contact the administrator");
        _iniFile.WriteValue("Login.Message", "passwordsmismatch", "Passwords do not match");
        _iniFile.WriteValue("Login.Message", "savedpasswordexpiry", "Saved password will be removed in {0} days");

        _iniFile.WriteValue("Login.Dialog.Overwritecredentials", "title", "Overwrite");
        _iniFile.WriteValue("Login.Dialog.Overwritecredentials", "message", "Overwrite existing credentials?");
        _iniFile.WriteValue("Login.Dialog.Passwordindatabase", "title", "Password in database");

        _iniFile.WriteValue("Login.Changepassword", "title", "Change Password");
        _iniFile.WriteValue("Login.Changepassword", "header", "Change Password:");
        _iniFile.WriteValue("Login.Changepassword.Currentpassword", "placeholder", "Current password");
        _iniFile.WriteValue("Login.Changepassword.Newpassword", "placeholder", "New password");
        _iniFile.WriteValue("Login.Changepassword.Confirmpassword", "placeholder", "Confirm new password");
        _iniFile.WriteValue("Login.Changepassword.Button", "primary", "Change");
        _iniFile.WriteValue("Login.Changepassword.Button", "secondary", "Cancel");

        _iniFile.WriteValue("Login.Guest", "username", "Guest");
        _iniFile.WriteValue("Login.Guest", "email", "guest@polyclinic.local");

        _iniFile.WriteValue("Login.Validation.Email", "required", "This field is required");
        _iniFile.WriteValue("Login.Validation.Email", "format", "Please enter a valid email format (e.g., user@example.com)");

        _iniFile.WriteValue("Auth.Registration.Window", "title", "Registration - Polyclinic");
        _iniFile.WriteValue("Auth.Registration", "header", "New User Registration");
        _iniFile.WriteValue("Auth.Registration", "subtitle", "Fill out the form to submit a registration request");

        _iniFile.WriteValue("Auth.Registration.Section", "required", "Required Fields");
        _iniFile.WriteValue("Auth.Registration.Section", "additional", "Additional Information");
        _iniFile.WriteValue("Auth.Registration.Section", "purpose", "Registration Purpose");

        _iniFile.WriteValue("Auth.Registration.Login", "tag", "Login *");
        _iniFile.WriteValue("Auth.Registration.Login", "label", "Login");
        _iniFile.WriteValue("Auth.Registration.Login", "watermark", "Enter unique login (min. 3 chars)");

        _iniFile.WriteValue("Auth.Registration.Email", "tag", "Email *");
        _iniFile.WriteValue("Auth.Registration.Email", "label", "Email");
        _iniFile.WriteValue("Auth.Registration.Email", "watermark", "Enter your email");

        _iniFile.WriteValue("Auth.Registration.Fullname", "tag", "Full Name");
        _iniFile.WriteValue("Auth.Registration.Fullname", "label", "Full Name");
        _iniFile.WriteValue("Auth.Registration.Fullname", "watermark", "Enter your full name");

        _iniFile.WriteValue("Auth.Registration.Password", "tag", "Password *");
        _iniFile.WriteValue("Auth.Registration.Password", "label", "Password");
        _iniFile.WriteValue("Auth.Registration.Password", "watermark", "Enter password (min. 8 chars)");

        _iniFile.WriteValue("Auth.Registration.Confirmpassword", "tag", "Confirm Password *");
        _iniFile.WriteValue("Auth.Registration.Confirmpassword", "label", "Password Confirmation");
        _iniFile.WriteValue("Auth.Registration.Confirmpassword", "watermark", "Repeat password");

        _iniFile.WriteValue("Auth.Registration.Mobilephone", "tag", "Mobile Phone +380 XX XXX XX XX");
        _iniFile.WriteValue("Auth.Registration.Mobilephone", "label", "Mobile Phone");
        _iniFile.WriteValue("Auth.Registration.Mobilephone", "watermark", "+380 (XX) XXX-XX-XX");

        _iniFile.WriteValue("Auth.Registration.Organization", "tag", "Organization Name");
        _iniFile.WriteValue("Auth.Registration.Organization", "label", "Organization");
        _iniFile.WriteValue("Auth.Registration.Organization", "watermark", "Organization name (optional)");

        _iniFile.WriteValue("Auth.Registration.Position", "tag", "Position Held");
        _iniFile.WriteValue("Auth.Registration.Position", "label", "Position");
        _iniFile.WriteValue("Auth.Registration.Position", "watermark", "Your position (optional)");

        _iniFile.WriteValue("Auth.Registration.Message", "tag", "Describe the purpose of registration");
        _iniFile.WriteValue("Auth.Registration.Message", "label", "Message");
        _iniFile.WriteValue("Auth.Registration.Message", "watermark", "Why do you want access to the system?");
        _iniFile.WriteValue("Auth.Registration.Message", "hint", "Explain for what purposes you need access to the system");

        _iniFile.WriteValue("Auth.Registration.Button", "submit", "Submit");
        _iniFile.WriteValue("Auth.Registration.Button", "clear", "Clear");
        _iniFile.WriteValue("Auth.Registration.Button", "back", "Back to Login");

        _iniFile.WriteValue("Auth.Registration.Footer", "checkdata", "Check data before submitting");
        _iniFile.WriteValue("Auth.Registration.Footer", "review", "Your request will be reviewed shortly");

        _iniFile.WriteValue("Auth.Registration.Loading", "text", "Submitting request...");

        _iniFile.WriteValue("Auth.Registration.Validation", "login", "Login must contain at least 3 characters");
        _iniFile.WriteValue("Auth.Registration.Validation", "email", "Enter a valid email address");
        _iniFile.WriteValue("Auth.Registration.Validation", "fullname", "Name must contain at least 3 characters");
        _iniFile.WriteValue("Auth.Registration.Validation", "password", "Password must contain at least 8 characters");
        _iniFile.WriteValue("Auth.Registration.Validation", "passwordmatch", "Passwords do not match");
        _iniFile.WriteValue("Auth.Registration.Validation", "message", "Message must contain at least 10 characters");

        _iniFile.WriteValue("Auth.Registration.Success", "title", "Request Sent!");
        _iniFile.WriteValue("Auth.Registration.Success", "message", "Your registration request has been successfully sent!\n\nRequest Code: {0}\n\nSave this code to check your request status.\nAn administrator will review your request shortly.");

        _iniFile.WriteValue("Auth.Registration.Error", "title", "Registration Error");
        _iniFile.WriteValue("Auth.Registration.Error", "message", "Failed to send registration request. Please try again later.");
        _iniFile.WriteValue("Auth.Registration.Error", "loginexistsinusers", "A user with this login is already registered in the system. Choose another login.");
        _iniFile.WriteValue("Auth.Registration.Error", "loginexistsinrequests", "A registration request with this login already exists. Choose another login.");
        _iniFile.WriteValue("Auth.Registration.Error", "emailexistsinusers", "A user with this email is already registered in the system.");
        _iniFile.WriteValue("Auth.Registration.Error", "emailexistsinrequests", "A registration request with this email already exists. Check your request status.");
        _iniFile.WriteValue("Auth.Registration.Error", "requestpending", "You already have an active registration request (status: pending review).\n\nRequest Code: {0}\n\nWait for review or contact an administrator.");
        _iniFile.WriteValue("Auth.Registration.Error", "requestapproved", "Your registration request has already been approved!\n\nRequest Code: {0}\n\nYou can now log in using your email and password.");
        _iniFile.WriteValue("Auth.Registration.Error", "requestrejected", "Your previous registration request was rejected.\n\nRequest Code: {0}\n\nYou can create a new request or contact an administrator.");

        _iniFile.WriteValue("Auth.Registration.Message", "submitting", "Sending request...");
        _iniFile.WriteValue("Auth.Registration.Message", "checking", "Checking user existence...");
        _iniFile.WriteValue("Auth.Registration.Message", "savecode", "Save code to file");
        _iniFile.WriteValue("Auth.Registration.Message", "codesaved", "Code successfully saved to file!");

        _iniFile.Save();
    }

    private void CreateUkrainianLangFile()
    {
        _iniFile.SetFilePath(Path.Combine(LanguagesDirectory, "uk-UA.lang"));

        _iniFile.WriteValue("IRWindow", "title", "Додаток вже запущено");
        _iniFile.WriteValue("IRWindow", "message", "Екземпляр цього додатку вже запущено.");
        _iniFile.WriteValue("IRWindow", "hint", "Будь ласка, перевірте панель завдань або системний трей.");
        _iniFile.WriteValue("IRWindow", "okbutton", "Гаразд");

        _iniFile.WriteValue("App", "language", "Мова");

        _iniFile.WriteValue("Config", "description", "Низка налаштувань стосовно бази даних, записів та застосунку");
        _iniFile.WriteValue("Config", "title", "Налаштування");

        _iniFile.WriteValue("Config.Application", "exportrows.label", "Максимум рядків для експорту");
        _iniFile.WriteValue("Config.Application", "language.label", "Мова за замовчуванням");
        _iniFile.WriteValue("Config.Application", "pagesize.label", "Розмір сторінки");

        _iniFile.WriteValue("Config.Application.Data", "description", "Налаштування відображення та експорту даних");
        _iniFile.WriteValue("Config.Application.Data", "title", "Управління даними");

        _iniFile.WriteValue("Config.Application.Localization", "description", "Налаштування мови інтерфейсу та регіональних параметрів");
        _iniFile.WriteValue("Config.Application.Localization", "title", "Локалізація");

        _iniFile.WriteValue("Config.Application.Section", "description", "Загальні параметри роботи додатку та інтерфейсу користувача");
        _iniFile.WriteValue("Config.Application.Section", "title", "Налаштування додатку");

        _iniFile.WriteValue("Config.Button", "continue", "Продовжити");
        _iniFile.WriteValue("Config.Button", "exit", "Вихід");
        _iniFile.WriteValue("Config.Button", "testconnection", "Тест з'єднання");
        _iniFile.WriteValue("Config.Button", "testmongo", "Тест MongoDB");

        _iniFile.WriteValue("Config.Common", "files", "файлів");
        _iniFile.WriteValue("Config.Common", "records", "записів");
        _iniFile.WriteValue("Config.Common", "seconds", "секунд");

        _iniFile.WriteValue("Config.Database", "authdb.label", "База автентифікації");
        _iniFile.WriteValue("Config.Database", "host.label", "Адреса сервера");
        _iniFile.WriteValue("Config.Database", "name.label", "Назва бази даних");
        _iniFile.WriteValue("Config.Database", "password.label", "Пароль");
        _iniFile.WriteValue("Config.Database", "password.placeholder", "Введіть пароль");
        _iniFile.WriteValue("Config.Database", "port.label", "Порт");
        _iniFile.WriteValue("Config.Database", "timeout.label", "Таймаут з'єднання");
        _iniFile.WriteValue("Config.Database", "username.label", "Користувач");
        _iniFile.WriteValue("Config.Database", "username.placeholder", "Ім'я користувача MongoDB");

        _iniFile.WriteValue("Config.Database.Auth", "description", "Налаштування автентифікації для доступу до бази даних");
        _iniFile.WriteValue("Config.Database.Auth", "title", "Автентифікація");

        _iniFile.WriteValue("Config.Database.Section", "description", "Налаштування підключення до MongoDB та параметри з'єднання");
        _iniFile.WriteValue("Config.Database.Section", "title", "Конфігурація бази даних");

        _iniFile.WriteValue("Config.Database.Server", "description", "Основні налаштування підключення до сервера бази даних");
        _iniFile.WriteValue("Config.Database.Server", "title", "Параметри сервера");

        _iniFile.WriteValue("Config.Logging", "filepath.label", "Шлях до файлу журналу");
        _iniFile.WriteValue("Config.Logging", "interval.hour", "Щогодини");
        _iniFile.WriteValue("Config.Logging", "interval.day", "Щодня");
        _iniFile.WriteValue("Config.Logging", "interval.label", "Інтервал ротації");
        _iniFile.WriteValue("Config.Logging", "interval.month", "Щомісяця");
        _iniFile.WriteValue("Config.Logging", "interval.week", "Щотижня");
        _iniFile.WriteValue("Config.Logging", "retained.label", "К-сть файлів для збереження");

        _iniFile.WriteValue("Config.Logging.File", "description", "Налаштування розташування та ротації файлів журналу");
        _iniFile.WriteValue("Config.Logging.File", "title", "Файли журналу");

        _iniFile.WriteValue("Config.Logging.Section", "description", "Конфігурація системи логування та збереження файлів журналу");
        _iniFile.WriteValue("Config.Logging.Section", "title", "Налаштування логування");

        _iniFile.WriteValue("Config.Window", "title", "PMSys - Налаштування");

        _iniFile.WriteValue("Config.Auth", "title", "Авторизація");
        _iniFile.WriteValue("Config.Auth", "description", "Можете увійти у систему тут.");
        _iniFile.WriteValue("Config.Auth", "login", "Увійти");

        _iniFile.WriteValue("Config.Other", "title", "Інші налаштування");

        _iniFile.WriteValue("Login.Window", "title", "Вхід до системи - Поліклініка");
        _iniFile.WriteValue("Login", "header", "Увійти");
        _iniFile.WriteValue("Login", "subtitle", "Введіть свої облікові дані");
        _iniFile.WriteValue("Login.Username", "watermark", "Введіть логін або електронну пошту");
        _iniFile.WriteValue("Login.Password", "watermark", "Введіть пароль");
        _iniFile.WriteValue("Login.Rememberme", "tooltip", "Запам'ятати мене");
        _iniFile.WriteValue("Login.Showpassword", "tooltip", "Показати пароль");
        _iniFile.WriteValue("Login.Forgotpassword", "link", "Забули пароль?");
        _iniFile.WriteValue("Login.Noaccount", "link", "Увійти як гість");
        _iniFile.WriteValue("Login.Noaccount", "text", "Ще немає облікового запису?");
        _iniFile.WriteValue("Login.Registration", "link", "Реєстрація");

        _iniFile.WriteValue("Login.Button", "back", "Назад");
        _iniFile.WriteValue("Login.Button", "login", "Увійти");
        _iniFile.WriteValue("Login.Button", "changepassword", "Змінити");

        _iniFile.WriteValue("Login.Message", "authenticating", "Автентифікація...");
        _iniFile.WriteValue("Login.Message", "credentialssaved", "Авторизаційні дані збережено!");
        _iniFile.WriteValue("Login.Message", "credentialssaveerror", "Не вдалося зберегти автентифікаційні дані");
        _iniFile.WriteValue("Login.Message", "passwordchangerequired", "Необхідно змінити пароль при першому вході");
        _iniFile.WriteValue("Login.Message", "userdataerror", "Помилка отримання даних користувача");
        _iniFile.WriteValue("Login.Message", "connectionerror", "Виникла невідома помилка при підключенні");
        _iniFile.WriteValue("Login.Message", "enterusernameforpasswordreset", "Введіть логін для відновлення паролю");
        _iniFile.WriteValue("Login.Message", "searchingaccount", "Пошук облікового запису...");
        _iniFile.WriteValue("Login.Message", "accountnotfound", "Такого облікового запису немає в базі");
        _iniFile.WriteValue("Login.Message", "usernotindatabase", "Користувач відсутній у базі");
        _iniFile.WriteValue("Login.Message", "adminpasswordchangeblocked", "Забороняється змінювати пароль адміністраторам системи.\nВипадок буде зафіксовано.");
        _iniFile.WriteValue("Login.Message", "adminpasswordchangenono", "Сказав ж, не можна.");
        _iniFile.WriteValue("Login.Message", "accountlocked", "Аккаунт заблоковано. Зверніться до адміністратора");
        _iniFile.WriteValue("Login.Message", "passwordsmismatch", "Паролі не співпадають");
        _iniFile.WriteValue("Login.Message", "savedpasswordexpiry", "Збережений пароль буде видалено через {0} днів");

        _iniFile.WriteValue("Login.Dialog.Overwritecredentials", "title", "Перезапис");
        _iniFile.WriteValue("Login.Dialog.Overwritecredentials", "message", "Перезаписати існуючі авторизаційні дані?");
        _iniFile.WriteValue("Login.Dialog.Passwordindatabase", "title", "Пароль у базі");

        _iniFile.WriteValue("Login.Changepassword", "title", "Зміна пароля");
        _iniFile.WriteValue("Login.Changepassword", "header", "Зміна пароля:");
        _iniFile.WriteValue("Login.Changepassword.Currentpassword", "placeholder", "Поточний пароль");
        _iniFile.WriteValue("Login.Changepassword.Newpassword", "placeholder", "Новий пароль");
        _iniFile.WriteValue("Login.Changepassword.Confirmpassword", "placeholder", "Підтвердити новий пароль");
        _iniFile.WriteValue("Login.Changepassword.Button", "primary", "Змінити");
        _iniFile.WriteValue("Login.Changepassword.Button", "secondary", "Скасувати");

        _iniFile.WriteValue("Login.Guest", "username", "Гість");
        _iniFile.WriteValue("Login.Guest", "email", "guest@polyclinic.local");

        _iniFile.WriteValue("Login.Validation.Email", "required", "Поле обов'язкове для заповнення");
        _iniFile.WriteValue("Login.Validation.Email", "format", "Будь ласка, введіть коректний формат електронної пошти (наприклад, user@example.com)");
        
        _iniFile.WriteValue("Auth.Registration.Window", "title", "Реєстрація - Поліклініка");
        _iniFile.WriteValue("Auth.Registration", "header", "Реєстрація нового користувача");
        _iniFile.WriteValue("Auth.Registration", "subtitle", "Заповніть форму для подання запиту на реєстрацію");

        _iniFile.WriteValue("Auth.Registration.Section", "required", "Обов'язкові поля");
        _iniFile.WriteValue("Auth.Registration.Section", "additional", "Додаткова інформація");
        _iniFile.WriteValue("Auth.Registration.Section", "purpose", "Мета реєстрації");

        _iniFile.WriteValue("Auth.Registration.Login", "tag", "Логін *");
        _iniFile.WriteValue("Auth.Registration.Login", "label", "Логін");
        _iniFile.WriteValue("Auth.Registration.Login", "watermark", "Введіть унікальний логін (мін. 3 символи)");

        _iniFile.WriteValue("Auth.Registration.Email", "tag", "Електронна пошта *");
        _iniFile.WriteValue("Auth.Registration.Email", "label", "Електронна пошта");
        _iniFile.WriteValue("Auth.Registration.Email", "watermark", "Введіть вашу електронну пошту");

        _iniFile.WriteValue("Auth.Registration.Fullname", "tag", "Прізвище Ім'я По-батькові");
        _iniFile.WriteValue("Auth.Registration.Fullname", "label", "Повне ім'я");
        _iniFile.WriteValue("Auth.Registration.Fullname", "watermark", "Введіть ваше повне ім'я");

        _iniFile.WriteValue("Auth.Registration.Password", "tag", "Пароль *");
        _iniFile.WriteValue("Auth.Registration.Password", "label", "Пароль");
        _iniFile.WriteValue("Auth.Registration.Password", "watermark", "Введіть пароль (мінімум 8 символів)");

        _iniFile.WriteValue("Auth.Registration.Confirmpassword", "tag", "Підтвердіть пароль *");
        _iniFile.WriteValue("Auth.Registration.Confirmpassword", "label", "Підтвердження паролю");
        _iniFile.WriteValue("Auth.Registration.Confirmpassword", "watermark", "Повторіть пароль");

        _iniFile.WriteValue("Auth.Registration.Mobilephone", "tag", "Мобільний телефон +380 XX XXX XX XX");
        _iniFile.WriteValue("Auth.Registration.Mobilephone", "label", "Мобільний телефон");
        _iniFile.WriteValue("Auth.Registration.Mobilephone", "watermark", "+380 (XX) XXX-XX-XX");

        _iniFile.WriteValue("Auth.Registration.Organization", "tag", "Назва організації");
        _iniFile.WriteValue("Auth.Registration.Organization", "label", "Організація");
        _iniFile.WriteValue("Auth.Registration.Organization", "watermark", "Назва організації (опціонально)");

        _iniFile.WriteValue("Auth.Registration.Position", "tag", "Посада, яку займаєте");
        _iniFile.WriteValue("Auth.Registration.Position", "label", "Посада");
        _iniFile.WriteValue("Auth.Registration.Position", "watermark", "Ваша посада (опціонально)");

        _iniFile.WriteValue("Auth.Registration.Message", "tag", "Опишіть мету реєстрації");
        _iniFile.WriteValue("Auth.Registration.Message", "label", "Повідомлення");
        _iniFile.WriteValue("Auth.Registration.Message", "watermark", "Чому ви хочете отримати доступ до системи?");
        _iniFile.WriteValue("Auth.Registration.Message", "hint", "Поясніть для яких цілей вам потрібен доступ до системи");

        _iniFile.WriteValue("Auth.Registration.Button", "submit", "Надіслати");
        _iniFile.WriteValue("Auth.Registration.Button", "clear", "Очистити");
        _iniFile.WriteValue("Auth.Registration.Button", "back", "Назад до входу");

        _iniFile.WriteValue("Auth.Registration.Footer", "checkdata", "Перевірте дані перед надсиланням");
        _iniFile.WriteValue("Auth.Registration.Footer", "review", "Ваш запит буде розглянуто найближчим часом");

        _iniFile.WriteValue("Auth.Registration.Loading", "text", "Надсилання запиту...");

        _iniFile.WriteValue("Auth.Registration.Validation", "login", "Логін має містити мінімум 3 символи");
        _iniFile.WriteValue("Auth.Registration.Validation", "email", "Введіть коректну електронну адресу");
        _iniFile.WriteValue("Auth.Registration.Validation", "fullname", "Ім'я має містити мінімум 3 символи");
        _iniFile.WriteValue("Auth.Registration.Validation", "password", "Пароль має містити мінімум 8 символів");
        _iniFile.WriteValue("Auth.Registration.Validation", "passwordmatch", "Паролі не співпадають");
        _iniFile.WriteValue("Auth.Registration.Validation", "message", "Повідомлення має містити мінімум 10 символів");

        _iniFile.WriteValue("Auth.Registration.Success", "title", "Запит відправлено!");
        _iniFile.WriteValue("Auth.Registration.Success", "message", "Ваш запит на реєстрацію успішно відправлено!\n\nКод запиту: {0}\n\nЗбережіть цей код для перевірки статусу вашого запиту.\nАдміністратор розгляне ваш запит найближчим часом.");

        _iniFile.WriteValue("Auth.Registration.Error", "title", "Помилка реєстрації");
        _iniFile.WriteValue("Auth.Registration.Error", "message", "Не вдалося відправити запит на реєстрацію. Спробуйте пізніше.");
        _iniFile.WriteValue("Auth.Registration.Error", "loginexistsinusers", "Користувач з таким логіном вже зареєстрований у системі. Оберіть інший логін.");
        _iniFile.WriteValue("Auth.Registration.Error", "loginexistsinrequests", "Запит на реєстрацію з цим логіном вже існує. Оберіть інший логін.");
        _iniFile.WriteValue("Auth.Registration.Error", "emailexistsinusers", "Користувач з такою електронною поштою вже зареєстрований у системі.");
        _iniFile.WriteValue("Auth.Registration.Error", "emailexistsinrequests", "Запит на реєстрацію з цією електронною поштою вже існує. Перевірте статус вашого запиту.");
        _iniFile.WriteValue("Auth.Registration.Error", "requestpending", "У вас вже є активний запит на реєстрацію (статус: очікування розгляду).\n\nКод запиту: {0}\n\nДочекайтесь розгляду або зверніться до адміністратора.");
        _iniFile.WriteValue("Auth.Registration.Error", "requestapproved", "Ваш запит на реєстрацію вже схвалено!\n\nКод запиту: {0}\n\nТепер ви можете увійти в систему, використовуючи вашу електронну пошту та пароль.");
        _iniFile.WriteValue("Auth.Registration.Error", "requestrejected", "Ваш попередній запит на реєстрацію було відхилено.\n\nКод запиту: {0}\n\nВи можете створити новий запит або зв'язатися з адміністратором.");

        _iniFile.WriteValue("Auth.Registration.Message", "submitting", "Відправка запиту...");
        _iniFile.WriteValue("Auth.Registration.Message", "checking", "Перевірка наявності користувача...");
        _iniFile.WriteValue("Auth.Registration.Message", "savecode", "Зберегти код у файл");
        _iniFile.WriteValue("Auth.Registration.Message", "codesaved", "Код успішно збережено у файл!");

        _iniFile.Save();
    }

    private void LoadAvailableLanguages()
    {
        if (AvailableLanguages.Count > 1) _availableLanguages.Clear();

        if (!Directory.Exists(_languagesDirectory))
        {
            return;
        }

        var langFiles = Directory.GetFiles(_languagesDirectory, "*.lang");
        foreach (var file in langFiles)
        {
            var langName = Path.GetFileNameWithoutExtension(file);
            _availableLanguages.Add(langName);
        }
    }

    public bool SetLanguage(string language)
    {
        if (string.IsNullOrEmpty(language) || !_availableLanguages.Contains(language)) return false;

        _iniFile.SetFilePath(Path.Combine(LanguagesDirectory, $"{language}.lang"));

        var translations = LoadLanguageFile(language);
        if (translations != null)
        {
            _currentTranslations = translations;
            CurrentLanguage = language;
            return true;
        }

        return false;
    }

    public string GetString(string key)
    {
        return _currentTranslations.TryGetValue(key, out var value) ? value : $"[{key}]";
    }

    public string GetString(string key, params object[] args)
    {
        var format = GetString(key);
        try
        {
            return string.Format(format, args);
        }
        catch
        {
            return format;
        }
    }

    private Dictionary<string, string>? LoadLanguageFile(string language)
    {
        _iniFile.SetFilePath(Path.Combine(LanguagesDirectory, $"{language}.lang"));

        if (!_iniFile.Exists())
        {
            return null;
        }

        _iniFile.Load();

        try
        {
            var translations = new Dictionary<string, string?>();
            var allSections = _iniFile.ReadAllSections();

            foreach (var section in allSections)
            {
                foreach (var kvp in section.Value)
                {
                    var key = $"{section.Key.ToLower()}.{kvp.Key.ToLower()}";
                    translations[key] = kvp.Value;
                }
            }

            return translations;
        }
        catch (Exception ex)
        {
            Log.Error("Error loading language file {Language}: {ExMessage}", language, ex.Message);
            return null;
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}