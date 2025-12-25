using System;

namespace PMS.Core.Enums.General;

public class TabTypeOption
{
    public Type ViewModelType { get; set; }
    public string DisplayName { get; set; }
    public string Icon { get; set; }

    public TabTypeOption(Type viewModelType, string displayName, string icon)
    {
        ViewModelType = viewModelType;
        DisplayName = displayName;
        Icon = icon;
    }
}