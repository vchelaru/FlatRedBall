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

namespace TileGraphicsPlugin.Views
{
    /// <summary>
    /// Interaction logic for TiledMapEntityCreationView.xaml
    /// </summary>
    public partial class TiledMapEntityCreationView : UserControl
    {
        public event EventHandler ViewTiledObjectXmlClicked;

        public TiledMapEntityCreationView()
        {
            InitializeComponent();
        }

        private void HandleViewTiledObjectXmlClicked(object sender, RoutedEventArgs e)
        {
            ViewTiledObjectXmlClicked?.Invoke(this, null);
        }
    }
}
