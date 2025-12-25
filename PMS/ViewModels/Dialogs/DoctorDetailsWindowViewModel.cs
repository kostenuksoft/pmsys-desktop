using Avalonia.Controls.Notifications;
using PMS.Core.Models;
using PMS.Core.Models.DTO;
using PMS.Core.Repositories.Interfaces;
using PMS.Core.Services.Interfaces;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using PMS.Core.Enums.General;

namespace PMS.ViewModels.Dialogs
{
    public class DoctorDetailsWindowViewModel : BaseViewModel
    {
        private readonly Doctor _doctor;
        private readonly IDoctorDetailsRepository _detailsRepository;
        private readonly IDialogService _dialogService;

        private int _selectedTabIndex;

        private ObservableCollection<CertificateDetail> _certificates = [];
        private string _certificatesSearchText = string.Empty;
        private DateTime? _certificatesStartDate;
        private DateTime? _certificatesEndDate;
        private int _certificatesCurrentPage = 1;
        private int _certificatesTotalPages = 1;
        private long _certificatesTotalCount;
        private readonly int _certificatesPageSize = 20;

        private ObservableCollection<PatientWithDoctor> _patients = [];
        private string _patientsSearchText = string.Empty;
        private int _patientsCurrentPage = 1;
        private int _patientsTotalPages = 1;
        private long _patientsTotalCount;
        private readonly int _patientsPageSize = 20;

        private ObservableCollection<ExaminationDetail> _examinations = [];
        private string _examinationsSearchText = string.Empty;
        private DateTime? _examinationsStartDate;
        private DateTime? _examinationsEndDate;
        private int _examinationsCurrentPage = 1;
        private int _examinationsTotalPages = 1;
        private long _examinationsTotalCount;
        private readonly int _examinationsPageSize = 20;

        private DoctorPatientStats? _patientStats;
        private DoctorAppointmentStats? _appointmentStats;

        private bool _isBusy;
        private string _busyMessage = string.Empty;

        public DoctorDetailsWindowViewModel(
            Doctor doctor,
            IDoctorDetailsRepository detailsRepository,
            IDialogService dialogService)
        {
            _doctor = doctor;
            _detailsRepository = detailsRepository;
            _dialogService = dialogService;

            WindowTitle = $"Деталі лікаря - {doctor.FullName}";

            this.WhenAnyValue(
                    x => x.CertificatesSearchText,
                    x => x.CertificatesStartDate,
                    x => x.CertificatesEndDate)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => OnCertificatesFiltersChanged());

            this.WhenAnyValue(x => x.CertificatesCurrentPage)
                .Skip(1)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => LoadCertificatesAsync());

            this.WhenAnyValue(x => x.PatientsSearchText)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => OnPatientsFiltersChanged());

            this.WhenAnyValue(x => x.PatientsCurrentPage)
                .Skip(1)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async _ => await LoadPatientsAsync());

            this.WhenAnyValue(
                    x => x.ExaminationsSearchText,
                    x => x.ExaminationsStartDate,
                    x => x.ExaminationsEndDate)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe( _ => OnExaminationsFiltersChanged());

            this.WhenAnyValue(x => x.ExaminationsCurrentPage)
                .Skip(1)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async _ => await LoadExaminationsAsync());

            RefreshCommand = ReactiveCommand.CreateFromTask(LoadAllDataAsync);
            ClearCertificatesFiltersCommand = ReactiveCommand.Create(ClearCertificatesFilters);
            ClearPatientsFiltersCommand = ReactiveCommand.Create(ClearPatientsFilters);
            ClearExaminationsFiltersCommand = ReactiveCommand.Create(ClearExaminationsFilters);

            CertificatesPrevPageCommand = ReactiveCommand.Create(CertificatesPrevPage,
                this.WhenAnyValue(x => x.CertificatesCurrentPage).Select(p => p > 1));
            CertificatesNextPageCommand = ReactiveCommand.Create(CertificatesNextPage,
                this.WhenAnyValue(x => x.CertificatesTotalPages).Select(p => p > 1));

            PatientsPrevPageCommand = ReactiveCommand.Create(PatientsPrevPage,
                this.WhenAnyValue(x => x.PatientsCurrentPage).Select(p => p > 1));
            PatientsNextPageCommand = ReactiveCommand.Create(PatientsNextPage,
                this.WhenAnyValue(x => x.PatientsTotalPages).Select(p => p > 1));

            ExaminationsPrevPageCommand = ReactiveCommand.Create(ExaminationsPrevPage,
                this.WhenAnyValue(x => x.ExaminationsCurrentPage).Select(p => p > 1));
            ExaminationsNextPageCommand = ReactiveCommand.Create(ExaminationsNextPage,
                this.WhenAnyValue(x => x.ExaminationsTotalPages).Select(p => p > 1));

            _ = LoadAllDataAsync();
        }

        #region Properties

        public Doctor Doctor => _doctor;
        public string WindowTitle { get; }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => this.RaiseAndSetIfChanged(ref _selectedTabIndex, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => this.RaiseAndSetIfChanged(ref _isBusy, value);
        }

        public string BusyMessage
        {
            get => _busyMessage;
            set => this.RaiseAndSetIfChanged(ref _busyMessage, value);
        }

        public ObservableCollection<CertificateDetail> Certificates
        {
            get => _certificates;
            set => this.RaiseAndSetIfChanged(ref _certificates, value);
        }

        public string CertificatesSearchText
        {
            get => _certificatesSearchText;
            set => this.RaiseAndSetIfChanged(ref _certificatesSearchText, value);
        }

        public DateTime? CertificatesStartDate
        {
            get => _certificatesStartDate;
            set => this.RaiseAndSetIfChanged(ref _certificatesStartDate, value);
        }

        public DateTime? CertificatesEndDate
        {
            get => _certificatesEndDate;
            set => this.RaiseAndSetIfChanged(ref _certificatesEndDate, value);
        }

        public int CertificatesCurrentPage
        {
            get => _certificatesCurrentPage;
            set => this.RaiseAndSetIfChanged(ref _certificatesCurrentPage, value);
        }

        public int CertificatesTotalPages
        {
            get => _certificatesTotalPages;
            set => this.RaiseAndSetIfChanged(ref _certificatesTotalPages, value);
        }

        public long CertificatesTotalCount
        {
            get => _certificatesTotalCount;
            set => this.RaiseAndSetIfChanged(ref _certificatesTotalCount, value);
        }

        public ObservableCollection<PatientWithDoctor> Patients
        {
            get => _patients;
            set => this.RaiseAndSetIfChanged(ref _patients, value);
        }

        public string PatientsSearchText
        {
            get => _patientsSearchText;
            set => this.RaiseAndSetIfChanged(ref _patientsSearchText, value);
        }

        public int PatientsCurrentPage
        {
            get => _patientsCurrentPage;
            set => this.RaiseAndSetIfChanged(ref _patientsCurrentPage, value);
        }

        public int PatientsTotalPages
        {
            get => _patientsTotalPages;
            set => this.RaiseAndSetIfChanged(ref _patientsTotalPages, value);
        }

        public long PatientsTotalCount
        {
            get => _patientsTotalCount;
            set => this.RaiseAndSetIfChanged(ref _patientsTotalCount, value);
        }

        public ObservableCollection<ExaminationDetail> Examinations
        {
            get => _examinations;
            set => this.RaiseAndSetIfChanged(ref _examinations, value);
        }

        public string ExaminationsSearchText
        {
            get => _examinationsSearchText;
            set => this.RaiseAndSetIfChanged(ref _examinationsSearchText, value);
        }

        public DateTime? ExaminationsStartDate
        {
            get => _examinationsStartDate;
            set => this.RaiseAndSetIfChanged(ref _examinationsStartDate, value);
        }

        public DateTime? ExaminationsEndDate
        {
            get => _examinationsEndDate;
            set => this.RaiseAndSetIfChanged(ref _examinationsEndDate, value);
        }

        public int ExaminationsCurrentPage
        {
            get => _examinationsCurrentPage;
            set => this.RaiseAndSetIfChanged(ref _examinationsCurrentPage, value);
        }

        public int ExaminationsTotalPages
        {
            get => _examinationsTotalPages;
            set => this.RaiseAndSetIfChanged(ref _examinationsTotalPages, value);
        }

        public long ExaminationsTotalCount
        {
            get => _examinationsTotalCount;
            set => this.RaiseAndSetIfChanged(ref _examinationsTotalCount, value);
        }

        public DoctorPatientStats? PatientStats
        {
            get => _patientStats;
            set => this.RaiseAndSetIfChanged(ref _patientStats, value);
        }

        public DoctorAppointmentStats? AppointmentStats
        {
            get => _appointmentStats;
            set => this.RaiseAndSetIfChanged(ref _appointmentStats, value);
        }

        #endregion

        #region Commands

        public ICommand RefreshCommand { get; }
        public ICommand ClearCertificatesFiltersCommand { get; }
        public ICommand ClearPatientsFiltersCommand { get; }
        public ICommand ClearExaminationsFiltersCommand { get; }

        public ICommand CertificatesPrevPageCommand { get; }
        public ICommand CertificatesNextPageCommand { get; }
        public ICommand PatientsPrevPageCommand { get; }
        public ICommand PatientsNextPageCommand { get; }
        public ICommand ExaminationsPrevPageCommand { get; }
        public ICommand ExaminationsNextPageCommand { get; }

        #endregion

        #region Methods

        private async Task LoadAllDataAsync()
        {
            try
            {
                IsBusy = true;
                BusyMessage = "Завантаження даних...";

                await Task.WhenAll(
                    LoadCertificatesAsync(),
                    LoadPatientsAsync(),
                    LoadExaminationsAsync(),
                    LoadStatisticsAsync()
                );

                Log.Information("Loaded all data for doctor {DoctorId}", _doctor.Id);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("Помилка", $"Не вдалося завантажити дані.");
                Log.Error(ex, "Error loading doctor details for {DoctorId}", _doctor.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadCertificatesAsync()
        {
            try
            {
                var searchText = string.IsNullOrWhiteSpace(CertificatesSearchText) ? null : CertificatesSearchText.Trim();

                var certificates = await _detailsRepository.GetDoctorCertificatesAsync(
                    _doctor.Id,
                    CertificatesCurrentPage,
                    _certificatesPageSize,
                    searchText,
                    CertificatesStartDate,
                    CertificatesEndDate);

                var totalCount = await _detailsRepository.CountDoctorCertificatesAsync(_doctor.Id);

                Certificates.Clear();
                foreach (var cert in certificates)
                {
                    Certificates.Add(cert);
                }

                CertificatesTotalCount = totalCount;
                CertificatesTotalPages = totalCount > 0
                    ? (int)Math.Ceiling(totalCount / (double)_certificatesPageSize)
                    : 1;

                Log.Information("Loaded {Count} certificates for doctor {DoctorId}",
                    certificates.Count, _doctor.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading certificates for doctor {DoctorId}", _doctor.Id);
                throw;
            }
        }

        private async Task LoadPatientsAsync()
        {
            try
            {
                var searchText = string.IsNullOrWhiteSpace(PatientsSearchText) ? null : PatientsSearchText.Trim();

                var patients = await _detailsRepository.GetDoctorPatientsAsync(
                    _doctor.Id,
                    PatientsCurrentPage,
                    _patientsPageSize,
                    searchText);

                var totalCount = await _detailsRepository.CountDoctorPatientsAsync(_doctor.Id);

                Patients.Clear();
                foreach (var patient in patients)
                {
                    Patients.Add(patient);
                }

                PatientsTotalCount = totalCount;
                PatientsTotalPages = totalCount > 0
                    ? (int)Math.Ceiling(totalCount / (double)_patientsPageSize)
                    : 1;

                Log.Information("Loaded {Count} patients for doctor {DoctorId}",
                    patients.Count, _doctor.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading patients for doctor {DoctorId}", _doctor.Id);
                throw;
            }
        }

        private async Task LoadExaminationsAsync()
        {
            try
            {
                var searchText = string.IsNullOrWhiteSpace(ExaminationsSearchText) ? null : ExaminationsSearchText.Trim();

                var examinations = await _detailsRepository.GetDoctorExaminationsAsync(
                    _doctor.Id,
                    ExaminationsCurrentPage,
                    _examinationsPageSize,
                    searchText,
                    ExaminationsStartDate,
                    ExaminationsEndDate);

                var totalCount = await _detailsRepository.CountDoctorExaminationsAsync(_doctor.Id);

                Examinations.Clear();
                foreach (var exam in examinations)
                {
                    Examinations.Add(exam);
                }

                ExaminationsTotalCount = totalCount;
                ExaminationsTotalPages = totalCount > 0
                    ? (int)Math.Ceiling(totalCount / (double)_examinationsPageSize)
                    : 1;

                Log.Information("Loaded {Count} examinations for doctor {DoctorId}",
                    examinations.Count, _doctor.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading examinations for doctor {DoctorId}", _doctor.Id);
                throw;
            }
        }

        private async Task LoadStatisticsAsync()
        {
            try
            {
                PatientStats = await _detailsRepository.GetDoctorPatientStatsAsync(_doctor.Id);
                AppointmentStats = await _detailsRepository.GetDoctorAppointmentStatsAsync(_doctor.Id);

                Log.Information("Loaded statistics for doctor {DoctorId}", _doctor.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading statistics for doctor {DoctorId}", _doctor.Id);
                throw;
            }
        }

        private void OnCertificatesFiltersChanged()
        {
            if (CertificatesCurrentPage == 1)
            {
                _ = LoadCertificatesAsync();
            }
            else
            {
                CertificatesCurrentPage = 1;
            }
        }

        private void OnPatientsFiltersChanged()
        {
            if (PatientsCurrentPage == 1)
            {
                _ = LoadPatientsAsync();
            }
            else
            {
                PatientsCurrentPage = 1;
            }
        }

        private void OnExaminationsFiltersChanged()
        {
            if (ExaminationsCurrentPage == 1)
            {
                _ = LoadExaminationsAsync();
            }
            else
            {
                ExaminationsCurrentPage = 1;
            }
        }

        private void ClearCertificatesFilters()
        {
            CertificatesSearchText = string.Empty;
            CertificatesStartDate = null;
            CertificatesEndDate = null;
        }

        private void ClearPatientsFilters()
        {
            PatientsSearchText = string.Empty;
        }

        private void ClearExaminationsFilters()
        {
            ExaminationsSearchText = string.Empty;
            ExaminationsStartDate = null;
            ExaminationsEndDate = null;

            Task.Delay(500);
            _dialogService.ShowNotification(
                "Фільтри очищені",
                "Фільтри для досліджень були скинені",
                NotificationPosition.BottomRight,
                NotificationSeverity.Information,
                3);
        }

        private void CertificatesPrevPage()
        {
            if (CertificatesCurrentPage > 1)
                CertificatesCurrentPage--;
        }

        private void CertificatesNextPage()
        {
            if (CertificatesCurrentPage < CertificatesTotalPages)
                CertificatesCurrentPage++;
        }

        private void PatientsPrevPage()
        {
            if (PatientsCurrentPage > 1)
                PatientsCurrentPage--;
        }

        private void PatientsNextPage()
        {
            if (PatientsCurrentPage < PatientsTotalPages)
                PatientsCurrentPage++;
        }

        private void ExaminationsPrevPage()
        {
            if (ExaminationsCurrentPage > 1)
                ExaminationsCurrentPage--;
        }

        private void ExaminationsNextPage()
        {
            if (ExaminationsCurrentPage < ExaminationsTotalPages)
                ExaminationsCurrentPage++;
        }

        #endregion
    }
}