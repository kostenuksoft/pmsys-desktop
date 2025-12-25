namespace PMS.Core.Models.DTO;

public class DiagnosisInfo
{
    public string Id { get; set; } = string.Empty;
    public string IcdCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }

    public string DisplayText => $"{IcdCode} - {Name}";
}