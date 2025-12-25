using System;
using System.Collections.Generic;

namespace PMS.Core.Models.DTO;

public class PatientWithMultipleDoctors
{
    public string PatientId { get; set; } = string.Empty;
    public string PatientFullName { get; set; } = string.Empty;
    public string PatientMedicalRecordNumber { get; set; } = string.Empty;
    public int UniqueDoctorCount { get; set; }
    public int TotalExaminations { get; set; }
    public List<DateTime> ExaminationDates { get; set; } = [];
    public List<DoctorInfo> Doctors { get; set; } = new();
}