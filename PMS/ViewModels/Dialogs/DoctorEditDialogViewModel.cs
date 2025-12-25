using PMS.Core.Enums.General;
using PMS.Core.Models;
using PMS.Core.Repositories.Interfaces;
using PMS.Core.Services.Interfaces;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PMS.ViewModels.Dialogs
{
    public class DoctorEditDialogViewModel : BaseDialogViewModel<Doctor>
    {
        private readonly Doctor? _originalDoctor;
        private readonly List<Specialty> _specialties;
        private readonly List<Room> _rooms;
        private readonly IDialogService _dialogService;
        private readonly IDoctorRepository _doctorRepository;

        private string _employeeNumber = string.Empty;
        private string _fullName = string.Empty;
        private DateTime? _birthDate;
        private DateTime _hireDate = DateTime.Now;
        private int _experienceYears;
        private string _phone = string.Empty;
        private string _email = string.Empty;
        private Specialty? _selectedSpecialty;
        private Room? _selectedRoom;
        private DoctorCategory _selectedCategory = DoctorCategory.Second;
        private bool _isDistrictDoctor;
        private string _districtArea = string.Empty;
        private bool _isActive = true;

        private ObservableCollection<Certification> _certifications = [];
        private Certification? _selectedCertification;

        public DoctorEditDialogViewModel(
            Doctor? doctor,
            IEnumerable<Specialty> specialties,
            IEnumerable<Room> rooms,
            IDialogService dialogService,
            IDoctorRepository doctorRepository)
        {
            _originalDoctor = doctor;
            _specialties = specialties.ToList();
            _rooms = rooms.ToList();
            _dialogService = dialogService;
            _doctorRepository = doctorRepository;

            Title = doctor == null ? "Додати лікаря" : "Редагувати лікаря";

            AvailableSpecialties = new ObservableCollection<Specialty>(_specialties);
            AvailableRooms = new ObservableCollection<Room>(_rooms);

            if (doctor != null)
            {
                EmployeeNumber = doctor.EmployeeNumber;
                FullName = doctor.FullName;
                BirthDate = doctor.BirthDate;
                HireDate = doctor.HireDate;
                ExperienceYears = doctor.ExperienceYears;
                Phone = doctor.Phone ?? string.Empty;
                Email = doctor.Email ?? string.Empty;
                SelectedSpecialty = _specialties.FirstOrDefault(s => s.Id == doctor.SpecialtyId);
                SelectedRoom = _rooms.FirstOrDefault(r => r.Id == doctor.RoomId);
                SelectedCategory = doctor.Category;
                IsDistrictDoctor = doctor.IsDistrictDoctor;
                DistrictArea = doctor.DistrictArea ?? string.Empty;
                IsActive = doctor.IsActive;

                foreach (var cert in doctor.Certifications)
                {
                    Certifications.Add(cert);
                }
            }
            else
            {
                BirthDate = DateTime.Now.AddYears(-30);
                HireDate = DateTime.Now;

                _ = GenerateEmployeeNumberAsync();
            }

            AddCertificationCommand = ReactiveCommand.CreateFromTask(AddCertificationAsync);
            RemoveCertificationCommand = ReactiveCommand.Create(
                RemoveCertification,
                this.WhenAnyValue(x => x.SelectedCertification).Select(c => c != null));

            SetupValidation();
        }

        #region Properties

        public ObservableCollection<Specialty> AvailableSpecialties { get; }
        public ObservableCollection<Room> AvailableRooms { get; }
        public ObservableCollection<Certification> Certifications
        {
            get => _certifications;
            set => SetAndRiseProperty(ref _certifications, value);
        }

        public Certification? SelectedCertification
        {
            get => _selectedCertification;
            set => SetAndRiseProperty(ref _selectedCertification, value);
        }

        public string EmployeeNumber
        {
            get => _employeeNumber;
            set => SetAndRiseProperty(ref _employeeNumber, value);
        }

        public string FullName
        {
            get => _fullName;
            set => SetAndRiseProperty(ref _fullName, value);
        }

        public DateTime? BirthDate
        {
            get => _birthDate;
            set => SetAndRiseProperty(ref _birthDate, value);
        }

        public DateTime HireDate
        {
            get => _hireDate;
            set => SetAndRiseProperty(ref _hireDate, value);
        }

        public int ExperienceYears
        {
            get => _experienceYears;
            set => SetAndRiseProperty(ref _experienceYears, value);
        }

        public string Phone
        {
            get => _phone;
            set => SetAndRiseProperty(ref _phone, value);
        }

        public string Email
        {
            get => _email;
            set => SetAndRiseProperty(ref _email, value);
        }

        public Specialty? SelectedSpecialty
        {
            get => _selectedSpecialty;
            set => SetAndRiseProperty(ref _selectedSpecialty, value);
        }

        public Room? SelectedRoom
        {
            get => _selectedRoom;
            set => SetAndRiseProperty(ref _selectedRoom, value);
        }

        public DoctorCategory SelectedCategory
        {
            get => _selectedCategory;
            set => SetAndRiseProperty(ref _selectedCategory, value);
        }

        public bool IsDistrictDoctor
        {
            get => _isDistrictDoctor;
            set => SetAndRiseProperty(ref _isDistrictDoctor, value);
        }

        public string DistrictArea
        {
            get => _districtArea;
            set => SetAndRiseProperty(ref _districtArea, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetAndRiseProperty(ref _isActive, value);
        }

        public Array DoctorCategories => Enum.GetValues(typeof(DoctorCategory));

        #endregion

        #region Commands

        public ICommand AddCertificationCommand { get; }
        public ICommand RemoveCertificationCommand { get; }

        #endregion

        #region Validation

        private void SetupValidation()
        {
            this.ValidationRule(
                vm => vm.EmployeeNumber,
                employeeNumber => !string.IsNullOrWhiteSpace(employeeNumber),
                "Табельний номер обов'язковий");

            this.ValidationRule(
                vm => vm.EmployeeNumber,
                employeeNumber => System.Text.RegularExpressions.Regex.IsMatch(employeeNumber ?? "", @"^D\d{5}$"),
                "Формат: D00001");

            this.ValidationRule(
                vm => vm.FullName,
                fullName => !string.IsNullOrWhiteSpace(fullName),
                "ПІБ обов'язкове");

            this.ValidationRule(
                vm => vm.FullName,
                fullName => fullName?.Length >= 3,
                "Мінімум 3 символи");

            this.ValidationRule(
                vm => vm.Phone,
                phone => !string.IsNullOrWhiteSpace(phone),
                "Телефон обов'язковий");

            this.ValidationRule(
                vm => vm.Phone,
                phone => phone?.Length >= 10,
                "Некоректний формат телефону");

            this.ValidationRule(
                vm => vm.Email,
                email => string.IsNullOrWhiteSpace(email) ||
                        (email.Contains("@") && email.Contains(".")),
                "Некоректний формат email");

            this.ValidationRule(
                vm => vm.SelectedSpecialty,
                specialty => specialty != null,
                "Спеціальність обов'язкова");

            this.ValidationRule(
                this.WhenAnyValue(
                    x => x.DistrictArea,
                    x => x.IsDistrictDoctor,
                    (area, isDistrict) => !isDistrict || !string.IsNullOrWhiteSpace(area)),
                "Дільнична територія обов'язкова для дільничного лікаря");

            this.WhenAnyValue(x => x.ValidationContext.IsValid)
                .Subscribe(isValid => CanExecutePrimary = isValid);
        }

        protected override async Task<bool> ValidateAsync()
        {
            if (_originalDoctor == null)
            {
                var existing = await _doctorRepository.GetByEmployeeNumberAsync(EmployeeNumber);
                if (existing != null)
                {
                    await _dialogService.ShowErrorAsync("Помилка", $"Табельний номер {EmployeeNumber} вже використовується");
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Methods

        private async Task GenerateEmployeeNumberAsync()
        {
            try
            {
                var allDoctors = await _doctorRepository.GetAllAsync();

                if (!allDoctors.Any())
                {
                    EmployeeNumber = "D00001";
                    return;
                }

                var existingNumbers = allDoctors
                    .Select(d => d.EmployeeNumber)
                    .Where(num => !string.IsNullOrEmpty(num) && num.StartsWith("D") && num.Length == 6)
                    .Select(num =>
                    {
                        if (int.TryParse(num.Substring(1), out int number))
                            return number;
                        return 0;
                    })
                    .Where(num => num > 0)
                    .ToList();

                int nextNumber = 1;
                if (existingNumbers.Any())
                {
                    nextNumber = existingNumbers.Max() + 1;
                }

                EmployeeNumber = $"D{nextNumber:D5}";
            }
            catch (Exception)
            {
                EmployeeNumber = "D00001";
            }
        }

        private async Task AddCertificationAsync()
        {
            try
            {
                var name = await _dialogService.ShowInputAsync(
                    "Додати сертифікат",
                    "Введіть назву сертифікату:");

                if (string.IsNullOrWhiteSpace(name))
                    return;

                var issueDate = await _dialogService.ShowDatePickerAsync(
                    "Дата видачі",
                    DateTime.Now);

                if (!issueDate.HasValue)
                    return;

                var expiryDate = await _dialogService.ShowDatePickerAsync(
                    "Термін дії до",
                    issueDate.Value.AddYears(3));

                if (!expiryDate.HasValue)
                    return;

                var certification = new Certification
                {
                    Name = name.Trim(),
                    IssueDate = issueDate.Value,
                    ExpiryDate = expiryDate.Value
                };

                Certifications.Add(certification);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("Помилка", $"Не вдалося додати сертифікат", ex);
            }
        }

        private void RemoveCertification()
        {
            if (SelectedCertification != null)
            {
                Certifications.Remove(SelectedCertification);
                SelectedCertification = null;
            }
        }

        #endregion

        #region Dialog Result

        protected override Doctor? GetResult()
        {
            var doctor = _originalDoctor ?? new Doctor();

            doctor.EmployeeNumber = EmployeeNumber.Trim();
            doctor.FullName = FullName.Trim();
            doctor.BirthDate = BirthDate;
            doctor.HireDate = HireDate;
            doctor.ExperienceYears = ExperienceYears;
            doctor.Phone = Phone.Trim();
            doctor.Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
            doctor.SpecialtyId = SelectedSpecialty?.Id ?? string.Empty;
            doctor.RoomId = SelectedRoom?.Id;
            doctor.Category = SelectedCategory;
            doctor.IsDistrictDoctor = IsDistrictDoctor;
            doctor.DistrictArea = IsDistrictDoctor && !string.IsNullOrWhiteSpace(DistrictArea)
                ? DistrictArea.Trim()
                : null;
            doctor.IsActive = IsActive;

            doctor.Certifications = Certifications.ToList();

            if (_originalDoctor == null)
            {
                doctor.CreatedDate = DateTime.UtcNow;
            }
            else
            {
                doctor.ModifiedDate = DateTime.UtcNow;
            }

            return doctor;
        }

        protected override Task OnSecondaryCommandAsync()
        {
            OnCancelCommand();
            return Task.CompletedTask;
        }

        #endregion
    }
}
