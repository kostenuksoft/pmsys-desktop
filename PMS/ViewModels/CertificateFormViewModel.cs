using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using MongoDB.Bson;
using MongoDB.Driver;
using PMS.Core.Database;
using PMS.Core.Enums.General;
using PMS.Core.Models;
using PMS.Core.Repositories;
using PMS.Core.Repositories.Interfaces;
using PMS.Core.Services;
using PMS.Core.Services.Interfaces;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using Serilog;

namespace PMS.ViewModels
{
    public class CertificateFormViewModel : PageViewModelBase, IParameterizedViewModel
    {
        private readonly ICertificateRepository _certificateRepository;
        private readonly IPatientRepository _patientRepository;
        private readonly IDoctorRepository _doctorRepository;
        private readonly IDiagnosisRepository _diagnosisRepository;
        private readonly ISpecialtyRepository _specialtyRepository;
        private readonly ISessionService _sessionService;
        private readonly INavigationService _navigationService;
        private readonly IPdfService _pdfService;
        private readonly IDialogService _dialogService;

        private string _patientSearchText = string.Empty;
        private Patient? _selectedPatient;
        private bool _isPatientInfoVisible;
        private string _patientNameDisplay = string.Empty;
        private string _birthDateDisplay = string.Empty;

        private CertificateType _selectedCertificateType = CertificateType.Health;
        private string _certificateNumber = string.Empty;
        private DateTime _issueDate = DateTime.Today;
        private DateTime _validFrom = DateTime.Today;
        private DateTime? _validUntil;
        private Doctor? _selectedDoctor;
        private Diagnosis? _selectedDiagnosis;
        private string _diagnosisText = string.Empty;
        private string _additionalInfo = string.Empty;
        private string _previewText = string.Empty;

        private ObservableCollection<Doctor> _availableDoctors = [];
        private ObservableCollection<Diagnosis> _commonDiagnoses = [];
        private ObservableCollection<string> _validationErrors = [];
        private Certificate? _editingCertificate;

        public CertificateFormViewModel(
            ICertificateRepository certificateRepository,
            IPatientRepository patientRepository,
            IDoctorRepository doctorRepository,
            IDiagnosisRepository diagnosisRepository,
            ISpecialtyRepository specialtyRepository,
            ISessionService sessionService,
            INavigationService navigationService,
            IPdfService pdfService,
            IDialogService dialogService)
        {
            _certificateRepository = certificateRepository;
            _patientRepository = patientRepository;
            _doctorRepository = doctorRepository;
            _diagnosisRepository = diagnosisRepository;
            _specialtyRepository = specialtyRepository;
            _sessionService = sessionService;
            _navigationService = navigationService;
            _pdfService = pdfService;
            _dialogService = dialogService;

            SetupValidation();

            SearchPatientCommand = ReactiveCommand.CreateFromTask(SearchPatientAsync);
            SearchDiagnosisCommand = ReactiveCommand.CreateFromTask(SearchDiagnosisAsync);
            SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync, this.IsValid());
            SaveAndPrintCommand = ReactiveCommand.CreateFromTask(SaveAndPrintAsync, this.IsValid());

            CancelCommand = ReactiveCommand.Create(Cancel);

            GeneratePreviewCommand = ReactiveCommand.Create(GeneratePreview);

            SelectIssueDateCommand = ReactiveCommand.CreateFromTask(SelectIssueDateAsync);
            SelectValidFromCommand = ReactiveCommand.CreateFromTask(SelectValidFromAsync);
            SelectValidUntilCommand = ReactiveCommand.CreateFromTask(SelectValidUntilAsync);

            this.WhenAnyValue(x => x.SelectedCertificateType)
                .Subscribe(type =>
                {
                    UpdateValidityPeriod(type);
                    LoadCommonDiagnoses(type);
                    GeneratePreview();
                });

            this.WhenAnyValue(
                    x => x.SelectedPatient,
                    x => x.SelectedCertificateType,
                    x => x.SelectedDoctor,
                    x => x.DiagnosisText,
                    x => x.ValidFrom,
                    x => x.ValidUntil,
                    x => x.AdditionalInfo)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Subscribe(_ => GeneratePreview());

            ValidationContext.ValidationStatusChange
                .Subscribe(_ => UpdateValidationErrors());
        }

        public string PatientSearchText
        {
            get => _patientSearchText;
            set => SetAndRiseProperty(ref _patientSearchText, value);
        }

        public Patient? SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                SetAndRiseProperty(ref _selectedPatient, value);
                IsPatientInfoVisible = value != null;
                UpdatePatientDisplay();
            }
        }

        public bool IsPatientInfoVisible
        {
            get => _isPatientInfoVisible;
            set => SetAndRiseProperty(ref _isPatientInfoVisible, value);
        }

        public string PatientNameDisplay
        {
            get => _patientNameDisplay;
            set => SetAndRiseProperty(ref _patientNameDisplay, value);
        }

        public string BirthDateDisplay
        {
            get => _birthDateDisplay;
            set => SetAndRiseProperty(ref _birthDateDisplay, value);
        }

        public CertificateType SelectedCertificateType
        {
            get => _selectedCertificateType;
            set => SetAndRiseProperty(ref _selectedCertificateType, value);
        }

        public string CertificateNumber
        {
            get => _certificateNumber;
            set => SetAndRiseProperty(ref _certificateNumber, value);
        }

        public DateTime IssueDate
        {
            get => _issueDate;
            set
            {
                SetAndRiseProperty(ref _issueDate, value);
                this.RaisePropertyChanged(nameof(IssueDateText));
            }
        }

        public string IssueDateText => IssueDate.ToString("dd.MM.yyyy");

        public DateTime ValidFrom
        {
            get => _validFrom;
            set
            {
                SetAndRiseProperty(ref _validFrom, value);
                this.RaisePropertyChanged(nameof(ValidFromText));
            }
        }

        public DateTime? ValidUntil
        {
            get => _validUntil;
            set
            {
                SetAndRiseProperty(ref _validUntil, value);
                this.RaisePropertyChanged(nameof(ValidUntilText));
            }
        }

        public string ValidFromText => ValidFrom.ToString("dd.MM.yyyy");

        public string ValidUntilText => ValidUntil?.ToString("dd.MM.yyyy") ?? "Не встановлено";

        public Doctor? SelectedDoctor
        {
            get => _selectedDoctor;
            set => SetAndRiseProperty(ref _selectedDoctor, value);
        }

        public Diagnosis? SelectedDiagnosis
        {
            get => _selectedDiagnosis;
            set
            {
                SetAndRiseProperty(ref _selectedDiagnosis, value);
                if (value != null)
                {
                    DiagnosisText = $"{value.IcdCode} - {value.Name}";
                }
            }
        }

        public string DiagnosisText
        {
            get => _diagnosisText;
            set => SetAndRiseProperty(ref _diagnosisText, value);
        }

        public string AdditionalInfo
        {
            get => _additionalInfo;
            set => SetAndRiseProperty(ref _additionalInfo, value);
        }

        public string PreviewText
        {
            get => _previewText;
            set => SetAndRiseProperty(ref _previewText, value);
        }

        public ObservableCollection<Doctor> AvailableDoctors
        {
            get => _availableDoctors;
            set => SetAndRiseProperty(ref _availableDoctors, value);
        }

        public ObservableCollection<Diagnosis> CommonDiagnoses
        {
            get => _commonDiagnoses;
            set => SetAndRiseProperty(ref _commonDiagnoses, value);
        }

        public ObservableCollection<string> ValidationErrors
        {
            get => _validationErrors;
            set => SetAndRiseProperty(ref _validationErrors, value);
        }

        public bool HasValidationErrors => ValidationErrors.Count > 0;

        public ObservableCollection<KeyValuePair<CertificateType, string>> CertificateTypes { get; } =
        [
            new(CertificateType.SickLeave, "Довідка про тимчасову непрацездатність"),
            new(CertificateType.Pool, "Довідка для відвідування басейну"),
            new(CertificateType.Health, "Довідка про стан здоров'я"),
            new(CertificateType.Driver, "Довідка для водіїв"),
            new(CertificateType.Work, "Довідка для роботи"),
            new(CertificateType.Education, "Довідка для навчального закладу"),
            new(CertificateType.Vaccination, "Довідка про вакцинацію"),
            new(CertificateType.Dispensary, "Довідка про диспансерний облік")
        ];

        public ICommand SearchPatientCommand { get; }
        public ICommand SearchDiagnosisCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveAndPrintCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand GeneratePreviewCommand { get; }
        public ICommand SelectIssueDateCommand { get; }
        public ICommand SelectValidFromCommand { get; }
        public ICommand SelectValidUntilCommand { get; }

        public void Initialize(object parameter)
        {
            if (parameter is Certificate certificate)
            {
                _editingCertificate = certificate;
                LoadCertificateData(certificate);
            }
            else if (parameter is Patient patient)
            {
                SelectedPatient = patient;
                PatientSearchText = patient.FullName;
            }
        }

        public override async void Initialize()
        {
            try
            {
                await LoadDoctorsAsync();
                await GenerateCertificateNumberAsync();
                LoadCommonDiagnoses(SelectedCertificateType);
            }
            catch (Exception e)
            {
                throw;
            }
        }

       

        private async Task LoadDoctorsAsync()
        {
            try
            {
                ShowInfoBar("Завантаження списку лікарів...");

                var doctors = await _doctorRepository.GetAllAsync();
                AvailableDoctors = new ObservableCollection<Doctor>(doctors.Where(d => d.IsActive));

                if (_sessionService.CurrentUser != null)
                {
                    SelectedDoctor = AvailableDoctors.FirstOrDefault();
                }

            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка завантаження: {ex.Message}");
            }
        }

        private async Task GenerateCertificateNumberAsync()
        {
            try
            {
                var lastCertificate = await _certificateRepository.GetLastCertificateAsync();
                if (lastCertificate != null)
                {
                    var lastNumber = int.Parse(lastCertificate.CertificateNumber.Substring(1));
                    CertificateNumber = $"C{(lastNumber + 1):D8}";
                }
                else
                {
                    CertificateNumber = "C00000001";
                }
            }
            catch
            {
                CertificateNumber = $"C{DateTime.Now:yyyyMMdd}01";
            }
        }

        private async Task SearchPatientAsync()
        {
            try
            {
                var dialogViewModel = new Dialogs.PatientSearchDialogViewModel(
                    _patientRepository,
                    _dialogService,
                    PatientSearchText);

                var result = await _dialogService.ShowViewModelDialogAsync<
                    Dialogs.PatientSearchDialogViewModel,
                    Patient>(dialogViewModel);

                if (result != null)
                {
                    SelectedPatient = result;
                    PatientSearchText = result.FullName;
                    Log.Information("Selected patient: {PatientName}", result.FullName);
                }
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка пошуку пацієнта: {ex.Message}");
                Log.Error(ex, "Error searching patient");
            }
        }

        private async Task SearchDiagnosisAsync()
        {
            try
            {
                var dialogViewModel = new Dialogs.DiagnosisSearchDialogViewModel(
                    _diagnosisRepository,
                    _dialogService,
                    DiagnosisText);

                var result = await _dialogService.ShowViewModelDialogAsync<
                    Dialogs.DiagnosisSearchDialogViewModel,
                    Diagnosis>(dialogViewModel);

                if (result != null)
                {
                    SelectedDiagnosis = result;
                    DiagnosisText = $"{result.IcdCode} - {result.Name}";
                    Log.Information("Selected diagnosis: {DiagnosisCode} - {DiagnosisName}", result.IcdCode, result.Name);
                }
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка пошуку діагнозу: {ex.Message}");
                Log.Error(ex, "Error searching diagnosis");
            }
        }

        private void UpdatePatientDisplay()
        {
            if (SelectedPatient != null)
            {
                PatientNameDisplay = SelectedPatient.FullName;
                BirthDateDisplay = SelectedPatient.BirthDate.ToString("dd.MM.yyyy");
            }
            else
            {
                PatientNameDisplay = string.Empty;
                BirthDateDisplay = string.Empty;
            }
        }

        private void UpdateValidityPeriod(CertificateType type)
        {
            ValidFrom = DateTime.Today;

            ValidUntil = type switch
            {
                CertificateType.SickLeave => DateTime.Today.AddDays(5),
                CertificateType.Pool => DateTime.Today.AddMonths(6),
                CertificateType.Health => DateTime.Today.AddMonths(1),
                CertificateType.Driver => DateTime.Today.AddYears(2),
                CertificateType.Work => DateTime.Today.AddYears(1),
                CertificateType.Education => DateTime.Today.AddMonths(1),
                CertificateType.Vaccination => null,
                CertificateType.Dispensary => DateTime.Today.AddYears(1),
                _ => DateTime.Today.AddMonths(1)
            };
        }

        private async void LoadCommonDiagnoses(CertificateType type)
        {
            try
            {
                var diagnoses = await _diagnosisRepository.GetByCategoryAsync(GetDiagnosisCategory(type));
                CommonDiagnoses = new ObservableCollection<Diagnosis>(diagnoses.Take(10));
            }
            catch
            {
                CommonDiagnoses.Clear();
            }
        }

        private string GetDiagnosisCategory(CertificateType type)
        {
            return type switch
            {
                CertificateType.SickLeave => "Хвороби органів дихання",
                CertificateType.Health => "Загальний стан",
                _ => "Всі"
            };
        }

        private void GeneratePreview()
        {
            if (SelectedPatient == null || SelectedDoctor == null)
            {
                PreviewText = "Заповніть дані пацієнта та лікаря для попереднього перегляду";
                return;
            }

            var preview = $"ДОВІДКА\n\n";
            preview += $"{GetCertificateTypeName(SelectedCertificateType)}\n\n";
            preview += $"Видана: {SelectedPatient.FullName}\n";
            preview += $"Дата народження: {SelectedPatient.BirthDate:dd.MM.yyyy}\n";
            preview += $"Адреса: {SelectedPatient.Address.Street} {SelectedPatient.Address.Building}, " +
                      $"кв. {SelectedPatient.Address.Apartment}\n\n";

            switch (SelectedCertificateType)
            {
                case CertificateType.SickLeave:
                    preview += $"Діагноз: {DiagnosisText}\n";
                    preview += $"Звільнений(а) від роботи з {ValidFrom:dd.MM.yyyy} по {ValidUntil:dd.MM.yyyy}\n";
                    break;

                case CertificateType.Health:
                    preview += "Стан здоров'я: Практично здоровий(а)\n";
                    if (!string.IsNullOrEmpty(DiagnosisText))
                        preview += $"Примітка: {DiagnosisText}\n";
                    break;

                case CertificateType.Pool:
                    preview += "Протипоказань для відвідування басейну не виявлено\n";
                    preview += $"Дійсна до: {ValidUntil:dd.MM.yyyy}\n";
                    break;

                case CertificateType.Vaccination:
                    preview += $"Вакцинація: {AdditionalInfo}\n";
                    preview += $"Дата проведення: {ValidFrom:dd.MM.yyyy}\n";
                    break;

                default:
                    preview += $"Довідка дійсна з {ValidFrom:dd.MM.yyyy}";
                    if (ValidUntil.HasValue)
                        preview += $" по {ValidUntil:dd.MM.yyyy}";
                    preview += "\n";
                    break;
            }

            if (!string.IsNullOrEmpty(AdditionalInfo) && SelectedCertificateType != CertificateType.Vaccination)
            {
                preview += $"\nДодаткова інформація: {AdditionalInfo}\n";
            }

            preview += $"\n\nЛікар: {SelectedDoctor.FullName}\n";
            preview += $"Дата видачі: {IssueDate:dd.MM.yyyy}\n";
            preview += $"№ {CertificateNumber}";

            PreviewText = preview;
        }

        private string GetCertificateTypeName(CertificateType type)
        {
            return CertificateTypes.FirstOrDefault(ct => ct.Key == type).Value ?? type.ToString();
        }

        private async Task SaveAsync()
        {
            try
            {
                await SaveCertificateAsync();
                _navigationService.NavigateBack();
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка збереження: {ex.Message}");
            }
        }

        private async Task<Certificate> SaveCertificateAsync()
        {
            ShowInfoBar("Збереження довідки...");

            var certificate = _editingCertificate ?? new Certificate();

            certificate.CertificateNumber = CertificateNumber;
            certificate.Type = SelectedCertificateType;
            certificate.PatientId = SelectedPatient!.Id;
            certificate.DoctorId = SelectedDoctor!.Id;
            certificate.IssueDate = IssueDate;
            certificate.ValidFrom = ValidFrom;
            certificate.ValidUntil = ValidUntil;
            certificate.DiagnosisId = SelectedDiagnosis?.Id;
            certificate.Content = GenerateCertificateContent();
            certificate.Purpose = GetCertificatePurpose();
            certificate.CreatedBy = _sessionService.CurrentUser!.Id;

            if (_editingCertificate != null)
            {
                await _certificateRepository.UpdateByIdAsync(certificate.Id, certificate);
            }
            else
            {
                await _certificateRepository.CreateAsync(certificate);
                _editingCertificate = certificate;
            }

            return certificate;
        }

        private string GenerateCertificateContent()
        {
            var content = PreviewText;

            content = $"МЕДИЧНА ДОВІДКА\n\n{content}\n\n" +
                     $"М.П.\nПідпис лікаря ___________";

            return content;
        }

        private string GetCertificatePurpose()
        {
            return SelectedCertificateType switch
            {
                CertificateType.SickLeave => "Для пред'явлення за місцем роботи",
                CertificateType.Pool => "Для відвідування басейну",
                CertificateType.Health => "Для пред'явлення за вимогою",
                CertificateType.Driver => "Для отримання/заміни водійського посвідчення",
                CertificateType.Work => "Для працевлаштування",
                CertificateType.Education => "Для навчального закладу",
                CertificateType.Vaccination => "Підтвердження вакцинації",
                CertificateType.Dispensary => "Для диспансерного обліку",
                _ => "Загального призначення"
            };
        }

        private async Task SaveAndPrintAsync()
        {
            try
            {
                await SaveCertificateAsync();

                if (_editingCertificate == null)
                {
                    ShowErrorBar("Помилка: довідка не збережена");
                    return;
                }

                var saveDialog = new Avalonia.Controls.SaveFileDialog
                {
                    Title = "Зберегти PDF довідку",
                    DefaultExtension = "pdf",
                    Filters = new List<Avalonia.Controls.FileDialogFilter>
                    {
                        new() { Name = "PDF файли", Extensions = new List<string> { "pdf" } }
                    },
                    InitialFileName = $"{SelectedPatient.FullName}.pdf"
                };

                var window = Avalonia.Application.Current?.ApplicationLifetime is
                    Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;

                var result = await saveDialog.ShowAsync(window);

                if (!string.IsNullOrEmpty(result))
                {
                    ShowInfoBar("Генерація PDF...");
                    var certificateDetail = await CreateCertificateDetailDtoAsync();
                    await _pdfService.GenerateCertificatePdfAsync(certificateDetail, result);

                    var openFolder = await _dialogService.ShowConfirmAsync(
                        "PDF створено",
                        $"Довідку успішно експортовано в PDF:\n{result}\n\nВідкрити папку з файлом?");

                    if (openFolder)
                    {
                        var folderPath = System.IO.Path.GetDirectoryName(result);
                        if (!string.IsNullOrEmpty(folderPath))
                        {
                            try
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = folderPath,
                                    UseShellExecute = true,
                                    Verb = "open"
                                });
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Error opening folder");
                                ShowErrorBar($"Не вдалося відкрити папку: {ex.Message}");
                            }
                        }
                    }

                    _navigationService.NavigateBack();
                }
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка експорту в PDF: {ex.Message}");
            }
        }

        private async Task<Core.Models.DTO.CertificateDetail> CreateCertificateDetailDtoAsync()
        {
            string doctorSpecialty = string.Empty;
            if (!string.IsNullOrEmpty(SelectedDoctor?.SpecialtyId))
            {
                try
                {
                    var specialty = await _specialtyRepository.GetByIdAsync(SelectedDoctor.SpecialtyId);
                    doctorSpecialty = specialty?.Name ?? "Не вказано";
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error loading specialty for doctor {DoctorId}", SelectedDoctor.Id);
                    doctorSpecialty = "Не вказано";
                }
            }

            var certificateDetail = new Core.Models.DTO.CertificateDetail
            {
                Id = _editingCertificate?.Id ?? string.Empty,
                CertificateNumber = CertificateNumber,
                Type = ConvertCertificateTypeToString(SelectedCertificateType),
                PatientId = SelectedPatient!.Id,
                PatientName = SelectedPatient.FullName,
                DoctorId = SelectedDoctor!.Id,
                DoctorName = SelectedDoctor.FullName,
                DoctorSpecialty = doctorSpecialty,
                IssueDate = IssueDate,
                ValidFrom = ValidFrom,
                ValidUntil = ValidUntil,
                DiagnosisId = SelectedDiagnosis?.Id,
                DiagnosisCode = SelectedDiagnosis?.IcdCode,
                DiagnosisName = SelectedDiagnosis?.Name ?? DiagnosisText,
                Content = AdditionalInfo,
                Purpose = GetCertificatePurpose(),
                CreatedById = _sessionService.CurrentUser?.Id ?? string.Empty,
                CreatedByName = _sessionService.CurrentUser?.FullName ?? string.Empty
            };

            return certificateDetail;
        }

        private string ConvertCertificateTypeToString(CertificateType type)
        {
            return type switch
            {
                CertificateType.SickLeave => "sick_leave",
                CertificateType.Health => "health",
                CertificateType.Vaccination => "vaccination",
                CertificateType.Driver => "driver",
                CertificateType.Pool => "pool",
                CertificateType.Work => "work",
                CertificateType.Education => "education",
                CertificateType.Dispensary => "dispensary",
                _ => "health"
            };
        }

        private void Cancel()
        {
            _navigationService.NavigateBack();
        }

        private async Task SelectIssueDateAsync()
        {
            try
            {
                var selectedDate = await _dialogService.ShowDatePickerAsync("Оберіть дату видачі", IssueDate);
                if (selectedDate.HasValue)
                {
                    IssueDate = selectedDate.Value;
                }
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка вибору дати: {ex.Message}");
                Log.Error(ex, "Error selecting issue date");
            }
        }

        private async Task SelectValidFromAsync()
        {
            try
            {
                var selectedDate = await _dialogService.ShowDatePickerAsync("Оберіть дату початку дії", ValidFrom);
                if (selectedDate.HasValue)
                {
                    ValidFrom = selectedDate.Value;
                }
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка вибору дати: {ex.Message}");
                Log.Error(ex, "Error selecting valid from date");
            }
        }

        private async Task SelectValidUntilAsync()
        {
            try
            {
                var selectedDate = await _dialogService.ShowDatePickerAsync("Оберіть дату закінчення дії", ValidUntil);
                if (selectedDate.HasValue)
                {
                    ValidUntil = selectedDate.Value;
                }
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка вибору дати: {ex.Message}");
                Log.Error(ex, "Error selecting valid until date");
            }
        }

        private void LoadCertificateData(Certificate certificate)
        {
            CertificateNumber = certificate.CertificateNumber;
            SelectedCertificateType = certificate.Type;
            IssueDate = certificate.IssueDate;
            ValidFrom = certificate.ValidFrom;
            ValidUntil = certificate.ValidUntil;

            Task.Run(async () =>
            {
                var patient = await _patientRepository.GetByIdAsync(certificate.PatientId);
                var doctor = await _doctorRepository.GetByIdAsync(certificate.DoctorId);

                SelectedPatient = patient;
                SelectedDoctor = doctor;

                if (!string.IsNullOrEmpty(certificate.DiagnosisId))
                {
                    var diagnosis = await _diagnosisRepository.GetByIdAsync(certificate.DiagnosisId);
                    SelectedDiagnosis = diagnosis;
                }
            });
        }

        private void SetupValidation()
        {
            this.ValidationRule(
                vm => vm.SelectedPatient,
                patient => patient != null,
                "Оберіть пацієнта");

            this.ValidationRule(
                vm => vm.CertificateNumber,
                number => !string.IsNullOrWhiteSpace(number),
                "Вкажіть номер довідки");

            this.ValidationRule(
                vm => vm.SelectedDoctor,
                doctor => doctor != null,
                "Оберіть лікаря");

            this.ValidationRule(
                vm => vm.IssueDate,
                date => date >= DateTime.Today.AddYears(-1) && date <= DateTime.Today,
                "Дата видачі має бути між минулим роком та сьогодні");
        }

        private void UpdateValidationErrors()
        {
            ValidationErrors.Clear();

            var validationText = ValidationContext.Text.ToSingleLine();
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
    }

    public interface ICertificateRepository : IBaseRepository<Certificate>
    {
        Task<Certificate?> GetByCertificateNumberAsync(string certificateNumber);
        Task<IEnumerable<Certificate>> GetByPatientAsync(string patientId);
        Task<IEnumerable<Certificate>> GetByDoctorAsync(string doctorId);
        Task<IEnumerable<Certificate>> GetByTypeAsync(CertificateType type);
        Task<Certificate?> GetLastCertificateAsync();
    }

    public class CertificateRepository : BaseRepository<Certificate>, ICertificateRepository
    {
        public CertificateRepository(IDatabaseContext context, ILogger logger)
            : base(context, "certificates", logger) { }

        public async Task<Certificate?> GetByCertificateNumberAsync(string certificateNumber)
        {
            return await Collection.Find(c => c.CertificateNumber == certificateNumber).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Certificate>> GetByPatientAsync(string patientId)
        {
            return await Collection
                .Find(c => c.PatientId == patientId)
                .SortByDescending(c => c.IssueDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Certificate>> GetByDoctorAsync(string doctorId)
        {
            return await Collection
                .Find(c => c.DoctorId == doctorId)
                .SortByDescending(c => c.IssueDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Certificate>> GetByTypeAsync(CertificateType type)
        {
            return await Collection
                .Find(c => c.Type == type)
                .SortByDescending(c => c.IssueDate)
                .ToListAsync();
        }

        public async Task<Certificate?> GetLastCertificateAsync()
        {
            return await Collection
                .Find(_ => true)
                .SortByDescending(c => c.CertificateNumber)
                .FirstOrDefaultAsync();
        }
    }

    public interface IDiagnosisRepository : IBaseRepository<Diagnosis>
    {
        Task<Diagnosis?> GetByIcdCodeAsync(string icdCode);
        Task<IEnumerable<Diagnosis>> GetByCategoryAsync(string category);
        Task<IEnumerable<Diagnosis>> SearchDiagnosesAsync(string searchTerm);
    }

    public class DiagnosisRepository : BaseRepository<Diagnosis>, IDiagnosisRepository
    {
        public DiagnosisRepository(IDatabaseContext context, ILogger logger)
            : base(context, "diagnoses", logger) { }

        public async Task<Diagnosis?> GetByIcdCodeAsync(string icdCode)
        {
            return await Collection.Find(d => d.IcdCode == icdCode).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Diagnosis>> GetByCategoryAsync(string category)
        {
            if (category == "Всі")
                return await Collection.Find(_ => true).ToListAsync();

            return await Collection.Find(d => d.Category == category).ToListAsync();
        }

        public async Task<IEnumerable<Diagnosis>> SearchDiagnosesAsync(string searchTerm)
        {
            var filter = Builders<Diagnosis>.Filter.Or(
                Builders<Diagnosis>.Filter.Regex(d => d.IcdCode, new BsonRegularExpression(searchTerm, "i")),
                Builders<Diagnosis>.Filter.Regex(d => d.Name, new BsonRegularExpression(searchTerm, "i"))
            );
            return await Collection.Find(filter).ToListAsync();
        }
    }
}