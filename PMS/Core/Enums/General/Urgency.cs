using System.Runtime.Serialization;

namespace PMS.Core.Enums.General;

public enum Urgency
{
    [EnumMember(Value = "regular")]
    Regular,
    [EnumMember(Value = "urgent")]
    Urgent,
    [EnumMember(Value = "emergency")]
    Emergency
}