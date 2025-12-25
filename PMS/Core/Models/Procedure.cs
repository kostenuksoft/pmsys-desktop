using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PMS.Core.Enums.General;
using System.Collections.Generic;

namespace PMS.Core.Models;

[BsonIgnoreExtraElements]
public partial class Procedure : BaseEntity
{
    [BsonElement("procedure_code")]
    public string ProcedureCode { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("procedure_type")]
    public ProcedureType ProcedureType { get; set; }

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("duration_minutes")]
    public int DurationMinutes { get; set; }

    [BsonElement("price")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Price { get; set; }

    [BsonElement("room_type_required")]
    public RoomType? RoomTypeRequired { get; set; }

    [BsonElement("equipment_required")]
    public List<string> EquipmentRequired { get; set; } = new();

    [BsonElement("contraindications")]
    public List<string> Contraindications { get; set; } = new();

    [BsonElement("preparation_instructions")]
    public string PreparationInstructions { get; set; } = string.Empty;

    [BsonElement("is_active")]
    public bool IsActive { get; set; } = true;

    [BsonElement("requires_doctor")]
    public bool RequiresDoctor { get; set; } = true;

    [BsonElement("max_per_day")]
    public int? MaxPerDay { get; set; }
}