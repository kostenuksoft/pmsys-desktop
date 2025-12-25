namespace PMS.Core.Models.Common;

public class SettingsMementoCaretaker
{
    public DatabaseSettingsMemento? DatabaseMemento { get; set; }
    public LoggingSettingsMemento? LoggingMemento { get; set; }
    public ApplicationSettingsMemento? ApplicationMemento { get; set; }

    public void SaveDatabaseState(DatabaseSettingsMemento memento)
    {
        DatabaseMemento = memento;
    }

    public void SaveLoggingState(LoggingSettingsMemento memento)
    {
        LoggingMemento = memento;
    }

    public void SaveApplicationState(ApplicationSettingsMemento memento)
    {
        ApplicationMemento = memento;
    }

    public void ClearDatabaseMemento()
    {
        DatabaseMemento = null;
    }

    public void ClearLoggingMemento()
    {
        LoggingMemento = null;
    }

    public void ClearApplicationMemento()
    {
        ApplicationMemento = null;
    }
}