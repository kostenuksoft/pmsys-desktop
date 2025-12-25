using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PMS.ViewModels;

namespace PMS.Views.Controls;

public partial class AggregationsView : UserControl
{
    public AggregationsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is AggregationsViewModel viewModel)
        {
            viewModel.Initialize();
        }
    }
}