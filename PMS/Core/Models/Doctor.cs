using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PMS.Core.Enums.General;

namespace PMS.Core.Models;

[BsonIgnoreExtraElements]
public partial class Doctor : BaseEntity
{
    [BsonElement("employee_number")]
    public string EmployeeNumber { get; set; } = string.Empty;

    [BsonElement("full_name")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("birth_date")]
    public DateTime? BirthDate { get; set; }

    [BsonElement("specialty_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string SpecialtyId { get; set; } = string.Empty;

    [BsonElement("category")]
      
    public DoctorCategory Category { get; set; }

    [BsonElement("experience_years")]
    public int ExperienceYears { get; set; }

    [BsonElement("hire_date")]
    public DateTime HireDate { get; set; }

    [BsonElement("phone")]
    public string? Phone { get; set; }

    [BsonElement("email")]
    public string? Email { get; set; }

    [BsonElement("room_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? RoomId { get; set; }

    [BsonElement("is_district_doctor")]
    public bool IsDistrictDoctor { get; set; }

    [BsonElement("district_area")]
    public string? DistrictArea { get; set; }

    [BsonElement("is_active")]
    public bool IsActive { get; set; } = true;

    [BsonElement("certifications")]
    public List<Certification> Certifications { get; set; } = [];

}