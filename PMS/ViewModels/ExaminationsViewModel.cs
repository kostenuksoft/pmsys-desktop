using Avalonia.Threading;
using PMS.Core.Models;
using PMS.Core.Repositories.Interfaces;
using PMS.Core.Services;
using PMS.Core.Services.Interfaces;
using PMS.ViewModels.Dialogs;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PMS.ViewModels
{
    public class ExaminationsViewModel : PageViewModelBase
    {
        #region Private Fields

        private readonly IExaminationRepository _examinationRepository;
        private readonly IPatientRepository _patientRepository;
        private readonly IDoctorRepository _doctorRepository;
        private readonly IDiagnosisRepository _diagnosisRepository;
        private readonly ISessionService _sessionService;
        private readonly IDialogService _dialogService;

        private ObservableCollection<Examination> _examinations = [];
        private ObservableCollection<Doctor> _availableDoctors = [];

        private string _searchText = string.Empty;
        private Patient? _selectedPatient;
        private Doctor? _selectedDoctor;
        private DateTime? _dateFrom;
        private DateTime? _dateTo;

        private int _currentPage = 1;
        private int _totalPages = 1;
        private long _totalExaminations;
        private int _pageSize;

        private Examination? _selectedExamination;

        private CancellationTokenSource? _loadCancellationTokenSource;

        #endregion

        #region Constructor

        public ExaminationsViewModel(
            IExaminationRepository examinationRepository,
            IPatientRepository patientRepository,
            IDoctorRepository doctorRepository,
            IDiagnosisRepository diagnosisRepository,
            ISessionService sessionService,
            IDialogService dialogService)
        {
            _examinationRepository = examinationRepository;
            _patientRepository = patientRepository;
            _doctorRepository = doctorRepository;
            _diagnosisRepository = diagnosisRepository;
            _sessionService = sessionService;
            _dialogService = dialogService;

            _pageSize = App.ApplicationSettings.PageSize;

            this.WhenAnyValue(
                    x => x.SearchText,
                    x => x.SelectedPatient,
                    x => x.SelectedDoctor,
                    x => x.DateFrom,
                    x => x.DateTo)
                .Skip(1)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => OnFiltersChanged());

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
                    Log.Error(ex, "Error loading page");
                    return Observable.Return(Unit.Default);
                })
                .Subscribe();

            var canEdit = this.WhenAnyValue(
                x => x._sessionService.CurrentUser,
                x => x._sessionService.CurrentUser!.AccessRights.EditData,
                (user, canEditData) => user != null && canEditData);

            AddExaminationCommand = ReactiveCommand.CreateFromTask(AddExaminationAsync, canEdit);
            EditExaminationCommand = ReactiveCommand.CreateFromTask<Examination>(EditExaminationAsync, canEdit);

            var canDelete = this.WhenAnyValue(
                x => x._sessionService.CurrentUser,
                x => x._sessionService.CurrentUser!.AccessRights.DeleteData,
                (user, canDeleteData) => user != null && canDeleteData);

            DeleteExaminationCommand = ReactiveCommand.CreateFromTask<Examination>(DeleteExaminationAsync, canDelete);

            RefreshCommand = ReactiveCommand.CreateFromTask(LoadCurrentPageAsync);
            ClearFiltersCommand = ReactiveCommand.Create(ClearFilters);

            PrevPageCommand = ReactiveCommand.Create(PrevPage,
                this.WhenAnyValue(x => x.CurrentPage).Select(p => p > 1));
            NextPageCommand = ReactiveCommand.Create(NextPage,
                this.WhenAnyValue(x => x.CurrentPage, x => x.TotalPages, (current, total) => current < total));

            SearchPatientCommand = ReactiveCommand.CreateFromTask(SearchPatientAsync);
        }

        #endregion

        #region Properties

        public ObservableCollection<Examination> Examinations
        {
            get => _examinations;
            set => SetAndRiseProperty(ref _examinations, value);
        }

        public ObservableCollection<Doctor> AvailableDoctors
        {
            get => _availableDoctors;
            set => SetAndRiseProperty(ref _availableDoctors, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetAndRiseProperty(ref _searchText, value);
        }

        public Patient? SelectedPatient
        {
            get => _selectedPatient;
            set => SetAndRiseProperty(ref _selectedPatient, value);
        }

        public Doctor? SelectedDoctor
        {
            get => _selectedDoctor;
            set => SetAndRiseProperty(ref _selectedDoctor, value);
        }

        public DateTime? DateFrom
        {
            get => _dateFrom;
            set => SetAndRiseProperty(ref _dateFrom, value);
        }

        public DateTime? DateTo
        {
            get => _dateTo;
            set => SetAndRiseProperty(ref _dateTo, value);
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

        public long TotalExaminations
        {
            get => _totalExaminations;
            set => SetAndRiseProperty(ref _totalExaminations, value);
        }

        public Examination? SelectedExamination
        {
            get => _selectedExamination;
            set => SetAndRiseProperty(ref _selectedExamination, value);
        }

        public string PatientFilterDisplay => SelectedPatient != null ? SelectedPatient.FullName : "Всі пацієнти";

        #endregion

        #region Commands

        public ICommand AddExaminationCommand { get; }
        public ICommand EditExaminationCommand { get; }
        public ICommand DeleteExaminationCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand PrevPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand SearchPatientCommand { get; }

        #endregion

        #region Initialization

        public override async void Initialize()
        {
            try
            {
                await LoadDoctorsAsync();
                await LoadCurrentPageAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing ExaminationsViewModel");
                ShowErrorBar($"Помилка ініціалізації: {ex.Message}");
            }
        }

        private async Task LoadDoctorsAsync()
        {
            try
            {
                var doctors = await _doctorRepository.GetAllAsync();
                AvailableDoctors = new ObservableCollection<Doctor>(doctors.Where(d => d.IsActive));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading doctors");
                ShowErrorBar($"Помилка завантаження лікарів: {ex.Message}");
            }
        }

        #endregion

        #region Load Data

        private async Task LoadCurrentPageAsync()
        {
            _loadCancellationTokenSource?.Cancel();
            _loadCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _loadCancellationTokenSource.Token;

            try
            {
                ShowInfoBar("Завантаження обстежень...");

                var (examinations, totalCount) = await _examinationRepository.GetPagedExaminationsAsync(
                    page: CurrentPage,
                    pageSize: _pageSize,
                    searchText: !string.IsNullOrWhiteSpace(SearchText) ? SearchText : null,
                    patientId: SelectedPatient?.Id,
                    doctorId: SelectedDoctor?.Id,
                    dateFrom: DateFrom,
                    dateTo: DateTo);

                if (cancellationToken.IsCancellationRequested) return;

                await LoadDisplayPropertiesAsync(examinations);

                Examinations = new ObservableCollection<Examination>(examinations);
                TotalExaminations = totalCount;
                TotalPages = (int)Math.Ceiling((double)totalCount / _pageSize);

                
            }
            catch (OperationCanceledException)
            {
                Log.Information("Load operation cancelled");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading examinations");
                ShowErrorBar($"Помилка завантаження: {ex.Message}");
            }
        }

        private async Task LoadDisplayPropertiesAsync(IEnumerable<Examination> examinations)
        {
            foreach (var examination in examinations)
            {
                try
                {
                    var patient = await _patientRepository.GetByIdAsync(examination.PatientId);
                    examination.PatientName = patient?.FullName ?? "Невідомо";

                    var doctor = await _doctorRepository.GetByIdAsync(examination.DoctorId);
                    examination.DoctorName = doctor?.FullName ?? "Невідомо";

                    if (examination.DiagnosisIds.Any())
                    {
                        var diagnoses = await Task.WhenAll(
                            examination.DiagnosisIds.Select(id => _diagnosisRepository.GetByIdAsync(id)));
                        var diagnosisNames = diagnoses.Where(d => d != null).Select(d => d!.Name);
                        examination.DiagnosesDisplay = string.Join(", ", diagnosisNames);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error loading display properties for examination {Id}", examination.Id);
                }
            }
        }

        #endregion

        #region Filters

        private void OnFiltersChanged()
        {
            CurrentPage = 1;
            Dispatcher.UIThread.InvokeAsync(LoadCurrentPageAsync);
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedPatient = null;
            SelectedDoctor = null;
            DateFrom = null;
            DateTo = null;
        }

        private async Task SearchPatientAsync()
        {
            try
            {
                var dialogViewModel = new PatientSearchDialogViewModel(
                    _patientRepository,
                    _dialogService,
                    string.Empty);

                var result = await _dialogService.ShowViewModelDialogAsync<
                    PatientSearchDialogViewModel,
                    Patient>(dialogViewModel);

                if (result != null)
                {
                    SelectedPatient = result;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error searching patient");
                ShowErrorBar($"Помилка пошуку пацієнта: {ex.Message}");
            }
        }

        #endregion

        #region Pagination

        private void PrevPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
            }
        }

        private void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
            }
        }

        #endregion

        #region CRUD Operations

        private async Task AddExaminationAsync()
        {
            try
            {
                var dialogViewModel = new ExaminationEditDialogViewModel(
                    null,
                    _patientRepository,
                    _doctorRepository,
                    _diagnosisRepository,
                    _dialogService);

                var result = await _dialogService.ShowViewModelDialogAsync<
                    ExaminationEditDialogViewModel,
                    Examination>(dialogViewModel);

                if (result != null)
                {
                    await _examinationRepository.CreateAsync(result);
                    ShowSuccessBar("Обстеження успішно додано");
                    
                    await LoadCurrentPageAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error adding examination");
                ShowErrorBar($"Помилка додавання: {ex.Message}");
            }
        }

        private async Task EditExaminationAsync(Examination examination)
        {
            try
            {
                var dialogViewModel = new ExaminationEditDialogViewModel(
                    examination,
                    _patientRepository,
                    _doctorRepository,
                    _diagnosisRepository,
                    _dialogService);

                var result = await _dialogService.ShowViewModelDialogAsync<
                    ExaminationEditDialogViewModel,
                    Examination>(dialogViewModel);

                if (result != null)
                {
                    await _examinationRepository.UpdateByIdAsync(examination.Id, result);
                    ShowSuccessBar("Обстеження успішно оновлено");
                    await LoadCurrentPageAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error editing examination");
                ShowErrorBar($"Помилка редагування: {ex.Message}");
            }
        }

        private async Task DeleteExaminationAsync(Examination examination)
        {
            try
            {
                var confirm = await _dialogService.ShowConfirmAsync(
                    "Видалення обстеження",
                    $"Ви впевнені, що хочете видалити обстеження від {examination.ExaminationDate:dd.MM.yyyy}?");

                if (confirm)
                {
                    await _examinationRepository.DeleteByIdAsync(examination.Id);
                    ShowSuccessBar("Обстеження успішно видалено");
                    await LoadCurrentPageAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting examination");
                ShowErrorBar($"Помилка видалення: {ex.Message}");
            }
        }

        #endregion
    }
}
