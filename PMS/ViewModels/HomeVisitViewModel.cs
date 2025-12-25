using PMS.Core.Enums.General;
using PMS.Core.Models;
using PMS.Core.Repositories.Interfaces;
using PMS.Core.Services;
using PMS.Core.Services.Interfaces;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using PMS.ViewModels.Dialogs;
using ReactiveUI.Validation.Extensions;

namespace PMS.ViewModels;

public class HomeVisitFormViewModel : PageViewModelBase, IParameterizedViewModel
{
    private readonly IHomeVisitRepository _homeVisitRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    private HomeVisit? _currentVisit;
    private bool _isEditMode;

    private string _patientName = string.Empty;
    private string _phone = string.Empty;
    private string _alternativePhone = string.Empty;
    private string _address = string.Empty;
    private string _entrance = string.Empty;

    private DateTime _callDate = DateTime.Today;
    private string _callTime = DateTime.Now.ToString("HH:mm");
    private string _callDateText = DateTime.Today.ToString("dd.MM.yyyy");
    private Urgency _urgency = Urgency.Regular;
    private string _symptoms = string.Empty;

    private Doctor? _districtDoctor;
    private Doctor? _assignedDoctor;
    private string _visitTimeSlot = string.Empty;
    private bool _urgentVisit;

    private string _notes = string.Empty;
    private HomeVisitStatus _status = HomeVisitStatus.New;
    private string _statusTimeText = string.Empty;

    private ObservableCollection<Doctor> _availableDoctors = new();
    private ObservableCollection<string> _timeSlots = new();
    private ObservableCollection<string> _validationErrors = new();

    public HomeVisitFormViewModel(
        IHomeVisitRepository homeVisitRepository,
        IPatientRepository patientRepository,
        IDoctorRepository doctorRepository,
        ISessionService sessionService,
        INavigationService navigationService,
        IDialogService dialogService)
    {
        _homeVisitRepository = homeVisitRepository;
        _patientRepository = patientRepository;
        _doctorRepository = doctorRepository;
        _sessionService = sessionService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        
        InitializeTimeSlots();

        SetupValidation();

        this.ValidationContext.ValidationStatusChange
            .Subscribe(_ => UpdateValidationErrors());

        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync, this.IsValid());

        AssignDoctorCommand = ReactiveCommand.CreateFromTask(AssignDoctorAsync,
            this.WhenAnyValue(
                x => x.AssignedDoctor,
                x => x.VisitTimeSlot,
                x => x.IsEditMode,
                (doctor, timeSlot, isEdit) =>
                    isEdit &&
                    doctor != null &&
                    !string.IsNullOrWhiteSpace(timeSlot)));
        SearchPatientCommand = ReactiveCommand.CreateFromTask(SearchPatientAsync);
        SelectCallDateCommand = ReactiveCommand.CreateFromTask(SelectCallDateAsync);
        SelectCallTimeCommand = ReactiveCommand.CreateFromTask(SelectCallTimeAsync);
        SelectVisitTimeSlotCommand = ReactiveCommand.CreateFromTask(SelectVisitTimeSlotAsync);

        CancelCommand = ReactiveCommand.Create(Cancel);

        this.WhenAnyValue(x => x.Urgency)
            .Subscribe(urgency =>
            {
                if (urgency == Urgency.Emergency)
                {
                    UrgentVisit = true;
                }
            });

        this.WhenAnyValue(x => x.Status)
            .Subscribe(_ => UpdateStatusTime());
    }

    public string PatientName
    {
        get => _patientName;
        set => SetAndRiseProperty(ref _patientName, value);
    }

    public string Phone
    {
        get => _phone;
        set => SetAndRiseProperty(ref _phone, value);
    }

    public string AlternativePhone
    {
        get => _alternativePhone;
        set => SetAndRiseProperty(ref _alternativePhone, value);
    }

    public string Address
    {
        get => _address;
        set => SetAndRiseProperty(ref _address, value);
    }

    public string Entrance
    {
        get => _entrance;
        set => SetAndRiseProperty(ref _entrance, value);
    }

    public DateTime CallDate
    {
        get => _callDate;
        set
        {
            if (SetAndRiseProperty(ref _callDate, value))
            {
                var newDateText = value.ToString("dd.MM.yyyy");
                if (_callDateText != newDateText)
                {
                    _callDateText = newDateText;
                    this.RaisePropertyChanged(nameof(CallDateText));
                }
            }
        }
    }

    public string CallDateText
    {
        get => _callDateText;
        set => SetAndRiseProperty(ref _callDateText, value);
    }

    public string CallTime
    {
        get => _callTime;
        set => SetAndRiseProperty(ref _callTime, value);
    }

    public Urgency Urgency
    {
        get => _urgency;
        set => SetAndRiseProperty(ref _urgency, value);
    }

    public string Symptoms
    {
        get => _symptoms;
        set => SetAndRiseProperty(ref _symptoms, value);
    }

    public Doctor? DistrictDoctor
    {
        get => _districtDoctor;
        set => SetAndRiseProperty(ref _districtDoctor, value);
    }

    [Required(ErrorMessage = "Виберіть призначеного лікаря")]
    public Doctor? AssignedDoctor
    {
        get => _assignedDoctor;
        set => SetAndRiseProperty(ref _assignedDoctor, value);
    }

    public string VisitTimeSlot
    {
        get => _visitTimeSlot;
        set => SetAndRiseProperty(ref _visitTimeSlot, value);
    }

    public bool UrgentVisit
    {
        get => _urgentVisit;
        set => SetAndRiseProperty(ref _urgentVisit, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetAndRiseProperty(ref _notes, value);
    }

    public HomeVisitStatus Status
    {
        get => _status;
        set => SetAndRiseProperty(ref _status, value);
    }

    public string StatusTimeText
    {
        get => _statusTimeText;
        set => SetAndRiseProperty(ref _statusTimeText, value);
    }

    public ObservableCollection<Doctor> AvailableDoctors
    {
        get => _availableDoctors;
        set => SetAndRiseProperty(ref _availableDoctors, value);
    }

    public ObservableCollection<string> TimeSlots
    {
        get => _timeSlots;
        set => SetAndRiseProperty(ref _timeSlots, value);
    }

    public ObservableCollection<string> ValidationErrors
    {
        get => _validationErrors;
        set => SetAndRiseProperty(ref _validationErrors, value);
    }

    public bool HasValidationErrors => ValidationErrors.Count > 0;

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetAndRiseProperty(ref _isEditMode, value);
    }

    public Array UrgencyOptions => Enum.GetValues(typeof(Urgency));

    public Array StatusOptions => Enum.GetValues(typeof(HomeVisitStatus));

    public ICommand SaveCommand { get; }
    public ICommand AssignDoctorCommand { get; }
    public ICommand SearchPatientCommand { get; }
    public ICommand SelectCallDateCommand { get; }
    public ICommand SelectCallTimeCommand { get; }
    public ICommand SelectVisitTimeSlotCommand { get; }
    public ICommand PrintCommand { get; }
    public ICommand CancelCommand { get; }

    public void Initialize(object parameter)
    {
        if (parameter is HomeVisit visit)
        {
            LoadHomeVisit(visit);
        }
    }

    public override async void Initialize()
    {
        await LoadDoctorsAsync();
    }
    
    private void InitializeTimeSlots()
    {
        TimeSlots =
        [
            "8:00 - 10:00",
            "10:00 - 12:00",
            "12:00 - 14:00",
            "14:00 - 16:00",
            "16:00 - 18:00",
            "18:00 - 20:00"
        ];
    }

    private void SetupValidation()
    {
        this.ValidationRule(
            vm => vm.PatientName,
            name => !string.IsNullOrWhiteSpace(name),
            "Вкажіть ім'я пацієнта");

        this.ValidationRule(
            vm => vm.Phone,
            phone => !string.IsNullOrWhiteSpace(phone),
            "Вкажіть номер телефону");

        this.ValidationRule(
            vm => vm.Phone,
            phone => string.IsNullOrWhiteSpace(phone) || phone.Length >= 10,
            "Номер телефону має містити мінімум 10 цифр");

        this.ValidationRule(
            vm => vm.Address,
            address => !string.IsNullOrWhiteSpace(address),
            "Вкажіть адресу");

        this.ValidationRule(
            vm => vm.CallDate,
            date => date >= DateTime.Today.AddDays(-7),
            "Дата виклику не може бути більше тижня назад");

        this.ValidationRule(
            vm => vm.CallTime,
            time => !string.IsNullOrWhiteSpace(time),
            "Вкажіть час виклику");

        this.ValidationRule(
            vm => vm.Symptoms,
            symptoms => !string.IsNullOrWhiteSpace(symptoms),
            "Опишіть скарги пацієнта");
    }

    private async Task LoadDoctorsAsync()
    {
        try
        {
            var doctors = await _doctorRepository.GetDistrictDoctorsAsync();
            AvailableDoctors = new ObservableCollection<Doctor>(doctors);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Помилка", $"Помилка завантаження лікарів: {ex.Message}");
        }
    }

    private void LoadHomeVisit(HomeVisit visit)
    {
        _currentVisit = visit;
        IsEditMode = true;

        PatientName = visit.PatientName;
        Phone = visit.Phone;
        AlternativePhone = visit.AlternativePhone ?? string.Empty;

        var addressParts = visit.Address.Split(',', StringSplitOptions.TrimEntries);
        Address = addressParts[0];

        foreach (var part in addressParts.Skip(1))
        {
            if (part.Contains("під'їзд", StringComparison.OrdinalIgnoreCase))
            {
                var entrancePart = part.Replace("під'їзд", "", StringComparison.OrdinalIgnoreCase)
                                      .Replace(":", "")
                                      .Trim();
                if (!string.IsNullOrWhiteSpace(entrancePart))
                {
                    Entrance = entrancePart;
                }
            }
        }

        CallDate = visit.CallDate;
        CallTime = visit.CallTime;
        Urgency = visit.Urgency;
        Symptoms = visit.Symptoms;

        if (!string.IsNullOrEmpty(visit.AssignedDoctorId))
        {
            AssignedDoctor = AvailableDoctors.FirstOrDefault(d => d.Id == visit.AssignedDoctorId);
        }

        VisitTimeSlot = visit.VisitTimeSlot ?? string.Empty;
        Notes = visit.Notes ?? string.Empty;
        Status = visit.Status;

        UpdateStatusTime();
    }

    private async Task SaveAsync()
    {
        try
        {
            var homeVisit = _currentVisit ?? new HomeVisit();

            homeVisit.PatientName = PatientName;
            homeVisit.Phone = Phone;
            homeVisit.AlternativePhone = string.IsNullOrWhiteSpace(AlternativePhone) ? null : AlternativePhone;
            homeVisit.Address = BuildFullAddress();
            homeVisit.CallDate = CallDate.Date;
            homeVisit.CallTime = CallTime;
            homeVisit.Urgency = Urgency;
            homeVisit.Symptoms = Symptoms;
            homeVisit.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes;
            homeVisit.ReceivedBy = _sessionService.CurrentUser!.Id;
            homeVisit.Status = Status;
            homeVisit.StatusUpdated = DateTime.UtcNow;
            
            if (IsEditMode)
            {
                await _homeVisitRepository.UpdateByIdAsync(homeVisit.Id, homeVisit);
            }
            else
            {
                homeVisit = await _homeVisitRepository.CreateAsync(homeVisit);
                _currentVisit = homeVisit;
                IsEditMode = true;
            }
            
            if (!IsEditMode && Status != HomeVisitStatus.Completed)
            {
                await _dialogService.ShowSuccessAsync("Виклик збережено");
            }
            else if (Status == HomeVisitStatus.Completed)
            {
                await _dialogService.ShowSuccessAsync("Виклик завершено");
            }
            else
            {
                await _dialogService.ShowSuccessAsync("Виклик оновлено");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Помилка", $"Помилка збереження: {ex.Message}");
        }
    }

    private async Task AssignDoctorAsync()
    {
        if (_currentVisit == null || AssignedDoctor == null || string.IsNullOrWhiteSpace(VisitTimeSlot))
            return;

        try
        {
            var visitDate = UrgentVisit ? DateTime.Today : CallDate.AddDays(1);

            var success = await _homeVisitRepository.AssignDoctorAsync(
                _currentVisit.Id,
                AssignedDoctor.Id,
                visitDate,
                VisitTimeSlot);

            if (success)
            {
                Status = HomeVisitStatus.Assigned;
                _currentVisit.AssignedDoctorId = AssignedDoctor.Id;
                _currentVisit.VisitDate = visitDate;
                _currentVisit.VisitTimeSlot = VisitTimeSlot;
                _currentVisit.Status = HomeVisitStatus.Assigned;

                await SaveAsync();
            }
            else
            {
                await _dialogService.ShowErrorAsync("Помилка", "Не вдалося призначити лікаря");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Помилка", $"Помилка призначення лікаря: {ex.Message}");
        }
    }
    
    
    private void Cancel()
    {
        _navigationService.NavigateTo<HomeViewModel>();
    }

    private async Task SearchPatientAsync()
    {
        try
        {
            var searchViewModel = new PatientSearchDialogViewModel(
                _patientRepository,
                _dialogService,
                PatientName); 

            var result = await _dialogService.ShowViewModelDialogAsync<
                PatientSearchDialogViewModel, Patient>(searchViewModel);

            if (result != null)
            {
                PatientName = result.FullName;
                Phone = result.Phone ?? string.Empty;
                AlternativePhone = result.AlternativePhone ?? string.Empty;
                Address = result.Address?.ToString() ?? string.Empty;
                Entrance = result.Address?.Entrance ?? string.Empty;

                if (_currentVisit != null)
                {
                    _currentVisit.PatientId = result.Id;
                }

                if (!string.IsNullOrEmpty(result.AssignedDoctorId))
                {
                    var doctor = AvailableDoctors.FirstOrDefault(d => d.Id == result.AssignedDoctorId);
                    if (doctor != null)
                    {
                        DistrictDoctor = doctor;
                        if (AssignedDoctor == null)
                        {
                            AssignedDoctor = doctor;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Помилка", $"Помилка пошуку пацієнта: {ex.Message}");
        }
    }

    private async Task SelectCallDateAsync()
    {
        try
        {
            var selectedDate = await _dialogService.ShowDatePickerAsync("Оберіть дату виклику", CallDate);
            if (selectedDate.HasValue)
            {
                CallDate = selectedDate.Value;
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Помилка", $"Помилка вибору дати: {ex.Message}");
        }
    }

    private async Task SelectCallTimeAsync()
    {
        try
        {
            TimeSpan currentTime = TimeSpan.TryParse(CallTime, out var parsedTime)
                ? parsedTime
                : DateTime.Now.TimeOfDay;

            var selectedTime = await _dialogService.ShowTimePickerAsync("Оберіть час виклику", currentTime);
            if (selectedTime.HasValue)
            {
                CallTime = selectedTime.Value.ToString(@"hh\:mm");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Помилка", $"Помилка вибору часу: {ex.Message}");
        }
    }

    private async Task SelectVisitTimeSlotAsync()
    {
        try
        {
            TimeSpan currentTime = TimeSpan.TryParse(VisitTimeSlot, out var parsedTime)
                ? parsedTime
                : new TimeSpan(14, 0, 0);

            var selectedTime = await _dialogService.ShowTimePickerAsync("Оберіть бажаний час візиту", currentTime);
            if (selectedTime.HasValue)
            {
                VisitTimeSlot = selectedTime.Value.ToString(@"hh\:mm");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Помилка", $"Помилка вибору часу: {ex.Message}");
        }
    }

    private string BuildFullAddress()
    {
        var fullAddress = Address;

        if (!string.IsNullOrWhiteSpace(Entrance))
            fullAddress += $", під'їзд {Entrance}";

        return fullAddress;
    }

    private void UpdateStatusTime()
    {
        StatusTimeText = $"Оновлено: {DateTime.Now:HH:mm}";
    }

    private void UpdateValidationErrors()
    {
        ValidationErrors.Clear();

        var validationText = this.ValidationContext.Text.ToSingleLine();
        if (!string.IsNullOrEmpty(validationText))
        {
            var errors = validationText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var error in errors)
            {
                if (!string.IsNullOrWhiteSpace(error))
                {
                    ValidationErrors.Add(error.Trim());
                }
            }
        }

        this.RaisePropertyChanged(nameof(HasValidationErrors));
    }

    public string GetUrgencyDisplayName(Urgency urgency)
    {
        return urgency switch
        {
            Urgency.Regular => "Звичайний",
            Urgency.Urgent => "Терміновий",
            Urgency.Emergency => "Екстрений",
            _ => urgency.ToString()
        };
    }

    public string GetStatusDisplayName(HomeVisitStatus status)
    {
        return status switch
        {
            HomeVisitStatus.New => "Новий",
            HomeVisitStatus.Assigned => "Призначено лікаря",
            HomeVisitStatus.InProgress => "В дорозі",
            HomeVisitStatus.Completed => "Виконано",
            HomeVisitStatus.Cancelled => "Скасовано",
            _ => status.ToString()
        };
    }
}