using FlatRedBall.Glue.Managers;
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

        private void TestPriorityClicked(object sender, RoutedEventArgs e)
        {
            var taskmanager = TaskManager.Self;

            const string addToEnd1 = "Add to end1";
            const string addToEnd2 = "Add to end2";

            for (int i = 0; i < 60; i++)
            {
                TaskManager.Self.Add(() => Task.Delay(100), "asap" + i);
            }
            
            TaskManager.Self.Add(() => Task.Delay(5_000), addToEnd1, TaskExecutionPreference.AddOrMoveToEnd);

            for (int i = 0; i < 6; i++)
            {
                TaskManager.Self.Add(() => Task.Delay(5_000), addToEnd2, TaskExecutionPreference.AddOrMoveToEnd);
                TaskManager.Self.Add(() => Task.Delay(5_000), addToEnd1, TaskExecutionPreference.AddOrMoveToEnd);
            }

            //TaskManager.Self.Add(() => Task.Delay(5_000), addToEnd1, TaskExecutionPreference.AddOrMoveToEnd); TaskManager.Self.Add(() => Task.Delay(5_000), addToEnd2, TaskExecutionPreference.AddOrMoveToEnd);
            //TaskManager.Self.Add(() => Task.Delay(5_000), addToEnd1, TaskExecutionPreference.AddOrMoveToEnd); TaskManager.Self.Add(() => Task.Delay(5_000), addToEnd2, TaskExecutionPreference.AddOrMoveToEnd);
            //TaskManager.Self.Add(() => Task.Delay(5_000), addToEnd1, TaskExecutionPreference.AddOrMoveToEnd);

        }
    }
}
