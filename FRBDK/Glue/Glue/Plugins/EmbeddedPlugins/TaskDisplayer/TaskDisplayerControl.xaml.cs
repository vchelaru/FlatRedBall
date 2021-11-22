using FlatRedBall.Glue.Managers;
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

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.TaskDisplayer
{
    /// <summary>
    /// Interaction logic for TaskDisplayerControl.xaml
    /// </summary>
    public partial class TaskDisplayerControl : UserControl
    {
        public TaskDisplayerControl()
        {
            InitializeComponent();

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            for(int i = 0; i < 10; i++)
            {
                TaskManager.Self.Add(() => System.Threading.Thread.Sleep(1000), $"Debug thread sleep {i}");
            }
        }
    }
}
