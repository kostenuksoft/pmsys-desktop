using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PMS.Views.Controls;

public partial class RegistrationView : UserControl
{
    public RegistrationView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}