using System;

namespace PMS.Core.Models.DTO;

public class CertificateDetail
{
    public string Id { get; set; } = string.Empty;
    public string CertificateNumber { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;
    public string TypeDisplay => Type switch
    {
        "sick_leave" => "Лікарняний",
        "health" => "Довідка про стан здоров'я",
        "vaccination" => "Довідка про вакцинацію",
        "driver" => "Довідка для водіїв",
        "pool" => "Довідка для басейну",
        "work" => "Довідка на роботу",
        "education" => "Довідка для навчання",
        "dispensary" => "Довідка диспансерного обліку",
        _ => Type
    };

    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;

    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string DoctorSpecialty { get; set; } = string.Empty;

    public DateTime IssueDate { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }

    public string? DiagnosisId { get; set; }
    public string? DiagnosisCode { get; set; }
    public string? DiagnosisName { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Purpose { get; set; }
    public string CreatedById { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public string IssueDateDisplay => IssueDate.ToString("dd.MM.yyyy");
    public string ValidityPeriod
    {
        get
        {
            if (!ValidUntil.HasValue)
                return $"З {ValidFrom:dd.MM.yyyy}";
            return $"{ValidFrom:dd.MM.yyyy} - {ValidUntil.Value:dd.MM.yyyy}";
        }
    }
    public string DiagnosisDisplay => !string.IsNullOrEmpty(DiagnosisCode)
        ? $"{DiagnosisCode} - {DiagnosisName}"
        : "—";
}