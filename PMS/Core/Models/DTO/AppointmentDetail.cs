using System;

namespace PMS.Core.Models.DTO;


public class AppointmentDetail
{
    public string Id { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;

    public string DoctorName { get; set; } = string.Empty;
    public string DoctorSpecialty { get; set; } = string.Empty;

    public string? RoomId { get; set; }
    public string? RoomNumber { get; set; }

    public DateTime AppointmentDate { get; set; }
    public string AppointmentTime { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;
    public string TypeDisplay => Type switch
    {
        "primary" => "Первинний",
        "secondary" => "Повторний",
        "checkup" => "Профогляд",
        "vaccination" => "Вакцинація",
        "procedure" => "Процедура",
        _ => Type
    };

    public string Status { get; set; } = string.Empty;
    public string StatusDisplay => Status switch
    {
        "scheduled" => "Заплановано",
        "in_progress" => "В процесі",
        "completed" => "Завершено",
        "cancelled" => "Скасовано",
        "no_show" => "Не з'явився",
        _ => Status
    };

    public string? Complaints { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedById { get; set; }
    public string? CreatedByName { get; set; }

    public string DateTimeDisplay => $"{AppointmentDate:dd.MM.yyyy} о {AppointmentTime}";
}