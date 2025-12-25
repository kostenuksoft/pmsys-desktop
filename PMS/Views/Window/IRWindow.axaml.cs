using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using PMS.Core.Services;
using PMS.ViewModels;

namespace PMS.Views.Window;

public partial class IrWindow : Avalonia.Controls.Window
{
    public IrWindow()
    {
        SystemDecorations = SystemDecorations.BorderOnly;

        var localizationService = new LocalizationService();
        DataContext = new IRWindowViewModel(localizationService);

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

