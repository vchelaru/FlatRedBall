using System;
using System.Collections;
using System.Collections.Generic;
#if FRB_MDX
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX.DirectInput;
using Direct3D=Microsoft.DirectX.Direct3D;

using Keys = Microsoft.DirectX.DirectInput.Key;
#elif FRB_XNA || SILVERLIGHT
using Microsoft.Xna.Framework.Input;
#endif
using FlatRedBall.Input;
using FlatRedBall.Graphics;


namespace FlatRedBall.Gui
{

	/// <summary>
	/// A List Box which can hold collapsable items.
	/// </summary>
    public class CollapseListBox : ListBoxBase
    {
        #region Fields

        const float horizontalBottom = 166/256f;
		const float horizontalPixelWidth = 2/256f;

		const float cornerBottom = 169/256f;
		const float cornerPixelWidth = 2/256f;

		const float centerLeft = 18/256f;
		const float centerRight = 19/256f;
		const float centerTop = 162/256f;
        const float centerBottom = 163 / 256f;

        // The right-click list window that appears
        ListBox mListBox;


        #endregion

        #region Properties

        

        public bool ShowExpandCollapseAllOption
        {
            get;
            set;
        }

        #endregion

        #region Event Methods

        private void PopupListBoxClick(Window callingWindow)
        {
            CollapseItem item = mListBox.GetFirstHighlightedItem();
            mListBox.Visible = false;

            if (item == null)
            {
                return;
            }

            if (item.Text == "Expand All")
            {
                ExpandAll();
            }
            else if (item.Text == "Collapse All")
            {
                CollapseAll();
            }
        }

        private void OnRightClick(Window callingWindow)
        {
            if (ShowExpandCollapseAllOption)
            {
                GuiManager.AddPerishableWindow(mListBox);

                mListBox.Visible = true;

                GuiManager.PositionTopLeftToCursor(mListBox);
            }

        }

        #endregion

        #region Methods

        #region Constructor

        public CollapseListBox(Cursor cursor) : 
            base(cursor)
		{
            mListBox = new ListBox(mCursor);
            mListBox.AddItem("Expand All");
            mListBox.AddItem("Collapse All");
            mListBox.SetScaleToContents(0);
            mListBox.HighlightOnRollOver = true;
            mListBox.ScrollBarVisible = false;
            mListBox.Click += PopupListBoxClick;


            SecondaryClick += OnRightClick;
        }

        #endregion

        #region Public Methods

		public CollapseItem AddItemUnique(String stringToAdd, object ReferenceObject)
		{
			if(this.ContainsObject(ReferenceObject))
				return GetItem(ReferenceObject);
			else
				return AddItem(stringToAdd, ReferenceObject);
		}


		public CollapseItem AddItemToItem(string stringToAdd, object ReferenceObject, CollapseItem itemToAddTo)
		{
            CollapseItem itemToReturn = itemToAddTo.AddItem(stringToAdd, ReferenceObject);
			AdjustScrollSize();
			return itemToReturn;

		}


		public CollapseItem AttachItem(CollapseItem itemToAttach)
		{
			itemToAttach.mParentBox = this;
			itemToAttach.parentItem = null;
			Items.Add(itemToAttach);
			AdjustScrollSize();


			return itemToAttach;

		}


        public void Clear()
        {
            Items.Clear();
            float numShowing = ((float)mScaleY - 1) / (Items.Count);
            if (numShowing > 1) numShowing = 1;
            mScrollBar.View = numShowing;
            mScrollBar.Sensitivity = (1 / (float)(Items.Count - (int)(mScaleY - 1)));
            mScrollBar.SetScrollPosition(mStartAt);

        }


        public override void ClearEvents()
        {
            base.ClearEvents();
        }


        public void CollapseAll()
        {
            foreach (CollapseItem collapseItem in mItems)
            {
                collapseItem.CollapseAll();
            }

            AdjustScrollSize();
        }


        public bool ContainsObject(object objectToSearchFor)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].Contains(objectToSearchFor))
                    return true;
            }
            return false;

        }


        public void DeselectObject(object objectToDeselect)
        {
            CollapseItem ci = GetItem(objectToDeselect);

            if (ci != null && mHighlightedItems.Contains(ci))
            {
                mHighlightedItems.Remove(ci);
            }
        }


        public void ExpandAll()
        {
            foreach (CollapseItem collapseItem in mItems)
            {
                collapseItem.ExpandAll();
            }

            AdjustScrollSize();
        }


        public List<CollapseItem> GetHighlightedItems()
        {
            return mHighlightedItems;

        }


        public Object GetFirstHighlightedParentObject()
        {
            if (this.mHighlightedItems.Count == 0)
                return null;
            else
            {
                if (mHighlightedItems[0].parentItem != null)
                    return mHighlightedItems[0].parentItem.ReferenceObject;
                else
                    return null;

            }
        }


        public string GetFirstHighlightedString()
        {
            if (mHighlightedItems.Count != 0)
                return mHighlightedItems[0].Text;
            else
                return "";
        }


        public object GetLastHighlightedObject()
        {
            if (mHighlightedItems.Count != 0)
                return mHighlightedItems[mHighlightedItems.Count - 1].ReferenceObject;
            else
                return null;
        }


        public int GetNumContained(object objectToCount)
        {
            int count = 0;
            for (int i = 0; i < Items.Count; i++)
            {
                Items[i].GetNumContained(objectToCount, ref count);
            }
            return count;
        }


        public Object GetObject(string objectToGet)
        {
            Object objectToReturn = null;
            for (int i = 0; i < Items.Count; i++)
            {
                objectToReturn = Items[i].GetObject(objectToGet);
                if (objectToReturn != null)
                    return objectToReturn;
            }
            return null;
        }


        public int IndexOf(object objectToSearchFor)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].ReferenceObject == objectToSearchFor)
                {
                    return i;
                }
            }

            return -1;
        }


        public bool MoveItemUp(CollapseItem itemToMove)
        {
            // if the item belongs to another item
            if (itemToMove.parentItem != null)
            {
                int num = itemToMove.parentItem.mItems.IndexOf(itemToMove);
                if (num == 0)
                    return false;

                itemToMove.parentItem.mItems.Remove(itemToMove);
                itemToMove.parentItem.mItems.Insert(num - 1, itemToMove);
            }
            else // item belongs to a ListBox
            {
                if (itemToMove.parentBox is CollapseListBox)
                {
                    CollapseListBox parentAsCollapseListBox = itemToMove.parentBox as CollapseListBox;

                    int num = parentAsCollapseListBox.Items.IndexOf(itemToMove);
                    if (num == 0)
                        return false;

                    parentAsCollapseListBox.Items.Remove(itemToMove);
                    parentAsCollapseListBox.Items.Insert(num - 1, itemToMove);
                }
                else if (itemToMove.parentBox is ListBox)
                {
                    ListBox parentAsListBox = itemToMove.parentBox as ListBox;

                    int num = parentAsListBox.mItems.IndexOf(itemToMove);
                    if (num == 0)
                        return false;

                    parentAsListBox.mItems.Remove(itemToMove);
                    parentAsListBox.mItems.Insert(num - 1, itemToMove);
                }

            }
            return true;
        }


        public bool MoveItemDown(CollapseItem itemToMove)
        {
            // if the item belongs to another item
            if (itemToMove.parentItem != null)
            {
                int num = itemToMove.parentItem.mItems.IndexOf(itemToMove);
                if (num == itemToMove.parentItem.mItems.Count - 1)
                    return false;

                itemToMove.parentItem.mItems.Remove(itemToMove);
                itemToMove.parentItem.mItems.Insert(num + 1, itemToMove);
            }
            else // item belongs to a ListBox
            {
                if (itemToMove.parentBox is CollapseListBox)
                {
                    CollapseListBox parentAsCollapseListBox = itemToMove.parentBox as CollapseListBox;

                    int num = parentAsCollapseListBox.Items.IndexOf(itemToMove);
                    if (itemToMove.ParentItem != null && num == itemToMove.parentItem.mItems.Count - 1)
                        return false;

                    parentAsCollapseListBox.Items.Remove(itemToMove);
                    parentAsCollapseListBox.Items.Insert(num + 1, itemToMove);
                }
                else if (itemToMove.parentBox is ListBox)
                {
                    ListBox parentAsListBox = itemToMove.parentBox as ListBox;

                    int num = parentAsListBox.mItems.IndexOf(itemToMove);
                    if (num == itemToMove.parentItem.mItems.Count - 1)
                        return false;

                    parentAsListBox.mItems.Remove(itemToMove);
                    parentAsListBox.mItems.Insert(num + 1, itemToMove);
                }
            }
            return true;
        }


        public CollapseItem MoveLeftOne(string itemToRemove)
        {
            CollapseItem tempItem = GetItemByName(itemToRemove);
            if (tempItem != null)
                tempItem.MoveLeftOne();
            return tempItem;
        }


        public CollapseItem MoveLeftOne(CollapseItem itemToRemove)
        {
            if (itemToRemove != null)
                itemToRemove.MoveLeftOne();
            return itemToRemove;
        }


        public CollapseItem MoveLeftOne(object objectReference)
        {
            CollapseItem tempItem = GetItem(objectReference);
            if (tempItem != null)
                tempItem.MoveLeftOne();
            return tempItem;

        }


        public void MoveHighlightedUp()
        {

            foreach (CollapseItem item in this.mHighlightedItems)
            {
                MoveItemUp(item);
            }
        }


        public void MoveHighlightedDown()
        {
            foreach (CollapseItem item in this.mHighlightedItems)
            {
                MoveItemDown(item);
            }


        }

        #region remove item methods

        /// <summary>
        /// Detaches the item from the CollapseListBox.
        /// </summary>
        /// <remarks>
        /// This method will keep all children of the detached item in tact so that the item can simply
        /// be reattached again.
        /// </remarks>
        /// <param name="itemToRemove"></param>
        /// <returns></returns>
        public CollapseItem DetachItem(string itemToRemove)
        {
            CollapseItem tempItem = GetItemByName(itemToRemove);

            if (tempItem.parentItem != null)
            {
                tempItem.parentItem.mItems.Remove(tempItem);
                tempItem.parentItem.FixCollapseIcon();
            }
            else
                this.Items.Remove(tempItem);

            int numOfCollapsedItems = GetNumCollapsed();
            if (mStartAt + mScaleY - 1 > numOfCollapsedItems) mStartAt = numOfCollapsedItems - (int)(mScaleY - 1);
            if (mStartAt < 0) mStartAt = 0;
            AdjustScrollSize();
            return tempItem;


        }



        public CollapseItem RemoveItemAndChildren(CollapseItem itemToRemove)
        {
            if (itemToRemove != null)
                itemToRemove.RemoveSelfAndChildren();
            int numOfCollapsedItems = GetNumCollapsed();
            if (mStartAt + mScaleY - 1 > numOfCollapsedItems) mStartAt = numOfCollapsedItems - (int)(mScaleY - 1);
            if (mStartAt < 0) mStartAt = 0;
            AdjustScrollSize();
            return itemToRemove;
        }


        public CollapseItem RemoveItemAndChildren(object objectToRemove)
        {

            return RemoveItemAndChildren(GetItem(objectToRemove));

        }


        public List<CollapseItem> RemoveHighlightedItems()
        {
            List<CollapseItem> arrayToReturn = new List<CollapseItem>();
            foreach (CollapseItem ci in mHighlightedItems)
                arrayToReturn.Add(ci);

            for (int i = mHighlightedItems.Count - 1; i > -1; i--)
            {
                mHighlightedItems[i].RemoveSelf();
            }


            int numOfCollapsedItems = GetNumCollapsed();
            if (mStartAt + mScaleY - 1 > numOfCollapsedItems) mStartAt = numOfCollapsedItems - (int)(mScaleY - 1);
            if (mStartAt < 0) mStartAt = 0;
            AdjustScrollSize();
            this.HighlightItem(null, false);

            return arrayToReturn;
        }


        public List<CollapseItem> RemoveHighlightedItemsAndChildren()
        {
            List<CollapseItem> arrayToReturn = new List<CollapseItem>();
            foreach (CollapseItem ci in mHighlightedItems)
                arrayToReturn.Add(ci);

            for (int i = mHighlightedItems.Count - 1; i > -1; i--)
            {
                mHighlightedItems[i].RemoveSelfAndChildren();
            }

            int numOfCollapsedItems = GetNumCollapsed();
            if (mStartAt + mScaleY - 1 > numOfCollapsedItems) mStartAt = numOfCollapsedItems - (int)(mScaleY - 1);
            if (mStartAt < 0) mStartAt = 0;
            AdjustScrollSize();
            return arrayToReturn;

        }


        #endregion



        #endregion


		#endregion
	}
}
