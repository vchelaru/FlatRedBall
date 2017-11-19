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

        public Type ListBoxItemType { get; set; }

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
                if (selectedIndex > 0 && selectedIndex < Items.Count)
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
            }
        }

        #endregion

        #region Events

        public event EventHandler NewItemSelected;

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

            NewItemSelected?.Invoke(Items[selectedIndex], null);

        }

        #endregion

        private ListBoxItem CreateNewListItemVisual(object o)
        {
            ListBoxItem item = new ListBoxItem();
            var constructor = ListBoxItemType.GetConstructor(new[] {typeof(bool)});
            item.Visual = constructor.Invoke(new object[]{ true }) as GraphicalUiElement;

            item.Selected += HandleItemSelected;
            item.UpdateToObject(o);

            return item;
        }
    }
}
