using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace OfficialPlugins.TreeViewPlugin.ViewModels
{
    class StatesRootNodeViewModel : NodeViewModel
    {
        private GlueElement glueElement;

        public StatesRootNodeViewModel(NodeViewModel parent, GlueElement glueElement) : base(FlatRedBall.Glue.FormHelpers.TreeNodeType.StateContainerNode, parent)
        {
            this.glueElement = glueElement;
        }

        public override void RefreshTreeNodes(TreeNodeRefreshType treeNodeRefreshType)
        {
            #region Update the states

            var parentTreeNode = this;

            UpdateStateContainingNode(glueElement.States, parentTreeNode);

            #endregion

            #region Update the StateSaveCategories

            foreach (StateSaveCategory stateSaveCategory in glueElement.StateCategoryList)
            {
                UpdateToStateCategory(stateSaveCategory);
            }

            for (int i = Children.Count - 1; i > -1; i--)
            {
                if (Children[i].Tag is StateSaveCategory category && !glueElement.StateCategoryList.Contains(category))
                {
                    Children.RemoveAt(i);
                }
            }


            #endregion

            Sort(this.Children);
        }

        private void UpdateStateContainingNode(List<StateSave> states, NodeViewModel parentTreeNode)
        {
            for(int stateIndex = 0; stateIndex < states.Count; stateIndex++)
            {
                var stateSave = states[stateIndex];
                var treeNode = GetTreeNodeFor(stateSave, parentTreeNode);

                if (treeNode == null)
                {
                    treeNode = new NodeViewModel(FlatRedBall.Glue.FormHelpers.TreeNodeType.StateNode, parentTreeNode);
                    treeNode.ImageSource = StateIcon;
                    treeNode.Text = stateSave.Name;
                    treeNode.Tag = stateSave;

                    //treeNode.ImageKey = "states.png";
                    //treeNode.SelectedImageKey = "states.png";

                    parentTreeNode.Children.Add(treeNode);
                }

                var indexOfTreeNode = parentTreeNode.Children.IndexOf(treeNode);
                if (indexOfTreeNode != -1 && indexOfTreeNode != stateIndex)
                {
                    parentTreeNode.Children.Move(indexOfTreeNode, stateIndex);
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

            for (int i = parentTreeNode.Children.Count - 1; i > -1; i--)
            {
                if (parentTreeNode.Children[i].Tag is StateSave state && !states.Contains(state))
                {
                    parentTreeNode.Children.RemoveAt(i);
                }
            }
        }

        public void UpdateToStateCategory(StateSaveCategory stateSaveCategory)
        {
            var treeNode = GetTreeNodeFor(stateSaveCategory);

            if (treeNode == null)
            {
                treeNode = new NodeViewModel(FlatRedBall.Glue.FormHelpers.TreeNodeType.StateCategoryNode, this);
                treeNode.Tag = stateSaveCategory;
                treeNode.Text = stateSaveCategory.Name;
                //treeNode.ForeColor = ElementViewWindow.StateCategoryColor;

                //treeNode.ImageKey = "folder.png";
                //treeNode.SelectedImageKey = "folder.png";

                this.Children.Add(treeNode);

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

        private void Sort(ObservableCollection<NodeViewModel> treeNodeCollection)
        {
            int lastObjectExclusive = treeNodeCollection.Count;
            int whereObjectBelongs;
            for (int i = 0 + 1; i < lastObjectExclusive; i++)
            {
                if (StateTreeNodeExtensionMethods.Compare(treeNodeCollection[i], treeNodeCollection[i - 1]) < 0)
                {
                    if (i == 1)
                    {
                        var treeNode = treeNodeCollection[i];
                        treeNodeCollection.RemoveAt(i);

                        treeNodeCollection.Insert(0, treeNode);
                        continue;
                    }

                    for (whereObjectBelongs = i - 2; whereObjectBelongs > -1; whereObjectBelongs--)
                    {
                        if (StateTreeNodeExtensionMethods.Compare(treeNodeCollection[i], treeNodeCollection[whereObjectBelongs]) >= 0)
                        {
                            var treeNode = treeNodeCollection[i];

                            treeNodeCollection.RemoveAt(i);
                            treeNodeCollection.Insert(whereObjectBelongs + 1, treeNode);
                            break;
                        }
                        else if (whereObjectBelongs == 0 &&
                            StateTreeNodeExtensionMethods.Compare(treeNodeCollection[i], treeNodeCollection[0]) < 0)
                        {
                            var treeNode = treeNodeCollection[i];
                            treeNodeCollection.RemoveAt(i);
                            treeNodeCollection.Insert(0, treeNode);
                            break;
                        }
                    }
                }
            }
        }


        NodeViewModel GetTreeNodeFor(StateSaveCategory category)
        {
            foreach (var treeNode in this.Children)
            {
                if (treeNode.Tag == category)
                {
                    return treeNode;
                }
            }

            return null;
        }

        NodeViewModel GetTreeNodeFor(StateSave stateSave, NodeViewModel parent)
        {
            foreach (NodeViewModel treeNode in parent.Children)
            {
                if (treeNode.Tag == stateSave)
                {
                    return treeNode;
                }
            }

            return null;
        }

    }

    public static class StateTreeNodeExtensionMethods
    {
        public static int Compare(NodeViewModel first, NodeViewModel second)
        {
            bool isFirstCategory = first.Tag is StateSaveCategory;
            bool isSecondCategory = second.Tag is StateSaveCategory;

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
