using Glue;
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

namespace TileGraphicsPlugin.Views
{
    /// <summary>
    /// Interaction logic for AddNewLevelView.xaml
    /// </summary>
    public partial class AddNewLevelView
    {
        public AddNewLevelView()
        {

            InitializeComponent();

            ////StartPosition = FormStartPosition.Manual;
            //this.

            this.Left = MainGlueWindow.MousePosition.X - this.Width / 2;
            this.Top = System.Math.Max(0, MainGlueWindow.MousePosition.Y - Height / 2);
            // Location = new Point(MainGlueWindow.MousePosition.X - this.Width / 2,
            //    System.Math.Max(0, MainGlueWindow.MousePosition.Y - Height / 2));
        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {

//#if DEBUG
//            if (viewModel.CreateShareTilesetWith && viewModel.SelectedSharedFile == null)
//            {
//                throw new Exception("The viewModel indicates that the user selected a shared file, but it'the file is not specified");
//            }
//#endif
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
