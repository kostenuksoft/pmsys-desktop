using System;
using PMS.Core.Models.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PMS.Core.Repositories.Interfaces;


public interface IScheduleDetailsRepository
{


    Task<ScheduleCard?> GetDoctorScheduleCardForDateAsync(
        string doctorId,
        DateTime date);

    Task<Dictionary<int, List<ScheduleCard>>> GetDoctorWeeklyScheduleAsync(
        string doctorId,
        DateTime weekStartDate);


    Task<List<AppointmentSlotInfo>> GetAppointmentSlotsForDateAsync(
        string doctorId,
        DateTime date);
}