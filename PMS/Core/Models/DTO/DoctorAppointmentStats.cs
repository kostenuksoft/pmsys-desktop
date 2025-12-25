namespace PMS.Core.Models.DTO;

public class DoctorAppointmentStats
{
    public string DoctorId { get; set; } = string.Empty;
    public int TotalAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public int UpcomingAppointments { get; set; }
    public int TodayAppointments { get; set; }
}