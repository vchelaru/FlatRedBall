using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Graphics.Animation;
using FlatRedBall;

namespace EditorObjects.Gui
{
    public class AnimationChainPropertyGrid : Window, IObjectDisplayer<AnimationChain>
    {
        #region Fields

        Button mFlipHorizontally;
        Button mFlipVertically;

        UpDown mSetRelativeX;
        UpDown mSetRelativeY;

        Sprite mDisplayingSprite;

        PropertyGrid<AnimationChain> mPropertyGrid;

        TextBox mNameTextBox;

        #endregion

        #region Properties

        public AnimationChain SelectedObject
        {
            get { return mPropertyGrid.SelectedObject; }
            set { mPropertyGrid.SelectedObject = value; }
        }

        public Sprite DisplayingSprite
        {
            get { return mDisplayingSprite; }
            set 
            { 
                mDisplayingSprite = value;

                UpdateRoundingOnUpDowns();
            }
        }

        #endregion

        #region Event Methods

        private void EveryFrameActivity(Window callingWindow)
        {
            #region Update the UpDown values to the first AnimationFrame's relative values
            if (SelectedObject != null && SelectedObject.Count != 0)
            {
                if (FlatRedBall.Input.InputManager.ReceivingInput != mSetRelativeX.textBox &&
                    mCursor.WindowPushed != mSetRelativeX.UpDownButton)
                {
                    mSetRelativeX.CurrentValue = SelectedObject[0].RelativeX;
                }

                if (FlatRedBall.Input.InputManager.ReceivingInput != mSetRelativeY.textBox &&
                    mCursor.WindowPushed != mSetRelativeY.UpDownButton)
                {
                    mSetRelativeY.CurrentValue = SelectedObject[0].RelativeY;
                }
            }
            #endregion

            UpdateRoundingOnUpDowns();
        }

        private void FlipHorizontallyClick(Window callingWindow)
        {
            if (SelectedObject == null)
            {
                return;
            }

            foreach (AnimationFrame animationFrame in SelectedObject)
            {
                animationFrame.FlipHorizontal = !animationFrame.FlipHorizontal;
            }

            OkCancelWindow okCancelWindow = GuiManager.ShowOkCancelWindow(
                "Invert RelativeX values?", "Invert RelativeX?");
            okCancelWindow.HasMoveBar = true;
            okCancelWindow.OkClick += new GuiMessage(InvertRelativeXValues);


        }

        void InvertRelativeXValues(Window callingWindow)
        {
            foreach (AnimationFrame animationFrame in SelectedObject)
            {
                animationFrame.RelativeX *= -1;
            }
        }

        private void FlipVerticallyClick(Window callingWindow)
        {
            if (SelectedObject == null)
            {
                return;
            }
            foreach (AnimationFrame animationFrame in SelectedObject)
            {
                animationFrame.FlipVertical = !animationFrame.FlipVertical;
            }

            OkCancelWindow okCancelWindow = GuiManager.ShowOkCancelWindow(
                "Invert RelativeY values?", "Invert RelativeY?");
            okCancelWindow.HasMoveBar = true;
            okCancelWindow.OkClick += new GuiMessage(InvertRelativeYValues);
        }

        void InvertRelativeYValues(Window callingWindow)
        {
            foreach (AnimationFrame animationFrame in SelectedObject)
            {
                animationFrame.RelativeY *= -1;
            }
        }

        void SetName(Window callingWindow)
        {
            if (SelectedObject != null)
            {
                SelectedObject.Name = mNameTextBox.Text;
            }
        }

        private void SetRelativeX(Window callingWindow)
        {
            if (SelectedObject == null)
            {
                return;
            }

            for (int i = 0; i < SelectedObject.Count; i++)
            {
                SelectedObject[i].RelativeX = mSetRelativeX.CurrentValue;
            }

            if (SelectedObject.Count != 0 && mDisplayingSprite != null && mDisplayingSprite.CurrentChain == SelectedObject)
            {
                mDisplayingSprite.RelativeX = SelectedObject[0].RelativeX;
            }
        }

        private void SetRelativeY(Window callingWindow)
        {
            if (SelectedObject == null)
            {
                return;
            }

            for (int i = 0; i < SelectedObject.Count; i++)
            {
                SelectedObject[i].RelativeY = mSetRelativeY.CurrentValue;
            }

            if (SelectedObject.Count != 0 && mDisplayingSprite != null && mDisplayingSprite.CurrentChain == SelectedObject)
            {
                mDisplayingSprite.RelativeY = SelectedObject[0].RelativeY;
            }
        }

        void UpdateContainedWindowPositions(Window callingWindow)
        {
            mPropertyGrid.X = mPropertyGrid.ScaleX + .25f;

            mPropertyGrid.Y = mPropertyGrid.ScaleY + 2.5f;
        }


        private void UpdateRoundingOnUpDowns()
        {
            if (mDisplayingSprite != null && 
                mDisplayingSprite.PixelSize != 0 && mSetRelativeX.RoundTo != mDisplayingSprite.PixelSize)
            {
                mSetRelativeX.RoundTo = mDisplayingSprite.PixelSize;
                mSetRelativeY.RoundTo = mDisplayingSprite.PixelSize;
            }
        }

        #endregion

        #region Methods

        #region Constructor

        public AnimationChainPropertyGrid(Cursor cursor) : base(cursor)
        {
            #region Set "this" properties
            Name = "AnimationChain Properties";

            Y = 22.8f;
            X = 82.75f;
            this.HasMoveBar = true;

            #endregion

            #region Create the "Name" UI

            TextDisplay nameDisplay = new TextDisplay(cursor);
            nameDisplay.Text = "Name:";
            this.AddWindow(nameDisplay);
            nameDisplay.SetPositionTL(1, 1.5f);

            mNameTextBox = new TextBox(cursor);
            mNameTextBox.ScaleX = 10;
            this.AddWindow(mNameTextBox);
            mNameTextBox.X = 17;
            mNameTextBox.Y = nameDisplay.Y;

            mNameTextBox.TextChange += SetName;

            #endregion

            #region Create the PropertyGrid

            mPropertyGrid = new PropertyGrid<AnimationChain>(cursor);
            mPropertyGrid.HasMoveBar = false;
            AddWindow(mPropertyGrid);
            this.mPropertyGrid.AfterUpdateDisplayedProperties += EveryFrameActivity;
            mPropertyGrid.DrawBorders = false;
            
            #endregion

            #region ExcludeMember calls

            // We'll handle this property in "this"
            mPropertyGrid.ExcludeMember("Name");

            mPropertyGrid.ExcludeMember("Capacity");
            mPropertyGrid.ExcludeMember("Item");
            mPropertyGrid.ExcludeMember("ColorKey");
            mPropertyGrid.ExcludeMember("ParentFileName");
            mPropertyGrid.ExcludeMember("LastFrame");

            mPropertyGrid.ExcludeMember("IndexInLoadedAchx");
            mPropertyGrid.ExcludeMember("ParentAchxFileName");
            mPropertyGrid.ExcludeMember("ParentGifFileName");

            #endregion

            #region mFlipHorizontally Creation

            mFlipHorizontally = new Button(mCursor);
            mFlipHorizontally.Text = "Flip All\nHorizontally";
            mFlipHorizontally.ScaleX = 6;
            mFlipHorizontally.ScaleY = 2.2f;
            mFlipHorizontally.Click += FlipHorizontallyClick;

            mPropertyGrid.AddWindow(mFlipHorizontally, "Actions");

            #endregion

            #region mFlipVertically Creation

            mFlipVertically = new Button(mCursor);
            mFlipVertically.Text = "Flip All\nVertically";
            mFlipVertically.ScaleX = 6;
            mFlipVertically.ScaleY = 2.2f;
            mFlipVertically.Click += FlipVerticallyClick;

            mPropertyGrid.AddWindow(mFlipVertically, "Actions");
            
            #endregion

            #region Categorize and modify Frames UI

            mPropertyGrid.IncludeMember("FrameTime", "Frames");

            ((UpDown)mPropertyGrid.GetUIElementForMember("FrameTime")).MinValue = 0;
            ((UpDown)mPropertyGrid.GetUIElementForMember("FrameTime")).CurrentValue = .1f;

            mPropertyGrid.IncludeMember("TotalLength", "Frames");
            mPropertyGrid.IncludeMember("Count", "Frames");

            #endregion

            mPropertyGrid.RemoveCategory("Uncategorized");
           // mPropertyGrid.ResizeEnd += UpdateThisScale;
            CreateUpDowns();
            mPropertyGrid.SelectCategory("Actions");
            UpdateContainedWindowPositions(null);
        }

        #endregion

        #region Public Methods

        public void SetOptionsForName(List<string> options)
        {
            this.mPropertyGrid.SetOptionsForMember("Name", options, false);
        }

        #endregion

        #region Private Methods

        private void CreateUpDowns()
        {
            mSetRelativeX = new UpDown(mCursor);
            mSetRelativeX.ValueChanged += SetRelativeX;
            mSetRelativeX.ScaleX = 6;
            mSetRelativeX.Sensitivity = .07f;
            mPropertyGrid.AddWindow(mSetRelativeX, "Actions");
            mPropertyGrid.SetLabelForWindow(mSetRelativeX, "Set all Relative X");


            mSetRelativeY = new UpDown(mCursor);
            mSetRelativeY.ValueChanged += SetRelativeY;
            mSetRelativeY.ScaleX = 6;
            mSetRelativeY.Sensitivity = .07f;
            mPropertyGrid.AddWindow(mSetRelativeY, "Actions");
            mPropertyGrid.SetLabelForWindow(mSetRelativeY, "Set all Relative Y");
        }

        #endregion

        #endregion

        #region IObjectDisplayer<AnimationChain> Members

        AnimationChain IObjectDisplayer<AnimationChain>.ObjectDisplaying
        {
            get
            {
                return mPropertyGrid.ObjectDisplaying;
            }
            set
            {
                mPropertyGrid.ObjectDisplaying = value;
            }
        }

        #endregion

        #region IObjectDisplayer Members

        object IObjectDisplayer.ObjectDisplayingAsObject
        {
            get
            {
                return mPropertyGrid.ObjectDisplaying;
            }
            set
            {
                ((IObjectDisplayer)mPropertyGrid).ObjectDisplayingAsObject = value;
            }
        }

        public void UpdateToObject()
        {
            mPropertyGrid.UpdateToObject();

            if (this.SelectedObject != null && !mNameTextBox.IsWindowOrChildrenReceivingInput)
            {
                mNameTextBox.Text = SelectedObject.Name;
            }

            this.SetScaleTL(mPropertyGrid.ScaleX + .5f, mPropertyGrid.ScaleY + 1.3f);

        }

        #endregion
    }
}
 