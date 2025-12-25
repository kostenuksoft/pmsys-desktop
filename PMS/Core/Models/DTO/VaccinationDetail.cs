using System;

namespace PMS.Core.Models.DTO;

public class VaccinationDetail
{
    public string Id { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string VaccineName { get; set; } = string.Empty;
    public string? VaccineLot { get; set; }
    public DateTime ScheduledDate { get; set; }
    public DateTime? AdministeredDate { get; set; }

    public string? AdministeredById { get; set; }
    public string? AdministeredByName { get; set; }

    public string Status { get; set; } = string.Empty;
    public string StatusDisplay => Status switch
    {
        "scheduled" => "Заплановано",
        "completed" => "Виконано",
        "missed" => "Пропущено",
        "contraindicated" => "Протипоказано",
        _ => Status
    };

    public int DoseNumber { get; set; }
    public string? Notes { get; set; }

    public string VaccineDisplay => $"{VaccineName} (доза {DoseNumber})";
    public string ScheduledDateDisplay => ScheduledDate.ToString("dd.MM.yyyy");
    public string AdministeredDateDisplay => AdministeredDate?.ToString("dd.MM.yyyy") ?? "—";
    public string LotDisplay => !string.IsNullOrEmpty(VaccineLot) ? VaccineLot : "—";
}