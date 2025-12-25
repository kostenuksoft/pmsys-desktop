using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using PMS.ViewModels;
using System;
using System.Linq;

namespace PMS.Views.Controls
{
    public partial class AppointmentFormView : UserControl
    {
        private Calendar? _calendar;

        public AppointmentFormView()
        {
            InitializeComponent();

            _calendar = this.FindControl<Calendar>("AppointmentCalendar");

            if (_calendar != null)
            {
                DataContextChanged += OnDataContextChanged;
            }
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (sender is Control control && control.DataContext is AppointmentFormViewModel oldViewModel)
            {
                oldViewModel.BlackoutDatesChanged -= OnBlackoutDatesChanged;
            }

            if (DataContext is AppointmentFormViewModel viewModel)
            {
                viewModel.BlackoutDatesChanged += OnBlackoutDatesChanged;

                
                viewModel.Initialize();

                UpdateBlackoutDates(viewModel.BlackoutDatesInfo);
            }
        }

        private void OnBlackoutDatesChanged(object? sender,
            System.Collections.ObjectModel.ObservableCollection<AppointmentFormViewModel.BlackoutDateInfo> blackoutDates)
        {
            UpdateBlackoutDates(blackoutDates);
        }

        private void UpdateBlackoutDates(
            System.Collections.ObjectModel.ObservableCollection<AppointmentFormViewModel.BlackoutDateInfo> blackoutDates)
        {
            if (_calendar == null) return;

            try
            {
                _calendar.BlackoutDates.Clear();

                foreach (var blackoutInfo in blackoutDates)
                {
                    _calendar.BlackoutDates.Add(new CalendarDateRange(blackoutInfo.Date));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating blackout dates: {ex.Message}");
            }
        }

        protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
        {
            if (DataContext is AppointmentFormViewModel viewModel)
            {
                viewModel.BlackoutDatesChanged -= OnBlackoutDatesChanged;
            }

            DataContextChanged -= OnDataContextChanged;

            base.OnDetachedFromVisualTree(e);
        }
    }
}