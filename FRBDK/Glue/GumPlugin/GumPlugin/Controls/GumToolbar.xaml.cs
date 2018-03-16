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

namespace GumPlugin.Controls
{
    /// <summary>
    /// Interaction logic for GumToolbar.xaml
    /// </summary>
    public partial class GumToolbar : UserControl
    {
        public event EventHandler GumButtonClicked;


        public GumToolbar()
        {
            InitializeComponent();
        }

        private void HandleButtonClick(object sender, RoutedEventArgs e)
        {
            GumButtonClicked?.Invoke(this, null);
        }
    }
}
