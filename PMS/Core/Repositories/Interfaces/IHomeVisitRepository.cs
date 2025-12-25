using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PMS.Core.Enums.General;
using PMS.Core.Models;

namespace PMS.Core.Repositories.Interfaces;

public interface IHomeVisitRepository : IBaseRepository<HomeVisit>
{
    Task<bool> AssignDoctorAsync(string visitId, string doctorId, DateTime visitDate, string timeSlot);
}