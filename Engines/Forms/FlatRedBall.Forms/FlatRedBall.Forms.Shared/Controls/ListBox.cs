using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
        }

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
                if(value > 0 && value < listBoxItems.Count)
                {
                    listBoxItems[value].IsSelected = true;
                }
                else
                {
                    foreach (var listBoxItem in listBoxItems)
                    {
                        listBoxItem.IsSelected = false;
                    }
                    SelectionChanged?.Invoke(null, null);

                }
            }
        }

        #endregion

        #region Events

        public event EventHandler SelectionChanged;

        #endregion

        #region Initialize Methods

        public ListBox()
        {
            Items = new ObservableCollection<object>();
            Items.CollectionChanged += HandleCollectionChanged;
        }

        protected override void ReactToVisualChanged()
        {
            base.ReactToVisualChanged();
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
            foreach (var listBoxItem in listBoxItems)
            {
                if(listBoxItem != sender)
                {
                    listBoxItem.IsSelected = false;
                }
            }

            selectedIndex = listBoxItems.IndexOf(sender as ListBoxItem);

            // todo - WPF uses SelectionChangedArgs, we prob want to incorporate that
            SelectionChanged?.Invoke(this, null);

        }

        #endregion

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
#if DEBUG
                if(ListBoxItemGumType == null)
                {
                    throw new Exception("The list box does not have a ListBoxItemGumType specified. " + 
                        "This property must be set before adding any items");
                }
                if(ListBoxItemFormsType == null)
                {
                    throw new Exception("The list box does not have a ListBoxItemFormsType specified. " +
                        "This property must be set before adding any items");
                }
#endif
                // vic says - this uses reflection, could be made faster, somehow...
                item = ListBoxItemFormsType.GetConstructor(new Type[0]).Invoke(new object[0]) as ListBoxItem;
                var gumConstructor = ListBoxItemGumType.GetConstructor(new[] {typeof(bool)});
                item.Visual = gumConstructor.Invoke(new object[]{ true }) as GraphicalUiElement;
                item.Selected += HandleItemSelected;
                item.UpdateToObject(o);
            }


            return item;
        }
    }
}
