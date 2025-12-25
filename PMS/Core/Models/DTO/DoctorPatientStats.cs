namespace PMS.Core.Models.DTO;

public class DoctorPatientStats
{
    public string DoctorId { get; set; } = string.Empty;
    public int TotalPatients { get; set; }
    public int ActivePatients { get; set; }
    public int PatientsThisMonth { get; set; }
    public int PatientsToday { get; set; }
}