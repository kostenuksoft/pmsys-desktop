using System;
using MongoDB.Bson.Serialization.Attributes;
using PMS.Core.Enums.General;

namespace PMS.Core.Models;

[BsonIgnoreExtraElements]
public class User : BaseEntity
{
    [BsonElement("login")]
    public string Login { get; set; } = string.Empty;

    [BsonElement("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("role")]
      
    public UserRole Role { get; set; }

    [BsonElement("full_name")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("email")]
    public string? Email { get; set; }

    [BsonElement("phone")]
    public string? Phone { get; set; }

    [BsonElement("last_login")]
    public DateTime? LastLogin { get; set; }

    [BsonElement("is_active")]
    public bool IsActive { get; set; } = true;

    [BsonElement("access_rights")]
      
    public AccessRights AccessRights { get; set; } = new();
}