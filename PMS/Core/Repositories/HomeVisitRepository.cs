using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using PMS.Core.Database;
using PMS.Core.Enums.General;
using PMS.Core.Models;
using PMS.Core.Repositories.Interfaces;
using Serilog;

namespace PMS.Core.Repositories
{
    public class HomeVisitRepository : BaseRepository<HomeVisit>, IHomeVisitRepository
    {
        public HomeVisitRepository(IDatabaseContext context, ILogger logger)
            : base(context, "home_visits", logger)
        {
        }

        public async Task<IEnumerable<HomeVisit>> GetByStatusAsync(HomeVisitStatus status)
        {
            try
            {
                var visits = await Collection
                    .Find(v => v.Status == status)
                    .SortByDescending(v => v.CallDate)
                    .ThenByDescending(v => v.CallTime)
                    .ToListAsync();

                await PopulateNavigationProperties(visits);
                
                Logger.Information("Retrieved {Count} home visits with status {Status}", visits.Count, status);
                return visits;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting home visits by status {Status}", status);
                throw;
            }
        }

        public async Task<IEnumerable<HomeVisit>> GetByDoctorAsync(string doctorId)
        {
            try
            {
                var visits = await Collection
                    .Find(v => v.AssignedDoctorId == doctorId)
                    .SortByDescending(v => v.CallDate)
                    .ThenByDescending(v => v.CallTime)
                    .ToListAsync();

                await PopulateNavigationProperties(visits);
                
                Logger.Information("Retrieved {Count} home visits for doctor {DoctorId}", visits.Count, doctorId);
                return visits;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting home visits by doctor {DoctorId}", doctorId);
                throw;
            }
        }

        public async Task<IEnumerable<HomeVisit>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var visits = await Collection
                    .Find(v => v.CallDate >= startDate && v.CallDate <= endDate)
                    .SortBy(v => v.CallDate)
                    .ThenBy(v => v.CallTime)
                    .ToListAsync();

                await PopulateNavigationProperties(visits);
                
                Logger.Information("Retrieved {Count} home visits between {Start} and {End}", 
                    visits.Count, startDate, endDate);
                return visits;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting home visits by date range");
                throw;
            }
        }

        public async Task<IEnumerable<HomeVisit>> GetPendingVisitsAsync()
        {
            try
            {
                var visits = await Collection
                    .Find(v => v.Status == HomeVisitStatus.New || v.Status == HomeVisitStatus.Assigned)
                    .SortBy(v => v.Urgency)
                    .ThenBy(v => v.CallDate)
                    .ThenBy(v => v.CallTime)
                    .ToListAsync();

                await PopulateNavigationProperties(visits);
                
                Logger.Information("Retrieved {Count} pending home visits", visits.Count);
                return visits;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting pending home visits");
                throw;
            }
        }

        public async Task<IEnumerable<HomeVisit>> GetByUrgencyAsync(Urgency urgency)
        {
            try
            {
                var visits = await Collection
                    .Find(v => v.Urgency == urgency && v.Status != HomeVisitStatus.Completed && v.Status != HomeVisitStatus.Cancelled)
                    .SortByDescending(v => v.CallDate)
                    .ThenByDescending(v => v.CallTime)
                    .ToListAsync();

                await PopulateNavigationProperties(visits);
                
                Logger.Information("Retrieved {Count} home visits with urgency {Urgency}", visits.Count, urgency);
                return visits;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting home visits by urgency {Urgency}", urgency);
                throw;
            }
        }

        public async Task<bool> AssignDoctorAsync(string visitId, string doctorId, DateTime visitDate, string timeSlot)
        {
            try
            {
                var update = Builders<HomeVisit>.Update
                    .Set(v => v.AssignedDoctorId, doctorId)
                    .Set(v => v.VisitDate, visitDate)
                    .Set(v => v.VisitTimeSlot, timeSlot)
                    .Set(v => v.Status, HomeVisitStatus.Assigned)
                    .Set(v => v.StatusUpdated, DateTime.UtcNow);

                var result = await Collection.UpdateOneAsync(
                    v => v.Id == visitId,
                    update);

                var success = result.ModifiedCount > 0;
                
                if (success)
                {
                    Logger.Information("Assigned doctor {DoctorId} to home visit {VisitId}", doctorId, visitId);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error assigning doctor to home visit {VisitId}", visitId);
                throw;
            }
        }

        public async Task<bool> UpdateStatusAsync(string visitId, HomeVisitStatus status)
        {
            try
            {
                var update = Builders<HomeVisit>.Update
                    .Set(v => v.Status, status)
                    .Set(v => v.StatusUpdated, DateTime.UtcNow);

                if (status == HomeVisitStatus.Completed)
                {
                }

                var result = await Collection.UpdateOneAsync(
                    v => v.Id == visitId,
                    update);

                var success = result.ModifiedCount > 0;
                
                if (success)
                {
                    Logger.Information("Updated home visit {VisitId} status to {Status}", visitId, status);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error updating home visit status {VisitId}", visitId);
                throw;
            }
        }

        public async Task<bool> CompleteVisitAsync(string visitId, string notes)
        {
            try
            {
                var update = Builders<HomeVisit>.Update
                    .Set(v => v.Status, HomeVisitStatus.Completed)
                    .Set(v => v.StatusUpdated, DateTime.UtcNow);

                if (!string.IsNullOrWhiteSpace(notes))
                {
                    update = update.Set(v => v.Notes, notes);
                }

                var result = await Collection.UpdateOneAsync(
                    v => v.Id == visitId,
                    update);

                var success = result.ModifiedCount > 0;
                
                if (success)
                {
                    Logger.Information("Completed home visit {VisitId}", visitId);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error completing home visit {VisitId}", visitId);
                throw;
            }
        }

        private async Task PopulateNavigationProperties(List<HomeVisit> visits)
        {
            if (visits.Count == 0) return;

            try
            {
                var doctorIds = visits
                    .Where(v => !string.IsNullOrEmpty(v.AssignedDoctorId))
                    .Select(v => v.AssignedDoctorId!)
                    .Distinct()
                    .ToList();

                var receivedByIds = visits
                    .Select(v => v.ReceivedBy)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct()
                    .ToList();

                var doctors = new Dictionary<string, string>();
                if (doctorIds.Any())
                {
                    var doctorCollection = App.GetService<DatabaseContext>().GetCollection<Doctor>("doctors");
                    var doctorsList = await doctorCollection
                        .Find(d => doctorIds.Contains(d.Id))
                        .ToListAsync();

                    foreach (var doctor in doctorsList)
                    {
                        doctors[doctor.Id] = doctor.FullName;
                    }
                }

                var users = new Dictionary<string, string>();
                if (receivedByIds.Any())
                {
                    var userCollection = App.GetService<DatabaseContext>().GetCollection<User>("users");
                    var usersList = await userCollection
                        .Find(u => receivedByIds.Contains(u.Id))
                        .ToListAsync();

                    foreach (var user in usersList)
                    {
                        users[user.Id] = user.FullName;
                    }
                }

                foreach (var visit in visits)
                {
                    if (!string.IsNullOrEmpty(visit.AssignedDoctorId) && doctors.ContainsKey(visit.AssignedDoctorId))
                    {
                        visit.AssignedDoctorName = doctors[visit.AssignedDoctorId];
                    }

                    if (!string.IsNullOrEmpty(visit.ReceivedBy) && users.ContainsKey(visit.ReceivedBy))
                    {
                        visit.ReceivedByName = users[visit.ReceivedBy];
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Error populating navigation properties for home visits");
            }
        }
    }
}