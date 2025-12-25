using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using PMS.Core.Services;
using PMS.ViewModels;
using System;
using System.Linq;

namespace PMS.Views.Controls;

public partial class LoginView : UserControl
{
    private TextBox? _usernameBox;
    private TextBox? _passwordBox;

    public LoginView()
    {
        InitializeComponent();
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

        if (_usernameBox != null)
        {
            _usernameBox.KeyDown += Control_KeyDown;
        }

        if (_passwordBox != null)
        {
            _passwordBox.KeyDown += Control_KeyDown;
        }

    }

    private void SubmitButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        
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


    private void DisableTextOperations_KeyDown(object sender, KeyEventArgs e)
    {
        if (e is { KeyModifiers: KeyModifiers.Control, Key: Key.C or Key.X or Key.A or Key.V })
        {
            e.Handled = true;
        }
    }

    private void Control_KeyDown(object? sender, KeyEventArgs e)
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

}