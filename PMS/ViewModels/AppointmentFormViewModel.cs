using Avalonia.Controls.Notifications;
using PMS.Core.Enums.General;
using PMS.Core.Models;
using PMS.Core.Models.DTO;
using PMS.Core.Repositories.Interfaces;
using PMS.Core.Services.Interfaces;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace PMS.ViewModels
{
    public class AppointmentFormViewModel : PageViewModelBase
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IPatientRepository _patientRepository;
        private readonly IDoctorRepository _doctorRepository;
        private readonly IScheduleRepository _scheduleRepository;
        private readonly ISpecialtyRepository _specialtyRepository;
        private readonly IRoomRepository _roomRepository;
        private readonly ISessionService _sessionService;
        private readonly ITabService _tabService;
        private readonly IDialogService _dialogService;

        private Patient? _selectedPatient;
        private DoctorWithDetails? _selectedDoctor;
        private Specialty? _selectedSpecialty;
        private Room? _selectedRoom;
        private DateTime? _appointmentDate = DateTime.Today;
        private string _selectedTimeSlot = string.Empty;
        private TimeSlotInfo? _selectedTimeSlotInfo;
        private AppointmentType _selectedAppointmentType = AppointmentType.Primary;
        private string _complaints = string.Empty;
        private string _initialSearchText = string.Empty;
        private DateTime _calendarDisplayDateStart = DateTime.Today;
        private DateTime _calendarDisplayDateEnd = DateTime.Today.AddMonths(2).AddDays(
            DateTime.DaysInMonth(DateTime.Today.AddMonths(2).Year, DateTime.Today.AddMonths(2).Month)
            - DateTime.Today.AddMonths(2).Day);

        private ObservableCollection<Specialty> _specialties = new();
        private ObservableCollection<DoctorWithDetails> _allDoctors = new();
        private ObservableCollection<DoctorWithDetails> _filteredDoctors = new();
        private ObservableCollection<Room> _allRooms = new();
        private ObservableCollection<Room> _availableRooms = new();
        private ObservableCollection<TimeSlotInfo> _availableTimeSlots = new();
        private ObservableCollection<BlackoutDateInfo> _blackoutDatesInfo = new();
        private ObservableCollection<string> _validationErrors = new();

        public event EventHandler<ObservableCollection<BlackoutDateInfo>>? BlackoutDatesChanged;

        public AppointmentFormViewModel(
            IAppointmentRepository appointmentRepository,
            IPatientRepository patientRepository,
            IDoctorRepository doctorRepository,
            IScheduleRepository scheduleRepository,
            ISpecialtyRepository specialtyRepository,
            IRoomRepository roomRepository,
            ISessionService sessionService,
            ITabService tabService,
            IDialogService dialogService)
        {
            _appointmentRepository = appointmentRepository;
            _patientRepository = patientRepository;
            _doctorRepository = doctorRepository;
            _scheduleRepository = scheduleRepository;
            _specialtyRepository = specialtyRepository;
            _roomRepository = roomRepository;
            _sessionService = sessionService;
            _tabService = tabService;
            _dialogService = dialogService;

            SetupCommands();
            SetupValidation();
            SetupReactiveBindings();

            this.ValidationContext.ValidationStatusChange
                .Subscribe(_ => UpdateValidationErrors());
        }

        #region Properties

        public Patient? SelectedPatient
        {
            get => _selectedPatient;
            set => SetAndRiseProperty(ref _selectedPatient, value);
        }

        public DoctorWithDetails? SelectedDoctor
        {
            get => _selectedDoctor;
            set => SetAndRiseProperty(ref _selectedDoctor, value);
        }

        public Specialty? SelectedSpecialty
        {
            get => _selectedSpecialty;
            set => SetAndRiseProperty(ref _selectedSpecialty, value);
        }

        public Room? SelectedRoom
        {
            get => _selectedRoom;
            set => SetAndRiseProperty(ref _selectedRoom, value);
        }

        public DateTime? AppointmentDate
        {
            get => _appointmentDate;
            set => SetAndRiseProperty(ref _appointmentDate, value);
        }

        public DateTime CalendarDisplayDateStart
        {
            get => _calendarDisplayDateStart;
            set => SetAndRiseProperty(ref _calendarDisplayDateStart, value);
        }

        public DateTime CalendarDisplayDateEnd
        {
            get => _calendarDisplayDateEnd;
            set => SetAndRiseProperty(ref _calendarDisplayDateEnd, value);
        }

        public string SelectedTimeSlot
        {
            get => _selectedTimeSlot;
            set => SetAndRiseProperty(ref _selectedTimeSlot, value);
        }

        public AppointmentType SelectedAppointmentType
        {
            get => _selectedAppointmentType;
            set => SetAndRiseProperty(ref _selectedAppointmentType, value);
        }

        public string Complaints
        {
            get => _complaints;
            set => SetAndRiseProperty(ref _complaints, value);
        }

        public string InitialSearchText
        {
            get => _initialSearchText;
            set => SetAndRiseProperty(ref _initialSearchText, value);
        }

        public ObservableCollection<Specialty> Specialties
        {
            get => _specialties;
            set => SetAndRiseProperty(ref _specialties, value);
        }

        public ObservableCollection<DoctorWithDetails> FilteredDoctors
        {
            get => _filteredDoctors;
            set => SetAndRiseProperty(ref _filteredDoctors, value);
        }

        public ObservableCollection<Room> AvailableRooms
        {
            get => _availableRooms;
            set => SetAndRiseProperty(ref _availableRooms, value);
        }

        public ObservableCollection<TimeSlotInfo> AvailableTimeSlots
        {
            get => _availableTimeSlots;
            set => SetAndRiseProperty(ref _availableTimeSlots, value);
        }

        public ObservableCollection<BlackoutDateInfo> BlackoutDatesInfo
        {
            get => _blackoutDatesInfo;
            set
            {
                SetAndRiseProperty(ref _blackoutDatesInfo, value);
                BlackoutDatesChanged?.Invoke(this, value);
            }
        }

        public ObservableCollection<string> ValidationErrors
        {
            get => _validationErrors;
            set => SetAndRiseProperty(ref _validationErrors, value);
        }

        public bool HasValidationErrors => ValidationErrors.Count > 0;
        public bool HasAvailableTimeSlots => AvailableTimeSlots.Count > 0;
        public bool IsRoomSelectionEnabled => SelectedDoctor != null && AppointmentDate.HasValue;

        public class AppointmentTypeItem
        {
            public AppointmentType Type { get; set; }
            public string DisplayName { get; set; } = string.Empty;
        }

        public ObservableCollection<AppointmentTypeItem> AppointmentTypeItems { get; } = new()
        {
            new() { Type = AppointmentType.Primary, DisplayName = "Первинний прийом" },
            new() { Type = AppointmentType.FollowUp, DisplayName = "Повторний прийом" },
            new() { Type = AppointmentType.Checkup, DisplayName = "Профілактичний огляд" },
            new() { Type = AppointmentType.Vaccination, DisplayName = "Вакцинація" },
            new() { Type = AppointmentType.Procedure, DisplayName = "Процедура" }
        };

        public override string TabHeader => "Новий запис";
        public string? TabIcon => "CalendarAdd";

        #endregion

        #region Commands

        public ReactiveCommand<Unit, Unit> SearchPatientCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> SaveCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> CancelCommand { get; private set; } = null!;

        #endregion

        #region Initialization

        public override async void Initialize()
        {
            try
            {
                IsBusy = true;
                BusyMessage = "Завантаження даних...";
                _selectedPatient = new Patient();
                
                var specialtiesTask = _specialtyRepository.GetAllAsync();
                var roomsTask = _roomRepository.GetAvailableRoomsAsync();
                
                var allDoctorsTask = _doctorRepository.GetAllAsync();

                await Task.WhenAll(specialtiesTask, roomsTask, allDoctorsTask);

                Specialties = new ObservableCollection<Specialty>(specialtiesTask.Result);
                _allRooms = new ObservableCollection<Room>(roomsTask.Result.Where(r => r.IsActive));

                var doctors = allDoctorsTask.Result.Where(d => d.IsActive);
                _allDoctors.Clear();
                foreach (var doctor in doctors)
                {
                    _allDoctors.Add(new DoctorWithDetails
                    {
                        Id = doctor.Id,
                        EmployeeNumber = doctor.EmployeeNumber,
                        FullName = doctor.FullName,
                        SpecialtyId = doctor.SpecialtyId,
                        RoomId = doctor.RoomId,
                        Phone = doctor.Phone,
                        Email = doctor.Email,
                        IsActive = doctor.IsActive,
                        IsDistrictDoctor = doctor.IsDistrictDoctor,
                    });
                }

                FilteredDoctors = new ObservableCollection<DoctorWithDetails>(_allDoctors);

                Log.Information("Loaded {SpecialtiesCount} specialties, {DoctorsCount} doctors, {RoomsCount} rooms",
                    Specialties.Count, _allDoctors.Count, _allRooms.Count);
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка завантаження даних: {ex.Message}");
                Log.Error(ex, "Error initializing appointment form");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void SetupCommands()
        {
            SearchPatientCommand = ReactiveCommand.CreateFromTask(SearchPatientAsync);
            SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync, this.IsValid());
            CancelCommand = ReactiveCommand.Create(Cancel);
        }

        private void SetupValidation()
        {
            this.ValidationRule(
                vm => vm.SelectedPatient,
                patient => patient != null,
                "Оберіть пацієнта");

            this.ValidationRule(
                vm => vm.SelectedDoctor,
                doctor => doctor != null,
                "Оберіть лікаря");

            this.ValidationRule(
                vm => vm.SelectedRoom,
                room => room != null,
                "Оберіть кабінет");

            this.ValidationRule(
                vm => vm.AppointmentDate,
                date => date.HasValue && date.Value >= DateTime.Today,
                "Оберіть коректну дату");

            this.ValidationRule(
                vm => vm.SelectedTimeSlot,
                slot => !string.IsNullOrWhiteSpace(slot),
                "Оберіть час прийому");
        }

        private void SetupReactiveBindings()
        {
            this.WhenAnyValue(x => x.SelectedSpecialty)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => FilterDoctorsBySpecialty());

            this.WhenAnyValue(x => x.SelectedDoctor)
                .Where(doctor => doctor != null)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async _ =>
                {
                    SelectedRoom = null;
                    AvailableRooms.Clear();
                    AvailableTimeSlots.Clear();
                    await LoadBlackoutDatesAsync();
                });

            this.WhenAnyValue(x => x.SelectedDoctor, x => x.AppointmentDate)
                .Where(values => values.Item1 != null && values.Item2.HasValue)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async _ =>
                {
                    SelectedRoom = null;
                    AvailableTimeSlots.Clear();
                    await LoadAvailableRoomsAsync();
                    this.RaisePropertyChanged(nameof(IsRoomSelectionEnabled));
                });

            this.WhenAnyValue(x => x.SelectedDoctor, x => x.AppointmentDate, x => x.SelectedRoom)
                .Where(values => values.Item1 != null && values.Item2.HasValue && values.Item3 != null)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .SelectMany(async _ => await LoadAvailableTimeSlotsAsync())
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(slots =>
                {
                    AvailableTimeSlots.Clear();
                    foreach (var slot in slots)
                    {
                        AvailableTimeSlots.Add(slot);
                    }
                    this.RaisePropertyChanged(nameof(HasAvailableTimeSlots));
                });
        }

        #endregion

        #region Methods

        private async Task SearchPatientAsync()
        {
            try
            {
                var dialogViewModel = new ViewModels.Dialogs.PatientSearchDialogViewModel(
                    _patientRepository,
                    _dialogService,
                    InitialSearchText);

                var result = await _dialogService.ShowViewModelDialogAsync<
                    ViewModels.Dialogs.PatientSearchDialogViewModel,
                    Patient>(dialogViewModel);

                if (result != null)
                {
                    SelectedPatient = result;
                    InitialSearchText = string.Empty;
                    Log.Information("Selected patient: {PatientName}", result.FullName);
                }
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка пошуку пацієнта: {ex.Message}", "Помилка");
                Log.Error(ex, "Error searching patient");
            }
        }

        private void FilterDoctorsBySpecialty()
        {
            FilteredDoctors.Clear();

            var doctors = SelectedSpecialty != null
                ? _allDoctors.Where(d => d.SpecialtyId == SelectedSpecialty.Id)
                : _allDoctors;

            foreach (var doctor in doctors)
            {
                FilteredDoctors.Add(doctor);
            }

            if (SelectedDoctor != null && !FilteredDoctors.Contains(SelectedDoctor))
            {
                SelectedDoctor = null;
            }
        }

        private async Task LoadBlackoutDatesAsync()
        {
            if (SelectedDoctor == null)
            {
                BlackoutDatesInfo.Clear();
                return;
            }

            try
            {
                BlackoutDatesInfo.Clear();
                var endDate = CalendarDisplayDateEnd;
                var currentDate = DateTime.Today;

                while (currentDate <= endDate)
                {
                    var dayOfWeek = (int)currentDate.DayOfWeek;
                    if (dayOfWeek == 0) dayOfWeek = 7;

                    var schedule = await _scheduleRepository.GetActiveScheduleAsync(
                        SelectedDoctor.Id,
                        EntityType.Doctor,
                        dayOfWeek);

                    if (schedule == null)
                    {
                        BlackoutDatesInfo.Add(new BlackoutDateInfo
                        {
                            Date = currentDate,
                            Reason = "Немає розкладу"
                        });
                    }
                    else
                    {
                        var allSlots = GenerateTimeSlots(schedule.StartTime, schedule.EndTime);
                        var bookedSlots = (await _appointmentRepository.GetByDoctorAndDateAsync(
                            SelectedDoctor.Id, currentDate))
                            .Where(a => a.Status != AppointmentStatus.Cancelled)
                            .Select(a => a.AppointmentTime)
                            .ToHashSet();

                        var availableSlots = allSlots.Where(slot => !bookedSlots.Contains(slot)).ToList();

                        if (currentDate.Date == DateTime.Today)
                        {
                            var currentTime = DateTime.Now.TimeOfDay;
                            availableSlots = availableSlots
                                .Where(slot => TimeSpan.Parse(slot) > currentTime)
                                .ToList();
                        }

                        if (availableSlots.Count == 0)
                        {
                            BlackoutDatesInfo.Add(new BlackoutDateInfo
                            {
                                Date = currentDate,
                                Reason = currentDate.Date == DateTime.Today
                                    ? "Всі слоти минули"
                                    : "Всі слоти зайняті"
                            });
                        }
                    }

                    currentDate = currentDate.AddDays(1);
                }

                Log.Information("Loaded {Count} blackout dates for doctor {DoctorId}",
                    BlackoutDatesInfo.Count, SelectedDoctor.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading blackout dates");
            }
        }

        private async Task LoadAvailableRoomsAsync()
        {
            if (SelectedDoctor == null || !AppointmentDate.HasValue)
            {
                AvailableRooms.Clear();
                return;
            }

            try
            {
                var selectedDate = AppointmentDate.Value.Date;
                var dayOfWeek = (int)selectedDate.DayOfWeek;
                if (dayOfWeek == 0) dayOfWeek = 7;

                var doctorSchedule = await _scheduleRepository.GetActiveScheduleAsync(
                    SelectedDoctor.Id,
                    EntityType.Doctor,
                    dayOfWeek);

                if (doctorSchedule == null)
                {
                    AvailableRooms.Clear();
                    return;
                }

              
                var availableRooms = new List<Room>();

                foreach (var room in _allRooms)
                {
                    var roomSchedule = await _scheduleRepository.GetActiveScheduleAsync(
                        room.Id,
                        EntityType.Room,
                        dayOfWeek);

                    if (roomSchedule != null && SchedulesOverlap(doctorSchedule, roomSchedule))
                    {
                        availableRooms.Add(room);
                    }
                }

                AvailableRooms.Clear();
                foreach (var room in availableRooms)
                {
                    AvailableRooms.Add(room);
                }

                Log.Information("Found {Count} available rooms for doctor {Doctor} on {Date}",
                    availableRooms.Count, SelectedDoctor.FullName, selectedDate.ToShortDateString());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading available rooms");
            }
        }

        private async Task<IEnumerable<TimeSlotInfo>> LoadAvailableTimeSlotsAsync()
        {
            if (SelectedDoctor == null || !AppointmentDate.HasValue || SelectedRoom == null)
                return Array.Empty<TimeSlotInfo>();

            try
            {
                var selectedDate = AppointmentDate.Value.Date;
                var dayOfWeek = (int)selectedDate.DayOfWeek;
                if (dayOfWeek == 0) dayOfWeek = 7;

                var doctorSchedule = await _scheduleRepository.GetActiveScheduleAsync(
                    SelectedDoctor.Id,
                    EntityType.Doctor,
                    dayOfWeek);

                var roomSchedule = await _scheduleRepository.GetActiveScheduleAsync(
                    SelectedRoom.Id,
                    EntityType.Room,
                    dayOfWeek);

                if (doctorSchedule == null || roomSchedule == null)
                {
                    ShowInfoBar("Немає розкладу для обраного лікаря або кабінету", "Розклад");
                    return Array.Empty<TimeSlotInfo>();
                }

                var startTime = MaxTime(doctorSchedule.StartTime, roomSchedule.StartTime);
                var endTime = MinTime(doctorSchedule.EndTime, roomSchedule.EndTime);

                if (TimeSpan.Parse(startTime) >= TimeSpan.Parse(endTime))
                {
                    ShowInfoBar("Розклади лікаря та кабінету не перетинаються", "Розклад");
                    return Array.Empty<TimeSlotInfo>();
                }

                var allSlots = GenerateTimeSlots(startTime, endTime);

                var existingAppointments = await _appointmentRepository
                    .GetByDoctorAndDateAsync(SelectedDoctor.Id, selectedDate);

                var bookedSlots = existingAppointments
                    .Where(a => a.Status != AppointmentStatus.Cancelled)
                    .Select(a => a.AppointmentTime)
                    .ToHashSet();

                var timeSlotInfos = new List<TimeSlotInfo>();

                foreach (var slot in allSlots)
                {
                    var isBooked = bookedSlots.Contains(slot);
                    var isPast = false;

                    if (selectedDate == DateTime.Today)
                    {
                        var currentTime = DateTime.Now.TimeOfDay;
                        isPast = TimeSpan.Parse(slot) <= currentTime;
                    }

                    timeSlotInfos.Add(new TimeSlotInfo
                    {
                        Time = slot,
                        IsAvailable = !isBooked && !isPast,
                        Status = isPast ? "Минув" : (isBooked ? "Зайнято" : "Вільно")
                    });
                }

                var availableCount = timeSlotInfos.Count(t => t.IsAvailable);
                if (availableCount == 0)
                {
                    ShowInfoBar("Всі слоти на цей день зайняті або минули", "Запис");
                }

                Log.Information("Generated {Total} slots, {Available} available for {Doctor} in {Room} on {Date}",
                    allSlots.Length, availableCount, SelectedDoctor.FullName,
                    SelectedRoom.RoomNumber, selectedDate.ToShortDateString());

                return timeSlotInfos;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading time slots");
                return Array.Empty<TimeSlotInfo>();
            }
        }

        private string[] GenerateTimeSlots(string startTime, string endTime)
        {
            var slots = new List<string>();
            var start = TimeSpan.Parse(startTime);
            var end = TimeSpan.Parse(endTime);
            var interval = TimeSpan.FromMinutes(30);

            var current = start;
            while (current < end)
            {
                slots.Add(current.ToString(@"hh\:mm"));
                current = current.Add(interval);
            }

            return slots.ToArray();
        }

        private bool SchedulesOverlap(Schedule schedule1, Schedule schedule2)
        {
            var start1 = TimeSpan.Parse(schedule1.StartTime);
            var end1 = TimeSpan.Parse(schedule1.EndTime);
            var start2 = TimeSpan.Parse(schedule2.StartTime);
            var end2 = TimeSpan.Parse(schedule2.EndTime);

            return start1 < end2 && start2 < end1;
        }

        private string MaxTime(string time1, string time2)
        {
            return TimeSpan.Parse(time1) > TimeSpan.Parse(time2) ? time1 : time2;
        }

        private string MinTime(string time1, string time2)
        {
            return TimeSpan.Parse(time1) < TimeSpan.Parse(time2) ? time1 : time2;
        }

        private async Task SaveAsync()
        {
            try
            {
                if (SelectedPatient == null || SelectedDoctor == null ||
                    SelectedRoom == null || !AppointmentDate.HasValue ||
                    string.IsNullOrWhiteSpace(SelectedTimeSlot))
                {
                    ShowErrorBar("Заповніть всі обов'язкові поля");
                    return;
                }

                IsBusy = true;
                BusyMessage = "Збереження запису...";

                var appointment = new Appointment
                {
                    PatientId = SelectedPatient.Id,
                    DoctorId = SelectedDoctor.Id,
                    RoomId = SelectedRoom.Id,
                    AppointmentDate = AppointmentDate.Value,
                    AppointmentTime = SelectedTimeSlot,
                    Type = SelectedAppointmentType,
                    Status = AppointmentStatus.Scheduled,
                    Complaints = Complaints,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = _sessionService.CurrentUser?.Id ?? string.Empty
                };

                await _appointmentRepository.CreateAsync(appointment);

                _dialogService.ShowNotification(
                    "Успіх",
                    $"Запис успішно створено на {AppointmentDate.Value:dd.MM.yyyy} о {SelectedTimeSlot}",
                    NotificationPosition.BottomRight,
                    NotificationSeverity.Success);

                Log.Information("Created appointment for patient {Patient} with doctor {Doctor}",
                    SelectedPatient.FullName, SelectedDoctor.FullName);

                var currentTab = _tabService.FindTabByViewModel<AppointmentFormViewModel>();
                if (currentTab != null)
                {
                    await _tabService.CloseTabAsync(currentTab);
                }
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка збереження: {ex.Message}");
                Log.Error(ex, "Error saving appointment");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void Cancel()
        {
            var currentTab = _tabService.FindTabByViewModel<AppointmentFormViewModel>();
            if (currentTab != null)
            {
                await _tabService.CloseTabAsync(currentTab);
            }

            Log.Information("Appointment form cancelled");
        }

        #endregion

        private void UpdateValidationErrors()
        {
            ValidationErrors.Clear();

            var validationText = ValidationContext.Text?.ToSingleLine();
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

        #region Helper Classes

        public class TimeSlotInfo
        {
            public string Time { get; set; } = string.Empty;
            public bool IsAvailable { get; set; }
            public string Status { get; set; } = string.Empty;
        }

        public class BlackoutDateInfo
        {
            public DateTime Date { get; set; }
            public string Reason { get; set; } = string.Empty;
        }

        #endregion
    }
}