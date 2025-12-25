using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PMS.Core.Models;
using PMS.Core.Models.DTO;

namespace PMS.Core.Repositories.Interfaces;

public interface IExaminationRepository : IBaseRepository<Examination>
{
    

    Task<(IEnumerable<Examination> examinations, long totalCount)> GetPagedExaminationsAsync(
        int page,
        int pageSize,
        string? searchText = null,
        string? patientId = null,
        string? doctorId = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null);

  
    Task<List<PatientWithMultipleDoctors>> GetPatientsWithMultipleDoctorsPerWeekAsync(DateTime startDate, DateTime endDate);
    Task<long> CountPatientsByDiagnosisInMonthAsync(string diagnosisName, DateTime monthStart, DateTime monthEnd);
    Task<List<PatientDiagnosisInfo>> GetPatientsByDiagnosisInMonthAsync(string diagnosisName, DateTime monthStart, DateTime monthEnd);
}