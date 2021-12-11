using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Navigation;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using OfficialPlugins.TreeViewPlugin.Logic;
using OfficialPlugins.TreeViewPlugin.ViewModels;
using PropertyTools.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace OfficialPlugins.TreeViewPlugin.Views
{


    /// <summary>
    /// Interaction logic for MainTreeViewControl.xaml
    /// </summary>
    public partial class MainTreeViewControl : UserControl
    {
        #region Enums

        public enum LeftOrRight
        {
            Left,
            Right
        }

        #endregion

        #region Fields/Properties

        LeftOrRight ButtonPressed;

        MainTreeViewViewModel ViewModel => DataContext as MainTreeViewViewModel;

        #endregion

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
            if(e.Key == Key.Enter)
            {
                var selectedNode = SelectionLogic.CurrentNode;
                GlueCommands.Self.TreeNodeCommands.HandleTreeNodeDoubleClicked(selectedNode);
                e.Handled = true;
            }
            else if(e.Key == Key.Delete)
            {
                HotkeyManager.HandleDeletePressed();
                e.Handled = true;
            }
            else if(HotkeyManager.Self.TryHandleKeys(e))
            {
                e.Handled = true;
            }
        }

        public void FocusSearchBox()
        {
            SearchTextBox.Focus();
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

            var isMouseButtonPressed =
                e.LeftButton == MouseButtonState.Pressed ||
                e.RightButton == MouseButtonState.Pressed;

            if (isMouseButtonPressed &&
                
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                ButtonPressed = e.LeftButton == MouseButtonState.Pressed ?
                    LeftOrRight.Left : LeftOrRight.Right;

                // Get the dragged ListViewItem
                var vm = (e.OriginalSource as FrameworkElement).DataContext as NodeViewModel;

                if(vm != null)
                {
                    // Initialize the drag & drop operation
                    DataObject dragData = new DataObject("NodeViewModel", vm);
                    DragDrop.DoDragDrop(e.OriginalSource as DependencyObject, dragData, DragDropEffects.Move);
                }
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

        private void MainTreeView_Drop(object sender, DragEventArgs e)
        {
            if(e.Data.GetDataPresent("FileDrop"))
            {
                HandleDropFileFromExplorerWindow(e);
            }
            else
            {
                HandleDropTreeNodeOnTreeNode(e);
            }
        }

        private void HandleDropFileFromExplorerWindow(DragEventArgs e)
        {
            FilePath[] droppedFiles = ((string[])e.Data.GetData("FileDrop"))
                .Select(item => new FilePath(item))
                .ToArray();

            var targetNode = (e.OriginalSource as FrameworkElement).DataContext as NodeViewModel;

            DragDropManager.Self.HandleDropExternalFileOnTreeNode(droppedFiles, targetNode);
        }

        private void HandleDropTreeNodeOnTreeNode(DragEventArgs e)
        {
            var objectDragged = e.Data.GetData("NodeViewModel");

            var targetNode = (e.OriginalSource as FrameworkElement).DataContext as NodeViewModel;

            if (objectDragged is NodeViewModel treeNodeMoving)
            {
                if (ButtonPressed == LeftOrRight.Left || targetNode == treeNodeMoving)
                {
                    // do something here...
                    DragDropManager.DragDropTreeNode(targetNode, treeNodeMoving);
                    if (ButtonPressed == LeftOrRight.Right)
                    {
                        RightClickContextMenu.IsOpen = true;// test this
                    }
                }
                else
                {
                    SelectionLogic.SelectByTag(targetNode.Tag);

                    var items = RightClickHelper.GetRightClickItems(targetNode, MenuShowingAction.RightButtonDrag, treeNodeMoving);


                    RightClickContextMenu.Items.Clear();

                    foreach (var item in items)
                    {
                        var wpfItem = CreateWpfItemFor(item);
                        RightClickContextMenu.Items.Add(wpfItem);
                    }
                    RightClickContextMenu.IsOpen = true;// test this

                }
            }
        }

        public object CreateWpfItemFor(GlueFormsCore.FormHelpers.GeneralToolStripMenuItem item)
        {
            if (item.Text == "-")
            {
                var separator = new Separator();
                return separator;
            }
            else
            {
                var menuItem = new MenuItem();
                menuItem.Header = item.Text;
                menuItem.Click += (not, used) =>
                {
                    if(item?.Click == null)
                    {
                        int m = 3;
                    }
                    item?.Click?.Invoke(menuItem, null);
                };

                foreach (var child in item.DropDownItems)
                {
                    var wpfItem = CreateWpfItemFor(child);
                    menuItem.Items.Add(wpfItem);
                }

                return menuItem;
            }
        }

        #endregion

        #region Searching

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Escape)
            {
                ViewModel.SearchBoxText = string.Empty;
            }
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            var whatWasSelected = SelectionLogic.CurrentNode?.Tag;

            ViewModel.SearchBoxText = string.Empty;
            ViewModel.ScreenRootNode.IsExpanded = false;
            ViewModel.EntityRootNode.IsExpanded = false;
            ViewModel.GlobalContentRootNode.IsExpanded = false;
            if (whatWasSelected != null)
            {
                SelectionLogic.SelectByTag(whatWasSelected);
                SelectionLogic.CurrentNode.ExpandParentsRecursively();
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ViewModel.IsSearchBoxFocused = true;
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ViewModel.IsSearchBoxFocused = false;
        }

        #endregion

        private void MainTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // todo - look at ElementViewWindow.cs ElementDoubleClick, extract that into common code
            var selectedNode = SelectionLogic.CurrentNode;
            GlueCommands.Self.TreeNodeCommands.HandleTreeNodeDoubleClicked(selectedNode);
        }

        private void BackButtonClicked(object sender, RoutedEventArgs e)
        {
            TreeNodeStackManager.Self.GoBack();
        }

        private void NextButtonClicked(object sender, RoutedEventArgs e)
        {
            TreeNodeStackManager.Self.GoForward();
        }

        private void CollapseAllClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.CollapseAll();
        }
    }
}
