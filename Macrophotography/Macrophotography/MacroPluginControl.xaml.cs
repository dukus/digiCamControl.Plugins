using System.Windows;
using System.Windows.Controls;

namespace Macrophotography
{
    /// <summary>
    /// Interaction logic for MacroPluginControl.xaml
    /// </summary>
    public partial class MacroPluginControl : UserControl
    {
        private Window window = null;
        public MacroPluginControl()
        {
            InitializeComponent();
        }

        private void but_qr_Click(object sender, RoutedEventArgs e)
        {
            if (window == null || !window.IsVisible)
                window = new LiveView();
            window.Show();
        }
    }
}
