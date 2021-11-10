using OfficialPlugins.TreeViewPlugin.ViewModels;
using System;
using System.Collections.Generic;
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

namespace OfficialPlugins.TreeViewPlugin.Views
{
    /// <summary>
    /// Interaction logic for MainTreeViewControl.xaml
    /// </summary>
    public partial class MainTreeViewControl : UserControl
    {
        public MainTreeViewControl()
        {
            InitializeComponent();


        }

        // This makes the selection not happen on push+move as explained here:
        // https://stackoverflow.com/questions/2645265/wpf-listbox-click-and-drag-selects-other-items
        private void MainTreeView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                (sender as ListBox).ReleaseMouseCapture();
        }
    }
}
