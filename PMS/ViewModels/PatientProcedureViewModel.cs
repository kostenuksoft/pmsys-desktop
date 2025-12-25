using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using ClosedXML.Excel;
using Newtonsoft.Json;
using PMS.Core.Enums.General;
using PMS.Core.Models;
using PMS.Core.Models.Common;
using PMS.Core.Repositories;
using PMS.Core.Repositories.Interfaces;
using PMS.Core.Services.Interfaces;
using ReactiveUI;
using Serilog;

namespace PMS.ViewModels;

public class PatientProceduresViewModel : PageViewModelBase
{
    private readonly IPatientProcedureRepository _patientProcedureRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IProcedureRepository _procedureRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly ISessionService _sessionService;
    private readonly IDialogService _dialogService;
    private readonly IWindowService _windowService;

    private ObservableCollection<PatientProcedureWithDetails> _patientProcedures = [];
    private PatientProcedureWithDetails? _selectedPatientProcedure;
    private string _searchText = string.Empty;
    private string _statusFilter = "Всі";
    private string _patientFilter = string.Empty;
    private string _doctorFilter = string.Empty;
    private DateTime? _startDateFilter;
    private DateTime? _endDateFilter;

    private int _currentPage = 1;
    private int _totalPages = 1;
    private int _pageSize;
    private long _totalProcedures;

    private CancellationTokenSource? _loadCancellationTokenSource;

    private Dictionary<string, Patient> _patientsCache = new();
    private Dictionary<string, Procedure> _proceduresCache = new();
    private Dictionary<string, Doctor> _doctorsCache = new();
    private Dictionary<string, Room> _roomsCache = new();

    public PatientProceduresViewModel(
        IPatientProcedureRepository patientProcedureRepository,
        IPatientRepository patientRepository,
        IProcedureRepository procedureRepository,
        IDoctorRepository doctorRepository,
        IRoomRepository roomRepository,
        ISessionService sessionService,
        IDialogService dialogService, 
        IWindowService windowService)
    {
        _patientProcedureRepository = patientProcedureRepository;
        _patientRepository = patientRepository;
        _procedureRepository = procedureRepository;
        _doctorRepository = doctorRepository;
        _roomRepository = roomRepository;
        _sessionService = sessionService;
        _dialogService = dialogService;
        _windowService = windowService;

        _pageSize = App.ApplicationSettings.PageSize;

        SetupCommands();
        SetupReactiveFilters();
    }

    #region Properties

    public ObservableCollection<PatientProcedureWithDetails> PatientProcedures
    {
        get => _patientProcedures;
        set => SetAndRiseProperty(ref _patientProcedures, value);
    }

    public PatientProcedureWithDetails? SelectedPatientProcedure
    {
        get => _selectedPatientProcedure;
        set => SetAndRiseProperty(ref _selectedPatientProcedure, value);
    }

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

    public string PatientFilter
    {
        get => _patientFilter;
        set => SetAndRiseProperty(ref _patientFilter, value);
    }

    public string DoctorFilter
    {
        get => _doctorFilter;
        set => SetAndRiseProperty(ref _doctorFilter, value);
    }

    public DateTime? StartDateFilter
    {
        get => _startDateFilter;
        set => SetAndRiseProperty(ref _startDateFilter, value);
    }

    public DateTime? EndDateFilter
    {
        get => _endDateFilter;
        set => SetAndRiseProperty(ref _endDateFilter, value);
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

    public int PageSize
    {
        get => _pageSize;
        set => SetAndRiseProperty(ref _pageSize, value);
    }

    public long TotalProcedures
    {
        get => _totalProcedures;
        set => SetAndRiseProperty(ref _totalProcedures, value);
    }

    public string DateRangeDisplay
    {
        get
        {
            if (StartDateFilter.HasValue && EndDateFilter.HasValue)
            {
                if (StartDateFilter.Value.Date == EndDateFilter.Value.Date)
                {
                    return StartDateFilter.Value.ToString("dd.MM.yyyy");
                }
                return $"{StartDateFilter.Value:dd.MM.yyyy} — {EndDateFilter.Value:dd.MM.yyyy}";
            }
            return "Весь період";
        }
    }

    public bool CanGoToPreviousPage => CurrentPage > 1;
    public bool CanGoToNextPage => CurrentPage < TotalPages;

    public List<string> StatusFilterOptions { get; } = new()
    {
        "Всі",
        "Призначено",
        "Заплановано",
        "Виконано",
        "Скасовано"
    };

    public override string TabHeader => "Процедури пацієнтів";
    public string? TabIcon => "Target";

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> RefreshCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> AddPatientProcedureCommand { get; private set; } = null!;
    public ReactiveCommand<PatientProcedureWithDetails?, Unit> EditPatientProcedureCommand { get; private set; } = null!;
    public ReactiveCommand<PatientProcedureWithDetails?, Unit> DeletePatientProcedureCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> ClearFiltersCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> SelectDateRangeCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> ExportToJsonCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> ExportToCsvCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> ExportToExcelCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> PrevPageCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> NextPageCommand { get; private set; } = null!;

    #endregion

    #region Setup

    private void SetupCommands()
    {
        RefreshCommand = ReactiveCommand.CreateFromTask(LoadCurrentPageAsync);
        AddPatientProcedureCommand = ReactiveCommand.CreateFromTask(AddPatientProcedureAsync);
        EditPatientProcedureCommand = ReactiveCommand.CreateFromTask<PatientProcedureWithDetails?>(EditPatientProcedureAsync);
        DeletePatientProcedureCommand = ReactiveCommand.CreateFromTask<PatientProcedureWithDetails?>(DeletePatientProcedureAsync);
        ClearFiltersCommand = ReactiveCommand.Create(ClearFilters);
        SelectDateRangeCommand = ReactiveCommand.CreateFromTask(SelectDateRangeAsync);
        ExportToJsonCommand = ReactiveCommand.CreateFromTask(ExportToJsonAsync);
        ExportToCsvCommand = ReactiveCommand.CreateFromTask(ExportToCsvAsync);
        ExportToExcelCommand = ReactiveCommand.CreateFromTask(ExportToExcelAsync);

        PrevPageCommand = ReactiveCommand.CreateFromTask(GoToPreviousPage,
            this.WhenAnyValue(x => x.CanGoToPreviousPage));
        NextPageCommand = ReactiveCommand.CreateFromTask(GoToNextPage,
            this.WhenAnyValue(x => x.CanGoToNextPage));
    }

    private void SetupReactiveFilters()
    {
        this.WhenAnyValue(
                x => x.SearchText,
                x => x.StatusFilter,
                x => x.PatientFilter,
                x => x.DoctorFilter,
                x => x.StartDateFilter,
                x => x.EndDateFilter)
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(700))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => OnFiltersChanged());

        this.WhenAnyValue(x => x.CurrentPage)
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async _ => await LoadCurrentPageAsync());
    }

    #endregion

    #region Initialization

    public override async void Initialize()
    {
        await LoadLookupDataAsync();
        await LoadCurrentPageAsync();
    }

    private async Task LoadLookupDataAsync()
    {
        try
        {
            var patients = await _patientRepository.GetAllAsync();
            _patientsCache = patients.ToDictionary(p => p.Id, p => p);

            var procedures = await _procedureRepository.GetAllAsync();
            _proceduresCache = procedures.ToDictionary(p => p.Id, p => p);

            var doctors = await _doctorRepository.GetAllAsync();
            _doctorsCache = doctors.ToDictionary(d => d.Id, d => d);

            var rooms = await _roomRepository.GetAllAsync();
            _roomsCache = rooms.ToDictionary(r => r.Id, r => r);

            Log.Information("Loaded lookup data: {Patients} patients, {Procedures} procedures, {Doctors} doctors, {Rooms} rooms",
                _patientsCache.Count, _proceduresCache.Count, _doctorsCache.Count, _roomsCache.Count);
        }
        catch (Exception ex)
        {
            _dialogService.ShowNotification(
                TabHeader,
                $"Помилка завантаження довідкових даних: {ex.Message}",
                NotificationPosition.BottomRight,
                NotificationSeverity.Error);
            Log.Error(ex, "Error loading lookup data");
        }
    }

    #endregion

    #region Data Loading

    private async Task LoadCurrentPageAsync()
    {
        _loadCancellationTokenSource?.Cancel();
        _loadCancellationTokenSource = new CancellationTokenSource();
        var token = _loadCancellationTokenSource.Token;

        try
        {
            IsBusy = true;
            BusyMessage = "Завантаження процедур...";

            ProcedureStatus? statusFilter = StatusFilter switch
            {
                "Призначено" => ProcedureStatus.Prescribed,
                "Заплановано" => ProcedureStatus.Scheduled,
                "Виконано" => ProcedureStatus.Completed,
                "Скасовано" => ProcedureStatus.Cancelled,
                _ => null
            };

            var (procedures, totalCount) = await _patientProcedureRepository.GetPagedPatientProceduresAsync(
                CurrentPage,
                PageSize,
                SearchText,
                null, 
                null, 
                null,
                statusFilter,
                StartDateFilter,
                EndDateFilter);

            if (token.IsCancellationRequested) return;

            TotalProcedures = totalCount;
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            if (TotalPages == 0) TotalPages = 1;

            var enrichedProcedures = procedures.Select(EnrichPatientProcedure).ToList();

            if (!string.IsNullOrWhiteSpace(PatientFilter))
            {
                var filter = PatientFilter.ToLower();
                enrichedProcedures = enrichedProcedures
                    .Where(p => p.PatientName.ToLower().Contains(filter))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(DoctorFilter))
            {
                var filter = DoctorFilter.ToLower();
                enrichedProcedures = enrichedProcedures
                    .Where(p => p.PrescribedByName.ToLower().Contains(filter))
                    .ToList();
            }

            PatientProcedures.Clear();
            foreach (var procedure in enrichedProcedures)
            {
                PatientProcedures.Add(procedure);
            }

            this.RaisePropertyChanged(nameof(CanGoToPreviousPage));
            this.RaisePropertyChanged(nameof(CanGoToNextPage));

            var prescribedCount = await _patientProcedureRepository.CountByStatusAsync(ProcedureStatus.Prescribed);
            var scheduledCount = await _patientProcedureRepository.CountByStatusAsync(ProcedureStatus.Scheduled);
            var completedCount = await _patientProcedureRepository.CountByStatusAsync(ProcedureStatus.Completed);
            var cancelledCount = await _patientProcedureRepository.CountByStatusAsync(ProcedureStatus.Cancelled);

            _dialogService.ShowNotification(
                TabHeader,
                $"Завантажено {PatientProcedures.Count} з {TotalProcedures} процедур\n" +
                $"Призначено: {prescribedCount} Заплановано: {scheduledCount}\nВиконано: {completedCount} Скасовано: {cancelledCount}",
                NotificationPosition.BottomRight,
                NotificationSeverity.Information,
                30);

            Log.Information("Loaded page {Page} with {Count} patient procedures",
                CurrentPage, PatientProcedures.Count);
        }
        catch (OperationCanceledException)
        {
            Log.Information("Page load cancelled");
        }
        catch (Exception ex)
        {
            _dialogService.ShowNotification(
                TabHeader,
                $"Помилка завантаження: {ex.Message}",
                NotificationPosition.BottomRight,
                NotificationSeverity.Error);
            Log.Error(ex, "Error loading page");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private PatientProcedureWithDetails EnrichPatientProcedure(PatientProcedure procedure)
    {
        var enriched = new PatientProcedureWithDetails
        {
            Id = procedure.Id,
            PatientId = procedure.PatientId,
            ProcedureId = procedure.ProcedureId,
            PrescribedBy = procedure.PrescribedBy,
            PrescribedDate = procedure.PrescribedDate,
            PerformedBy = procedure.PerformedBy,
            PerformedDate = procedure.PerformedDate,
            RoomId = procedure.RoomId,
            Status = procedure.Status,
            Results = procedure.Results,
            Notes = procedure.Notes
        };

        if (_patientsCache.TryGetValue(procedure.PatientId, out var patient))
        {
            enriched.PatientName = patient.FullName;
            enriched.PatientMedicalRecordNumber = patient.MedicalRecordNumber;
        }

        if (_proceduresCache.TryGetValue(procedure.ProcedureId, out var proc))
        {
            enriched.ProcedureName = proc.Name;
            enriched.ProcedureCode = proc.ProcedureCode;
            enriched.ProcedureCost = proc.Price;
        }

        if (_doctorsCache.TryGetValue(procedure.PrescribedBy, out var prescribedDoc))
        {
            enriched.PrescribedByName = prescribedDoc.FullName;
        }

        if (!string.IsNullOrEmpty(procedure.PerformedBy) &&
            _doctorsCache.TryGetValue(procedure.PerformedBy, out var performedDoc))
        {
            enriched.PerformedByName = performedDoc.FullName;
        }

        if (!string.IsNullOrEmpty(procedure.RoomId) &&
            _roomsCache.TryGetValue(procedure.RoomId, out var room))
        {
            enriched.RoomNumber = room.RoomNumber;
        }

        return enriched;
    }

    #endregion

    #region CRUD Operations

    private async Task AddPatientProcedureAsync()
    {
        try
        {
            _dialogService.ShowNotification(
                TabHeader,
                "Додавання нової процедури...",
                NotificationPosition.BottomRight,
                NotificationSeverity.Information);
            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            _dialogService.ShowNotification(
                TabHeader,
                $"Помилка створення: {ex.Message}",
                NotificationPosition.BottomRight,
                NotificationSeverity.Error);
            Log.Error(ex, "Error creating patient procedure");
        }
    }

    private async Task EditPatientProcedureAsync(PatientProcedureWithDetails? procedure)
    {
        try
        {
            if (procedure == null) return;

            _dialogService.ShowNotification(
                TabHeader,
                "Редагування процедури...",
                NotificationPosition.BottomRight,
                NotificationSeverity.Information);
         
            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            _dialogService.ShowNotification(
                TabHeader,
                $"Помилка редагування: {ex.Message}",
                NotificationPosition.BottomRight,
                NotificationSeverity.Error);
            Log.Error(ex, "Error editing patient procedure");
        }
    }

    private async Task DeletePatientProcedureAsync(PatientProcedureWithDetails? procedure)
    {
        try
        {
            if (procedure == null) return;

            var confirmed = await _dialogService.ShowConfirmAsync(
                "Видалення процедури",
                $"Ви впевнені, що хочете видалити процедуру '{procedure.ProcedureName}' для пацієнта {procedure.PatientName}?");

            if (!confirmed) return;

            IsBusy = true;
            BusyMessage = "Видалення процедури...";

            await _patientProcedureRepository.DeleteByIdAsync(procedure.Id);

            await LoadCurrentPageAsync();

            _dialogService.ShowNotification(
                TabHeader,
                $"Процедуру '{procedure.ProcedureName}' видалено",
                NotificationPosition.BottomRight,
                NotificationSeverity.Success);
            Log.Information("Deleted patient procedure {ProcedureId}", procedure.Id);
        }
        catch (Exception ex)
        {
            _dialogService.ShowNotification(
                TabHeader,
                $"Помилка видалення: {ex.Message}",
                NotificationPosition.BottomRight,
                NotificationSeverity.Error);
            Log.Error(ex, "Error deleting patient procedure");
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion

    #region Date Range Selection

    private async Task SelectDateRangeAsync()
    {
        try
        {
            var result = await _dialogService.ShowDateRangePickerAsync("Виберіть період");

            if (result.HasValue)
            {
                StartDateFilter = result.Value.startDate;
                EndDateFilter = result.Value.endDate;

                this.RaisePropertyChanged(nameof(DateRangeDisplay));

                _dialogService.ShowNotification(
                    TabHeader,
                    $"Обрано період: {DateRangeDisplay}",
                    NotificationPosition.BottomRight,
                    NotificationSeverity.Information,
                    3);
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowNotification(
                TabHeader,
                $"Помилка вибору періоду: {ex.Message}",
                NotificationPosition.BottomRight,
                NotificationSeverity.Error);
            Log.Error(ex, "Error selecting date range");
        }
    }

    #endregion

    #region Export

    private async Task ExportToJsonAsync()
    {
        try
        {
            var filter = await _dialogService.ShowSaveFileDialogAsync(
                "Експорт у JSON",
                null,
                [new FileDialogFilter { Name = "JSON Files", Extensions = ["json"] }], _windowService.MainWindow );

            if (string.IsNullOrEmpty(filter)) return;

            IsBusy = true;
            BusyMessage = "Експорт у JSON...";

            var allProcedures = PatientProcedures.ToList();
            var json = JsonConvert.SerializeObject(allProcedures, Formatting.Indented);
            await File.WriteAllTextAsync(filter, json);

            _dialogService.ShowNotification(
                TabHeader,
                $"Експортовано {allProcedures.Count} записів у JSON",
                NotificationPosition.BottomRight,
                NotificationSeverity.Success);
            Log.Information("Exported {Count} patient procedures to JSON", allProcedures.Count);
        }
        catch (Exception ex)
        {
            _dialogService.ShowNotification(
                TabHeader,
                $"Помилка експорту: {ex.Message}",
                NotificationPosition.BottomRight,
                NotificationSeverity.Error);
            Log.Error(ex, "Error exporting to JSON");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportToCsvAsync()
    {
        try
        {
            var filter = await _dialogService.ShowSaveFileDialogAsync(
                "Експорт у CSV",
                null,
                new[] { new FileDialogFilter { Name = "CSV Files", Extensions = new[] { "csv" } } }, _windowService.MainWindow);

            if (string.IsNullOrEmpty(filter)) return;

            IsBusy = true;
            BusyMessage = "Експорт у CSV...";

            var csv = new StringBuilder();
            csv.AppendLine("Пацієнт,Мед.картка,Процедура,Код,Вартість,Призначив,Дата призначення,Виконав,Дата виконання,Кабінет,Статус,Результати,Примітки");

            foreach (var proc in PatientProcedures)
            {
                csv.AppendLine($"\"{proc.PatientName}\",\"{proc.PatientMedicalRecordNumber}\",\"{proc.ProcedureName}\",\"{proc.ProcedureCode}\"," +
                               $"{proc.ProcedureCost},\"{proc.PrescribedByName}\",\"{proc.PrescribedDate:dd.MM.yyyy}\"," +
                               $"\"{proc.PerformedByName}\",\"{proc.PerformedDate?.ToString("dd.MM.yyyy")}\",\"{proc.RoomNumber}\"," +
                               $"\"{proc.StatusDisplay}\",\"{proc.Results}\",\"{proc.Notes}\"");
            }

            await File.WriteAllTextAsync(filter, csv.ToString(), Encoding.UTF8);

            _dialogService.ShowNotification(
                TabHeader,
                $"Експортовано {PatientProcedures.Count} записів у CSV",
                NotificationPosition.BottomRight,
                NotificationSeverity.Success);
            Log.Information("Exported {Count} patient procedures to CSV", PatientProcedures.Count);
        }
        catch (Exception ex)
        {
            _dialogService.ShowNotification(
                TabHeader,
                $"Помилка експорту: {ex.Message}",
                NotificationPosition.BottomRight,
                NotificationSeverity.Error);
            Log.Error(ex, "Error exporting to CSV");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportToExcelAsync()
    {
        try
        {
            var filter = await _dialogService.ShowSaveFileDialogAsync(
                "Експорт у Excel",
                null,
                new[] { new FileDialogFilter { Name = "Excel Files", Extensions = new[] { "xlsx" } } }, _windowService.MainWindow);

            if (string.IsNullOrEmpty(filter)) return;

            IsBusy = true;
            BusyMessage = "Експорт у Excel...";

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Процедури пацієнтів");

            worksheet.Cell(1, 1).Value = "Пацієнт";
            worksheet.Cell(1, 2).Value = "Мед.картка";
            worksheet.Cell(1, 3).Value = "Процедура";
            worksheet.Cell(1, 4).Value = "Код";
            worksheet.Cell(1, 5).Value = "Вартість";
            worksheet.Cell(1, 6).Value = "Призначив";
            worksheet.Cell(1, 7).Value = "Дата призначення";
            worksheet.Cell(1, 8).Value = "Виконав";
            worksheet.Cell(1, 9).Value = "Дата виконання";
            worksheet.Cell(1, 10).Value = "Кабінет";
            worksheet.Cell(1, 11).Value = "Статус";
            worksheet.Cell(1, 12).Value = "Результати";
            worksheet.Cell(1, 13).Value = "Примітки";

            int row = 2;
            foreach (var proc in PatientProcedures)
            {
                worksheet.Cell(row, 1).Value = proc.PatientName;
                worksheet.Cell(row, 2).Value = proc.PatientMedicalRecordNumber;
                worksheet.Cell(row, 3).Value = proc.ProcedureName;
                worksheet.Cell(row, 4).Value = proc.ProcedureCode;
                worksheet.Cell(row, 5).Value = proc.ProcedureCost;
                worksheet.Cell(row, 6).Value = proc.PrescribedByName;
                worksheet.Cell(row, 7).Value = proc.PrescribedDate.ToString("dd.MM.yyyy");
                worksheet.Cell(row, 8).Value = proc.PerformedByName ?? "";
                worksheet.Cell(row, 9).Value = proc.PerformedDate?.ToString("dd.MM.yyyy") ?? "";
                worksheet.Cell(row, 10).Value = proc.RoomNumber ?? "";
                worksheet.Cell(row, 11).Value = proc.StatusDisplay;
                worksheet.Cell(row, 12).Value = proc.Results ?? "";
                worksheet.Cell(row, 13).Value = proc.Notes ?? "";
                row++;
            }

            worksheet.Columns().AdjustToContents();

            var headerRange = worksheet.Range(1, 1, 1, 13);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            workbook.SaveAs(filter);

            _dialogService.ShowNotification(
                TabHeader,
                $"Експортовано {PatientProcedures.Count} записів у Excel",
                NotificationPosition.BottomRight,
                NotificationSeverity.Success);
            Log.Information("Exported {Count} patient procedures to Excel", PatientProcedures.Count);
        }
        catch (Exception ex)
        {
            _dialogService.ShowNotification(
                TabHeader,
                $"Помилка експорту: {ex.Message}",
                NotificationPosition.BottomRight,
                NotificationSeverity.Error);
            Log.Error(ex, "Error exporting to Excel");
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion

    #region Pagination

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

    private void ClearFilters()
    {
        SearchText = string.Empty;
        StatusFilter = "Всі";
        PatientFilter = string.Empty;
        DoctorFilter = string.Empty;
        StartDateFilter = null;
        EndDateFilter = null;

        this.RaisePropertyChanged(nameof(DateRangeDisplay));

        _dialogService.ShowNotification(
            TabHeader,
            "Фільтри очищено",
            NotificationPosition.BottomRight,
            NotificationSeverity.Information,
            3);
    }

    private async Task GoToPreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadCurrentPageAsync();
        }
    }

    private async Task GoToNextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadCurrentPageAsync();
        }
    }

    #endregion

    #region Helper Classes

    public class PatientProcedureWithDetails
    {
        public string Id { get; set; } = string.Empty;
        public string PatientId { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string PatientMedicalRecordNumber { get; set; } = string.Empty;
        public string ProcedureId { get; set; } = string.Empty;
        public string ProcedureName { get; set; } = string.Empty;
        public string ProcedureCode { get; set; } = string.Empty;
        public decimal ProcedureCost { get; set; }
        public string PrescribedBy { get; set; } = string.Empty;
        public string PrescribedByName { get; set; } = string.Empty;
        public DateTime PrescribedDate { get; set; }
        public string? PerformedBy { get; set; }
        public string? PerformedByName { get; set; }
        public DateTime? PerformedDate { get; set; }
        public string? RoomId { get; set; }
        public string? RoomNumber { get; set; }
        public ProcedureStatus Status { get; set; }
        public string? Results { get; set; }
        public string? Notes { get; set; }

        public string StatusDisplay => Status switch
        {
            ProcedureStatus.Prescribed => "Призначено",
            ProcedureStatus.Scheduled => "Заплановано",
            ProcedureStatus.Completed => "Виконано",
            ProcedureStatus.Cancelled => "Скасовано",
            _ => Status.ToString()
        };

        public string PrescribedDateDisplay => PrescribedDate.ToString("dd.MM.yyyy");
        public string PerformedDateDisplay => PerformedDate?.ToString("dd.MM.yyyy") ?? "—";
        public string CostDisplay => $"{ProcedureCost:F2} грн";
    }

    #endregion
}