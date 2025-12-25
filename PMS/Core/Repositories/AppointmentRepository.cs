using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using PMS.Core.Database;
using PMS.Core.Enums.General;
using PMS.Core.Models;
using PMS.Core.Repositories.Interfaces;
using Serilog;

namespace PMS.Core.Repositories;

public class AppointmentRepository : BaseRepository<Appointment>, IAppointmentRepository
{
    public AppointmentRepository(IDatabaseContext context, ILogger logger)
        : base(context, "appointments", logger)
    {
    }

    public async Task<IEnumerable<Appointment>> GetByDoctorAndDateAsync(string doctorId, DateTime date)
    {
        try
        {
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1);

            var filterBuilder = Builders<Appointment>.Filter;

            var filter = filterBuilder.And(
                filterBuilder.Eq(a => a.DoctorId, doctorId),
                filterBuilder.Gte(a => a.AppointmentDate, startOfDay),
                filterBuilder.Lt(a => a.AppointmentDate, endOfDay)
            );

            return await Collection
                .Find(filter)
                .SortBy(a => a.AppointmentTime)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting appointments for doctor {DoctorId} on {Date}",
                doctorId, date.ToShortDateString());
            throw;
        }
    }

}