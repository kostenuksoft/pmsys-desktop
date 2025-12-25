using System.Runtime.Serialization;

namespace PMS.Core.Enums.General;

public enum DatabaseAccessLevel
{
    [EnumMember(Value = "none")]
    None,
    [EnumMember(Value = "read_only")]
    ReadOnly,
    [EnumMember(Value = "read_write")]
    ReadWrite,
    [EnumMember(Value = "full")]
    Full
}