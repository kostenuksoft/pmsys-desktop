using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using PMS.Core.Enums.General;
using PMS.Core.Models;
using PMS.Core.Repositories.Interfaces;
using PMS.Core.Services.Interfaces;
using PMS.ViewModels.Dialogs;
using PMS.Views.Window;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PMS.ViewModels
{
    public class PatientsViewModel : PageViewModelBase
    {
        #region Private Fields

        private readonly IPatientRepository _patientRepository;

        private readonly IDoctorRepository _doctorRepository;

        private readonly ISpecialtyRepository _specialtyRepository;

        private readonly IExaminationRepository _examinationRepository;

        private readonly IPatientDetailsRepository _patientDetailsRepository;

        private readonly INavigationService _navigationService;

        private readonly IApplicationSettingsService _settingsService;

        private readonly ISessionService _sessionService;

        private readonly IDialogService _dialogService;

        private readonly ITabService _tabService;

        private readonly IWindowService _windowService;

        private ObservableCollection<Patient> _patients = [];

        private ObservableCollection<Specialty> _availableSpecialties = [];

        private ObservableCollection<Doctor> _availableDoctors = [];

        private ObservableCollection<Doctor> _filteredDoctors = [];

        private readonly ObservableCollection<string> _statusFilterOptions = ["Всі", "Активні", "Архів"];

        private readonly ObservableCollection<string> _healthStatusFilterOptions =
            ["Всі", "Здоровий", "Хронічні захворювання", "Гостре захворювання", "Одужання", "Спостереження"];

        private string _searchText = string.Empty;

        private string _statusFilter = "Всі";

        private Specialty? _specialtyFilter;

        private Doctor? _doctorFilter;

        private string _healthStatusFilter = "Всі";

        private int _currentPage = 1;

        private int _totalPages = 1;

        private long _totalPatients;

        private long _activePatients;

        private int _pageSize;

        private Patient? _selectedPatient;

        private CancellationTokenSource? _loadCancellationTokenSource;

        #endregion

        #region Constructor

        public PatientsViewModel(
            IPatientRepository patientRepository,
            IDoctorRepository doctorRepository,
            ISpecialtyRepository specialtyRepository,
            IExaminationRepository examinationRepository,
            IPatientDetailsRepository patientDetailsRepository,
            INavigationService navigationService,
            IApplicationSettingsService settingsService,
            IDialogService dialogService,
            ISessionService sessionService,
            ITabService tabService,
            IWindowService windowService)
        {
            _patientRepository = patientRepository;
            _doctorRepository = doctorRepository;
            _specialtyRepository = specialtyRepository;
            _examinationRepository = examinationRepository;
            _patientDetailsRepository = patientDetailsRepository;
            _navigationService = navigationService;
            _settingsService = settingsService;
            _dialogService = dialogService;
            _sessionService = sessionService;
            _tabService = tabService;
            _windowService = windowService;

            App.GetService<MainWindowViewModel>().ShowInfoBar("123");

            _pageSize = App.ApplicationSettings.PageSize;

            this.WhenAnyValue(
                    x => x.SearchText,
                    x => x.StatusFilter,
                    x => x.DoctorFilter,
                    x => x.HealthStatusFilter)
                .Skip(1)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => OnFiltersChanged());

            this.WhenAnyValue(x => x.SpecialtyFilter)
                .Skip(1)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => OnSpecialtyFilterChanged());

            this.WhenAnyValue(x => x.CurrentPage)
                .Skip(1)
                .SelectMany(async page =>
                {
                    await LoadCurrentPageAsync();
                    return Unit.Default;
                })
                .ObserveOn(RxApp.MainThreadScheduler)
                .Catch<Unit, Exception>(ex =>
                {
                    return Observable.Return(Unit.Default);
                })
                .Subscribe();


            var canAdd = this.WhenAnyValue(
                x => x._sessionService.CurrentUser,
                x => x._sessionService.CurrentUser!.AccessRights.EditData,
                (user, canEdit) => user != null && canEdit);


            AddPatientCommand = ReactiveCommand.CreateFromTask(AddPatientAsync, canAdd);

            RefreshCommand = ReactiveCommand.CreateFromTask(LoadCurrentPageAsync);
            SearchCommand = ReactiveCommand.Create(Search);

            ViewPatientCommand = ReactiveCommand.Create<Patient>(ViewPatient);

            var canEdit = this.WhenAnyValue(
                x => x._sessionService.CurrentUser,
                x => x._sessionService.CurrentUser!.AccessRights.EditData,
                (user, canEditData) => user != null && canEditData);

            EditPatientCommand = ReactiveCommand.CreateFromTask<Patient>(EditPatientAsync, canEdit);

            var canDelete = this.WhenAnyValue(
                x => x._sessionService.CurrentUser,
                x => x._sessionService.CurrentUser!.AccessRights.DeleteData,
                (user, canDeleteData) => user != null && canDeleteData);

            DeletePatientCommand = ReactiveCommand.CreateFromTask<Patient>(DeletePatientAsync, canDelete);

            CreateAppointmentCommand = ReactiveCommand.CreateFromTask<Patient>(CreateAppointmentAsync);
            CancelEditCommand = ReactiveCommand.Create(CancelEdit);

            PrevPageCommand = ReactiveCommand.Create(PrevPage,
                this.WhenAnyValue(x => x.TotalPages).Select(p => p > 1));
            NextPageCommand = ReactiveCommand.Create(NextPage,
                this.WhenAnyValue(x => x.TotalPages).Select(p => p > 1));

            ClearFiltersCommand = ReactiveCommand.Create(ClearFilters);
            ShowExaminationQueriesDialogCommand = ReactiveCommand.CreateFromTask(ShowExaminationQueriesDialogAsync);
        }

        #endregion

        #region Properties

        public ObservableCollection<Patient> Patients
        {
            get => _patients;
            set => SetAndRiseProperty(ref _patients, value);
        }

        public ObservableCollection<Specialty> AvailableSpecialties
        {
            get => _availableSpecialties;
            set => SetAndRiseProperty(ref _availableSpecialties, value);
        }

        public ObservableCollection<Doctor> AvailableDoctors
        {
            get => _availableDoctors;
            set => SetAndRiseProperty(ref _availableDoctors, value);
        }

        public ObservableCollection<Doctor> FilteredDoctors
        {
            get => _filteredDoctors;
            set => SetAndRiseProperty(ref _filteredDoctors, value);
        }

        public ObservableCollection<string> StatusFilterOptions => _statusFilterOptions;

        public ObservableCollection<string> HealthStatusFilterOptions => _healthStatusFilterOptions;

        public string SearchText
        {
            get => _searchText;
            set => SetAndRiseProperty(ref _searchText, value);
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set => SetAndRiseProperty(ref _statusFilter, value);
        }

        public Specialty? SpecialtyFilter
        {
            get => _specialtyFilter;
            set => SetAndRiseProperty(ref _specialtyFilter, value);
        }

        public Doctor? DoctorFilter
        {
            get => _doctorFilter;
            set => SetAndRiseProperty(ref _doctorFilter, value);
        }

        public string HealthStatusFilter
        {
            get => _healthStatusFilter;
            set => SetAndRiseProperty(ref _healthStatusFilter, value);
        }

        public int CurrentPage
        {
            get => _currentPage;
            set => SetAndRiseProperty(ref _currentPage, value);
        }

        public int TotalPages
        {
            get => _totalPages;
            set => SetAndRiseProperty(ref _totalPages, value);
        }

        public long TotalPatients
        {
            get => _totalPatients;
            set => SetAndRiseProperty(ref _totalPatients, value);
        }

        public long ActivePatients
        {
            get => _activePatients;
            set => SetAndRiseProperty(ref _activePatients, value);
        }

        public int PageSize
        {
            get => _pageSize;
            set => SetAndRiseProperty(ref _pageSize, value);
        }

        public Patient? SelectedPatient
        {
            get => _selectedPatient;
            set => SetAndRiseProperty(ref _selectedPatient, value);
        }

        public override string TabHeader => "Пацієнти";

        public override string? TabIconSource => "Contact";

        #endregion

        #region Commands

        public ICommand AddPatientCommand { get; }

        public ICommand RefreshCommand { get; }

        public ICommand SearchCommand { get; }

        public ICommand ViewPatientCommand { get; }

        public ICommand EditPatientCommand { get; }

        public ICommand DeletePatientCommand { get; }

        public ICommand CreateAppointmentCommand { get; }

        public ICommand CancelEditCommand { get; }

        public ICommand PrevPageCommand { get; }

        public ICommand NextPageCommand { get; }

        public ICommand ClearFiltersCommand { get; }

        public ICommand ShowExaminationQueriesDialogCommand { get; }

        #endregion

        #region Methods

        public override async Task InitializeAsync(object? parameter)
        {
            await base.InitializeAsync(parameter);
            await LoadInitialDataAsync();
        }

        private async Task LoadInitialDataAsync()
        {
            try
            {
                IsBusy = true;
                BusyMessage = "Завантаження даних...";

                if (_loadCancellationTokenSource != null)
                {
                    await _loadCancellationTokenSource.CancelAsync();
                }
                _loadCancellationTokenSource = new CancellationTokenSource();

                var specialties = await _specialtyRepository.GetAllOrderedAsync();
                AvailableSpecialties.Clear();

                var enumerable = specialties.ToList();
                var specialtyDict = enumerable.ToDictionary(s => s.Id);

                foreach (var specialty in enumerable)
                {
                    AvailableSpecialties.Add(specialty);
                }

                var doctors = await _doctorRepository.GetAllAsync();

                AvailableDoctors.Clear();
                FilteredDoctors.Clear();

                foreach (var doctor in doctors.Where(d => d.IsActive).OrderBy(d => d.FullName))
                {
                    if (specialtyDict.TryGetValue(doctor.SpecialtyId, out var specialty))
                    {
                        doctor.SpecialtyId = specialty.Id;
                    }

                    AvailableDoctors.Add(doctor);
                    FilteredDoctors.Add(doctor);
                }

                ActivePatients = await _patientRepository.CountAsync(p => p.IsActive);

                await LoadCurrentPageAsync();

                Log.Information("Initial data loaded successfully. Loaded {DoctorCount} doctors for filters",
                    AvailableDoctors.Count);
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка завантаження даних: {ex.Message}");
                Log.Error(ex, "Error loading initial data");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnSpecialtyFilterChanged()
        {
            FilteredDoctors.Clear();

            if (SpecialtyFilter == null)
            {
                foreach (var doctor in AvailableDoctors)
                {
                    FilteredDoctors.Add(doctor);
                }
            }
            else
            {
                var doctorsWithSpecialty = AvailableDoctors
                    .Where(d => d.SpecialtyId == SpecialtyFilter.Id)
                    .ToList();

                foreach (var doctor in doctorsWithSpecialty)
                {
                    FilteredDoctors.Add(doctor);
                }

                Log.Information("Filtered {Count} doctors for specialty {Specialty}",
                    FilteredDoctors.Count, SpecialtyFilter.Name);
            }

            if (DoctorFilter != null && SpecialtyFilter != null)
            {
                if (DoctorFilter.SpecialtyId != SpecialtyFilter.Id)
                {
                    DoctorFilter = null;
                }
            }

            OnFiltersChanged();
        }

        private async Task LoadCurrentPageAsync()
        {
            try
            {
                if (_loadCancellationTokenSource != null)
                {
                    await _loadCancellationTokenSource.CancelAsync();
                }
                _loadCancellationTokenSource = new CancellationTokenSource();
                var token = _loadCancellationTokenSource.Token;

                bool? isActive = StatusFilter switch
                {
                    "Активні" => true,
                    "Архів" => false,
                    _ => null
                };

                var searchText = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
                var doctorId = DoctorFilter?.Id;

                HealthStatus? healthStatusEnum = HealthStatusFilter switch
                {
                    "Здоровий" => HealthStatus.Healthy,
                    "Хронічні захворювання" => HealthStatus.Chronic,
                    "Гостре захворювання" => HealthStatus.Acute,
                    "Одужання" => HealthStatus.Recovery,
                    "Спостереження" => HealthStatus.Observation,
                    _ => null
                };

                var (patients, totalCount) = await Task.Run(async () =>
                        await _patientRepository.GetPagedPatientsAsync(
                            CurrentPage,
                            PageSize,
                            searchText,
                            isActive,
                            doctorId,
                            healthStatusEnum),
                    token);

                token.ThrowIfCancellationRequested();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Patients.Clear();
                    foreach (var patient in patients)
                    {
                        Patients.Add(patient);
                    }

                    TotalPatients = totalCount;
                    TotalPages = totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)PageSize) : 1;

                    if (CurrentPage > TotalPages && TotalPages > 0)
                    {
                        CurrentPage = TotalPages;
                    }

                    Log.Information(
                        "Loaded page {Page}/{Total} with {Count} patients",
                        CurrentPage, TotalPages, Patients.Count);
                }, DispatcherPriority.Normal);

                if (Patients.Count > 1)
                {
                    _dialogService.ShowNotification(
                        TabHeader,
                        $"Завантажено сторінку {CurrentPage}/{TotalPages} з {patients.Count} пацієнтами.",
                        NotificationPosition.BottomRight,
                        NotificationSeverity.Success,
                        10);
                }
                else if (Patients.Count == 1)
                {
                    _dialogService.ShowNotification(
                        TabHeader,
                        "Знайдено один запис за критерією.",
                        NotificationPosition.BottomRight,
                        NotificationSeverity.Success,
                        10);
                }
                else
                {
                    _dialogService.ShowNotification(
                        TabHeader,
                        "Схоже, що такого запису не існує. Натисніть на повідомлення, щоб створити запис.",
                        NotificationPosition.BottomRight,
                        NotificationSeverity.Warning,
                        10,
                        () => { AddPatientCommand.Execute(null); });
                }
            }
            catch (OperationCanceledException)
            {
                Log.Information("Page load cancelled.");
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка завантаження сторінки: {ex.Message}");
                Log.Error(ex, "Error loading current page");
            }
        }

        private void OnFiltersChanged()
        {
            if (CurrentPage == 1)
            {
                _ = LoadCurrentPageAsync();
            }
            else
            {
                CurrentPage = 1;
            }
        }

        private void Search()
        {
            OnFiltersChanged();
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            StatusFilter = "Всі";
            SpecialtyFilter = null;
            DoctorFilter = null;
            HealthStatusFilter = "Всі";

            _dialogService.ShowNotification(
                TabHeader,
                "Фільтри очищено",
                NotificationPosition.BottomRight,
                NotificationSeverity.Information,
                6);
        }

        private async Task ShowExaminationQueriesDialogAsync()
        {
            try
            {
                var viewModel = new ExaminationQueriesDialogViewModel(_examinationRepository, _dialogService);
                await _dialogService.ShowViewModelDialogAsync<ExaminationQueriesDialogViewModel, bool>(viewModel);
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка: {ex.Message}");
                Log.Error(ex, "Error showing examination queries dialog");
            }
        }

        private async Task AddPatientAsync()
        {
            try
            {
                if (!_sessionService.HasPermission("edit_data"))
                {
                    await _dialogService.ShowWarningAsync(
                        "Доступ заборонено",
                        "У вас немає прав для створення пацієнтів. Зверніться до адміністратора для отримання доступу.");
                    return;
                }

                IsBusy = true;
                BusyMessage = "Завантаження...";

                var doctors = await _doctorRepository.GetDistrictDoctorsAsync();

                var viewModel = new PatientEditDialogViewModel(null, doctors);
                var result =
                    await _dialogService.ShowViewModelDialogAsync<PatientEditDialogViewModel, Patient>(viewModel);

                if (result != null)
                {
                    result.CreatedDate = DateTime.UtcNow;
                    result.RegistrationDate = DateTime.Now;
                    await _patientRepository.CreateAsync(result);

                    await LoadCurrentPageAsync();

                    _dialogService.ShowNotification(
                        "Успіх",
                        $"Пацієнта {result.FullName} успішно додано",
                        NotificationPosition.BottomRight,
                        NotificationSeverity.Success);

                    Log.Information("Patient created: {PatientId} - {FullName}", result.Id, result.FullName);
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(
                    "Помилка створення пацієнта",
                    "Не вдалося створити нового пацієнта. Перевірте введені дані та спробуйте ще раз.",
                    ex);
                Log.Error(ex, "Error creating patient");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task EditPatientAsync(Patient patient)
        {
            try
            {
                if (patient == null) return;

                if (!_sessionService.HasPermission("edit_data"))
                {
                    await _dialogService.ShowWarningAsync(
                        "Доступ заборонено",
                        "У вас немає прав для редагування пацієнтів. Зверніться до адміністратора для отримання доступу.");
                    return;
                }

                IsBusy = true;
                BusyMessage = "Завантаження...";

                var patientToEdit = await _patientRepository.GetByIdAsync(patient.Id);
                if (patientToEdit == null)
                {
                    await _dialogService.ShowWarningAsync(
                        "Пацієнт не знайдений",
                        $"Пацієнта з ID {patient.Id} не знайдено в базі даних. Можливо, запис було видалено.");
                    await LoadCurrentPageAsync();
                    return;
                }

                var doctors = await _doctorRepository.GetDistrictDoctorsAsync();

                var viewModel = new PatientEditDialogViewModel(patientToEdit, doctors);
                var result =
                    await _dialogService.ShowViewModelDialogAsync<PatientEditDialogViewModel, Patient>(viewModel);

                if (result != null)
                {
                    result.ModifiedDate = DateTime.UtcNow;
                    await _patientRepository.UpdateByIdAsync(result.Id, result);

                    await LoadCurrentPageAsync();

                    _dialogService.ShowNotification(
                        "Успіх",
                        $"Дані пацієнта {result.FullName} оновлено",
                        NotificationPosition.BottomRight,
                        NotificationSeverity.Success);

                    Log.Information("Patient updated: {PatientId} - {FullName}", result.Id, result.FullName);
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(
                    "Помилка редагування",
                    $"Не вдалося оновити дані пацієнта {patient.FullName}. Перевірте підключення до бази даних.",
                    ex);
                Log.Error(ex, "Error updating patient {PatientId}", patient.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DeletePatientAsync(Patient patient)
        {
            try
            {
                if (patient == null) return;

                if (!_sessionService.HasPermission("delete_data"))
                {
                    await _dialogService.ShowWarningAsync(
                        "Доступ заборонено",
                        "У вас немає прав для видалення пацієнтів. Зверніться до адміністратора для отримання доступу.");
                    return;
                }

                var resultConfirmed = await _dialogService.ShowConfirmAsync(
                    "Підтвердження видалення",
                    $"Ви дійсно хочете видалити пацієнта {patient.FullName}?\n\n" +
                    $"Медична картка: {patient.MedicalRecordNumber}\n" +
                    $"Дата народження: {patient.BirthDate:dd.MM.yyyy}\n\n" +
                    "Ця дія незворотна і призведе до видалення всіх пов'язаних даних (прийомів, обстежень, тощо).");

                if (resultConfirmed)
                {
                    await _patientRepository.DeleteByIdAsync(patient.Id);
                    await LoadCurrentPageAsync();

                    _dialogService.ShowNotification(
                        "Успіх",
                        $"Пацієнта {patient.FullName} успішно видалено.",
                        NotificationPosition.BottomRight,
                        NotificationSeverity.Success);

                    Log.Information("Patient deleted: {PatientId} - {FullName}", patient.Id, patient.FullName);
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(
                    "Помилка видалення",
                    $"Не вдалося видалити пацієнта {patient.FullName}. Можливо, запис використовується в інших модулях системи.",
                    ex);
                Log.Error(ex, "Error deleting patient {PatientId}", patient?.Id);
            }
        }

        private async void ViewPatient(Patient patient)
        {
            if (patient == null) return;

            try
            {
                var viewModel = new PatientDetailsWindowViewModel(
                    patient,
                    _patientDetailsRepository,
                    _dialogService);

                var window = new PatientDetailsWindow(viewModel);
                window.Show(_windowService.MainWindow!);

                Log.Information("Opened patient details window for patient {PatientId}", patient.Id);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(
                    "Помилка відкриття деталей",
                    $"Не вдалося відкрити вікно деталей пацієнта {patient.FullName}.",
                    ex);
                Log.Error(ex, "Error opening patient details window for patient {PatientId}", patient.Id);
            }
        }


        private async Task CreateAppointmentAsync(Patient patient)
        {
            if (patient == null) return;

            try
            {
                var a = App.GetService<AppointmentFormViewModel>();

                await _tabService.OpenTabAsync(a, $"Реєстрація - {patient.FullName}", "Library");
                _navigationService.NavigateTo<AppointmentFormViewModel>(patient);

                Log.Information("Creating appointment for patient {PatientId}", patient.Id);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(
                    "Помилка створення прийому",
                    $"Не вдалося створити прийом для пацієнта {patient.FullName}.",
                    ex);
                Log.Error(ex, "Error creating appointment for patient {PatientId}", patient.Id);
            }
        }

        private void CancelEdit()
        {
            SelectedPatient = null;
            ShowInfoBar("Редагування скасовано");
        }

        private void PrevPage()
        {
            if (CurrentPage > 1)
                CurrentPage--;
            else
                CurrentPage = TotalPages;
        }

        private void NextPage()
        {
            if (CurrentPage < TotalPages)
                CurrentPage++;
            else
                CurrentPage = 1;
        }

        public override void CleanUp()
        {
            _loadCancellationTokenSource?.Cancel();
            _loadCancellationTokenSource?.Dispose();
            base.CleanUp();
        }

        #endregion
    }
}