using System;

namespace PMS.ViewModels.Tabs.Interfaces;

public interface INotifyUnsavedChanges
{
    bool HasUnsavedChanges { get; }
    event EventHandler? HasUnsavedChangesChanged;
}