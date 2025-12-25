using System.Runtime.Serialization;

namespace PMS.Core.Enums.General;

public enum UserRole
{
    [EnumMember(Value = "guest")]
    Guest,
    [EnumMember(Value = "authorized")]
    Authorized,
    [EnumMember(Value = "operator")]
    Operator,
    [EnumMember(Value = "administrator")]
    Administrator
}