using EntityPerformancePlugin.ViewModels;
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

namespace EntityPerformancePlugin.Views
{
    /// <summary>
    /// Interaction logic for MainControl.xaml
    /// </summary>
    public partial class MainControl : UserControl
    {
        private MainViewModel ViewModel
        {
            get
            {
                return (MainViewModel)this.DataContext;
            }
        }


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

                // deselect first, otherwise the view model will act as if an instance is selected and the root is selected:
                if(selectedInstance != null)
                {
                    // IsRootSelected is handled by binding
                    //viewModel.IsRootSelected = false;
                    viewModel.SelectedInstance = selectedInstance;
                }
                else
                {

                    viewModel.SelectedInstance = selectedInstance;
                    //viewModel.IsRootSelected = selectedInstance == null && e.NewValue != null;
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
