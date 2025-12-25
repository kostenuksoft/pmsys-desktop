using Avalonia.Interactivity;
using Avalonia.Media;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;

namespace PMS.Views.Abstractions.Window;

public abstract class BaseWindow : AppWindow
{

    protected BaseWindow()
    {
        InitializeTitleBar();
    }

    private void InitializeTitleBar()
    {
        TitleBar.BackgroundColor = Colors.DarkSlateGray;
        TitleBar.ForegroundColor = Colors.White;
        TitleBar.InactiveBackgroundColor = Colors.SlateGray;
        TitleBar.InactiveForegroundColor = Colors.LightGray;

        TitleBar.ButtonBackgroundColor = Colors.Transparent;
        TitleBar.ButtonForegroundColor = Colors.White;
        TitleBar.ButtonHoverBackgroundColor = Color.FromRgb(90, 130, 130);
        TitleBar.ButtonPressedBackgroundColor = Color.FromRgb(45, 80, 80);
        TitleBar.ButtonInactiveForegroundColor = Colors.White; 

        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;
        TitleBar.Height = 40;
        TitleBar.SetDragRectangles(null);

    }

    
}