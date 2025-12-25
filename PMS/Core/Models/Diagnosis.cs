using MongoDB.Bson.Serialization.Attributes;

namespace PMS.Core.Models;

[BsonIgnoreExtraElements]
public class Diagnosis : BaseEntity
{
    [BsonElement("icd_code")]
    public string IcdCode { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("category")]
    public string Category { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;
}