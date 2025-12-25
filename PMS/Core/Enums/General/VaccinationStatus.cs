using System.Runtime.Serialization;

namespace PMS.Core.Enums.General;

public enum VaccinationStatus
{
    [EnumMember(Value ="scheduled")]
    Scheduled,
    [EnumMember(Value ="completed")]
    Completed,
    [EnumMember(Value ="missed")]
    Missed,
    [EnumMember(Value ="contraindicated")]
    Contraindicated
}