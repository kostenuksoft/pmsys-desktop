namespace PMS.ViewModels.Tabs.Interfaces;

public interface ITabViewModel
{
    string TabHeader { get; }
    string? TabIconSource { get; }
    bool CanClose { get; }
}