using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using PMS.ViewModels.Dialogs;
using PMS.Views.Abstractions.Window;

namespace PMS.Views.Window
{
    public partial class DoctorDetailsWindow : BaseWindow
    {
        public DoctorDetailsWindow()
        {
            InitializeComponent();
        }

        public DoctorDetailsWindow(DoctorDetailsWindowViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Button_OnClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}