using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using PMS.ViewModels;
using PMS.Views.Abstractions.Window;
using System;

namespace PMS.Views.Window
{
    
    public partial class SettingsWindow : BaseWindow
    {
        private SettingsViewModel? _viewModel;

        public SettingsWindow()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            _viewModel = DataContext as SettingsViewModel;
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel?.CleanUp();

            base.OnClosed(e);
        }


        private void InfoBar_OnClosed(InfoBar sender, InfoBarClosedEventArgs args)
        {
            if (_viewModel != null) 
                _viewModel.IsInfoBarVisible = false;
        }
    }
}