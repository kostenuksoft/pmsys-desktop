using PMS.Core.Enums.General;

namespace PMS.Core.Models.DTO;

public class AppointmentSlotInfo
{
    public string TimeSlot { get; set; } = string.Empty;
    public bool IsBooked { get; set; }
    public string? PatientId { get; set; }
    public string? PatientName { get; set; }
    public string? AppointmentId { get; set; }
    public AppointmentType? AppointmentType { get; set; }

    public string StatusDisplay => IsBooked ? "Зайнято" : "Вільно";

    public string DisplayText => IsBooked && !string.IsNullOrEmpty(PatientName)
        ? $"{TimeSlot} - {PatientName}"
        : $"{TimeSlot} - Вільно";
}