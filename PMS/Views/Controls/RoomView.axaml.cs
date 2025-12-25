using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PMS.Views.Controls
{
    public partial class RoomsView : UserControl
    {
        public RoomsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}