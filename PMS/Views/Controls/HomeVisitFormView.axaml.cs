using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PMS.Core.Enums.General;
using PMS.ViewModels;

namespace PMS.Views.Controls
{
    public partial class HomeVisitFormView : UserControl
    {
        private ComboBox? _urgencyCombo;
        private ComboBox? _statusCombo;
        private TimePicker? _callTimePicker;

        public HomeVisitFormView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _urgencyCombo = this.FindControl<ComboBox>("UrgencyCombo");
            _statusCombo = this.FindControl<ComboBox>("StatusCombo");
            _callTimePicker = this.FindControl<TimePicker>("CallTimePicker");

            SetupEventHandlers();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is HomeVisitFormViewModel viewModel)
            {
                viewModel.Initialize();

                SyncViewModelToControls(viewModel);
            }
        }

        private void SetupEventHandlers()
        {
            if (_urgencyCombo != null)
            {
                _urgencyCombo.SelectionChanged += (s, e) =>
                {
                    if (DataContext is HomeVisitFormViewModel viewModel)
                    {
                        viewModel.Urgency = _urgencyCombo.SelectedIndex switch
                        {
                            0 => Urgency.Regular,
                            1 => Urgency.Urgent,
                            2 => Urgency.Emergency,
                            _ => Urgency.Regular
                        };
                    }
                };
            }

            if (_statusCombo != null)
            {
                _statusCombo.SelectionChanged += (s, e) =>
                {
                    if (DataContext is HomeVisitFormViewModel viewModel)
                    {
                        viewModel.Status = _statusCombo.SelectedIndex switch
                        {
                            0 => HomeVisitStatus.New,
                            1 => HomeVisitStatus.Assigned,
                            2 => HomeVisitStatus.InProgress,
                            3 => HomeVisitStatus.Completed,
                            4 => HomeVisitStatus.Cancelled,
                            _ => HomeVisitStatus.New
                        };
                    }
                };
            }

            if (_callTimePicker != null)
            {
                _callTimePicker.SelectedTimeChanged += (s, e) =>
                {
                    if (DataContext is HomeVisitFormViewModel viewModel && e.NewTime.HasValue)
                    {
                    }
                };
            }
        }

        private void SyncViewModelToControls(HomeVisitFormViewModel viewModel)
        {
            if (_urgencyCombo != null)
            {
                _urgencyCombo.SelectedIndex = viewModel.Urgency switch
                {
                    Urgency.Regular => 0,
                    Urgency.Urgent => 1,
                    Urgency.Emergency => 2,
                    _ => 0
                };
            }

            if (_statusCombo != null)
            {
                _statusCombo.SelectedIndex = viewModel.Status switch
                {
                    HomeVisitStatus.New => 0,
                    HomeVisitStatus.Assigned => 1,
                    HomeVisitStatus.InProgress => 2,
                    HomeVisitStatus.Completed => 3,
                    HomeVisitStatus.Cancelled => 4,
                    _ => 0
                };
            }

            if (_callTimePicker != null)
            {
            }
        }
    }
}