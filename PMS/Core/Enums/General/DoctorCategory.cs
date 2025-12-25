using System.Runtime.Serialization;

namespace PMS.Core.Enums.General;

public enum DoctorCategory
{
    [EnumMember(Value = "none")]
    None,
    [EnumMember(Value = "second")]
    Second,
    [EnumMember(Value = "first")]
    First,
    [EnumMember(Value = "highest")]
    Highest
}