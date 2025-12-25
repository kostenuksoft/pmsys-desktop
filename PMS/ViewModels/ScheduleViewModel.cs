using PMS.Core.Enums.General;
using PMS.Core.Models;
using PMS.Core.Repositories.Interfaces;
using PMS.Core.Services;
using PMS.Core.Services.Interfaces;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using PMS.Core.Models.DTO;
using PMS.ViewModels.Dialogs;
using Avalonia.Media;
using PMS.Core.Models.Common;

namespace PMS.ViewModels;


public class ScheduleViewModel : PageViewModelBase, IParameterizedViewModel
{
    private readonly IScheduleDetailsRepository _scheduleDetailsRepository;
    private readonly ISpecialtyRepository _specialtyRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IDialogService _dialogService;

    #region Private Fields

    private Doctor? _selectedDoctor;

    private ViewMode _currentViewMode = ViewMode.Week;

    private DateTime _currentDate = DateTime.Today;

    private ObservableCollection<DayColumn> _dayColumns = [];

    private List<string> _hourSlots = [];

    private ObservableCollection<GridRow> _gridRows = [];
    private ObservableCollection<string> _gridColumnHeaders = [];

    private Dictionary<int, ScheduleCard> _doctorScheduleByDayOfWeek = new();

    private string _periodDisplayText = string.Empty;
    private string _doctorDisplayText = "Лікар не обрано";
    private bool _isDoctorSelected;
    private bool _isLoading;

    #endregion

    public ScheduleViewModel(
        IScheduleDetailsRepository scheduleDetailsRepository,
        ISpecialtyRepository specialtyRepository,
        IDoctorRepository doctorRepository,
        IDialogService dialogService)
    {
        _scheduleDetailsRepository = scheduleDetailsRepository;
        _specialtyRepository = specialtyRepository;
        _doctorRepository = doctorRepository;
        _dialogService = dialogService;

        _hourSlots = GenerateHourSlots();

        SelectDoctorCommand = ReactiveCommand.CreateFromTask(SelectDoctorAsync);
        ChangeModeCommand = ReactiveCommand.Create<ViewMode>(ChangeMode);
        NextPeriodCommand = ReactiveCommand.Create(NextPeriod);
        PrevPeriodCommand = ReactiveCommand.Create(PrevPeriod);
        SlotClickCommand = ReactiveCommand.CreateFromTask<ScheduleSlotData>(OnSlotClick);
        LoadScheduleCommand = ReactiveCommand.CreateFromTask(LoadDoctorScheduleAsync);

        this.WhenAnyValue(
                x => x.SelectedDoctor,
                x => x.CurrentViewMode,
                x => x.CurrentDate,
                (_, _, _) => Unit.Default)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .InvokeCommand(LoadScheduleCommand);

        UpdatePeriodDisplay();
    }

    public ReactiveCommand<Unit, Unit> LoadScheduleCommand { get; }

    #region Properties

 
    public Doctor? SelectedDoctor
    {
        get => _selectedDoctor;
        set
        {
            SetAndRiseProperty(ref _selectedDoctor, value);
            IsDoctorSelected = value != null;
            DoctorDisplayText = value != null
                ? $"{value.FullName} ({value.Phone} - {value.CategoryDisplay})"
                : "Лікар не обрано";
        }
    }

  
    public ViewMode CurrentViewMode
    {
        get => _currentViewMode;
        set
        {
            SetAndRiseProperty(ref _currentViewMode, value);
            UpdatePeriodDisplay();
            this.RaisePropertyChanged(nameof(IsDayMode));
            this.RaisePropertyChanged(nameof(IsWeekMode));
            this.RaisePropertyChanged(nameof(IsMonthMode));
        }
    }

   
    public DateTime CurrentDate
    {
        get => _currentDate;
        set
        {
            SetAndRiseProperty(ref _currentDate, value);
            UpdatePeriodDisplay();
        }
    }

  
    public ObservableCollection<DayColumn> DayColumns
    {
        get => _dayColumns;
        set => SetAndRiseProperty(ref _dayColumns, value);
    }

 
    public ObservableCollection<GridRow> GridRows
    {
        get => _gridRows;
        set => SetAndRiseProperty(ref _gridRows, value);
    }

 
    public ObservableCollection<string> GridColumnHeaders
    {
        get => _gridColumnHeaders;
        set => SetAndRiseProperty(ref _gridColumnHeaders, value);
    }


    public string PeriodDisplayText
    {
        get => _periodDisplayText;
        set => SetAndRiseProperty(ref _periodDisplayText, value);
    }

 
    public string DoctorDisplayText
    {
        get => _doctorDisplayText;
        set => SetAndRiseProperty(ref _doctorDisplayText, value);
    }

 
    public bool IsDoctorSelected
    {
        get => _isDoctorSelected;
        set => SetAndRiseProperty(ref _isDoctorSelected, value);
    }


    public bool IsLoading
    {
        get => _isLoading;
        set => SetAndRiseProperty(ref _isLoading, value);
    }

    public bool IsDayMode => CurrentViewMode == ViewMode.Day;
    public bool IsWeekMode => CurrentViewMode == ViewMode.Week;
    public bool IsMonthMode => CurrentViewMode == ViewMode.Month;

    #endregion

    #region Commands

    public ICommand SelectDoctorCommand { get; }
    public ICommand ChangeModeCommand { get; }
    public ICommand NextPeriodCommand { get; }
    public ICommand PrevPeriodCommand { get; }
    public ICommand SlotClickCommand { get; }

    #endregion

    #region Initialization

    public override async void Initialize()
    {
        CurrentViewMode = ViewMode.Week;
    }

    public void Initialize(object parameter)
    {
        if (parameter is IDictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("DoctorId", out var doctorId) && doctorId is string docId)
            {
                _ = LoadDoctorByIdAsync(docId);
            }
        }
    }

    private async Task LoadDoctorByIdAsync(string doctorId)
    {
        try
        {
            var doctor = await _doctorRepository.GetByIdAsync(doctorId);
            if (doctor != null)
            {
                SelectedDoctor = doctor;
            }
        }
        catch (Exception ex)
        {
            ShowErrorBar($"Помилка завантаження лікаря: {ex.Message}");
        }
    }

    #endregion

    #region Command Handlers

  
    private async Task SelectDoctorAsync()
    {
        try
        {
            var dialogViewModel = new DoctorSearchDialogViewModel(
                _doctorRepository,
                _dialogService);

            var result = await _dialogService.ShowViewModelDialogAsync<DoctorSearchDialogViewModel, Doctor>(
                dialogViewModel);

            if (result != null)
            {
                SelectedDoctor = result;
            }
        }
        catch (Exception ex)
        {
            ShowErrorBar($"Помилка вибору лікаря: {ex.Message}");
        }
    }

  
    private void ChangeMode(ViewMode mode)
    {
        CurrentViewMode = mode;
    }

   
    private void NextPeriod()
    {
        CurrentDate = CurrentViewMode switch
        {
            ViewMode.Day => CurrentDate.AddDays(1),
            ViewMode.Week => CurrentDate.AddDays(7),
            ViewMode.Month => CurrentDate.AddMonths(1),
            _ => CurrentDate
        };
    }


    private void PrevPeriod()
    {
        CurrentDate = CurrentViewMode switch
        {
            ViewMode.Day => CurrentDate.AddDays(-1),
            ViewMode.Week => CurrentDate.AddDays(-7),
            ViewMode.Month => CurrentDate.AddMonths(-1),
            _ => CurrentDate
        };
    }

 
    private async Task OnSlotClick(ScheduleSlotData slot)
    {
        if (!slot.IsWorking || slot.Schedule == null)
            return;

        try
        {
            var detailedCard = await _scheduleDetailsRepository
                .GetDoctorScheduleCardForDateAsync(slot.Schedule.DoctorId, slot.Date);

            if (detailedCard == null)
            {
                ShowErrorBar("Не вдалося завантажити деталі розкладу");
                return;
            }

            var dialogViewModel = new ScheduleDetailsDialogViewModel();
            dialogViewModel.LoadScheduleCard(detailedCard);

            await _dialogService.ShowViewModelDialogAsync<ScheduleDetailsDialogViewModel, bool>(
                dialogViewModel);
        }
        catch (Exception ex)
        {
            ShowErrorBar($"Помилка відображення деталей: {ex.Message}");
        }
    }

    #endregion

    #region Data Loading

 
    private async Task LoadDoctorScheduleAsync()
    {
        if (SelectedDoctor == null)
        {
            GridRows = new ObservableCollection<GridRow>();
            GridColumnHeaders = new ObservableCollection<string>();
            return;
        }

        try
        {
            IsLoading = true;

            var (startDate, endDate) = GetPeriodRange();

            var weeklySchedule = await _scheduleDetailsRepository
                .GetDoctorWeeklyScheduleAsync(SelectedDoctor.Id, startDate);

            _doctorScheduleByDayOfWeek = weeklySchedule.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.FirstOrDefault()!);

            await BuildGridAsync(startDate, endDate);
        }
        catch (Exception ex)
        {
            ShowErrorBar($"Помилка завантаження розкладу: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private (DateTime startDate, DateTime endDate) GetPeriodRange()
    {
        return CurrentViewMode switch
        {
            ViewMode.Day => (CurrentDate, CurrentDate),
            ViewMode.Week => GetWeekRange(CurrentDate),
            ViewMode.Month => GetMonthRange(CurrentDate),
            _ => (CurrentDate, CurrentDate)
        };
    }

    private (DateTime start, DateTime end) GetWeekRange(DateTime date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        if (dayOfWeek == 0) dayOfWeek = 7; 

        var monday = date.AddDays(-(dayOfWeek - 1));
        var sunday = monday.AddDays(6);

        return (monday, sunday);
    }

    private (DateTime start, DateTime end) GetMonthRange(DateTime date)
    {
        var firstDay = new DateTime(date.Year, date.Month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);

        return (firstDay, lastDay);
    }

    #endregion

    #region Grid Building

  
    private async Task BuildGridAsync(DateTime startDate, DateTime endDate)
    {
        var days = new List<DayColumn>();
        var currentDate = startDate;
        while (currentDate <= endDate)
        {
            days.Add(CreateDayColumn(currentDate));
            currentDate = currentDate.AddDays(1);
        }
        DayColumns = new ObservableCollection<DayColumn>(days);

        GridColumnHeaders = new ObservableCollection<string>(
            days.Select(d => CurrentViewMode == ViewMode.Month
                ? d.ShortDisplayText
                : d.DisplayText));

        var rows = new List<GridRow>();
        foreach (var hourSlot in _hourSlots)
        {
            var row = new GridRow
            {
                RowHeader = hourSlot,
                Cells = new ObservableCollection<GridCell>()
            };

            foreach (var day in days)
            {
                var slot = await CreateScheduleSlotAsync(day.Date, hourSlot);
                var cell = new GridCell
                {
                    Date = day.Date,
                    TimeSlot = hourSlot,
                    SlotData = slot
                };

                row.Cells.Add(cell);
            }

            rows.Add(row);
        }

        GridRows = new ObservableCollection<GridRow>(rows);
    }


    private DayColumn CreateDayColumn(DateTime date)
    {
        var culture = new CultureInfo("uk-UA");
        var dayOfWeek = date.DayOfWeek;

        return new DayColumn
        {
            Date = date,
            DisplayText = $"{GetDayOfWeekShort(dayOfWeek)}, {date:dd.MM}",
            ShortDisplayText = date.Day.ToString(),
            DayOfWeekName = culture.DateTimeFormat.GetDayName(dayOfWeek),
            DayOfWeekShort = GetDayOfWeekShort(dayOfWeek),
            IsToday = date.Date == DateTime.Today,
            IsWeekend = dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday
        };
    }


    private async Task<ScheduleSlotData> CreateScheduleSlotAsync(DateTime date, string timeSlot)
    {
        var slot = new ScheduleSlotData
        {
            Date = date,
            TimeSlot = timeSlot,
            IsWorking = false
        };

        var dayOfWeek = (int)date.DayOfWeek;
        if (dayOfWeek == 0) dayOfWeek = 7;

        if (!_doctorScheduleByDayOfWeek.TryGetValue(dayOfWeek, out var scheduleCard))
        {
            return slot; 
        }

        var time = TimeSpan.Parse(timeSlot);
        var startTime = TimeSpan.Parse(scheduleCard.StartTime);
        var endTime = TimeSpan.Parse(scheduleCard.EndTime);

        if (time < startTime || time >= endTime)
        {
            return slot; 
        }

        slot.IsWorking = true;
        slot.Schedule = scheduleCard;
        slot.RoomNumber = scheduleCard.RoomNumber;

        if (CurrentViewMode == ViewMode.Day)
        {
            try
            {
                var appointments = await _scheduleDetailsRepository
                    .GetAppointmentSlotsForDateAsync(SelectedDoctor!.Id, date);

                slot.Appointments = appointments
                    .Where(a => a.TimeSlot == timeSlot)
                    .ToList();

                slot.BookedSlots = slot.Appointments.Count(a => a.IsBooked);
                slot.TotalSlots = 1; 
            }
            catch
            {
            }

            slot.DisplayText = $"Кабінет {slot.RoomNumber}";
            slot.ToolTip = $"{timeSlot} - Кабінет {slot.RoomNumber}";
        }
        else
        {
            slot.TotalSlots = scheduleCard.TotalSlotsAvailable;
            slot.BookedSlots = scheduleCard.BookedSlotsCount;

            var loadPercentage = slot.LoadPercentage;
            Color color;
            if (loadPercentage <= 33)
            {
                color = Color.FromRgb(76, 175, 80); 
            }
            else if (loadPercentage <= 66)
            {
                color = Color.FromRgb(255, 193, 7); 
            }
            else
            {
                color = Color.FromRgb(244, 67, 54);
            }

            var opacity = 0.3 + (loadPercentage / 100.0) * 0.5;
            slot.Background = new SolidColorBrush(color, opacity);

            slot.DisplayText = $"{slot.AvailableSlots}/{slot.TotalSlots}";
            slot.ToolTip = $"{timeSlot}\nВільно: {slot.AvailableSlots} з {slot.TotalSlots}\nЗайнято: {loadPercentage}%";
        }

        return slot;
    }

    #endregion

    #region Helper Methods

 
    private List<string> GenerateHourSlots()
    {
        var slots = new List<string>();
        var start = TimeSpan.FromHours(8);
        var end = TimeSpan.FromHours(20);
        var interval = TimeSpan.FromMinutes(30);

        var current = start;
        while (current < end)
        {
            slots.Add(current.ToString(@"hh\:mm"));
            current = current.Add(interval);
        }

        return slots;
    }


    private string GetDayOfWeekShort(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => "Пн",
            DayOfWeek.Tuesday => "Вт",
            DayOfWeek.Wednesday => "Ср",
            DayOfWeek.Thursday => "Чт",
            DayOfWeek.Friday => "Пт",
            DayOfWeek.Saturday => "Сб",
            DayOfWeek.Sunday => "Нд",
            _ => ""
        };
    }

 
    private void UpdatePeriodDisplay()
    {
        var culture = new CultureInfo("uk-UA");

        PeriodDisplayText = CurrentViewMode switch
        {
            ViewMode.Day => CurrentDate.ToString("d MMMM yyyy", culture),
            ViewMode.Week => GetWeekDisplayText(CurrentDate, culture),
            ViewMode.Month => CurrentDate.ToString("MMMM yyyy", culture),
            _ => CurrentDate.ToString("d MMMM yyyy", culture)
        };
    }

    private string GetWeekDisplayText(DateTime date, CultureInfo culture)
    {
        var (monday, sunday) = GetWeekRange(date);

        if (monday.Month == sunday.Month)
        {
            return $"{monday.Day}-{sunday.Day} {monday.ToString("MMMM yyyy", culture)}";
        }
        else
        {
            return $"{monday:d MMM} - {sunday:d MMM yyyy}";
        }
    }

    #endregion
}

#region Supporting Classes

public class GridRow
{
    public string RowHeader { get; set; } = string.Empty;
    public ObservableCollection<GridCell> Cells { get; set; } = new();
}

public class GridCell
{
    public DateTime Date { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public ScheduleSlotData? SlotData { get; set; }
}

#endregion

