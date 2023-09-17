using System.Windows;
using System.Windows.Controls;

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
