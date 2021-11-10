using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.TreeViewPlugin.ViewModels;
using OfficialPlugins.TreeViewPlugin.Views;
using System;
using System.Collections.Generic;
using System.Text;

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
            else if(tag is EntitySave entitySave)
            {
                GlueState.Self.CurrentEntitySave = entitySave;
            }
            else if(tag is ScreenSave screenSave)
            {
                GlueState.Self.CurrentScreenSave = screenSave;
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
