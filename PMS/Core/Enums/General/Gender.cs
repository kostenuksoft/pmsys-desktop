using System.Runtime.Serialization;

namespace PMS.Core.Enums.General;

public enum Gender
{
    [EnumMember(Value = "male")]
    Male,
    [EnumMember(Value = "female")]
    Female
}