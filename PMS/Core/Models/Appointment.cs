using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PMS.Core.Enums.General;

namespace PMS.Core.Models;

[BsonIgnoreExtraElements]
public partial class Appointment : BaseEntity
{
    [BsonElement("patient_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PatientId { get; set; } = string.Empty;

    [BsonElement("doctor_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string DoctorId { get; set; } = string.Empty;

    [BsonElement("room_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? RoomId { get; set; }

    [BsonElement("appointment_date")]
    public DateTime AppointmentDate { get; set; }

    [BsonElement("appointment_time")]
    public string AppointmentTime { get; set; } = string.Empty;

    [BsonElement("type")]
      
    public AppointmentType Type { get; set; }

    [BsonElement("status")]
      
    public AppointmentStatus Status { get; set; }

    [BsonElement("complaints")]
    public string? Complaints { get; set; }

    [BsonElement("created_by")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CreatedBy { get; set; } = string.Empty;
}