using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PMS.Views.Controls.Dialogs;

public partial class PatientEditDialogView : UserControl
{
    public PatientEditDialogView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}