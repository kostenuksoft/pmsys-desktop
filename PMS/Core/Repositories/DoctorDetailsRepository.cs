using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using PMS.Core.Database;
using PMS.Core.Enums.General;
using PMS.Core.Models;
using PMS.Core.Models.DTO;
using PMS.Core.Repositories.Interfaces;
using Serilog;

namespace PMS.Core.Repositories;

public class DoctorDetailsRepository : IDoctorDetailsRepository
{
    private readonly IDatabaseContext _context;
    private readonly ILogger _logger;

    public DoctorDetailsRepository(IDatabaseContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<CertificateDetail>> GetDoctorCertificatesAsync(
        string doctorId,
        int page,
        int pageSize,
        string? searchText = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var certificatesCollection = _context.GetCollection<Certificate>("certificates");

            var filterBuilder = Builders<Certificate>.Filter;
            var filters = new List<FilterDefinition<Certificate>>
            {
                filterBuilder.Eq(c => c.DoctorId, doctorId)
            };

            if (startDate.HasValue)
            {
                filters.Add(filterBuilder.Gte(c => c.IssueDate, startDate.Value));
            }

            if (endDate.HasValue)
            {
                filters.Add(filterBuilder.Lte(c => c.IssueDate, endDate.Value));
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filters.Add(filterBuilder.Regex(c => c.Content,
                    new BsonRegularExpression(searchText, "i")));
            }

            var filter = filterBuilder.And(filters);

            var pipeline = new[]
            {
                new BsonDocument("$match", filter.Render(
                    BsonSerializer.SerializerRegistry.GetSerializer<Certificate>(),
                    BsonSerializer.SerializerRegistry)),
                new BsonDocument("$sort", new BsonDocument("issue_date", -1)),
                new BsonDocument("$skip", (page - 1) * pageSize),
                new BsonDocument("$limit", pageSize),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "patients" },
                    { "localField", "patient_id" },
                    { "foreignField", "_id" },
                    { "as", "patient_info" }
                }),
                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$patient_info" },
                    { "preserveNullAndEmptyArrays", true }
                }),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "doctors" },
                    { "localField", "doctor_id" },
                    { "foreignField", "_id" },
                    { "as", "doctor_info" }
                }),
                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$doctor_info" },
                    { "preserveNullAndEmptyArrays", true }
                }),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "diagnoses" },
                    { "localField", "diagnosis_id" },
                    { "foreignField", "_id" },
                    { "as", "diagnosis_info" }
                }),
                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$diagnosis_info" },
                    { "preserveNullAndEmptyArrays", true }
                }),

                new BsonDocument("$project", new BsonDocument
                {
                    { "CertificateId", new BsonDocument("$toString", "$_id") },
                    { "PatientId", new BsonDocument("$toString", "$patient_id") },
                    { "PatientName", "$patient_info.full_name" },
                    { "DoctorId", new BsonDocument("$toString", "$doctor_id") },
                    { "DoctorName", "$doctor_info.full_name" },
                    { "Type", "$type" },
                    { "IssueDate", "$issue_date" },
                    { "ValidFrom", "$valid_from" },
                    { "ValidUntil", "$valid_until" },
                    { "DiagnosisId", new BsonDocument("$toString", "$diagnosis_id") },
                    { "DiagnosisCode", "$diagnosis_info.icd_code" },
                    { "DiagnosisName", "$diagnosis_info.name" },
                    { "Content", "$content" },
                    { "Purpose", "$purpose" },
                    { "CreatedById", new BsonDocument("$toString", "$created_by") },
                    { "CreatedByName", "" }
                })
            };

            var results = await certificatesCollection
                .Aggregate<BsonDocument>(pipeline)
                .ToListAsync();

            return results.Select(doc => new CertificateDetail
            {
                Id = doc.GetValue("Id", BsonNull.Value).IsBsonNull ? "" : doc["Id"].AsString,
                PatientId = doc.GetValue("PatientId", BsonNull.Value).IsBsonNull ? "" : doc["PatientId"].AsString,
                PatientName = doc.GetValue("PatientName", BsonNull.Value).IsBsonNull ? "" : doc["PatientName"].AsString,
                DoctorId = doc.GetValue("DoctorId", BsonNull.Value).IsBsonNull ? "" : doc["DoctorId"].AsString,
                DoctorName = doc.GetValue("DoctorName", BsonNull.Value).IsBsonNull ? "" : doc["DoctorName"].AsString,

                Type = doc.Contains("Type") && !doc["Type"].IsBsonNull
                                            && Enum.TryParse<CertificateType>(doc["Type"].AsString, out var certType)
                    ? nameof(certType)
                    : nameof(CertificateType.Health),

                IssueDate = doc.GetValue("IssueDate", DateTime.MinValue).ToUniversalTime(),
                ValidFrom = doc.GetValue("ValidFrom", DateTime.MinValue).ToUniversalTime(),

                ValidUntil = doc.Contains("ValidUntil") && !doc["ValidUntil"].IsBsonNull
                    ? doc["ValidUntil"].ToUniversalTime()
                    : (DateTime?)null,

                DiagnosisId = doc.Contains("DiagnosisId") && !doc["DiagnosisId"].IsBsonNull
                    ? doc["DiagnosisId"].AsString
                    : null,
                DiagnosisCode = doc.Contains("DiagnosisCode") && !doc["DiagnosisCode"].IsBsonNull
                    ? doc["DiagnosisCode"].AsString
                    : null,
                DiagnosisName = doc.Contains("DiagnosisName") && !doc["DiagnosisName"].IsBsonNull
                    ? doc["DiagnosisName"].AsString
                    : null,

                Content = doc.GetValue("Content", BsonNull.Value).IsBsonNull ? "" : doc["Content"].AsString,

                Purpose = doc.Contains("Purpose") && !doc["Purpose"].IsBsonNull
                    ? doc["Purpose"].AsString
                    : null,

                CreatedById = doc.GetValue("CreatedById", BsonNull.Value).IsBsonNull ? "" : doc["CreatedById"].AsString,
                CreatedByName = doc.GetValue("CreatedByName", BsonNull.Value).IsBsonNull
                    ? ""
                    : doc["CreatedByName"].AsString
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting doctor certificates for doctor {DoctorId}", doctorId);
            throw;
        }
    }

    public async Task<List<PatientWithDoctor>> GetDoctorPatientsAsync(
        string doctorId,
        int page,
        int pageSize,
        string? searchText = null)
    {
        try
        {
            var patientsCollection = _context.GetCollection<Patient>("patients");

            var filterBuilder = Builders<Patient>.Filter;
            var filters = new List<FilterDefinition<Patient>>
            {
                filterBuilder.Eq(p => p.AssignedDoctorId, doctorId)
            };

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filters.Add(filterBuilder.Regex(p => p.FullName,
                    new BsonRegularExpression(searchText, "i")));
            }

            var filter = filterBuilder.And(filters);

            var patients = await patientsCollection
                .Find(filter)
                .Sort(Builders<Patient>.Sort.Ascending(p => p.FullName))
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return patients.Select(p => new PatientWithDoctor
            {
                PatientId = p.Id,
                FullName = p.FullName,
                MedicalRecordNumber = p.MedicalRecordNumber,
                BirthDate = p.BirthDate,
                Phone = p.Phone,
                Email = p.Email,
                IsActive = p.IsActive,
                DoctorName = p.AssignedDoctorName,
                RegistrationDate = p.RegistrationDate
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting patients for doctor {DoctorId}", doctorId);
            throw;
        }
    }

    public async Task<List<ExaminationDetail>> GetDoctorExaminationsAsync(
        string doctorId,
        int page,
        int pageSize,
        string? searchText = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var examinationsCollection = _context.GetCollection<Examination>("examinations");

            var filterBuilder = Builders<Examination>.Filter;
            var filters = new List<FilterDefinition<Examination>>
            {
                filterBuilder.Eq(e => e.DoctorId, doctorId)
            };

            if (startDate.HasValue)
            {
                filters.Add(filterBuilder.Gte(e => e.ExaminationDate, startDate.Value));
            }

            if (endDate.HasValue)
            {
                filters.Add(filterBuilder.Lte(e => e.ExaminationDate, endDate.Value));
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var searchFilter = filterBuilder.Or(
                    filterBuilder.Regex(e => e.Anamnesis, new BsonRegularExpression(searchText, "i")),
                    filterBuilder.Regex(e => e.ObjectiveStatus, new BsonRegularExpression(searchText, "i")),
                    filterBuilder.Regex(e => e.Recommendations, new BsonRegularExpression(searchText, "i"))
                );
                filters.Add(searchFilter);
            }

            var filter = filterBuilder.And(filters);

            var pipeline = new[]
            {
                new BsonDocument("$match", filter.Render(
                    BsonSerializer.SerializerRegistry.GetSerializer<Examination>(),
                    BsonSerializer.SerializerRegistry)),
                new BsonDocument("$sort", new BsonDocument("examination_date", -1)),
                new BsonDocument("$skip", (page - 1) * pageSize),
                new BsonDocument("$limit", pageSize),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "doctors" },
                    { "localField", "doctor_id" },
                    { "foreignField", "_id" },
                    { "as", "doctor_info" }
                }),
                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$doctor_info" },
                    { "preserveNullAndEmptyArrays", true }
                }),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "specialties" },
                    { "localField", "doctor_info.specialty_id" },
                    { "foreignField", "_id" },
                    { "as", "specialty_info" }
                }),
                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$specialty_info" },
                    { "preserveNullAndEmptyArrays", true }
                }),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "diagnoses" },
                    { "localField", "diagnosis_ids" },
                    { "foreignField", "_id" },
                    { "as", "diagnoses_info" }
                }),

                new BsonDocument("$project", new BsonDocument
                {
                    { "Id", new BsonDocument("$toString", "$_id") },
                    { "AppointmentId", new BsonDocument("$toString", "$appointment_id") },
                    { "PatientId", new BsonDocument("$toString", "$patient_id") },
                    { "DoctorId", new BsonDocument("$toString", "$doctor_id") },
                    { "DoctorName", "$doctor_info.full_name" },
                    { "DoctorSpecialty", "$specialty_info.name" },
                    { "ExaminationDate", "$examination_date" },
                    { "Anamnesis", "$anamnesis" },
                    { "ObjectiveStatus", "$objective_status" },
                    { "Recommendations", "$recommendations" },
                    { "SickLeaveFrom", "$sick_leave_from" },
                    { "SickLeaveTo", "$sick_leave_to" },
                    { "FollowUpDate", "$follow_up_date" },
                    { "Diagnoses", "$diagnoses_info" }
                })
            };

            var results = await examinationsCollection
                .Aggregate<BsonDocument>(pipeline)
                .ToListAsync();

            return results.Select(doc => new ExaminationDetail
            {
                Id = doc.Contains("_id") && doc["_id"].IsObjectId
                    ? doc["_id"].AsObjectId.ToString()
                    : "",
                AppointmentId = doc.Contains("AppointmentId") && doc["AppointmentId"].IsObjectId
                    ? doc["AppointmentId"].AsObjectId.ToString()
                    : "",
                PatientId = doc.Contains("PatientId") && doc["PatientId"].IsObjectId
                    ? doc["PatientId"].AsObjectId.ToString()
                    : "",
                DoctorId = doc.Contains("DoctorId") && doc["DoctorId"].IsObjectId
                    ? doc["DoctorId"].AsObjectId.ToString()
                    : "",
                DoctorName = doc.Contains("DoctorName") && !doc["DoctorName"].IsBsonNull
                    ? doc["DoctorName"].AsString
                    : "",
                DoctorSpecialty = doc.Contains("DoctorSpecialty") && !doc["DoctorSpecialty"].IsBsonNull
                    ? doc["DoctorSpecialty"].AsString
                    : "",
                ExaminationDate = doc.GetValue("ExaminationDate", DateTime.MinValue).ToUniversalTime(),
                Anamnesis = doc.Contains("Anamnesis") && !doc["Anamnesis"].IsBsonNull
                    ? doc["Anamnesis"].AsString
                    : null,
                ObjectiveStatus = doc.Contains("ObjectiveStatus") && !doc["ObjectiveStatus"].IsBsonNull
                    ? doc["ObjectiveStatus"].AsString
                    : null,
                Recommendations = doc.Contains("Recommendations") && !doc["Recommendations"].IsBsonNull
                    ? doc["Recommendations"].AsString
                    : null,
                SickLeaveFrom = doc.Contains("SickLeaveFrom") && !doc["SickLeaveFrom"].IsBsonNull
                    ? doc["SickLeaveFrom"].ToUniversalTime()
                    : null,
                SickLeaveTo = doc.Contains("SickLeaveTo") && !doc["SickLeaveTo"].IsBsonNull
                    ? doc["SickLeaveTo"].ToUniversalTime()
                    : null,
                FollowUpDate = doc.Contains("FollowUpDate") && !doc["FollowUpDate"].IsBsonNull
                    ? doc["FollowUpDate"].ToUniversalTime()
                    : null,
                Diagnoses = doc.Contains("Diagnoses") && doc["Diagnoses"].IsBsonArray
                    ? doc["Diagnoses"].AsBsonArray
                        .Select(d => d.AsBsonDocument)
                        .Select(d => new DiagnosisInfo
                        {
                            Id = d.Contains("_id") && d["_id"].IsObjectId
                                ? d["_id"].AsObjectId.ToString()
                                : "",
                            IcdCode = d.Contains("icd_code") && !d["icd_code"].IsBsonNull
                                ? d["icd_code"].AsString
                                : "",
                            Name = d.Contains("name") && !d["name"].IsBsonNull
                                ? d["name"].AsString
                                : ""
                        }).ToList()
                    : []
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting examinations for doctor {DoctorId}", doctorId);
            throw;
        }
    }

    public async Task<DoctorAppointmentStats> GetDoctorAppointmentStatsAsync(string doctorId)
    {
        try
        {
            var appointmentsCollection = _context.GetCollection<Appointment>("appointments");
            var now = DateTime.UtcNow;
            var today = now.Date;

            var total = await appointmentsCollection.CountDocumentsAsync(a => a.DoctorId == doctorId);

            var completed = await appointmentsCollection.CountDocumentsAsync(a =>
                a.DoctorId == doctorId && a.Status == AppointmentStatus.Completed);

            var cancelled = await appointmentsCollection.CountDocumentsAsync(a =>
                a.DoctorId == doctorId && a.Status == AppointmentStatus.Cancelled);

            var upcoming = await appointmentsCollection.CountDocumentsAsync(a =>
                a.DoctorId == doctorId &&
                a.Status == AppointmentStatus.Scheduled &&
                a.AppointmentDate >= now);

            var todayAppointments = await appointmentsCollection.CountDocumentsAsync(a =>
                a.DoctorId == doctorId &&
                a.AppointmentDate >= today &&
                a.AppointmentDate < today.AddDays(1));

            return new DoctorAppointmentStats
            {
                DoctorId = doctorId,
                TotalAppointments = (int)total,
                CompletedAppointments = (int)completed,
                CancelledAppointments = (int)cancelled,
                UpcomingAppointments = (int)upcoming,
                TodayAppointments = (int)todayAppointments
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting appointment stats for doctor {DoctorId}", doctorId);
            throw;
        }
    }

    public async Task<DoctorPatientStats> GetDoctorPatientStatsAsync(string doctorId)
    {
        try
        {
            var patientsCollection = _context.GetCollection<Patient>("patients");
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var total = await patientsCollection.CountDocumentsAsync(p => p.AssignedDoctorId == doctorId);

            var active = await patientsCollection.CountDocumentsAsync(p =>
                p.AssignedDoctorId == doctorId && p.IsActive);

            var thisMonth = await patientsCollection.CountDocumentsAsync(p =>
                p.AssignedDoctorId == doctorId &&
                p.RegistrationDate >= monthStart);

            return new DoctorPatientStats
            {
                DoctorId = doctorId,
                TotalPatients = (int)total,
                ActivePatients = (int)active,
                PatientsThisMonth = (int)thisMonth,
                PatientsToday = 0
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting patient stats for doctor {DoctorId}", doctorId);
            throw;
        }
    }


    public async Task<long> CountDoctorCertificatesAsync(string doctorId)
    {
        try
        {
            var certificatesCollection = _context.GetCollection<Certificate>("certificates");
            return await certificatesCollection.CountDocumentsAsync(c => c.DoctorId == doctorId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error counting certificates for doctor {DoctorId}", doctorId);
            throw;
        }
    }

    public async Task<long> CountDoctorExaminationsAsync(string doctorId)
    {
        try
        {
            var examinationsCollection = _context.GetCollection<Examination>("examinations");
            return await examinationsCollection.CountDocumentsAsync(e => e.DoctorId == doctorId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error counting examinations for doctor {DoctorId}", doctorId);
            throw;
        }
    }

    public async Task<long> CountDoctorPatientsAsync(string doctorId)
    {
        try
        {
            var patientsCollection = _context.GetCollection<Patient>("patients");
            return await patientsCollection.CountDocumentsAsync(p => p.AssignedDoctorId == doctorId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error counting patients for doctor {DoctorId}", doctorId);
            throw;
        }
    }
}