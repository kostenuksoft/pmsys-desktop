using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PMS.Core.Models;

namespace PMS.Core.Repositories.Interfaces;

public interface IAppointmentRepository : IBaseRepository<Appointment>
{
    Task<IEnumerable<Appointment>> GetByDoctorAndDateAsync(string doctorId, DateTime date);
}