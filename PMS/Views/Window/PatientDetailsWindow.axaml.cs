using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Windowing;
using PMS.ViewModels.Dialogs;
using PMS.Views.Abstractions.Window;

namespace PMS.Views.Window
{
    public partial class PatientDetailsWindow : BaseWindow
    {
        public PatientDetailsWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            SystemDecorations = SystemDecorations.BorderOnly;
            CanResize = true;
        }

        public PatientDetailsWindow(PatientDetailsWindowViewModel viewModel) : this()
        {
            DataContext = viewModel;

            Loaded += async (s, e) => await viewModel.InitializeAsync();
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}