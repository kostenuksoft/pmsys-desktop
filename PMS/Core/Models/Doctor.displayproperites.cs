using Avalonia.Media;
using MongoDB.Bson.Serialization.Attributes;
using PMS.Core.Enums.General;
using System;

namespace PMS.Core.Models;

public partial class Doctor
{
    private static readonly Random Random = new();
    private Color? _assignedColor;

    [BsonIgnore]
    public Color AssignedColor
    {
        get
        {
            if (_assignedColor == null)
            {
                var hue = Random.Next(0, 360);
                var saturation = 0.6 + Random.NextDouble() * 0.3;
                var value = 0.7 + Random.NextDouble() * 0.2;

                _assignedColor = HsvToRgb(hue, saturation, value);
            }
            return _assignedColor.Value;
        }
        set => _assignedColor = value;
    }

    [BsonIgnore]
    public string CategoryDisplay => Category switch
    {
        DoctorCategory.None => "Без категорії",
        DoctorCategory.Second => "ІІ категорія",
        DoctorCategory.First => "І категорія",
        DoctorCategory.Highest => "Вища категорія",
        _ => "Не визначено"
    };

    [BsonIgnore]
    public string StatusDisplay => IsActive ? "Активний" : "Неактивний";

    [BsonIgnore]
    public string ExperienceDisplay => $"{ExperienceYears} років";

    [BsonIgnore]
    public string DistrictDisplay => IsDistrictDoctor && !string.IsNullOrEmpty(DistrictArea)
        ? $"Дільничний ({DistrictArea})"
        : "Не дільничний";

    [BsonIgnore]
    public string CertificationsDisplay => Certifications?.Count > 0
        ? $"{Certifications.Count} сертифікатів"
        : "Немає сертифікатів";

    private static Color HsvToRgb(double h, double s, double v)
    {
        var c = v * s;
        var x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
        var m = v - c;

        double r = 0, g = 0, b = 0;

        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return Color.FromRgb(
            (byte)Math.Round((r + m) * 255),
            (byte)Math.Round((g + m) * 255),
            (byte)Math.Round((b + m) * 255)
        );
    }
}