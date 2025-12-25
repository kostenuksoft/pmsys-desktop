using PMS.Core.Enums.General;
using PMS.Core.Models;
using PMS.Core.Models.Common;
using ReactiveUI.Validation.Extensions;
using System;
using System.Text.RegularExpressions;

namespace PMS.ViewModels.Dialogs
{
    public class UserEditDialogViewModel : BaseDialogViewModel<User>
    {
        private readonly User? _originalUser;
        private readonly bool _isEditMode;

        private string _login = string.Empty;
        private string _password = string.Empty;
        private string _fullName = string.Empty;
        private string _email = string.Empty;
        private string _phone = string.Empty;
        private int _selectedRoleIndex = 0; 
        private bool _isActive = true;

        private bool _viewData = false;
        private bool _editData = false;
        private bool _deleteData = false;
        private bool _runAggregations = false;
        private bool _saveResults = false;
        private bool _manageUsers = false;

        public UserEditDialogViewModel(User? user)
        {
            _originalUser = user;
            _isEditMode = user != null;

            if (_isEditMode && user != null)
            {
                Title = "Редагування користувача";
                LoadUserData(user);
            }
            else
            {
                Title = "Новий користувач";
                ViewData = true;
            }

            SetupValidation();
        }

        private void SetupValidation()
        {
            this.ValidationRule(
                vm => vm.Login,
                login => !string.IsNullOrWhiteSpace(login),
                "Логін обов'язковий");

            this.ValidationRule(
                vm => vm.Login,
                login => login != null && login.Length >= 3,
                "Логін має містити мінімум 3 символи");

            this.ValidationRule(
                vm => vm.Login,
                login => login == null || Regex.IsMatch(login, @"^[a-zA-Z0-9_]+$"),
                "Логін може містити лише літери, цифри та '_'");

            if (!_isEditMode)
            {
                this.ValidationRule(
                    vm => vm.Password,
                    password => !string.IsNullOrWhiteSpace(password),
                    "Пароль обов'язковий");

                this.ValidationRule(
                    vm => vm.Password,
                    password => password != null && password.Length >= 6,
                    "Пароль має містити мінімум 6 символів");
            }

            this.ValidationRule(
                vm => vm.FullName,
                name => !string.IsNullOrWhiteSpace(name),
                "ПІБ обов'язкове");

            this.ValidationRule(
                vm => vm.Email,
                email => string.IsNullOrWhiteSpace(email) ||
                         Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"),
                "Невірний формат email");

            this.IsValid()
                .Subscribe(isValid => CanExecutePrimary = isValid);
        }

        private void LoadUserData(User user)
        {
            Login = user.Login;
            FullName = user.FullName;
            Email = user.Email ?? string.Empty;
            Phone = user.Phone ?? string.Empty;
            SelectedRoleIndex = (int)user.Role;
            IsActive = user.IsActive;

            if (user.AccessRights != null)
            {
                ViewData = user.AccessRights.ViewData;
                EditData = user.AccessRights.EditData;
                DeleteData = user.AccessRights.DeleteData;
                RunAggregations = user.AccessRights.RunAggregations;
                SaveResults = user.AccessRights.SaveResults;
                ManageUsers = user.AccessRights.ManageUsers;
            }
        }

        protected override User? GetResult()
        {
            if (!IsConfirmed)
                return null;

            var user = _isEditMode && _originalUser != null
                ? _originalUser 
                : new User();

            user.Login = Login;
            user.FullName = FullName;
            user.Email = string.IsNullOrWhiteSpace(Email) ? string.Empty : Email;
            user.Phone = string.IsNullOrWhiteSpace(Phone) ? string.Empty : Phone;
            user.Role = (UserRole)SelectedRoleIndex;
            user.IsActive = IsActive;

            user.AccessRights = new AccessRights
            {
                ViewData = ViewData,
                EditData = EditData,
                DeleteData = DeleteData,
                RunAggregations = RunAggregations,
                SaveResults = SaveResults,
                ManageUsers = ManageUsers
            };

            if (!_isEditMode)
            {
                user.PasswordHash = Password;
            }

            return user;
        }

        #region Properties

        public string Login
        {
            get => _login;
            set => SetAndRiseProperty(ref _login, value);
        }

        public string Password
        {
            get => _password;
            set => SetAndRiseProperty(ref _password, value);
        }

        public string FullName
        {
            get => _fullName;
            set => SetAndRiseProperty(ref _fullName, value);
        }

        public string Email
        {
            get => _email;
            set => SetAndRiseProperty(ref _email, value);
        }

        public string Phone
        {
            get => _phone;
            set => SetAndRiseProperty(ref _phone, value);
        }

        public int SelectedRoleIndex
        {
            get => _selectedRoleIndex;
            set => SetAndRiseProperty(ref _selectedRoleIndex, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetAndRiseProperty(ref _isActive, value);
        }

        public bool ViewData
        {
            get => _viewData;
            set => SetAndRiseProperty(ref _viewData, value);
        }

        public bool EditData
        {
            get => _editData;
            set => SetAndRiseProperty(ref _editData, value);
        }

        public bool DeleteData
        {
            get => _deleteData;
            set => SetAndRiseProperty(ref _deleteData, value);
        }

        public bool RunAggregations
        {
            get => _runAggregations;
            set => SetAndRiseProperty(ref _runAggregations, value);
        }

        public bool SaveResults
        {
            get => _saveResults;
            set => SetAndRiseProperty(ref _saveResults, value);
        }

        public bool ManageUsers
        {
            get => _manageUsers;
            set => SetAndRiseProperty(ref _manageUsers, value);
        }

        public bool IsEditMode => _isEditMode;

        #endregion
    }
}
