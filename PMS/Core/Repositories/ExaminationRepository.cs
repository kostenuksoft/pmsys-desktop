using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using PMS.Core.Database;
using PMS.Core.Models;
using PMS.Core.Models.DTO;
using PMS.Core.Repositories.Interfaces;
using Serilog;

namespace PMS.Core.Repositories;

public class ExaminationRepository : BaseRepository<Examination>, IExaminationRepository
{

    public ExaminationRepository(IDatabaseContext context, ILogger logger)
        : base(context, "examinations", logger)
    {
    }

    

    public async Task<(IEnumerable<Examination> examinations, long totalCount)> GetPagedExaminationsAsync(
        int page,
        int pageSize,
        string? searchText = null,
        string? patientId = null,
        string? doctorId = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null)
    {
        try
        {
            var filterBuilder = Builders<Examination>.Filter;
            var filter = filterBuilder.Empty;

            if (!string.IsNullOrWhiteSpace(patientId))
            {
                filter &= filterBuilder.Eq(e => e.PatientId, patientId);
            }

            if (!string.IsNullOrWhiteSpace(doctorId))
            {
                filter &= filterBuilder.Eq(e => e.DoctorId, doctorId);
            }

            if (dateFrom.HasValue)
            {
                filter &= filterBuilder.Gte(e => e.ExaminationDate, dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                filter &= filterBuilder.Lte(e => e.ExaminationDate, dateTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var textFilter = filterBuilder.Or(
                    filterBuilder.Regex(e => e.Anamnesis, new BsonRegularExpression(searchText, "i")),
                    filterBuilder.Regex(e => e.ObjectiveStatus, new BsonRegularExpression(searchText, "i")),
                    filterBuilder.Regex(e => e.Recommendations, new BsonRegularExpression(searchText, "i"))
                );
                filter &= textFilter;
            }

            var totalCount = await Collection.CountDocumentsAsync(filter);

            var examinations = await Collection
                .Find(filter)
                .SortByDescending(e => e.ExaminationDate)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            Logger.Information(
                "Retrieved {Count} examinations (page {Page}/{TotalPages})",
                examinations.Count,
                page,
                Math.Ceiling((double)totalCount / pageSize));

            return (examinations, totalCount);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting paged examinations");
            throw;
        }
    }

 
    public async Task<List<PatientWithMultipleDoctors>> GetPatientsWithMultipleDoctorsPerWeekAsync(
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument
                {
                    { "examination_date", new BsonDocument
                        {
                            { "$gte", startDate },
                            { "$lte", endDate }
                        }
                    }
                }),

                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$patient_id" },
                    { "doctorIds", new BsonDocument("$addToSet", "$doctor_id") },
                    { "examinationCount", new BsonDocument("$sum", 1) },
                    { "examinationDates", new BsonDocument("$push", "$examination_date") }
                }),

                new BsonDocument("$addFields", new BsonDocument
                {
                    { "uniqueDoctorCount", new BsonDocument("$size", "$doctorIds") }
                }),

                new BsonDocument("$match", new BsonDocument
                {
                    { "uniqueDoctorCount", new BsonDocument("$gt", 2) }
                }),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "patients" },
                    { "localField", "_id" },
                    { "foreignField", "_id" },
                    { "as", "patientInfo" }
                }),

                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$patientInfo" },
                    { "preserveNullAndEmptyArrays", false }
                }),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "doctors" },
                    { "localField", "doctorIds" },
                    { "foreignField", "_id" },
                    { "as", "doctors" }
                }),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "specialties" },
                    { "localField", "doctors.specialty_id" },
                    { "foreignField", "_id" },
                    { "as", "specialties" }
                }),

                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 0 },
                    { "PatientId", new BsonDocument("$toString", "$_id") },
                    { "PatientFullName", "$patientInfo.full_name" },
                    { "PatientMedicalRecordNumber", "$patientInfo.medical_record_number" },
                    { "UniqueDoctorCount", "$uniqueDoctorCount" },
                    { "TotalExaminations", "$examinationCount" },
                    { "ExaminationDates", "$examinationDates" },
                    { "Doctors", new BsonDocument("$map", new BsonDocument
                        {
                            { "input", "$doctors" },
                            { "as", "doc" },
                            { "in", new BsonDocument
                                {
                                    { "DoctorId", new BsonDocument("$toString", "$$doc._id") },
                                    { "FullName", "$$doc.full_name" },
                                    { "Specialty", new BsonDocument("$let", new BsonDocument
                                        {
                                            { "vars", new BsonDocument
                                                {
                                                    { "spec", new BsonDocument("$arrayElemAt", new BsonArray
                                                        {
                                                            new BsonDocument("$filter", new BsonDocument
                                                            {
                                                                { "input", "$specialties" },
                                                                { "as", "s" },
                                                                { "cond", new BsonDocument("$eq", new BsonArray { "$$s._id", "$$doc.specialty_id" }) }
                                                            }),
                                                            0
                                                        })
                                                    }
                                                }
                                            },
                                            { "in", "$$spec.name" }
                                        })
                                    }
                                }
                            }
                        })
                    }
                }),

                new BsonDocument("$sort", new BsonDocument
                {
                    { "UniqueDoctorCount", -1 },
                    { "PatientFullName", 1 }
                })
            };

            var pipelineDefinition = PipelineDefinition<Examination, PatientWithMultipleDoctors>.Create(pipeline);
            var result = await Collection.Aggregate(pipelineDefinition).ToListAsync();

            Logger.Information(
                "Found {Count} patients examined by >2 doctors between {Start} and {End}",
                result.Count, startDate.ToShortDateString(), endDate.ToShortDateString());

            return result;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting patients with multiple doctors");
            throw;
        }
    }


    public async Task<long> CountPatientsByDiagnosisInMonthAsync(
        string diagnosisName,
        DateTime monthStart,
        DateTime monthEnd)
    {
        try
        {
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument
                {
                    { "examination_date", new BsonDocument
                        {
                            { "$gte", monthStart },
                            { "$lte", monthEnd }
                        }
                    }
                }),

                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$diagnosis_ids" }
                }),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "diagnoses" },
                    { "localField", "diagnosis_ids" },
                    { "foreignField", "_id" },
                    { "as", "diagnosis" }
                }),

                new BsonDocument("$unwind", "$diagnosis"),

                new BsonDocument("$match", new BsonDocument
                {
                    { "diagnosis.name", new BsonRegularExpression(diagnosisName, "i") }
                }),

                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$patient_id" }
                }),

                new BsonDocument("$count", "patientCount")
            };

            var pipelineDefinition = PipelineDefinition<Examination, BsonDocument>.Create(pipeline);
            var result = await Collection.Aggregate(pipelineDefinition).FirstOrDefaultAsync();

            var count = result?["patientCount"].AsInt32 ?? 0;

            Logger.Information(
                "Found {Count} patients with diagnosis '{Diagnosis}'",
                count, diagnosisName);

            return count;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error counting patients by diagnosis");
            throw;
        }
    }


    public async Task<List<PatientDiagnosisInfo>> GetPatientsByDiagnosisInMonthAsync(
        string diagnosisName,
        DateTime monthStart,
        DateTime monthEnd)
    {
        try
        {
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument
                {
                    { "examination_date", new BsonDocument
                        {
                            { "$gte", monthStart },
                            { "$lte", monthEnd }
                        }
                    }
                }),

                new BsonDocument("$unwind", "$diagnosis_ids"),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "diagnoses" },
                    { "localField", "diagnosis_ids" },
                    { "foreignField", "_id" },
                    { "as", "diagnosis" }
                }),

                new BsonDocument("$unwind", "$diagnosis"),

                new BsonDocument("$match", new BsonDocument
                {
                    { "diagnosis.name", new BsonRegularExpression(diagnosisName, "i") }
                }),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "patients" },
                    { "localField", "patient_id" },
                    { "foreignField", "_id" },
                    { "as", "patient" }
                }),

                new BsonDocument("$unwind", "$patient"),

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

                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$specialty" },
                    { "preserveNullAndEmptyArrays", true }
                }),

                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$patient_id" },
                    { "PatientFullName", new BsonDocument("$first", "$patient.full_name") },
                    { "PatientMedicalRecordNumber", new BsonDocument("$first", "$patient.medical_record_number") },
                    { "PatientPhone", new BsonDocument("$first", "$patient.phone") },
                    { "DiagnosisName", new BsonDocument("$first", "$diagnosis.name") },
                    { "DiagnosisCode", new BsonDocument("$first", "$diagnosis.icd_code") },
                    { "ExaminationCount", new BsonDocument("$sum", 1) },
                    { "FirstExaminationDate", new BsonDocument("$min", "$examination_date") },
                    { "LastExaminationDate", new BsonDocument("$max", "$examination_date") },
                    { "Doctors", new BsonDocument("$addToSet", new BsonDocument
                        {
                            { "DoctorId", new BsonDocument("$toString", "$doctor._id") },
                            { "FullName", "$doctor.full_name" },
                            { "Specialty", "$specialty.name" }
                        })
                    }
                }),

                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 0 },
                    { "PatientId", new BsonDocument("$toString", "$_id") },
                    { "PatientFullName", 1 },
                    { "PatientMedicalRecordNumber", 1 },
                    { "PatientPhone", 1 },
                    { "DiagnosisName", 1 },
                    { "DiagnosisCode", 1 },
                    { "ExaminationCount", 1 },
                    { "FirstExaminationDate", 1 },
                    { "LastExaminationDate", 1 },
                    { "Doctors", 1 }
                }),

                new BsonDocument("$sort", new BsonDocument
                {
                    { "LastExaminationDate", -1 }
                })
            };

            var pipelineDefinition = PipelineDefinition<Examination, PatientDiagnosisInfo>.Create(pipeline);
            var result = await Collection.Aggregate(pipelineDefinition).ToListAsync();

            Logger.Information("Found {Count} patients with diagnosis details", result.Count);

            return result;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting patients by diagnosis");
            throw;
        }
    }

 
}