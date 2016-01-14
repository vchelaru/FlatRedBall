using System;

#if FRB_MDX
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Direct3D=Microsoft.DirectX.Direct3D;
#else

#endif

using FlatRedBall.Graphics;
using FlatRedBall.ManagedSpriteGroups;


namespace FlatRedBall.Gui
{
	/// <summary>
	/// Summary description for ScrollBar.
	/// </summary>
	public class ScrollBar : Window
    {

        #region Enums

        public enum ScrollBarAlignment
        {
            Vertical,
            Horizontal
        }
            

        #endregion

        #region Fields
        // The amount that pushing the up or down
        // arrows moves the scroll bar.  This is a value
        // between 0 and 1, where 1 represents the entire height
        // of the scroll bar.  This is generally set to 1 / numberOfElements
        // when used in a ListBox.
		double mSensitivity;

		// work here.
		internal Button upButton;
		internal Button downButton;
		Button mPositionBar;

		double view;

        ScrollBarAlignment mAlignment = ScrollBarAlignment.Vertical;

        int mFirstVisibleIndex = 0;
        int mTotalCount = 0;

		#endregion

		#region Properties

        public ScrollBarAlignment Alignment
        {
            get { return mAlignment; }
            set 
            { 
                mAlignment = value;

                if (mAlignment == ScrollBarAlignment.Horizontal)
                {
                    ScaleX = ScaleX;
                }
                else
                {
                    ScaleY = ScaleY;
                }
            }
        }

        public float RatioDown
        {
            get
            {
                if (mAlignment == ScrollBarAlignment.Vertical)
                {
                    return 
                       (ScaleY - 2 - mPositionBar.ScaleY - mPositionBar.WorldUnitRelativeY) / (2 * (mScaleY - 2)) ;
                }
                else
                {
                    float ratioDown = -(-ScaleX + 2 + mPositionBar.ScaleX - mPositionBar.WorldUnitRelativeX) /
                        (2 * (mScaleX - 2));

                    const float epsilon = .000001f;

                    if (ratioDown < epsilon)
                    {
                        return 0;
                    }
                    else
                    {
                        return ratioDown;
                    }
                }

            }
            set
            {
                if (mAlignment == ScrollBarAlignment.Vertical)
                {
					mPositionBar.WorldUnitRelativeY = ScaleY - (float)(2 + mPositionBar.ScaleY + value * (2 * (mScaleY - 2)));

                }
                else
                {
                    mPositionBar.WorldUnitRelativeX = -ScaleX + (float)(2 + mPositionBar.ScaleX + value * (2 * (mScaleX - 2)));
                }

				FixBar();
            }
        }

        #region XML Docs
        /// <summary>
        /// The distance from the center to the edge of the ScrollBar on the Y axis.
        /// </summary>
        #endregion
        public new float ScaleY
		{
			get{	return mScaleY;	}
			set
			{
                
                base.ScaleY = value;

                if (mAlignment == ScrollBarAlignment.Vertical)
                {
                    upButton.SetPositionTL(ScaleX, 1);
                    downButton.SetPositionTL(ScaleX, 2 * value - 1);
                }
                FixBar();
			}
        }

        #region XML Docs
        /// <summary>
        /// The distance from the center to the edge of the ScrollBar on the X axis.
        /// </summary>
        #endregion
        public new float ScaleX
		{
			get{	return (float)mScaleX; }
			set
			{
				mScaleX = value;

                if (mAlignment == ScrollBarAlignment.Horizontal)
                { 
                    // Vic says:  The up button should have a value of 1 since 0 is the left
                    // edge of the scroll bar.
                    upButton.SetPositionTL(1, ScaleY);
                    downButton.SetPositionTL(2 * mScaleX - 1, ScaleY);
                }
                FixBar();
			}

        }

        #region XML Docs
        /// <summary>
        /// The ratio of the ScrollBar (excluding the up and down buttons) that the
        /// position bar should travel when pusing the up or down buttons.
        /// </summary>
        /// <remarks>
        /// This value is generally set to 1 divided by the number of elements.
        /// </remarks>
        #endregion
        public double Sensitivity
        {
            get { return mSensitivity; }
            set 
            { 
                mSensitivity = value;
                mTotalCount = (int)(System.Math.Round(1 / mSensitivity));
            }
        }

        #region XML Docs
        /// <summary>
        /// Sets the size of the position bar - this value should be the number
        /// of elements in view divided by the total number of elements in a list.
        /// </summary>
        /// <remarks>
        /// This value should be between 0 and 1.  For example, if a ListBox has
        /// 10 items, but 5 are visible, then the SetView method should be called with
        /// .5 as the argument (5/10).
        /// </remarks>
        #endregion
        public double View
        {
            get { return view; }
            set 
            { 
                view = System.Math.Min(1, value);

                if (mAlignment == ScrollBarAlignment.Vertical)
                {
                    mPositionBar.ScaleY = (float)(view * (mScaleY - 2));

                    mPositionBar.ScaleY = System.Math.Max(mPositionBar.ScaleY, .6f);
                }
                else
                {
                    mPositionBar.ScaleX = (float)(view * (mScaleX - 2));

                    mPositionBar.ScaleX = System.Math.Max(mPositionBar.ScaleX, .6f);
                }

                FixBar();
            }
        }


		#endregion

        #region Events

        public event GuiMessage UpButtonClick;
        public event GuiMessage DownButtonClick;
        public event GuiMessage PositionBarMove;
        #endregion

        #region Event Methods
        private void OnUpButtonClick(Window callingWindow)
        {
            if (mFirstVisibleIndex > 0)
            {
                mFirstVisibleIndex--;
            }

            if (mAlignment == ScrollBarAlignment.Vertical)
            {
                mPositionBar.WorldUnitRelativeY += (float)(2 * (mScaleY - 2) * mSensitivity);
            }
            else
            {
                mPositionBar.WorldUnitRelativeX -= (float)(2 * (mScaleX - 2) * mSensitivity);
            }

            FixBar();

            if (UpButtonClick != null)
                UpButtonClick(this);
        }


        private void OnDownButtonClick(Window callingWindow)
        {
            int lastFirstVisible = (int)System.Math.Round((double)mTotalCount * (1 - view));

            if (mFirstVisibleIndex < lastFirstVisible)
            {
                mFirstVisibleIndex++;
            }

            if (mAlignment == ScrollBarAlignment.Vertical)
            {
                mPositionBar.WorldUnitRelativeY -= (float)(2 * (mScaleY - 2) * mSensitivity);
            }
            else
            {
                mPositionBar.WorldUnitRelativeX += (float)(2 * (mScaleX - 2) * mSensitivity);
            }

            FixBar();

            if (DownButtonClick != null)
                DownButtonClick(this);
        }



        #endregion

        #region Methods

        #region Constructor

        #region XML Docs
        /// <summary>
        /// Instantiates a new ScrollBar instance.  The new instance will not automatically be
        /// added to the GuiManager.
        /// </summary>
        /// <param name="cursor">The Cursor that the ScrollBar will interact with.</param>
        #endregion
        public ScrollBar(Cursor cursor) : 
            base(cursor)
		{

            upButton = new Button(mCursor);
            AddWindow(upButton);

            downButton = new Button(mCursor);
            AddWindow(downButton);

            mPositionBar = new Button(mCursor);
            AddWindow(mPositionBar);

			upButton.Click += new GuiMessage(OnUpButtonClick);
			downButton.Click += new GuiMessage(OnDownButtonClick);

			upButton.ScaleX = upButton.ScaleY = downButton.ScaleX = downButton.ScaleY = .95f;
			mSensitivity = .1;

			View = .1f;

            mPositionBar.SetPositionTL(0, 3);
			this.mNumberOfVertices = 6 + 6 + 18 + 18; // top button + bottom button + scrollbar (3*6) + base (3*6)

            ScaleX = 1;
            ScaleY = 5;
            FixBar();

        }


        #region XML Docs
        /// <summary>
        /// Instantiates a new ScrollBar using a GuiSkin.  The new instance will not automatically be
        /// added to the GuiManager.
        /// </summary>
        /// <param name="guiSkin">The GuiSkin to customize the appearance of the ScrollBar.</param>
        /// <param name="cursor">The Cursor that the ScrollBar will interact with.</param>
        #endregion
        public ScrollBar(GuiSkin guiSkin, Cursor cursor)
            : base(guiSkin, cursor)
        {
            mScaleX = 1;
            mScaleY = 1;

            upButton = new Button(guiSkin, cursor);
            base.AddWindow(upButton);

            downButton = new Button(guiSkin, cursor);
            base.AddWindow(downButton);
            
            mPositionBar = new Button(guiSkin, cursor);
            base.AddWindow(mPositionBar);

            upButton.SpriteFrame.RelativeZ = -.01f * FlatRedBall.Math.MathFunctions.ForwardVector3.Z;
            downButton.SpriteFrame.RelativeZ = -.01f * FlatRedBall.Math.MathFunctions.ForwardVector3.Z;
            mPositionBar.SpriteFrame.RelativeZ = -.01f * FlatRedBall.Math.MathFunctions.ForwardVector3.Z;

            upButton.SpriteFrame.RelativeY = 10;
            downButton.SpriteFrame.RelativeY = -10;


            upButton.Click += new GuiMessage(OnUpButtonClick);
            downButton.Click += new GuiMessage(OnDownButtonClick);

           // upButton.ScaleX = upButton.ScaleY = downButton.ScaleX = downButton.ScaleY = .95f;

            mSensitivity = .1;

            View = .1f;

            mPositionBar.SetPositionTL(mScaleX, 3);
        }

        #endregion


        #region Public Methods

        #region XML Docs
        /// <summary>
        /// Gets the index of the first element shown in a list given the position of the position bar.
        /// </summary>
        /// <remarks>
        /// Prior to this method being called, the SetView function must be called and the Sensitivity 
        /// property must be set.
        /// </remarks>
        /// <returns>The number of elements down.</returns>
        #endregion
        public int GetNumDown()
		{
            return mFirstVisibleIndex;

            /*
            if (mAlignment == ScrollBarAlignment.Vertical)
            {
                float topOfPosBarFromTopOfScrollBar = (float)(mPositionBar.Parent.ScaleY - 2 - (mPositionBar.WorldUnitRelativeY + mPositionBar.ScaleY));
                return (int)System.Math.Round(topOfPosBarFromTopOfScrollBar /
                              (2 * (mScaleY - 2) * mSensitivity));
            }
            else
            {
                float topOfPosBarFromTopOfScrollBar = (float)(mPositionBar.Parent.ScaleX - 2 - (mPositionBar.WorldUnitRelativeX + mPositionBar.ScaleX));
                return (int)System.Math.Round(topOfPosBarFromTopOfScrollBar /
                              (2 * (mScaleX - 2) * mSensitivity));

            }
            */

        }

        #region XML Docs
        /// <summary>
        /// Sets the position bar so that it reflects the number down passed as the argument.
        /// </summary>
        /// <param name="numDown">The number down to match with the PositionBar's position.</param>
        #endregion
        public void SetScrollPosition(int numDown)
		{
            mFirstVisibleIndex = numDown;
            if (mAlignment == ScrollBarAlignment.Vertical)
            {
                mPositionBar.WorldUnitRelativeY = ScaleY - (float)(2 + mPositionBar.ScaleY + numDown * (2 * (mScaleY - 2) * mSensitivity));
            }
            else
            {
                mPositionBar.WorldUnitRelativeX = ScaleX - (float)(2 + mPositionBar.ScaleX + numDown * (2 * (mScaleX - 2) * mSensitivity));
            }

        }

        #region XML Docs 
        /// <summary>
        /// Sets the ScrollBar's skin and refreshes its appearance.
        /// </summary>
        /// <param name="guiSkin">The GuiSkin to set.</param>
        #endregion
        public override void SetSkin(GuiSkin guiSkin)
        {
            base.SetFromWindowSkin(guiSkin.ScrollBarSkin);

            if (upButton != null && downButton != null && mPositionBar != null)
            {
                upButton.SetSkin(guiSkin.ScrollBarSkin.UpButtonSkin,
                    guiSkin.ScrollBarSkin.UpButtonDownSkin);

                downButton.SetSkin(guiSkin.ScrollBarSkin.DownButtonSkin,
                    guiSkin.ScrollBarSkin.DownButtonDownSkin);

                mPositionBar.SetFromWindowSkin(guiSkin.ScrollBarSkin.PositionBarSkin);
            }
        }

        [Obsolete("Use View property")]
        public void SetView(double view)
		{
            View = (float)view;
		}

        #endregion

        #region Internal
#if !SILVERLIGHT
        internal override void DrawSelfAndChildren(Camera camera)
		{

            if (Visible == false) return;

            float xToUse = mWorldUnitX;
            float yToUse = mWorldUnitY;

            #region Set the color and Z position for all vertices

            StaticVertices[0].Color = StaticVertices[1].Color = StaticVertices[2].Color = StaticVertices[3].Color = StaticVertices[4].Color = StaticVertices[5].Color = mColor;

			StaticVertices[0].Position.Z = StaticVertices[1].Position.Z = StaticVertices[2].Position.Z = 
                StaticVertices[3].Position.Z = StaticVertices[4].Position.Z = StaticVertices[5].Position.Z = 
                camera.Z + FlatRedBall.Math.MathFunctions.ForwardVector3.Z * 100;

            #endregion

            if (mAlignment == ScrollBarAlignment.Vertical)
            {
                #region draw the back of the scrollbar

                #region Top of window
                StaticVertices[0].Position.X = xToUse - ScaleX;
                StaticVertices[0].Position.Y = yToUse + ScaleY - 2.6f;
                StaticVertices[0].TextureCoordinate.X = .054688f;
                StaticVertices[0].TextureCoordinate.Y = .62109375f;

                StaticVertices[1].Position.X = xToUse - ScaleX;
                StaticVertices[1].Position.Y = yToUse + ScaleY - 1.1f;
                StaticVertices[1].TextureCoordinate.X = .054688f;
                StaticVertices[1].TextureCoordinate.Y = .570313f;

                StaticVertices[2].Position.X = xToUse + ScaleX;
                StaticVertices[2].Position.Y = yToUse + ScaleY - 1.1f;
                StaticVertices[2].TextureCoordinate.X = .1171875f;
                StaticVertices[2].TextureCoordinate.Y = .570313f;

                StaticVertices[3] = StaticVertices[0];
                StaticVertices[4] = StaticVertices[2];

                StaticVertices[5].Position.X = xToUse + ScaleX;
                StaticVertices[5].Position.Y = yToUse + ScaleY - 2.6f;
                StaticVertices[5].TextureCoordinate.X = .1171875f;
                StaticVertices[5].TextureCoordinate.Y = .62109375f;

                GuiManager.WriteVerts(StaticVertices);

                #endregion

                #region Center of window
                StaticVertices[0].Position.X = xToUse - ScaleX;
                StaticVertices[0].Position.Y = yToUse - ScaleY + 2.6f;
                StaticVertices[0].TextureCoordinate.X = .054688f;
                StaticVertices[0].TextureCoordinate.Y = .6328125f;

                StaticVertices[1].Position.X = xToUse - ScaleX;
                StaticVertices[1].Position.Y = yToUse + ScaleY - 2.6f;
                StaticVertices[1].TextureCoordinate.X = .054688f;
                StaticVertices[1].TextureCoordinate.Y = .62109375f;

                StaticVertices[2].Position.X = xToUse + ScaleX;
                StaticVertices[2].Position.Y = yToUse + ScaleY - 2.6f;
                StaticVertices[2].TextureCoordinate.X = .1171875f;
                StaticVertices[2].TextureCoordinate.Y = .62109375f;

                StaticVertices[3] = StaticVertices[0];
                StaticVertices[4] = StaticVertices[2];

                StaticVertices[5].Position.X = xToUse + ScaleX;
                StaticVertices[5].Position.Y = yToUse - ScaleY + 2.6f;
                StaticVertices[5].TextureCoordinate.X = .1171875f;
                StaticVertices[5].TextureCoordinate.Y = .6328125f;

                GuiManager.WriteVerts(StaticVertices);

                #endregion

                #region Bottom of window
                StaticVertices[0].Position.X = xToUse - ScaleX;
                StaticVertices[0].Position.Y = yToUse - ScaleY + 1.1f;
                StaticVertices[0].TextureCoordinate.X = .054688f;
                StaticVertices[0].TextureCoordinate.Y = .570313f;

                StaticVertices[1].Position.X = xToUse - ScaleX;
                StaticVertices[1].Position.Y = yToUse - ScaleY + 2.6f;
                StaticVertices[1].TextureCoordinate.X = .054688f;
                StaticVertices[1].TextureCoordinate.Y = .62109375f;

                StaticVertices[2].Position.X = xToUse + ScaleX;
                StaticVertices[2].Position.Y = yToUse - ScaleY + 2.6f;
                StaticVertices[2].TextureCoordinate.X = .1171875f;
                StaticVertices[2].TextureCoordinate.Y = .62109375f;

                StaticVertices[3] = StaticVertices[0];
                StaticVertices[4] = StaticVertices[2];

                StaticVertices[5].Position.X = xToUse + ScaleX;
                StaticVertices[5].Position.Y = yToUse - ScaleY + 1.1f;
                StaticVertices[5].TextureCoordinate.X = .1171875f;
                StaticVertices[5].TextureCoordinate.Y = .570313f;

                GuiManager.WriteVerts(StaticVertices);

                #endregion

                #endregion

                #region Top button

                if (upButton.ButtonPushedState == ButtonPushedState.Down)
                {
                    StaticVertices[0].Position.X = xToUse - ScaleX;
                    StaticVertices[0].Position.Y = yToUse + ScaleY - 1.1f;
                    StaticVertices[0].TextureCoordinate.X = .25390625f;
                    StaticVertices[0].TextureCoordinate.Y = .675781f;

                    StaticVertices[1].Position.X = xToUse - ScaleX;
                    StaticVertices[1].Position.Y = yToUse + ScaleY;
                    StaticVertices[1].TextureCoordinate.X = .25390625f;
                    StaticVertices[1].TextureCoordinate.Y = .632813f;

                    StaticVertices[2].Position.X = xToUse + ScaleX;
                    StaticVertices[2].TextureCoordinate.X = .31640625f;
                    StaticVertices[2].TextureCoordinate.Y = .632813f;
                    StaticVertices[2].Position.Y = yToUse + ScaleY;

                    StaticVertices[3] = StaticVertices[0];
                    StaticVertices[4] = StaticVertices[2];

                    StaticVertices[5].Position.X = xToUse + ScaleX;
                    StaticVertices[5].TextureCoordinate.X = .31640625f;
                    StaticVertices[5].TextureCoordinate.Y = .675781f;
                    StaticVertices[5].Position.Y = yToUse + ScaleY - 1.1f;

                    GuiManager.WriteVerts(StaticVertices);
                }
                else
                {
                    StaticVertices[0].Position.X = xToUse - ScaleX;
                    StaticVertices[0].TextureCoordinate.X = .1875f;
                    StaticVertices[0].TextureCoordinate.Y = .675781f;
                    StaticVertices[0].Position.Y = yToUse + ScaleY - 1.1f;

                    StaticVertices[1].Position.X = xToUse - ScaleX;
                    StaticVertices[1].TextureCoordinate.X = .1875f;
                    StaticVertices[1].TextureCoordinate.Y = .632813f;
                    StaticVertices[1].Position.Y = yToUse + ScaleY;

                    StaticVertices[2].Position.X = xToUse + ScaleX;
                    StaticVertices[2].TextureCoordinate.X = .25f;
                    StaticVertices[2].TextureCoordinate.Y = .632813f;
                    StaticVertices[2].Position.Y = yToUse + ScaleY;

                    StaticVertices[3] = StaticVertices[0];
                    StaticVertices[4] = StaticVertices[2];

                    StaticVertices[5].Position.X = xToUse + ScaleX;
                    StaticVertices[5].TextureCoordinate.X = .25f;
                    StaticVertices[5].TextureCoordinate.Y = .675781f;
                    StaticVertices[5].Position.Y = yToUse + ScaleY - 1.1f;

                    GuiManager.WriteVerts(StaticVertices);
                }

                #endregion

                #region Bottom button

                if (downButton.ButtonPushedState == ButtonPushedState.Down)
                {
                    StaticVertices[0].Position.X = xToUse - ScaleX;
                    StaticVertices[0].TextureCoordinate.X = .25390625f;
                    StaticVertices[0].TextureCoordinate.Y = .71875f;
                    StaticVertices[0].Position.Y = yToUse - ScaleY;

                    StaticVertices[1].Position.X = xToUse - ScaleX;
                    StaticVertices[1].TextureCoordinate.X = .25390625f;
                    StaticVertices[1].TextureCoordinate.Y = .675781f;
                    StaticVertices[1].Position.Y = yToUse - ScaleY + 1.1f;

                    StaticVertices[2].Position.X = xToUse + ScaleX;
                    StaticVertices[2].TextureCoordinate.X = .31640625f;
                    StaticVertices[2].TextureCoordinate.Y = .675781f;
                    StaticVertices[2].Position.Y = yToUse - ScaleY + 1.1f;


                    StaticVertices[3] = StaticVertices[0];
                    StaticVertices[4] = StaticVertices[2];

                    StaticVertices[5].Position.X = xToUse + ScaleX;
                    StaticVertices[5].TextureCoordinate.X = .31640625f;
                    StaticVertices[5].TextureCoordinate.Y = .71875f;
                    StaticVertices[5].Position.Y = yToUse - ScaleY;

                    GuiManager.WriteVerts(StaticVertices);
                }
                else
                {

                    StaticVertices[0].Position.X = xToUse - ScaleX;
                    StaticVertices[0].TextureCoordinate.X = .1875f;
                    StaticVertices[0].TextureCoordinate.Y = .71875f;
                    StaticVertices[0].Position.Y = yToUse - ScaleY;

                    StaticVertices[1].Position.X = xToUse - ScaleX;
                    StaticVertices[1].TextureCoordinate.X = .1875f;
                    StaticVertices[1].TextureCoordinate.Y = .675781f;
                    StaticVertices[1].Position.Y = yToUse - ScaleY + 1.1f;

                    StaticVertices[2].Position.X = xToUse + ScaleX;
                    StaticVertices[2].TextureCoordinate.X = .25f;
                    StaticVertices[2].TextureCoordinate.Y = .675781f;
                    StaticVertices[2].Position.Y = yToUse - ScaleY + 1.1f;

                    StaticVertices[3] = StaticVertices[0];
                    StaticVertices[4] = StaticVertices[2];

                    StaticVertices[5].Position.X = xToUse + ScaleX;
                    StaticVertices[5].TextureCoordinate.X = .25f;
                    StaticVertices[5].TextureCoordinate.Y = .71875f;
                    StaticVertices[5].Position.Y = yToUse - ScaleY;

                    GuiManager.WriteVerts(StaticVertices);

                }

                #endregion

                #region positionBar
                xToUse = mWorldUnitX;
                yToUse = mWorldUnitY + mPositionBar.WorldUnitRelativeY;

                #region Top of bar
                StaticVertices[0].Position.X = xToUse - ScaleX + .1f;
                StaticVertices[0].TextureCoordinate.X = .11719f;
                StaticVertices[0].TextureCoordinate.Y = .62109375f;
                StaticVertices[0].Position.Y = yToUse + mPositionBar.ScaleY - .8f;

                StaticVertices[1].Position.X = xToUse - ScaleX + .1f;
                StaticVertices[1].TextureCoordinate.X = .11719f;
                StaticVertices[1].TextureCoordinate.Y = .59765625f;
                StaticVertices[1].Position.Y = yToUse + mPositionBar.ScaleY;

                StaticVertices[2].Position.X = xToUse + ScaleX - .1f;
                StaticVertices[2].TextureCoordinate.X = .17578125f;
                StaticVertices[2].TextureCoordinate.Y = .59765625f;
                StaticVertices[2].Position.Y = yToUse + mPositionBar.ScaleY;

                StaticVertices[3] = StaticVertices[0];
                StaticVertices[4] = StaticVertices[2];

                StaticVertices[5].Position.X = xToUse + ScaleX - .1f;
                StaticVertices[5].TextureCoordinate.X = .17578125f;
                StaticVertices[5].TextureCoordinate.Y = .62109375f;
                StaticVertices[5].Position.Y = yToUse + mPositionBar.ScaleY - .8f;

                GuiManager.WriteVerts(StaticVertices);

                #endregion

                #region Center of bar
                StaticVertices[0].Position.X = xToUse - ScaleX + .1f;
                StaticVertices[0].TextureCoordinate.X = .11719f;
                StaticVertices[0].TextureCoordinate.Y = .6328125f;
                StaticVertices[0].Position.Y = yToUse - mPositionBar.ScaleY + .8f;

                StaticVertices[1].Position.X = xToUse - ScaleX + .1f;
                StaticVertices[1].TextureCoordinate.X = .11719f;
                StaticVertices[1].TextureCoordinate.Y = .62109375f;
                StaticVertices[1].Position.Y = yToUse + mPositionBar.ScaleY - .8f;

                StaticVertices[2].Position.X = xToUse + ScaleX - .1f;
                StaticVertices[2].TextureCoordinate.X = .17578125f;
                StaticVertices[2].TextureCoordinate.Y = .62109375f;
                StaticVertices[2].Position.Y = yToUse + mPositionBar.ScaleY - .8f;

                StaticVertices[3] = StaticVertices[0];
                StaticVertices[4] = StaticVertices[2];

                StaticVertices[5].Position.X = xToUse + ScaleX - .1f;
                StaticVertices[5].TextureCoordinate.X = .17578125f;
                StaticVertices[5].TextureCoordinate.Y = .6328125f;
                StaticVertices[5].Position.Y = yToUse - mPositionBar.ScaleY + .8f;

                GuiManager.WriteVerts(StaticVertices);

                #endregion

                #region Bottom of bar
                StaticVertices[0].Position.X = xToUse - ScaleX + .1f;
                StaticVertices[0].TextureCoordinate.X = .11719f;
                StaticVertices[0].TextureCoordinate.Y = .59765625f;
                StaticVertices[0].Position.Y = yToUse - mPositionBar.ScaleY;

                StaticVertices[1].Position.X = xToUse - ScaleX + .1f;
                StaticVertices[1].TextureCoordinate.X = .11719f;
                StaticVertices[1].TextureCoordinate.Y = .62109375f;
                StaticVertices[1].Position.Y = yToUse - mPositionBar.ScaleY + .8f;

                StaticVertices[2].Position.X = xToUse + ScaleX - .1f;
                StaticVertices[2].TextureCoordinate.X = .17578125f;
                StaticVertices[2].TextureCoordinate.Y = .62109375f;
                StaticVertices[2].Position.Y = yToUse - mPositionBar.ScaleY + .8f;

                StaticVertices[3] = StaticVertices[0];
                StaticVertices[4] = StaticVertices[2];

                StaticVertices[5].Position.X = xToUse + ScaleX - .1f;
                StaticVertices[5].TextureCoordinate.X = .17578125f;
                StaticVertices[5].TextureCoordinate.Y = .59765625f;
                StaticVertices[5].Position.Y = yToUse - mPositionBar.ScaleY;

                GuiManager.WriteVerts(StaticVertices);

                #endregion

                #endregion
            }
            else
            {
                #region draw the back of the scrollbar

                #region Left of window
                StaticVertices[0].Position.X = xToUse - ScaleX + 1.1f;
                StaticVertices[0].Position.Y = yToUse - ScaleY;
                StaticVertices[0].TextureCoordinate.X = .054688f;
                StaticVertices[0].TextureCoordinate.Y = .570313f;

                StaticVertices[1].Position.X = xToUse - ScaleX + 1.1f;
                StaticVertices[1].Position.Y = yToUse + ScaleY;
                StaticVertices[1].TextureCoordinate.X = .1171875f;
                StaticVertices[1].TextureCoordinate.Y = .570313f;

                StaticVertices[2].Position.X = xToUse - ScaleX + 2.6f;
                StaticVertices[2].Position.Y = yToUse + ScaleY;
                StaticVertices[2].TextureCoordinate.X = .1171875f;
                StaticVertices[2].TextureCoordinate.Y = .62109375f;

                StaticVertices[3] = StaticVertices[0];
                StaticVertices[4] = StaticVertices[2];

                StaticVertices[5].Position.X = xToUse - ScaleX + 2.6f;
                StaticVertices[5].Position.Y = yToUse - ScaleY;
                StaticVertices[5].TextureCoordinate.X = .054688f;
                StaticVertices[5].TextureCoordinate.Y = .62109375f;

                GuiManager.WriteVerts(StaticVertices);

                #endregion

                #region Center of window
                StaticVertices[0].Position.X = xToUse - ScaleX + 2.6f;
                StaticVertices[0].Position.Y = yToUse - ScaleY;
                StaticVertices[0].TextureCoordinate.X = .054688f;
                StaticVertices[0].TextureCoordinate.Y = .62109375f;

                StaticVertices[1].Position.X = xToUse - ScaleX + 2.6f;
                StaticVertices[1].Position.Y = yToUse + ScaleY;
                StaticVertices[1].TextureCoordinate.X = .1171875f;
                StaticVertices[1].TextureCoordinate.Y = .62109375f;

                StaticVertices[2].Position.X = xToUse + ScaleX - 2.6f;
                StaticVertices[2].Position.Y = yToUse + ScaleY;
                StaticVertices[2].TextureCoordinate.X = .1171875f;
                StaticVertices[2].TextureCoordinate.Y = .6328125f;

                StaticVertices[3] = StaticVertices[0];
                StaticVertices[4] = StaticVertices[2];

                StaticVertices[5].Position.X = xToUse + ScaleX - 2.6f;
                StaticVertices[5].Position.Y = yToUse - ScaleY;
                StaticVertices[5].TextureCoordinate.X = .054688f;
                StaticVertices[5].TextureCoordinate.Y = .62109375f;

                GuiManager.WriteVerts(StaticVertices);

                #endregion

                #region Right of window
                StaticVertices[0].Position.X = xToUse + ScaleX - 2.6f;
                StaticVertices[0].Position.Y = yToUse - ScaleY;
                StaticVertices[0].TextureCoordinate.X = .054688f;
                StaticVertices[0].TextureCoordinate.Y = .62109375f;

                StaticVertices[1].Position.X = xToUse + ScaleX - 2.6f;
                StaticVertices[1].Position.Y = yToUse + ScaleY;
                StaticVertices[1].TextureCoordinate.X = .1171875f;
                StaticVertices[1].TextureCoordinate.Y = .62109375f;

                StaticVertices[2].Position.X = xToUse + ScaleX - 1.1f;
                StaticVertices[2].Position.Y = yToUse + ScaleY;
                StaticVertices[2].TextureCoordinate.X = .1171875f;
                StaticVertices[2].TextureCoordinate.Y = .570313f;

                StaticVertices[3] = StaticVertices[0];
                StaticVertices[4] = StaticVertices[2];

                StaticVertices[5].Position.X = xToUse + ScaleX - 1.1f;
                StaticVertices[5].Position.Y = yToUse - ScaleY;
                StaticVertices[5].TextureCoordinate.X = .054688f;
                StaticVertices[5].TextureCoordinate.Y = .570313f;

                GuiManager.WriteVerts(StaticVertices);

                #endregion

                #endregion

                #region Left button

                if (upButton.ButtonPushedState == ButtonPushedState.Down)
                {
                    StaticVertices[0].Position.X = xToUse - ScaleX;
                    StaticVertices[0].Position.Y = yToUse - ScaleY;
                    StaticVertices[0].TextureCoordinate.X = .25390625f;
                    StaticVertices[0].TextureCoordinate.Y = .632813f;

                    StaticVertices[1].Position.X = xToUse - ScaleX;
                    StaticVertices[1].Position.Y = yToUse + ScaleY;
                    StaticVertices[1].TextureCoordinate.X = .31640625f;
                    StaticVertices[1].TextureCoordinate.Y = .632813f;

                    StaticVertices[2].Position.X = xToUse - ScaleX + 1.1f;
                    StaticVertices[2].Position.Y = yToUse + ScaleY;
                    StaticVertices[2].TextureCoordinate.X = .31640625f;
                    StaticVertices[2].TextureCoordinate.Y = .675781f;

                    StaticVertices[3] = StaticVertices[0];
                    StaticVertices[4] = StaticVertices[2];

                    StaticVertices[5].Position.X = xToUse - ScaleX + 1.1f;
                    StaticVertices[5].Position.Y = yToUse - ScaleY;
                    StaticVertices[5].TextureCoordinate.X = .25390625f;
                    StaticVertices[5].TextureCoordinate.Y = .675781f;

                    GuiManager.WriteVerts(StaticVertices);
                }
                else
                {
                    StaticVertices[0].Position.X = xToUse - ScaleX;
                    StaticVertices[0].Position.Y = yToUse - ScaleY;
                    StaticVertices[0].TextureCoordinate.X = .1875f;
                    StaticVertices[0].TextureCoordinate.Y = .632813f;

                    StaticVertices[1].Position.X = xToUse - ScaleX;
                    StaticVertices[1].Position.Y = yToUse + ScaleY;
                    StaticVertices[1].TextureCoordinate.X = .25f;
                    StaticVertices[1].TextureCoordinate.Y = .632813f;

                    StaticVertices[2].Position.X = xToUse - ScaleX + 1.1f;
                    StaticVertices[2].Position.Y = yToUse + ScaleY;
                    StaticVertices[2].TextureCoordinate.X = .25f;
                    StaticVertices[2].TextureCoordinate.Y = .675781f;

                    StaticVertices[3] = StaticVertices[0];
                    StaticVertices[4] = StaticVertices[2];

                    StaticVertices[5].Position.X = xToUse - ScaleX + 1.1f;
                    StaticVertices[5].Position.Y = yToUse - ScaleY;
                    StaticVertices[5].TextureCoordinate.X = .1875f;
                    StaticVertices[5].TextureCoordinate.Y = .675781f;

                    GuiManager.WriteVerts(StaticVertices);
                }

                #endregion

                #region Right button

                if (downButton.ButtonPushedState == ButtonPushedState.Down)
                {
                    StaticVertices[0].Position.X = xToUse + ScaleX - 1.1f;
                    StaticVertices[0].Position.Y = yToUse - ScaleY;
                    StaticVertices[0].TextureCoordinate.X = .25390625f;
                    StaticVertices[0].TextureCoordinate.Y = .675781f;

                    StaticVertices[1].Position.X = xToUse + ScaleX - 1.1f;
                    StaticVertices[1].Position.Y = yToUse + ScaleY;
                    StaticVertices[1].TextureCoordinate.X = .31640625f;
                    StaticVertices[1].TextureCoordinate.Y = .675781f;

                    StaticVertices[2].Position.X = xToUse + ScaleX;
                    StaticVertices[2].Position.Y = yToUse + ScaleY;
                    StaticVertices[2].TextureCoordinate.X = .31640625f;
                    StaticVertices[2].TextureCoordinate.Y = .71875f;

                    StaticVertices[3] = StaticVertices[0];
                    StaticVertices[4] = StaticVertices[2];

                    StaticVertices[5].Position.X = xToUse + ScaleX;
                    StaticVertices[5].Position.Y = yToUse - ScaleY;
                    StaticVertices[5].TextureCoordinate.X = .25390625f;
                    StaticVertices[5].TextureCoordinate.Y = .71875f;

                    GuiManager.WriteVerts(StaticVertices);
                }
                else
                {

                    StaticVertices[0].Position.X = xToUse + ScaleX - 1.1f;
                    StaticVertices[0].Position.Y = yToUse - ScaleY;
                    StaticVertices[0].TextureCoordinate.X = .1875f;
                    StaticVertices[0].TextureCoordinate.Y = .675781f;

                    StaticVertices[1].Position.X = xToUse + ScaleX - 1.1f;
                    StaticVertices[1].Position.Y = yToUse + ScaleY;
                    StaticVertices[1].TextureCoordinate.X = .25f;
                    StaticVertices[1].TextureCoordinate.Y = .675781f;

                    StaticVertices[2].Position.X = xToUse + ScaleX;
                    StaticVertices[2].Position.Y = yToUse + ScaleY;
                    StaticVertices[2].TextureCoordinate.X = .25f;
                    StaticVertices[2].TextureCoordinate.Y = .71875f;

                    StaticVertices[3] = StaticVertices[0];
                    StaticVertices[4] = StaticVertices[2];

                    StaticVertices[5].Position.X = xToUse + ScaleX;
                    StaticVertices[5].Position.Y = yToUse - ScaleY;
                    StaticVertices[5].TextureCoordinate.X = .1875f;
                    StaticVertices[5].TextureCoordinate.Y = .71875f;

                    GuiManager.WriteVerts(StaticVertices);

                }

                #endregion

                #region positionBar
                xToUse = mWorldUnitX + mPositionBar.WorldUnitRelativeX;
                yToUse = mWorldUnitY;

                #region left of bar
                StaticVertices[0].Position.X = xToUse - mPositionBar.ScaleX;
                StaticVertices[0].Position.Y = yToUse - ScaleY + .1f;
                StaticVertices[0].TextureCoordinate.X = .11719f;
                StaticVertices[0].TextureCoordinate.Y = .59765625f;

                StaticVertices[1].Position.X = xToUse - mPositionBar.ScaleX;
                StaticVertices[1].Position.Y = yToUse + ScaleY - .1f;
                StaticVertices[1].TextureCoordinate.X = .17578125f;
                StaticVertices[1].TextureCoordinate.Y = .59765625f;

                StaticVertices[2].Position.X = xToUse - mPositionBar.ScaleX + .8f;
                StaticVertices[2].Position.Y = yToUse + ScaleY - .1f;
                StaticVertices[2].TextureCoordinate.X = .17578125f;
                StaticVertices[2].TextureCoordinate.Y = .62109375f;

                StaticVertices[3] = StaticVertices[0];
                StaticVertices[4] = StaticVertices[2];

                StaticVertices[5].Position.X = xToUse - mPositionBar.ScaleX + .8f;
                StaticVertices[5].Position.Y = yToUse - ScaleY + .1f;
                StaticVertices[5].TextureCoordinate.X = .11719f;
                StaticVertices[5].TextureCoordinate.Y = .62109375f;

                GuiManager.WriteVerts(StaticVertices);

                #endregion

                #region Center of bar
                StaticVertices[0].Position.X = xToUse - mPositionBar.ScaleX + .8f;
                StaticVertices[0].Position.Y = yToUse - ScaleY + .1f;
                StaticVertices[0].TextureCoordinate.X = .11719f;
                StaticVertices[0].TextureCoordinate.Y = .62109375f;

                StaticVertices[1].Position.X = xToUse - mPositionBar.ScaleX + .8f;
                StaticVertices[1].Position.Y = yToUse + ScaleY - .1f;
                StaticVertices[1].TextureCoordinate.X = .17578125f;
                StaticVertices[1].TextureCoordinate.Y = .62109375f;

                StaticVertices[2].Position.X = xToUse + mPositionBar.ScaleX - .8f;
                StaticVertices[2].Position.Y = yToUse + ScaleY - .1f;
                StaticVertices[2].TextureCoordinate.X = .17578125f;
                StaticVertices[2].TextureCoordinate.Y = .6328125f;

                StaticVertices[3] = StaticVertices[0];
                StaticVertices[4] = StaticVertices[2];

                StaticVertices[5].Position.X = xToUse + mPositionBar.ScaleX - .8f;
                StaticVertices[5].Position.Y = yToUse - ScaleY + .1f;
                StaticVertices[5].TextureCoordinate.X = .11719f;
                StaticVertices[5].TextureCoordinate.Y = .6328125f;

                GuiManager.WriteVerts(StaticVertices);

                #endregion

                #region right of bar
                StaticVertices[0].Position.X = xToUse + mPositionBar.ScaleX - .8f;
                StaticVertices[0].Position.Y = yToUse - ScaleY + .1f;
                StaticVertices[0].TextureCoordinate.X = .11719f;
                StaticVertices[0].TextureCoordinate.Y = .62109375f;

                StaticVertices[1].Position.X = xToUse + mPositionBar.ScaleX - .8f;
                StaticVertices[1].Position.Y = yToUse + ScaleY - .1f;
                StaticVertices[1].TextureCoordinate.X = .17578125f;
                StaticVertices[1].TextureCoordinate.Y = .62109375f;

                StaticVertices[2].Position.X = xToUse + mPositionBar.ScaleX;
                StaticVertices[2].Position.Y = yToUse + ScaleY - .1f;
                StaticVertices[2].TextureCoordinate.X = .17578125f;
                StaticVertices[2].TextureCoordinate.Y = .59765625f;

                StaticVertices[3] = StaticVertices[0];
                StaticVertices[4] = StaticVertices[2];

                StaticVertices[5].Position.X = xToUse + mPositionBar.ScaleX;
                StaticVertices[5].Position.Y = yToUse - ScaleY + .1f;
                StaticVertices[5].TextureCoordinate.X = .11719f;
                StaticVertices[5].TextureCoordinate.Y = .59765625f;

                GuiManager.WriteVerts(StaticVertices);

                #endregion

                #endregion

            }

        }
#endif
        #region XML Docs
        /// <summary>
        /// Keeps the ScrollBar's position bar from overlapping the up and down buttons and 
        /// keeps its horizontal position inside the ScrollBar.
        /// </summary>
        #endregion
        internal void FixBar()
        {
            if (GuiManagerDrawn)
            {
                if (mAlignment == ScrollBarAlignment.Vertical)
                {
                    float area = ScaleY - 2;

                    if (mPositionBar.ScaleY + mPositionBar.WorldUnitRelativeY > ScaleY - 2)
                        mPositionBar.WorldUnitRelativeY = ScaleY - 2 - mPositionBar.ScaleY;

                    if (-mPositionBar.ScaleY + mPositionBar.WorldUnitRelativeY < -ScaleY + 2)
                        mPositionBar.WorldUnitRelativeY = -ScaleY + 2 + mPositionBar.ScaleY;

                    double topOfPosBarFromTopOfScrollBar =
                        mPositionBar.Parent.ScaleY - 2 - (mPositionBar.WorldUnitRelativeY + mPositionBar.ScaleY);

                    double totalAvailableArea = 2 * (mScaleY - 2) - 2 * mPositionBar.ScaleY;
                    
                    double invisibleCount = mTotalCount * 
                        (1 - view);
                    if (totalAvailableArea == 0)
                    {
                        mFirstVisibleIndex = 0;
                    }
                    else
                    {
                        mFirstVisibleIndex = (int)System.Math.Round(
                            invisibleCount * topOfPosBarFromTopOfScrollBar / totalAvailableArea);
                    }

                    mPositionBar.WorldUnitRelativeX = 0;
                }
                else
                {
                    if (mPositionBar.ScaleX + mPositionBar.WorldUnitRelativeX > ScaleX - 2)
                        mPositionBar.WorldUnitRelativeX = ScaleX - 2 - mPositionBar.ScaleX;

                    if (-mPositionBar.ScaleX + mPositionBar.WorldUnitRelativeX < -ScaleX + 2)
                        mPositionBar.WorldUnitRelativeX = -ScaleX + 2 + mPositionBar.ScaleX;

                    mPositionBar.WorldUnitRelativeY = 0;

                    double topOfPosBarFromTopOfScrollBar =
                        mPositionBar.Parent.ScaleX - 2 - (mPositionBar.WorldUnitRelativeX + mPositionBar.ScaleX);

                    mFirstVisibleIndex = (int)System.Math.Round(topOfPosBarFromTopOfScrollBar /
                                  (2 * (mScaleX - 2) * mSensitivity));

                }
            }
            else
            {
                if (mAlignment == ScrollBarAlignment.Vertical)
                {
                    if (mPositionBar.ScaleY + mPositionBar.WorldUnitRelativeY > ScaleY - 2)
                        mPositionBar.WorldUnitRelativeY = ScaleY - 2 - mPositionBar.ScaleY;

                    if (-mPositionBar.ScaleY + mPositionBar.WorldUnitRelativeY < -ScaleY + 2)
                        mPositionBar.WorldUnitRelativeY = -ScaleY + 2 + mPositionBar.ScaleY;

                    mPositionBar.WorldUnitRelativeX = 0;

                    double topOfPosBarFromTopOfScrollBar =
                        mPositionBar.Parent.ScaleY - 2 - (mPositionBar.WorldUnitRelativeY + mPositionBar.ScaleY);

                    double totalAvailableArea = 2 * (mScaleY - 2) - 2 * mPositionBar.ScaleY;

                    double invisibleCount = mTotalCount *
                        (1 - view);

                    mFirstVisibleIndex = (int)System.Math.Round(
                        invisibleCount * topOfPosBarFromTopOfScrollBar / totalAvailableArea);
                }
                else
                {
                    if (mPositionBar.WorldUnitRelativeX > 2 * mScaleX - 3)
                        mPositionBar.WorldUnitRelativeX = 2 * mScaleX - 3;
                    if (mPositionBar.WorldUnitRelativeX < 3)
                        mPositionBar.WorldUnitRelativeX = 3;

                    mPositionBar.WorldUnitRelativeY = 1;

                    double topOfPosBarFromTopOfScrollBar =
                        mPositionBar.Parent.ScaleX - 2 - (mPositionBar.WorldUnitRelativeX + mPositionBar.ScaleX);

                    mFirstVisibleIndex = (int)System.Math.Round(topOfPosBarFromTopOfScrollBar /
                                  (2 * (mScaleX - 2) * mSensitivity));

                }
            }
        }

#if !SILVERLIGHT
        internal override int GetNumberOfVerticesToDraw()
		{
			return 48; // 6 * (base (3) + topButton (1) + bottomButton (1) + scrollbar(3) )
		}
#endif

        internal void RaisePositionBarMoveEvent()
        {
            if (PositionBarMove != null)
                PositionBarMove(this);
        }


		public override void TestCollision(Cursor cursor)
		{
			base.TestCollision(cursor);


			if(cursor.PrimaryPush && cursor.WindowOver == mPositionBar)
			{// dragging the position bar
				cursor.GrabWindow(mPositionBar);
			}
		}

        #endregion

        #endregion
    }// end of ScrollBox class
}// end of namespace