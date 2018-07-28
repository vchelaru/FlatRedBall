using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace FlatRedBall.Forms.Controls
{
    public class TreeView : ScrollViewer
    {
        #region Fields/Properties

        TreeViewLogic treeViewLogic = new TreeViewLogic();

        public Type TreeViewItemGumType
        {
            get { return treeViewLogic.TreeViewItemGumType; }
            set { treeViewLogic.TreeViewItemGumType = value; }
        }
        public Type TreeViewItemFormsType
        {
            get { return treeViewLogic.TreeViewItemFormsType; }
            set { treeViewLogic.TreeViewItemFormsType = value; }
        }

        public ObservableCollection<object> Items
        {
            get { return treeViewLogic.Items; }
        }

        public object SelectedObject
        {
            get { return treeViewLogic.SelectedObject; }
            //set { treeViewLogic.SelectedObject = value; }
        }

        public TreeViewItem SelectedItem
        {
            get
            {
                return treeViewLogic.SelectedItem;
            }
        }

        //public int SelectedIndex
        //{
        //    get { return treeViewLogic.SelectedIndex; }
        //    set { treeViewLogic.SelectedIndex = value; }
        //}

        #endregion

        #region Events

        public event EventHandler SelectedItemChanged;

        #endregion

        #region Initialize

        public TreeView() : base()
        {
            InitializeTreeViewLogic();

        }

        public TreeView(GraphicalUiElement visual) : base(visual)
        {
            InitializeTreeViewLogic();
        }

        private void InitializeTreeViewLogic()
        {
            // Do we need this?
            //treeViewLogic.Items.CollectionChanged += HandleCollectionChanged;

            // Do we need this?
            treeViewLogic.NewTreeViewItemCreated = (treeViewItem) =>
                treeViewItem.Selected += HandleChildItemSelected;
        }

        protected override void ReactToVisualChanged()
        {
            base.ReactToVisualChanged();

            // This has to happen after the base call where InnerPanel is assigned
            treeViewLogic.AssignControls(InnerPanel);
        }

        #endregion

        #region Events

        private void HandleChildItemSelected(object sender, EventArgs e)
        {
            // We need to see if this became newly active. An active tree node is either
            // selected itself or one of its children is active


            TreeViewItem activeChild = null;
            activeChild = treeViewLogic.TreeViewItems.FirstOrDefault(item =>
                item != sender && item.IsSelectionActive);

            if (activeChild != null)
            {
                activeChild.DeselectRecursively();
            }

            SelectedItemChanged?.Invoke(this, null);
        }

        #endregion

        public void BringIntoView(TreeViewItem item)
        {
            var topOfView = this.clipContainer.GetAbsoluteY();

            var bottomOfView = topOfView +
                this.clipContainer.GetAbsoluteHeight();

            var topOfItem = item.Visual.GetAbsoluteY();

            var bottomOfItem = topOfItem +
                item.SelectableHeight;

            var isAbove = topOfItem < topOfView;
            var isBelow = bottomOfItem > bottomOfView;



            if(isAbove)
            {
                var amountToMove = topOfItem - topOfView;
                verticalScrollBar.Value += amountToMove;
            }
            else if(isBelow)
            {
                var amountToMove = bottomOfItem - bottomOfView;
                verticalScrollBar.Value += amountToMove;
            }
        }

        public void SelectNextVisible(bool bringIntoView = true)
        {
            var selected = this.SelectedItem;
            SelectNextVisibleInternal(treeViewItem: selected, canSelectChildren:true);
            if(bringIntoView)
            {
                BringIntoView(this.SelectedItem);
            }
        }

        public void SelectPreviousVisible(bool bringIntoView = true)
        {
            var selected = this.SelectedItem;
            SelectPreviousVisible(selected);
            if(bringIntoView)
            {
                BringIntoView(this.SelectedItem);
            }
        }

        private void SelectPreviousVisible(TreeViewItem treeViewItem)
        {
            if (treeViewItem == null)
            {
                throw new InvalidOperationException("No tree view is selected");
            }

            List<TreeViewItem> siblings = GetSiblings(treeViewItem);

            var indexInSiblings = siblings.IndexOf(treeViewItem);

            if(indexInSiblings > 0)
            {
                var previousSibling = siblings[indexInSiblings - 1];

                if(previousSibling.IsExpanded == false || previousSibling.TreeViewItems.Count == 0)
                {
                    previousSibling.IsSelected = true;
                }
                else
                {
                    SelectLastVisibleChild(previousSibling);

                }
            }
            else
            {
                if(treeViewItem.Parent != null)
                {
                    treeViewItem.Parent.IsSelected = true;
                }
            }
        }

        private void SelectLastVisibleChild(TreeViewItem treeViewItem)
        {
            var last = treeViewItem.TreeViewItems.Last();

            if(last.IsExpanded == false || last.TreeViewItems.Count == 0)
            {
                last.IsSelected = true;
            }
            else
            {
                SelectLastVisibleChild(last.TreeViewItems.Last());
            }
        }

        private void SelectNextVisibleInternal(TreeViewItem treeViewItem, bool canSelectChildren)
        {
            if (treeViewItem == null)
            {
                throw new InvalidOperationException("No tree view is selected");
            }

            if(treeViewItem.TreeViewItems.Count > 0 && treeViewItem.IsExpanded && canSelectChildren)
            {
                treeViewItem.TreeViewItems.First().IsSelected = true;
            }
            else
            {
                List<TreeViewItem> siblings = GetSiblings(treeViewItem);

                var indexOfThis = siblings.IndexOf(treeViewItem);

                if (indexOfThis == -1)
                {
                    throw new InvalidOperationException();
                }

                if (indexOfThis == siblings.Count - 1)
                {
                    if (treeViewItem.Parent == null)
                    {
                        // do nothing, it's the last one
                    }
                    else
                    {
                        SelectNextVisibleInternal(treeViewItem.Parent, false);
                    }
                }
                else
                {
                    siblings[indexOfThis + 1].IsSelected = true;
                }
            }
        }

        private List<TreeViewItem> GetSiblings(TreeViewItem treeViewItem)
        {
            var parent = treeViewItem.Parent;

            List<TreeViewItem> siblings;

            if (parent == null)
            {
                siblings = this.treeViewLogic.TreeViewItems;
            }
            else
            {
                // Cheat here, for performance:
                siblings = (List<TreeViewItem>)parent.TreeViewItems;
            }

            return siblings;
        }
    }

}
