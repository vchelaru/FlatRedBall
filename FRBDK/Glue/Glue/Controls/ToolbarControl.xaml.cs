using System.Windows.Controls;

namespace FlatRedBall.Glue.Controls
{
    /// <summary>
    /// Interaction logic for ToolbarControl.xaml
    /// </summary>
    public partial class ToolbarControl : UserControl
    {
        public ToolBarTray Tray => this.ToolBarTray;

        public ToolbarControl()
        {
            InitializeComponent();
        }
    }
}
