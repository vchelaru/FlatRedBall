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

namespace OfficialPluginsCore.QuickActionPlugin.Views
{
    /// <summary>
    /// Interaction logic for QuickActionButton.xaml
    /// </summary>
    public partial class QuickActionButton : UserControl
    {
        public event RoutedEventHandler Clicked;

        public string Title
        {
            get => TitleTextBlock.Text;
            set => TitleTextBlock.Text = value;
        }

        public string Details
        {
            get => DetailsTextBlock.Text;
            set => DetailsTextBlock.Text = value;
        }

        public QuickActionButton()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Clicked?.Invoke(this, e);
        }
    }
}
