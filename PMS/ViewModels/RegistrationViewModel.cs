using System;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using PMS.Core.Database;
using PMS.Core.Enums.General;
using PMS.Core.Models;
using PMS.Core.Models.Common;
using PMS.Core.Repositories.Interfaces;
using PMS.Core.Services;
using PMS.Core.Services.Interfaces;
using PMS.Views.Window;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using FileDialogFilter = PMS.Core.Models.Common.FileDialogFilter;
using ILoggerService = Serilog.ILogger;

namespace PMS.ViewModels;

public class RegistrationViewModel : BaseViewModel
{
    private readonly IGuestRequestRepository _guestRequestRepository;
    private readonly ISecurityService _securityService;
    private readonly IDialogService _dialogService;
    private readonly ILocalizationService _localization;
    private readonly IDatabaseContext _databaseContext;
    private readonly ILoggerService _loggerService;

    private string _login = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _fullName = string.Empty;
    private string? _mobilePhone;
    private string? _organization;
    private string? _position;
    private string _message = string.Empty;
    private bool _isSubmitting;

    #region Localized Strings

    public LocalizedString WindowTitle { get; private set; }
    public LocalizedString Header { get; private set; }
    public LocalizedString Subtitle { get; private set; }

    public LocalizedString SectionRequired { get; private set; }
    public LocalizedString SectionAdditional { get; private set; }
    public LocalizedString SectionPurpose { get; private set; }

    public LocalizedString LoginTag { get; private set; }
    public LocalizedString LoginLabel { get; private set; }
    public LocalizedString LoginWatermark { get; private set; }
    public LocalizedString EmailTag { get; private set; }
    public LocalizedString EmailLabel { get; private set; }
    public LocalizedString EmailWatermark { get; private set; }
    public LocalizedString FullNameTag { get; private set; }
    public LocalizedString FullNameLabel { get; private set; }
    public LocalizedString FullNameWatermark { get; private set; }
    public LocalizedString PasswordTag { get; private set; }
    public LocalizedString PasswordLabel { get; private set; }
    public LocalizedString PasswordWatermark { get; private set; }
    public LocalizedString ConfirmPasswordTag { get; private set; }
    public LocalizedString ConfirmPasswordLabel { get; private set; }
    public LocalizedString ConfirmPasswordWatermark { get; private set; }
    public LocalizedString MobilePhoneTag { get; private set; }
    public LocalizedString MobilePhoneLabel { get; private set; }
    public LocalizedString MobilePhoneWatermark { get; private set; }
    public LocalizedString OrganizationTag { get; private set; }
    public LocalizedString OrganizationLabel { get; private set; }
    public LocalizedString OrganizationWatermark { get; private set; }
    public LocalizedString PositionTag { get; private set; }
    public LocalizedString PositionLabel { get; private set; }
    public LocalizedString PositionWatermark { get; private set; }
    public LocalizedString MessageTag { get; private set; }
    public LocalizedString MessageLabel { get; private set; }
    public LocalizedString MessageWatermark { get; private set; }
    public LocalizedString MessageHint { get; private set; }

    public LocalizedString SubmitButton { get; private set; }
    public LocalizedString ClearButton { get; private set; }
    public LocalizedString BackButton { get; private set; }

    public LocalizedString FooterCheckData { get; private set; }
    public LocalizedString FooterReview { get; private set; }

    public LocalizedString LoadingText { get; private set; }

    public LocalizedString ValidationLogin { get; private set; }
    public LocalizedString ValidationEmail { get; private set; }
    public LocalizedString ValidationFullName { get; private set; }
    public LocalizedString ValidationPassword { get; private set; }
    public LocalizedString ValidationPasswordMatch { get; private set; }
    public LocalizedString ValidationMessage { get; private set; }

    public LocalizedString SuccessTitle { get; private set; }
    public LocalizedString SuccessMessage { get; private set; }
    public LocalizedString ErrorTitle { get; private set; }
    public LocalizedString ErrorMessage { get; private set; }
    public LocalizedString ErrorLoginExistsInUsers { get; private set; }
    public LocalizedString ErrorLoginExistsInRequests { get; private set; }
    public LocalizedString ErrorEmailExistsInUsers { get; private set; }
    public LocalizedString ErrorEmailExistsInRequests { get; private set; }
    public LocalizedString ErrorRequestPending { get; private set; }
    public LocalizedString ErrorRequestApproved { get; private set; }
    public LocalizedString ErrorRequestRejected { get; private set; }

    public LocalizedString MessageSubmitting { get; private set; }
    public LocalizedString MessageChecking { get; private set; }
    public LocalizedString MessageSaveCode { get; private set; }
    public LocalizedString MessageCodeSaved { get; private set; }

    #endregion

    public RegistrationViewModel(
        IGuestRequestRepository guestRequestRepository,
        ISecurityService securityService,
        IDialogService dialogService,
        ILocalizationService localization,
        IDatabaseContext databaseContext,
        ILoggerService loggerService)
    {
        _guestRequestRepository = guestRequestRepository;
        _securityService = securityService;
        _dialogService = dialogService;
        _localization = localization;
        _databaseContext = databaseContext;
        _loggerService = loggerService;

        InitializeLocalizedStrings();

        this.ValidationRule(
            vm => vm.Login,
            login => !string.IsNullOrWhiteSpace(login) && login.Length >= 3,
            _localization.GetString("auth.registration.validation.login"));

        this.ValidationRule(
            vm => vm.Email,
            email => !string.IsNullOrWhiteSpace(email) && Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"),
            _localization.GetString("auth.registration.validation.email"));

        this.ValidationRule(
            vm => vm.FullName,
            name => !string.IsNullOrWhiteSpace(name) && name.Length >= 3,
            _localization.GetString("auth.registration.validation.fullname"));

        this.ValidationRule(
            vm => vm.Password,
            pass => !string.IsNullOrWhiteSpace(pass) && pass.Length >= 8,
            _localization.GetString("auth.registration.validation.password"));

        this.ValidationRule(
            vm => vm.ConfirmPassword,
            confirm => confirm == Password,
            _localization.GetString("auth.registration.validation.passwordmatch"));

        this.ValidationRule(
            vm => vm.Message,
            msg => !string.IsNullOrWhiteSpace(msg) && msg.Length >= 10,
            _localization.GetString("auth.registration.validation.message"));

       var canSubmit = this.IsValid().CombineLatest(
           this.WhenAnyValue(x => x.IsSubmitting),
           (_, _) => true);

        SubmitRequestCommand = ReactiveCommand.CreateFromTask(
            SubmitRegistrationRequestAsync,
            canSubmit);

        ClearFormCommand = ReactiveCommand.Create(ClearForm);
    }

    public string Login
    {
        get => _login;
        set => this.RaiseAndSetIfChanged(ref _login, value);
    }

    public string Email
    {
        get => _email;
        set => this.RaiseAndSetIfChanged(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => this.RaiseAndSetIfChanged(ref _confirmPassword, value);
    }

    public string FullName
    {
        get => _fullName;
        set => this.RaiseAndSetIfChanged(ref _fullName, value);
    }

    public string? MobilePhone
    {
        get => _mobilePhone;
        set => this.RaiseAndSetIfChanged(ref _mobilePhone, value);
    }

    public string? Organization
    {
        get => _organization;
        set => this.RaiseAndSetIfChanged(ref _organization, value);
    }

    public string? Position
    {
        get => _position;
        set => this.RaiseAndSetIfChanged(ref _position, value);
    }

    public string Message
    {
        get => _message;
        set => this.RaiseAndSetIfChanged(ref _message, value);
    }

    public bool IsSubmitting
    {
        get => _isSubmitting;
        private set => this.RaiseAndSetIfChanged(ref _isSubmitting, value);
    }

    public ICommand SubmitRequestCommand { get; }
    public ICommand ClearFormCommand { get; }

    private void InitializeLocalizedStrings()
    {
        WindowTitle = new LocalizedString(_localization, "auth.registration.window.title");
        Header = new LocalizedString(_localization, "auth.registration.header");
        Subtitle = new LocalizedString(_localization, "auth.registration.subtitle");

        SectionRequired = new LocalizedString(_localization, "auth.registration.section.required");
        SectionAdditional = new LocalizedString(_localization, "auth.registration.section.additional");
        SectionPurpose = new LocalizedString(_localization, "auth.registration.section.purpose");

        LoginTag = new LocalizedString(_localization, "auth.registration.login.tag");
        LoginLabel = new LocalizedString(_localization, "auth.registration.login.label");
        LoginWatermark = new LocalizedString(_localization, "auth.registration.login.watermark");

        EmailTag = new LocalizedString(_localization, "auth.registration.email.tag");
        EmailLabel = new LocalizedString(_localization, "auth.registration.email.label");
        EmailWatermark = new LocalizedString(_localization, "auth.registration.email.watermark");

        FullNameTag = new LocalizedString(_localization, "auth.registration.fullname.tag");
        FullNameLabel = new LocalizedString(_localization, "auth.registration.fullname.label");
        FullNameWatermark = new LocalizedString(_localization, "auth.registration.fullname.watermark");

        PasswordTag = new LocalizedString(_localization, "auth.registration.password.tag");
        PasswordLabel = new LocalizedString(_localization, "auth.registration.password.label");
        PasswordWatermark = new LocalizedString(_localization, "auth.registration.password.watermark");

        ConfirmPasswordTag = new LocalizedString(_localization, "auth.registration.confirmpassword.tag");
        ConfirmPasswordLabel = new LocalizedString(_localization, "auth.registration.confirmpassword.label");
        ConfirmPasswordWatermark = new LocalizedString(_localization, "auth.registration.confirmpassword.watermark");

        MobilePhoneTag = new LocalizedString(_localization, "auth.registration.mobilephone.tag");
        MobilePhoneLabel = new LocalizedString(_localization, "auth.registration.mobilephone.label");
        MobilePhoneWatermark = new LocalizedString(_localization, "auth.registration.mobilephone.watermark");

        OrganizationTag = new LocalizedString(_localization, "auth.registration.organization.tag");
        OrganizationLabel = new LocalizedString(_localization, "auth.registration.organization.label");
        OrganizationWatermark = new LocalizedString(_localization, "auth.registration.organization.watermark");

        PositionTag = new LocalizedString(_localization, "auth.registration.position.tag");
        PositionLabel = new LocalizedString(_localization, "auth.registration.position.label");
        PositionWatermark = new LocalizedString(_localization, "auth.registration.position.watermark");

        MessageTag = new LocalizedString(_localization, "auth.registration.message.tag");
        MessageLabel = new LocalizedString(_localization, "auth.registration.message.label");
        MessageWatermark = new LocalizedString(_localization, "auth.registration.message.watermark");
        MessageHint = new LocalizedString(_localization, "auth.registration.message.hint");

        SubmitButton = new LocalizedString(_localization, "auth.registration.button.submit");
        ClearButton = new LocalizedString(_localization, "auth.registration.button.clear");
        BackButton = new LocalizedString(_localization, "auth.registration.button.back");

        FooterCheckData = new LocalizedString(_localization, "auth.registration.footer.checkdata");
        FooterReview = new LocalizedString(_localization, "auth.registration.footer.review");

        LoadingText = new LocalizedString(_localization, "auth.registration.loading.text");

        ValidationLogin = new LocalizedString(_localization, "auth.registration.validation.login");
        ValidationEmail = new LocalizedString(_localization, "auth.registration.validation.email");
        ValidationFullName = new LocalizedString(_localization, "auth.registration.validation.fullname");
        ValidationPassword = new LocalizedString(_localization, "auth.registration.validation.password");
        ValidationPasswordMatch = new LocalizedString(_localization, "auth.registration.validation.passwordmatch");
        ValidationMessage = new LocalizedString(_localization, "auth.registration.validation.message");

        SuccessTitle = new LocalizedString(_localization, "auth.registration.success.title");
        SuccessMessage = new LocalizedString(_localization, "auth.registration.success.message");
        ErrorTitle = new LocalizedString(_localization, "auth.registration.error.title");
        ErrorMessage = new LocalizedString(_localization, "auth.registration.error.message");
        ErrorLoginExistsInUsers = new LocalizedString(_localization, "auth.registration.error.loginexistsinusers");
        ErrorLoginExistsInRequests = new LocalizedString(_localization, "auth.registration.error.loginexistsinrequests");
        ErrorEmailExistsInUsers = new LocalizedString(_localization, "auth.registration.error.emailexistsinusers");
        ErrorEmailExistsInRequests = new LocalizedString(_localization, "auth.registration.error.emailexistsinrequests");
        ErrorRequestPending = new LocalizedString(_localization, "auth.registration.error.requestpending");
        ErrorRequestApproved = new LocalizedString(_localization, "auth.registration.error.requestapproved");
        ErrorRequestRejected = new LocalizedString(_localization, "auth.registration.error.requestrejected");

        MessageSubmitting = new LocalizedString(_localization, "auth.registration.message.submitting");
        MessageChecking = new LocalizedString(_localization, "auth.registration.message.checking");
        MessageSaveCode = new LocalizedString(_localization, "auth.registration.message.savecode");
        MessageCodeSaved = new LocalizedString(_localization, "auth.registration.message.codesaved");
    }

    private async Task SubmitRegistrationRequestAsync()
    {
        try
        {
            IsSubmitting = true;

            _loggerService.Information("Starting registration request for login: {Login}, email: {Email}", Login, Email);

            _dialogService.ShowNotification(
                MessageChecking.Value,
                "",
                Avalonia.Controls.Notifications.NotificationPosition.BottomRight,
                NotificationSeverity.Information,
                2);

            var existingUserByLogin = await _databaseContext.FindOneAsync<User>(
                u => u.Login == Login, "users");

            if (existingUserByLogin != null)
            {
                _loggerService.Warning("Registration failed: Login {Login} already exists in Users collection", Login);
                await _dialogService.ShowErrorAsync(
                    ErrorTitle.Value,
                    ErrorLoginExistsInUsers.Value);
                return;
            }

            var existingUserByEmail = await _databaseContext.FindOneAsync<User>(
                u => u.Email == Email, "users");

            if (existingUserByEmail != null)
            {
                _loggerService.Warning("Registration failed: Email {Email} already exists in Users collection", Email);
                await _dialogService.ShowErrorAsync(
                    ErrorTitle.Value,
                    ErrorEmailExistsInUsers.Value);
                return;
            }

            var existingRequestByLogin = await _guestRequestRepository.GetByLoginAsync(Login);

            if (existingRequestByLogin != null)
            {
                _loggerService.Warning("Registration failed: Login {Login} already exists in GuestRequests collection", Login);
                await _dialogService.ShowErrorAsync(
                    ErrorTitle.Value,
                    ErrorLoginExistsInRequests.Value);
                return;
            }
            var existingRequest = await _guestRequestRepository.GetByEmailAsync(Email);

            if (existingRequest != null)
            {
                _loggerService.Warning("Registration failed: GuestRequest already exists for email {Email} with status {Status}",
                    Email, existingRequest.Status);

                switch (existingRequest.Status)
                {
                    case RequestStatus.Pending:
                        await _dialogService.ShowWarningAsync(
                            ErrorTitle.Value,
                            string.Format(ErrorRequestPending.Value, existingRequest.RequestCode));
                        break;

                    case RequestStatus.Approved:
                        await _dialogService.ShowInfoAsync(
                            ErrorTitle.Value,
                            string.Format(ErrorRequestApproved.Value, existingRequest.RequestCode));
                        break;

                    case RequestStatus.Rejected:
                        var createNew = await _dialogService.ShowConfirmAsync(
                            ErrorTitle.Value,
                            string.Format(ErrorRequestRejected.Value, existingRequest.RequestCode));

                        if (!createNew)
                        {
                            return;
                        }
                        break;

                    default:
                        await _dialogService.ShowErrorAsync(
                            ErrorTitle.Value,
                            ErrorEmailExistsInRequests.Value);
                        return;
                }

                if (existingRequest.Status != RequestStatus.Rejected)
                {
                    return;
                }
            }

            _dialogService.ShowNotification(
                MessageSubmitting.Value,
                "",
                Avalonia.Controls.Notifications.NotificationPosition.BottomRight,
                NotificationSeverity.Information,
                2);

            var requestCode = GenerateRequestCode();

            var guestRequest = new GuestRequest
            {
                GuestUserId = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                RequestDate = DateTime.UtcNow,
                Status = RequestStatus.Pending,
                Login = Login,
                Email = Email,
                PasswordHash = _securityService.HashPassword(Password),
                FullName = FullName,
                MobilePhone = MobilePhone,
                Organization = Organization,
                Position = Position,
                Message = Message,
                RequestCode = requestCode
            };

            await _guestRequestRepository.CreateAsync(guestRequest);

            _loggerService.Information("Registration request created successfully for login: {Login}, email: {Email} with code {RequestCode}",
                Login, Email, requestCode);

            await _dialogService.ShowSuccessAsync(
                string.Format(SuccessMessage.Value, requestCode));

            var saveCode = await _dialogService.ShowConfirmAsync(
                MessageSaveCode.Value,
                $"Зберігаємо ваш код ({requestCode}) у файл?");

            if (saveCode)
            {
                var filePath = await _dialogService.ShowSaveFileDialogAsync(
                    MessageSaveCode.Value,
                    $"{Login}.cd",
                    [
                        new FileDialogFilter
                        {
                            Name = "Код запиту",
                            Extensions = ["cd"]
                        }
                        
                    ], App.GetService<IWindowService>().Get<AuthWindow>());

                if (!string.IsNullOrEmpty(filePath))
                {
                    try
                    {
                        var codeContent = $"Код запиту на реєстрацію\n\n" +
                                        $"Логін: {Login}\n" +
                                        $"Email: {Email}\n" +
                                        $"Дата запиту: {DateTime.Now:dd.MM.yyyy HH:mm:ss}\n\n" +
                                        $"Код для перевірки статусу:\n{requestCode}\n\n" +
                                        $"Збережіть цей код для перевірки статусу вашого запиту на реєстрацію.";

                        await System.IO.File.WriteAllTextAsync(filePath, codeContent, System.Text.Encoding.UTF8);

                        _dialogService.ShowNotification(
                            MessageCodeSaved.Value,
                            $"Файл збережено: {filePath}",
                            Avalonia.Controls.Notifications.NotificationPosition.BottomRight,
                            NotificationSeverity.Success,
                            3);

                        _loggerService.Information("Request code saved to file: {FilePath} for login: {Login}", filePath, Login);
                    }
                    catch (Exception ex)
                    {
                        _loggerService.Error(ex, "Failed to save request code to file for login: {Login}", Login);
                        await _dialogService.ShowErrorAsync(
                            "Помилка збереження",
                            "Не вдалося зберегти файл з кодом запиту.",
                            ex);
                    }
                }
            }

            ClearForm();
        }
        catch (Exception ex)
        {
            _loggerService.Error(ex, "Error submitting registration request for email: {Email}", Email);
            await _dialogService.ShowErrorAsync(
                ErrorTitle.Value,
                ErrorMessage.Value,
                ex);
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private string GenerateRequestCode()
    {
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var code = new char[8];

        for (int i = 0; i < code.Length; i++)
        {
            code[i] = chars[random.Next(chars.Length)];
        }

        return new string(code);
    }

    private void ClearForm()
    {
        Login = string.Empty;
        Email = string.Empty;
        Password = string.Empty;
        ConfirmPassword = string.Empty;
        FullName = string.Empty;
        MobilePhone = null;
        Organization = null;
        Position = null;
        Message = string.Empty;
    }
}