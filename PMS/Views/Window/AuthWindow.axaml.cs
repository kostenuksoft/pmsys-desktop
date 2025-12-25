using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using PMS.Core.Services;
using PMS.ViewModels;
using PMS.Views.Abstractions.Window;
using System;
using System.Threading.Tasks;

namespace PMS.Views.Window
{
    public partial class AuthWindow : BaseWindow
    {
        private AuthViewModel? _viewModel;

        public AuthWindow()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected void OnDataContextChanged(object? sender, EventArgs? e)
        {
            Console.WriteLine($"DataContext changed to: {DataContext?.GetType().Name}");
            _viewModel = DataContext as AuthViewModel;
        }

        private void OnBackRequested(object? sender, NavigationViewBackRequestedEventArgs e)
        {
            Hide();
            _viewModel?.ReturnBackCommand.Execute(null);
        }

        private void TopLevel_OnClosed(object? sender, EventArgs e)
        {
            Hide();
        }
    }
}