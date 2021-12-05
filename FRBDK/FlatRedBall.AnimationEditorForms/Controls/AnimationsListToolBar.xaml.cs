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

namespace FlatRedBall.AnimationEditorForms.Controls
{
    /// <summary>
    /// Interaction logic for AnimationsListToolBar.xaml
    /// </summary>
    public partial class AnimationsListToolBar : UserControl
    {
        public event EventHandler AddAnimationClick;
        public event EventHandler ExpandAllClick;
        public event EventHandler CollapseAllClick;

        public AnimationsListToolBar()
        {
            InitializeComponent();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddAnimationClick?.Invoke(this, null);
        }

        private void ExpandAll_Click(object sender, RoutedEventArgs e)
        {
            ExpandAllClick?.Invoke(this, null);
        }

        private void CollapseAll_Click(object sender, RoutedEventArgs e)
        {
            CollapseAllClick?.Invoke(this, null);
        }
    }
}
