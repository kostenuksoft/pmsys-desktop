using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;

namespace PMS.Views.Controls;

public partial class StatusCheckView : UserControl
{
    private TeachingTip? _helpTip;
    private TextBox? _requestCodeBox;

    public StatusCheckView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        _helpTip = this.FindControl<TeachingTip>("HelpTip");
        _requestCodeBox = this.FindControl<TextBox>("RequestCodeBox");
        Loaded += (_, _) => _requestCodeBox?.Focus();
    }

    private void ShowHelp_Click(object? sender, RoutedEventArgs e)
    {
        if (_helpTip != null && sender is Control button)
        {
            _helpTip.Width = 500;
            _helpTip.Target = button;
            _helpTip.IsOpen = true;
        }
    }
}