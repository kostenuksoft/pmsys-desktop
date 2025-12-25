using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using PMS.Views.Abstractions.Window;

namespace PMS.Views.Window;

public partial class TechTaskView : BaseWindow
{
    
    public TechTaskView()
    {

        TitleBar.BackgroundColor = Colors.White;
        CanResize = true;
        
        InitializeComponent();
    }

    private void InitializeComponent()
    {
       AvaloniaXamlLoader.Load(this);
    }


    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}