using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;

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
        }

    }
}
