using FlatRedBall.Glue.Plugins.ExportedImplementations;
using OfficialPlugins.CollisionPlugin.ViewModels;
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
    /// Interaction logic for RelationshipListCell.xaml
    /// </summary>
    public partial class RelationshipListCell : UserControl
    {
        RelationshipListCellViewModel ViewModel
        {
            get
            {
                return DataContext as RelationshipListCellViewModel;
            }
        }
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
            }
        }
    }
}
