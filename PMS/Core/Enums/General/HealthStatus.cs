using System.Runtime.Serialization;

namespace PMS.Core.Enums.General;

public enum HealthStatus
{
    [EnumMember(Value = "healthy")]
    Healthy,
    [EnumMember(Value = "chronic")]
    Chronic,
    [EnumMember(Value = "acute")]
    Acute,
    [EnumMember(Value = "recovery")]
    Recovery,
    [EnumMember(Value = "observation")]
    Observation
}