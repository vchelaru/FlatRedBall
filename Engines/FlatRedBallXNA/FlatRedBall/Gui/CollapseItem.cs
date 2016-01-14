using System;
using System.Collections;
using FlatRedBall.Graphics;

using System.Collections.Generic;
using System.Collections.ObjectModel;
#if FRB_MDX
using Microsoft.DirectX.Direct3D;
using System.Reflection;
#elif FRB_XNA || SILVERLIGHT || WINDOWS_PHONE
using Microsoft.Xna.Framework.Graphics;
#endif


namespace FlatRedBall.Gui
{
	/// <summary>
	/// Summary description for CollapseItem.
	/// </summary>
	public class CollapseItem
	{
		#region Fields

		protected string itemText;
		internal bool mExpanded = true;

		public object ReferenceObject;

		internal Window mParentBox;
		internal CollapseItem parentItem;

		internal List<CollapseItem> mItems;

        internal List<ListBoxIcon> icons;
        ReadOnlyCollection<ListBoxIcon> mIconsReadOnly;

        //protected static FlatRedBall.TransformedColoredTextured[] v = new TransformedColoredTextured[6];
#if !SILVERLIGHT

        protected static VertexPositionColorTexture[] v = new VertexPositionColorTexture[6];
#endif

        bool mEnabled;

        internal bool mDrawHighlighted;

		#endregion



		#region Properties

		public CollapseItem this[int i]
		{
			get		{return mItems[i];			}
		}

		public int Count
		{
			get{ return mItems.Count;}
		}


		public int Depth
        {
            get
            {
                if (this.parentItem == null)
                    return 0;
                else
                    return 1 + parentItem.Depth;

            }
        }

        public bool Enabled
        {
            get { return mEnabled; }
            set 
            {
                mEnabled = value;

                if (mParentBox != null && !mParentBox.GuiManagerDrawn)
                {
                    // Need to update the text objects:
                    ((ListBoxBase)mParentBox).UpdateTextStrings();

                }

                if (parentItem != null)
                {
                    if (mEnabled) //Enabled was just set to true.
                    {
                        parentItem.Enabled = true;
                    }
                    else //Enabled was just set to false.
                    {
                        if (parentItem.mItems.Count <= 1)
                        {
                            parentItem.Enabled = false;
                        }
                    }
                }
            }
		}

        public ReadOnlyCollection<ListBoxIcon> Icons
        {
            get
            {
                return mIconsReadOnly;
            }
        }

		public Window parentBox
		{
			get{ return mParentBox; }
		}

        public CollapseItem ParentItem
        {
            get { return parentItem; }
        }


		public string Text
		{
			get{ return itemText;}
			set
            {
                // currnetly we don't support newlines


                    itemText = value;
//                }
            
            }

		}

#if !SILVERLIGHT
        public CollapseItem TopParent
        {
            get
            {
                if (parentItem != null)
                    return parentItem.TopParent;
                else
                    return this;
            }
        }

        public int VisibleChildrenCount
        {
            get
            {
                if (mExpanded)
                {

                    int count = 0;

                    foreach (CollapseItem item in mItems)
                    {
                        count++;

                        count += item.VisibleChildrenCount;
                    }

                    return count;
                }
                else
                {
                    return 0;
                }

            }

        }
#endif

		#endregion

		#region Methods

        #region Constructor

        public CollapseItem(string text, object referenceObject)
		{
			ReferenceObject = referenceObject;
			mItems = new List<CollapseItem>();
            this.Text = text;

            this.icons = new List<ListBoxIcon>();
            mIconsReadOnly = new ReadOnlyCollection<ListBoxIcon>(icons);

            Enabled = true;

#if FRB_MDX
            v[0].Color = v[1].Color = v[2].Color = v[3].Color = v[4].Color = v[5].Color = 0xFF000000;
#elif FRB_XNA
            v[0].Color.PackedValue = v[1].Color.PackedValue = v[2].Color.PackedValue =
                v[3].Color.PackedValue = v[4].Color.PackedValue = v[5].Color.PackedValue = 0xFF000000;
#endif

        }

        #endregion

        #region Public Methods

        public ListBoxIcon AddIcon(Texture2D texture, string name)
        {
            ListBoxIcon newIcon = new ListBoxIcon(texture, name);
            icons.Add(newIcon);
            return newIcon;
        }

        /// <summary>
        /// Adds an icon to the CollapseItem
        /// </summary>
        /// <remarks>
        /// Do not use $ in the name of the icon as the Collapse list boxes use this character for 
        /// icons such as + and - boxes for expanding CollapseItems with children.
        /// </remarks>
        /// <param name="top"></param>
        /// <param name="bottom"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="name"></param>
        public ListBoxIcon AddIcon(float top, float bottom, float left, float right, string name)
        {
            if(name.Contains("$"))
            {
                throw new ArgumentException("Don't use the $ character in icons.  This is reserved");
            }

            return AddIcon(top, bottom, left, right, name, icons.Count);
        }


        public ListBoxIcon AddIcon(float top, float bottom, float left, float right, string name, int index)
        {
            ListBoxIcon lbi = new ListBoxIcon(top, bottom, left, right, name);
            icons.Insert(index, lbi);
            return lbi;
        }


        public CollapseItem AddItem(string itemToAdd)
        {
            return AddItem(itemToAdd, null);
        }


		public CollapseItem AddItem(string itemToAdd, object referenceObject)
		{
            CollapseItem tempItem = new CollapseItem(itemToAdd, referenceObject);
			tempItem.mParentBox = mParentBox;
			tempItem.parentItem = this;
			
			mItems.Add(tempItem);

            FixCollapseIcon();


			if(null != mParentBox as ListBoxBase)
				((ListBoxBase)mParentBox).AdjustScrollSize();

			return tempItem;

		}


		public CollapseItem AttachItemToThis(CollapseItem itemToAdd)
		{
            if (itemToAdd.parentItem != null)
                itemToAdd.Detach();
			if(itemToAdd.mParentBox as ListBoxBase != null)
				((ListBoxBase)itemToAdd.mParentBox).Items.Remove(itemToAdd);

			itemToAdd.mParentBox = mParentBox;
			itemToAdd.parentItem = this;


            mItems.Add(itemToAdd);

            FixCollapseIcon();

			if(null != mParentBox as ListBoxBase)
				((ListBoxBase)mParentBox).AdjustScrollSize();

			return itemToAdd;


        }

        #region XML Docs
        /// <summary>
        /// Returns whether this or any children CollapseItems contain the argument object as their ReferenceObject.
        /// </summary>
        /// <param name="objectToSearchFor">The object to search for.</param>
        /// <returns>Whether this or any children CollapseItems reference the argument.</returns>
        #endregion
        public bool Contains(object objectToSearchFor)
		{
			if(ReferenceObject == objectToSearchFor)
				return true;
			for(int i = 0; i < mItems.Count; i++)
			{
				if(mItems[i].Contains(objectToSearchFor))
					return true;
			}
			return false;

		}


        public bool ContainsItem(CollapseItem item)
        {

			for(int i = 0; i < mItems.Count; i++)
			{
				if(mItems[i] == item || mItems[i].ContainsItem(item))
					return true;
			}
			return false;
        }


        public void ClearIcons()
        {
            this.icons.Clear();
        }


		public void Collapse()
		{
            mExpanded = false;

			foreach (ListBoxIcon icon in mIconsReadOnly)
			{
				if (icon.Name == "$FRB_MINUS_BOX")
				{
					icon.Name = "$FRB_PLUS_BOX";

					icon.Top = 159 / 256.0f;
					icon.Bottom = 175 / 256.0f;
					icon.Left = 81 / 256.0f;
					icon.Right = 97 / 256.0f;

					
				}
			}
		}


        public void CollapseAll()
        {
			Collapse();

            foreach (CollapseItem item in mItems)
            {
                item.CollapseAll();
            }
        }


        public void Detach()
        {
            ListBoxBase listBoxBase = this.mParentBox as ListBoxBase;

            if (parentItem != null)
            {
                parentItem.mItems.Remove(this);
                parentItem.FixCollapseIcon();
                this.parentItem = null;
            }

            if (listBoxBase.Items.Contains(this) == false)
            {
                listBoxBase.Items.Add(this);
            }
            mParentBox = listBoxBase;

        }


		public void Expand()
		{
            mExpanded = true;

			foreach (ListBoxIcon icon in mIconsReadOnly)
			{
				if (icon.Name == "$FRB_PLUS_BOX")
				{
					icon.Name = "$FRB_MINUS_BOX";

					icon.Left = 140 / 256.0f;
					icon.Right = 156 / 256.0f;
					icon.Top = 146 / 256.0f;
					icon.Bottom = 162 / 256.0f;
				}

			}
		}


		public void ExpandAll()
        {
			Expand();

            foreach (CollapseItem item in mItems)
            {
                item.ExpandAll();
            }
        }


        public void FillWithAllDescendantCollapseItems(IList listToFill)
        {
            for (int i = 0; i < mItems.Count; i++)
            {
                listToFill.Add(mItems[i]);

                mItems[i].FillWithAllDescendantCollapseItems(listToFill);
            }
        }


        public void FillWithAllReferencedItems(IList listToFill)
        {
            if (ReferenceObject != null)
            {
                listToFill.Add(ReferenceObject);
            }

            foreach (CollapseItem item in mItems)
            {
                item.FillWithAllReferencedItems(listToFill);
            }
        }


        public void FixCollapseIcon()
        {
            if(icons.Count == 0 && mItems.Count != 0)
            {
                ListBoxIcon lbi = 
                    AddIcon(146 / 256.0f, 162 / 256.0f, 140 / 256.0f, 156 / 256.0f, "$FRB_MINUS_BOX", 0);

                lbi.IconClick += new ListBoxFunction(((ListBoxBase)parentBox).ClickOutliningButton);

 //               icons.Insert(0, lbi);
                
                mExpanded = true;
            }
            else if (icons.Count != 0 && (icons[0].Name == "$FRB_PLUS_BOX" || icons[0].Name == "$FRB_MINUS_BOX")
                && mItems.Count == 0)
            {
                this.icons.RemoveAt(0);
            }
        }


		public bool GetItemNum(ref int count, string itemToGet)
		{
            if (itemToGet == Text)
				return true;
			count ++;

			if(mExpanded == true)
			{
				for(int i = 0; i < mItems.Count; i++)
				{								   
					if(mItems[i].GetItemNum(ref count, itemToGet) == true)
						return true;
				}
			}
			return false;
		}


        public bool GetItemNum(ref int count, CollapseItem itemToGet)
        {
            if (itemToGet == this)
                return true;
            count++;

            if (mExpanded)
            {
                for (int i = 0; i < mItems.Count; i++)
                {
                    if (mItems[i].GetItemNum(ref count, itemToGet) == true)
                        return true;
                }
            }
            return false;
        }


        public bool GetItemNum(ref int count, object itemToGet)
		{
			if(itemToGet == ReferenceObject)
				return true;
			count++;

			if(mExpanded)
			{
				for(int i = 0; i < mItems.Count; i++)
					if(mItems[i].GetItemNum(ref count, itemToGet) == true)
						return true;
			}
			return false;
		}


		public int GetCount(ref int count)
		{
			count++;
			if(mExpanded == true)
			{
				for(int i = 0; i < mItems.Count; i++)
				{
					mItems[i].GetCount(ref count);
				}

			}
			return count;
		}


		public int GetNumContained(object objectToCount, ref int count)
		{
			if(objectToCount == ReferenceObject)
				count++;

			for(int i = 0; i < mItems.Count; i++)
				mItems[i].GetNumContained(objectToCount, ref count);
			return count;

        }


        public CollapseItem GetItem(object itemToGet)
        {
            CollapseItem itemToReturn = null;
            if (itemToGet == ReferenceObject)
                return this;
            for (int i = 0; i < mItems.Count; i++)
            {
                itemToReturn = mItems[i].GetItem(itemToGet);
                if (itemToReturn != null)
                    return itemToReturn;
            }
            return itemToReturn;
        }


        public CollapseItem GetItem(string itemToGet)
        {
            CollapseItem itemToReturn = null;
            if (itemToGet == itemText)
                return this;
            for (int i = 0; i < mItems.Count; i++)
            {
                itemToReturn = mItems[i].GetItem(itemToGet);
                if (itemToReturn != null)
                    return itemToReturn;
            }
            return itemToReturn;

        }


        public CollapseItem GetHighlighted(ref int count, int highlightedNum)
        {
            CollapseItem tempItem = null;
            if (highlightedNum == count)
                return this;
            count++;

            if (mExpanded == true)
            {
                for (int i = 0; i < mItems.Count; i++)
                {
                    tempItem = mItems[i].GetHighlighted(ref count, highlightedNum);
                    if (tempItem != null) return tempItem;
                }
            }
            return tempItem;
        }


        public object GetObject(string objectToGet)
        {
            object objectToReturn = null;
            if (itemText == objectToGet)
                return ReferenceObject;

            for (int i = 0; i < mItems.Count; i++)
            {
                objectToReturn = mItems[i].GetObject(objectToGet);
                if (objectToReturn != null)
                    return objectToReturn;
            }
            return objectToReturn;

        }


        public CollapseItem InsertItem(int index, string itemToAdd, object referenceObject)
        {
            CollapseItem tempItem = new CollapseItem(itemToAdd, referenceObject);
            tempItem.mParentBox = mParentBox;
            tempItem.parentItem = this;

            mItems.Insert(index, tempItem);

            FixCollapseIcon();


            if (mParentBox as ListBoxBase != null)
                ((ListBoxBase)parentBox).AdjustScrollSize();

            return tempItem;
        }


        public bool IsChildOf(CollapseItem potentialParent)
        {
            CollapseItem item = this.parentItem;

            while (item != null)
            {
                if (item == potentialParent)
                    return true;
                else
                    item = item.parentItem;
            }

            return false;
        }


        public void MoveLeftOne()
        {
            if (parentItem != null)
            {
                parentItem.mItems.Remove(this);
                parentItem = parentItem.parentItem;

                if (parentItem != null)
                    parentItem.AttachItemToThis(this);
                else
                {
                    if (null != mParentBox as ListBoxBase)
                        ((ListBoxBase)mParentBox).mItems.Add(this);
                }
            }
        }


        public void RemoveAllChildren()
        {
            for (int i = mItems.Count - 1; i > -1; i--)
            {
                mItems[i].RemoveSelfAndChildren();
            }
        }


        public CollapseItem RemoveObject(object objectToRemove)
        {
            for (int i = 0; i < mItems.Count; i++)
            {
                if (mItems[i].ReferenceObject == objectToRemove)
                {
                    CollapseItem ciRemoved = mItems[i];
                    ciRemoved.RemoveSelf();
                    return ciRemoved;
                    //					i--;
                }
                else
                {
                    CollapseItem ciRemoved = mItems[i].RemoveObject(objectToRemove);
                    if (ciRemoved != null)
                        return ciRemoved;

                }
            }
            return null;

        }

        // This doesn't exist because you should call itemToRemove.RemoveSelf();
        //public void RemoveItem(CollapseItem itemToRemove)
        //{


        //}


        public void RemoveSelf()
        {
            for (int i = mItems.Count - 1; i > -1; i--)
            {
                mItems[i].parentItem = null;
                if (null != mParentBox as CollapseListBox)
                    ((CollapseListBox)mParentBox).AttachItem(mItems[i]);
            }


            if (parentItem == null)
            {
                if (null != mParentBox as ListBoxBase)
                {
                    ((ListBoxBase)mParentBox).mItems.Remove(this);
                }

            }
            else
            {
                parentItem.mItems.Remove(this);
                parentItem.FixCollapseIcon();
            }
            if (((ListBoxBase)mParentBox).GetFirstHighlightedItem() == this)
            {
                ((ListBoxBase)mParentBox).HighlightItem((CollapseItem)null);
            }


            mParentBox = null;
            parentItem = null;
        }


        public void RemoveSelfAndChildren()
        {
            for (int i = mItems.Count - 1; i > -1; i--)
                mItems[i].RemoveSelfAndChildren();

            if (parentItem == null)
            {
                if (null != mParentBox as ListBoxBase)
                    ((ListBoxBase)mParentBox).mItems.Remove(this);
            }
            else
            {
                parentItem.mItems.Remove(this);
                parentItem.FixCollapseIcon();

            }
        }


        public void ReorderToMatchList(IEnumerable list)
        {
            int i = 0;

            foreach(object o in list)
            {             
                if (i >= mItems.Count)
                {
                    CollapseItem itemToMove = GetItem(o);

                    mItems.Remove(itemToMove);                    
                }
                else if (o != mItems[i].ReferenceObject)
                {
                    CollapseItem itemToMove = GetItem(o);

                    mItems.Remove(itemToMove);
                    mItems.Insert(i, itemToMove);
                }

                i++;
            }
        }


        public override string ToString()
        {
            return Text;
        }




        #endregion

        #region Internal Methods
#if !SILVERLIGHT
        internal void Draw(Camera camera, double startY, double startX, double maxWidth,
            int numDeep, ref int itemNum, ref int numDrawn, int startAt, int numToDraw, float distanceBetweenLines)
        {
            if (itemNum > startAt - 1)
            {
                float yForThisRender = (float)(startY - distanceBetweenLines * numDrawn);
                float xPos = (float)(startX + numDeep * .7);
                if (icons.Count != 0 && icons[0].ScaleX > .7f)
                {
                    xPos += icons[0].ScaleX - .7f;

                }
                float yPos = yForThisRender;

                #region draw the icons

                foreach (ListBoxIcon icon in icons)
                {
                    if (icon.Texture != null)
                    {
                        GuiManager.AddTextureSwitch(icon.Texture);
                    }



                    v[0].Position.X = xPos - icon.ScaleX;
                    v[0].Position.Y = yPos - icon.ScaleY;
                    v[0].TextureCoordinate.X = icon.Left;
                    v[0].TextureCoordinate.Y = icon.Bottom;


                    v[1].Position.X = xPos - icon.ScaleX;
                    v[1].Position.Y = yPos + icon.ScaleY;
                    v[1].TextureCoordinate.X = icon.Left;
                    v[1].TextureCoordinate.Y = icon.Top;

                    v[2].Position.X = xPos + icon.ScaleX;
                    v[2].Position.Y = yPos + icon.ScaleY;
                    v[2].TextureCoordinate.X = icon.Right;
                    v[2].TextureCoordinate.Y = icon.Top;

                    v[3] = v[0];
                    v[4] = v[2];

                    v[5].Position.X = xPos + icon.ScaleX;
                    v[5].Position.Y = yPos - icon.ScaleY;
                    v[5].TextureCoordinate.X = icon.Right;
                    v[5].TextureCoordinate.Y = icon.Bottom;

                    GuiManager.WriteVerts(v);

                    xPos += .1f + 2 * icon.ScaleX;
                }
                #endregion

                if (this.Text != null)
                {
                    string textToUse = this.Text.Replace('\n', ' ');




                    int numOfChars = TextManager.GetNumberOfCharsIn((float)(maxWidth - numDeep * .7f), textToUse, GuiManager.TextSpacing);

                    // TODO:  need to modify this to handle varying text sizes
                    TextManager.mScaleForVertexBuffer = GuiManager.TextHeight / 2.0f;
                    TextManager.mSpacingForVertexBuffer = GuiManager.TextSpacing;

                    TextManager.mYForVertexBuffer = yForThisRender;
#if FRB_MDX
				    TextManager.mZForVertexBuffer = (float)camera.Z + 100;
#else
                    TextManager.mZForVertexBuffer = (float)camera.Z - 100;
#endif
                    if (Enabled)
                    {
                        TextManager.mAlphaForVertexBuffer = 1 * FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue;
                    }
                    else
                    {
                        TextManager.mAlphaForVertexBuffer = .5f * FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue;
                    }                    
                    
                    TextManager.mAlignmentForVertexBuffer = HorizontalAlignment.Left;

                    if (mDrawHighlighted)
                    {
                        TextManager.mRedForVertexBuffer = TextManager.mGreenForVertexBuffer = TextManager.mBlueForVertexBuffer =
                            FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue;
                    }
                    else
                    {
                        TextManager.mRedForVertexBuffer = TextManager.mGreenForVertexBuffer = TextManager.mBlueForVertexBuffer = 20;
                    }


#if FRB_MDX
                v[0].Position.Z = v[1].Position.Z = v[2].Position.Z = v[3].Position.Z =
                    v[4].Position.Z = v[5].Position.Z = camera.Z + 100;
#else
                    v[0].Position.Z = v[1].Position.Z = v[2].Position.Z = v[3].Position.Z =
                        v[4].Position.Z = v[5].Position.Z = camera.Z - 100;
#endif

                    TextManager.mXForVertexBuffer = xPos;

                    if (numOfChars != 0)
                    {
                        if (numOfChars == textToUse.Length)
                        {
                            TextManager.Draw(ref textToUse);

                        }
                        else
                        {
                            string asString = textToUse.Substring(0, numOfChars);
                            TextManager.Draw(ref asString);
                        }
                    }
                }

                numDrawn++;
            }
            itemNum++;
            if (numDrawn > numToDraw - 1)
                return;
            if (mExpanded)
            {
                for (int i = 0; i < mItems.Count; i++)
                {
                    mItems[i].Draw(camera, startY, startX, maxWidth, numDeep + 1, ref itemNum, ref numDrawn, startAt, numToDraw, distanceBetweenLines);
                    if (numDrawn > numToDraw - 1)
                        return;
                }
            }
        }
#endif
        internal CollapseItem GetNthVisibleItem(ref int countedSoFar, int itemNumber)
        {
            if (countedSoFar == itemNumber)
                return this;

            countedSoFar++;

            if (mExpanded)
            {
                foreach (CollapseItem item in mItems)
                {
                    CollapseItem itemToReturn = item.GetNthVisibleItem(ref countedSoFar, itemNumber);

                    if (itemToReturn != null)
                        return itemToReturn;
                }
            }
            return null;
        }

        internal int GetVisibleIndex(ref int countedSoFar, CollapseItem collapseItem)
        {
            if (collapseItem == this)
                return countedSoFar;

            countedSoFar++;

            if (mExpanded)
            {
                foreach (CollapseItem item in mItems)
                {
                    int index = item.GetVisibleIndex(ref countedSoFar, collapseItem);

                    if (index != -1)
                    {
                        return index;
                    }
                }
            }

            return -1;
		}

#if !SILVERLIGHT

        internal int GetNumberOfVerticesToDraw(Camera camera, double startY, double startX, double maxWidth, int numDeep, ref int itemNum, ref int numDrawn, int startAt, int numToDraw)
		{
			int numToReturn = 0;

		    #region count the number of vertices for the text
            if (Text != null && itemNum > startAt - 1)
			{

                string textToUse = Text.Replace('\n', ' ') ;



                int numOfChars = TextManager.GetNumberOfCharsIn((float)(maxWidth - numDeep * .7f), textToUse, GuiManager.TextSpacing); 
				numDrawn++;
				numToReturn += numOfChars * 6;

                numToReturn += this.icons.Count * 6;

            }
			#endregion

            itemNum++;
			if(numDrawn > numToDraw - 1)
				return numToReturn;

            if (mExpanded)
            {
                foreach (CollapseItem ci in this.mItems)
                {
                    numToReturn += ci.GetNumberOfVerticesToDraw(camera, startY, startX, maxWidth, numDeep, ref itemNum, ref numDrawn, startAt, numToDraw);
                    if (numDrawn > numToDraw - 1)
                        return numToReturn;
                }
            }

			return numToReturn;
        }


        internal void TurnOffDrawHighlighted()
        {
            mDrawHighlighted = false;

            for (int i = 0; i < mItems.Count; i++)
            {
                mItems[i].TurnOffDrawHighlighted();
            }
        }
#endif
		#endregion

		#endregion
	}
}