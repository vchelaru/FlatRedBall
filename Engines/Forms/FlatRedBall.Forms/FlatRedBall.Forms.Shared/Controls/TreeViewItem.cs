using FlatRedBall.Gui;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace FlatRedBall.Forms.Controls
{
    public class TreeViewItem : FrameworkElement
    {

        #region Fields/Properties

        ToggleButton expandCollapseButton;
        GraphicalUiElement innerPanel;
        ListBoxItem mainListBoxItem;

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


        /// <summary>
        /// Whether this or any contained TreeViewItems are selected.
        /// </summary>
        public bool IsSelectionActive
        {
            get
            {
                return IsSelected || treeViewLogic.TreeViewItems.Any(item => item.IsSelectionActive);
            }
        }

        /// <summary>
        /// Whether this is selected.
        /// </summary>
        public bool IsSelected
        {
            get { return mainListBoxItem?.IsSelected == true; }
            set
            {
                if(mainListBoxItem != null)
                {
                    mainListBoxItem.IsSelected = value;
                }
            }
        }

        bool isExpanded;
        /// <summary>
        /// Whether the TreeViewItem is displaying its expanded content (sub items)
        /// </summary>
        public bool IsExpanded
        {
            get { return isExpanded; }
            set
            {
                if(isExpanded != value)
                {
                    isExpanded = value;
                    UpdateInnerPanelCollapsedState();
                    UpdateToggleButtonState();
                }
            }
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

        public TreeViewItem Parent { get; private set; }

        public ICollection<TreeViewItem> TreeViewItems
        {
            get
            {
                return treeViewLogic.TreeViewItems;
            }
        }

        public float SelectableHeight => this.mainListBoxItem?.ActualHeight ?? 0;

        #endregion

        #region Events
        public event EventHandler Collapsed;
        public event EventHandler Expanded;
        public event EventHandler Selected;
        // todo : need to implement this
        //public event EventHandler Unselected;
        #endregion

        #region Initialize

        public TreeViewItem() : base()
        {
            InitializeTreeViewLogic();

        }
        public TreeViewItem(GraphicalUiElement visual) : base(visual)
        {
            InitializeTreeViewLogic();
        }

        private void InitializeTreeViewLogic()
        {
            treeViewLogic.Items.CollectionChanged += HandleCollectionChanged;

            treeViewLogic.NewTreeViewItemCreated = HandleChildCreated;

        }

        protected override void ReactToVisualChanged()
        {
            // optional
            var mainListBoxItemVisual = Visual.GetGraphicalUiElementByName("ListBoxItemInstance");
            mainListBoxItem = mainListBoxItemVisual?.FormsControlAsObject as ListBoxItem;
            if(mainListBoxItem != null)
            {
                mainListBoxItem.Selected += HandleMainItemSelected;
            }

            // optional
            var expandCollapseButtonVisual = Visual.GetGraphicalUiElementByName("ToggleButtonInstance");
            expandCollapseButton = expandCollapseButtonVisual?.FormsControlAsObject as ToggleButton;
            if(expandCollapseButton != null)
            {
                expandCollapseButton.Checked += HandleExpandCollapseButtonCheckChange;
                expandCollapseButton.Unchecked += HandleExpandCollapseButtonCheckChange;
                // will start out invisible, will be made visible when items are added
                expandCollapseButton.IsVisible = false;
            }

            // required:
            innerPanel = Visual.GetGraphicalUiElementByName("InnerPanelInstance");
            UpdateInnerPanelCollapsedState();
#if DEBUG
            if (innerPanel == null)
            {
                throw new Exception("Missing InnerPanelInstance");
            }
#endif
            treeViewLogic.ContainerTreeViewItemGumType =
                Visual.GetType();

            treeViewLogic.AssignControls(innerPanel);
        }

        #endregion

        #region Event Handler

        private void HandleChildCreated(TreeViewItem newItem)
        {
            newItem.Selected += HandleChildItemSelected;
            newItem.Parent = this;
        }

        private void HandleMainItemSelected(object sender, EventArgs e)
        {
            TreeViewItem activeChild = null;
            activeChild = treeViewLogic.TreeViewItems.FirstOrDefault(item =>
                item != sender && item.IsSelectionActive);

            activeChild?.DeselectRecursively();

            this.Selected?.Invoke(this, null);
        }

        private void HandleExpandCollapseButtonCheckChange(object sender, EventArgs e)
        {
            IsExpanded = expandCollapseButton.IsChecked == true;
        }

        private void HandleChildItemSelected(object sender, EventArgs e)
        {
            // We need to see if this became newly active. An active tree node is either
            // selected itself or one of its children is active


            TreeViewItem activeChild = null;
            activeChild = treeViewLogic.TreeViewItems.FirstOrDefault(item =>
                item != sender && item.IsSelectionActive);

            bool wasAlreadyActive = this.mainListBoxItem?.IsSelected == true ||
                activeChild != null;

            
            if(activeChild != null)
            {
                activeChild.DeselectRecursively();
            }
            else if(IsSelected)
            {
                IsSelected = false;
            }

            // do this after deselecting others so the right item is selected
            Selected?.Invoke(this, null);
        }


        public virtual void UpdateToObject(object o)
        {
            this.mainListBoxItem?.UpdateToObject(o);
        }

        private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (expandCollapseButton != null)
            {
                expandCollapseButton.IsVisible = this.Items.Count > 0;

                expandCollapseButton.IsChecked = this.IsExpanded;
            }
        }


        #endregion

        internal void DeselectRecursively()
        {
            if(IsSelected)
            {
                IsSelected = false;
            }
            else
            {
                foreach(var item in treeViewLogic.TreeViewItems)
                {
                    item.DeselectRecursively();
                }
            }
        }

        #region Utilities

        public TreeViewItem GetSelectedTreeViewRecursively()
        {
            if(IsSelected)
            {
                return this;
            }
            else
            {
                foreach(var childTreeItem in treeViewLogic.TreeViewItems)
                {
                    var foundSelection = childTreeItem.GetSelectedTreeViewRecursively();

                    if(foundSelection != null)
                    {
                        return foundSelection;
                    }
                }
            }

            return null;
        }

        public override string ToString()
        {
            if(mainListBoxItem != null)
            {
                return mainListBoxItem.ToString();
            }
            else
            {
                return "TreeViewItem";
            }
        }

        #endregion

        #region Update To

        private void UpdateInnerPanelCollapsedState()
        {
            if (isExpanded && innerPanel.Parent == null)
            {
                // todo - need to support collapsing without removing the child
                innerPanel.Parent = Visual;
                Expanded?.Invoke(this, null);
            }
            else if (!isExpanded && innerPanel.Parent != null)
            {
                innerPanel.Parent = null;
                Collapsed?.Invoke(this, null);
            }
        }

        private void UpdateToggleButtonState()
        {
            if (this.expandCollapseButton != null)
            {
                expandCollapseButton.IsChecked = IsExpanded;
            }
        }
        #endregion
    }
}
