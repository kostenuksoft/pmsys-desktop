using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PMS.Views.Controls.Dialogs;

public partial class UserEditDialogView : UserControl
{
    public UserEditDialogView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
