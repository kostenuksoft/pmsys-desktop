using Avalonia.Controls;
using PMS.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace PMS.Core.Services.Interfaces
{
    public interface IWindowService
    {
        
        Window? MainWindow { get; }
        void SetMainWindow(Window window);


        TWindow Show<TWindow>(object? dataContext = null) where TWindow : Window, new();
        TWindow ShowDialog<TWindow>(Window? owner = null, object? dataContext = null) where TWindow : Window, new();
        Task<TResult?> ShowDialog<TWindow, TResult>(Window? owner = null, object? dataContext = null) where TWindow : Window, new();

        TWindow? Get<TWindow>() where TWindow : Window;
        Window? Get(Type windowType);
        Window? Get(string windowKey);
        IEnumerable<Window> GetAll();
        IEnumerable<TWindow> GetAll<TWindow>() where TWindow : Window;

        bool IsOpen<TWindow>() where TWindow : Window;
        bool IsOpen(Type windowType);
        void Close<TWindow>() where TWindow : Window;
        void Close(Window window);
        void CloseAll(bool exceptMain = true);

        void Hide<TWindow>() where TWindow : Window;
        void Hide(Window window);
        void HideAll(bool exceptMain = true);
        void HideAllExcept<TWindow>() where TWindow : Window;
        void HideAllExcept(Type windowType);
        void HideAllExcept(Window window);

        void Activate<TWindow>() where TWindow : Window;
        void Activate(Window window);

        void Register(string key, Window window);
        void Unregister(string key);

        event EventHandler<WindowEventArgs>? WindowOpened;
        event EventHandler<WindowEventArgs>? WindowClosed;
        event EventHandler<WindowEventArgs>? WindowActivated;
        event EventHandler<WindowEventArgs>? WindowHidden;
        event EventHandler<WindowEventArgs>? WindowShown;
    }
}

