using System;
using MongoDB.Bson.Serialization.Attributes;

namespace PMS.Core.Models;

[BsonIgnoreExtraElements]
public class Keys : BaseEntity
{
    [BsonElement("login")]
    public string Login { get; set; } = string.Empty;

    [BsonElement("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("access_rights")]
      
    public KeyAccessRights AccessRights { get; set; } = new();

    [BsonElement("last_password_change")]
    public DateTime LastPasswordChange { get; set; } = DateTime.UtcNow;

    [BsonElement("password_expires")]
    public DateTime? PasswordExpires { get; set; }

    [BsonElement("account_locked")]
    public bool AccountLocked { get; set; }

    [BsonElement("failed_login_attempts")]
    public int FailedLoginAttempts { get; set; }

    [BsonElement("last_failed_attempt")]
    public DateTime? LastFailedAttempt { get; set; }
}