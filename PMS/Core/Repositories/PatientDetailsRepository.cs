using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using PMS.Core.Database;
using PMS.Core.Models.DTO;
using PMS.Core.Repositories.Interfaces;
using Serilog;

namespace PMS.Core.Repositories;

public class PatientDetailsRepository : IPatientDetailsRepository
{
    private readonly IDatabaseContext _context;
    private readonly ILogger _logger;

    public PatientDetailsRepository(IDatabaseContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    
    public async Task<List<PatientProcedureDetail>> GetPatientProceduresAsync(string patientId)
    {
        try
        {
            var collection = _context.GetCollection<BsonDocument>("patients_procedures");

            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument("patient_id", ObjectId.Parse(patientId))),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "procedures" },
                    { "localField", "procedure_id" },
                    { "foreignField", "_id" },
                    { "as", "procedure" }
                }),
                new BsonDocument("$unwind", "$procedure"),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "doctors" },
                    { "localField", "prescribed_by" },
                    { "foreignField", "_id" },
                    { "as", "prescribed_doctor" }
                }),
                new BsonDocument("$unwind", "$prescribed_doctor"),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "doctors" },
                    { "localField", "performed_by" },
                    { "foreignField", "_id" },
                    { "as", "performed_doctor" }
                }),
                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$performed_doctor" },
                    { "preserveNullAndEmptyArrays", true }
                }),

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

                new BsonDocument("$sort", new BsonDocument("prescribed_date", -1))
            };

            var results = await collection.Aggregate<BsonDocument>(pipeline).ToListAsync();

            return results.Select(doc => new PatientProcedureDetail
            {
                Id = doc["_id"].AsObjectId.ToString(),
                PatientId = doc["patient_id"].AsObjectId.ToString(),
                ProcedureId = doc["procedure_id"].AsObjectId.ToString(),
                ProcedureName = doc["procedure"]["name"].AsString,
                ProcedureCode = doc["procedure"]["code"].AsString,
                ProcedureCost = doc["procedure"]["cost"].ToDecimal(),
                PrescribedById = doc["prescribed_by"].AsObjectId.ToString(),
                PrescribedByName = doc["prescribed_doctor"]["full_name"].AsString,
                PrescribedDate = doc["prescribed_date"].ToUniversalTime(),
                PerformedById = doc.Contains("performed_by") && !doc["performed_by"].IsBsonNull
                    ? doc["performed_by"].AsObjectId.ToString()
                    : null,
                PerformedByName = doc.Contains("performed_doctor") && doc["performed_doctor"].IsBsonDocument
                    ? doc["performed_doctor"]["full_name"].AsString
                    : null,
                PerformedDate = doc.Contains("performed_date") && !doc["performed_date"].IsBsonNull
                    ? doc["performed_date"].ToUniversalTime()
                    : null,
                RoomId = doc.Contains("room_id") && !doc["room_id"].IsBsonNull
                    ? doc["room_id"].AsObjectId.ToString()
                    : null,
                RoomNumber = doc.Contains("room") && doc["room"].IsBsonDocument
                    ? doc["room"]["room_number"].AsString
                    : null,
                Status = doc["status"].AsString,
                Results = doc.Contains("results") && !doc["results"].IsBsonNull ? doc["results"].AsString : null,
                Notes = doc.Contains("notes") && !doc["notes"].IsBsonNull ? doc["notes"].AsString : null
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting patient procedures for patient {PatientId}", patientId);
            throw;
        }
    }

   
    public async Task<List<ExaminationDetail>> GetPatientExaminationsAsync(string patientId)
    {
        try
        {
            var collection = _context.GetCollection<BsonDocument>("examinations");

            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument("patient_id", ObjectId.Parse(patientId))),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "doctors" },
                    { "localField", "doctor_id" },
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

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "diagnosis" },
                    { "localField", "diagnosis_ids" },
                    { "foreignField", "_id" },
                    { "as", "diagnoses" }
                }),

                new BsonDocument("$sort", new BsonDocument("examination_date", -1))
            };

            var results = await collection.Aggregate<BsonDocument>(pipeline).ToListAsync();

            return results.Select(doc => new ExaminationDetail
            {
                Id = doc["_id"].AsObjectId.ToString(),
                AppointmentId = doc["appointment_id"].AsObjectId.ToString(),
                PatientId = doc["patient_id"].AsObjectId.ToString(),
                DoctorId = doc["doctor_id"].AsObjectId.ToString(),
                DoctorName = doc["doctor"]["full_name"].AsString,
                DoctorSpecialty = doc["specialty"]["name"].AsString,
                ExaminationDate = doc["examination_date"].ToUniversalTime(),
                Anamnesis = doc.Contains("anamnesis") && !doc["anamnesis"].IsBsonNull
                    ? doc["anamnesis"].AsString
                    : null,
                ObjectiveStatus = doc.Contains("objective_status") && !doc["objective_status"].IsBsonNull
                    ? doc["objective_status"].AsString
                    : null,
                Diagnoses = doc["diagnoses"].AsBsonArray.Select(d => new DiagnosisInfo
                {
                    Id = d["_id"].AsObjectId.ToString(),
                    IcdCode = d["icd_code"].AsString,
                    Name = d["name"].AsString,
                    Category = d["category"].AsString,
                    Description = d.AsBsonDocument.Contains("description") && !d["description"].IsBsonNull
                        ? d["description"].AsString
                        : null
                }).ToList(),
                Recommendations = doc.Contains("recommendations") && !doc["recommendations"].IsBsonNull
                    ? doc["recommendations"].AsString
                    : null,
                SickLeaveFrom = doc.Contains("sick_leave_from") && !doc["sick_leave_from"].IsBsonNull
                    ? doc["sick_leave_from"].ToUniversalTime()
                    : null,
                SickLeaveTo = doc.Contains("sick_leave_to") && !doc["sick_leave_to"].IsBsonNull
                    ? doc["sick_leave_to"].ToUniversalTime()
                    : null,
                FollowUpDate = doc.Contains("follow_up_date") && !doc["follow_up_date"].IsBsonNull
                    ? doc["follow_up_date"].ToUniversalTime()
                    : null
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting patient examinations for patient {PatientId}", patientId);
            throw;
        }
    }

  
    public async Task<List<PatientDiagnosisSummary>> GetPatientDiagnosesSummaryAsync(string patientId)
    {
        try
        {
            var collection = _context.GetCollection<BsonDocument>("examinations");

            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument("patient_id", ObjectId.Parse(patientId))),

                new BsonDocument("$unwind", "$diagnosis_ids"),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "diagnosis" },
                    { "localField", "diagnosis_ids" },
                    { "foreignField", "_id" },
                    { "as", "diagnosis" }
                }),
                new BsonDocument("$unwind", "$diagnosis"),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "doctors" },
                    { "localField", "doctor_id" },
                    { "foreignField", "_id" },
                    { "as", "doctor" }
                }),
                new BsonDocument("$unwind", "$doctor"),

                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$diagnosis_ids" },
                    { "icd_code", new BsonDocument("$first", "$diagnosis.icd_code") },
                    { "name", new BsonDocument("$first", "$diagnosis.name") },
                    { "category", new BsonDocument("$first", "$diagnosis.category") },
                    { "occurrence_count", new BsonDocument("$sum", 1) },
                    { "first_diagnosed", new BsonDocument("$min", "$examination_date") },
                    { "last_diagnosed", new BsonDocument("$max", "$examination_date") },
                    { "doctor_names", new BsonDocument("$addToSet", "$doctor.full_name") }
                }),

                new BsonDocument("$sort", new BsonDocument("last_diagnosed", -1))
            };

            var results = await collection.Aggregate<BsonDocument>(pipeline).ToListAsync();

            return results.Select(doc => new PatientDiagnosisSummary
            {
                DiagnosisId = doc["_id"].AsObjectId.ToString(),
                IcdCode = doc["icd_code"].AsString,
                Name = doc["name"].AsString,
                Category = doc["category"].AsString,
                OccurrenceCount = doc["occurrence_count"].AsInt32,
                FirstDiagnosed = doc["first_diagnosed"].ToUniversalTime(),
                LastDiagnosed = doc["last_diagnosed"].ToUniversalTime(),
                DoctorNames = doc["doctor_names"].AsBsonArray.Select(d => d.AsString).ToList()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting patient diagnoses summary for patient {PatientId}", patientId);
            throw;
        }
    }

    
    public async Task<List<AppointmentDetail>> GetPatientAppointmentsAsync(string patientId)
    {
        try
        {
            var collection = _context.GetCollection<BsonDocument>("appointments");

            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument("patient_id", ObjectId.Parse(patientId))),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "doctors" },
                    { "localField", "doctor_id" },
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

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "users" },
                    { "localField", "created_by" },
                    { "foreignField", "_id" },
                    { "as", "created_by_user" }
                }),
                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$created_by_user" },
                    { "preserveNullAndEmptyArrays", true }
                }),

                new BsonDocument("$sort", new BsonDocument("appointment_date", -1))
            };

            var results = await collection.Aggregate<BsonDocument>(pipeline).ToListAsync();

            return results.Select(doc => new AppointmentDetail
            {
                Id = doc["_id"].AsObjectId.ToString(),
                PatientId = doc["patient_id"].AsObjectId.ToString(),
                DoctorId = doc["doctor_id"].AsObjectId.ToString(),
                DoctorName = doc["doctor"]["full_name"].AsString,
                DoctorSpecialty = doc["specialty"]["name"].AsString,
                RoomId = doc.Contains("room_id") && !doc["room_id"].IsBsonNull
                    ? doc["room_id"].AsObjectId.ToString()
                    : null,
                RoomNumber = doc.Contains("room") && doc["room"].IsBsonDocument
                    ? doc["room"]["room_number"].AsString
                    : null,
                AppointmentDate = doc["appointment_date"].ToUniversalTime(),
                AppointmentTime = doc["appointment_time"].AsString,
                Type = doc["type"].AsString,
                Status = doc["status"].AsString,
                Complaints = doc.Contains("complaints") && !doc["complaints"].IsBsonNull
                    ? doc["complaints"].AsString
                    : null,
                CreatedDate = doc["created_date"].ToUniversalTime(),
                CreatedById = doc.Contains("created_by") && !doc["created_by"].IsBsonNull
                    ? doc["created_by"].AsObjectId.ToString()
                    : null,
                CreatedByName = doc.Contains("created_by_user") && doc["created_by_user"].IsBsonDocument
                    ? doc["created_by_user"]["username"].AsString
                    : null
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting patient appointments for patient {PatientId}", patientId);
            throw;
        }
    }

   
    public async Task<List<HomeVisitDetail>> GetPatientHomeVisitsAsync(string patientId)
    {
        try
        {
            var collection = _context.GetCollection<BsonDocument>("home_visits");

            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument("patient_id", ObjectId.Parse(patientId))),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "doctors" },
                    { "localField", "assigned_doctor_id" },
                    { "foreignField", "_id" },
                    { "as", "assigned_doctor" }
                }),
                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$assigned_doctor" },
                    { "preserveNullAndEmptyArrays", true }
                }),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "users" },
                    { "localField", "received_by" },
                    { "foreignField", "_id" },
                    { "as", "received_by_user" }
                }),
                new BsonDocument("$unwind", "$received_by_user"),

                new BsonDocument("$sort", new BsonDocument("call_date", -1))
            };

            var results = await collection.Aggregate<BsonDocument>(pipeline).ToListAsync();

            return results.Select(doc => new HomeVisitDetail
            {
                Id = doc["_id"].AsObjectId.ToString(),
                PatientId = doc.Contains("patient_id") && !doc["patient_id"].IsBsonNull
                    ? doc["patient_id"].AsObjectId.ToString()
                    : null,
                PatientName = doc["patient_name"].AsString,
                Address = doc["address"].AsString,
                Phone = doc["phone"].AsString,
                AlternativePhone = doc.Contains("alternative_phone") && !doc["alternative_phone"].IsBsonNull
                    ? doc["alternative_phone"].AsString
                    : null,
                CallDate = doc["call_date"].ToUniversalTime(),
                CallTime = doc["call_time"].AsString,
                Urgency = doc["urgency"].AsString,
                Symptoms = doc["symptoms"].AsString,
                AssignedDoctorId = doc.Contains("assigned_doctor_id") && !doc["assigned_doctor_id"].IsBsonNull
                    ? doc["assigned_doctor_id"].AsObjectId.ToString()
                    : null,
                AssignedDoctorName = doc.Contains("assigned_doctor") && doc["assigned_doctor"].IsBsonDocument
                    ? doc["assigned_doctor"]["full_name"].AsString
                    : null,
                VisitDate = doc.Contains("visit_date") && !doc["visit_date"].IsBsonNull
                    ? doc["visit_date"].ToUniversalTime()
                    : null,
                VisitTimeSlot = doc.Contains("visit_time_slot") && !doc["visit_time_slot"].IsBsonNull
                    ? doc["visit_time_slot"].AsString
                    : null,
                Status = doc["status"].AsString,
                StatusUpdated = doc["status_updated"].ToUniversalTime(),
                ReceivedById = doc["received_by"].AsObjectId.ToString(),
                ReceivedByName = doc["received_by_user"]["username"].AsString,
                Notes = doc.Contains("notes") && !doc["notes"].IsBsonNull
                    ? doc["notes"].AsString
                    : null,
                ExaminationId = doc.Contains("examination_id") && !doc["examination_id"].IsBsonNull
                    ? doc["examination_id"].AsObjectId.ToString()
                    : null
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting patient home visits for patient {PatientId}", patientId);
            throw;
        }
    }

    
    public async Task<List<VaccinationDetail>> GetPatientVaccinationsAsync(string patientId)
    {
        try
        {
            var collection = _context.GetCollection<BsonDocument>("vaccinations");

            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument("patient_id", ObjectId.Parse(patientId))),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "doctors" },
                    { "localField", "administered_by" },
                    { "foreignField", "_id" },
                    { "as", "administrator" }
                }),
                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$administrator" },
                    { "preserveNullAndEmptyArrays", true }
                }),

                new BsonDocument("$sort", new BsonDocument("scheduled_date", -1))
            };

            var results = await collection.Aggregate<BsonDocument>(pipeline).ToListAsync();

            return results.Select(doc => new VaccinationDetail
            {
                Id = doc["_id"].AsObjectId.ToString(),
                PatientId = doc["patient_id"].AsObjectId.ToString(),
                VaccineName = doc["vaccine_name"].AsString,
                VaccineLot = doc.Contains("vaccine_lot") && !doc["vaccine_lot"].IsBsonNull
                    ? doc["vaccine_lot"].AsString
                    : null,
                ScheduledDate = doc["scheduled_date"].ToUniversalTime(),
                AdministeredDate = doc.Contains("administered_date") && !doc["administered_date"].IsBsonNull
                    ? doc["administered_date"].ToUniversalTime()
                    : null,
                AdministeredById = doc.Contains("administered_by") && !doc["administered_by"].IsBsonNull
                    ? doc["administered_by"].AsObjectId.ToString()
                    : null,
                AdministeredByName = doc.Contains("administrator") && doc["administrator"].IsBsonDocument
                    ? doc["administrator"]["full_name"].AsString
                    : null,
                Status = doc["status"].AsString,
                DoseNumber = doc["dose_number"].AsInt32,
                Notes = doc.Contains("notes") && !doc["notes"].IsBsonNull
                    ? doc["notes"].AsString
                    : null
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting patient vaccinations for patient {PatientId}", patientId);
            throw;
        }
    }
}