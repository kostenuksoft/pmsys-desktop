namespace PMS.Core.Models.Common;

public class LoggingSettings
{
    public string? FilePath { get; set; } = "Logs/PMS.log";
    public string? RollingInterval { get; set; } = "Day";
    public int RetainedFileCountLimit { get; set; } = 30;
}