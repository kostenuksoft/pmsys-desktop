using System;

namespace PMS.Core.Models.DTO;


public class HomeVisitDetail
{
    public string Id { get; set; } = string.Empty;
    public string? PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? AlternativePhone { get; set; }

    public DateTime CallDate { get; set; }
    public string CallTime { get; set; } = string.Empty;

    public string Urgency { get; set; } = string.Empty;
    public string UrgencyDisplay => Urgency switch
    {
        "regular" => "Звичайний",
        "urgent" => "Терміновий",
        "emergency" => "Екстрений",
        _ => Urgency
    };

    public string Symptoms { get; set; } = string.Empty;

    public string? AssignedDoctorId { get; set; }
    public string? AssignedDoctorName { get; set; }

    public DateTime? VisitDate { get; set; }
    public string? VisitTimeSlot { get; set; }

    public string Status { get; set; } = string.Empty;
    public string StatusDisplay => Status switch
    {
        "new" => "Новий",
        "assigned" => "Призначено",
        "in_progress" => "В процесі",
        "completed" => "Завершено",
        "cancelled" => "Скасовано",
        _ => Status
    };

    public DateTime StatusUpdated { get; set; }

    public string ReceivedById { get; set; } = string.Empty;
    public string ReceivedByName { get; set; } = string.Empty;

    public string? Notes { get; set; }
    public string? ExaminationId { get; set; }

    public string CallDateTimeDisplay => $"{CallDate:dd.MM.yyyy} о {CallTime}";
    public string VisitScheduledDisplay => VisitDate.HasValue && !string.IsNullOrEmpty(VisitTimeSlot)
        ? $"{VisitDate.Value:dd.MM.yyyy} {VisitTimeSlot}"
        : "Не заплановано";
}