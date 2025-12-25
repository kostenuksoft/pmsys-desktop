using System;
using MongoDB.Bson.Serialization.Attributes;

namespace PMS.Core.Models;

public class Certification
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("issue_date")]
    public DateTime IssueDate { get; set; }

    [BsonElement("expiry_date")]
    public DateTime ExpiryDate { get; set; }

}