using Avalonia.Controls.Notifications;
using PMS.Core.Models.DTO;
using PMS.Core.Repositories.Interfaces;
using PMS.Core.Services.Interfaces;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using PMS.Core.Enums.General;

namespace PMS.ViewModels.Dialogs
{
    public class ExaminationQueriesDialogViewModel : BaseDialogViewModel<bool>
    {
        private readonly IExaminationRepository _examinationRepository;
        private readonly IDialogService _dialogService;

        private ObservableCollection<DateTime> _selectedExaminationDates = new();
        private ObservableCollection<PatientWithMultipleDoctors> _patientsWithMultipleDoctors = new();

        private string _diagnosisSearchText = string.Empty;
        private ObservableCollection<DateTime> _selectedDiagnosisDates = new();
        private long _diagnosisPatientCount;
        private ObservableCollection<PatientDiagnosisInfo> _patientsDiagnosisResults = new();

        private int _selectedTabIndex = 0;

        public ExaminationQueriesDialogViewModel(
            IExaminationRepository examinationRepository,
            IDialogService dialogService)
        {
            _examinationRepository = examinationRepository;
            _dialogService = dialogService;

            Title = "Запити обстежень";

            SearchMultipleDoctorsCommand = ReactiveCommand.CreateFromTask(SearchPatientsWithMultipleDoctorsAsync);
            SearchDiagnosisCommand = ReactiveCommand.CreateFromTask(SearchPatientsByDiagnosisAsync);
            ClearFiltersCommand = ReactiveCommand.Create(ClearFilters);
        }

        #region Properties

        public void UpdateExaminationSelectedDates(IEnumerable<DateTime> dates)
        {
            SelectedExaminationDates.Clear();
            foreach (var date in dates)
            {
                SelectedExaminationDates.Add(date);
            }
        }

        public void UpdateDiagnosisSelectedDates(IEnumerable<DateTime> dates)
        {
            SelectedDiagnosisDates.Clear();
            foreach (var date in dates)
            {
                SelectedDiagnosisDates.Add(date);
            }
        }

        public ObservableCollection<DateTime> SelectedExaminationDates
        {
            get => _selectedExaminationDates;
            set => this.RaiseAndSetIfChanged(ref _selectedExaminationDates, value);
        }

        public ObservableCollection<PatientWithMultipleDoctors> PatientsWithMultipleDoctors
        {
            get => _patientsWithMultipleDoctors;
            set => this.RaiseAndSetIfChanged(ref _patientsWithMultipleDoctors, value);
        }

        public string DiagnosisSearchText
        {
            get => _diagnosisSearchText;
            set => this.RaiseAndSetIfChanged(ref _diagnosisSearchText, value);
        }

        public ObservableCollection<DateTime> SelectedDiagnosisDates
        {
            get => _selectedDiagnosisDates;
            set => this.RaiseAndSetIfChanged(ref _selectedDiagnosisDates, value);
        }

        public long DiagnosisPatientCount
        {
            get => _diagnosisPatientCount;
            set => this.RaiseAndSetIfChanged(ref _diagnosisPatientCount, value);
        }

        public ObservableCollection<PatientDiagnosisInfo> PatientsDiagnosisResults
        {
            get => _patientsDiagnosisResults;
            set => this.RaiseAndSetIfChanged(ref _patientsDiagnosisResults, value);
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => this.RaiseAndSetIfChanged(ref _selectedTabIndex, value);
        }

        public DateTime MaximumDate => DateTime.Today;

        #endregion

        #region Commands

        public ReactiveCommand<Unit, Unit> SearchMultipleDoctorsCommand { get; }
        public ReactiveCommand<Unit, Unit> SearchDiagnosisCommand { get; }
        public ReactiveCommand<Unit, Unit> ClearFiltersCommand { get; }

        #endregion

        #region Methods

        private async Task SearchPatientsWithMultipleDoctorsAsync()
        {
            try
            {
                if (SelectedExaminationDates == null || SelectedExaminationDates.Count == 0)
                {
                    await _dialogService.ShowWarningAsync(
                        "Увага",
                        "Оберіть дати для пошуку на календарі");
                    return;
                }

                IsBusy = true;
                BusyMessage = "Пошук пацієнтів з декількома лікарями...";

                var startDate = SelectedExaminationDates.Min();
                var endDate = SelectedExaminationDates.Max();

                var results = await _examinationRepository.GetPatientsWithMultipleDoctorsPerWeekAsync(
                    startDate,
                    endDate);

                PatientsWithMultipleDoctors.Clear();
                foreach (var result in results)
                {
                    PatientsWithMultipleDoctors.Add(result);
                }

                _dialogService.ShowNotification(
                    "Результати пошуку",
                    $"Знайдено {results.Count} пацієнтів, які обстежувалися у більше ніж 2 лікарів",
                    NotificationPosition.BottomRight,
                    NotificationSeverity.Success,
                    10);

                Log.Information("Found {Count} patients with multiple doctors between {Start} and {End}",
                    results.Count, startDate.ToShortDateString(), endDate.ToShortDateString());
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(
                    "Помилка пошуку",
                    $"Не вдалося виконати запит: {ex.Message}",
                    ex);

                Log.Error(ex, "Error searching patients with multiple doctors");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SearchPatientsByDiagnosisAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(DiagnosisSearchText))
                {
                    await _dialogService.ShowWarningAsync(
                        "Увага",
                        "Введіть назву діагнозу для пошуку");
                    return;
                }

                if (SelectedDiagnosisDates == null || SelectedDiagnosisDates.Count == 0)
                {
                    await _dialogService.ShowWarningAsync(
                        "Увага",
                        "Оберіть дати для пошуку на календарі");
                    return;
                }

                IsBusy = true;
                BusyMessage = $"Пошук пацієнтів з діагнозом '{DiagnosisSearchText}'...";
          
                var startDate = SelectedDiagnosisDates.Min();
                var endDate = SelectedDiagnosisDates.Max();

                var count = await _examinationRepository.CountPatientsByDiagnosisInMonthAsync(
                    DiagnosisSearchText,
                    startDate,
                    endDate);

                DiagnosisPatientCount = count;

                var results = await _examinationRepository.GetPatientsByDiagnosisInMonthAsync(
                    DiagnosisSearchText,
                    startDate,
                    endDate);

                PatientsDiagnosisResults.Clear();
                foreach (var result in results)
                {
                    PatientsDiagnosisResults.Add(result);
                }

                _dialogService.ShowNotification(
                    "Результати пошуку",
                    $"Знайдено {count} пацієнтів з діагнозом '{DiagnosisSearchText}'",
                    NotificationPosition.BottomRight,
                    NotificationSeverity.Success,
                    10);

                Log.Information("Found {Count} patients with diagnosis {Diagnosis} between {Start} and {End}",
                    count, DiagnosisSearchText, startDate.ToShortDateString(), endDate.ToShortDateString());
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(
                    "Помилка пошуку",
                    $"Не вдалося виконати запит: {ex.Message}",
                    ex);

                Log.Error(ex, "Error searching patients by diagnosis");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ClearFilters()
        {
            List<string> options = new List<string>
            {
                "Очистити всі фільтри та результати",
                "s"
            };
            _dialogService.ShowMultiSelectionDialogAsync("123", "123", options, (s => s));

            SelectedExaminationDates.Clear();
            PatientsWithMultipleDoctors.Clear();

            DiagnosisSearchText = string.Empty;
            SelectedDiagnosisDates.Clear();
            DiagnosisPatientCount = 0;
            PatientsDiagnosisResults.Clear();

            _dialogService.ShowNotification(
                "Фільтри очищено",
                "Всі фільтри та результати скинуті",
                NotificationPosition.BottomRight,
                NotificationSeverity.Information,
                3);

            Log.Information("Examination queries filters cleared");
        }

      
        protected override bool GetResult()
        {
            return true;
        }

        #endregion
    }
}