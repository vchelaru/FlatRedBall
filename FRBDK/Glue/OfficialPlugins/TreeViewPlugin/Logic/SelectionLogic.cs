using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.TreeViewPlugin.ViewModels;
using OfficialPlugins.TreeViewPlugin.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OfficialPlugins.TreeViewPlugin.Logic;

internal class SelectionLogic
{
    #region Fields/Properties

    public static SelectionLogic Current { get; private set; }

    MainTreeViewViewModel mainViewModel;
    ITreeViewDisplay mainView;
    readonly Action<IReadOnlyList<ITreeNode>> _reportSelection;

    List<NodeViewModel> currentNodes = new List<NodeViewModel>();

    public bool IsUpdatingThisSelectionOnGlueEvent = true;
    public bool IsPushingSelectionOutToGlue = true;
    public bool SuppressFocus = false;

    public NodeViewModel CurrentNode
    {
        get => currentNodes.FirstOrDefault();
    }

    public NamedObjectSave CurrentNamedObjectSave
    {
        set => _ = SelectByTag(value, false);
    }

    public ReferencedFileSave CurrentReferencedFileSave
    {
        set => _ = SelectByTag(value, false);
    }

    public CustomVariable CurrentCustomVariable
    {
        set => _ = SelectByTag(value, false);
    }

    public EventResponseSave CurrentEventResponseSave
    {
        set => _ = SelectByTag(value, false);
    }

    public StateSave CurrentStateSave
    {
        set => _ = SelectByTag(value, false);
    }

    public StateSaveCategory CurrentStateSaveCategory
    {
        set => _ = SelectByTag(value, false);
    }

    public EntitySave CurrentEntitySave
    {
        set => _ = SelectByTag(value, false);
    }

    public ScreenSave CurrentScreenSave
    {
        set => _ = SelectByTag(value, false);
    }

    #endregion

    public SelectionLogic(MainTreeViewViewModel mainViewModel, ITreeViewDisplay mainView,
        Action<IReadOnlyList<ITreeNode>> reportSelection)
    {
        this.mainViewModel = mainViewModel;
        this.mainView = mainView;
        _reportSelection = reportSelection;
        Current = this;
        NodeViewModel.NodeSelected = (node, focus, replace) => HandleSelected(node, focus, replace);
        NodeViewModel.NodeDeselected = HandleDeselection;
    }

    public void HandleDeselection(NodeViewModel nodeViewModel)
    {
        if (currentNodes.Contains(nodeViewModel))
        {
            currentNodes.Remove(nodeViewModel);
        }

        RefreshGlueState(false);
    }

    public void HandleSelected(NodeViewModel nodeViewModel, bool focus, bool replaceSelection)
    {
        IsUpdatingThisSelectionOnGlueEvent = false;

        var newTag = nodeViewModel?.Tag;

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

            if (!didSelectionChange && replaceSelection)
            {
                didSelectionChange = currentNodes.Contains(nodeViewModel) == false;
            }
        }


        if (replaceSelection)
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
            mainView.FocusNode(nodeViewModel);
        }

        RefreshGlueState(didSelectionChange);

        IsUpdatingThisSelectionOnGlueEvent = true;
    }

    private void RefreshGlueState(bool forcePushToGlue)
    {
        if (IsPushingSelectionOutToGlue
            // The node can change if the user deletes a tree node and then a new one
            // automatically gets re-selected. In this case, we do still want to push the selection out.
            || forcePushToGlue)
        {
            _reportSelection(currentNodes);
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

    internal async Task SelectByPath(string path, bool addToSelection)
    {
        var treeNode = mainViewModel.GetTreeNodeByRelativePath(path);
        await SelectByTreeNode(treeNode, addToSelection);
    }

    public async Task SelectByTag(object value, bool addToSelection)
    {
        NodeViewModel treeNode = value == null ? null : mainViewModel.GetTreeNodeByTag(value);

        await SelectByTreeNode(treeNode, addToSelection);

    }

    public async Task SelectByTreeNode(NodeViewModel treeNode, bool addToSelection, bool selectAndScroll = true)
    {
        // record the value here since we delay on this method
        var suppressFocusCopy = SuppressFocus;
        if (treeNode == null)
        {
            if (currentNodes.Count > 0 && !addToSelection)
            {
                IsUpdatingThisSelectionOnGlueEvent = false;

                mainViewModel.DeselectResursively(true);
                //currentNode.IsSelected = false;
                currentNodes.Clear();

                IsUpdatingThisSelectionOnGlueEvent = true;
            }
        }
        else
        {
            if (treeNode != null && (treeNode.IsSelected == false || currentNodes.Contains(treeNode) == false))
            {
                if (CurrentNode?.IsSelected == false && !addToSelection)
                {
                    mainViewModel.DeselectResursively(true);
                    // Selecting a tree node deselects the current node, but that can take some time and cause
                    // some inconsistent behavior. To solve this, we will forcefully deselect the current node
                    // so the consequence of selecting this node is immediate:
                    foreach (var node in currentNodes)
                    {
                        node.IsSelected = false;
                    }
                    // do we null out currentNode
                }
                if (suppressFocusCopy)
                {
                    treeNode.SetSelectNoSelectionLogic(true);
                    if (addToSelection)
                    {
                        if (currentNodes.Contains(treeNode) == false)
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

                if (selectAndScroll)
                {
                    treeNode.ExpandParentsRecursively();
                }
            }

            if (selectAndScroll)
            {
                // If we don't do this, sometimes it doesn't scroll into view...
                await System.Threading.Tasks.Task.Delay(120);

                mainView.UpdateTreeViewLayout();

                mainView.ScrollNodeIntoView(treeNode);

                // Do this after the delay
                if (treeNode?.IsSelected == true && !suppressFocusCopy)
                {
                    mainView.FocusNode(treeNode);
                }
            }
        }
    }
}
