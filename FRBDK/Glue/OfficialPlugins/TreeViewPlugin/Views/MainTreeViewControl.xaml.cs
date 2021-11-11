using FlatRedBall.Glue.Managers;
using OfficialPlugins.TreeViewPlugin.ViewModels;
using PropertyTools.Wpf;
using System;
using System.Collections.Generic;
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

namespace OfficialPlugins.TreeViewPlugin.Views
{
    /// <summary>
    /// Interaction logic for MainTreeViewControl.xaml
    /// </summary>
    public partial class MainTreeViewControl : UserControl
    {
        public MainTreeViewControl()
        {
            InitializeComponent();


        }

        // This makes the selection not happen on push+move as explained here:
        // https://stackoverflow.com/questions/2645265/wpf-listbox-click-and-drag-selects-other-items
        private void MainTreeView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                (sender as ListBox).ReleaseMouseCapture();
        }

        #region Hotkey

        private void MainTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            if(HotkeyManager.Self.TryHandleKeys(e))
            {
                e.Handled = true;
            }
        }

        #endregion

        #region Drag+drop
        Point startPoint;
        private void MainTreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
        }

        private void MainTreeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // Get the current mouse position
            Point mousePos = e.GetPosition(null);
            Vector diff = startPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                // Get the dragged ListViewItem
                var vm = (e.OriginalSource as FrameworkElement).DataContext as NodeViewModel;

                // Initialize the drag & drop operation
                DataObject dragData = new DataObject("NodeViewModel", vm);
                DragDrop.DoDragDrop(e.OriginalSource as DependencyObject, dragData, DragDropEffects.Move);
            }
        }

        // Helper to search up the VisualTree
        private static T FindAnchestor<T>(DependencyObject current)
            where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }

        private void MainTreeView_Drop(object sender, DragEventArgs e)
        {
            var objectDragged = e.Data.GetData("NodeViewModel");

            var targetNode = (e.OriginalSource as FrameworkElement).DataContext as NodeViewModel;

            if(objectDragged is NodeViewModel treeNodeMoving)
            {
                // do something here...
                DragDropManager.DragDropTreeNode(targetNode, treeNodeMoving);
            }
        }

        private void MainTreeView_DragEnter(object sender, DragEventArgs e)
        {
            if (//!e.Data.GetDataPresent("myFormat") ||
                sender == e.Source)
            {
                e.Effects = DragDropEffects.None;
            }
        }
        #endregion
    }
}
