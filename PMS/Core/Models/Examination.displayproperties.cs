using MongoDB.Bson.Serialization.Attributes;

namespace PMS.Core.Models;


public partial class Examination
{
    [BsonIgnore]
    public string PatientName { get; set; } = string.Empty;

    [BsonIgnore]
    public string DoctorName { get; set; } = string.Empty;

    [BsonIgnore]
    public string DiagnosesDisplay { get; set; } = string.Empty;

    [BsonIgnore]
    public string HasSickLeaveDisplay => SickLeaveFrom.HasValue ? "Так" : "Ні";

    [BsonIgnore]
    public string RecommendationsShort =>
        !string.IsNullOrEmpty(Recommendations) && Recommendations.Length > 50
            ? Recommendations.Substring(0, 50) + "..."
            : Recommendations ?? string.Empty;

    [BsonIgnore]
    public string AnamnesisShort =>
        !string.IsNullOrEmpty(Anamnesis) && Anamnesis.Length > 50
            ? Anamnesis.Substring(0, 50) + "..."
            : Anamnesis ?? string.Empty;
}
