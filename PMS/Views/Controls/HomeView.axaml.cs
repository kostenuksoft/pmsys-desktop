using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PMS.ViewModels;

namespace PMS.Views.Controls
{
    public partial class HomeView : UserControl
    {
        public HomeView()
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

            if (DataContext is HomeViewModel viewModel)
            {
                viewModel.Initialize();
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (DataContext is HomeViewModel viewModel)
            {
                viewModel.RefreshCommand.Execute(null);
            }
        }
    }
}