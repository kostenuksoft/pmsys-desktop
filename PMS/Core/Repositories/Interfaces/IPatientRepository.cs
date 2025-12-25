using System.Collections.Generic;
using System.Threading.Tasks;
using PMS.Core.Enums.General;
using PMS.Core.Models;

namespace PMS.Core.Repositories.Interfaces;

public interface IPatientRepository : IBaseRepository<Patient>
{
    
    Task<(List<Patient> Patients, long TotalCount)> GetPagedPatientsAsync(
        int page,
        int pageSize,
        string? searchText = null,
        bool? isActive = null,
        string? assignedDoctorId = null,
        HealthStatus? healthStatus = null);

    Task<(List<Patient> Patients, long TotalCount)> SearchPatientsAsync(
        string searchText,
        int page,
        int pageSize);
}