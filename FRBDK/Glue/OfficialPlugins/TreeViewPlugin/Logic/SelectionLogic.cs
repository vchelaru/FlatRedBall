using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.TreeViewPlugin.ViewModels;
using OfficialPlugins.TreeViewPlugin.Views;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;

namespace OfficialPlugins.TreeViewPlugin.Logic
{
    internal static class SelectionLogic
    {
        #region Fields/Properties

        static MainTreeViewViewModel mainViewModel;
        static MainTreeViewControl mainView;

        static NodeViewModel currentNode;

        public static bool IsUpdatingSelectionOnGlueEvent = true;

        public static NodeViewModel CurrentNode
        {
            get => currentNode;
        }

        public static NamedObjectSave CurrentNamedObjectSave
        {
            set => SelectByTag(value);
        }

        public static ReferencedFileSave CurrentReferencedFileSave
        {
            set => SelectByTag(value);
        }

        public static CustomVariable CurrentCustomVariable
        {
            set => SelectByTag(value);
        }

        public static EventResponseSave CurrentEventResponseSave
        {
            set => SelectByTag(value);
        }

        public static StateSave CurrentStateSave
        {
            set => SelectByTag(value);
        }

        public static StateSaveCategory CurrentStateSaveCategory
        {
            set => SelectByTag(value);
        }

        public static EntitySave CurrentEntitySave
        {
            set => SelectByTag(value);
        }

        public static ScreenSave CurrentScreenSave
        {
            set => SelectByTag(value);
        }

        #endregion

        public static void HandleSelected(NodeViewModel nodeViewModel)
        {
            IsUpdatingSelectionOnGlueEvent = false;

            currentNode = nodeViewModel;

            var tag = nodeViewModel.Tag;

            if (tag is NamedObjectSave nos)
            {
                GlueState.Self.CurrentNamedObjectSave = nos;
            }
            else if (tag is ReferencedFileSave rfs)
            {
                GlueState.Self.CurrentReferencedFileSave = rfs;
            }
            else if (tag is CustomVariable variable)
            {
                GlueState.Self.CurrentCustomVariable = variable;
            }
            else if (tag is EventResponseSave eventResponse)
            {
                GlueState.Self.CurrentEventResponseSave = eventResponse;
            }
            else if (tag is StateSave state)
            {
                GlueState.Self.CurrentStateSave = state;
            }
            else if (tag is StateSaveCategory stateCategory)
            {
                GlueState.Self.CurrentStateSaveCategory = stateCategory;
            }
            else if (tag is EntitySave entitySave)
            {
                GlueState.Self.CurrentEntitySave = entitySave;
            }
            else if (tag is ScreenSave screenSave)
            {
                GlueState.Self.CurrentScreenSave = screenSave;
            }
            else if(tag == null)
            {
                //var element = ((ITreeNode)nodeViewModel).GetContainingElementTreeNode()?.Tag;

                //if (element is EntitySave)
                //{
                //    GlueState.Self.CurrentEntitySave = element as EntitySave;

                //}
                //else if(element is ScreenSave)
                //{
                //    GlueState.Self.CurrentScreenSave = element as ScreenSave;
                //}
                // cheating, this will eventually go away:
                ElementViewWindow.SelectByRelativePath((nodeViewModel as ITreeNode).GetRelativePath());
            }

            RefreshRightClickMenu();

            IsUpdatingSelectionOnGlueEvent = true;

        }

        private static void RefreshRightClickMenu()
        {
            var items = RightClickHelper.GetRightClickItems(currentNode, MenuShowingAction.RegularRightClick);

            mainView.RightClickContextMenu.Items.Clear();

            foreach (var item in items)
            {
                var wpfItem = mainView.CreateWpfItemFor(item);
                mainView.RightClickContextMenu.Items.Add(wpfItem);
            }
        }




        public static async void SelectByTag(object value)
        {
            if(value == null)
            {
                if(currentNode != null)
                {
                    SelectionLogic.IsUpdatingSelectionOnGlueEvent = false;
                    currentNode.IsSelected = false;
                    currentNode = null;
                    SelectionLogic.IsUpdatingSelectionOnGlueEvent = true;
                }
            }
            else
            {
                var treeNode = mainViewModel.GetTreeNodeByTag(value);

                if (treeNode != null && (treeNode.IsSelected == false || treeNode != currentNode))
                {
                    treeNode.IsSelected = true;
                    treeNode.ExpandParentsRecursively();
                    // If we don't do this, sometimes it doesn't scroll into view...
                    await System.Threading.Tasks.Task.Delay(100);
                    mainView.MainTreeView.ScrollIntoView(treeNode);
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
