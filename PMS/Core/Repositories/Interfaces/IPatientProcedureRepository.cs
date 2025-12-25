using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PMS.Core.Enums.General;
using PMS.Core.Models;

namespace PMS.Core.Repositories.Interfaces;



public interface IPatientProcedureRepository : IBaseRepository<PatientProcedure>
{

    Task<(List<PatientProcedure> Procedures, long TotalCount)> GetPagedPatientProceduresAsync(
        int page,
        int pageSize,
        string? searchText = null,
        string? patientId = null,
        string? procedureId = null,
        string? prescribedById = null,
        ProcedureStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task<long> CountByStatusAsync(ProcedureStatus status);
}