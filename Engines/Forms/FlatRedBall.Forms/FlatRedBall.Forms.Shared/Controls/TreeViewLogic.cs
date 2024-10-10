using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using System.Text;

namespace FlatRedBall.Forms.Controls
{
    class TreeViewLogic
    {
        public Type TreeViewItemGumType { get; set; }
        public Type ContainerTreeViewItemGumType { get; set; }

        public Type TreeViewItemFormsType { get; set; } = typeof(TreeViewItem);

        public List<TreeViewItem> TreeViewItems = new List<TreeViewItem>();

        public ObservableCollection<object> Items
        {
            get;
            private set;
        } = new ObservableCollection<object>();

        public Action<TreeViewItem> NewTreeViewItemCreated;

        public object SelectedObject
        {
            get
            {
                for(int i = 0; i < TreeViewItems.Count; i++)
                {
                    if(TreeViewItems[i].IsSelected)
                    {
                        return Items[i];
                    }

                    var selectedObject = TreeViewItems[i].SelectedObject;

                    if(selectedObject != null)
                    {
                        return selectedObject;
                    }
                }

                return null;
            }
        }

        public TreeViewItem SelectedItem
        {
            get
            {
                for (int i = 0; i < TreeViewItems.Count; i++)
                {
                    if (TreeViewItems[i].IsSelected)
                    {
                        return TreeViewItems[i];
                    }

                    var selectedItem = TreeViewItems[i].SelectedItem;

                    if (selectedItem != null)
                    {
                        return selectedItem;
                    }
                }

                return null;
            }
        }
        

        GraphicalUiElement InnerPanel;


        public TreeViewLogic()
        {
            Items.CollectionChanged += HandleCollectionChanged;
        }

        public void AssignControls(GraphicalUiElement innerPanel)
        {
            this.InnerPanel = innerPanel;
        }

        private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:

                    foreach (var item in e.NewItems)
                    {
                        var newItem = CreateNewTreeViewItemVisual(item);

                        var newIndex = e.NewStartingIndex;
                        InnerPanel.Children.Insert(newIndex, newItem.Visual);

                        newItem.Visual.Parent = InnerPanel;

                        TreeViewItems.Insert(e.NewStartingIndex, newItem);

                    }

                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        var index = e.OldStartingIndex;

                        var treeViewItem = InnerPanel.Children[index];
                        TreeViewItems.RemoveAt(index);
                        treeViewItem.Parent = null;
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:

                    for (int i = InnerPanel.Children.Count - 1; i > -1; i--)
                    {
                        InnerPanel.Children[i].Parent = null;
                    }
                    TreeViewItems.Clear();
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        var index = e.NewStartingIndex;
                        var treeViewItem = InnerPanel.Children[index];

                        TreeViewItems[e.NewStartingIndex].UpdateToObject(Items[index]);
                    }

                    break;
            }
        }

        private TreeViewItem CreateNewTreeViewItemVisual(object o)
        {
            TreeViewItem item = null;

            if (o is TreeViewItem)
            {
                item = o as TreeViewItem;
            }
            else
            {
                var treeViewItemGumType = TreeViewItemGumType;

                if (treeViewItemGumType == null && FrameworkElement.DefaultFormsComponents.ContainsKey(typeof(TreeViewItem)))
                {
                    treeViewItemGumType = FrameworkElement.DefaultFormsComponents[typeof(TreeViewItem)];
                }

                if (treeViewItemGumType == null)
                {
                    treeViewItemGumType = ContainerTreeViewItemGumType;
                }

#if DEBUG
                if (treeViewItemGumType == null)
                {
                    throw new Exception("The tree view item box does not have a TreeViewItemGumType specified, nor does the DefaultFormsComponents have an entry for TreeViewItemItem. " +
                        "This property must be set before adding any items");
                }
                if (TreeViewItemFormsType == null)
                {
                    throw new Exception("The tree view does not have a TreeViewItemFormsType specified. " +
                        "This property must be set before adding any items");
                }
#endif

                var gumConstructor = treeViewItemGumType.GetConstructor(new[] { typeof(bool), typeof(bool) });
                var visual = gumConstructor.Invoke(new object[] { true, true }) as GraphicalUiElement;

                var treeViewFormsConstructor = TreeViewItemFormsType.GetConstructor(new Type[] { typeof(GraphicalUiElement) });

                if (treeViewFormsConstructor == null)
                {
                    string message =
                        $"Could not find a constructor for {TreeViewItemFormsType} which takes a single GraphicalUiElement argument. If you defined {TreeViewItemFormsType} without specifying a constructor, you need to add a constructor which takes a GraphicalUiElement and calls the base constructor.";
                    throw new Exception(message);
                }

                item = treeViewFormsConstructor.Invoke(new object[] { visual }) as TreeViewItem;
                item.TreeViewItemFormsType = this.TreeViewItemFormsType;
                item.TreeViewItemGumType = this.TreeViewItemGumType;
                item.UpdateToObject(o);
            }

            NewTreeViewItemCreated?.Invoke(item);


            return item;
        }


    }
}
