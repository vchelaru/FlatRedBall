using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.TreeViewPlugin.ViewModels;
using OfficialPlugins.TreeViewPlugin.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OfficialPlugins.TreeViewPlugin.Logic
{
    internal static class SelectionLogic
    {
        #region Fields/Properties

        static MainTreeViewViewModel mainViewModel;
        static MainTreeViewControl mainView;

        static List<NodeViewModel> currentNodes = new List<NodeViewModel>();

        public static bool IsUpdatingThisSelectionOnGlueEvent = true;
        public static bool IsPushingSelectionOutToGlue = true;

        public static NodeViewModel CurrentNode
        {
            get => currentNodes.FirstOrDefault();
        }

        public static NamedObjectSave CurrentNamedObjectSave
        {
            set => _=SelectByTag(value, false);
        }

        public static ReferencedFileSave CurrentReferencedFileSave
        {
            set => _ = SelectByTag(value, false);
        }

        public static CustomVariable CurrentCustomVariable
        {
            set => _ = SelectByTag(value, false);
        }

        public static EventResponseSave CurrentEventResponseSave
        {
            set => _ = SelectByTag(value, false);
        }

        public static StateSave CurrentStateSave
        {
            set => _ = SelectByTag(value, false);
        }

        public static StateSaveCategory CurrentStateSaveCategory
        {
            set => _ = SelectByTag(value, false);
        }

        public static EntitySave CurrentEntitySave
        {
            set => _ = SelectByTag(value, false);
        }

        public static ScreenSave CurrentScreenSave
        {
            set => _ = SelectByTag(value, false);
        }

        #endregion

        public static void HandleDeselection(NodeViewModel nodeViewModel)
        {
            if (currentNodes.Contains(nodeViewModel))
            {
                currentNodes.Remove(nodeViewModel);
            }

            RefreshGlueState(false);
        }

        public static void HandleSelected(NodeViewModel nodeViewModel, bool focus, bool replaceSelection)
        {
            IsUpdatingThisSelectionOnGlueEvent = false;

            var newTag = nodeViewModel.Tag;

            bool didSelectionChange;
            if (currentNodes?.Contains(nodeViewModel) == true)
            {
                didSelectionChange = false;
            }
            // Someone could change from a node without a tag to a different node without a tag,
            // so base it on the nodeViewModel
            //else if (currentNodes.Count == 0 && newTag == null)
            else if (currentNodes.Count == 0 && nodeViewModel == null)
            {
                didSelectionChange = false;
            }
            else if (currentNodes.Count > 0 && nodeViewModel == null)
            {
                didSelectionChange = true;
            }
            else if (currentNodes.Count == 0 && nodeViewModel != null)
            {
                didSelectionChange = true;
            }
            else
            {
                didSelectionChange = currentNodes.Any(item => item.Tag == nodeViewModel.Tag) == false;

                if(!didSelectionChange && replaceSelection)
                {
                    didSelectionChange = currentNodes.Contains(nodeViewModel) == false;
                }
            }


            if(replaceSelection)
            {
                currentNodes.Clear();

                mainViewModel.DeselectResursively(callSelectionLogic: false);

            }


            if (nodeViewModel != null)
            {
                currentNodes.Add(nodeViewModel);
                nodeViewModel.SetSelectNoSelectionLogic(true);
            }

            if (nodeViewModel != null && nodeViewModel.IsSelected && focus)
            {
                nodeViewModel.Focus(mainView);
            }

            RefreshGlueState(didSelectionChange);

            IsUpdatingThisSelectionOnGlueEvent = true;
        }

        private static void RefreshGlueState(bool forcePushToGlue)
        {
            if (IsPushingSelectionOutToGlue
                // The node can change if the user deletes a tree node and then a new one
                // automatically gets re-selected. In this case, we do still want to push the selection out.
                || forcePushToGlue)
            {
                GlueState.Self.CurrentTreeNodes = currentNodes;
            }

            // We used to refresh here on a normal click. This is unnecessary
            // since most of the time the right-click menu isn't accessed. Moved this to preview
            // right click in TMainTreeviewControl.xaml.cs
            //RefreshRightClickMenu();
            // Update April 16, 2023
            // We should assign this because if the user directly right-clicks on a new node,
            // we want this to get called

            mainView.RefreshRightClickMenu();
        }

        internal static async Task SelectByPath(string path, bool addToSelection)
        {
            var treeNode = mainViewModel.GetTreeNodeByRelativePath(path);
            await SelectByTreeNode(treeNode, addToSelection);
        }

        public static async Task SelectByTag(object value, bool addToSelection)
        {
            NodeViewModel treeNode = value == null ? null : mainViewModel.GetTreeNodeByTag(value);

            await SelectByTreeNode(treeNode, addToSelection);

        }

        public static bool SuppressFocus = false;

        public static async Task SelectByTreeNode(NodeViewModel treeNode, bool addToSelection, bool selectAndScroll = true)
        {
            // record the value here since we delay on this method
            var suppressFocusCopy = SuppressFocus;
            if (treeNode == null)
            {
                if (currentNodes.Count > 0 && !addToSelection)
                {
                    SelectionLogic.IsUpdatingThisSelectionOnGlueEvent = false;

                    mainViewModel.DeselectResursively(true);
                    //currentNode.IsSelected = false;
                    currentNodes.Clear();

                    SelectionLogic.IsUpdatingThisSelectionOnGlueEvent = true;
                }
            }
            else
            {
                if (treeNode != null && (treeNode.IsSelected == false || currentNodes.Contains(treeNode) == false))
                {
                    if(CurrentNode?.IsSelected == false && !addToSelection)
                    {
                        mainViewModel.DeselectResursively(true);
                        // Selecting a tree node deselects the current node, but that can take some time and cause
                        // some inconsistent behavior. To solve this, we will forcefully deselect the current node 
                        // so the consequence of selecting this node is immediate:
                        foreach(var node in currentNodes)
                        {
                            node.IsSelected = false;
                        }
                        // do we null out currentNode
                    }
                    if(suppressFocusCopy)
                    {
                        treeNode.SetSelectNoSelectionLogic(true);
                        if(addToSelection)
                        {
                            if(currentNodes.Contains(treeNode) == false)
                            {
                                currentNodes.Add(treeNode);
                            }
                        }
                        else
                        {
                            currentNodes.Clear();
                            currentNodes.Add(treeNode);
                        }
                    }
                    else
                    {
                        treeNode.IsSelected = true;
                    }

                    if(selectAndScroll)
                    {
                        treeNode.ExpandParentsRecursively();
                    }
                }

                if (selectAndScroll)
                {
                    // If we don't do this, sometimes it doesn't scroll into view...
                    await System.Threading.Tasks.Task.Delay(120);

                    mainView.MainTreeView.UpdateLayout();

                    mainView.MainTreeView.ScrollIntoView(treeNode);

                    // Do this after the delay
                    if(treeNode?.IsSelected == true && !suppressFocusCopy)
                    {
                        treeNode.Focus(mainView);
                    }
                }
            }
        }

        public static void Initialize(MainTreeViewViewModel mainViewModel, MainTreeViewControl mainView)
        {
            SelectionLogic.mainViewModel = mainViewModel;
            SelectionLogic.mainView = mainView;
        }
    }
}
