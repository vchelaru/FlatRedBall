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
        static MainTreeViewViewModel mainViewModel;
        static MainTreeViewControl mainView;

        static NodeViewModel currentNode;

        public static void HandleSelected(NodeViewModel nodeViewModel)
        {
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
                var element = ((ITreeNode)nodeViewModel).GetContainingElementTreeNode()?.Tag;

                // When a folder node is selected, this should set the Current 
                // screen or entity. But doing so results in that tree node being selected
                // and that causes the right click to show the wrong UI. 
                // To fix this, selecting a folder node here should select
                // a folder node in the tree view too....or should we separate
                // from using the tree view as the current object?
                Need to think on this
                if (element is EntitySave)
                {
                    CurrentEntitySave = element as EntitySave;

                }
                else if(element is ScreenSave)
                {
                    CurrentScreenSave = element as ScreenSave;
                }
            }

            RefreshRightClickMenu();
        }

        private static void RefreshRightClickMenu()
        {
            var items = RightClickHelper.GetRightClickItems(currentNode, MenuShowingAction.RegularRightClick);

            mainView.RightClickContextMenu.Items.Clear();

            foreach (var item in items)
            {
                var wpfItem = CreateWpfItemFor(item);
                mainView.RightClickContextMenu.Items.Add(wpfItem);
            }
        }

        private static object CreateWpfItemFor(GlueFormsCore.FormHelpers.GeneralToolStripMenuItem item)
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
                menuItem.Click += (not, used) => item.Click(menuItem, null);

                foreach(var child in item.DropDownItems)
                {
                    var wpfItem = CreateWpfItemFor(child);
                    menuItem.Items.Add(wpfItem);
                }

                return menuItem;
            }
        }

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

        public static void SelectByTag(object value)
        {
            var treeNode = mainViewModel.GetTreeNodeByTag(value);

            if (treeNode != null && treeNode != currentNode)
            {
                treeNode.ExpandParentsRecursively();
                treeNode.IsSelected = true;
                mainView.MainTreeView.ScrollIntoView(treeNode);
            }


        }


        public static void Initialize(MainTreeViewViewModel mainViewModel, MainTreeViewControl mainView)
        {
            SelectionLogic.mainViewModel = mainViewModel;
            SelectionLogic.mainView = mainView;
        }
    }
}
