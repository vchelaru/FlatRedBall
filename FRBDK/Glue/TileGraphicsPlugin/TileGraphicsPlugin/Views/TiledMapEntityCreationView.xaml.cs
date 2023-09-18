using System;
using System.Windows;
using System.Windows.Controls;

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
