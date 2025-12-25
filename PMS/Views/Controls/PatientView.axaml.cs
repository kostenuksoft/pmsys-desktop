using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using PMS.Core.Models;
using PMS.ViewModels;
using System;
using PMS.Core.Services;

namespace PMS.Views.Controls
{
    public partial class PatientsView : UserControl
    {
        public PatientsView()
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

            if (DataContext is PatientsViewModel viewModel)
            {
                viewModel.Initialize();
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (DataContext is PatientsViewModel viewModel)
            {
            }
        }

        private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var source = e.Source as Control;

            var row = source?.FindAncestorOfType<DataGridRow>();


            if (row?.DataContext is Patient patient && DataContext is PatientsViewModel vm)
            {
                vm.ViewPatientCommand.Execute(patient);
            }

            
        }


    }
}