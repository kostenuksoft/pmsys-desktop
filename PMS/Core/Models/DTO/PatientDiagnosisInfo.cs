using System;
using System.Collections.Generic;

namespace PMS.Core.Models.DTO;

public class PatientDiagnosisInfo
{
    public string PatientId { get; set; } = string.Empty;
    public string PatientFullName { get; set; } = string.Empty;
    public string PatientMedicalRecordNumber { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;
    public string DiagnosisName { get; set; } = string.Empty;
    public string DiagnosisCode { get; set; } = string.Empty;
    public int ExaminationCount { get; set; }
    public DateTime FirstExaminationDate { get; set; }
    public DateTime LastExaminationDate { get; set; }
    public List<DoctorInfo> Doctors { get; set; } = new();
}