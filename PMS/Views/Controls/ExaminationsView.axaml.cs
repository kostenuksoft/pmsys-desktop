using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PMS.Views.Controls
{
    public partial class ExaminationsView : UserControl
    {
        public ExaminationsView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
