using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PMS.Core.Models;

[BsonIgnoreExtraElements]
public partial class Examination : BaseEntity
{
    [BsonElement("appointment_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string AppointmentId { get; set; } = string.Empty;

    [BsonElement("patient_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PatientId { get; set; } = string.Empty;

    [BsonElement("doctor_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string DoctorId { get; set; } = string.Empty;

    [BsonElement("examination_date")]
    public DateTime ExaminationDate { get; set; }

    [BsonElement("anamnesis")]
    public string Anamnesis { get; set; } = string.Empty;

    [BsonElement("objective_status")]
    public string ObjectiveStatus { get; set; } = string.Empty;

    [BsonElement("diagnosis_ids")]
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> DiagnosisIds { get; set; } = new();

    [BsonElement("recommendations")]
    public string Recommendations { get; set; } = string.Empty;

    [BsonElement("sick_leave_from")]
    public DateTime? SickLeaveFrom { get; set; }

    [BsonElement("sick_leave_to")]
    public DateTime? SickLeaveTo { get; set; }

    [BsonElement("follow_up_date")]
    public DateTime? FollowUpDate { get; set; }
}