using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using PMS.Core.Enums.General;

namespace PMS.Core.Models;

public class KeyAccessRights
{
    [BsonElement("database_access")]
      
    public DatabaseAccessLevel DatabaseAccess { get; set; }

    [BsonElement("role")]
      
    public UserRole Role { get; set; }

    [BsonElement("specific_permissions")]
    public List<string> SpecificPermissions { get; set; } = new();
}