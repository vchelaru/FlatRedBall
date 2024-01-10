using FlatRedBall.Glue.Plugins.ExportedImplementations;
using OfficialPlugins.CollisionPlugin.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace OfficialPlugins.CollisionPlugin.Views
{
    /// <summary>
    /// Interaction logic for RelationshipListCell.xaml
    /// </summary>
    public partial class RelationshipListCell : UserControl
    {
        RelationshipListCellViewModel ViewModel => DataContext as RelationshipListCellViewModel;

        public RelationshipListCell()
        {
            InitializeComponent();
        }

        private void HandleCollisionClicked(object sender, RoutedEventArgs e)
        {
            var namedObject = ViewModel.CollisionRelationshipNamedObject;

            if(namedObject != null)
            {
                GlueState.Self.CurrentNamedObjectSave = namedObject;

                GlueCommands.Self.DialogCommands.FocusTab("Collision");
            }
        }
    }
}
