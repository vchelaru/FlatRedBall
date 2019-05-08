using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RedGrinPlugin.Views
{
    /// <summary>
    /// Interaction logic for MainScreenView.xaml
    /// </summary>
    public partial class MainScreenView : UserControl
    {
        public MainScreenView()
        {
            InitializeComponent();

            CodeTextBox.Text =
                "var networkConfiguration = new GameNetworkConfiguration();\n" +
                "RedGrin.NetworkManager.Self.Initialize(networkConfiguration, Network.NetworkLogger.Self);";
        }
    }
}
