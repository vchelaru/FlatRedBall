using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System.Windows;
using System.Windows.Controls;

namespace OfficialPluginsCore.CollisionPlugin.Views
{
    /// <summary>
    /// Interaction logic for EventCell.xaml
    /// </summary>
    public partial class EventCell : UserControl
    {
        EventResponseSave ViewModel => this.DataContext as EventResponseSave;
        public EventCell()
        {
            InitializeComponent();
        }

        private void HandleCollisionClicked(object sender, RoutedEventArgs e)
        {
            // todo:
            var currentElement = GlueState.Self.CurrentElement;
            var eventSave = currentElement.GetEvent(ViewModel.EventName);

            GlueState.Self.CurrentEventResponseSave = eventSave;
        }
    }
}
