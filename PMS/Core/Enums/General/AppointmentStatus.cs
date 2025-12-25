using System.Runtime.Serialization;

namespace PMS.Core.Enums.General;

public enum AppointmentStatus
{
    [EnumMember(Value = "scheduled")]
    Scheduled,
    [EnumMember(Value = "in_progress")]
    InProgress,
    [EnumMember(Value = "completed")]
    Completed,
    [EnumMember(Value = "cancelled")]
    Cancelled,
    [EnumMember(Value = "no_show")]
    NoShow
}