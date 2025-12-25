using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PMS.Views.Controls.Dialogs;

public partial class RoomEditDialogView : UserControl
{
    public RoomEditDialogView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}