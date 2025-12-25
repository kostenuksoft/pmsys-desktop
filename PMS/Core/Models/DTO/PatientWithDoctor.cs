using System;

namespace PMS.Core.Models.DTO;

public class PatientWithDoctor
{
    public string PatientId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public string? DoctorName { get; set; }
    public DateTime RegistrationDate { get; set; }

    public string BirthDateDisplay => BirthDate.ToString("dd.MM.yyyy");
    public int Age => DateTime.Now.Year - BirthDate.Year;
}