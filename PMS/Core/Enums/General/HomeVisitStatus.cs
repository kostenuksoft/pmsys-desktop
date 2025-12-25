using System.Runtime.Serialization;

namespace PMS.Core.Enums.General;

public enum HomeVisitStatus
{
    [EnumMember(Value = "new")]
    New,
    [EnumMember(Value = "assigned")]
    Assigned,
    [EnumMember(Value = "in_progress")]
    InProgress,
    [EnumMember(Value = "completed")]
    Completed,
    [EnumMember(Value = "cancelled")]
    Cancelled
}