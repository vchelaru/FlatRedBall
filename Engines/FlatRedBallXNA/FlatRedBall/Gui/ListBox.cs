using System;
using System.Collections;
using System.Collections.Generic;
#if FRB_MDX
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Direct3D=Microsoft.DirectX.Direct3D;
using Microsoft.DirectX.DirectInput;

using Keys = Microsoft.DirectX.DirectInput.Key;
#elif FRB_XNA || SILVERLIGHT || WINDOWS_PHONE
using Microsoft.Xna.Framework.Input;
#endif
using FlatRedBall.Input;
using FlatRedBall.Graphics;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Math;


namespace FlatRedBall.Gui
{
	/// <summary>
	/// Summary description for ListBox.
	/// </summary>
	public class ListBox : ListBoxBase, IInputReceiver
	{
		#region Fields

		const float horizontalBottom = 166/256f;
		const float horizontalPixelWidth = 2/256f;

		const float cornerBottom = 169/256f;
		const float cornerPixelWidth = 2/256f;

		const float centerLeft = 18/256f;
		const float centerRight = 19/256f;
		const float centerTop = 162/256f;
		const float centerBottom = 163/256f;

		//internal List<int> highlighted;


//		Sprite highlight;

//        float mTextureYOffset;
		
        public bool ctrlClickOn;

        List<Keys> mIgnoredKeys = new List<Keys>();
		#endregion

		#region Properties

        //public List<Keys> IgnoredKeys
        //{
        //    get { return mIgnoredKeys; }
        //}

        public float LeftBorderWidth
        {
            get { return mLeftBorder; }
            set { mLeftBorder = value; }
        }

		#endregion

		#region Methods

        #region Constructors

        public ListBox(Cursor cursor)
            : base(cursor)
        { }

        public ListBox(GuiSkin guiSkin, Cursor cursor)
            : base(guiSkin, cursor)
        {

        }

        //public ListBox(GuiSkin guiSkin, Cursor cursor)
        //{

        //}
 //       public ListBox(SpriteFrame windowSpriteFrame, SpriteFrame highlightBar, SpriteFrame scrollBaseSF, SpriteFrame upButtonSF, SpriteFrame downButtonSF,
 //           SpriteFrame barSF, Cursor cursor)
 //           : base(windowSpriteFrame, cursor)
 //       {
 //           HighlightOnRollOver = false;

 //           this.mHighlightBar = highlightBar;

 //           if (highlightBar != null)
 //           {
 //               highlightBar.Z = SpriteFrame.Z - .005f;
 //               UpdateHighlightSpriteFrame();
 //           }

 //           if (scrollBaseSF != null)
 //           {
 //               mScrollBar = base.AddScrollBar(scrollBaseSF, upButtonSF, downButtonSF, barSF);

 //               mScrollBar.upButton.Click += new GuiMessage(onUpButtonClick);
 //               mScrollBar.downButton.Click += new GuiMessage(onDownButtonClick);

 //               // need to set the initial positions of the Scrollbar according to its position.
 ////               scrollBar.SetPositionTL(
 //  //                 scrollBar.sf.X - (sf.X - sf.ScaleX),
 //    //               (sf.Y + sf.ScaleY) - scrollBar.sf.Y);
 //           }

 //           mItems = new List<CollapseItem>();

 //           mTexts = new AttachableList<Text>();
 //       }
        #endregion

        #region Public Methods

        #region AddItem/AddArray
        public void AddArray(IList<string> arrayToAdd)
		{
			foreach(string str in arrayToAdd)
				AddItem(str, null);
		}

        #endregion


        public override void OnClick()
        {
            CollapseItem highlightedItem = GetHighlightedItem();
            if(highlightedItem == null || highlightedItem.Enabled)
                base.OnClick();
        }

        #region XML Docs
        /// <summary>
        /// Removes all items from the list box.
        /// </summary>
        #endregion
        public void Clear()
		{
			mItems.Clear();

            if (mScrollBar != null)
            {
                mScrollBar.View = NumberOfVisibleElements;

                if (NumberOfVisibleElements != 0)
                {
                    mScrollBar.Sensitivity = (1 / NumberOfVisibleElements);
                }
                mScrollBar.SetScrollPosition(StartAt);
                AdjustScrollSize();
            }

		}


		public bool Contains(string s)
		{
			foreach(CollapseItem ci in mItems)
			{
				if(ci.Text == s)	return true;
			}
			return false;
        }


        public bool ContainsObject(object objectToSearchFor)
        {
            foreach (CollapseItem collapseitem in mItems)
            {
                if (collapseitem.ReferenceObject == objectToSearchFor)
                {
                    return true;
                }
            }
            return false;
        }


        #region GetHighlighted Methods
        public List<string> GetHighlighted()
        {
            List<string> array = new List<string>();
            foreach (CollapseItem item in mHighlightedItems)
            {
                array.Add(item.Text);
            }
            return array;
        }


        public CollapseItem GetHighlightedItem()
        {
            if (mHighlightedItems.Count != 0)
                return mHighlightedItems[0];
            else
                return null;
        }


        public int GetFirstHighlightedIndex()
        {
            if (mHighlightedItems.Count == 0)
                return -1;
            else
                return mItems.IndexOf( mHighlightedItems[0]);
        }

        #endregion


        public object GetObject(int num)
        {
            if (num < mItems.Count)
                return mItems[num].ReferenceObject;

            return null;
        }

        
        public object GetObject(string itemString)
        {
            for (int i = 0; i < mItems.Count; i++)
            {
                if (mItems[i].Text == itemString)
                    return mItems[i].ReferenceObject;
            }
            return null;
        }


        public string GetString(int index)
        {
            if (index < mItems.Count)
                return mItems[index].Text;
            return null;
        }




        #region RemoveItem methods
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





        #endregion

        // Updates the X and Y Scales to fit the items in the list
        public void SetScaleToContents(float minimumScale)
        {
            ScaleY = Count + 1;
            //SetPositionTL(ScaleX, ScaleY + listBox.ScaleY + 1);

            if (true)
            {
                // find the width of the longest text item
                float largestWidth = minimumScale * 2;
                foreach (CollapseItem item in mItems)
                {
                    largestWidth = System.Math.Max(largestWidth, TextManager.GetWidth(item.Text, GuiManager.TextSpacing));
                }

                ScaleX = largestWidth / 2.0f + 2;
            }
        }

        #endregion

		#endregion
	}
}