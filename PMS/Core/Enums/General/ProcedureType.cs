using System.Runtime.Serialization;

namespace PMS.Core.Enums.General;

public enum ProcedureType
{
 
    [EnumMember(Value = "diagnostic")]
    Diagnostic,
    [EnumMember(Value = "therapeutic")]
    Therapeutic,
    [EnumMember(Value = "physical_therapy")]
    PhysicalTherapy,
    [EnumMember(Value = "laboratory")]
    Laboratory,
    [EnumMember(Value = "imaging")]
    Imaging,
    [EnumMember(Value = "vaccination")]
    Vaccination,
    [EnumMember(Value = "preventive")]
    Preventive,
    [EnumMember(Value = "rehabilitation")]
    Rehabilitation,
    [EnumMember(Value = "emergency")]
    Emergency
}