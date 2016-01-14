using AtlasPlugin.ViewModels;
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

namespace AtlasPlugin.Controls
{
    /// <summary>
    /// Interaction logic for MainAtlasControl.xaml
    /// </summary>
    public partial class MainAtlasControl : UserControl
    {
        public MainAtlasControl()
        {
            InitializeComponent();
        }

        private void HandleListBoxKeyDown(object sender, KeyEventArgs e)
        {
            (this.DataContext as AtlasListViewModel).HandleListBoxKeyDown(e);
        }
    }
}
