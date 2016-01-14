using System;

#if FRB_MDX
using Microsoft.DirectX;
#else

#endif
using System.Collections.Generic;
using FlatRedBall.Graphics;
using FlatRedBall.ManagedSpriteGroups;


namespace FlatRedBall.Gui
{
	/// <summary>
	/// Summary description for ComboBox.
	/// </summary>
	public class ComboBox : Window
	{
		#region Fields
		Button mDropDownButton;
		TextBox mSelectionDisplay;
		ListBox mListBox;
        List<string> stringArray = new List<string>();

		public object SelectedObject;

        object mPreviousSelectedObject;

        bool mExpandOnTextBoxClick;
        //bool mStretchListBoxToContentWidth;
        
		#endregion


		#region Properties

        public CollapseItem this[int i]
        {
            get
            {
                return mListBox.mItems[i];
            }
        }


        public bool AllowTypingInTextBox
        {
            get
            {
                return mSelectionDisplay.TakingInput;
            }
            set
            {
                mSelectionDisplay.TakingInput = value;
            }
        }


        public int Count
        {
            get
            {
                return mListBox.mItems.Count;
            }

        }


        public override bool Enabled
        {
            get
            {
                return base.Enabled;
            }
            set
            {
                base.Enabled = value;

                // Vic says: I added this on September 1, 2009
                // to make the UI change if disabled.  Undo this if it 
                // causes problems.
                if (mSelectionDisplay != null && mDropDownButton != null)
                {
                    mSelectionDisplay.Enabled = value;
                    mDropDownButton.Enabled = value;
                }
            }
        }


        public bool ExpandOnTextBoxClick
        {
            get { return mExpandOnTextBoxClick; }
            set 
            {
                if (mExpandOnTextBoxClick == false && value == true)
                    mSelectionDisplay.Click += new GuiMessage(OnDropDownButtonClick);
                else if (mExpandOnTextBoxClick == true && value == false)
                    mSelectionDisplay.Click -= new GuiMessage(OnDropDownButtonClick);

                

                mExpandOnTextBoxClick = value; 
            }
        }


        public bool HighlightOnRollOver
        {
            get { return mListBox.HighlightOnRollOver; }
            set { mListBox.HighlightOnRollOver = value; }
        }


        public override bool IsWindowOrChildrenReceivingInput
        {
            get
            {
                {
                    return this.mListBox.Visible || base.IsWindowOrChildrenReceivingInput;
                }
            }
        }

        public object PreviousSelectedObject
        {
            get { return mPreviousSelectedObject; }
        }


		public override float ScaleX
		{
			get{ return mScaleX;	}
			set
			{
				mScaleX = value;
				mDropDownButton.SetPositionTL(2*value - 1.3f, ScaleY);

				mSelectionDisplay.ScaleX = value - 1.4f;
				mSelectionDisplay.SetPositionTL(ScaleX -.9f, ScaleY);

                if (mListBox != null)
                {
                    mListBox.ScaleX = value;
                }
			}
		}


        public ListBox.Sorting SortingStyle
        {
            get
            {
                return mListBox.SortingStyle;
            }
            set
            {
                mListBox.SortingStyle = value;
            }
        }


		public string Text
		{
			get{  return mSelectionDisplay.Text; }
			set
			{
				mSelectionDisplay.Text = value;
			}
		}


		#endregion


		#region Events
		public event GuiMessage ItemClick = null;

        public event GuiMessage TextChange = null;


		#endregion


        #region Delegate Methods

        /// <summary>
        /// Event clicked when the dropDownButton is clicked.  If the ListBox is not visible
        /// it will appear.
        /// </summary>
        /// <param name="callingWindow"></param>
        void OnDropDownButtonClick(Window callingWindow)
        {
            if (mListBox.Visible)
                return;

            // The ListBox is not visible, so "expand" it.

            GuiManager.AddPerishableWindow(mListBox);
            mListBox.Visible = true;

            if (mListBox.SpriteFrame != null)
            {
                SpriteManager.AddSpriteFrame(mListBox.SpriteFrame);
            }

//                listBox.SetPositionTL(ScaleX, ScaleY + 4);
            mListBox.SetScaleToContents(ScaleX);
            mListBox.SetPositionTL(ScaleX, ScaleY + mListBox.ScaleY + 1);

            mListBox.HighlightOnRollOver = true;

            mListBox.UpdateDependencies();


            float maximumScale = GuiManager.YEdge - (MenuStrip.MenuStripHeight / 2.0f);
            if (mListBox.ScaleY > maximumScale)
            {
                mListBox.ScrollBarVisible = true;
                mListBox.ScaleY = maximumScale;
            }

            if (mListBox.WorldUnitY - mListBox.ScaleY < -GuiManager.Camera.YEdge)
                mListBox.Y -= -GuiManager.Camera.YEdge - (mListBox.WorldUnitY - mListBox.ScaleY);

            if (mListBox.WorldUnitX + mListBox.ScaleX > GuiManager.Camera.XEdge)
                mListBox.X -= (mListBox.WorldUnitX + mListBox.ScaleX) - GuiManager.Camera.XEdge;

            if (mListBox.WorldUnitX - mListBox.ScaleX < -GuiManager.Camera.XEdge)
                mListBox.X += -GuiManager.Camera.XEdge - (mListBox.WorldUnitX - mListBox.ScaleX);

            mListBox.HighlightItem(mSelectionDisplay.Text);

        }


        void OnListBoxClicked(Window callingWindow)
        {
            mSelectionDisplay.Text = "";

            List<string> a = ((ListBox)callingWindow).GetHighlighted();
            if (a.Count != 0)
            {
                mPreviousSelectedObject = SelectedObject;

                mSelectionDisplay.Text = a[0];
                this.SelectedObject = ((ListBox)callingWindow).GetHighlightedObject()[0];
                
            }

            if (ItemClick != null)
                ItemClick(this);

            GuiManager.RemoveWindow(callingWindow, true);

            callingWindow.Visible = false;
        }


        void OnMouseWheelScroll(Window callingWindow)
        {
            if (mListBox.Items.Count == 0)
                return;


            int index = GetItemIndex(Text);

            if (mCursor.ZVelocity > 0)
            {
                index--;
            }
            else
            {
                index++;
            }



            index %= mListBox.Items.Count;

            if (index == -1)
            {
                index = mListBox.Items.Count - 1;
            }
            else if (index < -1)
            {
                index = 0;
            }

            mSelectionDisplay.Text = mListBox.Items[index].Text;

            mPreviousSelectedObject = SelectedObject;

            this.SelectedObject =
                mListBox.Items[index].ReferenceObject;
            

            if (ItemClick != null)
                ItemClick(this);

        }

        void RaiseTextChange(Window callingWindow)
        {
            if (TextChange != null)
            {
                TextChange(this);
            }
        }

        #endregion


        #region Methods

        #region Constructors 

        public ComboBox(Cursor cursor) : base(cursor)
        {
            #region Create the TextBox (mSelectionDisplay)
            mSelectionDisplay = new TextBox(mCursor);
            AddWindow(mSelectionDisplay);
			mSelectionDisplay.TakingInput = false;
            mSelectionDisplay.fixedLength = false;
            //mStretchListBoxToContentWidth = true;
            #endregion

            #region Create drop-down button

            mDropDownButton = new Button(mCursor);
            AddWindow(mDropDownButton);
            // Not sure why this is here.  Commented out July 31 2007
			//dropDownButton.mSprite.RotationZ = (float)System.Math.PI;
            mDropDownButton.ScaleX = .9f;
            
			mDropDownButton.Click += new GuiMessage(OnDropDownButtonClick);

            #endregion

            this.ScaleY = 1.4f;
			SelectedObject = null;
            this.ScaleX = 4;

            mListBox = new ListBox(mCursor);
            AddWindow(mListBox);
			this.RemoveWindow(mListBox); // just a quick way to have a list box initialized for us, but not keep it on this window
			mListBox.SetPositionTL(ScaleX, ScaleY + 2);
            mListBox.Visible = false;
			mListBox.ScrollBarVisible = false;
			mListBox.Click += new GuiMessage(OnListBoxClicked);


            MouseWheelScroll += OnMouseWheelScroll;
            mSelectionDisplay.MouseWheelScroll += OnMouseWheelScroll;
            mDropDownButton.MouseWheelScroll += OnMouseWheelScroll;
            mSelectionDisplay.LosingFocus += RaiseTextChange;
		}

//        public ComboBox(SpriteFrame baseSF, SpriteFrame textBoxSF,
//            string buttonTexture,
//            Cursor cursor, Camera camera, string contentManagerName)
//            : base(baseSF, cursor)
//        {
////            baseSF.colorOperation = Microsoft.DirectX.Direct3D.TextureOperation.Add;
////            baseSF.tintBlue = 255;
//            mSelectionDisplay = base.AddTextBox(textBoxSF, "redball.bmp", camera, contentManagerName);
//            mSelectionDisplay.TakingInput = false;
//            mSelectionDisplay.fixedLength = false;
//            mSelectionDisplay.SpriteFrame.Z = baseSF.Z - .001f;
//            mSelectionDisplay.SpriteFrame.ScaleY = baseSF.ScaleY - .2f;
//            mStretchListBoxToContentWidth = false;
 
//            mDropDownButton = base.AddButton(buttonTexture, GuiManager.InternalGuiContentManagerName);

//            mDropDownButton.Click += new GuiMessage(OnDropDownButtonClick);
//            mDropDownButton.SpriteFrame.Z = baseSF.Z - .001f;
//            mDropDownButton.SpriteFrame.ScaleX = mDropDownButton.SpriteFrame.ScaleY = baseSF.ScaleY - .2f;
            

//            mName = baseSF.Name;
            
//            mListBox = this.AddListBox(baseSF.Clone(), null, null, null, null, null);
//            mListBox.SpriteFrame.Name = baseSF.Name + "ListBoxSpriteFrame";
//            mListBox.SpriteFrame.UpdateInternalSpriteNames();
//            SpriteManager.AddSpriteFrame(mListBox.SpriteFrame);

//            mListBox.SpriteFrame.Z = baseSF.Z - .1f;

//            this.RemoveWindow(mListBox); // just a quick way to have a list box initialized for us, but not keep it on this window
            
            
//            mListBox.SetPositionTL(ScaleX, ScaleY + 2);
//            mListBox.Visible = false;
//            mListBox.ScrollBarVisible = false;
//            mListBox.Click += new GuiMessage(OnListBoxClicked);

//            // I have no clue why this is in here.
////            listBox.sf.xVelocity = 1;

//            SelectedObject = null;

//            this.ScaleX = SpriteFrame.ScaleX;
//        }

//        public ComboBox(SpriteFrame baseSF, SpriteFrame textBoxSF, SpriteFrame buttonSpriteFrame,
//            Cursor cursor, Camera camera)
//            : base(baseSF, cursor)
//        {
//            //            baseSF.colorOperation = Microsoft.DirectX.Direct3D.TextureOperation.Add;
//            //            baseSF.tintBlue = 255;
//            mSelectionDisplay = base.AddTextBox(textBoxSF, "redball.bmp", camera, GuiManager.InternalGuiContentManagerName);
//            mSelectionDisplay.TakingInput = false;
//            mSelectionDisplay.fixedLength = false;
//            mSelectionDisplay.SpriteFrame.Z = baseSF.Z - .001f;
//            mSelectionDisplay.SpriteFrame.ScaleY = baseSF.ScaleY - .2f;
//            mStretchListBoxToContentWidth = false;

//            mDropDownButton = base.AddButton(buttonSpriteFrame);

//            mDropDownButton.Click += new GuiMessage(OnDropDownButtonClick);
//            mDropDownButton.SpriteFrame.Z = baseSF.Z - .001f;
////            dropDownButton.sf.ScaleX = dropDownButton.sf.ScaleY = baseSF.ScaleY - .2f;

//            mName = baseSF.Name;

//            mListBox = this.AddListBox(baseSF.Clone(), null, null, null, null, null);
//            mListBox.SpriteFrame.Name = baseSF.Name + "ListBoxSpriteFrame";
//            mListBox.SpriteFrame.UpdateInternalSpriteNames();
//            SpriteManager.AddSpriteFrame(mListBox.SpriteFrame);

//            mListBox.SpriteFrame.Z = baseSF.Z - .1f;

//            this.RemoveWindow(mListBox); // just a quick way to have a list box initialized for us, but not keep it on this window

//            mListBox.SetPositionTL(ScaleX, ScaleY + 2);
//            mListBox.Visible = false;
//            mListBox.ScrollBarVisible = false;
//            mListBox.Click += new GuiMessage(OnListBoxClicked);

//            // I have no clue why this was here
////            listBox.sf.xVelocity = 1;

//            SelectedObject = null;

//            this.ScaleX = SpriteFrame.ScaleX;
//        }

        #endregion

        #region Public Methods

        public void AddItem(string stringToAdd)
		{
			mListBox.AddItem(stringToAdd);
            
		}
		

		public void AddItem(string stringToAdd, object referenceObject)
		{

			mListBox.AddItem(stringToAdd, referenceObject);
		}


        public void AddItemsFromEnum(Type enumType)
        {
#if XBOX360 || WINDOWS_PHONE
            throw new NotImplementedException();
#else
            Clear();

            string[] availableValues = Enum.GetNames(enumType);
            Array array = Enum.GetValues(enumType);

            for(int i = 0; i < array.Length; i++)
            {
                string s = availableValues[i];
                AddItem(s, array.GetValue(i));
            }
#endif
        }


		public void CallOnItemClick()
		{
			if(this.ItemClick != null)
				ItemClick(this);
		}


		public void Clear()
		{
			mListBox.Clear();
        }


        public bool ContainsText(string textToSearchFor)
        {
            foreach (CollapseItem ci in mListBox.mItems)
            {
                if (ci.Text == textToSearchFor)
                {
                    return true;
                }
            }
            return false;
        }


        public bool ContainsObject(object objectToSearchFor)
        {
            foreach (CollapseItem ci in mListBox.mItems)
            {
                if (ci.ReferenceObject == objectToSearchFor)
                {
                    return true;
                }
            }
            return false;
        }


        public bool ContainsObject(string objectToSearchFor)
        {
            foreach (CollapseItem ci in mListBox.mItems)
            {
                if (ci.ReferenceObject as string == objectToSearchFor)
                {
                    return true;
                }
            }
            return false;
        }


        public CollapseItem FindItemByText(string textToSearchFor)
        {
            foreach (CollapseItem ci in mListBox.mItems)
            {
                if (ci.Text == textToSearchFor)
                {
                    return ci;
                }
            }
            return null;
        }


        public int GetItemIndex(string text)
        {
            CollapseItem item = mListBox.GetItemByName(text);

            if (item != null)
            {
                return mListBox.Items.IndexOf(item);
            }
            else
            {
                return -1;
            }
        }


        public void InsertItem(int index, string stringToAdd)
        {
            mListBox.InsertItem(index, stringToAdd);
        }


        public void InsertItem(int index, string stringToAdd, object referenceObject)
        {
            mListBox.InsertItem(index, stringToAdd, referenceObject);
        }


        public void RemoveAt(int index)
        {
            mListBox.RemoveItemAt(index);
        }


        public void SelectItem(int index)
        {
            mSelectionDisplay.Text = mListBox.mItems[index].Text;

            mPreviousSelectedObject = SelectedObject;

            this.SelectedObject = mListBox.mItems[index].ReferenceObject;

            if (ItemClick != null)
                ItemClick(this);
        }


        public void SelectItemNoCall(int index)
        {
            mSelectionDisplay.Text = mListBox.mItems[index].Text;

            mPreviousSelectedObject = SelectedObject;

            this.SelectedObject = mListBox.mItems[index].ReferenceObject;
        }


        public void SelectItemByObject(object referenceObject)
        {
            foreach (CollapseItem ci in mListBox.mItems)
            {
                if (ci.ReferenceObject == referenceObject)
                {
                    mSelectionDisplay.Text = ci.Text;

                    mPreviousSelectedObject = SelectedObject;
                    SelectedObject = ci.ReferenceObject;
                    break;
                }
            }

            if (ItemClick != null)
                ItemClick(this);
        }


        public void SelectItemByObject(string referenceObject)
        {
            foreach (CollapseItem ci in mListBox.mItems)
            {
                if (ci.ReferenceObject as string == referenceObject)
                {
                    mSelectionDisplay.Text = ci.Text;

                    mPreviousSelectedObject = SelectedObject;
                    SelectedObject = ci.ReferenceObject;
                    break;
                }
            }

            if (ItemClick != null)
                ItemClick(this);
        }


        public void SelectItemByObjectNoCall(object referenceObject)
        {
            foreach (CollapseItem ci in mListBox.mItems)
            {
                if (ci.ReferenceObject == referenceObject)
                {
                    mSelectionDisplay.Text = ci.Text;

                    mPreviousSelectedObject = SelectedObject;
                    SelectedObject = ci.ReferenceObject;
                    break;
                }
            }
        }


        public void SelectItemByText(string itemText)
        {
            foreach (CollapseItem ci in mListBox.mItems)
            {
                if (ci.Text == itemText)
                {
                    mSelectionDisplay.Text = ci.Text;

                    mPreviousSelectedObject = SelectedObject;
                    SelectedObject = ci.ReferenceObject;
                    break;
                }
            }
            if (ItemClick != null)
                ItemClick(this);
        }


        public void SelectItemByTextNoCall(string itemText)
        {
            foreach (CollapseItem ci in mListBox.mItems)
            {
                if (ci.Text == itemText)
                {
                    mSelectionDisplay.Text = ci.Text;

                    mPreviousSelectedObject = SelectedObject;
                    SelectedObject = ci.ReferenceObject;
                    break;
                }
            }

        }


        #endregion

        #region Internal Methods

        internal override void Destroy()
        {
            base.Destroy();

            this.mListBox.Destroy();
        }


        internal protected override void Destroy(bool keepEvents)
        {
            base.Destroy(keepEvents);

            mListBox.Destroy(keepEvents);
        }


        #endregion


		#endregion

	}
}
