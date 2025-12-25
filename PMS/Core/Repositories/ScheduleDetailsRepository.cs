using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using PMS.Core.Database;
using PMS.Core.Enums.General;
using PMS.Core.Models;
using PMS.Core.Models.DTO;
using PMS.Core.Repositories.Interfaces;
using Serilog;

namespace PMS.Core.Repositories;

public class ScheduleDetailsRepository : IScheduleDetailsRepository
{
    private readonly IDatabaseContext _context;
    private readonly ILogger _logger;

    public ScheduleDetailsRepository(IDatabaseContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ScheduleCard>> GetScheduleCardsAsync(
        DateTime startDate,
        DateTime endDate,
        string? specialtyId = null,
        string? doctorId = null,
        Shift? shift = null)
    {
        try
        {
            var schedulesCollection = _context.GetCollection<BsonDocument>("schedules");

            var matchFilter = new BsonDocument
            {
                { "entity_type", "doctor" },
                { "effective_from", new BsonDocument("$lte", endDate) },
                {
                    "$or", new BsonArray
                    {
                        new BsonDocument("effective_until", BsonNull.Value),
                        new BsonDocument("effective_until", new BsonDocument("$gte", startDate))
                    }
                }
            };

            if (!string.IsNullOrEmpty(doctorId))
            {
                matchFilter.Add("entity_id", new ObjectId(doctorId));
            }

            if (shift.HasValue)
            {
                var shiftValue = shift.Value switch
                {
                    Shift.First => "first",
                    Shift.Second => "second",
                    Shift.Full => "both",
                    _ => "first"
                };
                matchFilter.Add("shift", shiftValue);
            }

            var pipeline = new[]
            {
                new BsonDocument("$match", matchFilter),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "doctors" },
                    { "localField", "entity_id" },
                    { "foreignField", "_id" },
                    { "as", "doctor" }
                }),
                new BsonDocument("$unwind", "$doctor"),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "specialties" },
                    { "localField", "doctor.specialty_id" },
                    { "foreignField", "_id" },
                    { "as", "specialty" }
                }),
                new BsonDocument("$unwind", "$specialty"),

                new BsonDocument("$match", specialtyId != null
                    ? new BsonDocument("doctor.specialty_id", new ObjectId(specialtyId))
                    : new BsonDocument()),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "rooms" },
                    { "localField", "room_id" },
                    { "foreignField", "_id" },
                    { "as", "room" }
                }),
                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$room" },
                    { "preserveNullAndEmptyArrays", true }
                }),

                new BsonDocument("$project", new BsonDocument
                {
                    { "DoctorId", new BsonDocument("$toString", "$doctor._id") },
                    { "DoctorFullName", "$doctor.full_name" },
                    { "DoctorSpecialty", "$specialty.name" },
                    { "DoctorCategory", "$doctor.category" },
                    { "DoctorPhone", "$doctor.phone" },
                    { "DoctorEmail", "$doctor.email" },
                    { "DoctorExperienceYears", "$doctor.experience_years" },
                    { "IsDistrictDoctor", "$doctor.is_district_doctor" },

                    { "RoomId", new BsonDocument("$toString", "$room._id") },
                    { "RoomNumber", "$room.room_number" },
                    { "RoomFloor", "$room.floor" },
                    { "RoomType", "$room.room_type" },

                    { "ScheduleId", new BsonDocument("$toString", "$_id") },
                    { "DayOfWeek", "$day_of_week" },
                    { "Shift", "$shift" },
                    { "StartTime", "$start_time" },
                    { "EndTime", "$end_time" },
                    { "EffectiveFrom", "$effective_from" },
                    { "EffectiveUntil", "$effective_until" }
                }),

                new BsonDocument("$sort", new BsonDocument
                {
                    { "DayOfWeek", 1 },
                    { "StartTime", 1 }
                })
            };

            var results = await schedulesCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();

            var scheduleCards = results.Select(doc => new ScheduleCard
            {
                DoctorId = doc.GetValue("DoctorId", "").AsString,
                DoctorFullName = doc.GetValue("DoctorFullName", "").AsString,
                DoctorSpecialty = doc.GetValue("DoctorSpecialty", "").AsString,

                DoctorCategory = doc.Contains("DoctorCategory") && !doc["DoctorCategory"].IsBsonNull
                    ? Enum.Parse<DoctorCategory>(doc["DoctorCategory"].AsString, true)
                    : DoctorCategory.None,

                DoctorPhone = doc.Contains("DoctorPhone") && !doc["DoctorPhone"].IsBsonNull
                    ? doc["DoctorPhone"].AsString
                    : null,

                DoctorEmail = doc.Contains("DoctorEmail") && !doc["DoctorEmail"].IsBsonNull
                    ? doc["DoctorEmail"].AsString
                    : null,

                DoctorExperienceYears = doc.GetValue("DoctorExperienceYears", 0).AsInt32,
                IsDistrictDoctor = doc.GetValue("IsDistrictDoctor", false).AsBoolean,

                RoomId = doc.Contains("RoomId") && !doc["RoomId"].IsBsonNull
                    ? doc["RoomId"].AsString
                    : null,

                RoomNumber = doc.Contains("RoomNumber") && !doc["RoomNumber"].IsBsonNull
                    ? doc["RoomNumber"].AsString
                    : null,

                RoomFloor = doc.Contains("RoomFloor") && !doc["RoomFloor"].IsBsonNull
                    ? doc["RoomFloor"].AsInt32
                    : null,

                RoomType = RoomType.Consultation,

                ScheduleId = doc.GetValue("ScheduleId", "").AsString,
                DayOfWeek = doc.GetValue("DayOfWeek", 0).AsInt32,

                Shift = doc.Contains("Shift") && !doc["Shift"].IsBsonNull
                    ? Enum.Parse<Shift>(doc["Shift"].AsString, true)
                    : Shift.First,

                StartTime = doc.GetValue("StartTime", "").AsString,
                EndTime = doc.GetValue("EndTime", "").AsString,
                EffectiveFrom = doc.GetValue("EffectiveFrom", DateTime.MinValue).ToUniversalTime(),

                EffectiveUntil = doc.Contains("EffectiveUntil") && !doc["EffectiveUntil"].IsBsonNull
                    ? doc["EffectiveUntil"].ToUniversalTime()
                    : null
            }).ToList();

            foreach (var card in scheduleCards)
            {
                await PopulateAppointmentStatsAsync(card, startDate, endDate);
            }

            _logger.Information("Retrieved {Count} schedule cards", scheduleCards.Count);
            return scheduleCards;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting schedule cards");
            throw;
        }
    }

    public async Task<ScheduleCard?> GetDoctorScheduleCardForDateAsync(string doctorId, DateTime date)
    {
        try
        {
            var dayOfWeek = (int)date.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7;

            var cards = await GetScheduleCardsAsync(
                date,
                date,
                doctorId: doctorId);

            var card = cards.FirstOrDefault(c => c.DayOfWeek == dayOfWeek);

            if (card != null)
            {
                card.AppointmentSlots = await GetAppointmentSlotsForDateAsync(doctorId, date);
            }

            return card;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting doctor schedule card for date");
            throw;
        }
    }

    public async Task<Dictionary<int, List<ScheduleCard>>> GetDoctorWeeklyScheduleAsync(
        string doctorId,
        DateTime weekStartDate)
    {
        try
        {
            var weekEndDate = weekStartDate.AddDays(6);
            var cards = await GetScheduleCardsAsync(
                weekStartDate,
                weekEndDate,
                doctorId: doctorId);

            var groupedByDay = cards
                .GroupBy(c => c.DayOfWeek)
                .ToDictionary(g => g.Key, g => g.ToList());

            return groupedByDay;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting doctor weekly schedule");
            throw;
        }
    }


    public async Task<List<AppointmentSlotInfo>> GetAppointmentSlotsForDateAsync(
        string doctorId,
        DateTime date)
    {
        try
        {
            var schedulesCollection = _context.GetCollection<Schedule>("schedules");
            var appointmentsCollection = _context.GetCollection<BsonDocument>("appointments");

            var dayOfWeek = (int)date.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7;

            var now = DateTime.UtcNow;
            var schedule = await schedulesCollection
                .Find(s =>
                    s.EntityId == doctorId &&
                    s.EntityType == EntityType.Doctor &&
                    s.DayOfWeek == dayOfWeek &&
                    s.EffectiveFrom <= now &&
                    (s.EffectiveUntil == null || s.EffectiveUntil >= now))
                .FirstOrDefaultAsync();

            if (schedule == null)
                return new List<AppointmentSlotInfo>();

            var timeSlots = GenerateTimeSlots(schedule.StartTime, schedule.EndTime);

            var dateStart = date.Date;
            var dateEnd = date.Date.AddDays(1);

            var appointmentsPipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument
                {
                    { "doctor_id", new ObjectId(doctorId) },
                    { "appointment_date", new BsonDocument
                        {
                            { "$gte", dateStart },
                            { "$lt", dateEnd }
                        }
                    },
                    { "status", new BsonDocument("$ne", "cancelled") }
                }),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "patients" },
                    { "localField", "patient_id" },
                    { "foreignField", "_id" },
                    { "as", "patient" }
                }),
                new BsonDocument("$unwind", "$patient"),

                new BsonDocument("$project", new BsonDocument
                {
                    { "AppointmentId", new BsonDocument("$toString", "$_id") },
                    { "AppointmentTime", "$appointment_time" },
                    { "PatientId", new BsonDocument("$toString", "$patient_id") },
                    { "PatientName", "$patient.full_name" },
                    { "AppointmentType", "$type" }
                })
            };

            var appointments = await appointmentsCollection
                .Aggregate<BsonDocument>(appointmentsPipeline)
                .ToListAsync();

            var appointmentDict = appointments.ToDictionary(
                a => a["AppointmentTime"].AsString,
                a => a);

            var slotInfos = timeSlots.Select(time =>
            {
                var isBooked = appointmentDict.TryGetValue(time, out var apt);

                return new AppointmentSlotInfo
                {
                    TimeSlot = time,
                    IsBooked = isBooked,
                    PatientId = isBooked ? apt["PatientId"].AsString : null,
                    PatientName = isBooked ? apt["PatientName"].AsString : null,
                    AppointmentId = isBooked ? apt["AppointmentId"].AsString : null,
                    AppointmentType = isBooked && apt.Contains("AppointmentType")
                        ? Enum.Parse<AppointmentType>(apt["AppointmentType"].AsString, true)
                        : null
                };
            }).ToList();

            return slotInfos;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting appointment slots for date");
            throw;
        }
    }

    #region Private Helper Methods

    private async Task PopulateAppointmentStatsAsync(
        ScheduleCard card,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            var totalSlots = CalculateTotalSlots(card.StartTime, card.EndTime);

            var daysInRange = (endDate - startDate).Days + 1;
            var daysWithThisSchedule = 0;

            for (int i = 0; i < daysInRange; i++)
            {
                var currentDate = startDate.AddDays(i);
                var currentDayOfWeek = (int)currentDate.DayOfWeek;
                if (currentDayOfWeek == 0) currentDayOfWeek = 7;

                if (currentDayOfWeek == card.DayOfWeek)
                {
                    daysWithThisSchedule++;
                }
            }

            card.TotalSlotsAvailable = totalSlots * daysWithThisSchedule;

            var bookedCount = 0;
            for (int i = 0; i < daysInRange; i++)
            {
                var currentDate = startDate.AddDays(i);
                var currentDayOfWeek = (int)currentDate.DayOfWeek;
                if (currentDayOfWeek == 0) currentDayOfWeek = 7;

                if (currentDayOfWeek == card.DayOfWeek)
                {
                    bookedCount += await CountBookedSlotsAsync(card.DoctorId, currentDate);
                }
            }

            card.BookedSlotsCount = bookedCount;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error populating appointment stats");
            card.TotalSlotsAvailable = 0;
            card.BookedSlotsCount = 0;
        }
    }

    private async Task<int> CountBookedSlotsAsync(string doctorId, DateTime date)
    {
        try
        {
            var appointmentsCollection = _context.GetCollection<Appointment>("appointments");
            var dateStart = date.Date;
            var dateEnd = date.Date.AddDays(1);

            var filterBuilder = Builders<Appointment>.Filter;

            var filter = filterBuilder.And(
                filterBuilder.Eq(a => a.DoctorId, doctorId),
                filterBuilder.Gte(a => a.AppointmentDate, dateStart),
                filterBuilder.Lt(a => a.AppointmentDate, dateEnd),
                filterBuilder.Ne(a => a.Status, AppointmentStatus.Cancelled)
            );

            var count = await appointmentsCollection.CountDocumentsAsync(filter);

            return (int)count;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error counting booked slots");
            return 0;
        }
    }

    private int CalculateTotalSlots(string startTime, string endTime)
    {
        try
        {
            var start = TimeSpan.Parse(startTime);
            var end = TimeSpan.Parse(endTime);
            var slotDuration = TimeSpan.FromMinutes(30);

            var totalMinutes = (end - start).TotalMinutes;
            return (int)(totalMinutes / slotDuration.TotalMinutes);
        }
        catch(Exception ex)
        {
            
            return 0;
        }
    }

    private List<string> GenerateTimeSlots(string startTime, string endTime)
    {
        var slots = new List<string>();

        try
        {
            var start = TimeSpan.Parse(startTime);
            var end = TimeSpan.Parse(endTime);
            var interval = TimeSpan.FromMinutes(30);

            var current = start;
            while (current < end)
            {
                slots.Add(current.ToString(@"hh\:mm"));
                current = current.Add(interval);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error generating time slots");
        }

        return slots;
    }

    #endregion
}