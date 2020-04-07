using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using RenderingLibrary;

namespace FlatRedBall.Forms.Controls
{
    public class ListBox : ItemsControl
    {
        #region Fields/Properties

        int selectedIndex = -1;

        public Type ListBoxItemGumType
        {
            get
            {
                return base.ItemGumType;
            }
            set
            {
                base.ItemGumType = value;
            }
        }
        public Type ListBoxItemFormsType
        {
            get { return base.ItemFormsType; }
            set { base.ItemFormsType = value; }
        }

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

                PushValueToViewModel();
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
        }

        public ListBox(GraphicalUiElement visual) : base(visual) 
        {
        }

        protected override void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            base.HandleCollectionChanged(sender, e);

            if(e.Action == NotifyCollectionChangedAction.Remove && 
                (e.OldStartingIndex == selectedIndex ||
                    selectedIndex >= Items.Count))
            {
                // we removed the selected item, so update the VM:

                PushValueToViewModel(nameof(SelectedObject));
                PushValueToViewModel(nameof(SelectedIndex));
            }
            else if(e.Action == NotifyCollectionChangedAction.Reset && selectedIndex >= 0)
            {
                SelectedIndex = -1;
                PushValueToViewModel(nameof(SelectedObject));
            }
        }

        #endregion

        protected override void OnItemSelected(object sender, SelectionChangedEventArgs args)
        {
            base.OnItemSelected(sender, args);

            selectedIndex = listBoxItems.IndexOf(sender as ListBoxItem);
            if (selectedIndex > -1)
            {
                args.AddedItems.Add(Items[selectedIndex]);
            }

            SelectionChanged?.Invoke(this, args);

            PushValueToViewModel(nameof(SelectedObject));
            PushValueToViewModel(nameof(SelectedIndex));
        }

        public void ScrollIntoView(object item)
        {
            var itemIndex = Items.IndexOf(item);

            if(itemIndex != -1)
            {
                var visual = listBoxItems[itemIndex];

                var visualAsIpso = (IPositionedSizedObject)visual.Visual;
                var visualTop = visualAsIpso.Y;
                var visualBottom = visualAsIpso.Y + visualAsIpso.Height;

                var viewTop = -InnerPanel.Y;
                var viewBottom = -InnerPanel.Y + clipContainer.GetAbsoluteHeight();
                var isAboveView = visualTop < viewTop;
                var isBelowView = visualBottom > viewBottom;

                if(isAboveView)
                {
                    var amountToScroll = visualTop - viewTop;
                    verticalScrollBar.Value += amountToScroll;
                }
                else if(isBelowView)
                {
                    var amountToScroll = visualBottom - viewBottom;
                    verticalScrollBar.Value += amountToScroll;
                }
            }
        }
    }
}
