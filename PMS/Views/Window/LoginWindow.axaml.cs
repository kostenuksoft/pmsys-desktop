using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using PMS.ViewModels;
using PMS.Views.Abstractions.Window;
using System;
using System.Linq;
using System.Text;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using PMS.Core.Enums.General;
using ReactiveUI;
using Serilog;
using PMS.Core.Services.Interfaces;

namespace PMS.Views.Window
{
    public partial class LoginWindow : BaseWindow
    {
        private TextBox? _usernameBox;
        private TextBox? _passwordBox; 

        public LoginWindow()
        {
            InitializeComponent();
            Opened += OnOpened;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _usernameBox = this.FindControl<TextBox>("UsernameBox");
            _passwordBox = this.FindControl<TextBox>("PasswordBox");

            if (_passwordBox != null)
            {
                _passwordBox.PropertyChanged += PasswordBox_PropertyChanged;
            }

        }

        private void SubmitButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel lvm)
            {
                var allErrors = string.Join(
                    Environment.NewLine,
                    lvm.GetErrors(nameof(lvm.Username))
                        .Cast<string>()
                        .Concat(lvm.GetErrors(nameof(lvm.Password))
                        .Cast<string>()));


                if (!lvm.HasErrors)
                {
                    lvm.CleanUp();

                    _usernameBox?.Classes.Remove("invalid");
                    _passwordBox?.Classes.Remove("invalid");
                }
                else
                {
                    var dialogService = App.GetService<IDialogService>();

                    dialogService.ShowNotification("Валідаційна помилка",allErrors, NotificationPosition.BottomCenter, NotificationSeverity.Error);
                  
                    _usernameBox?.Classes.Add("invalid");
                    _passwordBox?.Classes.Add("invalid");
                }
            }
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            _usernameBox?.Focus();
        }

        private void PasswordBox_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TextBox.PasswordCharProperty && DataContext is LoginViewModel viewModel)
            {
                if (_passwordBox != null && !viewModel.ShowPassword)
                {
                    viewModel.Password = _passwordBox.Text ?? string.Empty;
                }
            }
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is LoginViewModel viewModel)
            {
                if (viewModel.LoginCommand.CanExecute(null))
                {
                    viewModel.LoginCommand.Execute(null);
                }
            }
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is LoginViewModel viewModel && _passwordBox != null)
            {
                _passwordBox.Text = viewModel.Password;
            }
        }

        protected override async void OnClosed(EventArgs e)
        {
            if (DataContext is LoginViewModel loginViewModel)
            {
                loginViewModel.CleanUp();
            }

            base.OnClosed(e);
        }

        private void OnClose(object? sender, RoutedEventArgs e)
        {
           Close();
        }
    }
}