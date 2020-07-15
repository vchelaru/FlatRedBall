using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
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

            //if (namedObject != null)
            //{
            //    GlueState.Self.CurrentNamedObjectSave = namedObject;

            //    GlueCommands.Self.DialogCommands.FocusTab("Collision");
            //}
        }
    }
}
