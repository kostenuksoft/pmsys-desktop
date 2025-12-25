namespace PMS.Core.Models.Common;

public class ApplicationSettings
{
    public string? DefaultLanguage { get; set; } = "uk-UA";
    public string? DateFormat { get; set; } = "dd.MM.yyyy";
    public string? TimeFormat { get; set; } = "HH:mm";
    public int PageSize { get; set; } = 50;
    public int MaxExportRows { get; set; } = 10000;
    public bool AskWhenQuitting { get; set; } = true;
}