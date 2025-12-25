using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PMS.Core.Enums.General;
using PMS.ViewModels;
using System;

namespace PMS.Views.Controls
{
    public partial class UserManagementView : UserControl
    {
        public UserManagementView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is UserManagementViewModel viewModel)
            {
                viewModel.Initialize();
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var searchBox = this.FindControl<TextBox>("SearchBox");
            searchBox?.Focus();
        }

        private void OnRoleFilterChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && 
                comboBox.SelectedItem is ComboBoxItem item &&
                DataContext is UserManagementViewModel viewModel)
            {
                var tag = item.Tag as string;
                
                if (string.IsNullOrEmpty(tag))
                {
                    viewModel.RoleFilter = null;
                }
                else if (Enum.TryParse<UserRole>(tag, out var role))
                {
                    viewModel.RoleFilter = role;
                }
            }
        }

        private void OnStatusFilterChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && 
                comboBox.SelectedItem is ComboBoxItem item &&
                DataContext is UserManagementViewModel viewModel)
            {
                var tag = item.Tag as string;
                
                if (string.IsNullOrEmpty(tag))
                {
                    viewModel.StatusFilter = null;
                }
                else if (bool.TryParse(tag, out var status))
                {
                    viewModel.StatusFilter = status;
                }
            }
        }
    }
}