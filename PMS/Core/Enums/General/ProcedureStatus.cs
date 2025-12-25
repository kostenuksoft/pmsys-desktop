using System.Runtime.Serialization;

namespace PMS.Core.Enums.General;

public enum ProcedureStatus
{
    [EnumMember(Value ="prescribed")]
    Prescribed,
    [EnumMember(Value ="scheduled")]
    Scheduled,
    [EnumMember(Value ="completed")]
    Completed,
    [EnumMember(Value ="cancelled")]
    Cancelled
}