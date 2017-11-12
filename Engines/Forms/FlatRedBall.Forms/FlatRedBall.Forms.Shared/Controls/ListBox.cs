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

        List<object> objects;

        public Type ListBoxItemType { get; set; }

        public ObservableCollection<object> Items
        {
            get;
            private set;
        }

        List<ListBoxItem> listBoxItems = new List<ListBoxItem>();

        #endregion

        public event EventHandler NewItemSelected;

        public ListBox()
        {
            Items = new ObservableCollection<object>();
            Items.CollectionChanged += HandleCollectionChanged;
        }

        protected override void ReactToVisualChanged()
        {
            base.ReactToVisualChanged();
        }

        private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch(e.Action)
            {
                case NotifyCollectionChangedAction.Add:

                    foreach(var item in e.NewItems)
                    {
                        CreateNewListItemVisual(item);
                    }

                    break;
            }
        }

        private void CreateNewListItemVisual(object o)
        {
            ListBoxItem item = new ListBoxItem();
            listBoxItems.Add(item);
            var constructor = ListBoxItemType.GetConstructor(new[] {typeof(bool)});
            item.Visual = constructor.Invoke(new object[]{ true }) as GraphicalUiElement;

            item.Visual.Parent = base.InnerPanel;
            item.Selected += HandleItemSelected;
            item.UpdateToObject(o);
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

    }
}
