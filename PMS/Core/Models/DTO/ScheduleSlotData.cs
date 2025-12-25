using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace PMS.Core.Models.DTO;

public class ScheduleSlotData
{
    public DateTime Date { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public bool IsWorking { get; set; }
    public ScheduleCard? Schedule { get; set; }
    public List<AppointmentSlotInfo> Appointments { get; set; } = new();
    public int TotalSlots { get; set; }
    public int BookedSlots { get; set; }
    public int AvailableSlots => TotalSlots - BookedSlots;
    public int LoadPercentage => TotalSlots > 0 ? (BookedSlots * 100 / TotalSlots) : 0;
    public IBrush? Background { get; set; }
    public string? RoomNumber { get; set; }
    public string DisplayText { get; set; } = string.Empty;
    public string ToolTip { get; set; } = string.Empty;
}