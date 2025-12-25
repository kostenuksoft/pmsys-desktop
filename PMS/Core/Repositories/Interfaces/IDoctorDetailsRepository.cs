using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PMS.Core.Models.DTO;

namespace PMS.Core.Repositories.Interfaces;


public interface IDoctorDetailsRepository
{
    Task<List<CertificateDetail>> GetDoctorCertificatesAsync(
        string doctorId,
        int page,
        int pageSize,
        string? searchText = null,
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task<List<PatientWithDoctor>> GetDoctorPatientsAsync(
        string doctorId,
        int page,
        int pageSize,
        string? searchText = null);

    Task<List<ExaminationDetail>> GetDoctorExaminationsAsync(
        string doctorId,
        int page,
        int pageSize,
        string? searchText = null,
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task<DoctorAppointmentStats> GetDoctorAppointmentStatsAsync(string doctorId);
    Task<DoctorPatientStats> GetDoctorPatientStatsAsync(string doctorId);

    Task<long> CountDoctorCertificatesAsync(string doctorId);
    Task<long> CountDoctorExaminationsAsync(string doctorId);
    Task<long> CountDoctorPatientsAsync(string doctorId);
}