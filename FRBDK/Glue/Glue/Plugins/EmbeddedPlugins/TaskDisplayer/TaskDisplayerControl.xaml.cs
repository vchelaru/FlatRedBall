using FlatRedBall.Glue.Managers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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
            var text = TaskManager.Self.AllTasksDescription;

            System.Windows.Clipboard.SetText(text);
        }


        private void AddTempTasksClicked(object sender, RoutedEventArgs e)
        {

            for(int i = 0; i < 22; i++)
            {
                TaskManager.Self.Add(() =>
                {
                    TaskManager.Self.Add(async () =>
                    {
                        await Task.Delay(2_000);
                    }, "Testing it out", TaskExecutionPreference.AddOrMoveToEnd);

                }, "outer add");
            }
        }

        

    }
}
