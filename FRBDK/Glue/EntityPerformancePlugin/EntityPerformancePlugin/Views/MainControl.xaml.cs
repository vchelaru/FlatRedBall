using EntityPerformancePlugin.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace EntityPerformancePlugin.Views
{
    /// <summary>
    /// Interaction logic for MainControl.xaml
    /// </summary>
    public partial class MainControl : UserControl
    {
        private MainViewModel ViewModel => DataContext as MainViewModel;
        
        public MainControl()
        {
            InitializeComponent();
        }

        public void RefreshSelection()
        {
            if (ViewModel.IsRootSelected)
            {
                // do nothing, this is bound
            }
            else if(ViewModel.SelectedInstance != null)
            {
                TreeViewInstance.SetSelectedItem(ViewModel.SelectedInstance);
            }
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if(this.DataContext != null)
            {
                var selectedInstance = e.NewValue as InstanceViewModel;

                var viewModel = ViewModel;

                viewModel.SelectedInstance = selectedInstance;
                
                if(selectedInstance != null)
                {
                    viewModel.SelectedIsContainer = selectedInstance.IsContainer;
                }

            }


        }
    }


    public static class TreeViewExtension
    {
        public static bool SetSelectedItem(this TreeView treeView, object item)
        {
            return SetSelected(treeView, item);
        }

        private static bool SetSelected(ItemsControl parent, object child)
        {
            if (parent == null || child == null)
                return false;

            TreeViewItem childNode = parent.ItemContainerGenerator
            .ContainerFromItem(child) as TreeViewItem;

            if (childNode != null)
            {
                childNode.Focus();
                return childNode.IsSelected = true;
            }

            if (parent.Items.Count > 0)
            {
                foreach (object childItem in parent.Items)
                {
                    ItemsControl childControl = parent
                      .ItemContainerGenerator
                      .ContainerFromItem(childItem)
                      as ItemsControl;

                    if (SetSelected(childControl, child))
                        return true;
                }
            }

            return false;
        }
    }
}
