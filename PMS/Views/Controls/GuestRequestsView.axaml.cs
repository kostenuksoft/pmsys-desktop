using Avalonia.Controls;

namespace PMS.Views.Controls;

public partial class GuestRequestsView : UserControl
{
    public GuestRequestsView()
    {
        InitializeComponent();
    }


    private void InitializeComponent()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
    }
}