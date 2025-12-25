using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace PMS.Core.Extensions;

public static class AvaloniaExtensions
{
    public static TopLevel? GetNotificationTargetWindow(this Application app)
    {
        if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.Windows?
                .Where(w => w.IsVisible)
                .OrderByDescending(w => w.IsActive)
                .ThenByDescending(w => w.IsFocused)
                .ThenBy(w => w == desktop.MainWindow ? 0 : 1)
                .FirstOrDefault();
        }

        return null;
    }
}