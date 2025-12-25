using MongoDB.Bson.Serialization.Attributes;
using PMS.Core.Enums.General;
using System.Collections.Generic;

namespace PMS.Core.Models;

[BsonIgnoreExtraElements]
public partial class Room : BaseEntity
{
    [BsonElement("room_number")]
    public string RoomNumber { get; set; } = string.Empty;

    [BsonElement("room_type")]
      
    public RoomType RoomType { get; set; }

    [BsonElement("floor")]
    public int Floor { get; set; }

    [BsonElement("capacity")]
    public int Capacity { get; set; }

    [BsonElement("equipment")]
    public List<string> Equipment { get; set; } = new();

    [BsonElement("is_active")]
    public bool IsActive { get; set; } = true;
}

