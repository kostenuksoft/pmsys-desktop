using System.Runtime.Serialization;

namespace PMS.Core.Enums.General;

public enum Shift
{
    [EnumMember(Value = "first")]
    First,
    [EnumMember(Value = "second")]
    Second,
    [EnumMember(Value = "full")]
    Full
}