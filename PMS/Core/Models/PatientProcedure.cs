using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PMS.Core.Enums.General;

namespace PMS.Core.Models;

[BsonIgnoreExtraElements]
public class PatientProcedure : BaseEntity
{
    [BsonElement("patient_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PatientId { get; set; } = string.Empty;

    [BsonElement("procedure_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProcedureId { get; set; } = string.Empty;

    [BsonElement("prescribed_by")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PrescribedBy { get; set; } = string.Empty;

    [BsonElement("prescribed_date")]
    public DateTime PrescribedDate { get; set; }

    [BsonElement("performed_by")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? PerformedBy { get; set; }

    [BsonElement("performed_date")]
    public DateTime? PerformedDate { get; set; }

    [BsonElement("room_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? RoomId { get; set; }

    [BsonElement("status")]
      
    public ProcedureStatus Status { get; set; }

    [BsonElement("results")]
    public string? Results { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }
}