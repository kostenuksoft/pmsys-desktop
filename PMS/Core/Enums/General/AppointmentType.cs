using System.Runtime.Serialization;

namespace PMS.Core.Enums.General;

public enum AppointmentType
{
    [EnumMember(Value = "primary")]
    Primary,
    [EnumMember(Value = "secondary")]
    Secondary,
    [EnumMember(Value = "checkup")]
    Checkup,
    [EnumMember(Value = "follow_up")]
    FollowUp,
    [EnumMember(Value = "vaccination")]
    Vaccination,
    [EnumMember(Value = "procedure")]
    Procedure
}