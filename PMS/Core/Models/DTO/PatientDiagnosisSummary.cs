using System;
using System.Collections.Generic;

namespace PMS.Core.Models.DTO;

public class PatientDiagnosisSummary
{
    public string DiagnosisId { get; set; } = string.Empty;
    public string IcdCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int OccurrenceCount { get; set; }
    public DateTime FirstDiagnosed { get; set; }
    public DateTime LastDiagnosed { get; set; }
    public List<string> DoctorNames { get; set; } = new();

    public string OccurrenceDisplay => $"{OccurrenceCount} раз";
    public string PeriodDisplay => $"{FirstDiagnosed:dd.MM.yyyy} - {LastDiagnosed:dd.MM.yyyy}";
    public string DoctorsDisplay => DoctorNames.Count > 0
        ? string.Join(", ", DoctorNames)
        : "—";
}