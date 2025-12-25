using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PMS.Core.Enums.General;

namespace PMS.Core.Models;

[BsonIgnoreExtraElements]
public class Certificate : BaseEntity
{
    
    [BsonElement("certificate_number")]
    public string CertificateNumber { get; set; } = string.Empty;

    [BsonElement("type")]
      
    public CertificateType Type { get; set; }

    [BsonElement("patient_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PatientId { get; set; } = string.Empty;

    [BsonElement("doctor_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string DoctorId { get; set; } = string.Empty;

    [BsonElement("issue_date")]
    public DateTime IssueDate { get; set; }

    [BsonElement("valid_from")]
    public DateTime ValidFrom { get; set; }

    [BsonElement("valid_until")]
    public DateTime? ValidUntil { get; set; }

    [BsonElement("diagnosis_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? DiagnosisId { get; set; }

    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    [BsonElement("purpose")]
    public string? Purpose { get; set; }

    [BsonElement("created_by")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CreatedBy { get; set; } = string.Empty;
}