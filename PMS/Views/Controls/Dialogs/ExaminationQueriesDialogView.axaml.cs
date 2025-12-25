using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using PMS.ViewModels;
using System;
using System.Collections.Generic;
using PMS.ViewModels.Dialogs;

namespace PMS.Views.Controls.Dialogs
{
    public partial class ExaminationQueriesDialogView : UserControl
    {
        public ExaminationQueriesDialogView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnExaminationSelectedDatesChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is Calendar calendar)
            {
                if (DataContext is not ExaminationQueriesDialogViewModel viewModel) return;

                viewModel.UpdateExaminationSelectedDates(calendar.SelectedDates);
            }
        }

        private void OnDiagnosisSelectedDatesChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is Calendar calendar)
            {
                if (DataContext is not ExaminationQueriesDialogViewModel viewModel) return;

                viewModel.UpdateDiagnosisSelectedDates(calendar.SelectedDates);
            }
        }
    }
}