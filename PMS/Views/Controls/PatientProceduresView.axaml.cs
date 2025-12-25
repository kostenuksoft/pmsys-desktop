using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PMS.ViewModels;
using System;

namespace PMS.Views.Controls;

public partial class PatientProceduresView : UserControl
{
    public PatientProceduresView()
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

        if (DataContext is PatientProceduresViewModel viewModel)
        {
            viewModel.Initialize();
        }
    }
}