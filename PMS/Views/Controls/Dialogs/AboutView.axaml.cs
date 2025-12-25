using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PMS.Views.Other;

public partial class AboutView : UserControl
{
    public AboutView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}