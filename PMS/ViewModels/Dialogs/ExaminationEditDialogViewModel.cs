using PMS.Core.Models;
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
using PMS.Core.Repositories;

namespace PMS.ViewModels.Dialogs
{
    public class ExaminationEditDialogViewModel : BaseDialogViewModel<Examination>
    {
        private readonly Examination? _originalExamination;
        private readonly bool _isEditMode;
        private readonly IPatientRepository _patientRepository;
        private readonly IDoctorRepository _doctorRepository;
        private readonly IDiagnosisRepository _diagnosisRepository;
        private readonly IDialogService _dialogService;

        private Patient? _selectedPatient;
        private string _patientSearchText = string.Empty;
        private Doctor? _selectedDoctor;
        private DateTime _examinationDate = DateTime.Today;
        private string _anamnesis = string.Empty;
        private string _objectiveStatus = string.Empty;
        private ObservableCollection<Diagnosis> _selectedDiagnoses = new();
        private string _recommendations = string.Empty;
        private bool _hasSickLeave;
        private DateTime? _sickLeaveFrom;
        private DateTime? _sickLeaveTo;
        private bool _hasFollowUp;
        private DateTime? _followUpDate;
        private ObservableCollection<Doctor> _availableDoctors = new();

        public ExaminationEditDialogViewModel(
            Examination? examination,
            IPatientRepository patientRepository,
            IDoctorRepository doctorRepository,
            IDiagnosisRepository diagnosisRepository,
            IDialogService dialogService)
        {
            _originalExamination = examination;
            _isEditMode = examination != null;
            _patientRepository = patientRepository;
            _doctorRepository = doctorRepository;
            _diagnosisRepository = diagnosisRepository;
            _dialogService = dialogService;

            SearchPatientCommand = ReactiveCommand.CreateFromTask(SearchPatientAsync);
            AddDiagnosisCommand = ReactiveCommand.CreateFromTask(AddDiagnosisAsync);
            RemoveDiagnosisCommand = ReactiveCommand.Create<Diagnosis>(RemoveDiagnosis);

            Title = _isEditMode ? "Редагування обстеження" : "Нове обстеження";

            _ = LoadAvailableDoctorsAsync();

            if (_isEditMode && examination != null)
            {
                _ = LoadExaminationDataAsync(examination);
            }

            SetupValidation();
        }

        private void SetupValidation()
        {
            this.ValidationRule(
                vm => vm.SelectedPatient,
                patient => patient != null,
                "Пацієнт обов'язковий");

            this.ValidationRule(
                vm => vm.SelectedDoctor,
                doctor => doctor != null,
                "Лікар обов'язковий");

            this.ValidationRule(
                vm => vm.ExaminationDate,
                date => date <= DateTime.Today,
                "Дата обстеження не може бути в майбутньому");


            this.IsValid()
                .Subscribe(isValid => CanExecutePrimary = isValid);
        }

        private async Task LoadAvailableDoctorsAsync()
        {
            try
            {
                var doctors = await _doctorRepository.GetAllAsync();
                var activeDoctors = doctors.Where(d => d.IsActive).ToList();
                AvailableDoctors = new ObservableCollection<Doctor>(activeDoctors);

                Log.Information("Loaded {Count} active doctors", activeDoctors.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading doctors");
                await _dialogService.ShowErrorAsync("Помилка", $"Помилка завантаження лікарів: {ex.Message}");
            }
        }

        private async Task LoadExaminationDataAsync(Examination examination)
        {
            try
            {
                if (!string.IsNullOrEmpty(examination.PatientId))
                {
                    SelectedPatient = await _patientRepository.GetByIdAsync(examination.PatientId);
                    if (SelectedPatient != null)
                    {
                        PatientSearchText = SelectedPatient.FullName;
                    }
                }

                if (!string.IsNullOrEmpty(examination.DoctorId))
                {
                    var doctor = await _doctorRepository.GetByIdAsync(examination.DoctorId);
                    SelectedDoctor = AvailableDoctors.FirstOrDefault(d => d.Id == examination.DoctorId) ?? doctor;
                }

                ExaminationDate = examination.ExaminationDate;
                Anamnesis = examination.Anamnesis;
                ObjectiveStatus = examination.ObjectiveStatus;
                Recommendations = examination.Recommendations;

                if (examination.DiagnosisIds != null && examination.DiagnosisIds.Count > 0)
                {
                    var diagnoses = new ObservableCollection<Diagnosis>();
                    foreach (var diagnosisId in examination.DiagnosisIds)
                    {
                        var diagnosis = await _diagnosisRepository.GetByIdAsync(diagnosisId);
                        if (diagnosis != null)
                        {
                            diagnoses.Add(diagnosis);
                        }
                    }
                    SelectedDiagnoses = diagnoses;
                }

                if (examination.SickLeaveFrom.HasValue && examination.SickLeaveTo.HasValue)
                {
                    HasSickLeave = true;
                    SickLeaveFrom = examination.SickLeaveFrom;
                    SickLeaveTo = examination.SickLeaveTo;
                }

                if (examination.FollowUpDate.HasValue)
                {
                    HasFollowUp = true;
                    FollowUpDate = examination.FollowUpDate;
                }

                Log.Information("Loaded examination data for editing");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading examination data");
                await _dialogService.ShowErrorAsync("Помилка", $"Помилка завантаження даних обстеження: {ex.Message}");
            }
        }

        protected override Examination? GetResult()
        {
            if (!IsConfirmed)
                return null;

            if (SelectedPatient == null || SelectedDoctor == null)
            {
                Log.Warning("Cannot create examination result: Patient or Doctor is null");
                return null;
            }

            var examination = _isEditMode && _originalExamination != null
                ? _originalExamination
                : new Examination();

            examination.PatientId = SelectedPatient.Id;
            examination.DoctorId = SelectedDoctor.Id;
            examination.ExaminationDate = ExaminationDate;
            examination.Anamnesis = Anamnesis;
            examination.ObjectiveStatus = ObjectiveStatus;
            examination.DiagnosisIds = SelectedDiagnoses.Select(d => d.Id).ToList();
            examination.Recommendations = Recommendations;

            if (HasSickLeave)
            {
                examination.SickLeaveFrom = SickLeaveFrom;
                examination.SickLeaveTo = SickLeaveTo;
            }
            else
            {
                examination.SickLeaveFrom = null;
                examination.SickLeaveTo = null;
            }

            if (HasFollowUp)
            {
                examination.FollowUpDate = FollowUpDate;
            }
            else
            {
                examination.FollowUpDate = null;
            }

            return examination;
        }

        #region Commands

        public ReactiveCommand<Unit, Unit> SearchPatientCommand { get; }
        public ReactiveCommand<Unit, Unit> AddDiagnosisCommand { get; }
        public ReactiveCommand<Diagnosis, Unit> RemoveDiagnosisCommand { get; }

        private async Task SearchPatientAsync()
        {
            try
            {
                var searchDialog = new PatientSearchDialogViewModel(
                    _patientRepository,
                    _dialogService,
                    PatientSearchText);

                var result = await _dialogService.ShowViewModelDialogAsync<PatientSearchDialogViewModel, Patient>(searchDialog);

                if (result != null)
                {
                    SelectedPatient = result;
                    PatientSearchText = result.FullName;
                    Log.Information("Patient selected: {PatientName}", result.FullName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error searching patient");
                await _dialogService.ShowErrorAsync("Помилка", $"Помилка пошуку пацієнта: {ex.Message}");
            }
        }

        private async Task AddDiagnosisAsync()
        {
            try
            {
                var searchDialog = new DiagnosisSearchDialogViewModel(
                    _diagnosisRepository,
                    _dialogService);

                var result = await _dialogService.ShowViewModelDialogAsync<DiagnosisSearchDialogViewModel, Diagnosis>(searchDialog);

                if (result != null)
                {
                    if (SelectedDiagnoses.Any(d => d.Id == result.Id))
                    {
                        await _dialogService.ShowWarningAsync("Попередження", "Цей діагноз вже додано");
                        return;
                    }

                    SelectedDiagnoses.Add(result);
                    Log.Information("Diagnosis added: {DiagnosisName}", result.Name);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error adding diagnosis");
                await _dialogService.ShowErrorAsync("Помилка", $"Помилка додавання діагнозу: {ex.Message}");
            }
        }

        private void RemoveDiagnosis(Diagnosis diagnosis)
        {
            if (diagnosis != null && SelectedDiagnoses.Contains(diagnosis))
            {
                SelectedDiagnoses.Remove(diagnosis);
                Log.Information("Diagnosis removed: {DiagnosisName}", diagnosis.Name);
            }
        }

        #endregion

        #region Properties

        public Patient? SelectedPatient
        {
            get => _selectedPatient;
            set => SetAndRiseProperty(ref _selectedPatient, value);
        }

        public string PatientSearchText
        {
            get => _patientSearchText;
            set => SetAndRiseProperty(ref _patientSearchText, value);
        }

        public Doctor? SelectedDoctor
        {
            get => _selectedDoctor;
            set => SetAndRiseProperty(ref _selectedDoctor, value);
        }

        public DateTime ExaminationDate
        {
            get => _examinationDate;
            set => SetAndRiseProperty(ref _examinationDate, value);
        }

        public string Anamnesis
        {
            get => _anamnesis;
            set => SetAndRiseProperty(ref _anamnesis, value);
        }

        public string ObjectiveStatus
        {
            get => _objectiveStatus;
            set => SetAndRiseProperty(ref _objectiveStatus, value);
        }

        public ObservableCollection<Diagnosis> SelectedDiagnoses
        {
            get => _selectedDiagnoses;
            set => SetAndRiseProperty(ref _selectedDiagnoses, value);
        }

        public string Recommendations
        {
            get => _recommendations;
            set => SetAndRiseProperty(ref _recommendations, value);
        }

        public bool HasSickLeave
        {
            get => _hasSickLeave;
            set => SetAndRiseProperty(ref _hasSickLeave, value);
        }

        public DateTime? SickLeaveFrom
        {
            get => _sickLeaveFrom;
            set => SetAndRiseProperty(ref _sickLeaveFrom, value);
        }

        public DateTime? SickLeaveTo
        {
            get => _sickLeaveTo;
            set => SetAndRiseProperty(ref _sickLeaveTo, value);
        }

        public bool HasFollowUp
        {
            get => _hasFollowUp;
            set => SetAndRiseProperty(ref _hasFollowUp, value);
        }

        public DateTime? FollowUpDate
        {
            get => _followUpDate;
            set => SetAndRiseProperty(ref _followUpDate, value);
        }

        public ObservableCollection<Doctor> AvailableDoctors
        {
            get => _availableDoctors;
            set => SetAndRiseProperty(ref _availableDoctors, value);
        }

        public bool IsEditMode => _isEditMode;

        #endregion
    }

   
}
