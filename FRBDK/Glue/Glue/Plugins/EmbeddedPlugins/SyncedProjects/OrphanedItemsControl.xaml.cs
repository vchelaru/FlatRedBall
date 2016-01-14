using System;
using System.Collections.Generic;
using System.Linq;
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

namespace FlatRedBall.Glue.Controls.ProjectSync
{
    /// <summary>
    /// Interaction logic for SyncedProjectControl.xaml
    /// </summary>
    public partial class OrphanedItemsControl : UserControl
    {
        SyncedProjectViewModel ViewModel
        {
            get
            {
                return DataContext as SyncedProjectViewModel;
            }
        }
        
        public OrphanedItemsControl()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            bool wasAnythingRemoved = false;
            foreach (var orphan in ViewModel.Orphans)
            {
                wasAnythingRemoved |= orphan.CleanSelf();
            }

            if(wasAnythingRemoved)
            {
                ViewModel.SaveProject();
            }

            ViewModel.RefreshOrphans();
            ViewModel.RefreshGeneralErrors();
        }
    }
}
