using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PMS.Views.Controls.Dialogs;

public partial class PatientSearchDialogView : UserControl
{
    public PatientSearchDialogView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}