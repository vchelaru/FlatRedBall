using Gum.Wireframe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using System.Text;

namespace FlatRedBall.Forms.Controls
{
    public class ItemsControl : ScrollViewer
    {

        protected Type ItemGumType { get; set; }
        protected Type ItemFormsType { get; set; } = typeof(ListBoxItem);

        public ObservableCollection<object> Items
        {
            get;
            private set;
        } = new ObservableCollection<object>();

        protected List<ListBoxItem> listBoxItems = new List<ListBoxItem>();

        public ItemsControl() : base()
        {
            Items.CollectionChanged += HandleCollectionChanged;
        }

        public ItemsControl(GraphicalUiElement visual) : base(visual)
        {
            Items.CollectionChanged += HandleCollectionChanged;
        }

        private ListBoxItem CreateNewListItemVisual(object o)
        {
            ListBoxItem item;
            if (o is ListBoxItem)
            {
                // the user provided a list box item, so just use that directly instead of creating a new one
                item = o as ListBoxItem;
                // let's hope the item doesn't already have this event - if the user recycles them that could be a problem...
                item.Selected += HandleItemSelected;
            }
            else
            {
                var listBoxItemGumType = ItemGumType;

                if (listBoxItemGumType == null && DefaultFormsComponents.ContainsKey(typeof(ListBoxItem)))
                {

                    listBoxItemGumType = DefaultFormsComponents[typeof(ListBoxItem)];
                }
#if DEBUG
                string controlType = "control";
                string prefix = "";
                
                if (listBoxItemGumType == null)
                {


                    throw new Exception($"The {controlType} does not have a {prefix}ItemGumType specified, nor does the DefaultFormsControl have an entry for ListBoxItem. " +
                        "This property must be set before adding any items");
                }
                if (ItemFormsType == null)
                {
                    throw new Exception($"The {controlType} does not have a {prefix}ItemFormsType specified. " +
                        "This property must be set before adding any items");
                }
#endif
                // vic says - this uses reflection, could be made faster, somehow...

                var gumConstructor = listBoxItemGumType.GetConstructor(new[] { typeof(bool), typeof(bool) });
                var listBoxFormsConstructor = ItemFormsType.GetConstructor(new Type[] { typeof(GraphicalUiElement) });

                var visual = gumConstructor.Invoke(new object[] { true, true }) as GraphicalUiElement;


                if (listBoxFormsConstructor == null)
                {
                    string message =
                        $"Could not find a constructor for {ItemFormsType} which takes a single GraphicalUiElement argument. " +
                        $"If you defined {ItemFormsType} without specifying a constructor, you need to add a constructor which takes a GraphicalUiElement and calls the base constructor.";
                    throw new Exception(message);
                }

                item = listBoxFormsConstructor.Invoke(new object[] { visual }) as ListBoxItem;
                item.Selected += HandleItemSelected;
                item.UpdateToObject(o);
            }


            return item;
        }

#region Event Handler methods

        private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:

                    foreach (var item in e.NewItems)
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

                    for (int i = InnerPanel.Children.Count - 1; i > -1; i--)
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
            OnItemSelected(sender, new SelectionChangedEventArgs());

        }

        protected virtual void OnItemSelected(object sender, SelectionChangedEventArgs args)
        {
            for (int i = 0; i < listBoxItems.Count; i++)
            {
                var listBoxItem = listBoxItems[i];
                if (listBoxItem != sender && listBoxItem.IsSelected)
                {
                    args.RemovedItems.Add(Items[i]);
                    listBoxItem.IsSelected = false;
                }
            }


        }

#endregion
    }
}
