
using System;

namespace PMS.Core.Models.DTO;


public class PatientProcedureDetail
{
    public string Id { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string ProcedureId { get; set; } = string.Empty;

    public string ProcedureName { get; set; } = string.Empty;
    public string ProcedureCode { get; set; } = string.Empty;
    public decimal ProcedureCost { get; set; }

    public string PrescribedById { get; set; } = string.Empty;
    public string PrescribedByName { get; set; } = string.Empty;
    public DateTime PrescribedDate { get; set; }

    public string? PerformedById { get; set; }
    public string? PerformedByName { get; set; }
    public DateTime? PerformedDate { get; set; }

    public string? RoomId { get; set; }
    public string? RoomNumber { get; set; }

    public string Status { get; set; } = string.Empty;
    public string StatusDisplay => Status switch
    {
        "prescribed" => "Призначено",
        "scheduled" => "Заплановано",
        "completed" => "Виконано",
        "cancelled" => "Скасовано",
        _ => Status
    };

    public string? Results { get; set; }
    public string? Notes { get; set; }
}