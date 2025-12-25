using System;
using System.Collections.Generic;
using Avalonia.Media;
using PMS.Core.Enums.General;

namespace PMS.Core.Models.DTO;


public class ScheduleCard
{
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorFullName { get; set; } = string.Empty;
    public string DoctorSpecialty { get; set; } = string.Empty;
    public DoctorCategory DoctorCategory { get; set; }
    public string? DoctorPhone { get; set; }
    public string? DoctorEmail { get; set; }
    public int DoctorExperienceYears { get; set; }
    public bool IsDistrictDoctor { get; set; }
    public string? RoomId { get; set; }
    public string? RoomNumber { get; set; }
    public int? RoomFloor { get; set; }
    public RoomType? RoomType { get; set; }
    public string ScheduleId { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
    public Shift Shift { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveUntil { get; set; }
    public int TotalSlotsAvailable { get; set; }
    public int BookedSlotsCount { get; set; }
    public int AvailableSlotsCount => TotalSlotsAvailable - BookedSlotsCount;


    public List<AppointmentSlotInfo> AppointmentSlots { get; set; } = new();

    public string DayOfWeekDisplay => DayOfWeek switch
    {
        1 => "Понеділок",
        2 => "Вівторок",
        3 => "Середа",
        4 => "Четвер",
        5 => "П'ятниця",
        6 => "Субота",
        7 => "Неділя",
        _ => "Невідомо"
    };

    public string ShiftDisplay => Shift switch
    {
        Shift.First => "Перша зміна",
        Shift.Second => "Друга зміна",
        Shift.Full => "Дві зміни",
        _ => "Не визначено"
    };

    public string WorkingHoursDisplay => $"{StartTime} - {EndTime}";

    public string DoctorCategoryDisplay => DoctorCategory switch
    {
        DoctorCategory.None => "",
        DoctorCategory.Second => "II категорія",
        DoctorCategory.First => "I категорія",
        DoctorCategory.Highest => "Вища категорія",
        _ => ""
    };

    public string RoomLocationDisplay => RoomFloor.HasValue && !string.IsNullOrEmpty(RoomNumber)
        ? $"Каб. {RoomNumber} ({RoomFloor} поверх)"
        : RoomNumber ?? "Не призначено";

    public string AvailabilitySummary => $"Записано: {BookedSlotsCount}/{TotalSlotsAvailable}";
}