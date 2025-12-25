using System;
using System.Collections.ObjectModel;
using PMS.Core.Models.DTO;

namespace PMS.ViewModels.Dialogs;

public class ScheduleDetailsDialogViewModel : BaseDialogViewModel<bool>
{
    private ScheduleCard _scheduleCard;
    private ObservableCollection<AppointmentSlotInfo> _appointmentSlots;
    private string _selectedDate;

    public ScheduleDetailsDialogViewModel()
    {
        _scheduleCard = new ScheduleCard();
        _appointmentSlots = new ObservableCollection<AppointmentSlotInfo>();
        _selectedDate = DateTime.Today.ToString("dd.MM.yyyy");

        Title = "Деталі розкладу";
    }

    public ScheduleCard ScheduleCard
    {
        get => _scheduleCard;
        set => SetAndRiseProperty(ref _scheduleCard, value);
    }

    public ObservableCollection<AppointmentSlotInfo> AppointmentSlots
    {
        get => _appointmentSlots;
        set => SetAndRiseProperty(ref _appointmentSlots, value);
    }

    public string SelectedDate
    {
        get => _selectedDate;
        set => SetAndRiseProperty(ref _selectedDate, value);
    }

    public void LoadScheduleCard(ScheduleCard card)
    {
        ScheduleCard = card;
        AppointmentSlots = new ObservableCollection<AppointmentSlotInfo>(card.AppointmentSlots);
        SelectedDate = DateTime.Today.ToString("dd.MM.yyyy");
    }

    protected override bool GetResult()
    {
        return true;
    }
}