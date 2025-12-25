using System;

namespace PMS.Core.Models.Common;

public class TabEventArgs : EventArgs
{
    public TabItemModel Tab { get; }
    public TabEventArgs(TabItemModel tab) => Tab = tab;
}