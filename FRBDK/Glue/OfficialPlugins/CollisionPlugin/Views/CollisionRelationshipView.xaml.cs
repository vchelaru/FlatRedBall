using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GlueFormsCore.ViewModels;
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

namespace OfficialPlugins.CollisionPlugin.Views
{
    /// <summary>
    /// Interaction logic for CollisionRelationshipView.xaml
    /// </summary>
    public partial class CollisionRelationshipView : UserControl
    {
        public CollisionRelationshipView()
        {
            InitializeComponent();
        }

        private void AddEventButtonClicked(object sender, RoutedEventArgs e)
        {
            var namedObject = GlueState.Self.CurrentNamedObjectSave;

            var viewModel = new AddEventViewModel();
            viewModel.DesiredEventType = FlatRedBall.Glue.Controls.CustomEventType.Tunneled;
            viewModel.TunnelingObject = namedObject.InstanceName;
            viewModel.TunnelingEvent = "CollisionOccurred";

            GlueCommands.Self.DialogCommands.ShowAddNewEventDialog(viewModel);
        }
    }
}
