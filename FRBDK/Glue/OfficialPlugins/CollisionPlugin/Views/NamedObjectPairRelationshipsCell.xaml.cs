using OfficialPlugins.CollisionPlugin.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace OfficialPlugins.CollisionPlugin.Views
{
    /// <summary>
    /// Interaction logic for NamedObjectPairRelationshipsCell.xaml
    /// </summary>
    public partial class NamedObjectPairRelationshipsCell : UserControl
    {
        NamedObjectPairRelationshipViewModel ViewModel => DataContext as NamedObjectPairRelationshipViewModel;

        public NamedObjectPairRelationshipsCell()
        {
            InitializeComponent();
        }

        private void AddClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.AddNewRelationship();
        }
    }
}
