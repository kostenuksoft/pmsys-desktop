namespace PMS.Core.Models.Common;

public class LoggingSettingsMemento
{
    public string LogFilePath { get; init; }
    public string LogRollingInterval { get; init; }
    public int RetainedFileCountLimit { get; init; }

    public LoggingSettingsMemento(
        string logFilePath,
        string logRollingInterval,
        int retainedFileCountLimit)
    {
        LogFilePath = logFilePath;
        LogRollingInterval = logRollingInterval;
        RetainedFileCountLimit = retainedFileCountLimit;
    }
}