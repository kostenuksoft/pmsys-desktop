using System.Runtime.Serialization;

namespace PMS.Core.Enums.General;

public enum BloodType
{
    [EnumMember(Value = "A+")]
    APositive,
    [EnumMember(Value = "A-")]
    ANegative,
    [EnumMember(Value = "B+")]
    BPositive,
    [EnumMember(Value = "B-")]
    BNegative,
    [EnumMember(Value = "AB+")]
    ABPositive,
    [EnumMember(Value = "AB-")]
    ABNegative,
    [EnumMember(Value = "O+")]
    OPositive,
    [EnumMember(Value = "O-")]
    ONegative
}