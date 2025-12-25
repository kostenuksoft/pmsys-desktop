using System.Runtime.Serialization;

namespace PMS.Core.Enums.General;

public enum EntityType
{
    [EnumMember(Value = "doctor")]
    Doctor,
    [EnumMember(Value = "room")]
    Room
}