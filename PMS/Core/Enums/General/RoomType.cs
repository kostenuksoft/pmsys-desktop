using System.Runtime.Serialization;

namespace PMS.Core.Enums.General;


public enum RoomType
{
    [EnumMember(Value = "doctor_office")]
    DoctorOffice,
    [EnumMember(Value = "ultrasound")]
    Ultrasound,
    [EnumMember(Value = "laboratory")]
    Laboratory,
    [EnumMember(Value = "reception")]
    Reception,
    [EnumMember(Value = "adminstrative")]
    Administrative,
    [EnumMember(Value = "consultation")]
    Consultation,
    [EnumMember(Value = "procedure")]
    Procedure,
    [EnumMember(Value = "physical_therapy")]
    PhysicalTherapy,
    [EnumMember(Value = "diagnostic")]
    Diagnostic,
    [EnumMember(Value = "vaccination")]
    Vaccination
}