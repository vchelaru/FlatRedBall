using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Navigation;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
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

        #region Hotkey

        private async void MainTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            var ctrlDown = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            var altDown = (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;
            var shiftDown = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;


            if (e.Key == Key.Enter)
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
            else if(e.Key==Key.N && ctrlDown)
            {
                e.Handled=true;
                ITreeNode currentNode = SelectionLogic.CurrentNode;
                if(currentNode.IsFilesContainerNode())
                {
                    await GlueCommands.Self.DialogCommands.ShowAddNewFileDialogAsync();
                }
                else if(currentNode.IsRootNamedObjectNode())
                {
                    await GlueCommands.Self.DialogCommands.ShowAddNewObjectDialog();
                }
                else if(currentNode.IsRootCustomVariablesNode())
                {
                    GlueCommands.Self.DialogCommands.ShowAddNewVariableDialog();
                }
                else if(currentNode.IsRootStateNode())
                {
                    GlueCommands.Self.DialogCommands.ShowAddNewCategoryDialog();
                }
                else if(currentNode.IsStateCategoryNode())
                {
                    // show new state? That doesn't currently exist in the DialogCommands
                }
                else if(currentNode.IsRootEventsNode())
                {
                    GlueCommands.Self.DialogCommands.ShowAddNewEventDialog((NamedObjectSave)null);
                }
                else if(currentNode.IsRootEntityNode())
                {
                    GlueCommands.Self.DialogCommands.ShowAddNewEntityDialog();
                }
                else if(currentNode.IsRootScreenNode())
                {
                    GlueCommands.Self.DialogCommands.ShowAddNewScreenDialog();
                }
            }
            else if(await HotkeyManager.Self.TryHandleKeys(e, isTextBoxFocused:false))
            {
                e.Handled = true;
            }
        }

        public void FocusSearchBox()
        {
            SearchBar.FocusTextBox();
        }

        #endregion

        #region Drag+drop
        Point startPoint;
        NodeViewModel nodePushed;
        NodeViewModel nodeWaitingOnSelection;

        DateTime lastClick;
        private void MainTreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Normally tree nodes are selected
            // on a push, not click. However, this
            // default behavior would unload the current
            // editor level and show the new selection. This
            // is distracting, and we want to allow the user to
            // drag+drop entities into screens to create new instances
            // without having to deselect the room. To do this, we suppress
            // the default selected behavior by setting e.Handled=true down below.
            // Update - by suppressing the click, we also suppress the double-click.
            // to solve this, we keep track of how often a click happens and if it's faster
            // than .25 seconds, we manually call the DoubleClick event.
            if(e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed)
            {
                var timeSinceLastClick = DateTime.Now - lastClick;
                if(timeSinceLastClick.TotalSeconds < .25)
                {
                    MainTreeView_MouseDoubleClick(this, null);
                }
                lastClick = DateTime.Now;
                startPoint = e.GetPosition(null);
            }

            var objectPushed = e.OriginalSource;
            var frameworkElementPushed = (objectPushed as FrameworkElement);

            nodePushed = frameworkElementPushed?.DataContext as NodeViewModel;

            //MainTreeView.
            if (e.LeftButton == MouseButtonState.Pressed)
                (sender as ListBox).ReleaseMouseCapture();

            if(nodePushed != null && ClickedOnGrid(objectPushed as FrameworkElement))
            {
                nodeWaitingOnSelection = nodePushed;
                // don't select anything (yet)
                e.Handled = true;
            }
        }

        private void MainTreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed)
            {
                var timeSinceLastClick = DateTime.Now - lastClick;
                if (timeSinceLastClick.TotalSeconds < .25)
                {
                    MainTreeView_MouseDoubleClick(this, null);
                }
                lastClick = DateTime.Now;
                startPoint = e.GetPosition(null);
            }
            var objectPushed = e.OriginalSource;
            var frameworkElementPushed = (objectPushed as FrameworkElement);

            nodePushed = frameworkElementPushed?.DataContext as NodeViewModel;
        }

        private bool ClickedOnGrid(FrameworkElement frameworkElement)
        {
            if(frameworkElement.Name == "ItemGrid")
            {
                return true;
            }
            else
            {
                var parent = frameworkElement.Parent as FrameworkElement;
                if(parent == null)
                {
                    return false;
                }
                else
                {
                    return ClickedOnGrid(parent);
                }
            }
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

                GlueState.Self.DraggedTreeNode = nodePushed;
                // Get the dragged ListViewItem
                var vm = (e.OriginalSource as FrameworkElement).DataContext as NodeViewModel;

                if(vm != null)
                {
                    // Initialize the drag & drop operation
                    DataObject dragData = new DataObject("NodeViewModel", vm);
                    DragDrop.DoDragDrop(e.OriginalSource as DependencyObject, dragData, DragDropEffects.Move);
                }
            }
            if(!isMouseButtonPressed && nodePushed != null)
            {
                nodePushed = null;
            }
        }


        private void MainTreeView_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            nodePushed = null;
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

        private async void HandleDropTreeNodeOnTreeNode(DragEventArgs e)
        {
            // There's a bug in the tree view when dragging quickly, which can result in the wrong item dropped.
            // To solve this, we're going to use the NodePushed. For more info on the bug, see this:
            // https://github.com/vchelaru/FlatRedBall/issues/312
            //var objectDragged = e.Data.GetData("NodeViewModel");
            var targetNode = (e.OriginalSource as FrameworkElement).DataContext as NodeViewModel;

            if (nodePushed != null && targetNode != null)
            {
                //if (nodePushed.IsSelected == false)
                //{
                    // This addresses a bug in the tree view which can result in "rolling selection" as you grab
                    // and drag down the tree view quickly. It won't produce a bug anymore (see above) but this is just for visual confirmation.
                    // Update, we no longer select on a push anyway
                    //nodePushed.IsSelected = true;
                //}
                if (ButtonPressed == LeftOrRight.Left || targetNode == nodePushed)
                {
                    // do something here...
                    await DragDropManager.DragDropTreeNode(targetNode, nodePushed);
                    if (ButtonPressed == LeftOrRight.Right)
                    {
                        RightClickContextMenu.IsOpen = true;// test this
                    }
                }
                else
                {
                    SelectionLogic.SelectByTag(targetNode.Tag);

                    var items = RightClickHelper.GetRightClickItems(targetNode, MenuShowingAction.RightButtonDrag, nodePushed);


                    RightClickContextMenu.Items.Clear();

                    foreach (var item in items)
                    {
                        var wpfItem = CreateWpfItemFor(item);
                        RightClickContextMenu.Items.Add(wpfItem);
                    }
                    // Do this or it closes immediately
                    // 100 too fast
                    await System.Threading.Tasks.Task.Delay(300);
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

        #region Selection

        private void MainTreeView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if(nodeWaitingOnSelection != null)
            {
                nodeWaitingOnSelection.IsSelected = true;
                nodeWaitingOnSelection = null;
            }
        }

        // This makes the selection not happen on push+move as explained here:
        // https://stackoverflow.com/questions/2645265/wpf-listbox-click-and-drag-selects-other-items
        private void MainTreeView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                (sender as ListBox).ReleaseMouseCapture();
        }

        #endregion

        #region Double-click

        private void MainTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedNode = SelectionLogic.CurrentNode;
            GlueCommands.Self.TreeNodeCommands.HandleTreeNodeDoubleClicked(selectedNode);
        }

        #endregion

        #region Back/Forward navigation

        private void BackButtonClicked(object sender, RoutedEventArgs e)
        {
            TreeNodeStackManager.Self.GoBack();
        }

        private void NextButtonClicked(object sender, RoutedEventArgs e)
        {
            TreeNodeStackManager.Self.GoForward();
        }

        #endregion

        private void CollapseAllClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.CollapseAll();
        }

        private void CollapseToDefinitionsClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.CollapseToDefinitions();
        }

        #region Searching

        private void SearchBar_ClearSearchButtonClicked()
        {
            var whatWasSelected = SelectionLogic.CurrentNode?.Tag;

            ViewModel.SearchBoxText = string.Empty;
            ViewModel.ScreenRootNode.IsExpanded = false;
            ViewModel.EntityRootNode.IsExpanded = false;
            ViewModel.GlobalContentRootNode.IsExpanded = false;
            if (whatWasSelected != null)
            {
                SelectionLogic.SelectByTag(whatWasSelected);
                SelectionLogic.CurrentNode?.ExpandParentsRecursively();
            }
        }

        private void FlatList_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var objectPushed = e.OriginalSource;
            var frameworkElementPushed = (objectPushed as FrameworkElement);

            var searchNodePushed = frameworkElementPushed?.DataContext as NodeViewModel;
            SelectSearchNode(searchNodePushed);
        }

        private void SelectSearchNode(NodeViewModel searchNodePushed)
        {
            var foundSomething = true;
            if (searchNodePushed?.Tag is ScreenSave screenSave) GlueState.Self.CurrentScreenSave = screenSave;
            else if (searchNodePushed?.Tag is EntitySave entitySave) GlueState.Self.CurrentEntitySave = entitySave;
            else if (searchNodePushed?.Tag is ReferencedFileSave rfs) GlueState.Self.CurrentReferencedFileSave = rfs;
            else if (searchNodePushed?.Tag is NamedObjectSave nos) GlueState.Self.CurrentNamedObjectSave = nos;
            else if (searchNodePushed?.Tag is StateSaveCategory stateSaveCategory) GlueState.Self.CurrentStateSaveCategory = stateSaveCategory;
            else if (searchNodePushed?.Tag is StateSave stateSave) GlueState.Self.CurrentStateSave = stateSave;
            else if (searchNodePushed?.Tag is CustomVariable variable) GlueState.Self.CurrentCustomVariable = variable;
            else if (searchNodePushed?.Tag is EventResponseSave eventResponse) GlueState.Self.CurrentEventResponseSave = eventResponse;
            else foundSomething = false;

            if (foundSomething)
            {
                ViewModel.SearchBoxText = String.Empty;
            }
        }

        private void SearchBar_ArrowKeyPushed(Key key)
        {
            var selectedIndex = this.FlatList.SelectedIndex;
            if(key == Key.Up && selectedIndex > 0)
            {
                this.FlatList.SelectedIndex--;
                this.FlatList.ScrollIntoView(this.FlatList.SelectedItem);
            }
            else if(key == Key.Down && selectedIndex < FlatList.Items.Count-1)
            {
                this.FlatList.SelectedIndex++;
                this.FlatList.ScrollIntoView(this.FlatList.SelectedItem);
            }
        }
        private void SearchBar_EnterPressed()
        {
            if(FlatList.SelectedItem != null)
            {
                SelectSearchNode(FlatList.SelectedItem as NodeViewModel);
            }
        }


        #endregion

        private async void SearchBar_DismissHintTextClicked()
        {
            ViewModel.HasUserDismissedTips = true;
            await TaskManager.Self.AddAsync(() =>
            {
                if(GlueState.Self.GlueSettingsSave != null)
                {
                    GlueState.Self.GlueSettingsSave.Properties
                        .SetValue(nameof(ViewModel.HasUserDismissedTips), true);
                    GlueCommands.Self.GluxCommands.SaveSettings();
                }
            }, "Saving settings after dismissing tree view hint text");
        }
    }
}
