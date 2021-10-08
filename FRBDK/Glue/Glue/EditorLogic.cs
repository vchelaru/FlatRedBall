using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using Glue;
using System.Windows.Forms;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GlueFormsCore.Plugins.EmbeddedPlugins.ExplorerTabPlugin;

namespace FlatRedBall.Glue
{
    public static class EditorLogicSnapshot
    {
        public static BaseElementTreeNode CurrentElementTreeNode;
        public static IElement CurrentElement;
        public static TreeNode CurrentTreeNode;
        public static StateSave CurrentState;
        public static NamedObjectSave CurrentNamedObject;
    }

    public static class EditorLogic
    {
        public static EventResponseSave CurrentEventResponseSave
        {
            get
            {
                //This is needed because of designer issues.
                if (MainGlueWindow.Self == null) return null;
                
                TreeNode treeNode = MainExplorerPlugin.Self.ElementTreeView.SelectedNode;

                if (treeNode == null)
                {
                    return null;
                }
                else if (treeNode.Tag != null && treeNode.Tag is EventResponseSave)
                {
                    return (EventResponseSave)treeNode.Tag;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                TreeNode treeNode = GlueState.Self.Find.EventResponseTreeNode(value);

                ElementViewWindow.SelectedNode = treeNode;

            }
        }

        public static IEventContainer CurrentEventContainer
        {
            get
            {
                if (GlueState.Self.CurrentScreenSave != null)
                {
                    return GlueState.Self.CurrentScreenSave;
                }
                else if (GlueState.Self.CurrentEntitySave != null)
                {
                    return GlueState.Self.CurrentEntitySave;
                }

                return null;

            }
        }

        public static CustomVariable CurrentCustomVariable
        {
            get
            {
                TreeNode treeNode = CurrentTreeNode;

                if (treeNode == null)
                {
                    return null;
                }
                else if (treeNode.IsCustomVariable())
                {
                    return (CustomVariable)treeNode.Tag;
                }
                else
                {
                    return null;
                }

            }

            set
            {
                CurrentTreeNode = GlueState.Self.Find.CustomVariableTreeNode(value);

            }

        }

        public static ReferencedFileSave CurrentReferencedFile
        {
            get
            {
                TreeNode treeNode = MainExplorerPlugin.Self.ElementTreeView.SelectedNode;

                if (treeNode != null && treeNode.Tag != null && treeNode.Tag is ReferencedFileSave)
                {
                    return (ReferencedFileSave)treeNode.Tag;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                CurrentTreeNode = GlueState.Self.Find.ReferencedFileSaveTreeNode(value);
            }
        }

        public static BaseElementTreeNode CurrentElementTreeNode => GetCurrentElementTreeNodeFromSelection();
        private static BaseElementTreeNode GetCurrentElementTreeNodeFromSelection()
        {
            TreeNode treeNode = MainExplorerPlugin.Self.ElementTreeView.SelectedNode;

            while (treeNode != null)
            {
                if (treeNode is BaseElementTreeNode)
                {
                    return ((BaseElementTreeNode)treeNode);
                }
                else
                {
                    treeNode = treeNode.Parent;
                }
            }
            return null;
        }

        public static TreeNode CurrentTreeNode
		{
			get => GetCurrentTreeNodeFromSelection();
            set
            {
                MainExplorerPlugin.Self.ElementTreeView.SelectedNode = value;
            }
		}
        private static TreeNode GetCurrentTreeNodeFromSelection()
        {
            return MainExplorerPlugin.Self.ElementTreeView.SelectedNode;
        }

        [Obsolete("Use GlueState")]
        public static StateSave CurrentStateSave => GetCurrentStateSaveFromSelection();
        static StateSave GetCurrentStateSaveFromSelection()
        {
            TreeNode treeNode = CurrentTreeNode;

            if (treeNode != null && treeNode.IsStateNode())
            {
                return (StateSave)treeNode.Tag;
            }

            return null;
        }

        public static StateSaveCategory CurrentStateSaveCategory
        {
            get
            {
                TreeNode treeNode = CurrentTreeNode;

                if (treeNode != null)
                {
                    if (treeNode.IsStateCategoryNode())
                    {
                        return (StateSaveCategory)treeNode.Tag;
                    }
                    // if the current node is a state, maybe the parent is a category
                    else if(treeNode.Parent != null && treeNode.Parent.IsStateCategoryNode())
                    {
                        return (StateSaveCategory)treeNode.Parent.Tag;
                    }
                }

                return null;
            }
        }
        
    }
}
