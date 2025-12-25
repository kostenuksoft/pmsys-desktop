using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using PMS.ViewModels;

namespace PMS.Views.Controls
{
    public partial class CertificateFormView : UserControl
    {
        private TextBox? _patientSearchBox;
        private ComboBox? _certificateTypeCombo;
        private ComboBox? _doctorCombo;
        private ComboBox? _diagnosisCombo;
        private CalendarDatePicker? _issueDatePicker;
        private CalendarDatePicker? _validFromPicker;
        private CalendarDatePicker? _validUntilPicker;

        public CertificateFormView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _patientSearchBox = this.FindControl<TextBox>("PatientSearchBox");
            _certificateTypeCombo = this.FindControl<ComboBox>("CertificateTypeCombo");
            _doctorCombo = this.FindControl<ComboBox>("DoctorCombo");
            _diagnosisCombo = this.FindControl<ComboBox>("DiagnosisCombo");
            _issueDatePicker = this.FindControl<CalendarDatePicker>("IssueDatePicker");
            _validFromPicker = this.FindControl<CalendarDatePicker>("ValidFromPicker");
            _validUntilPicker = this.FindControl<CalendarDatePicker>("ValidUntilPicker");

            if (_patientSearchBox != null)
            {
                _patientSearchBox.KeyDown += PatientSearchBox_KeyDown;
            }

            if (_certificateTypeCombo != null)
            {
                _certificateTypeCombo.SelectionChanged += CertificateTypeCombo_SelectionChanged;
            }

            if (_issueDatePicker != null)
            {
                _issueDatePicker.DisplayDateStart = DateTime.Now.AddMonths(-1);
                _issueDatePicker.DisplayDateEnd = DateTime.Now.AddDays(7);
            }

            if (_validFromPicker != null)
            {
                _validFromPicker.DisplayDateStart = DateTime.Now.AddMonths(-1);
                _validFromPicker.DisplayDateEnd = DateTime.Now.AddMonths(1);
            }

            if (_validUntilPicker != null)
            {
                _validUntilPicker.DisplayDateStart = DateTime.Now;
                _validUntilPicker.DisplayDateEnd = DateTime.Now.AddYears(2);
            }
        }

        private void PatientSearchBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is CertificateFormViewModel viewModel)
            {
                viewModel.SearchPatientCommand.Execute(null);
            }
        }

        private void CertificateTypeCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DataContext is CertificateFormViewModel viewModel &&
                _certificateTypeCombo?.SelectedItem != null)
            {
            }
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is CertificateFormViewModel viewModel)
            {
                viewModel.Initialize();

                if (_doctorCombo != null)
                {
                    _doctorCombo.ItemsSource = viewModel.AvailableDoctors;
                    _doctorCombo.DisplayMemberBinding = new Avalonia.Data.Binding("FullName");
                }

                if (_diagnosisCombo != null)
                {
                    _diagnosisCombo.ItemsSource = viewModel.CommonDiagnoses;
                    _diagnosisCombo.DisplayMemberBinding = new Avalonia.Data.Binding
                    {
                        StringFormat = "{0} - {1}",
                        Path = "IcdCode"
                    };
                }
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            _patientSearchBox?.Focus();
        }
    }
}