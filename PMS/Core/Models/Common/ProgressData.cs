namespace PMS.Core.Models.Common;

public class ProgressData
{
    public int Percentage { get; set; }
    public string? Message { get; set; }
    public string? SubMessage { get; set; }
    public bool IsIndeterminate { get; set; }
}