using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace FlatRedBall.Forms.Controls
{
    public class ListBox : ScrollViewer
    {
        #region Fields/Properties

        int selectedIndex = -1;

        public Type ListBoxItemGumType { get; set; }
        public Type ListBoxItemFormsType { get; set; } = typeof(ListBoxItem);

        public ObservableCollection<object> Items
        {
            get;
            private set;
        } = new ObservableCollection<object>();

        List<ListBoxItem> listBoxItems = new List<ListBoxItem>();

        public object SelectedObject
        {
            get
            {
                if (selectedIndex > -1 && selectedIndex < Items.Count)
                {
                    return Items[selectedIndex];
                }
                else
                {
                    return null;
                }
            }
            set
            {
                var index = Items.IndexOf(value);

                SelectedIndex = index;
            }
        }

        public int SelectedIndex
        {
            get
            {
                return selectedIndex;
            }
            set
            {
                if(value > -1 && value < listBoxItems.Count)
                {
                    listBoxItems[value].IsSelected = true;
                }
                else if(value == -1)
                {
                    // do we just set it to the value before doing any logic?
                    selectedIndex = -1;

                    var selectionChangedArgs = new SelectionChangedEventArgs();

                    for(int i = 0; i < listBoxItems.Count; i++)
                    {
                        var listBoxItem = listBoxItems[i];

                        if (listBoxItem.IsSelected)
                        {
                            selectionChangedArgs.RemovedItems.Add(Items[i]);
                            listBoxItem.IsSelected = false;
                        }
                    }

    
                    SelectionChanged?.Invoke(this, selectionChangedArgs);

                }
                else
                {
                    throw new IndexOutOfRangeException();
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Event raised whenever the selection changes. The object parameter is the sender (list box) and the SelectionChangedeventArgs
        /// contains information about the changed selected items.
        /// </summary>
        public event Action<object, SelectionChangedEventArgs> SelectionChanged;

        #endregion

        #region Initialize Methods

        public ListBox() : base()
        {
            Items.CollectionChanged += HandleCollectionChanged;
        }

        public ListBox(GraphicalUiElement visual) : base(visual) 
        {
            Items.CollectionChanged += HandleCollectionChanged;
        }

        private ListBoxItem CreateNewListItemVisual(object o)
        {
            ListBoxItem item;
            if(o is ListBoxItem)
            {
                // the user provided a list box item, so just use that directly instead of creating a new one
                item = o as ListBoxItem;
                // let's hope the item doesn't already have this event - if the user recycles them that could be a problem...
                item.Selected += HandleItemSelected;
            }
            else
            {
                var listBoxItemGumType = ListBoxItemGumType;

                if(listBoxItemGumType == null && DefaultFormsComponents.ContainsKey(typeof(ListBoxItem)))
                {

                    listBoxItemGumType = DefaultFormsComponents[typeof(ListBoxItem)];
                }
#if DEBUG
                if (listBoxItemGumType == null)
                {
                    throw new Exception("The list box does not have a ListBoxItemGumType specified, nor does the DefaultFormsControl have an entry for ListBoxItem. " + 
                        "This property must be set before adding any items");
                }
                if(ListBoxItemFormsType == null)
                {
                    throw new Exception("The list box does not have a ListBoxItemFormsType specified. " +
                        "This property must be set before adding any items");
                }
#endif
                // vic says - this uses reflection, could be made faster, somehow...
                var gumConstructor = listBoxItemGumType.GetConstructor(new[] {typeof(bool), typeof(bool)});
                var visual = gumConstructor.Invoke(new object[] { true, true }) as GraphicalUiElement;

                var listBoxFormsConstructor = ListBoxItemFormsType.GetConstructor(new Type[] { typeof(GraphicalUiElement) });

                if(listBoxFormsConstructor == null)
                {
                    string message =
                        $"Could not find a constructor for {ListBoxItemFormsType} which takes a single GraphicalUiElement argument. If you defined {ListBoxItemFormsType} without specifying a constructor, you need to add a constructor which takes a GraphicalUiElement and calls the base constructor.";
                    throw new Exception(message);
                }

                item = listBoxFormsConstructor.Invoke(new object[] { visual }) as ListBoxItem;
                item.Selected += HandleItemSelected;
                item.UpdateToObject(o);
            }


            return item;
        }

        #endregion

        #region Event Handler methods

        private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch(e.Action)
            {
                case NotifyCollectionChangedAction.Add:

                    foreach(var item in e.NewItems)
                    {
                        var newItem = CreateNewListItemVisual(item);

                        var newIndex = e.NewStartingIndex;
                        InnerPanel.Children.Insert(newIndex, newItem.Visual);

                        newItem.Visual.Parent = base.InnerPanel;

                        listBoxItems.Insert(e.NewStartingIndex, newItem);

                    }

                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        var index = e.OldStartingIndex;

                        var listItem = InnerPanel.Children[index];
                        listBoxItems.RemoveAt(index);
                        listItem.Parent = null;
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:

                    for(int i = InnerPanel.Children.Count - 1; i > -1; i--)
                    {
                        InnerPanel.Children[i].Parent = null;
                    }
                    listBoxItems.Clear();
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        var index = e.NewStartingIndex;
                        var listItem = InnerPanel.Children[index];

                        listBoxItems[e.NewStartingIndex].UpdateToObject(Items[index]);
                    }

                    break;
            }
        }

        private void HandleItemSelected(object sender, EventArgs e)
        {
            var args = new SelectionChangedEventArgs();

            for(int i = 0; i < listBoxItems.Count; i++)
            {
                var listBoxItem = listBoxItems[i];
                if(listBoxItem != sender && listBoxItem.IsSelected)
                {
                    args.RemovedItems.Add(Items[i]);
                    listBoxItem.IsSelected = false;
                }
            }

            selectedIndex = listBoxItems.IndexOf(sender as ListBoxItem);
            if(selectedIndex > -1)
            {
                args.AddedItems.Add(Items[selectedIndex]);
            }

            SelectionChanged?.Invoke(this, args);

        }

        #endregion

    }
}
