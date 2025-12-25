using MongoDB.Bson.Serialization.Attributes;

namespace PMS.Core.Models;

[BsonIgnoreExtraElements]
public class Specialty : BaseEntity
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("code")]
    public string Code { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("is_therapist")]
    public bool IsTherapist { get; set; }
}