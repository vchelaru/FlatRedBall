using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.FormHelpers;

namespace FlatRedBall.Glue.Controls
{
    public class StateListTreeNode : TreeNode
    {
        public StateListTreeNode(string text)
            : base(text)
        {

        }

        TreeNode GetTreeNodeFor(StateSaveCategory category)
        {
            foreach (TreeNode treeNode in this.Nodes)
            {
                if (treeNode.Tag == category)
                {
                    return treeNode;
                }
            }

            return null;
        }

        TreeNode GetTreeNodeFor(StateSave stateSave, TreeNode parent)
        {
            foreach (TreeNode treeNode in parent.Nodes)
            {
                if (treeNode.Tag == stateSave)
                {
                    return treeNode;
                }
            }

            return null;
        }

        public void UpdateToStates(List<StateSave> states, List<StateSaveCategory> stateCategoryList)
        {
            #region Update the states

            TreeNode parentTreeNode = this;

            UpdateStateContainingNode(states, parentTreeNode);

            #endregion

            #region Update the StateSaveCategories

            foreach (StateSaveCategory stateSaveCategory in stateCategoryList)
            {
                TreeNode treeNode = GetTreeNodeFor(stateSaveCategory);

                if (treeNode == null)
                {
                    treeNode = new TreeNode(stateSaveCategory.Name);
                    treeNode.ForeColor = ElementViewWindow.StateCategoryColor;

                    if (BaseElementTreeNode.UseIcons)
                    {
                        treeNode.ImageKey = "folder.png";
                        treeNode.SelectedImageKey = "folder.png";
                    }
                    this.Nodes.Add(treeNode);
                    
                }

                if (treeNode.Text != stateSaveCategory.Name)
                {
                    treeNode.Text = stateSaveCategory.Name;
                }
                if (treeNode.Tag != stateSaveCategory)
                {
                    treeNode.Tag = stateSaveCategory;
                }

                UpdateStateContainingNode(stateSaveCategory.States, treeNode);
            }

            for (int i = Nodes.Count - 1; i > -1; i--)
            {
                if (Nodes[i].Tag is StateSaveCategory && !stateCategoryList.Contains(Nodes[i].Tag))
                {
                    Nodes.RemoveAt(i);
                }
            }


            #endregion

            Sort(this.Nodes);
        }

        private void Sort(TreeNodeCollection treeNodeCollection)
        {
            int lastObjectExclusive = treeNodeCollection.Count;
            int whereObjectBelongs;
            for (int i = 0 + 1; i < lastObjectExclusive; i++)
            {
                if (StateTreeNodeExtensionMethods.Compare(treeNodeCollection[i], treeNodeCollection[i - 1]) < 0)
                {
                    if (i == 1)
                    {
                        TreeNode treeNode = treeNodeCollection[i];
                        treeNodeCollection.RemoveAt(i);

                        treeNodeCollection.Insert(0, treeNode);
                        continue;
                    }

                    for (whereObjectBelongs = i - 2; whereObjectBelongs > -1; whereObjectBelongs--)
                    {
                        if (StateTreeNodeExtensionMethods.Compare(treeNodeCollection[i], treeNodeCollection[whereObjectBelongs]) >= 0)
                        {
                            TreeNode treeNode = treeNodeCollection[i];

                            treeNodeCollection.RemoveAt(i);
                            treeNodeCollection.Insert(whereObjectBelongs + 1, treeNode);
                            break;
                        }
                        else if (whereObjectBelongs == 0 && 
                            StateTreeNodeExtensionMethods.Compare(treeNodeCollection[i], treeNodeCollection[0]) < 0)
                        {
                            TreeNode treeNode = treeNodeCollection[i];
                            treeNodeCollection.RemoveAt(i);
                            treeNodeCollection.Insert(0, treeNode);
                            break;
                        }
                    }
                }
            }
        }

        private void UpdateStateContainingNode(List<StateSave> states, TreeNode parentTreeNode)
        {
            foreach (StateSave stateSave in states)
            {
                TreeNode treeNode = GetTreeNodeFor(stateSave, parentTreeNode);

                if (treeNode == null)
                {
                    treeNode = new TreeNode(stateSave.Name);

                    if (BaseElementTreeNode.UseIcons)
                    {
                        treeNode.ImageKey = "states.png";
                        treeNode.SelectedImageKey = "states.png";
                    }


                    parentTreeNode.Nodes.Add(treeNode);
                }

                if (treeNode.Text != stateSave.Name)
                {
                    treeNode.Text = stateSave.Name;
                }
                if (treeNode.Tag != stateSave)
                {
                    treeNode.Tag = stateSave;
                }

            }

            for (int i = parentTreeNode.Nodes.Count - 1; i > -1; i--)
            {
                if (parentTreeNode.Nodes[i].Tag is StateSave && !states.Contains(parentTreeNode.Nodes[i].Tag))
                {
                    parentTreeNode.Nodes.RemoveAt(i);
                }
            }
        }


    }


    public static class StateTreeNodeExtensionMethods
    {
        public static int Compare(TreeNode first, TreeNode second)
        {
            bool isFirstCategory = first.IsStateCategoryNode();
            bool isSecondCategory = second.IsStateCategoryNode();

            if (isFirstCategory && !isSecondCategory)
            {
                return -1;
            }
            else if (isSecondCategory && !isFirstCategory)
            {
                return 1;
            }
            else
            {
                return first.Text.CompareTo(second.Text);
            }


        }


    }
}
