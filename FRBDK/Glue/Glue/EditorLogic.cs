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
        public static void TakeSnapshot()
        {
            MainGlueWindow.Self.Invoke((MethodInvoker)delegate
            {
                EditorLogicSnapshot.CurrentElementTreeNode = CurrentElementTreeNode;
                EditorLogicSnapshot.CurrentState = CurrentStateSave;
                EditorLogicSnapshot.CurrentTreeNode = CurrentTreeNode;
                EditorLogicSnapshot.CurrentNamedObject = CurrentNamedObject;
                EditorLogicSnapshot.CurrentElement = CurrentElement;
            });
        }

        public static EventResponseSave CurrentEventResponseSave
        {
            get
            {
                //This is needed because of designer issues.
                if (MainGlueWindow.Self == null) return null;
                
                TreeNode treeNode = MainGlueWindow.Self.ElementTreeView.SelectedNode;

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
                if (CurrentScreenSave != null)
                {
                    return CurrentScreenSave;
                }
                else if (CurrentEntitySave != null)
                {
                    return CurrentEntitySave;
                }

                return null;

            }
        }

        [Obsolete("Use GlueState")]
        public static NamedObjectSave CurrentNamedObject
        {
            get
            {
                return GlueState.Self.CurrentNamedObjectSave;
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
                TreeNode treeNode = MainGlueWindow.Self.ElementTreeView.SelectedNode;

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

        [Obsolete("Use GlueState.CurrentElement")]
		public static IElement CurrentElement
		{
			get
			{
				if (CurrentEntitySave != null)
				{
					return CurrentEntitySave;
				}
				else if (CurrentScreenSave != null)
				{
					return CurrentScreenSave;
				}
				else
				{
					return null;
				}
			}
            set
            {
                var treeNode = GlueState.Self.Find.ElementTreeNode(value);

                CurrentTreeNode = treeNode;

            }
		}

        public static EntitySave CurrentEntitySave
        {
            get
            {
#if UNIT_TESTS
                return null;
#endif
                TreeNode treeNode = MainGlueWindow.Self.ElementTreeView.SelectedNode;

                while (treeNode != null)
                {
                    if (treeNode is EntityTreeNode)
                    {
                        return ((EntityTreeNode)treeNode).EntitySave;
                    }
                    else
                    {
                        treeNode = treeNode.Parent;
                    }
                }

                return null;
            }

        }

        public static ScreenSave CurrentScreenSave
        {
            get
            {
#if UNIT_TESTS
                return null;
#endif
                TreeNode treeNode = MainGlueWindow.Self.ElementTreeView.SelectedNode;

                while (treeNode != null)
                {
                    if (treeNode is BaseElementTreeNode && ((BaseElementTreeNode)treeNode).SaveObject is ScreenSave)
                    {
                        return ((BaseElementTreeNode)treeNode).SaveObject as ScreenSave;
                    }
                    else
                    {
                        treeNode = treeNode.Parent;
                    }
                }

                return null;
            }
            set
            {
                MainGlueWindow.Self.ElementTreeView.SelectedNode =
                    GlueState.Self.Find.ScreenTreeNode(value);
            }
        }

        public static ScreenTreeNode CurrentScreenTreeNode
        {
            get
            {
                TreeNode treeNode = MainGlueWindow.Self.ElementTreeView.SelectedNode;

                while (treeNode != null)
                {
                    if (treeNode is ScreenTreeNode)
                    {
                        return ((ScreenTreeNode)treeNode);
                    }
                    else
                    {
                        treeNode = treeNode.Parent;
                    }
                }

                return null;
            }
        }

        public static EntityTreeNode CurrentEntityTreeNode
        {
            get
            {
                TreeNode treeNode = MainGlueWindow.Self.ElementTreeView.SelectedNode;

                while (treeNode != null)
                {
                    if (treeNode is EntityTreeNode)
                    {
                        return ((EntityTreeNode)treeNode);
                    }
                    else
                    {
                        treeNode = treeNode.Parent;
                    }
                }
                return null;
            }
        }

        public static BaseElementTreeNode CurrentElementTreeNode
        {
            get
            {
                TreeNode treeNode = MainGlueWindow.Self.ElementTreeView.SelectedNode;

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
        }

		public static TreeNode CurrentTreeNode
		{
			get 
            { 
                return MainGlueWindow.Self.ElementTreeView.SelectedNode; 
            }
            set
            {
                MainGlueWindow.Self.ElementTreeView.SelectedNode = value;
            }
		}

        public static string CurrentCodeFile
        {
            get
            {
                TreeNode treeNode = MainGlueWindow.Self.ElementTreeView.SelectedNode;
                {
                    if (treeNode != null && treeNode.Text.EndsWith(".cs"))
                    {
                        return treeNode.Text;
                    }
                    else
                    {
                        return "";
                    }
                }
            }
        }


        [Obsolete("Use GlueState")]
        public static StateSave CurrentStateSave
        {
            get
            {
                TreeNode treeNode = CurrentTreeNode;

                if (treeNode != null && treeNode.IsStateNode())
                {
                    return (StateSave)treeNode.Tag;
                }

                return null;
            }
        }

        public static StateSaveCategory CurrentStateSaveCategory
        {
            get
            {
                TreeNode treeNode = CurrentTreeNode;

                if (treeNode != null && treeNode.IsStateCategoryNode())
                {
                    return (StateSaveCategory)treeNode.Tag;
                }

                return null;
            }
        }
        
    }
}
