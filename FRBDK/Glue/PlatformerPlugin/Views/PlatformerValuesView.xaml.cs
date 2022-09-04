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

namespace FlatRedBall.PlatformerPlugin.Views
{
    /// <summary>
    /// Interaction logic for PlatformerValuesView.xaml
    /// </summary>
    public partial class PlatformerValuesView : UserControl
    {
        public event RoutedEventHandler XClick;
        public event RoutedEventHandler MoveUpClicked;
        public event RoutedEventHandler MoveDownClicked;
        public event RoutedEventHandler DuplicateClicked;


        public PlatformerValuesView()
        {
            InitializeComponent();
        }

        private void HandleXClick       (object sender, RoutedEventArgs e) => XClick?.Invoke(this, null);
        private void HandleMoveUpClicked(object sender, RoutedEventArgs e) => MoveUpClicked?.Invoke(this, null);
        private void HandleMoveDownClicked(object sender, RoutedEventArgs e) => MoveDownClicked?.Invoke(this, null);
        private void HandleDuplicateClicked(object sender, RoutedEventArgs e) => DuplicateClicked?.Invoke(this, null);
    }
}
