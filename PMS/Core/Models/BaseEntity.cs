using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Converters;
using System;
using System.Text.Json.Serialization;

namespace PMS.Core.Models;

public abstract class BaseEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("created_date")]
    public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;

    [BsonElement("modified_date")]
    public DateTime? ModifiedDate { get; set; } = DateTime.UtcNow;


}
