using System.Runtime.Serialization;

namespace PMS.Core.Enums.General;

public enum CertificateType
{
    [EnumMember(Value = "sick_leave")]
    SickLeave,
    [EnumMember(Value = "health")]
    Health,
    [EnumMember(Value = "vaccination")]
    Vaccination,
    [EnumMember(Value = "driver")]
    Driver,
    [EnumMember(Value = "pool")]
    Pool,
    [EnumMember(Value = "work")]
    Work,
    [EnumMember(Value = "education")]
    Education,
    [EnumMember(Value = "dispensary")]
    Dispensary
}