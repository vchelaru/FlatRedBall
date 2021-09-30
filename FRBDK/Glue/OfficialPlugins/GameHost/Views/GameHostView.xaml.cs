using OfficialPlugins.Compiler;
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

namespace OfficialPlugins.GameHost.Views
{
    /// <summary>
    /// Interaction logic for GameHostView.xaml
    /// </summary>
    public partial class GameHostView : UserControl
    {
        public event EventHandler DoItClicked;

        public GameHostView()
        {
            InitializeComponent();
        }

        public void AddChild(UIElement child)
        {
            MainGrid.Children.Add(child);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DoItClicked(this, null);
        }
    }
}
