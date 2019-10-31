using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TmxEditor.UI
{
    /// <summary>
    /// Interaction logic for TilesetWindow.xaml
    /// </summary>
    public partial class TilesetWindow : Window
    {
        public TilesetWindow()
        {
            InitializeComponent();

            this.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;

            Left = Form1.MousePosition.X - this.Width/2;
            Top = Form1.MousePosition.Y - this.Height/2;
            //Location = new Point(MainGlueWindow.MousePosition.X - this.Width / 2,
    //System.Math.Max(0, MainGlueWindow.MousePosition.Y - Height / 2));
        }



        public void PositionAtCursor()
        {
            Point absolutePosition = PointToScreen(Mouse.GetPosition(this));

            this.Left = absolutePosition.X - this.Width / 2;
            this.Top = System.Math.Max(0, absolutePosition.Y - this.Height / 2);

        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
