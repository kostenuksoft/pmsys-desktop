using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using PMS.Core.Services.Interfaces;

namespace PMS.Core.Extensions
{

    public static class WindowManagerExtensions
    {

        public static TWindow ShowOrActivate<TWindow>(this IWindowService manager, object? dataContext = null)
            where TWindow : Window, new()
        {
            var existing = manager.Get<TWindow>();
            if (existing is { IsVisible: true })
            {
                manager.Activate(existing);
                return existing;
            }

            return manager.Show<TWindow>(dataContext);
        }


        public static TWindow ShowSingleton<TWindow>(this IWindowService manager, object? dataContext = null)
            where TWindow : Window, new()
        {
            return manager.ShowOrActivate<TWindow>(dataContext);
        }


        public static TWindow ReplaceWindow<TWindow>(this IWindowService manager, object? dataContext = null)
            where TWindow : Window, new()
        {
            manager.Close<TWindow>();
            return manager.Show<TWindow>(dataContext);
        }


        public static Window ShowWithKey(this IWindowService manager, string key, Window window, object? dataContext = null)
        {
            if (dataContext != null)
            {
                window.DataContext = dataContext;
            }

            manager.Register(key, window);
            window.Show();

            return window;
        }


        public static TWindow GetOrCreate<TWindow>(this IWindowService manager, Func<TWindow> factory)
            where TWindow : Window
        {
            return manager.Get<TWindow>() ?? factory();
        }


        public static void CloseAll<TWindow>(this IWindowService manager) where TWindow : Window
        {
            var windows = manager.GetAll<TWindow>();
            foreach (var window in windows)
            {
                manager.Close(window);
            }
        }


        public static async Task WaitForClose<TWindow>(this IWindowService manager) where TWindow : Window
        {
            var window = manager.Get<TWindow>();
            if (window == null) return;

            var tcs = new TaskCompletionSource<bool>();

            void OnClosed(object? sender, EventArgs e)
            {
                window.Closed -= OnClosed;
                tcs.SetResult(true);
            }

            window.Closed += OnClosed;
            await tcs.Task;
        }


        public static TWindow SwitchTo<TWindow>(this IWindowService manager, Window currentWindow, object? dataContext = null)
            where TWindow : Window, new()
        {
            manager.Hide(currentWindow);
            return manager.Show<TWindow>(dataContext);
        }


        public static TWindow ShowExclusive<TWindow>(this IWindowService manager, object? dataContext = null)
            where TWindow : Window, new()
        {
            manager.HideAllExcept<TWindow>();
            return manager.Show<TWindow>(dataContext);
        }


        public static void HideAll<TWindow>(this IWindowService manager) where TWindow : Window
        {
            var windows = manager.GetAll<TWindow>();
            foreach (var window in windows)
            {
                manager.Hide(window);
            }
        }
    }
}