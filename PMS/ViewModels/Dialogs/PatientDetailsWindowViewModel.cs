using PMS.Core.Models;
using PMS.Core.Models.DTO;
using PMS.Core.Repositories.Interfaces;
using PMS.Core.Services.Interfaces;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PMS.ViewModels.Dialogs
{
   
    public class PatientDetailsWindowViewModel : PageViewModelBase
    {
        private readonly IPatientDetailsRepository _patientDetailsRepository;
        private readonly IDialogService _dialogService;

        private Patient _patient;

        private List<PatientProcedureDetail> _allProcedures = [];
        private List<ExaminationDetail> _allExaminations = [];
        private List<PatientDiagnosisSummary> _allDiagnoses = [];
        private List<AppointmentDetail> _allAppointments = [];
        private List<HomeVisitDetail> _allHomeVisits = [];
        private List<VaccinationDetail> _allVaccinations = [];

        private ObservableCollection<PatientProcedureDetail> _procedures = [];
        private ObservableCollection<ExaminationDetail> _examinations = [];
        private ObservableCollection<PatientDiagnosisSummary> _diagnoses = [];
        private ObservableCollection<AppointmentDetail> _appointments = [];
        private ObservableCollection<HomeVisitDetail> _homeVisits = [];
        private ObservableCollection<VaccinationDetail> _vaccinations = [];

        private int _selectedTabIndex = 0;

        private string _proceduresSearch = string.Empty;
        private DateTime? _proceduresDateFilter;

        private string _examinationsSearch = string.Empty;
        private DateTime? _examinationsDateFilter;

        private string _diagnosesSearch = string.Empty;

        private string _appointmentsSearch = string.Empty;
        private DateTime? _appointmentsDateFilter;

        private string _homeVisitsSearch = string.Empty;
        private DateTime? _homeVisitsDateFilter;

        private string _vaccinationsSearch = string.Empty;
        private DateTime? _vaccinationsDateFilter;

        private const int PageSize = 20;

        private int _proceduresCurrentPage = 1;
        private int _proceduresTotalPages = 1;

        private int _examinationsCurrentPage = 1;
        private int _examinationsTotalPages = 1;

        private int _diagnosesCurrentPage = 1;
        private int _diagnosesTotalPages = 1;

        private int _appointmentsCurrentPage = 1;
        private int _appointmentsTotalPages = 1;

        private int _homeVisitsCurrentPage = 1;
        private int _homeVisitsTotalPages = 1;

        private int _vaccinationsCurrentPage = 1;
        private int _vaccinationsTotalPages = 1;

        public PatientDetailsWindowViewModel(
            Patient patient,
            IPatientDetailsRepository patientDetailsRepository,
            IDialogService dialogService)
        {
            _patient = patient;
            _patientDetailsRepository = patientDetailsRepository;
            _dialogService = dialogService;

            RefreshCommand = ReactiveCommand.CreateFromTask(LoadAllDataAsync);
            CloseCommand = ReactiveCommand.Create(() => { });

            SearchCurrentTabCommand = ReactiveCommand.Create(SearchCurrentTab);
            ClearCurrentTabFiltersCommand = ReactiveCommand.Create(ClearCurrentTabFilters);
            SelectDateFilterCommand = ReactiveCommand.CreateFromTask(SelectDateFilterAsync);

            ProceduresPrevPageCommand = ReactiveCommand.Create(ProceduresPrevPage);
            ProceduresNextPageCommand = ReactiveCommand.Create(ProceduresNextPage);

            ExaminationsPrevPageCommand = ReactiveCommand.Create(ExaminationsPrevPage);
            ExaminationsNextPageCommand = ReactiveCommand.Create(ExaminationsNextPage);

            DiagnosesPrevPageCommand = ReactiveCommand.Create(DiagnosesPrevPage);
            DiagnosesNextPageCommand = ReactiveCommand.Create(DiagnosesNextPage);

            AppointmentsPrevPageCommand = ReactiveCommand.Create(AppointmentsPrevPage);
            AppointmentsNextPageCommand = ReactiveCommand.Create(AppointmentsNextPage);

            HomeVisitsPrevPageCommand = ReactiveCommand.Create(HomeVisitsPrevPage);
            HomeVisitsNextPageCommand = ReactiveCommand.Create(HomeVisitsNextPage);

            VaccinationsPrevPageCommand = ReactiveCommand.Create(VaccinationsPrevPage);
            VaccinationsNextPageCommand = ReactiveCommand.Create(VaccinationsNextPage);

            SetupReactiveSearch();
        }

        #region Properties

        public Patient Patient
        {
            get => _patient;
            set => SetAndRiseProperty(ref _patient, value);
        }

        public ObservableCollection<PatientProcedureDetail> Procedures
        {
            get => _procedures;
            set => SetAndRiseProperty(ref _procedures, value);
        }

        public ObservableCollection<ExaminationDetail> Examinations
        {
            get => _examinations;
            set => SetAndRiseProperty(ref _examinations, value);
        }

        public ObservableCollection<PatientDiagnosisSummary> Diagnoses
        {
            get => _diagnoses;
            set => SetAndRiseProperty(ref _diagnoses, value);
        }

        public ObservableCollection<AppointmentDetail> Appointments
        {
            get => _appointments;
            set => SetAndRiseProperty(ref _appointments, value);
        }

        public ObservableCollection<HomeVisitDetail> HomeVisits
        {
            get => _homeVisits;
            set => SetAndRiseProperty(ref _homeVisits, value);
        }

        public ObservableCollection<VaccinationDetail> Vaccinations
        {
            get => _vaccinations;
            set => SetAndRiseProperty(ref _vaccinations, value);
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (SetAndRiseProperty(ref _selectedTabIndex, value))
                {
                    this.RaisePropertyChanged(nameof(IsProceduresTabSelected));
                    this.RaisePropertyChanged(nameof(IsExaminationsTabSelected));
                    this.RaisePropertyChanged(nameof(IsDiagnosesTabSelected));
                    this.RaisePropertyChanged(nameof(IsAppointmentsTabSelected));
                    this.RaisePropertyChanged(nameof(IsHomeVisitsTabSelected));
                    this.RaisePropertyChanged(nameof(IsVaccinationsTabSelected));
                }
            }
        }

        public bool IsProceduresTabSelected => SelectedTabIndex == 0;
        public bool IsExaminationsTabSelected => SelectedTabIndex == 1;
        public bool IsDiagnosesTabSelected => SelectedTabIndex == 2;
        public bool IsAppointmentsTabSelected => SelectedTabIndex == 3;
        public bool IsHomeVisitsTabSelected => SelectedTabIndex == 4;
        public bool IsVaccinationsTabSelected => SelectedTabIndex == 5;

        public string ProceduresSearch
        {
            get => _proceduresSearch;
            set => SetAndRiseProperty(ref _proceduresSearch, value);
        }

        public DateTime? ProceduresDateFilter
        {
            get => _proceduresDateFilter;
            set => SetAndRiseProperty(ref _proceduresDateFilter, value);
        }

        public string ExaminationsSearch
        {
            get => _examinationsSearch;
            set => SetAndRiseProperty(ref _examinationsSearch, value);
        }

        public DateTime? ExaminationsDateFilter
        {
            get => _examinationsDateFilter;
            set => SetAndRiseProperty(ref _examinationsDateFilter, value);
        }

        public string DiagnosesSearch
        {
            get => _diagnosesSearch;
            set => SetAndRiseProperty(ref _diagnosesSearch, value);
        }

        public string AppointmentsSearch
        {
            get => _appointmentsSearch;
            set => SetAndRiseProperty(ref _appointmentsSearch, value);
        }

        public DateTime? AppointmentsDateFilter
        {
            get => _appointmentsDateFilter;
            set => SetAndRiseProperty(ref _appointmentsDateFilter, value);
        }

        public string HomeVisitsSearch
        {
            get => _homeVisitsSearch;
            set => SetAndRiseProperty(ref _homeVisitsSearch, value);
        }

        public DateTime? HomeVisitsDateFilter
        {
            get => _homeVisitsDateFilter;
            set => SetAndRiseProperty(ref _homeVisitsDateFilter, value);
        }

        public string VaccinationsSearch
        {
            get => _vaccinationsSearch;
            set => SetAndRiseProperty(ref _vaccinationsSearch, value);
        }

        public DateTime? VaccinationsDateFilter
        {
            get => _vaccinationsDateFilter;
            set => SetAndRiseProperty(ref _vaccinationsDateFilter, value);
        }

        public int ProceduresCurrentPage
        {
            get => _proceduresCurrentPage;
            set => SetAndRiseProperty(ref _proceduresCurrentPage, value);
        }

        public int ProceduresTotalPages
        {
            get => _proceduresTotalPages;
            set => SetAndRiseProperty(ref _proceduresTotalPages, value);
        }

        public int ExaminationsCurrentPage
        {
            get => _examinationsCurrentPage;
            set => SetAndRiseProperty(ref _examinationsCurrentPage, value);
        }

        public int ExaminationsTotalPages
        {
            get => _examinationsTotalPages;
            set => SetAndRiseProperty(ref _examinationsTotalPages, value);
        }

        public int DiagnosesCurrentPage
        {
            get => _diagnosesCurrentPage;
            set => SetAndRiseProperty(ref _diagnosesCurrentPage, value);
        }

        public int DiagnosesTotalPages
        {
            get => _diagnosesTotalPages;
            set => SetAndRiseProperty(ref _diagnosesTotalPages, value);
        }

        public int AppointmentsCurrentPage
        {
            get => _appointmentsCurrentPage;
            set => SetAndRiseProperty(ref _appointmentsCurrentPage, value);
        }

        public int AppointmentsTotalPages
        {
            get => _appointmentsTotalPages;
            set => SetAndRiseProperty(ref _appointmentsTotalPages, value);
        }

        public int HomeVisitsCurrentPage
        {
            get => _homeVisitsCurrentPage;
            set => SetAndRiseProperty(ref _homeVisitsCurrentPage, value);
        }

        public int HomeVisitsTotalPages
        {
            get => _homeVisitsTotalPages;
            set => SetAndRiseProperty(ref _homeVisitsTotalPages, value);
        }

        public int VaccinationsCurrentPage
        {
            get => _vaccinationsCurrentPage;
            set => SetAndRiseProperty(ref _vaccinationsCurrentPage, value);
        }

        public int VaccinationsTotalPages
        {
            get => _vaccinationsTotalPages;
            set => SetAndRiseProperty(ref _vaccinationsTotalPages, value);
        }

        public string WindowTitle => $"Деталі пацієнта: {Patient.FullName}";
        public string PatientInfo => $"{Patient.FullName}";

        public string ProceduresDateFilterDisplay => ProceduresDateFilter?.ToString("dd.MM.yyyy") ?? "Всі дати";
        public string ExaminationsDateFilterDisplay => ExaminationsDateFilter?.ToString("dd.MM.yyyy") ?? "Всі дати";
        public string AppointmentsDateFilterDisplay => AppointmentsDateFilter?.ToString("dd.MM.yyyy") ?? "Всі дати";
        public string HomeVisitsDateFilterDisplay => HomeVisitsDateFilter?.ToString("dd.MM.yyyy") ?? "Всі дати";
        public string VaccinationsDateFilterDisplay => VaccinationsDateFilter?.ToString("dd.MM.yyyy") ?? "Всі дати";

        #endregion

        #region Commands

        public ICommand RefreshCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand SearchCurrentTabCommand { get; }
        public ICommand ClearCurrentTabFiltersCommand { get; }
        public ICommand SelectDateFilterCommand { get; }

        public ICommand ProceduresPrevPageCommand { get; }
        public ICommand ProceduresNextPageCommand { get; }
        public ICommand ExaminationsPrevPageCommand { get; }
        public ICommand ExaminationsNextPageCommand { get; }
        public ICommand DiagnosesPrevPageCommand { get; }
        public ICommand DiagnosesNextPageCommand { get; }
        public ICommand AppointmentsPrevPageCommand { get; }
        public ICommand AppointmentsNextPageCommand { get; }
        public ICommand HomeVisitsPrevPageCommand { get; }
        public ICommand HomeVisitsNextPageCommand { get; }
        public ICommand VaccinationsPrevPageCommand { get; }
        public ICommand VaccinationsNextPageCommand { get; }

        #endregion

        #region Methods

        private void SetupReactiveSearch()
        {
            this.WhenAnyValue(x => x.ProceduresSearch, x => x.ProceduresDateFilter)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    if (SelectedTabIndex == 0)
                    {
                        ProceduresCurrentPage = 1;
                        FilterAndDisplayProcedures();
                    }
                });

            this.WhenAnyValue(x => x.ExaminationsSearch, x => x.ExaminationsDateFilter)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    if (SelectedTabIndex == 1)
                    {
                        ExaminationsCurrentPage = 1;
                        FilterAndDisplayExaminations();
                    }
                });

            this.WhenAnyValue(x => x.DiagnosesSearch)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    if (SelectedTabIndex == 2)
                    {
                        DiagnosesCurrentPage = 1;
                        FilterAndDisplayDiagnoses();
                    }
                });

            this.WhenAnyValue(x => x.AppointmentsSearch, x => x.AppointmentsDateFilter)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    if (SelectedTabIndex == 3)
                    {
                        AppointmentsCurrentPage = 1;
                        FilterAndDisplayAppointments();
                    }
                });

            this.WhenAnyValue(x => x.HomeVisitsSearch, x => x.HomeVisitsDateFilter)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    if (SelectedTabIndex == 4)
                    {
                        HomeVisitsCurrentPage = 1;
                        FilterAndDisplayHomeVisits();
                    }
                });

            this.WhenAnyValue(x => x.VaccinationsSearch, x => x.VaccinationsDateFilter)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    if (SelectedTabIndex == 5)
                    {
                        VaccinationsCurrentPage = 1;
                        FilterAndDisplayVaccinations();
                    }
                });
        }

        public async Task InitializeAsync()
        {
            SelectedTabIndex = 0;

            await LoadAllDataAsync();
        }

        private async Task LoadAllDataAsync()
        {
            try
            {
                IsBusy = true;
                BusyMessage = "Завантаження даних пацієнта...";

                await Task.WhenAll(
                    LoadProceduresAsync(),
                    LoadExaminationsAsync(),
                    LoadDiagnosesAsync(),
                    LoadVaccinationsAsync()
                );

                await _dialogService.ShowSuccessAsync($"Дані пацієнта оновлені станом на {DateTime.UtcNow}");
                ShowSuccessBar($"Дані пацієнта оновлені станом на {DateTime.UtcNow}");
                Log.Information("Successfully loaded all data for patient {PatientId}", Patient.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading patient details for patient {PatientId}", Patient.Id);
                ShowErrorBar($"Не вдалося завантажити дані пацієнта: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
            }
        }

        private async Task LoadProceduresAsync()
        {
            try
            {
                _allProcedures = await _patientDetailsRepository.GetPatientProceduresAsync(Patient.Id);
                FilterAndDisplayProcedures();
                Log.Information("Loaded {Count} procedures for patient {PatientId}", _allProcedures.Count, Patient.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading procedures for patient {PatientId}", Patient.Id);
                throw;
            }
        }

        private async Task LoadExaminationsAsync()
        {
            try
            {
                _allExaminations = await _patientDetailsRepository.GetPatientExaminationsAsync(Patient.Id);
                FilterAndDisplayExaminations();
                Log.Information("Loaded {Count} examinations for patient {PatientId}", _allExaminations.Count, Patient.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading examinations for patient {PatientId}", Patient.Id);
                throw;
            }
        }

        private async Task LoadDiagnosesAsync()
        {
            try
            {
                _allDiagnoses = await _patientDetailsRepository.GetPatientDiagnosesSummaryAsync(Patient.Id);
                FilterAndDisplayDiagnoses();
                Log.Information("Loaded {Count} unique diagnoses for patient {PatientId}", _allDiagnoses.Count, Patient.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading diagnoses for patient {PatientId}", Patient.Id);
                throw;
            }
        }

        private async Task LoadAppointmentsAsync()
        {
            try
            {
                _allAppointments = await _patientDetailsRepository.GetPatientAppointmentsAsync(Patient.Id);
                FilterAndDisplayAppointments();
                Log.Information("Loaded {Count} appointments for patient {PatientId}", _allAppointments.Count, Patient.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading appointments for patient {PatientId}", Patient.Id);
                throw;
            }
        }

        private async Task LoadHomeVisitsAsync()
        {
            try
            {
                _allHomeVisits = await _patientDetailsRepository.GetPatientHomeVisitsAsync(Patient.Id);
                FilterAndDisplayHomeVisits();
                Log.Information("Loaded {Count} home visits for patient {PatientId}", _allHomeVisits.Count, Patient.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading home visits for patient {PatientId}", Patient.Id);
                throw;
            }
        }

        private async Task LoadVaccinationsAsync()
        {
            try
            {
                _allVaccinations = await _patientDetailsRepository.GetPatientVaccinationsAsync(Patient.Id);
                FilterAndDisplayVaccinations();
                Log.Information("Loaded {Count} vaccinations for patient {PatientId}", _allVaccinations.Count, Patient.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading vaccinations for patient {PatientId}", Patient.Id);
                throw;
            }
        }

        #endregion

        #region Filtering and Pagination

        private void FilterAndDisplayProcedures()
        {
            var filtered = _allProcedures.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(ProceduresSearch))
            {
                var search = ProceduresSearch.ToLower();
                filtered = filtered.Where(p =>
                    p.ProcedureName.ToLower().Contains(search) ||
                    p.ProcedureCode.ToLower().Contains(search) ||
                    p.PrescribedByName.ToLower().Contains(search));
            }

            if (ProceduresDateFilter.HasValue)
            {
                var filterDate = ProceduresDateFilter.Value.Date;
                filtered = filtered.Where(p => p.PrescribedDate.Date == filterDate);
            }

            var filteredList = filtered.ToList();

            ProceduresTotalPages = (int)Math.Ceiling(filteredList.Count / (double)PageSize);
            if (ProceduresTotalPages == 0) ProceduresTotalPages = 1;
            if (ProceduresCurrentPage > ProceduresTotalPages) ProceduresCurrentPage = 1;

            var paginated = filteredList
                .Skip((ProceduresCurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            Procedures.Clear();
            foreach (var item in paginated)
            {
                Procedures.Add(item);
            }
        }

        private void FilterAndDisplayExaminations()
        {
            var filtered = _allExaminations.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(ExaminationsSearch))
            {
                var search = ExaminationsSearch.ToLower();
                filtered = filtered.Where(e =>
                    e.DoctorName.ToLower().Contains(search) ||
                    e.DoctorSpecialty.ToLower().Contains(search) ||
                    (e.Anamnesis != null && e.Anamnesis.ToLower().Contains(search)) ||
                    (e.ObjectiveStatus != null && e.ObjectiveStatus.ToLower().Contains(search)));
            }

            if (ExaminationsDateFilter.HasValue)
            {
                var filterDate = ExaminationsDateFilter.Value.Date;
                filtered = filtered.Where(e => e.ExaminationDate.Date == filterDate);
            }

            var filteredList = filtered.ToList();

            ExaminationsTotalPages = (int)Math.Ceiling(filteredList.Count / (double)PageSize);
            if (ExaminationsTotalPages == 0) ExaminationsTotalPages = 1;
            if (ExaminationsCurrentPage > ExaminationsTotalPages) ExaminationsCurrentPage = 1;

            var paginated = filteredList
                .Skip((ExaminationsCurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            Examinations.Clear();
            foreach (var item in paginated)
            {
                Examinations.Add(item);
            }
        }

        private void FilterAndDisplayDiagnoses()
        {
            var filtered = _allDiagnoses.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(DiagnosesSearch))
            {
                var search = DiagnosesSearch.ToLower();
                filtered = filtered.Where(d =>
                    d.IcdCode.ToLower().Contains(search) ||
                    d.Name.ToLower().Contains(search) ||
                    d.Category.ToLower().Contains(search));
            }

            var filteredList = filtered.ToList();

            DiagnosesTotalPages = (int)Math.Ceiling(filteredList.Count / (double)PageSize);
            if (DiagnosesTotalPages == 0) DiagnosesTotalPages = 1;
            if (DiagnosesCurrentPage > DiagnosesTotalPages) DiagnosesCurrentPage = 1;

            var paginated = filteredList
                .Skip((DiagnosesCurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            Diagnoses.Clear();
            foreach (var item in paginated)
            {
                Diagnoses.Add(item);
            }
        }

        private void FilterAndDisplayAppointments()
        {
            var filtered = _allAppointments.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(AppointmentsSearch))
            {
                var search = AppointmentsSearch.ToLower();
                filtered = filtered.Where(a =>
                    a.DoctorName.ToLower().Contains(search) ||
                    a.DoctorSpecialty.ToLower().Contains(search) ||
                    a.TypeDisplay.ToLower().Contains(search) ||
                    (a.Complaints != null && a.Complaints.ToLower().Contains(search)));
            }

            if (AppointmentsDateFilter.HasValue)
            {
                var filterDate = AppointmentsDateFilter.Value.Date;
                filtered = filtered.Where(a => a.AppointmentDate.Date == filterDate);
            }

            var filteredList = filtered.ToList();

            AppointmentsTotalPages = (int)Math.Ceiling(filteredList.Count / (double)PageSize);
            if (AppointmentsTotalPages == 0) AppointmentsTotalPages = 1;
            if (AppointmentsCurrentPage > AppointmentsTotalPages) AppointmentsCurrentPage = 1;

            var paginated = filteredList
                .Skip((AppointmentsCurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            Appointments.Clear();
            foreach (var item in paginated)
            {
                Appointments.Add(item);
            }
        }

        private void FilterAndDisplayHomeVisits()
        {
            var filtered = _allHomeVisits.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(HomeVisitsSearch))
            {
                var search = HomeVisitsSearch.ToLower();
                filtered = filtered.Where(h =>
                    h.Symptoms.ToLower().Contains(search) ||
                    (h.AssignedDoctorName != null && h.AssignedDoctorName.ToLower().Contains(search)) ||
                    h.UrgencyDisplay.ToLower().Contains(search));
            }

            if (HomeVisitsDateFilter.HasValue)
            {
                var filterDate = HomeVisitsDateFilter.Value.Date;
                filtered = filtered.Where(h => h.CallDate.Date == filterDate);
            }

            var filteredList = filtered.ToList();

            HomeVisitsTotalPages = (int)Math.Ceiling(filteredList.Count / (double)PageSize);
            if (HomeVisitsTotalPages == 0) HomeVisitsTotalPages = 1;
            if (HomeVisitsCurrentPage > HomeVisitsTotalPages) HomeVisitsCurrentPage = 1;

            var paginated = filteredList
                .Skip((HomeVisitsCurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            HomeVisits.Clear();
            foreach (var item in paginated)
            {
                HomeVisits.Add(item);
            }
        }

        private void FilterAndDisplayVaccinations()
        {
            var filtered = _allVaccinations.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(VaccinationsSearch))
            {
                var search = VaccinationsSearch.ToLower();
                filtered = filtered.Where(v =>
                    v.VaccineName.ToLower().Contains(search) ||
                    (v.VaccineLot != null && v.VaccineLot.ToLower().Contains(search)) ||
                    (v.AdministeredByName != null && v.AdministeredByName.ToLower().Contains(search)));
            }

            if (VaccinationsDateFilter.HasValue)
            {
                var filterDate = VaccinationsDateFilter.Value.Date;
                filtered = filtered.Where(v => v.ScheduledDate.Date == filterDate);
            }

            var filteredList = filtered.ToList();

            VaccinationsTotalPages = (int)Math.Ceiling(filteredList.Count / (double)PageSize);
            if (VaccinationsTotalPages == 0) VaccinationsTotalPages = 1;
            if (VaccinationsCurrentPage > VaccinationsTotalPages) VaccinationsCurrentPage = 1;

            var paginated = filteredList
                .Skip((VaccinationsCurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            Vaccinations.Clear();
            foreach (var item in paginated)
            {
                Vaccinations.Add(item);
            }
        }

        #endregion

        #region Command Handlers

        private void SearchCurrentTab()
        {
            switch (SelectedTabIndex)
            {
                case 0: FilterAndDisplayProcedures(); break;
                case 1: FilterAndDisplayExaminations(); break;
                case 2: FilterAndDisplayDiagnoses(); break;
                case 3: FilterAndDisplayAppointments(); break;
                case 4: FilterAndDisplayHomeVisits(); break;
                case 5: FilterAndDisplayVaccinations(); break;
            }
        }

        private void ClearCurrentTabFilters()
        {
            switch (SelectedTabIndex)
            {
                case 0:
                    ProceduresSearch = string.Empty;
                    ProceduresDateFilter = null;
                    break;
                case 1:
                    ExaminationsSearch = string.Empty;
                    ExaminationsDateFilter = null;
                    break;
                case 2:
                    DiagnosesSearch = string.Empty;
                    break;
                case 3:
                    AppointmentsSearch = string.Empty;
                    AppointmentsDateFilter = null;
                    break;
                case 4:
                    HomeVisitsSearch = string.Empty;
                    HomeVisitsDateFilter = null;
                    break;
                case 5:
                    VaccinationsSearch = string.Empty;
                    VaccinationsDateFilter = null;
                    break;
            }

            ShowInfoBar("Фільтри очищено");
        }

        private async Task SelectDateFilterAsync()
        {
            var selectedDate = await _dialogService.ShowDatePickerAsync("Оберіть дату");

            if (!selectedDate.HasValue) return;

            switch (SelectedTabIndex)
            {
                case 0:
                    ProceduresDateFilter = selectedDate;
                    break;
                case 1:
                    ExaminationsDateFilter = selectedDate;
                    break;
                case 3:
                    AppointmentsDateFilter = selectedDate;
                    break;
                case 4:
                    HomeVisitsDateFilter = selectedDate;
                    break;
                case 5:
                    VaccinationsDateFilter = selectedDate;
                    break;
            }
        }

        private void ProceduresPrevPage()
        {
            if (ProceduresCurrentPage > 1)
            {
                ProceduresCurrentPage--;
                FilterAndDisplayProcedures();
            }
        }

        private void ProceduresNextPage()
        {
            if (ProceduresCurrentPage < ProceduresTotalPages)
            {
                ProceduresCurrentPage++;
                FilterAndDisplayProcedures();
            }
        }

        private void ExaminationsPrevPage()
        {
            if (ExaminationsCurrentPage > 1)
            {
                ExaminationsCurrentPage--;
                FilterAndDisplayExaminations();
            }
        }

        private void ExaminationsNextPage()
        {
            if (ExaminationsCurrentPage < ExaminationsTotalPages)
            {
                ExaminationsCurrentPage++;
                FilterAndDisplayExaminations();
            }
        }

        private void DiagnosesPrevPage()
        {
            if (DiagnosesCurrentPage > 1)
            {
                DiagnosesCurrentPage--;
                FilterAndDisplayDiagnoses();
            }
        }

        private void DiagnosesNextPage()
        {
            if (DiagnosesCurrentPage < DiagnosesTotalPages)
            {
                DiagnosesCurrentPage++;
                FilterAndDisplayDiagnoses();
            }
        }

        private void AppointmentsPrevPage()
        {
            if (AppointmentsCurrentPage > 1)
            {
                AppointmentsCurrentPage--;
                FilterAndDisplayAppointments();
            }
        }

        private void AppointmentsNextPage()
        {
            if (AppointmentsCurrentPage < AppointmentsTotalPages)
            {
                AppointmentsCurrentPage++;
                FilterAndDisplayAppointments();
            }
        }

        private void HomeVisitsPrevPage()
        {
            if (HomeVisitsCurrentPage > 1)
            {
                HomeVisitsCurrentPage--;
                FilterAndDisplayHomeVisits();
            }
        }

        private void HomeVisitsNextPage()
        {
            if (HomeVisitsCurrentPage < HomeVisitsTotalPages)
            {
                HomeVisitsCurrentPage++;
                FilterAndDisplayHomeVisits();
            }
        }

        private void VaccinationsPrevPage()
        {
            if (VaccinationsCurrentPage > 1)
            {
                VaccinationsCurrentPage--;
                FilterAndDisplayVaccinations();
            }
        }

        private void VaccinationsNextPage()
        {
            if (VaccinationsCurrentPage < VaccinationsTotalPages)
            {
                VaccinationsCurrentPage++;
                FilterAndDisplayVaccinations();
            }
        }

        #endregion
    }
}