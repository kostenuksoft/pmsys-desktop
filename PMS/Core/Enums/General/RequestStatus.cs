using System.Runtime.Serialization;

namespace PMS.Core.Enums.General;

public enum RequestStatus
{
    [EnumMember(Value ="pending")]
    Pending,
    [EnumMember(Value ="approved")]
    Approved,
    [EnumMember(Value ="rejected")]
    Rejected
}