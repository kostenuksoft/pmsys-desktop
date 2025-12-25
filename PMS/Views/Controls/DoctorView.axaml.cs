using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using PMS.Core.Models;
using PMS.ViewModels;

namespace PMS.Views.Controls
{
    public partial class DoctorsView : UserControl
    {
        private TextBox? _searchBox;
        private DataGrid? _doctorsGrid;

        public DoctorsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _searchBox = this.FindControl<TextBox>("SearchBox");
            _doctorsGrid = this.FindControl<DataGrid>("DoctorsGrid");

            if (_searchBox != null)
            {
                _searchBox.KeyDown += SearchBox_KeyDown;
            }

            if (_doctorsGrid != null)
            {
                _doctorsGrid.DoubleTapped += DoctorsGrid_DoubleTapped;
                _doctorsGrid.LoadingRow += DoctorsGrid_LoadingRow;
            }

            WireUpDataGridButtons();
        }

        private void OnDataGridLostFocus(object? sender, RoutedEventArgs e)
        {
            if (sender is DataGrid grid)
            {
                grid.SelectedItem = null;
            }
        }

        private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is DoctorsViewModel viewModel)
            {
                viewModel.RefreshCommand.Execute(null);
            }
        }

        private void DoctorsGrid_DoubleTapped(object? sender, TappedEventArgs e)
        {
            if (DataContext is DoctorsViewModel viewModel &&
                _doctorsGrid?.SelectedItem is Doctor doctor)
            {
                if (viewModel.EditDoctorCommand.CanExecute(doctor))
                {
                    viewModel.EditDoctorCommand.Execute(doctor);
                    
                }
            }
        }

        private void DoctorsGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void WireUpDataGridButtons()
        {
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is DoctorsViewModel viewModel)
            {
                viewModel.Initialize();
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            _searchBox?.Focus();
        }
    }
}