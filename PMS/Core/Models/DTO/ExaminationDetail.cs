using System;
using System.Collections.Generic;
using System.Linq;

namespace PMS.Core.Models.DTO;


public class ExaminationDetail
{
    public string Id { get; set; } = string.Empty;
    public string AppointmentId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;

    public string DoctorName { get; set; } = string.Empty;
    public string DoctorSpecialty { get; set; } = string.Empty;

    public DateTime ExaminationDate { get; set; }
    public string? Anamnesis { get; set; }
    public string? ObjectiveStatus { get; set; }

    public List<DiagnosisInfo> Diagnoses { get; set; } = new();

    public string? Recommendations { get; set; }
    public DateTime? SickLeaveFrom { get; set; }
    public DateTime? SickLeaveTo { get; set; }
    public DateTime? FollowUpDate { get; set; }

    public string SickLeavePeriod
    {
        get
        {
            if (SickLeaveFrom.HasValue && SickLeaveTo.HasValue)
                return $"{SickLeaveFrom.Value:dd.MM.yyyy} - {SickLeaveTo.Value:dd.MM.yyyy}";
            return "—";
        }
    }

    public string FollowUpDateDisplay => FollowUpDate?.ToString("dd.MM.yyyy") ?? "—";

    public string DiagnosesDisplay => Diagnoses.Count > 0
        ? string.Join(", ", Diagnoses.Select(d => d.Name))
        : "Не вказано";
}