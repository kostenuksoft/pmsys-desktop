using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PMS.Views.Controls;

public partial class ProceduresView : UserControl
{
    public ProceduresView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}