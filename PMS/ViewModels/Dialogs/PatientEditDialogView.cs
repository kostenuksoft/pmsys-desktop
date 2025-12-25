using PMS.Core.Enums.General;
using PMS.Core.Models;
using ReactiveUI.Validation.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using PMS.Core.Models.Common;

namespace PMS.ViewModels.Dialogs
{
    public class PatientEditDialogViewModel : BaseDialogViewModel<Patient>
    {
        private readonly Patient? _originalPatient;
        private readonly bool _isEditMode;

        private string _medicalRecordNumber = string.Empty;
        private string _fullName = string.Empty;
        private DateTime _birthDate = DateTime.Now.AddYears(-30);
        private int _selectedGenderIndex;
        private string _phone = string.Empty;
        private string _alternativePhone = string.Empty;
        private string _email = string.Empty;
        private DateTime _registrationDate = DateTime.Now;
        private bool _isActive = true;

        private string _city = string.Empty;
        private string _district = string.Empty;
        private string _street = string.Empty;
        private string _building = string.Empty;
        private string _apartment = string.Empty;
        private string _entrance = string.Empty;
        private int? _floor;
        private string _postalCode = string.Empty;

        private string? _assignedDoctorId;
        private int _selectedHealthStatusIndex = 0;
        private int _selectedBloodTypeIndex = 0;
        private string _allergiesText = string.Empty;

        private ObservableCollection<Doctor> _availableDoctors = [];

        public PatientEditDialogViewModel(Patient? patient, IEnumerable<Doctor> doctors)
        {
            _originalPatient = patient;
            _isEditMode = patient != null;

            AvailableDoctors = new ObservableCollection<Doctor>(doctors);

            if (_isEditMode && patient != null)
            {
                Title = "Редагування пацієнта";
                LoadPatientData(patient);
            }
            else
            {
                Title = "Новий пацієнт";
                GenerateNewMedicalRecordNumber();
            }

            SetupValidation();
        }

        private void GenerateNewMedicalRecordNumber()
        {
            var random = new Random();
            var number = random.Next(1, 9999999);
            MedicalRecordNumber = $"P{number:D7}";
        }

        private void SetupValidation()
        {
            this.ValidationRule(
                vm => vm.MedicalRecordNumber,
                mrn => !string.IsNullOrWhiteSpace(mrn),
                "Номер медичної картки обов'язковий");

            this.ValidationRule(
                vm => vm.MedicalRecordNumber,
                mrn => Regex.IsMatch(mrn ?? "", @"^P[0-9]{7}$"),
                "Номер повинен бути у форматі P0000000");

            this.ValidationRule(
                vm => vm.FullName,
                name => !string.IsNullOrWhiteSpace(name),
                "Прізвище та ім'я обов'язкові");

            this.ValidationRule(
                vm => vm.Phone,
                phone => !string.IsNullOrWhiteSpace(phone),
                "Телефон обов'язковий");

            this.ValidationRule(
                vm => vm.Street,
                street => !string.IsNullOrWhiteSpace(street),
                "Вулиця обов'язкова");

            this.ValidationRule(
                vm => vm.Building,
                building => !string.IsNullOrWhiteSpace(building),
                "Номер будинку обов'язковий");

            this.ValidationRule(
                vm => vm.City,
                city => !string.IsNullOrWhiteSpace(city),
                "Місто обов'язкове");

            this.ValidationRule(
                vm => vm.PostalCode,
                postal => !string.IsNullOrWhiteSpace(postal),
                "Поштовий індекс обов'язковий");

            this.ValidationRule(
                vm => vm.District,
                district => !string.IsNullOrWhiteSpace(district),
                "Район обов'язковий");

            this.IsValid()
                .Subscribe(isValid => CanExecutePrimary = isValid);
        }

        private void LoadPatientData(Patient patient)
        {
            MedicalRecordNumber = patient.MedicalRecordNumber;
            FullName = patient.FullName;
            BirthDate = patient.BirthDate;
            SelectedGenderIndex = (int)patient.Gender;
            Phone = patient.Phone;
            AlternativePhone = patient.AlternativePhone ?? string.Empty;
            Email = patient.Email ?? string.Empty;
            RegistrationDate = patient.RegistrationDate;
            IsActive = patient.IsActive;

            if (patient.Address != null)
            {
                City = patient.Address.City;
                District = patient.Address.District;
                Street = patient.Address.Street;
                Building = patient.Address.Building;
                Apartment = patient.Address.Apartment ?? string.Empty;
                Entrance = patient.Address.Entrance ?? string.Empty;
                Floor = patient.Address.Floor;
                PostalCode = patient.Address.PostalCode;
            }

            AssignedDoctorId = patient.AssignedDoctorId;
            SelectedHealthStatusIndex = (int)patient.HealthStatus;

            if (patient.BloodType.HasValue)
            {
                SelectedBloodTypeIndex = (int)patient.BloodType.Value + 1;
            }
            else
            {
                SelectedBloodTypeIndex = 0;
            }

            if (patient.Allergies != null && patient.Allergies.Count > 0)
            {
                AllergiesText = string.Join(Environment.NewLine, patient.Allergies);
            }
        }

        protected override Patient? GetResult()
        {
            if (!IsConfirmed)
                return null;

            var allergies = AllergiesText
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .ToList();

            BloodType? bloodType = null;
            if (SelectedBloodTypeIndex > 0)
            {
                bloodType = (BloodType)(SelectedBloodTypeIndex - 1);
            }

            var patient = _isEditMode && _originalPatient != null
                ? _originalPatient
                : new Patient();

            patient.MedicalRecordNumber = MedicalRecordNumber;
            patient.FullName = FullName;
            patient.BirthDate = BirthDate;
            patient.Gender = (Gender)SelectedGenderIndex;
            patient.Phone = Phone;
            patient.AlternativePhone = string.IsNullOrWhiteSpace(AlternativePhone) ? null : AlternativePhone;
            patient.Email = string.IsNullOrWhiteSpace(Email) ? null : Email;
            patient.RegistrationDate = RegistrationDate;
            patient.IsActive = IsActive;

            patient.Address = new Address
            {
                City = City,
                District = District,
                Street = Street,
                Building = Building,
                Apartment = string.IsNullOrWhiteSpace(Apartment) ? null : Apartment,
                Entrance = string.IsNullOrWhiteSpace(Entrance) ? null : Entrance,
                Floor = Floor,
                PostalCode = PostalCode
            };

            patient.AssignedDoctorId = AssignedDoctorId;
            patient.HealthStatus = (HealthStatus)SelectedHealthStatusIndex;
            patient.BloodType = bloodType;
            patient.Allergies = allergies;

            return patient;
        }

        #region Properties

        public string MedicalRecordNumber
        {
            get => _medicalRecordNumber;
            set => SetAndRiseProperty(ref _medicalRecordNumber, value);
        }

        public string FullName
        {
            get => _fullName;
            set => SetAndRiseProperty(ref _fullName, value);
        }

        public DateTime BirthDate
        {
            get => _birthDate;
            set => SetAndRiseProperty(ref _birthDate, value);
        }

        public int SelectedGenderIndex
        {
            get => _selectedGenderIndex;
            set => SetAndRiseProperty(ref _selectedGenderIndex, value);
        }

        public string Phone
        {
            get => _phone;
            set => SetAndRiseProperty(ref _phone, value);
        }

        public string AlternativePhone
        {
            get => _alternativePhone;
            set => SetAndRiseProperty(ref _alternativePhone, value);
        }

        public string Email
        {
            get => _email;
            set => SetAndRiseProperty(ref _email, value);
        }

        public DateTime RegistrationDate
        {
            get => _registrationDate;
            set => SetAndRiseProperty(ref _registrationDate, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetAndRiseProperty(ref _isActive, value);
        }

        public string City
        {
            get => _city;
            set => SetAndRiseProperty(ref _city, value);
        }

        public string District
        {
            get => _district;
            set => SetAndRiseProperty(ref _district, value);
        }

        public string Street
        {
            get => _street;
            set => SetAndRiseProperty(ref _street, value);
        }

        public string Building
        {
            get => _building;
            set => SetAndRiseProperty(ref _building, value);
        }

        public string Apartment
        {
            get => _apartment;
            set => SetAndRiseProperty(ref _apartment, value);
        }

        public string Entrance
        {
            get => _entrance;
            set => SetAndRiseProperty(ref _entrance, value);
        }

        public int? Floor
        {
            get => _floor;
            set => SetAndRiseProperty(ref _floor, value);
        }

        public string PostalCode
        {
            get => _postalCode;
            set => SetAndRiseProperty(ref _postalCode, value);
        }

        public string? AssignedDoctorId
        {
            get => _assignedDoctorId;
            set => SetAndRiseProperty(ref _assignedDoctorId, value);
        }

        public int SelectedHealthStatusIndex
        {
            get => _selectedHealthStatusIndex;
            set => SetAndRiseProperty(ref _selectedHealthStatusIndex, value);
        }

        public int SelectedBloodTypeIndex
        {
            get => _selectedBloodTypeIndex;
            set => SetAndRiseProperty(ref _selectedBloodTypeIndex, value);
        }

        public string AllergiesText
        {
            get => _allergiesText;
            set => SetAndRiseProperty(ref _allergiesText, value);
        }

        public ObservableCollection<Doctor> AvailableDoctors
        {
            get => _availableDoctors;
            set => SetAndRiseProperty(ref _availableDoctors, value);
        }

        public bool IsEditMode => _isEditMode;

        #endregion
    }
}

