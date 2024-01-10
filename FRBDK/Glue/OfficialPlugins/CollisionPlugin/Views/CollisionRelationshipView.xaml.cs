using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GlueFormsCore.ViewModels;
using OfficialPlugins.CollisionPlugin.Controllers;
using OfficialPlugins.CollisionPlugin.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace OfficialPlugins.CollisionPlugin.Views
{
    /// <summary>
    /// Interaction logic for CollisionRelationshipView.xaml
    /// </summary>
    public partial class CollisionRelationshipView : UserControl
    {
        CollisionRelationshipViewModel ViewModel => DataContext as CollisionRelationshipViewModel;

        public CollisionRelationshipView()
        {
            InitializeComponent();
        }

        private void AddEventButtonClicked(object sender, RoutedEventArgs e)
        {
            var namedObject = GlueState.Self.CurrentNamedObjectSave;

            var viewModel = new AddEventViewModel();
            viewModel.DesiredEventType = CustomEventType.Tunneled;
            viewModel.TunnelingObject = namedObject.InstanceName;
            viewModel.TunnelingEvent = "CollisionOccurred";

            viewModel.EventName = namedObject.InstanceName + "Collided";
            GlueCommands.Self.GluxCommands.ElementCommands.AddEventToElement(viewModel, GlueState.Self.CurrentElement);
        }

        private void FirstObjectCollisionPartitioningButtonClicked(object sender, RoutedEventArgs e)
        {
            CollisionRelationshipViewModelController.HandleFirstCollisionPartitionClicked(ViewModel);
        }

        private void SecondObjectCollisionPartitioningButtonClicked(object sender, RoutedEventArgs e)
        {
            CollisionRelationshipViewModelController.HandleSecondCollisionPartitionClicked(ViewModel);
        }
    }
}
