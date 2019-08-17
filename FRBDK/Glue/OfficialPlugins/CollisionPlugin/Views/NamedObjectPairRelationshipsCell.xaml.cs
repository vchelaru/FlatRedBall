using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.ViewModels;
using OfficialPlugins.CollisionPlugin.Controllers;
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
    /// Interaction logic for NamedObjectPairRelationshipsCell.xaml
    /// </summary>
    public partial class NamedObjectPairRelationshipsCell : UserControl
    {
        NamedObjectPairRelationshipViewModel ViewModel
        {
            get
            {
                return DataContext as NamedObjectPairRelationshipViewModel;
            }
        }

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
