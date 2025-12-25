using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PMS.Views.Controls.Dialogs;


public partial class DoctorEditDialogView : UserControl
{
    public DoctorEditDialogView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}