using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PMS.Core.Enums.General;

namespace PMS.Core.Models;

[BsonIgnoreExtraElements]
public class HomeVisit : BaseEntity
{
    [BsonElement("patient_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? PatientId { get; set; }

    [BsonElement("patient_name")]
    public string PatientName { get; set; } = string.Empty;

    [BsonElement("address")]
    public string Address { get; set; } = string.Empty;

    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    [BsonElement("alternative_phone")]
    public string? AlternativePhone { get; set; }

    [BsonElement("call_date")]
    public DateTime CallDate { get; set; }

    [BsonElement("call_time")]
    public string CallTime { get; set; } = string.Empty;

    [BsonElement("urgency")]
      
    public Urgency Urgency { get; set; }

    [BsonElement("symptoms")]
    public string Symptoms { get; set; } = string.Empty;

    [BsonElement("assigned_doctor_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? AssignedDoctorId { get; set; }

    [BsonElement("visit_date")]
    public DateTime? VisitDate { get; set; }

    [BsonElement("visit_time_slot")]
    public string? VisitTimeSlot { get; set; }

    [BsonElement("status")]
      
    public HomeVisitStatus Status { get; set; }

    [BsonElement("status_updated")]
    public DateTime StatusUpdated { get; set; }

    [BsonElement("received_by")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ReceivedBy { get; set; } = string.Empty;

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("examination_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? ExaminationId { get; set; }

    [BsonIgnore]
    public string? AssignedDoctorName { get; set; }

    [BsonIgnore]
    public string? ReceivedByName { get; set; }
}