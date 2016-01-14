using System;
using System.Collections;
using System.Collections.Generic;

using System.Text;

using FlatRedBall;

using FlatRedBall.Graphics;

using FlatRedBall.Gui;

using FlatRedBall.Math;

using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Input;
#if FRB_XNA || SILVERLIGHT || WINDOWS_PHONE
using Microsoft.Xna.Framework.Input;
#elif FRB_MDX

using Keys = Microsoft.DirectX.DirectInput.Key;

#endif

namespace FlatRedBall.Gui
{
    #region ReorderMessage Delegate

    public delegate void ReorderMessage(ReorderValues reorderValues);

    #endregion

    #region ReorderValues struct
    public struct ReorderValues
    {
        public CollapseItem ItemMoved;
        public CollapseItem OldParent;
        public CollapseItem NewParent;
        public int OldIndex;
        public int NewIndex;
    }
    #endregion

    public abstract class ListBoxBase : Window, IInputReceiver
    {
        #region Enums

        public enum Sorting
        {
            None,
            AlphabeticalIncreasing,
        }

        public enum ToolTipOption
        {
            None,
            CursorOver
        }

        #endregion



        #region Fields

        static float TITLE_BAR_X_SHIFT = -.9f;
        const float TextZOffset = .02f;
        const float SeparatorZOffset = .01f;
        const float HighlightBarOffset = .015f;

        //float mHighlightBarExtension;// = 0;

        internal List<CollapseItem> mItems = new List<CollapseItem>();

        internal List<CollapseItem> mHighlightedItems = new List<CollapseItem>();

        internal int mStartAt;

        private float mDistanceBetweenLines;

        protected float mFirstItemDistanceFromTop;

        protected ScrollBar mScrollBar;

        protected PositionedObjectList<Text> mTexts;

        protected float mLeftBorder;

        HorizontalAlignment mHorizontalAlignment = HorizontalAlignment.Left;

        BitmapFont mFont;

        float mSpacing;
        float mScale;

        float mTextRed;
        float mTextGreen;
        float mTextBlue;

        float mDisabledTextRed;
        float mDisabledTextGreen;
        float mDisabledTextBlue;



        // Stores values for highlighting when GuiManagerDrawn == true
        protected Sprite mHighlight;
        // The highlight bar when the GuiManagerDrawn == false
        protected FlatRedBall.ManagedSpriteGroups.SpriteFrame mHighlightBar;
        SeparatorSkin mSeparatorSkin;

        private PositionedObjectList<SpriteFrame> mSeparators = new PositionedObjectList<SpriteFrame>();
        protected bool mTakingInput;

        public bool ShiftClickOn = false;
        public bool CtrlClickOn = false;

        internal bool mAllowReordering = false;

        #region XML Docs
        /// <summary>
        /// Marks where to insert CollapseItems if reordering with the mouse.
        /// Default is -1, which specifies that it shouldn't be drawn.
        /// </summary>
        #endregion
        int mInsertLocation = -1;

        public bool Lined = false;

        private bool mHighlightOnRollOver = false;
        IInputReceiver mNextInTabSequence;
        Sorting mSortingStyle = Sorting.None;

        float mHighlightBarHorizontalBuffer = 1.4f;

        /// <summary>
        /// When the user pushes the mouse over the list box
        /// this value will be set.  Then this value can be compared
        /// against to see if reordering should occur.
        /// </summary>
        float mYPositionPushed = -1;

        bool mScrollBarVisibility = true;
        List<Keys> mIgnoredKeys = new List<Keys>();
        #endregion

        #region Properties

        public CollapseItem this[int i]
        {
            get { return mItems[i]; }
        }

        public List<CollapseItem> Items
        {
            get { return mItems; }
        }

        public bool AllowReordering
        {
            get { return mAllowReordering; }
            set { mAllowReordering = value; }
        }

        public int Count
        {
            get { return mItems.Count; }
        }

        public ToolTipOption CurrentToolTipOption
        {
            get;
            set;
        }

        public float DistanceBetweenLines
        {
            get { return mDistanceBetweenLines; }
            set
            {
                mDistanceBetweenLines = value;
                if (GuiManagerDrawn == false)
                    UpdateTextPositions();

                AdjustScrollSize();
            }
        }
        public float FirstItemDistanceFromTop
        {
            get { return mFirstItemDistanceFromTop; }
            set
            {
                mFirstItemDistanceFromTop = value;
                if (GuiManagerDrawn == false)
                    UpdateTextPositions();
                AdjustScrollSize();
            }
        }

        public float FirstItemScreenY
        {
            get
            {
                return (float)(mWorldUnitY + mScaleY) - mFirstItemDistanceFromTop;
            }
        }

        public BitmapFont Font
        {
            get { return mFont; }
            set
            {
                mFont = value;
                if (mTexts != null)
                {
                    foreach (Text t in mTexts)
                    {
                        t.Font = value;
                    }
                }

            }
        }

        public SpriteFrame HighlightBar
        {
            get { return mHighlightBar; }
            set { mHighlightBar = value; }
        }

        public int HighlightedCount
        {
            get
            {
                return mHighlightedItems.Count;
            }
        }

        public bool HighlightOnRollOver
        {
            get { return mHighlightOnRollOver; }
            set { mHighlightOnRollOver = value; }
        }

        public List<Keys> IgnoredKeys
        {
            get { return mIgnoredKeys; }
        }

        public IInputReceiver NextInTabSequence
        {
            get { return mNextInTabSequence; }
            set { mNextInTabSequence = value; }
        }
        #region XML Docs
        /// <summary>
        /// Returns the number of elements that can be shown in the list box.  Increasing the ScaleY or decreasing
        /// the DistanceBetweenLines increases this value.
        /// </summary>
        #endregion
        public int NumberOfVisibleElements
        {
            get
            {
                float textScale = GuiManager.TextHeight / 2.0f;
                if (textScale * 2 < DistanceBetweenLines) // the text objects don't overlap
                    return (int)((ScaleY * 2 - mFirstItemDistanceFromTop + textScale) / mDistanceBetweenLines);
                else
                {
                    // text objects overlap.  Consider the bottom item may "spill over" if the overlapping space is not removed
                    return (int)((ScaleY * 2 - mFirstItemDistanceFromTop + textScale - (textScale * 2 - mDistanceBetweenLines))
                        / mDistanceBetweenLines);
                }
            }
        }
#if !SILVERLIGHT

        public override float ScaleY
        {
            get { return base.ScaleY; }
            set
            {
                base.ScaleY = value;

                if (mScrollBar != null)
                {
                    mScrollBar.ScaleY = value - .4f;

                    mScrollBar.SetPositionTL(2 * mScaleX - mHighlightBarHorizontalBuffer, mScaleY);
                }

                if (StartAt + NumberOfVisibleElements > mItems.Count)
                    StartAt = mItems.Count - NumberOfVisibleElements;
                if (StartAt < 0) StartAt = 0;

                if (GuiManagerDrawn == false)
                {
                    this.UpdateTextPositions();
                    UpdateSeparators();
                }

                AdjustScrollSize();


                //highlight.Y = si.ScaleY - 2 - 2*highlightedNum;


            }
        }

        public override float ScaleX
        {
            get { return (float)mScaleX; }
            set
            {
                base.ScaleX = value;
                if (mScrollBar != null)
                    mScrollBar.SetPositionTL(2 * value - mHighlightBarHorizontalBuffer, ScaleY);
                mHighlight.ScaleX = value - mHighlightBarHorizontalBuffer;
                if (mHighlightBar != null)
                {
                    mHighlightBar.ScaleX = value - mHighlightBarHorizontalBuffer;
                }

                if (!GuiManagerDrawn)
                {
                    UpdateSeparators();
                }

            }
        }

#endif

        public bool ScrollBarVisible
        {
            get { return mScrollBar != null && mScrollBar.Visible; }
            set
            {
                if (mScrollBar == null) return;

                mScrollBar.Visible = value;

                mScrollBarVisibility = value;

                //if (value)
                //{
                //    mHighlightBarExtension = 0;
                //}
                //else
                //{
                //    mHighlightBarExtension = 1.7f;
                //}

                if (!this.Children.Contains(mScrollBar))
                {
                    mScrollBar.Destroy();

                    // Vic says:  This is somewhat of a hack - let's talk about why we need this
                    // ListBoxes are used by ComboBoxes.  When the user clicks on the list box, it is
                    // removed from the GuiManager, which destroys it...but that's a problem because a 
                    // call to Destroy also destroys the scroll bar.  But the ComboBox recycles this ListBox,
                    // but the ScrollBar is destroyed.  Therefore we need to recreate it if this is set to true
                    mScrollBar = new ScrollBar(mCursor);
                    AddWindow(mScrollBar);


                    mScrollBar.upButton.Click += new GuiMessage(UpdateStartAtToScrollBar);
                    mScrollBar.downButton.Click += new GuiMessage(UpdateStartAtToScrollBar);
                }
            }
        }
#if !SILVERLIGHT
        public Sorting SortingStyle
        {
            get { return mSortingStyle; }
            set
            {
                if (value != mSortingStyle)
                {
                    mSortingStyle = value;

                    if (value == Sorting.AlphabeticalIncreasing)
                    {
                        Sort();
                    }
                }
            }
        }
#endif

        public int StartAt
        {
            get { return mStartAt; }
            set
            {
                mStartAt = value;

                if (mStartAt + NumberOfVisibleElements > GetNumCollapsed())
                    mStartAt = GetNumCollapsed() - NumberOfVisibleElements;
                if (mStartAt < 0)
                    mStartAt = 0;

                if (GuiManagerDrawn == false)
                {
                    UpdateHighlightSpriteFrame();

                    UpdateTextStrings();
                }
            }
        }

        public bool StrongSelectOnHighlight
        {
            get;
            set;
        }
        public bool TakingInput
        {
            get { return mTakingInput; }
            set { mTakingInput = value; }
        }
#if !SILVERLIGHT
        public float TextScaleX
        {
            get
            {
                if (this.ScrollBarVisible == true)
                    return (float)mScaleX - 2.0f;
                else
                    return (float)mScaleX - 1.0f;

            }
        }

        public override bool Visible
        {
            get
            {
                return base.Visible;
            }
            set
            {
                base.Visible = value;
                if (this.GuiManagerDrawn == false)
                {
                    foreach (Text text in this.mTexts)
                        text.Visible = value;

                    for (int i = 0; i < mSeparators.Count; i++)
                    {
                        mSeparators[i].Visible = value;
                    }

                    if (value)
                    {
                        mScrollBar.Visible = mScrollBarVisibility;
                        UpdateSeparators();
                    }

                    if (value && mHighlightedItems.Count != 0)
                    {
                        UpdateHighlightSpriteFrame();
                    }
                    else
                    {
                        mHighlightBar.Visible = false;
                    }
                }

                if (value == false && InputManager.ReceivingInput == this)
                {
                    InputManager.ReceivingInput = null;
                }
            }
        }
#endif
        #endregion

        #region Events

        public event GuiMessage CollapseItemDropped;

        public event GuiMessage EscapeRelease;

        public event GuiMessage GainFocus;
        /// <summary>
        /// This event is fired when highlighting an item that was not 
        /// previously highlighted.
        /// </summary>
        public event GuiMessage OnNewHighlight;

        public event GuiMessage Highlight;

        public event ReorderMessage Reorder;

        public event FocusUpdateDelegate FocusUpdate;

        public event GuiMessage StrongSelect;


        #endregion

        #region Delegate and Event Methods

        void HighlightItemAtCursor(Window callingWindow)
        {
            CollapseItem ci = GetItemAtCursor();

            // If the user didn't click on anything, then exit the method now
            if (ci == null)
                return;

            #region find out if we are over any icons.  Icons have special functionality

            float collapseItemXStart = mWorldUnitX - ScaleX + 1.2f + ci.Depth * .7f - .7f;

            #region Get the cursorX to see if it's over an item
            float cursorY = 0;
            float cursorX = 0;

            // get the cursor position
            if (this.GuiManagerDrawn == false)
            {
                mCursor.GetCursorPosition(out cursorX, out cursorY, AbsoluteWorldUnitZ);
            }
            else
            {
                cursorY = mCursor.YForUI;
                cursorX = mCursor.XForUI;
            }
            float difference = cursorX - collapseItemXStart;
            #endregion

            float divisor = 1.5f;

            if (ci.Icons.Count != 0)
            {
                divisor = .1f + ci.Icons[0].ScaleX * 2;
            }

            int itemOn = (int)(difference / divisor);

            //                System.Console.Out.Write(itemOn);
            // remember, negative numbers less than 1 will round to 0, so only consider if difference >= 0
            if (difference >= 0 && itemOn < ci.icons.Count)
            {
                if (ci.icons[itemOn].Enabled)
                {
                    ci.icons[itemOn].RaiseIconClick(ci, this);

                    // if we click on an icon, we don't want to highlight anything
                    return;
                }
            }
            #endregion

            bool controlDown =
                InputManager.Keyboard.KeyDown(Keys.LeftControl) ||
                InputManager.Keyboard.KeyDown(Keys.RightControl);

            bool shiftDown =
                InputManager.Keyboard.KeyDown(Keys.LeftShift) ||
                InputManager.Keyboard.KeyDown(Keys.RightShift);

            if (this.ShiftClickOn && shiftDown && this.mHighlightedItems.Count != 0)
            {
                int startingHighlight = Items.IndexOf(mHighlightedItems[0]);

                int endingHighlight = Items.IndexOf(ci);

                int start = System.Math.Min(startingHighlight, endingHighlight);
                int end = System.Math.Max(startingHighlight, endingHighlight) + 1;

                int currentlyHighlightedIndex = Items.IndexOf(mHighlightedItems[0]);

                for (int i = start; i < end; i++)
                {
                    if (i != currentlyHighlightedIndex)
                    {
                        HighlightItem(Items[i], true);
                    }
                }
            }
            else
            {
                HighlightItem(ci,
                    controlDown && this.CtrlClickOn);
            }
        }


        internal void ClickOutliningButton(CollapseItem collapseItem, ListBoxBase collapseListBox, ListBoxIcon listBoxIcon)
        {
            if (listBoxIcon.Name == "$FRB_PLUS_BOX")
            {
                collapseItem.Expand();
            }
            else if (listBoxIcon.Name == "$FRB_MINUS_BOX")
            {
                collapseItem.Collapse();
            }

            collapseListBox.AdjustScrollSize();

        }

        public void OnFocusUpdate()
        {
            if (FocusUpdate != null)
            {
                FocusUpdate(this);
            }
        }
        public void OnGainFocus()
        {
            if (GainFocus != null)
                GainFocus(this);
        }
        void OnPrimaryPush(Window callingWindow)
        {
            // this will hold the cursor's positions - the position depends
            // on whether the ListBox is GuiManagerDrawn or not.
            float cursorY = 0;
            float cursorX = 0;

            // get the cursor position
            if (this.GuiManagerDrawn == false)
                mCursor.GetCursorPosition(out cursorX, out cursorY, AbsoluteWorldUnitZ);
            else
                cursorY = mCursor.YForUI;

            #region If the cursor is within the valid area for grabbing

            if (cursorY > mWorldUnitY - mScaleY + 1.0f)
            {
                mYPositionPushed = mCursor.YForUI;

                float topOfFirstItem = mWorldUnitY + mScaleY - (FirstItemDistanceFromTop - 1);

                int numToHighlight;
                if (topOfFirstItem - cursorY < 0)
                    numToHighlight = -1;
                else
                    numToHighlight =
                    StartAt + (int)((topOfFirstItem - cursorY) / DistanceBetweenLines);

                if (numToHighlight < StartAt + NumberOfVisibleElements && numToHighlight > -1)
                {
                    GuiManager.mCollapseItemDraggedOff = GetNthVisibleItem(numToHighlight);

                }
            }

            #endregion
        }


        void OnPrimaryClick(Window callingWindow)
        {
            InputManager.ReceivingInput = this;

            string errorMessage = null;

            #region Reordering

            if (mInsertLocation != -1)
            {
                #region Create the ReorderValues

                CollapseItem item =
                    GuiManager.mCollapseItemDraggedOff;

                ReorderValues reorderValues = new ReorderValues();

                reorderValues.ItemMoved = item;
                reorderValues.OldParent = item.parentItem;

                if (item.parentItem == null)
                {
                    reorderValues.OldIndex = mItems.IndexOf(item);
                }
                else
                {
                    reorderValues.OldIndex = item.parentItem.mItems.IndexOf(item);
                }


                // the reorderValues.NewIndex value is going to be set below depending on whether
                // the item that is moving is being moved to a place where there is a parent.




                // temporary - items can't be moved to a different parent
                reorderValues.NewParent = reorderValues.OldParent;

                #endregion

                #region Reorder the CollapseItem

                #region The item being reordered has a parent
                if (reorderValues.OldParent != null)
                {
                    int oldIndex = reorderValues.OldParent.mItems.IndexOf(reorderValues.ItemMoved);

                    // mInsertLocation is the index of the pink line.
                    // Let's find out what item is located there.
                    CollapseItem levelItem = GetItem(
                        System.Math.Max(mInsertLocation, 0));

                    bool isLastItem = false;

                    // If it's null, then it's possible that it's past the last item.

                    if (levelItem == null)
                    {

                        if (mInsertLocation != 0 && mItems.Count != 0)
                        {
                            int insertLocation = mInsertLocation;

                            while (insertLocation > 0 && levelItem == null)
                            {
                                insertLocation--;

                                levelItem = GetItem(
                                    System.Math.Max(insertLocation, 0));
                                isLastItem = true;
                            }

                        }
                        else
                        {

                            levelItem = reorderValues.ItemMoved;
                        }

                    }

                    bool climbedToParents = false;

                    while (levelItem.Depth > item.Depth)
                    {
                        levelItem = levelItem.parentItem;
                        climbedToParents = true;
                    }

                    if (levelItem.Depth < item.Depth && levelItem != item.parentItem)
                    {
                        // It's possible that the item is the next item in the moved item's 
                        // parent's list.  In other words, if our list is:
                        // 1
                        //  a
                        //  b
                        //  c
                        // 2
                        // If the item being moved is a,b,or c, then the item could be 2.
                        // If that's the case then we'll want to move to the end of the list.
                        CollapseItem itemAbove = GetItem(mInsertLocation - 1);

                        if (itemAbove != null && itemAbove.IsChildOf(reorderValues.OldParent))
                        {
                            mInsertLocation = item.parentItem.mItems.Count - 1;
                        }
                        else
                        {
                            errorMessage = "Could not reorder list.  The new parent is not at the same depth.";
                        }
                    }
                    else if (levelItem != reorderValues.OldParent && levelItem.parentItem != reorderValues.OldParent)
                    {
                        errorMessage = "Cannot move item to a different list.";

                    }
                    else
                    {
                        int newIndex = 0;

                        if (levelItem.parentItem != null)
                        {
                            newIndex = levelItem.parentItem.mItems.IndexOf(levelItem);
                        }

                        if (climbedToParents)
                            newIndex++;

                        if (newIndex > reorderValues.OldIndex && !isLastItem)
                            newIndex--;



                        mInsertLocation = newIndex;
                    }

                    if (errorMessage == null)
                    {
                        reorderValues.OldParent.mItems.Remove(reorderValues.ItemMoved);

                        reorderValues.NewIndex = mInsertLocation;

                        reorderValues.NewIndex = System.Math.Max(0, reorderValues.NewIndex);
                        reorderValues.NewIndex = System.Math.Min(reorderValues.OldParent.mItems.Count, reorderValues.NewIndex);


                        reorderValues.OldParent.mItems.Insert(
                            reorderValues.NewIndex, reorderValues.ItemMoved);
                    }

                }
                #endregion

                #region The item being reordered has no parent
                else
                {
                    reorderValues.NewIndex = mInsertLocation;

                    foreach (CollapseItem itemForIndex in mItems)
                    {
                        int visibleIndex = GetVisibleIndex(itemForIndex);

                        if (visibleIndex < mInsertLocation)
                        {
                            continue;
                        }
                        else
                        {
                            reorderValues.NewIndex = mItems.IndexOf(itemForIndex);
                            break;
                        }
                    }

                    if (reorderValues.NewIndex > reorderValues.OldIndex)
                    {
                        reorderValues.NewIndex--;
                        reorderValues.NewIndex = System.Math.Min(reorderValues.NewIndex, mItems.Count - 1);
                    }


                    // assume for now that there are no parents involved
                    mItems.Remove(reorderValues.ItemMoved);

                    mItems.Insert(reorderValues.NewIndex, reorderValues.ItemMoved);
                }

                #endregion

                #endregion

                #region Show error if there is one to show

                if (errorMessage != null)
                {
#if SILVERLIGHT || WINDOWS_PHONE
					throw new Exception();
#else
                    GuiManager.ShowMessageBox(errorMessage, "Error reordering");
#endif
                }
                #endregion

                #region Raise the Reorder event
                if (Reorder != null &&
                    (reorderValues.OldParent != reorderValues.NewParent ||
                     reorderValues.OldIndex != reorderValues.NewIndex))
                {
                    Reorder(reorderValues);
                }
                #endregion
            }

            #endregion
        }

        void StrongSelectAtCursor(Window callingWindow)
        {
            CollapseItem ci = GetCIAt(mCursor.YForUI);

            // If the user didn't click on anything, then exit the method now
            if (ci == null)
                return;

            #region find out if we are over any icons.  Icons have special functionality

            if (ci != null)
            {
                float collapseItemXStart = mWorldUnitX - ScaleX + 1.2f + ci.Depth * .7f - .7f;
                float cursorX = mCursor.XForUI;
                float difference = cursorX - collapseItemXStart;

                int itemOn = (int)(difference / 1.5);

                // Vic says:  We don't want to raise icon clicks
                // here because if we do, then a double-click
                // will raise the event 3 times - once for each push, 
                // and once for the double-click (strong select).
                //if (difference >= 0 && itemOn < ci.icons.Count)
                //{
                //    ci.icons[itemOn].RaiseIconClick(ci, this);

                //    // if we click on an icon, we don't want to highlight anything
                //    return;
                //}
            }
            #endregion

            if (mCursor.WindowOver != mScrollBar &&
                    mHighlightedItems.Count != 0 &&
                    (mScrollBar == null || mScrollBar.Children.Contains(mCursor.WindowOver) == false) &&
                    StrongSelect != null)
                StrongSelect(this);
        }

        private void UpdateStartAtToScrollBar(Window callingWindow)
        {
            StartAt = mScrollBar.GetNumDown();
        }

        #endregion

        #region Methods

        #region Constructors

        public ListBoxBase(Cursor cursor)
            : base(cursor)
        {
            mHighlight = new Sprite();
            mHighlight.Z = AbsoluteWorldUnitZ - .0001f *
                FlatRedBall.Math.MathFunctions.ForwardVector3.Z;
            mScrollBar = new ScrollBar(mCursor);
            AddWindow(mScrollBar);

            Initialize();

        }

        public ListBoxBase(GuiSkin guiSkin, Cursor cursor)
            : base(guiSkin, cursor)
        {
            mTexts = new PositionedObjectList<Text>();
            mHighlight = new Sprite();
            mHighlight.Z = AbsoluteWorldUnitZ - .0001f *
                FlatRedBall.Math.MathFunctions.ForwardVector3.Z;
            mScrollBar = new ScrollBar(guiSkin, cursor);
            AddWindow(mScrollBar);
            Initialize();
            mScrollBar.SpriteFrame.RelativeZ = -.01f * FlatRedBall.Math.MathFunctions.ForwardVector3.Z;


            // Even though the base constructor calls SetSkin, the mTexts are not
            // created yet so their font is never set.  This set the fonts once again.

            mHighlightBar = new SpriteFrame(
                guiSkin.WindowSkin.Texture, guiSkin.WindowSkin.BorderSides);
            SpriteManager.AddSpriteFrame(mHighlightBar);
            mHighlightBar.AttachTo(SpriteFrame, false);
            mHighlightBar.RelativeZ = -HighlightBarOffset * Math.MathFunctions.ForwardVector3.Z;
            mHighlightBar.Visible = false;

            SetSkin(guiSkin);
        }

        private void Initialize()
        {
            this.Push += new GuiMessage(HighlightItemAtCursor);
            this.Push += new GuiMessage(OnPrimaryPush);
            this.Click += OnPrimaryClick;
            this.DoubleClick += StrongSelectAtCursor;

            FirstItemDistanceFromTop = 2.0f;
            DistanceBetweenLines = 2.0f;
            mLeftBorder = 1.2f;

            mScrollBar.upButton.Click += new GuiMessage(UpdateStartAtToScrollBar);
            mScrollBar.downButton.Click += new GuiMessage(UpdateStartAtToScrollBar);

        }

        //public ListBoxBase(SpriteFrame spriteFrame, Cursor cursor)
        //    : base(spriteFrame, cursor)
        //{

        //    this.Push += new GuiMessage(HighlightItemAtCursor);
        //    this.Push += new GuiMessage(OnPrimaryPush);
        //    this.Click += OnPrimaryClick;

        //    FirstItemDistanceFromTop = 2.0f;
        //    DistanceBetweenLines = 2.0f;
        //    mLeftBorder = 1.2f;
        //}

        #endregion

        #region Public Methods

        #region AddItem

        public CollapseItem AddItem(string stringToAdd)
        {
            return AddItem(stringToAdd, null);
        }


        public virtual CollapseItem AddItem(string stringToAdd, object referenceObject)
        {
            CollapseItem tempCollapseItem = new CollapseItem(stringToAdd, referenceObject);
            tempCollapseItem.mParentBox = this;
            tempCollapseItem.parentItem = null;
            mItems.Add(tempCollapseItem);

            AdjustScrollSize();

            if (GuiManagerDrawn == false && Visible)
            {
                this.UpdateTextStrings();
                UpdateTextPositions();
                UpdateSeparators();
            }

            if (mSortingStyle == Sorting.AlphabeticalIncreasing)
            {
                Sort();
            }

            return tempCollapseItem;
        }

        #endregion

#if !SILVERLIGHT

        public override void AddToLayer(Layer layerToAddTo)
        {
            if (SpriteFrame.LayerBelongingTo != null)
            {
                Layer layer = SpriteFrame.LayerBelongingTo;

                for (int i = 0; i < mTexts.Count; i++)
                {
                    layer.Remove(mTexts[i]);
                }
            }

            base.AddToLayer(layerToAddTo);

            SpriteManager.AddToLayer(mHighlightBar, layerToAddTo);

            for (int i = 0; i < mSeparators.Count; i++)
            {
                SpriteManager.AddToLayer(mSeparators[i], layerToAddTo);
            }

            if (layerToAddTo != null)
            {
                for (int i = 0; i < mTexts.Count; i++)
                {
                    TextManager.AddToLayer(mTexts[i], layerToAddTo);
                }
            }
        }
#endif
        public void AdjustScrollSize()
        {
            if (mScrollBar == null) return;

            int numOfItems = GetNumCollapsed();


            if (numOfItems == 0)
            {
                mScrollBar.Sensitivity = .1f;
                mScrollBar.View = 1;
            }
            else
            {
                mScrollBar.Sensitivity = (1 / (float)(numOfItems));
                float ratioShown = NumberOfVisibleElements / (float)numOfItems;
                if (ratioShown > 1) ratioShown = 1;
                mScrollBar.View = ratioShown;
            }

            // refreshes the startAt so it is a valid value
            StartAt = StartAt;

            mScrollBar.SetScrollPosition(StartAt);
        }

#if !SILVERLIGHT
        public override void ClearEvents()
        {
            base.ClearEvents();
            OnNewHighlight = null;
            Highlight = null;
            Reorder = null;
            GainFocus = null;
            FocusUpdate = null;
            StrongSelect = null;
            EscapeRelease = null;
        }

#endif
        public bool Contains(CollapseItem itemToSearchFor)
        {
            foreach (CollapseItem item in mItems)
            {
                if (item == itemToSearchFor || item.ContainsItem(itemToSearchFor))
                {
                    return true;
                }
            }
            return false;
        }

#if !SILVERLIGHT
        public void FillWithAllReferencedItems(IList listToFill)
        {
            foreach (CollapseItem collapseItem in mItems)
            {
                collapseItem.FillWithAllReferencedItems(listToFill);
            }
        }
#endif

        public List<object> GetHighlightedObject()
        {
            List<object> arrayListToReturn = new List<object>();

            foreach (CollapseItem ci in mHighlightedItems)
                arrayListToReturn.Add(ci.ReferenceObject);
            return arrayListToReturn;
        }

        public CollapseItem GetFirstHighlightedItem()
        {
            if (mHighlightedItems.Count != 0)
                return mHighlightedItems[0];
            else
                return null;
        }


        public object GetFirstHighlightedObject()
        {
            if (mHighlightedItems.Count != 0)
                return mHighlightedItems[0].ReferenceObject;
            else
                return null;
        }

        public CollapseItem GetItemAtCursor()
        {
            float cursorY = 0;
            float cursorX = 0;

            // get the cursor position
            if (this.GuiManagerDrawn == false)
            {
                mCursor.GetCursorPosition(out cursorX, out cursorY, AbsoluteWorldUnitZ);
            }
            else
            {
                cursorY = mCursor.YForUI;
                cursorX = mCursor.XForUI;
            }

            return GetCIAt(cursorY);
        }


        public CollapseItem GetItemByName(string itemToGet)
        {
            CollapseItem itemToReturn = null;
            for (int i = 0; i < Items.Count; i++)
            {
                itemToReturn = Items[i].GetItem(itemToGet);
                if (itemToReturn != null)
                    return itemToReturn;
            }
            return null;
        }


        public int GetNumCollapsed()
        {
            int count = 0;
            for (int i = 0; i < Items.Count; i++)
            {
                Items[i].GetCount(ref count);

            }
            return count;

        }


        public CollapseItem GetItem(object itemToGet)
        {
            CollapseItem itemToReturn = null;
            for (int i = 0; i < Items.Count; i++)
            {
                itemToReturn = Items[i].GetItem(itemToGet);
                if (itemToReturn != null)
                    return itemToReturn;
            }
            return null;
        }

#if !SILVERLIGHT

        /// <summary>
        /// Returns the CollapseItem at index itemNumber traversing from the top of the list down.
        /// </summary>
        /// <remarks>
        /// This method will skip over items that are children of collapsed items.  
        /// The list is traversed
        /// down ignoring hierarchy, but only counting "visible" items.
        /// </remarks>
        /// <param name="itemNumber">The index to return</param>
        /// <returns>The item at index itemNumber.</returns>
        public CollapseItem GetItem(int itemNumber)
        {
            int count = 0;
            CollapseItem tempItem = null;
            for (int i = 0; i < Items.Count; i++)
            {
                tempItem = Items[i].GetHighlighted(ref count, itemNumber);
                if (tempItem != null) return tempItem;

            }
            return null;
        }

#endif
        #region Highlight Methods

        public virtual void HighlightItem(string itemToHighlight)
        {
            HighlightItem(GetItemByName(itemToHighlight), false);
        }


        public void HighlightItem(CollapseItem itemToHighlight)
        {
            HighlightItem(itemToHighlight, false);
        }

        public void HighlightItem(CollapseItem itemToHighlight, bool addToHighlighted)
        {
            // store off in bools to simplify the statement.
            bool reselectingNull = itemToHighlight == null && mHighlightedItems.Count == 0;
            bool alreadySelected = mHighlightedItems.Contains(itemToHighlight);
            bool selectingSingleAndMultipleAreaAreadySelected = mHighlightedItems.Count > 1 && addToHighlighted == false;


            bool newHighlight =
                !reselectingNull &&
                !alreadySelected;

            newHighlight |= selectingSingleAndMultipleAreaAreadySelected;

            if (newHighlight)
            {
                if (addToHighlighted == false)
                {
                    mHighlightedItems.Clear();
                }

                if (itemToHighlight != null)
                {
                    mHighlightedItems.Add(itemToHighlight);
                }

                if (Highlight != null)
                {
                    Highlight(this);
                }

                if (OnNewHighlight != null)
                    OnNewHighlight(this);


                if (StrongSelectOnHighlight)
                {
                    if (StrongSelect != null)
                    {
                        StrongSelect(this);
                    }
                }
            }


            KeepHighlightInView(itemToHighlight);


            if (GuiManagerDrawn == false)
            {
                if (mHighlightBar != null)
                    UpdateHighlightSpriteFrame();

                UpdateHighlightSpriteFrame();

                UpdateTextStrings();
            }
        }


        public void HighlightItemNoCall(CollapseItem itemToHighlight, bool addToHighlighted)
        {
            if (addToHighlighted == false)
            {
                mHighlightedItems.Clear();
            }

            if (itemToHighlight != null && mHighlightedItems.Contains(itemToHighlight) == false)
            {
                mHighlightedItems.Add(itemToHighlight);
            }
        }


        public virtual void HighlightObject(object objectToHighlight)
        {
            HighlightObject(objectToHighlight, false);
        }


        public void HighlightObject(object objectToHighlight, bool addToHighlighted)
        {
            CollapseItem itemToHighlight = this.GetItem(objectToHighlight);

            HighlightItem(itemToHighlight, addToHighlighted);

        }


        public void HighlightObjectNoCall(object objectToHighlight, bool addToHighlighted)
        {
            CollapseItem itemToHighlight = this.GetItem(objectToHighlight);

            HighlightItemNoCall(itemToHighlight, addToHighlighted);

            int itemNumber = this.GetItemNumber(itemToHighlight);


            int numToDraw = NumberOfVisibleElements;

            if (itemNumber + 1 > mStartAt + numToDraw)
                mStartAt = 1 + itemNumber - numToDraw;
            else if (itemNumber < mStartAt)
                mStartAt = System.Math.Max(0, itemNumber);

            //			highlightedNum = count - startAt;
            mScrollBar.SetScrollPosition(mStartAt);

        }
        #endregion

#if !SILVERLIGHT
        #region InsertItem

        public CollapseItem InsertItem(int index, string stringToAdd)
        {
            return InsertItem(index, stringToAdd, null);
        }


        public CollapseItem InsertItem(int index, string stringToAdd, object referenceObject)
        {
            CollapseItem tempCollapseItem = new CollapseItem(stringToAdd, referenceObject);
            tempCollapseItem.mParentBox = this;
            tempCollapseItem.parentItem = null;
            mItems.Insert(index, tempCollapseItem);

            AdjustScrollSize();

            if (GuiManagerDrawn == false && Visible)
                this.UpdateTextStrings();

            return tempCollapseItem;
        }


        public CollapseItem InsertItem(int index, CollapseItem itemToInsert)
        {
            itemToInsert.mParentBox = this;
            itemToInsert.parentItem = null;
            mItems.Insert(index, itemToInsert);

            AdjustScrollSize();

            if (GuiManagerDrawn == false && Visible)
                this.UpdateTextStrings();

            return itemToInsert;

        }

        #endregion


        public bool IsVisibleInBox(CollapseItem ci)
        {
            int itemNumber = this.GetItemNumber(ci);

            return itemNumber >= this.mStartAt &&
                itemNumber < mStartAt + NumberOfVisibleElements;
        }

#endif

        public void LoseFocus()
        {
            // Needed just to meet IInterface requirements
        }

        public void ReceiveInput()
        {
            // Need to fix this later

            if (InputManager.Keyboard.KeyTyped(Keys.Up))
            {
                if (mHighlightedItems.Count > 0)
                {
                    int index = GetVisibleIndex(mHighlightedItems[0]);

                    while (true)
                    {
                        index--;

                        if (index < 0)
                            break;

                        CollapseItem itemAtIndexAbove = GetNthVisibleItem(index);

                        if (itemAtIndexAbove != null)
                        {
                            if (itemAtIndexAbove.Enabled)
                            {
                                HighlightItem(itemAtIndexAbove);
                                break;
                            }
                            // else continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            if (InputManager.Keyboard.KeyTyped(Keys.Down))
            {
                if (mHighlightedItems.Count > 0)
                {
                    int index = GetVisibleIndex(mHighlightedItems[0]);

                    while (true)
                    {
                        index++;

                        // No need to check the value out of bounds - it'll check automatically
                        // in the GetNthVisibleItem

                        CollapseItem itemAtIndexAbove = GetNthVisibleItem(index);

                        if (itemAtIndexAbove != null)
                        {
                            if (itemAtIndexAbove.Enabled)
                            {
                                HighlightItem(itemAtIndexAbove);
                                break;
                            }
                            // else continue;
                        }
                        else
                        {
                            break;
                        }
                    }

                }
            }


#if FRB_MDX
            if (InputManager.Keyboard.KeyPushed(Keys.Return) || InputManager.Keyboard.KeyPushed(Keys.NumPadEnter))
#else
            if (InputManager.Keyboard.KeyPushed(Keys.Enter))
#endif
            {
                if (this.StrongSelect != null)
                    this.StrongSelect(this);
            }

            if (InputManager.Keyboard.KeyReleased(Keys.Escape) && this.EscapeRelease != null)
                EscapeRelease(this);
        }
#if !SILVERLIGHT

        public void RemoveCollapseItem(CollapseItem item)
        {
            if (mHighlightedItems.Contains(item))
            {
                mHighlightedItems.Remove(item);
            }

            #region Fix StartAt
            int numOfCollapsedItems = GetNumCollapsed();
            if (mStartAt + mScaleY - 1 > numOfCollapsedItems) mStartAt = numOfCollapsedItems - (int)(mScaleY - 1);
            if (mStartAt < 0) mStartAt = 0;
            #endregion

            item.RemoveSelf();

            AdjustScrollSize();

            UpdateTextStrings();

            if (!GuiManagerDrawn)
            {
                UpdateSeparators();
            }
        }


        public CollapseItem RemoveItemByObject(object objectToRemove)
        {
            CollapseItem itemToRemove = GetItem(objectToRemove);
            RemoveCollapseItem(itemToRemove);

            return itemToRemove;
        }

        public CollapseItem RemoveItemAt(int index)
        {
            CollapseItem tempItem = mItems[index];
            tempItem.RemoveSelf();

            UpdateTextStrings();

            if (!GuiManagerDrawn)
            {
                UpdateSeparators();
            }

            return tempItem;
        }

        /// <summary>
        /// Removes the item from the CollapseListBox.
        /// </summary>
        /// <remarks>
        /// This method removes the item from the CollapseListBox and detaches the item's children and
        /// reattaches them to the box.  The assumption here is that the item will not be reattached.
        /// </remarks>
        /// <param name="itemToRemove"></param>
        /// <returns></returns>
        public CollapseItem RemoveItemByName(string itemToRemove)
        {
            CollapseItem item = GetItemByName(itemToRemove);
            RemoveCollapseItem(item);
            return item;
        }

#endif

        public void Sort()
        {

            if (Items.Count == 0 || Items.Count == 1)
                return;

            int whereItemBelongs;

            for (int i = 1; i < Items.Count; i++)
            {

                if (!string.IsNullOrEmpty(mItems[i].Text) && !string.IsNullOrEmpty(mItems[i - 1].Text) &&
                    Items[i].Text.CompareTo(Items[i - 1].Text) < 0)
                {
                    if (i == 1)
                    {
                        Items.Insert(0, Items[i]);
                        Items.RemoveAt(i + 1);
                        continue;
                    }

                    for (whereItemBelongs = i - 2; whereItemBelongs > -1; whereItemBelongs--)
                    {
                        if (Items[i].Text.CompareTo(Items[whereItemBelongs].Text) > 0 || Items[i].Text.CompareTo(Items[whereItemBelongs].Text) == 0)
                        {
                            Items.Insert(whereItemBelongs + 1, Items[i]);
                            Items.RemoveAt(i + 1);
                            break;
                        }
                        else if (whereItemBelongs == 0 && Items[i].Text.CompareTo(Items[0].Text) < 0)
                        {
                            Items.Insert(0, Items[i]);
                            Items.RemoveAt(i + 1);
                            break;
                        }
                    }
                }
            }
        }


        protected void UpdateTextPositions()
        {
            float startY = (float)(SpriteFrame.Y + SpriteFrame.ScaleY - FirstItemDistanceFromTop);
            for (int j = StartAt; j < System.Math.Min(StartAt + this.NumberOfVisibleElements, mItems.Count); j++)
            {
                if (j - StartAt >= mTexts.Count)
                {
                    mTexts.Add(TextManager.AddText("", SpriteFrame.LayerBelongingTo));
                    mTexts[j - StartAt].AttachTo(this.SpriteFrame, false);
                    mTexts[j - StartAt].RelativeZ = -MathFunctions.ForwardVector3.Z * TextZOffset;

                    mTexts[j - StartAt].AdjustPositionForPixelPerfectDrawing = false;
#if FRB_MDX
                    mTexts[j - StartAt].ColorOperation = Microsoft.DirectX.Direct3D.TextureOperation.Modulate;
#else
                    mTexts[j - StartAt].ColorOperation = ColorOperation.Modulate;

#endif
                    mTexts[j - StartAt].DisplayText = mItems[j].Text;

                    if (this.ScrollBarVisible == true)
                        mTexts[j - StartAt].MaxWidth = 2 * (float)mScaleX - mLeftBorder * 2 - 2;
                    else
                        mTexts[j - StartAt].MaxWidth = 2 * (float)mScaleX - mLeftBorder * 2;

                }

                if (mHorizontalAlignment == HorizontalAlignment.Center)
                {
                    mTexts[j - StartAt].RelativeX = (0 + mLeftBorder);
                }
                else
                {
                    mTexts[j - StartAt].RelativeX = (-SpriteFrame.ScaleX + mLeftBorder);

                }
                mTexts[j - StartAt].RelativeY = startY - SpriteFrame.Y;

                startY -= DistanceBetweenLines;
            }
        }

        #endregion

        #region Protected Methods


        protected CollapseItem GetNthVisibleItem(int number)
        {
            int countedSoFar = 0;

            foreach (CollapseItem item in mItems)
            {
                CollapseItem nthItem = item.GetNthVisibleItem(ref countedSoFar, number);

                if (nthItem != null)
                    return nthItem;
            }

            return null;
        }

        protected float GetAbsoluteScreenY(CollapseItem collapseItem)
        {
            int index = GetVisibleIndex(collapseItem);

            float startingY = mWorldUnitY + this.ScaleY - this.FirstItemDistanceFromTop;

            return startingY - (index - mStartAt) * mDistanceBetweenLines;
        }

        protected int GetVisibleIndex(CollapseItem collapseItem)
        {
            int countedSoFar = 0;

            foreach (CollapseItem item in mItems)
            {
                int index = item.GetVisibleIndex(ref countedSoFar, collapseItem);

                if (index != -1)
                    return index;
            }

            return -1;
        }

        internal protected void KeepHighlightInView(CollapseItem itemToHighlight)
        {
            int itemNumber = this.GetItemNumber(itemToHighlight);


            int numToDraw = NumberOfVisibleElements;

            if (itemNumber + 1 > mStartAt + numToDraw)
                mStartAt = 1 + itemNumber - numToDraw;
            else if (itemNumber > -1 && itemNumber < mStartAt)
                mStartAt = System.Math.Max(0, itemNumber);

            mScrollBar.SetScrollPosition(mStartAt);
        }
#if !SILVERLIGHT

        protected void OnHighlight()
        {
            if (Highlight != null)
                Highlight(this);
        }

        public override void SetSkin(GuiSkin guiSkin)
        {
            ListBoxSkin listBoxSkin = guiSkin.ListBoxSkin;
            mSeparatorSkin = guiSkin.ListBoxSkin.SeparatorSkin;
            mLeftBorder = listBoxSkin.HorizontalTextOffset;
            SetFromWindowSkin(listBoxSkin);


            mSpacing = listBoxSkin.TextSpacing;
            mScale = listBoxSkin.TextScale;
            // Use the property so the scroll bar gets updated
            DistanceBetweenLines = listBoxSkin.DistanceBetweenLines;
            FirstItemDistanceFromTop = listBoxSkin.FirstItemDistanceFromTop;
            Font = listBoxSkin.Font;

            mTextRed = listBoxSkin.Red;
            mTextGreen = listBoxSkin.Green;
            mTextBlue = listBoxSkin.Blue;

            mDisabledTextRed = listBoxSkin.DisabledRed;
            mDisabledTextGreen = listBoxSkin.DisabledGreen;
            mDisabledTextBlue = listBoxSkin.DisabledBlue;


            mHighlightBarHorizontalBuffer = listBoxSkin.HighlightBarSkin.HighlightBarHorizontalBuffer;

            if (mHighlightBar != null)
            {
                mHighlightBar.ScaleY = listBoxSkin.HighlightBarSkin.ScaleY;
                mHighlightBar.RelativeX = listBoxSkin.HighlightBarSkin.HorizontalOffset;
            }

            if (mScrollBar != null)
            {
                mScrollBar.SetSkin(guiSkin);

                // If the scroll bar isn't null then the mHighlightBar
                // should be a valid reference too.
                mHighlightBar.Texture = listBoxSkin.HighlightBarSkin.Texture;
                mHighlightBar.Borders = listBoxSkin.HighlightBarSkin.BorderSides;
                mHighlightBar.SpriteBorderWidth = listBoxSkin.HighlightBarSkin.SpriteBorderWidth;
                mHighlightBar.TextureBorderWidth = listBoxSkin.HighlightBarSkin.TextureBorderWidth;
                mHighlightBar.ScaleX = ScaleX - mHighlightBarHorizontalBuffer;

                // also the separators
                UpdateSeparators();
            }

            UpdateTextStrings();
        }
#endif
        protected void UpdateHighlightSpriteFrame()
        {
            if (mHighlightBar != null)
            {
                if (mHighlightedItems.Count == 0)
                    mHighlightBar.Visible = false;
                else if (mHighlightedItems.Count != 0)
                {
                    int relativeHighlight = mItems.IndexOf(mHighlightedItems[0]) - StartAt;

                    if (relativeHighlight > -1 && relativeHighlight < NumberOfVisibleElements)
                    {
                        mHighlightBar.Visible = true;
                        mHighlightBar.RelativeY = ScaleY - FirstItemDistanceFromTop - (DistanceBetweenLines * relativeHighlight);
                    }
                    else
                    {
                        mHighlightBar.Visible = false;
                    }
                }
            }
        }

        protected internal void UpdateSeparators()
        {
            if (mSeparatorSkin == null)
            {
                return;
            }

            int requiredSeparatorCount = 0;

            if (mSeparatorSkin != null && mItems.Count != 0)
            {
                requiredSeparatorCount =
                    System.Math.Min(mItems.Count, NumberOfVisibleElements) + mSeparatorSkin.ExtraSeparators;
            }

            while (mSeparators.Count < requiredSeparatorCount)
            {
                // There aren't enough separators, so let's add them
                SpriteFrame newSeparator = new SpriteFrame(
                    mSeparatorSkin.Texture, mSeparatorSkin.BorderSides);

                SpriteManager.AddSpriteFrame(newSeparator);
                if (SpriteFrame.LayerBelongingTo != null)
                {
                    SpriteManager.AddToLayer(newSeparator, SpriteFrame.LayerBelongingTo);
                }

                int indexOfNewSeparator = mSeparators.Count;
                mSeparators.Add(newSeparator);

                newSeparator.AttachTo(SpriteFrame, false);
                newSeparator.RelativeZ =
                    -MathFunctions.ForwardVector3.Z * SeparatorZOffset;
            }

            while (mSeparators.Count > requiredSeparatorCount)
            {
                // There are too many separators, so remove some
                SpriteManager.RemoveSpriteFrame(mSeparators.Last);
            }

            UpdateSeparatorProperties();

        }

        protected internal void UpdateTextStrings()
        {
            //           float startY = (float)(si.Y + si.ScaleY - FirstItemDistanceFromTop);

            if (mTexts != null)
            {
                foreach (Text t in mTexts)
                    t.DisplayText = "";

                // make sure there aren't more Texts than items in the list
                while (Count < mTexts.Count)
                    mTexts.RemoveAt(0);

                for (int j = StartAt; j < System.Math.Min(StartAt + this.NumberOfVisibleElements, mItems.Count); j++)
                {
                    if (j - StartAt >= mTexts.Count)
                    {
                        mTexts.Add(TextManager.AddText(mItems[j].Text, SpriteFrame.LayerBelongingTo));
                        mTexts[j - StartAt].AdjustPositionForPixelPerfectDrawing = false;
#if FRB_MDX
                        mTexts[j - StartAt].ColorOperation = Microsoft.DirectX.Direct3D.TextureOperation.Modulate;
#else
                        mTexts[j - StartAt].ColorOperation = ColorOperation.Modulate ;
#endif
                        mTexts[j - StartAt].AttachTo(SpriteFrame, false);
                        mTexts[j - StartAt].RelativeZ = -MathFunctions.ForwardVector3.Z * TextZOffset;

                        if (Font != null)
                            mTexts[j - StartAt].Font = Font;
                    }

                    if (ScrollBarVisible == true)
                        mTexts[j - StartAt].MaxWidth = 2 * (float)mScaleX - mLeftBorder * 2 - 2;
                    else
                        mTexts[j - StartAt].MaxWidth = 2 * (float)mScaleX - mLeftBorder * 2;

                    if (mItems[j].Enabled)
                    {
                        mTexts[j - StartAt].Red = mTextRed;
                        mTexts[j - StartAt].Green = mTextGreen;
                        mTexts[j - StartAt].Blue = mTextBlue;
                    }
                    else
                    {
                        mTexts[j - StartAt].Red = mDisabledTextRed;
                        mTexts[j - StartAt].Green = mDisabledTextGreen;
                        mTexts[j - StartAt].Blue = mDisabledTextBlue;
                    }

                    mTexts[j - StartAt].HorizontalAlignment = mHorizontalAlignment;
                    mTexts[j - StartAt].Spacing = mSpacing;
                    mTexts[j - StartAt].Scale = mScale;

                    mTexts[j - StartAt].DisplayText = mItems[j].Text;
                }
            }
        }


        #endregion

        #region Internal Methods

        public override void Activity(Camera camera)
        {
            if (GuiManagerDrawn == false)
            {
                base.Activity(camera); // for the SpriteFrame

                UpdateTextPositions();
            }
        }


        internal override void Destroy()
        {
            Destroy(false);
        }


        internal protected override void Destroy(bool keepEvents)
        {
            while (mTexts != null && mTexts.Count != 0)
            {
                TextManager.RemoveText(mTexts.Last);
            }

            while (mSeparators.Count != 0)
            {
                SpriteManager.RemoveSpriteFrame(mSeparators.Last);
            }

            if (this.HighlightBar != null)
            {
                SpriteManager.RemoveSpriteFrame(HighlightBar);
            }

            base.Destroy(keepEvents);
        }

#if !SILVERLIGHT

        internal override void DrawSelfAndChildren(Camera camera)
        {

            base.DrawSelfAndChildren(camera);

            #region Highlighted

            float xPos = TITLE_BAR_X_SHIFT + (float)mWorldUnitX;

            for (int i = 0; i < mItems.Count; i++)
            {
                mItems[i].TurnOffDrawHighlighted();
            }

            foreach (CollapseItem ci in mHighlightedItems)
            {
                if (IsVisibleInBox(ci))
                {
                    ci.mDrawHighlighted = true;
                    int highlightedNum = GetItemNumber(ci) - mStartAt;

                    mHighlight.Y = mScaleY - 2 - mDistanceBetweenLines * highlightedNum + mWorldUnitY;

#if FRB_MDX
                    StaticVertices[0].Color = StaticVertices[1].Color = StaticVertices[2].Color = StaticVertices[3].Color = StaticVertices[4].Color = StaticVertices[5].Color = 0xff000000;
#else
                    StaticVertices[0].Color.PackedValue = StaticVertices[1].Color.PackedValue = StaticVertices[2].Color.PackedValue =
                        StaticVertices[3].Color.PackedValue = StaticVertices[4].Color.PackedValue = StaticVertices[5].Color.PackedValue = 0xff000000;

#endif

                    StaticVertices[0].Position.Z = StaticVertices[1].Position.Z = StaticVertices[2].Position.Z =
                        StaticVertices[3].Position.Z = StaticVertices[4].Position.Z = StaticVertices[5].Position.Z =
                        camera.Z + FlatRedBall.Math.MathFunctions.ForwardVector3.Z * 100;
                    StaticVertices[0].Position.X = xPos - (float)mHighlight.ScaleX;
                    StaticVertices[0].Position.Y = (float)(mHighlight.Y - mHighlight.ScaleY);
                    StaticVertices[0].TextureCoordinate.X = .0234375f;
                    StaticVertices[0].TextureCoordinate.Y = .65f;

                    StaticVertices[1].Position.X = xPos - (float)mHighlight.ScaleX;
                    StaticVertices[1].Position.Y = (float)(mHighlight.Y + mHighlight.ScaleY);
                    StaticVertices[1].TextureCoordinate.X = .0234375f;
                    StaticVertices[1].TextureCoordinate.Y = .65f;

                    StaticVertices[2].Position.X = xPos + (float)mHighlight.ScaleX;
                    StaticVertices[2].Position.Y = (float)(mHighlight.Y + mHighlight.ScaleY);
                    StaticVertices[2].TextureCoordinate.X = .02734375f;
                    StaticVertices[2].TextureCoordinate.Y = .66f;

                    StaticVertices[3] = StaticVertices[0];
                    StaticVertices[4] = StaticVertices[2];

                    StaticVertices[5].Position.X = xPos + (float)mHighlight.ScaleX;
                    StaticVertices[5].Position.Y = (float)(mHighlight.Y - mHighlight.ScaleY);
                    StaticVertices[5].TextureCoordinate.X = .02734375f;
                    StaticVertices[5].TextureCoordinate.Y = .66f;

                    GuiManager.WriteVerts(StaticVertices);
                }

            }
            #endregion

            #region Draw the CollapseItems
            float startY = FirstItemScreenY;
            int numToDraw = NumberOfVisibleElements;
            int numDrawn = 0;
            int itemNum = 0;
            for (int j = 0; j < Items.Count; j++)
            {
                bool isHighlighted = mHighlightedItems.Contains(Items[j]);

                Items[j].Draw(camera, startY,
                    mWorldUnitX - mScaleX + 1.2f,
                    TextScaleX * 2, 0, ref itemNum,
                    ref numDrawn, mStartAt, numToDraw, mDistanceBetweenLines);
                if (numDrawn > numToDraw - 1) break;
            }

            #endregion

            #region Draw the Lines

            startY = (float)(mWorldUnitY + mScaleY - 2.0f);

            float highlightScaleX = ScaleX - mHighlightBarHorizontalBuffer;

            // .135 is too low
            const float lineThickness = .137f;
            // .14 is too high

            if (Lined)
            {
                for (int j = mStartAt; j < GetNumCollapsed(); j++)
                {
                    startY -= 2;
                    if (startY < mWorldUnitY - mScaleY + .5f) break;

                    StaticVertices[0].Position.X = xPos - highlightScaleX;
                    StaticVertices[0].Position.Y = (startY + .95f);
                    StaticVertices[0].TextureCoordinate.X = 0f;
                    StaticVertices[0].TextureCoordinate.Y = .905f;


                    StaticVertices[1].Position.X = xPos - highlightScaleX;
                    StaticVertices[1].Position.Y = (startY + .95f + lineThickness);
                    StaticVertices[1].TextureCoordinate.X = .0f;
                    StaticVertices[1].TextureCoordinate.Y = .904f;

                    StaticVertices[2].Position.X = xPos + highlightScaleX;
                    StaticVertices[2].Position.Y = (startY + .95f + lineThickness);
                    StaticVertices[2].TextureCoordinate.X = .001f;
                    StaticVertices[2].TextureCoordinate.Y = .904f;

                    StaticVertices[3] = StaticVertices[0];
                    StaticVertices[4] = StaticVertices[2];

                    StaticVertices[5].Position.X = xPos + highlightScaleX;
                    StaticVertices[5].Position.Y = (startY + .95f);
                    StaticVertices[5].TextureCoordinate.X = .001f;
                    StaticVertices[5].TextureCoordinate.Y = .905f;

                    GuiManager.WriteVerts(StaticVertices);
                }
            }
            #endregion

            #region Draw the insert location (if visible)



            if (mInsertLocation != -1)
            {
                startY = (float)(mWorldUnitY + mScaleY - 2.0f) - mDistanceBetweenLines * (mInsertLocation - mStartAt);


                StaticVertices[0].Position.X = xPos - highlightScaleX;
                StaticVertices[0].Position.Y = (startY + .91f);
                StaticVertices[0].TextureCoordinate.X = 0f;
                StaticVertices[0].TextureCoordinate.Y = .901f;

                StaticVertices[1].Position.X = xPos - highlightScaleX;
                StaticVertices[1].Position.Y = (startY + 1.19f);
                StaticVertices[1].TextureCoordinate.X = .0f;
                StaticVertices[1].TextureCoordinate.Y = .90f;

                StaticVertices[2].Position.X = xPos + highlightScaleX;
                StaticVertices[2].Position.Y = (startY + 1.19f);
                StaticVertices[2].TextureCoordinate.X = .001f;
                StaticVertices[2].TextureCoordinate.Y = .90f;

                StaticVertices[3] = StaticVertices[0];
                StaticVertices[4] = StaticVertices[2];

                StaticVertices[5].Position.X = xPos + highlightScaleX;
                StaticVertices[5].Position.Y = (startY + .91f);
                StaticVertices[5].TextureCoordinate.X = .001f;
                StaticVertices[5].TextureCoordinate.Y = .901f;

                GuiManager.WriteVerts(StaticVertices);
            }
            #endregion

        }


        internal override int GetNumberOfVerticesToDraw()
        {
            int vertexCount = base.GetNumberOfVerticesToDraw(); // base


            int numberOfVerticesForLine = 0;
            if (Lined)
                numberOfVerticesForLine = 6;

            foreach (CollapseItem ci in mHighlightedItems)
            {
                if (this.IsVisibleInBox(ci))
                    vertexCount += 6;
            }
            //			if(highlightedNum != -1)
            //				i += 6;

            float startY = (float)(mWorldUnitY + mScaleY) - 2.0f;
            int numToDraw = (int)(mScaleY - .25f);
            int numDrawn = 0;
            int itemNum = 0;

            for (int j = 0; j < Items.Count; j++)
            {
                vertexCount += Items[j].GetNumberOfVerticesToDraw(GuiManager.Camera, startY,
                    mWorldUnitX - mScaleX + 1.2f,
                    TextScaleX * 2, 0, ref itemNum,
                    ref numDrawn, mStartAt, numToDraw);

                if (numDrawn > numToDraw - 1) break;

            }

            startY = (mWorldUnitY + mScaleY - 2);

            for (int j = mStartAt; j < GetNumCollapsed(); j++)
            {

                startY -= 2;
                if (startY < mWorldUnitY - mScaleY + .5f) break;
                if (numberOfVerticesForLine != 0)
                    vertexCount += numberOfVerticesForLine;

            }


            if (mInsertLocation != -1)
            {
                vertexCount += 6;
            }

            return vertexCount;
        }
#endif

        public override void TestCollision(Cursor cursor)
        {
            base.TestCollision(cursor);

            #region Make sure that we're not highlighting items that don't exist

            for (int i = mHighlightedItems.Count - 1; i > -1; i--)
            {
                if (!Contains(mHighlightedItems[i]))
                {
                    mHighlightedItems.RemoveAt(i);
                }
            }

            #endregion

            #region Highlight item at cursor if HiglightOnRollOver
            if (HighlightOnRollOver && (cursor.XVelocity != 0 || cursor.YVelocity != 0) &&
                cursor.WindowOver == this)
            {
                HighlightItemAtCursor(this);
            }
            #endregion

            #region Show the tool tip if CurrentToolTipOption is CursorOver
            if (CurrentToolTipOption == ToolTipOption.CursorOver &&
                !mCursor.IsOn((IWindow)mScrollBar))
            {
                CollapseItem itemOver = GetItemAtCursor();
                if (itemOver != null)
                {
                    GuiManager.ToolTipText = itemOver.Text;
                }
            }
            #endregion

            #region Move the scroll bar if the mouse wheel is moving
            if (cursor.ZVelocity != 0 && mScrollBar != null)
            {
                if (cursor.ZVelocity > 0)
                    mScrollBar.upButton.OnClick();
                else
                    mScrollBar.downButton.OnClick();
            }
            #endregion

            #region If the user's grabbed and dropped a CollapseItem, raise the event

            if (mCursor.PrimaryClick && GuiManager.CollapseItemDraggedOff != null &&
                GuiManager.CollapseItemDraggedOff.parentBox != this)
            {
                if (this.CollapseItemDropped != null)
                {
                    this.CollapseItemDropped(this);
                }
            }

            #endregion

            #region Regulate the mStartAt property according to the number of elements in the list

            int numOfCollapsedItems = GetNumCollapsed();
            if (mStartAt + NumberOfVisibleElements > numOfCollapsedItems) mStartAt = numOfCollapsedItems - NumberOfVisibleElements;
            if (mStartAt < 0) mStartAt = 0;

            #endregion

            SetInsertLocationIndex();

        }
        #endregion

        #region Private Methods


        internal CollapseItem GetCIAt(float yPos)
        {
            float worldY = mWorldUnitY;
            if (SpriteFrame != null)
                worldY = SpriteFrame.Y;

            float bottom = worldY + mScaleY - mFirstItemDistanceFromTop - ((NumberOfVisibleElements - 1) * mDistanceBetweenLines) - 1;

            int highlightedNum = -1;

            if (yPos > bottom)
                highlightedNum = mStartAt + (int)((worldY + mScaleY - 1 - yPos) / mDistanceBetweenLines);

            return GetItem(highlightedNum);
        }


        private int GetItemNumber(CollapseItem item)
        {
            int count = 0;
            for (int i = 0; i < mItems.Count; i++)
            {
                if (mItems[i].GetItemNum(ref count, item)) return count;
            }
            return -1;
        }

        private void SetInsertLocationIndex()
        {
            int index = -1;

            #region Find out if the insert location line should be shown and store in shouldShow

            bool shouldShow = true;

            shouldShow = mAllowReordering &&
                GuiManager.mCollapseItemDraggedOff != null &&
                // Make sure that the user has clicked and dragged at least
                // 1 unit before showing the insert location:
                System.Math.Abs(mYPositionPushed - mCursor.YForUI) > 1;

            if (shouldShow)
            {
                index = GetVisibleIndex(GuiManager.mCollapseItemDraggedOff);

            }
            #endregion

            #region If shouldShow, set the mInsertLocation;

            if (shouldShow && index != -1)
            {
                float cursorY = 0;
                float cursorX = 0;

                // get the cursor position
                if (this.GuiManagerDrawn == false)
                {
                    mCursor.GetCursorPosition(out cursorX, out cursorY, AbsoluteWorldUnitZ);
                }
                else
                {
                    cursorY = mCursor.YForUI;
                    cursorX = mCursor.XForUI;
                }

                // Pull the cursor down half a line:
                cursorY -= 1.0f;

                float bottom = mWorldUnitY + mScaleY - mFirstItemDistanceFromTop - ((NumberOfVisibleElements - 1) * mDistanceBetweenLines) - 1;

                mInsertLocation = mStartAt + (int)((mWorldUnitY + mScaleY - 1 - cursorY) / mDistanceBetweenLines);

                mInsertLocation = System.Math.Min(mInsertLocation, mStartAt + NumberOfVisibleElements);
            }
            #endregion

            #region Else, hide
            else
            {
                mInsertLocation = -1;
            }
            #endregion
        }


        private void UpdateSeparatorProperties()
        {
            for (int i = 0; i < mSeparators.Count; i++)
            {
                SpriteFrame separator = mSeparators[i];

                separator.ScaleX = this.ScaleX;
                separator.ScaleY = mSeparatorSkin.ScaleY;

                separator.RelativeX = 0;
                separator.RelativeY = SpriteFrame.ScaleY + mSeparatorSkin.HorizontalOffset -
                    (.5f + i) * mDistanceBetweenLines;

                separator.SpriteBorderWidth = mSeparatorSkin.SpriteBorderWidth;
                separator.TextureBorderWidth = mSeparatorSkin.TextureBorderWidth;

                separator.Texture = mSeparatorSkin.Texture;
                separator.Borders = mSeparatorSkin.BorderSides;
            }
        }


        #endregion
        #endregion
    }
}
