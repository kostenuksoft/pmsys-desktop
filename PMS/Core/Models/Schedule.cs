using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PMS.Core.Enums.General;

namespace PMS.Core.Models;

[BsonIgnoreExtraElements]
public class Schedule : BaseEntity
{
    [BsonElement("entity_type")]
      
    public EntityType EntityType { get; set; }

    [BsonElement("entity_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string EntityId { get; set; } = string.Empty;

    [BsonElement("day_of_week")]
    public int DayOfWeek { get; set; }

    [BsonElement("shift")]
      
    public Shift Shift { get; set; }

    [BsonElement("start_time")]
    public string StartTime { get; set; } = string.Empty;

    [BsonElement("end_time")]
    public string EndTime { get; set; } = string.Empty;

    [BsonElement("room_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? RoomId { get; set; }

    [BsonElement("effective_from")]
    public DateTime EffectiveFrom { get; set; }

    [BsonElement("effective_until")]
    public DateTime? EffectiveUntil { get; set; }
}