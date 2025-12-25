using DocumentFormat.OpenXml.Wordprocessing;
using MongoDB.Bson.Serialization.Attributes;
using PMS.Core.Enums.General;
using System.Linq;
using System.Runtime.Serialization;

namespace PMS.Core.Models;

public partial class Room
{
    [BsonIgnore]
    public string RoomTypeDisplay => RoomType switch
    {
        RoomType.DoctorOffice => "Кабінет лікаря",
        RoomType.PhysicalTherapy => "Фізіотерапевтичний кабінет",
        RoomType.Ultrasound => "Кабінет УЗД",
        RoomType.Laboratory => "Лабораторія",
        RoomType.Reception => "Реєстратура",
        RoomType.Administrative => "Адміністративний",
        RoomType.Consultation => "Консультативний кабінет",
        RoomType.Procedure => "Процедурний",
        RoomType.Diagnostic => "Діагностичний кабінет",
        RoomType.Vaccination => "Кабінет вакцинації",
        _ => RoomType.ToString()
    };

    [BsonIgnore]
    public string StatusDisplay => IsActive ? "Активний" : "Неактивний";

    [BsonIgnore]
    public string EquipmentDisplay => Equipment != null && Equipment.Any()
        ? string.Join(", ", Equipment.Take(3)) + (Equipment.Count > 3 ? $" та ще {Equipment.Count - 3}" : "")
        : "—";

    [BsonIgnore]
    public string FloorDisplay => Floor switch
    {
        -1 => "Підвал",
        0 => "Партер",
        1 => "1 поверх",
        2 => "2 поверх",
        3 => "3 поверх",
        4 => "4 поверх",
        5 => "5 поверх",
        6 => "6 поверх",
        _ => $"{Floor} поверх"
    };

    [BsonIgnore]
    public string CapacityDisplay => Capacity switch
    {
        1 => "1 місце",
        2 or 3 or 4 => $"{Capacity} місця",
        _ => $"{Capacity} місць"
    };
}