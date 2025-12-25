using System;

namespace PMS.Core.Models.DTO;


public class DoctorScheduleEntry
{
    public string DoctorId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? RoomNumber { get; set; }
    public string? Notes { get; set; }

    public string TimeDisplay => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    public string DateDisplay => Date.ToString("dd.MM.yyyy");
}