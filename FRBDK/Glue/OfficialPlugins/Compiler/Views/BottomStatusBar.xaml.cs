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

namespace OfficialPlugins.Compiler.Views
{
    /// <summary>
    /// Interaction logic for BottomStatusBar.xaml
    /// </summary>
    public partial class BottomStatusBar : UserControl
    {
        public event Action ZoomMinusClick;
        public event Action ZoomPlusClick;
        public BottomStatusBar()
        {
            InitializeComponent();
        }

        private void ZoomMinusClicked(object sender, RoutedEventArgs e)
        {
            ZoomMinusClick?.Invoke();
        }

        private void ZoomPlusClicked(object sender, RoutedEventArgs e)
        {
            ZoomPlusClick?.Invoke();
        }
    }
}
