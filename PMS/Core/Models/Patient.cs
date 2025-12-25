using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PMS.Core.Database;
using PMS.Core.Enums.General;
using PMS.Core.Models.Common;

namespace PMS.Core.Models;

[BsonIgnoreExtraElements]
public partial class Patient : BaseEntity
{
    [BsonElement("medical_record_number")]
    public string MedicalRecordNumber { get; set; } = string.Empty;

    [BsonElement("full_name")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("birth_date")]
    public DateTime BirthDate { get; set; }

    [BsonElement("gender")]
    public Gender Gender { get; set; }

    [BsonElement("address")]
    public Address Address { get; set; } = new();

    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    [BsonElement("alternative_phone")]
    public string? AlternativePhone { get; set; }

    [BsonElement("email")]
    public string? Email { get; set; }

    [BsonElement("assigned_doctor_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? AssignedDoctorId { get; set; }

    [BsonElement("health_status")]
    public HealthStatus HealthStatus { get; set; }

    [BsonElement("blood_type")]
    public BloodType? BloodType { get; set; }

    [BsonElement("allergies")]
    public List<string> Allergies { get; set; } = [];

    [BsonElement("registration_date")]
    public DateTime RegistrationDate { get; set; }

    [BsonElement("is_active")]
    public bool IsActive { get; set; } = true;

}