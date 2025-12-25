using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PMS.ViewModels.Dialogs;

namespace PMS.Views.Controls.Dialogs;

public partial class DiagnosisSearchDialogView : UserControl
{
    public DiagnosisSearchDialogView()
    {
        InitializeComponent();
    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}