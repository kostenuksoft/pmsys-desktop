using System;
using Avalonia.Controls;

namespace PMS.Core.Models.Common;

public class WindowEventArgs(Window window, string? key = null) : EventArgs
{
    public Window Window { get; } = window;
    public string? Key { get; } = key;
    public Type WindowType { get; } = window.GetType();
}