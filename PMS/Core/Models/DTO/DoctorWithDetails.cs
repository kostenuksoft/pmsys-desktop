using System;

namespace PMS.Core.Models.DTO;

public class DoctorWithDetails
{
    public string Id { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }

    public string SpecialtyId { get; set; } = string.Empty;
    public string SpecialtyName { get; set; } = string.Empty;
    public string SpecialtyCode { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;
    public string CategoryDisplay => Category switch
    {
        "highest" => "Вища",
        "first" => "Перша",
        "second" => "Друга",
        "none" => "Без категорії",
        _ => Category
    };

    public int ExperienceYears { get; set; }
    public DateTime HireDate { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? RoomId { get; set; }
    public string? RoomNumber { get; set; }
    public bool IsDistrictDoctor { get; set; }
    public string? DistrictArea { get; set; }
    public bool IsActive { get; set; }

    public string DoctorTypeDisplay => IsDistrictDoctor ? "Дільничний" : "Спеціаліст";
    public string StatusDisplay => IsActive ? "Активний" : "Неактивний";
}