using Gum.Wireframe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FlatRedBall.Forms.Controls
{
    public class ItemsControl : ScrollViewer
    {
        #region Fields/Properties

        protected Type ItemGumType { get; set; }
        protected Type ItemFormsType { get; set; } = typeof(ListBoxItem);

        IList items;
        public IList Items
        {
            get => items;
            set
            {
                if(items != value)
                {
                    if(items != null)
                    {
                        ClearVisualsInternal();
                    }

                    if(items is INotifyCollectionChanged notifyCollectionChanged)
                    {
                        notifyCollectionChanged.CollectionChanged -= HandleCollectionChanged;
                    }
                    items = value;
                    if(items is INotifyCollectionChanged newNotifyCollectionChanged)
                    {
                        newNotifyCollectionChanged.CollectionChanged += HandleCollectionChanged;
                    }

                    if(items?.Count > 0)
                    {
                        // refresh!
                        var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, 
                            items, startingIndex:0);
                        HandleCollectionChanged(this, args);
                    }
                }
            }
        } 

        protected List<ListBoxItem> ListBoxItemsInternal = new List<ListBoxItem>();

        ReadOnlyCollection<ListBoxItem> listBoxItemsReadOnly;
        public ReadOnlyCollection<ListBoxItem> ListBoxItems
        {
            get
            {
                if(listBoxItemsReadOnly == null)
                {
                    listBoxItemsReadOnly = new ReadOnlyCollection<ListBoxItem>(ListBoxItemsInternal);
                }
                return listBoxItemsReadOnly;
            }
        }

        public FrameworkElementTemplate FrameworkElementTemplate { get; set; }
        public VisualTemplate VisualTemplate { get; set; }

        #endregion

        #region Events

        public event EventHandler ItemClicked;

        #endregion

        public ItemsControl() : base()
        {
            Items = new ObservableCollection<object>();
        }

        public ItemsControl(GraphicalUiElement visual) : base(visual)
        {
            Items = new ObservableCollection<object>();
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
                item.GotFocus += HandleItemFocused;
                item.Clicked += HandleListBoxItemClicked;
            }
            else
            {
                var visual = CreateNewVisual(o);

                item = CreateNewListBoxItem(visual);

                item.Selected += HandleItemSelected;
                item.GotFocus += HandleItemFocused;
                item.Clicked += HandleListBoxItemClicked;

                item.UpdateToObject(o);
                item.BindingContext = o;
            }

            return item;
        }

        private ListBoxItem CreateNewListBoxItem(GraphicalUiElement visual)
        {
            if(FrameworkElementTemplate != null)
            {
                return FrameworkElementTemplate.CreateContent() as ListBoxItem;
            }
            else
            {
    #if DEBUG
                if (ItemFormsType == null)
                {
                    throw new Exception($"The control does not have a ItemFormsType specified. " +
                        "This property must be set before adding any items");
                }
    #endif
                var listBoxFormsConstructor = ItemFormsType.GetConstructor(new Type[] { typeof(GraphicalUiElement) });

                if (listBoxFormsConstructor == null)
                {
                    string message =
                        $"Could not find a constructor for {ItemFormsType} which takes a single GraphicalUiElement argument. " +
                        $"If you defined {ItemFormsType} without specifying a constructor, you need to add a constructor which takes a GraphicalUiElement and calls the base constructor.";
                    throw new Exception(message);
                }

                ListBoxItem item;
                if (visual.FormsControlAsObject is ListBoxItem asListBoxItem)
                {
                    item = asListBoxItem;
                }
                else
                {
                    item = listBoxFormsConstructor.Invoke(new object[] { visual }) as ListBoxItem;
                }

                return item;
            }
        }

        private GraphicalUiElement CreateNewVisual(object vm)
        {
            if(VisualTemplate != null)
            {
                return VisualTemplate.CreateContent(vm);
            }
            else
            {
                var listBoxItemGumType = ItemGumType;

                if (listBoxItemGumType == null && DefaultFormsComponents.ContainsKey(typeof(ListBoxItem)))
                {

                    listBoxItemGumType = DefaultFormsComponents[typeof(ListBoxItem)];
                }
    #if DEBUG
                if (listBoxItemGumType == null)
                {
                    throw new Exception($"The control does not have a ItemGumType specified, nor does the DefaultFormsControl have an entry for ListBoxItem. " +
                        "This property must be set before adding any items");
                }
    #endif
                // vic says - this uses reflection, could be made faster, somehow...

                var gumConstructor = listBoxItemGumType.GetConstructor(new[] { typeof(bool), typeof(bool) });
                var visual = gumConstructor.Invoke(new object[] { true, true }) as GraphicalUiElement;
                return visual;
            }
        }

        #region Event Handler methods

        protected virtual void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        int index = e.NewStartingIndex;
                        foreach (var item in e.NewItems)
                        {
                            var newItem = CreateNewListItemVisual(item);

                            InnerPanel.Children.Insert(index, newItem.Visual);

                            newItem.Visual.Parent = base.InnerPanel;

                            ListBoxItemsInternal.Insert(index, newItem);
                            index++;
                        }
                    }

                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        var index = e.OldStartingIndex;

                        var listItem = InnerPanel.Children[index];
                        ListBoxItemsInternal.RemoveAt(index);
                        listItem.Parent = null;
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    ClearVisualsInternal();
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        var index = e.NewStartingIndex;
                        var listItem = InnerPanel.Children[index];

                        ListBoxItemsInternal[e.NewStartingIndex].UpdateToObject(Items[index]);
                    }

                    break;
            }
        }

        private void ClearVisualsInternal()
        {
            for (int i = InnerPanel.Children.Count - 1; i > -1; i--)
            {
                InnerPanel.Children[i].Parent = null;
            }
            ListBoxItemsInternal.Clear();
        }

        private void HandleItemSelected(object sender, EventArgs e)
        {
            OnItemSelected(sender, new SelectionChangedEventArgs());

        }
        
        private void HandleItemFocused(object sender, EventArgs e)
        {
            OnitemFocused(sender, EventArgs.Empty);
        }


        private void HandleListBoxItemClicked(object sender, EventArgs e)
        {
            OnItemClicked(sender, null);
        }

        protected virtual void OnItemSelected(object sender, SelectionChangedEventArgs args)
        {
            for (int i = 0; i < ListBoxItemsInternal.Count; i++)
            {
                var listBoxItem = ListBoxItemsInternal[i];
                if (listBoxItem != sender && listBoxItem.IsSelected)
                {
                    args.RemovedItems.Add(Items[i]);
                    listBoxItem.IsSelected = false;
                }
            }
        }

        protected virtual void OnitemFocused(object sender, EventArgs args)
        {
            for (int i = 0; i < ListBoxItemsInternal.Count; i++)
            {
                var listBoxItem = ListBoxItemsInternal[i];
                if (listBoxItem != sender && listBoxItem.IsFocused)
                {
                    listBoxItem.IsFocused = false;
                }
            }
        }

        protected void OnItemClicked(object sender, EventArgs args)
        {
            ItemClicked?.Invoke(sender, args);
        }

        #endregion

        #region Update To


        #endregion
    }
}
