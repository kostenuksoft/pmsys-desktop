using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using PMS.Core.Models.Common;

namespace PMS.Core.Services.Interfaces;

public class WindowService : IWindowService, IDisposable
{
    private readonly Dictionary<Type, Window> _windowsByType = new();
    private readonly Dictionary<string, Window> _windowsByKey = new();
    private readonly HashSet<Window> _allWindows = new();
    private readonly object _lock = new();
    private Window? _mainWindow;

    public event EventHandler<WindowEventArgs>? WindowOpened;
    public event EventHandler<WindowEventArgs>? WindowClosed;
    public event EventHandler<WindowEventArgs>? WindowActivated;
    public event EventHandler<WindowEventArgs>? WindowHidden;
    public event EventHandler<WindowEventArgs>? WindowShown;

    public Window? MainWindow
    {
        get
        {
            lock (_lock)
            {
                return _mainWindow;
            }
        }
    }

    public void SetMainWindow(Window window)
    {
        if (window == null)
            throw new ArgumentNullException(nameof(window));

        lock (_lock)
        {
            if (_mainWindow != null)
            {
                UntrackWindow(_mainWindow);
            }

            _mainWindow = window;
            TrackWindow(window, isMain: true);

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = window;
            }
        }
    }


    #region Window Operations

    public TWindow Show<TWindow>(object? dataContext = null) where TWindow : Window, new()
    {
        return ShowInternal<TWindow>(modal: false, owner: null, dataContext);
    }

    public TWindow ShowDialog<TWindow>(Window? owner = null, object? dataContext = null) where TWindow : Window, new()
    {
        return ShowInternal<TWindow>(modal: true, owner, dataContext);
    }

    public async Task<TResult?> ShowDialog<TWindow, TResult>(Window? owner = null, object? dataContext = null)
        where TWindow : Window, new()
    {
        var window = ShowInternal<TWindow>(modal: true, owner, dataContext);

        var tcs = new TaskCompletionSource<TResult?>();

        void OnClosed(object? s, EventArgs e)
        {
            window.Closed -= OnClosed;

            if (window.DataContext is IDialogResult<TResult> dialogResult)
            {
                tcs.SetResult(dialogResult.Result);
            }
            else
            {
                tcs.SetResult(default);
            }
        }

        window.Closed += OnClosed;

        return await tcs.Task;
    }

    private TWindow ShowInternal<TWindow>(bool modal, Window? owner, object? dataContext)
        where TWindow : Window, new()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            return Dispatcher.UIThread.InvokeAsync(() =>
                ShowInternal<TWindow>(modal, owner, dataContext)).GetAwaiter().GetResult();
        }

        lock (_lock)
        {
            if (_windowsByType.TryGetValue(typeof(TWindow), out var existing))
            {
                if (existing.IsVisible)
                {
                    existing.Activate();
                    WindowActivated?.Invoke(this, new WindowEventArgs(existing));
                    return (TWindow)existing;
                }
                else
                {
                    existing.Show();
                    existing.Activate();
                    WindowShown?.Invoke(this, new WindowEventArgs(existing));
                    return (TWindow)existing;
                }
            }

            var window = new TWindow();

            if (dataContext != null)
            {
                window.DataContext = dataContext;
            }

            owner ??= _mainWindow;
            if (owner != null && owner != window)
            {
                if (modal)
                {
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
            }

            TrackWindow(window);

            if (modal && owner != null)
            {
                window.ShowDialog(owner);
            }
            else
            {
                window.Show();
                window.Activate();
            }

            WindowOpened?.Invoke(this, new WindowEventArgs(window));

            return window;
        }
    }

    #endregion

    #region Window Retrieval

    public TWindow? Get<TWindow>() where TWindow : Window
    {
        lock (_lock)
        {
            return _windowsByType.TryGetValue(typeof(TWindow), out var window)
                ? window as TWindow
                : null;
        }
    }

    public Window? Get(Type windowType)
    {
        if (!typeof(Window).IsAssignableFrom(windowType))
            throw new ArgumentException("Type must be a Window", nameof(windowType));

        lock (_lock)
        {
            return _windowsByType.GetValueOrDefault(windowType);
        }
    }

    public Window? Get(string windowKey)
    {
        if (string.IsNullOrEmpty(windowKey))
            throw new ArgumentException("Window key cannot be empty", nameof(windowKey));

        lock (_lock)
        {
            return _windowsByKey.GetValueOrDefault(windowKey);
        }
    }

    public IEnumerable<Window> GetAll()
    {
        lock (_lock)
        {
            return _allWindows.ToList();
        }
    }

    public IEnumerable<TWindow> GetAll<TWindow>() where TWindow : Window
    {
        lock (_lock)
        {
            return _allWindows.OfType<TWindow>().ToList();
        }
    }

    #endregion

    #region Window State

    public bool IsOpen<TWindow>() where TWindow : Window
    {
        lock (_lock)
        {
            return _windowsByType.TryGetValue(typeof(TWindow), out var window) && window.IsVisible;
        }
    }

    public bool IsOpen(Type windowType)
    {
        lock (_lock)
        {
            return _windowsByType.TryGetValue(windowType, out var window) && window.IsVisible;
        }
    }

    public void Close<TWindow>() where TWindow : Window
    {
        var window = Get<TWindow>();
        if (window != null)
        {
            Close(window);
        }
    }

    public void Close(Window window)
    {
        if (window == null)
            return;

        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => Close(window));
            return;
        }

        window.Close();
    }

    public void CloseAll(bool exceptMain = true)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => CloseAll(exceptMain));
            return;
        }

        List<Window> windowsToClose;

        lock (_lock)
        {
            windowsToClose = exceptMain
                ? _allWindows.Where(w => w != _mainWindow).ToList()
                : _allWindows.ToList();
        }

        foreach (var window in windowsToClose)
        {
            window.Close();
        }
    }

    #endregion

    #region Window Visibility

    public void Hide<TWindow>() where TWindow : Window
    {
        var window = Get<TWindow>();
        if (window != null)
        {
            Hide(window);
        }
    }

    public void Hide(Window window)
    {
        if (window == null)
            return;

        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => Hide(window));
            return;
        }

        if (window.IsVisible)
        {
            window.Hide();
            WindowHidden?.Invoke(this, new WindowEventArgs(window));
        }
    }

    public void HideAll(bool exceptMain = true)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => HideAll(exceptMain));
            return;
        }

        List<Window> windowsToHide;

        lock (_lock)
        {
            windowsToHide = exceptMain
                ? _allWindows.Where(w => w != _mainWindow && w.IsVisible).ToList()
                : _allWindows.Where(w => w.IsVisible).ToList();
        }

        foreach (var window in windowsToHide)
        {
            window.Hide();
            WindowHidden?.Invoke(this, new WindowEventArgs(window));
        }
    }

    public void HideAllExcept<TWindow>() where TWindow : Window
    {
        HideAllExcept(typeof(TWindow));
    }

    public void HideAllExcept(Type windowType)
    {
        if (windowType == null)
            throw new ArgumentNullException(nameof(windowType));

        if (!typeof(Window).IsAssignableFrom(windowType))
            throw new ArgumentException("Type must be a Window", nameof(windowType));

        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => HideAllExcept(windowType));
            return;
        }

        List<Window> windowsToHide;

        lock (_lock)
        {
            windowsToHide = _allWindows
                .Where(w => w.GetType() != windowType && w.IsVisible)
                .ToList();
        }

        foreach (var window in windowsToHide)
        {
            window.Hide();
            WindowHidden?.Invoke(this, new WindowEventArgs(window));
        }
    }

    public void HideAllExcept(Window window)
    {
        if (window == null)
            throw new ArgumentNullException(nameof(window));

        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => HideAllExcept(window));
            return;
        }

        List<Window> windowsToHide;

        lock (_lock)
        {
            windowsToHide = _allWindows
                .Where(w => w != window && w.IsVisible)
                .ToList();
        }

        foreach (var w in windowsToHide)
        {
            w.Hide();
            WindowHidden?.Invoke(this, new WindowEventArgs(w));
        }
    }

    #endregion

    #region Window Activation

    public void Activate<TWindow>() where TWindow : Window
    {
        var window = Get<TWindow>();
        if (window != null)
        {
            Activate(window);
        }
    }

    public void Activate(Window window)
    {
        if (window == null)
            return;

        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => Activate(window));
            return;
        }

        if (!window.IsVisible)
        {
            window.Show();
            WindowShown?.Invoke(this, new WindowEventArgs(window));
        }

        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
        window.Topmost = true;
        window.Topmost = false;
        window.Focus();

        WindowActivated?.Invoke(this, new WindowEventArgs(window));
    }

    #endregion

    #region Window Registration

    public void Register(string key, Window window)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));
        if (window == null)
            throw new ArgumentNullException(nameof(window));

        lock (_lock)
        {
            if (!_windowsByKey.TryAdd(key, window))
            {
                throw new InvalidOperationException($"Window with key '{key}' is already registered");
            }

            TrackWindow(window);
        }
    }

    public void Unregister(string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        lock (_lock)
        {
            _windowsByKey.Remove(key, out _);
        }
    }

    #endregion

    #region Private Methods

    private void TrackWindow(Window window, bool isMain = false)
    {
        lock (_lock)
        {
            _allWindows.Add(window);

            if (!isMain)
            {
                _windowsByType[window.GetType()] = window;
            }

            window.Closed += OnWindowClosed;
            window.Activated += OnWindowActivated;
        }
    }

    private void UntrackWindow(Window window)
    {
        lock (_lock)
        {
            _allWindows.Remove(window);
            _windowsByType.Remove(window.GetType());

            var keysToRemove = _windowsByKey
                .Where(kvp => kvp.Value == window)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _windowsByKey.Remove(key);
            }

            window.Closed -= OnWindowClosed;
            window.Activated -= OnWindowActivated;
        }
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        if (sender is Window window)
        {
            UntrackWindow(window);
            WindowClosed?.Invoke(this, new WindowEventArgs(window));
        }
    }

    private void OnWindowActivated(object? sender, EventArgs e)
    {
        if (sender is Window window)
        {
            WindowActivated?.Invoke(this, new WindowEventArgs(window));
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        CloseAll(exceptMain: false);

        lock (_lock)
        {
            _windowsByType.Clear();
            _windowsByKey.Clear();
            _allWindows.Clear();
            _mainWindow = null;
        }
    }

    #endregion
}