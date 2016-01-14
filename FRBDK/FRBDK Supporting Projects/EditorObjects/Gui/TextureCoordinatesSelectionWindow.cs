using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall.Gui;
using FlatRedBall.Math;
using FlatRedBall.Input;
using FlatRedBall;

#if FRB_XNA
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
//using Microsoft.Xna.Framework.Input;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using FlatRedBall.Math.Geometry;
#elif FRB_MDX
using Keys = Microsoft.DirectX.DirectInput.Key;
using FlatRedBall.Math.Geometry;
#endif


namespace EditorObjects.Gui
{
    public class TextureCoordinatesSelectionWindow : Window
    {
        #region Fields

        enum CursorState
        {
            DraggingCorner,
            MovingSelection,
            DraggingSides
        }



        CursorState mCursorState;

        Window mSelectedArea;
        Window mTextureDisplayArea;

        ToggleButton mPixelPerfect;
        TextDisplay mMinimumXSelectionLabel;
        TextDisplay mMinimumYSelectionLabel;
        ComboBox mMinimumXSelection;
        ComboBox mMinimumYSelection;

        Sides mSideOver = Sides.None;
        //ComboBox mPixelPerfectComboBox;

        Button mAddToListButton;

        float originalClickX;
        float originalClickY;

        float mLeftTU = 0;
        float mRightTU = 1;
        float mTopTV = 0;
        float mBottomTV = 1;

        float mLastFrameCursorXRelativeToWindow;
        float mLastFrameCursorYRelativeToWindow;

        ScrollBar mVerticalScrollBar;
        ScrollBar mHorizontalScrollBar;

        FloatRectangle mVisibleBounds = new FloatRectangle(0, 1, 0, 1);

        float mZoom = 1;

        #endregion

        #region Properties

        public Texture2D DisplayedTexture
        {
            set { mTextureDisplayArea.BaseTexture = value; }
            get { return mTextureDisplayArea.BaseTexture; }
        }

        public float LeftTU
        {
            set
            {
                mLeftTU = value;

                float displayLeft = Math.Max(0,  Math.Min(1, mLeftTU));

                mSelectedArea.X = (UToX(displayLeft) + UToX(mRightTU)) / 2.0f;
                mSelectedArea.ScaleX = (UToX(mRightTU) - UToX(displayLeft)) / 2.0f;
            }
            get { return mLeftTU; }
        }

        public float RightTU
        {
            set
            {
                mRightTU = value;

                float displayRight = Math.Max(0, Math.Min(1, mRightTU));

                mSelectedArea.X = (UToX(mLeftTU) + UToX(displayRight)) / 2.0f;
                mSelectedArea.ScaleX = (UToX(displayRight) - UToX(mLeftTU)) / 2.0f;
            }

            get { return mRightTU; }
        }

        public float TopTV
        {
            set
            {
                mTopTV = value;

                float displayTop = Math.Max(0, Math.Min(1, mTopTV));

                mSelectedArea.Y = (VToY(displayTop) + VToY(mBottomTV)) / 2.0f;
                mSelectedArea.ScaleY = (VToY(mBottomTV) - VToY(displayTop)) / 2.0f;
            }
            get { return mTopTV; }
        }

        public float BottomTV
        {
            set
            {
                mBottomTV = value;

                float displayBottom = Math.Max(0, Math.Min(1, mBottomTV));

                mSelectedArea.Y = (VToY(mTopTV) + VToY(displayBottom)) / 2.0f;
                mSelectedArea.ScaleY = (VToY(displayBottom) - VToY(mTopTV)) / 2.0f;
            }
            get { return mBottomTV; }
        }

        public bool IsCursorInSelectedArea
        {
            get
            {
                if (!mSelectedArea.Visible)
                {
                    return false;
                }
                else
                {
                    float cursorTU = XToU(CursorXRelativeToWindowRaw);
                    float cursorTV = YToV(CursorYRelativeToWindowRaw);
                    return (cursorTU >= LeftTU && cursorTU <= RightTU &&
                        cursorTV >= TopTV && cursorTV <= BottomTV);
                }
            }
        }

        float CursorXRelativeToWindow
        {
            get 
            {
                float rawValue = CursorXRelativeToWindowRaw;
                if (!mPixelPerfect.IsPressed || mTextureDisplayArea.BaseTexture == null)
                    return rawValue; 
                else
                {
                    int minimumXSnapValue = int.Parse(mMinimumXSelection.Text);

                    // Adjust the value so that the edge is treated as being on-the-pixel
                    float pixelPerfectLeft = 
                        MathFunctions.RoundFloat(
                            mVisibleBounds.Left,
                            minimumXSnapValue / 
                            (float)(mTextureDisplayArea.BaseTexture.Width));

                    // Difference is how much to adjust the cursor position
                    float difference = pixelPerfectLeft - mVisibleBounds.Left;

                    float returnValue =  MathFunctions.RoundFloat(
                        rawValue,
                        minimumXSnapValue * 2 * this.mTextureDisplayArea.ScaleX / 
                        (float)(mTextureDisplayArea.BaseTexture.Width * mZoom));

                    returnValue += difference * 2 * this.mTextureDisplayArea.ScaleX / mZoom;

                    return returnValue;
                }
            }
        }

        float CursorXRelativeToWindowRaw
        {
            get
            {
                return mCursor.XForUI - X + GuiManager.Camera.XEdge - mTextureDisplayArea.WorldUnitRelativeX
                    +
                    mTextureDisplayArea.ScaleX + 0f;
            }
        }

        float CursorYRelativeToWindow
        {
            get 
            {
                int minimumYSnapValue = int.Parse(mMinimumYSelection.Text);

                float rawValue = CursorYRelativeToWindowRaw;
                if (!mPixelPerfect.IsPressed || mTextureDisplayArea.BaseTexture == null)
                    return rawValue;
                else
                {
                    float pixelPerfectTop = 
                        MathFunctions.RoundFloat(
                            mVisibleBounds.Top,
                            minimumYSnapValue / 
                            (float)(mTextureDisplayArea.BaseTexture.Height));

                    // Difference is how much to adjust the cursor position
                    float pixelDifference = pixelPerfectTop - mVisibleBounds.Top;


                    float returnValue = MathFunctions.RoundFloat(
                        rawValue,
                        minimumYSnapValue * 2 * this.mTextureDisplayArea.ScaleY /
                        (float)(mTextureDisplayArea.BaseTexture.Height * mZoom));

                    returnValue += pixelDifference * 2 * this.mTextureDisplayArea.ScaleY / mZoom;

                    return returnValue;

                }
            }
        }

        float CursorYRelativeToWindowRaw
        {
            get
            {
                float difference = ScaleY - mTextureDisplayArea.Y;

                return -mCursor.YForUI - Y + GuiManager.Camera.YEdge + mTextureDisplayArea.ScaleY + difference;
            }
        }

        public bool AddToListButtonShown
        {
            get { return mAddToListButton.Visible; }
            set 
            {
                if (value != mAddToListButton.Visible)
                {
                    mAddToListButton.Visible = value;
                    OnWindowResize(this);
                }
            }
        }

        private float Zoom
        {
            get{ return mZoom;}
            set
            {
                float centerX = (mVisibleBounds.Right + mVisibleBounds.Left) / 2.0f;
                float centerY = (mVisibleBounds.Bottom + mVisibleBounds.Top) / 2.0f;

                mZoom = value;
                mZoom = Math.Min(mZoom, 1);

                mHorizontalScrollBar.Sensitivity = mZoom / 4.0f;
                mVerticalScrollBar.Sensitivity = mZoom / 4.0f;

                UpdateVisibleBoundsToZoom(centerX, centerY);

                UpdateScrollBarView();

                UpdateSelectionPositionAndScale();
            }
        }

        #endregion

        #region Event

        public GuiMessage TextureCoordinateChange;

        #endregion

        #region Delegate Methods

        void AdjustToVerticalScrollBar(Window callingWindow)
        {
            mVisibleBounds.Left = mHorizontalScrollBar.RatioDown;
            mVisibleBounds.Right = mVisibleBounds.Left + (float)mHorizontalScrollBar.View;
            
            mVisibleBounds.Top = mVerticalScrollBar.RatioDown;
            mVisibleBounds.Bottom = mVisibleBounds.Top + (float)mVerticalScrollBar.View;
            
            UpdateToVisibleBounds();

            UpdateSelectionPositionAndScale();
        }

        void MouseWheelZoom(Window callingWindow)
        {
            if (mCursor.ZVelocity > 0)
            {
                Zoom = Zoom / 2.0f;
            }
            else
            {
                Zoom = Zoom * 2.0f;
            }
        }

        void PixelPerfectClick(Window callingWindow)
        {
            mMinimumXSelection.Visible = mPixelPerfect.IsPressed;
            mMinimumYSelection.Visible = mPixelPerfect.IsPressed;
        }

        void OnWindowPush(Window callingWindow)
        {
            mLastFrameCursorXRelativeToWindow = CursorXRelativeToWindow;
            mLastFrameCursorYRelativeToWindow = CursorYRelativeToWindow;

            if (mSideOver != Sides.None)
            {
                mCursorState = CursorState.DraggingSides;
            }            
            else if (!mCursor.ResizingWindow && !IsCursorInSelectedArea && mCursor.mWindowGrabbed == null)
            {
                mSelectedArea.Visible = true;

                mLeftTU = mRightTU = XToU(CursorXRelativeToWindow);
                mTopTV = mBottomTV = YToV(CursorYRelativeToWindow);

                originalClickX = CursorXRelativeToWindow;
                originalClickY = CursorYRelativeToWindow;

                mLastFrameCursorXRelativeToWindow = originalClickX;
                mLastFrameCursorYRelativeToWindow = originalClickY;

                mSelectedArea.X = CursorXRelativeToWindow;
                mSelectedArea.ScaleX = 0;

                mSelectedArea.Y = CursorYRelativeToWindow;
                mSelectedArea.ScaleY = 0;

                mCursorState = CursorState.DraggingCorner;
            }
            else if (IsCursorInSelectedArea)
            {
                mCursorState = CursorState.MovingSelection;
            }
        }

        void OnWindowDrag(Window callingWindow)
        {
            // If the user is resizing the window or moving the window by the move bar,
            // don't allow the selection area to change.

            bool canModifySelection = !mCursor.ResizingWindow && mCursor.mWindowGrabbed == null;

            bool ShouldRaiseCoordinateChangeEvent = false;

            #region If Dragging Corner
            if (mCursorState == CursorState.DraggingCorner)
            {
                if (canModifySelection)
                {

                    // holding the Space button lets the user move the origin
                    if (InputManager.Keyboard.KeyDown(Keys.Space))
                    {
                        originalClickX += CursorXRelativeToWindow - mLastFrameCursorXRelativeToWindow;
                        originalClickY += CursorYRelativeToWindow - mLastFrameCursorYRelativeToWindow;

                        originalClickX = System.Math.Max(0, originalClickX);
                        originalClickY = System.Math.Max(0, originalClickY);

                        originalClickX =  System.Math.Min(UToX(1), originalClickX);
                        originalClickY =  System.Math.Min(VToY(1), originalClickY);
                    }

                    // Find the left and right points of the rectangle
                    float left =  System.Math.Min(CursorXRelativeToWindow, originalClickX);
                    float right = System.Math.Max(CursorXRelativeToWindow, originalClickX);

                    // top is System.Math.Min( because texture values increase when moving down
                    float top =  System.Math.Min(CursorYRelativeToWindow, originalClickY);
                    float bottom = System.Math.Max(CursorYRelativeToWindow, originalClickY);

                    mSelectedArea.ScaleX = .5f * (right - left);
                    mSelectedArea.X = mSelectedArea.ScaleX + left;

                    mSelectedArea.ScaleY = .5f * (bottom - top);
                    mSelectedArea.Y = mSelectedArea.ScaleY + top;

                    UpdateUVsToSelectedArea();

                    mLastFrameCursorXRelativeToWindow = CursorXRelativeToWindow;
                    mLastFrameCursorYRelativeToWindow = CursorYRelativeToWindow;

                    ShouldRaiseCoordinateChangeEvent = true;
                }
            }
            #endregion

            #region Else If MovingSelection

            else if (mCursorState == CursorState.MovingSelection)
            {
                if (canModifySelection)
                {
                    float differenceX = CursorXRelativeToWindow - mLastFrameCursorXRelativeToWindow;
                    float differenceY = CursorYRelativeToWindow - mLastFrameCursorYRelativeToWindow;

                    if (differenceX < 0)
                    {
                        float furthestLeft = UToX(0);
                        float currentLeft = mSelectedArea.X - mSelectedArea.ScaleX;

                        differenceX = System.Math.Max(differenceX, furthestLeft - currentLeft);
                    }
                    else
                    {
                        float furthestRight = UToX(1);
                        float currentRight = mSelectedArea.X + mSelectedArea.ScaleX;

                        differenceX =  System.Math.Min(differenceX, furthestRight - currentRight);
                    }

                    if (differenceY > 0)
                    {
                        float furthestBottom = VToY(1);
                        float currentBottom = mSelectedArea.Y + mSelectedArea.ScaleY;

                        differenceY =  System.Math.Min(differenceY, furthestBottom - currentBottom);
                    }
                    else
                    {
                        float furthestTop = VToY(0);
                        float currentTop = mSelectedArea.Y - mSelectedArea.ScaleY;

                        differenceY = System.Math.Max(differenceY, furthestTop - currentTop);
                    }

                    mSelectedArea.X += differenceX;
                    mSelectedArea.Y += differenceY;

                    UpdateUVsToSelectedArea();

                    mLastFrameCursorXRelativeToWindow = CursorXRelativeToWindow;
                    mLastFrameCursorYRelativeToWindow = CursorYRelativeToWindow;

                    ShouldRaiseCoordinateChangeEvent = true;
                }
            }
            #endregion

            #region Else if DraggingSides
            else if (mCursorState == CursorState.DraggingSides)
            {
                float change = 0;

                switch (mSideOver)
                {
                    case Sides.Top:
                        {
                            float oldTop = mSelectedArea.Y - mSelectedArea.ScaleY;

                            float newTop = CursorYRelativeToWindow;

                            change = newTop - oldTop;

                            mSelectedArea.Y += change;

                            float oldBottom = BottomTV;
                            //mSelectedArea.ScaleY -= change;
                            UpdateUVsToSelectedArea();

                            BottomTV = oldBottom;

                            ShouldRaiseCoordinateChangeEvent = true;
                            break;
                        }
                    case Sides.Bottom:
                        {
                            float oldBottom = mSelectedArea.Y + mSelectedArea.ScaleY;
                            float newBottom = CursorYRelativeToWindow;
                            change = newBottom - oldBottom;

                            mSelectedArea.Y += change;

                            float oldTop = TopTV;

                            UpdateUVsToSelectedArea();

                            TopTV = oldTop;

                            ShouldRaiseCoordinateChangeEvent = true;
                        }
                        break;
                    case Sides.Left:
                        {
                            float oldLeft = mSelectedArea.X - mSelectedArea.ScaleX;

                            float newLeft = CursorXRelativeToWindow;

                            change = newLeft - oldLeft;

                            mSelectedArea.X += change;

                            float oldRight = RightTU;
                            //mSelectedArea.ScaleY -= change;
                            UpdateUVsToSelectedArea();

                            RightTU = oldRight;

                            ShouldRaiseCoordinateChangeEvent = true;
                        }
                        break;

                    case Sides.Right:
                        {
                            float oldRight = mSelectedArea.X + mSelectedArea.ScaleX;
                            float newRight = CursorXRelativeToWindow;
                            change = newRight - oldRight;

                            mSelectedArea.X += change;

                            float oldLeft = LeftTU;

                            UpdateUVsToSelectedArea();

                            LeftTU = oldLeft;

                            ShouldRaiseCoordinateChangeEvent = true;
                            break;
                        }

                }

            }

            #endregion

            if (ShouldRaiseCoordinateChangeEvent && TextureCoordinateChange != null)
            {
                TextureCoordinateChange(this);
            }
        }

        void OnWindowClick(Window callingWindow)
        {
            if (mTopTV == mBottomTV && mLeftTU == mRightTU)
            {
                mTopTV = 0;
                mBottomTV = 1;
                mLeftTU = 0;
                mRightTU = 1;

                mSelectedArea.Visible = false;
            }
        }

        void OnWindowDoubleClick(Window callingWindow)
        {
            mSelectedArea.Visible = false;

            mLeftTU = 0;
            mRightTU = 1;
            mTopTV = 0;
            mBottomTV = 1;
            
            mSelectedArea.X = 0;
            mSelectedArea.ScaleX = 0;

            mSelectedArea.Y = 0;
            mSelectedArea.ScaleY = 0;
        }

        void OnWindowResize(Window callingWindow)
        {
            #region Update the mSelectedArea

            UpdateSelectionPositionAndScale();

            #endregion

            #region Reposition GUI elements according to the new size

            mTextureDisplayArea.X = ScaleX - 1f;
            mTextureDisplayArea.ScaleX = ScaleX - 1.5f;

            mPixelPerfect.X = .5f + mPixelPerfect.ScaleX;
            mMinimumXSelection.X =
                mPixelPerfect.X + mPixelPerfect.ScaleX +
                mMinimumXSelection.ScaleX;

            mMinimumYSelection.X =
                mMinimumXSelection.X + mMinimumXSelection.ScaleX +
                mMinimumYSelection.ScaleX;

            if (mAddToListButton.VisibleSettingIgnoringParent)
            {
                mTextureDisplayArea.Y = ScaleY - 3f;
                mTextureDisplayArea.ScaleY = ScaleY - 3.5f;

                mAddToListButton.X = ScaleX;
                mAddToListButton.ScaleX = ScaleX - .4f;
                mAddToListButton.Y = 2 * ScaleY - 1.4f;
            }
            else
            {
                mTextureDisplayArea.Y = ScaleY - 2f;
                mTextureDisplayArea.ScaleY = ScaleY - 2.5f;
            }



            mVerticalScrollBar.X = 2*ScaleX - mVerticalScrollBar.ScaleX - .5f;
            mVerticalScrollBar.Y = mTextureDisplayArea.Y;
            mVerticalScrollBar.ScaleY = mTextureDisplayArea.ScaleY;


            mHorizontalScrollBar.X = mTextureDisplayArea.X;
            mHorizontalScrollBar.Y = mTextureDisplayArea.Y + mTextureDisplayArea.ScaleY + mHorizontalScrollBar.ScaleY;
            mHorizontalScrollBar.ScaleX = mTextureDisplayArea.ScaleX;

            mPixelPerfect.Y = mHorizontalScrollBar.Y + mHorizontalScrollBar.ScaleY + mPixelPerfect.ScaleY;
            mMinimumXSelection.Y = mPixelPerfect.Y;
            mMinimumYSelection.Y = mPixelPerfect.Y;



            UpdateScrollBarView();

            #endregion
        }

        void OnRollOver(Window callingWindow)
        {
            if (InputManager.Mouse.ButtonDown(FlatRedBall.Input.Mouse.MouseButtons.MiddleButton))
            {
                float coefficient = .02f;

                mVisibleBounds.Left += -coefficient * GuiManager.Cursor.XVelocity * mZoom;
                mVisibleBounds.Right += -coefficient * GuiManager.Cursor.XVelocity * mZoom;
                mVisibleBounds.Top += coefficient * GuiManager.Cursor.YVelocity * mZoom;
                mVisibleBounds.Bottom += coefficient * GuiManager.Cursor.YVelocity * mZoom;

                FixVisibleBounds();

                UpdateScrollBarView();

                UpdateToVisibleBounds();

                UpdateSelectionPositionAndScale();
            }
            else 
            {
                if (!InputManager.Mouse.ButtonDown(Mouse.MouseButtons.LeftButton))
                {
                    UpdateSideOver();
                }

                switch (mSideOver)
                {
                    case Sides.Left:
                        System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.SizeWE;

                        break;
                    case Sides.Right:
                        System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.SizeWE;

                        break;
                    case Sides.Top:
                        System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.SizeNS;

                        break;
                    case Sides.Bottom:
                        System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.SizeNS;

                        break;
                    case Sides.None:
                        if (IsCursorInSelectedArea)
                        {
                            // If the cursor is inside the texture coordinate square allow the user
                            // to drag it around.                
                            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.SizeAll;
                        }
                        break;

                }


            }
            
        }

        void RightClickMenu(Window callingWindow)
        {
            ListBox listBox = GuiManager.AddPerishableListBox();

            listBox.AddItem("Zoom In");
            listBox.AddItem("Zoom Out");

            listBox.SetScaleToContents(0);

            listBox.HighlightOnRollOver = true;

            GuiManager.PositionTopLeftToCursor(listBox);

            listBox.ScrollBarVisible = false;

            listBox.Click += SelectItemInRightClickMenu;
        }

        void SelectItemInRightClickMenu(Window callingWindow)
        {
            string optionSelected = ((ListBox)callingWindow).GetFirstHighlightedItem().Text;

            switch (optionSelected)
            {
                case "Zoom In":
                    Zoom = Zoom / 2.0f;
                    GuiManager.RemoveWindow(callingWindow);
                    break;
                case "Zoom Out":
                    Zoom = Zoom * 2;
                    GuiManager.RemoveWindow(callingWindow);
                    break;
            }
 
        
        }

        #endregion

        #region Methods

        #region Constructor

        public TextureCoordinatesSelectionWindow() : 
            base(GuiManager.Cursor)
        {
            // Victor says:  This class USED to
            // add itself to the GuiManager.  This
            // is no longer recommended as it makes
            // windows not as reusable.  Therefore, I
            // removed the automatic adding to the GuiManager.
            // This might break your code if you're using this,
            // so if your TextureCoordinatesSelectionWindow isn't
            // showing up, you might want to make sure you're adding
            // it to the GuiManager.

            #region Create "this" and add it to the GuiManager
            HasMoveBar = true;
            ScaleY = 12.5f;
            ScaleX = 11.4f;

            Resizable = true;
            MinimumScaleX = ScaleX;
            MinimumScaleY = ScaleY;

            this.Resizing += OnWindowResize;
            #endregion

            #region Create the texture display area

            mTextureDisplayArea = new Window(mCursor);
            AddWindow(mTextureDisplayArea);

            mTextureDisplayArea.DrawBorders = false;
            mTextureDisplayArea.Push += OnWindowPush;
            mTextureDisplayArea.Dragging += OnWindowDrag;
            mTextureDisplayArea.Click += OnWindowClick;
            mTextureDisplayArea.RollingOver += this.OnRollOver;
            mTextureDisplayArea.DoubleClick += OnWindowDoubleClick;

            mTextureDisplayArea.MouseWheelScroll += MouseWheelZoom;

            mTextureDisplayArea.SecondaryClick += RightClickMenu;

            #endregion

            mSelectedArea = new Window(mCursor);
            mTextureDisplayArea.AddWindow(mSelectedArea);
            mSelectedArea.ScaleX = 3;
            mSelectedArea.ScaleY = 3;
            mSelectedArea.BaseTexture = 
                FlatRedBallServices.Load<Texture2D>("genGfx/targetBox.bmp", GuiManager.InternalGuiContentManagerName);
            mSelectedArea.Enabled = false; // so it doesn't block input from the this (the parent Window)
            mSelectedArea.DrawBorders = false;
            mSelectedArea.Alpha = 127;

            mAddToListButton = new Button(mCursor);
            AddWindow(mAddToListButton);
            mAddToListButton.Text = "Add To List";
            mAddToListButton.Visible = false;

            #region Pixel Perfect ToggleButton and ComboBoxes

            mPixelPerfect = new ToggleButton(mCursor);
            AddWindow(mPixelPerfect);

            mPixelPerfect.ScaleX = 5;
            mPixelPerfect.SetText("Free", "Snapping");
            mPixelPerfect.Press();
            mPixelPerfect.Click += PixelPerfectClick;

            mMinimumXSelection = new ComboBox(mCursor);
            AddWindow(mMinimumXSelection);
            mMinimumXSelection.ScaleX = 3;
            mMinimumXSelection.AddItem("1");
            mMinimumXSelection.AddItem("4");
            mMinimumXSelection.AddItem("8");
            mMinimumXSelection.AddItem("16");
            mMinimumXSelection.AddItem("32");
            mMinimumXSelection.Text = "1";

            mMinimumYSelection = new ComboBox(mCursor);
            AddWindow(mMinimumYSelection);
            mMinimumYSelection.ScaleX = 3;
            mMinimumYSelection.AddItem("1");
            mMinimumYSelection.AddItem("4");
            mMinimumYSelection.AddItem("8");
            mMinimumYSelection.AddItem("16");
            mMinimumYSelection.AddItem("32");
            mMinimumYSelection.Text = "1";

            #endregion

            #region Create the ScrollBars
            mVerticalScrollBar = new ScrollBar(mCursor);
            AddWindow(mVerticalScrollBar);
            mVerticalScrollBar.UpButtonClick += AdjustToVerticalScrollBar;
            mVerticalScrollBar.DownButtonClick += AdjustToVerticalScrollBar;
            mVerticalScrollBar.PositionBarMove += AdjustToVerticalScrollBar;

            mHorizontalScrollBar = new ScrollBar(mCursor);
            AddWindow(mHorizontalScrollBar);
            mHorizontalScrollBar.UpButtonClick += AdjustToVerticalScrollBar;
            mHorizontalScrollBar.DownButtonClick += AdjustToVerticalScrollBar;
            mHorizontalScrollBar.PositionBarMove += AdjustToVerticalScrollBar;
            mHorizontalScrollBar.Alignment = ScrollBar.ScrollBarAlignment.Horizontal;
            mHorizontalScrollBar.ScaleY = 1;
            #endregion

            OnWindowResize(this);
        }

        #endregion

        #region Public Methods

        public void AddToListClickEventAdd(GuiMessage AddToListClick)
        {
            mAddToListButton.Click += AddToListClick;
        }

        public void AddTextureDragEvent(GuiMessage message)
        {
            mTextureDisplayArea.Dragging += message;
        }

        public void ReplaceTexture(Texture2D oldTexture, Texture2D newTexture)
        {
            if (DisplayedTexture == oldTexture)
            {
                DisplayedTexture = newTexture;
            }

        }

        #region XML Docs
        /// <summary>
        /// Sets the texture coordinates of the displayed texture.
        /// </summary>
        /// <remarks>
        /// The base method sets the texture coordinates of the actual window while this
        /// override sets the texture coordinates of the texture that is being displayed.
        /// The assumption is that the user is not going to ever want to change the way that
        /// this window is drawn but rather the coordinates of the texture that is being displayed.
        /// </remarks>
        /// <param name="top">The top texture coordinate ( V ).</param>
        /// <param name="bottom">The bottom texture coordinate ( V ).</param>
        /// <param name="left">The left texture coordinate ( U ).</param>
        /// <param name="right">The right texture coordinate ( U ).</param>
        #endregion
        public override void SetTextureCoordinates(float top, float bottom, float left, float right)
        {
            mLeftTU = left;
            mRightTU = right;
            mTopTV = top;
            mBottomTV = bottom;

            UpdateSelectionPositionAndScale();
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("LeftTU: ").Append(mLeftTU);
            stringBuilder.Append("\nTopTV: ").Append(mTopTV);
            stringBuilder.Append("\nRightTU: ").Append(mRightTU);
            stringBuilder.Append("\nBottomTV: ").Append(mBottomTV);

            return stringBuilder.ToString();
        }

        #endregion

        #region Private Methods

        private void FixVisibleBounds()
        {
            if (mVisibleBounds.Left < 0)
            {
                mVisibleBounds.Right -= mVisibleBounds.Left;
                mVisibleBounds.Left -= mVisibleBounds.Left;
            }

            if (mVisibleBounds.Top < 0)
            {
                mVisibleBounds.Bottom -= mVisibleBounds.Top;
                mVisibleBounds.Top -= mVisibleBounds.Top;
            }

            if (mVisibleBounds.Right > 1)
            {
                mVisibleBounds.Left -= mVisibleBounds.Right - 1;
                mVisibleBounds.Right = 1;
            }

            if (mVisibleBounds.Bottom > 1)
            {
                mVisibleBounds.Top -= mVisibleBounds.Bottom - 1;
                mVisibleBounds.Bottom = 1;
            }
        }

        void UpdateScrollBarView()
        {
            mVerticalScrollBar.View = mVisibleBounds.Bottom - mVisibleBounds.Top;
            mHorizontalScrollBar.View = mVisibleBounds.Right - mVisibleBounds.Left;

            mVerticalScrollBar.RatioDown = mVisibleBounds.Top;
            mHorizontalScrollBar.RatioDown = mVisibleBounds.Left;

        }

        void UpdateSelectionPositionAndScale()
        {
            mSelectedArea.X = (UToX(mLeftTU) + UToX(mRightTU)) / 2.0f;
            mSelectedArea.ScaleX = (UToX(mRightTU) - UToX(mLeftTU)) / 2.0f;

            mSelectedArea.Y = (VToY(mTopTV) + VToY(mBottomTV)) / 2.0f;
            mSelectedArea.ScaleY = (VToY(mBottomTV) - VToY(mTopTV)) / 2.0f;

        }


        private void UpdateSideOver()
        {
            //float unitThreshold = .5f;

            if (mSelectedArea.Visible == false)
            {
                mSideOver = Sides.None;
            }
            else
            {
                Cursor cursor = GuiManager.Cursor;

                const float distanceForGrabbing = 1;
                mSideOver = Sides.None;

                if (CursorXRelativeToWindowRaw > mSelectedArea.X - mSelectedArea.ScaleX &&
                    CursorXRelativeToWindowRaw < mSelectedArea.X + mSelectedArea.ScaleX)
                {
                    
                    // The cursor is within the left and right boundaries, so let's see if
                    // we're close enough to the top/bottom to resize it.
                    float distanceFromTop = Math.Abs(
                        CursorYRelativeToWindowRaw -
                        (mSelectedArea.Y - mSelectedArea.ScaleY));

                    float distanceFromBottom = Math.Abs(
                        CursorYRelativeToWindowRaw -
                        (mSelectedArea.Y + mSelectedArea.ScaleY));

                    if (distanceFromTop < distanceFromBottom && distanceFromTop < distanceForGrabbing)
                    {
                        mSideOver = Sides.Top;
                    }
                    else if (distanceFromBottom < distanceFromTop && distanceFromBottom < distanceForGrabbing)
                    {
                        mSideOver = Sides.Bottom;
                    }
                }
                if (CursorYRelativeToWindowRaw > mSelectedArea.Y - mSelectedArea.ScaleY &&
                    CursorYRelativeToWindowRaw < mSelectedArea.Y + mSelectedArea.ScaleY)
                {
                    // The cursor is within the left and right boundaries, so let's see if
                    // we're close enough to the top/bottom to resize it.
                    float distanceFromLeft = Math.Abs(
                        CursorXRelativeToWindowRaw -
                        (mSelectedArea.X - mSelectedArea.ScaleX));

                    float distanceFromRight = Math.Abs(
                        CursorXRelativeToWindowRaw -
                        (mSelectedArea.X + mSelectedArea.ScaleX));

                    if (distanceFromLeft < distanceFromRight && distanceFromLeft < distanceForGrabbing)
                    {
                        mSideOver = Sides.Left;
                    }
                    else if (distanceFromRight < distanceFromLeft && distanceFromRight < distanceForGrabbing)
                    {
                        mSideOver = Sides.Right;
                    }

                }

            }
        }

        void UpdateUVsToSelectedArea()
        {
            float right = mSelectedArea.X + mSelectedArea.ScaleX;
            float left = mSelectedArea.X - mSelectedArea.ScaleX;

            float top = mSelectedArea.Y - mSelectedArea.ScaleY;
            float bottom = mSelectedArea.Y + mSelectedArea.ScaleY;

            mRightTU = XToU(right);
            mLeftTU = XToU(left);

            mTopTV = YToV(top);
            mBottomTV = YToV(bottom);

            // make sure that the values don't get moved outside of the range
            mRightTU = System.Math.Max(mRightTU, 0);
            mLeftTU = System.Math.Max(mLeftTU, 0);
            mTopTV = System.Math.Max(mTopTV, 0);
            mBottomTV = System.Math.Max(mBottomTV, 0);

            mRightTU =  System.Math.Min(mRightTU, 1);
            mLeftTU =  System.Math.Min(mLeftTU, 1);
            mTopTV =  System.Math.Min(mTopTV, 1);
            mBottomTV =  System.Math.Min(mBottomTV, 1);

        }

        private void UpdateVisibleBoundsToZoom(float centerX, float centerY)
        {
            mVisibleBounds.Left = centerX - mZoom / 2.0f;
            mVisibleBounds.Top = centerY - mZoom / 2.0f;

            mVisibleBounds.Right = centerX + mZoom / 2.0f;
            mVisibleBounds.Bottom = centerY + mZoom / 2.0f;

            FixVisibleBounds();

            UpdateToVisibleBounds();
        }

        private void UpdateToVisibleBounds()
        {
            mTextureDisplayArea.SetTextureCoordinates(mVisibleBounds.Top, mVisibleBounds.Bottom, mVisibleBounds.Left, mVisibleBounds.Right);
        }


        float UToX(float tu)
        {
            return (-mVisibleBounds.Left + tu) * (mTextureDisplayArea.ScaleX * 2)/mZoom;
        }

        float VToY(float tv)
        {
            return (-mVisibleBounds.Top + tv) * (mTextureDisplayArea.ScaleY * 2)/mZoom;
        }

        float XToU(float x)
        {
            float value = mVisibleBounds.Left + (mVisibleBounds.Right - mVisibleBounds.Left) * (x / (mTextureDisplayArea.ScaleX * 2.0f));

            // There is sometimes some floating point inaccuracy, so let's solve that here
            int minimumXSnapValue = int.Parse(mMinimumXSelection.Text);
            if (mPixelPerfect.IsPressed)
            {
                float pixelValue = value * this.DisplayedTexture.Width;
                pixelValue = MathFunctions.RoundFloat(pixelValue, minimumXSnapValue);
                value = pixelValue / this.DisplayedTexture.Width;
            }


            return value;
        }
        

        float YToV(float y)
        {
            float value = mVisibleBounds.Top + (mVisibleBounds.Bottom - mVisibleBounds.Top) * (y / (mTextureDisplayArea.ScaleY * 2.0f));


            int minimumYSnapValue = int.Parse(mMinimumYSelection.Text);
            if (mPixelPerfect.IsPressed)
            {
                float pixelValue = value * this.DisplayedTexture.Height;
                pixelValue = MathFunctions.RoundFloat(pixelValue, minimumYSnapValue);
                value = pixelValue / this.DisplayedTexture.Height;
            }

            return value;

        }

        #endregion

        #endregion

    }
}