using PMS.Core.Models;
using PMS.Core.Models.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PMS.Core.Repositories.Interfaces;

public interface IDoctorRepository : IBaseRepository<Doctor>
{
    Task<Doctor?> GetByEmployeeNumberAsync(string employeeNumber);
    Task<IEnumerable<Doctor>> GetDistrictDoctorsAsync();

    Task<(IEnumerable<DoctorWithDetails> doctors, long totalCount)> GetPagedDoctorsWithDetailsAsync(
        int page,
        int pageSize,
        string? searchText = null,
        bool? isActive = null,
        string? specialtyId = null,
        bool? isDistrictDoctor = null);

    Task<(List<Doctor> Doctors, long TotalCount)> SearchDoctorsAsync(
        string searchText,
        int page,
        int pageSize);
}