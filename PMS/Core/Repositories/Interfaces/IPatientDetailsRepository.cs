using System.Collections.Generic;
using System.Threading.Tasks;
using PMS.Core.Models.DTO;

namespace PMS.Core.Repositories.Interfaces;


public interface IPatientDetailsRepository
{
  
    Task<List<PatientProcedureDetail>> GetPatientProceduresAsync(string patientId);
    Task<List<ExaminationDetail>> GetPatientExaminationsAsync(string patientId);
    Task<List<PatientDiagnosisSummary>> GetPatientDiagnosesSummaryAsync(string patientId);
    Task<List<AppointmentDetail>> GetPatientAppointmentsAsync(string patientId);
    Task<List<HomeVisitDetail>> GetPatientHomeVisitsAsync(string patientId);
    Task<List<VaccinationDetail>> GetPatientVaccinationsAsync(string patientId);
}