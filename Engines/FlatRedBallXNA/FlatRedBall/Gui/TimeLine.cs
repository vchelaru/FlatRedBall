using System;
using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.ManagedSpriteGroups;

#if FRB_MDX
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
#else

#endif
using FlatRedBall.Graphics;
using System.Collections.Generic;



namespace FlatRedBall.Gui
{
	/// <summary>
	/// Summary description for TimeLine.
	/// </summary>
	public class TimeLine : Window
	{
		#region Enums
        /// <summary>
        /// The time units that the TimeLine can show.
        /// </summary>
		public enum TimeUnit
        {
            #region XML Docs
            /// <summary>
            /// The time unit of 1/1000 fo a second.
            /// </summary>
            #endregion
            Millisecond,

            #region XML Docs
            /// <summary>
            /// The time unit of one second.
            /// </summary>
            #endregion
            Second
		}
		#endregion

		#region Fields

//		FrbTexture blackBar;

		double mMinimumValue;
		double mMaximumValue;

		double mStart;
		double mValueWidth;

		double mVerticalBarIncrement = 10;
		double mSmallVerticalBarIncrement = 2.5f;
        bool mShowValues = true;

		Button positionBar;

		private double mCurrentValue;
        private double mBeforeChangeValue;

		TimeUnit mTimeUnitDisplayed = TimeUnit.Second;

        //float mTextSpacing = .6f;
        //float mTextScale = .6f;

        int mPrecision = 15; // essentially no precision limit

        List<string> mCachedIncrementValues = new List<string>();
        double mCacheStartValue;
        double mCacheVerticalBarIncrement;
        double mCacheValueWidth;

        ScrollBar mScrollBar;

		#endregion

		#region Properties

        #region XML Docs
        /// <summary>
        /// The TimeLine's value before the last change was initiated.  Can be used for undos.
        /// </summary>
        #endregion
        public double BeforeChangeValue
        {
            get { return mBeforeChangeValue; }
        }

        #region XML Docs
        /// <summary>
        /// The precision-adjusted current value on the TimeLine.
        /// </summary>
        /// <remarks>
        /// This value will always fall between the MinimumValue and MaximumValue properties.
        /// Setting this value will also change the position of the bar and potentially change the
        /// Start property so that the bar is in view.
        /// </remarks>
        #endregion
        public double CurrentValue
		{
			get{	return System.Math.Round(mCurrentValue, mPrecision);	}
			set
			{
                if (mCurrentValue != value)
                {
                    mCurrentValue = value;

                    if (mCurrentValue < mMinimumValue)
                        mCurrentValue = mMinimumValue;
                    if (mCurrentValue > mMaximumValue)
                        mCurrentValue = mMaximumValue;

                    SetBarToValue();

                    if (mScrollBar != null)
                    {
                        mScrollBar.RatioDown = (float)((this.Start - MinimumValue) / (MaximumValue - MinimumValue));
                    }
                }

                
			}
        }


        public double CurrentValueRaw
        {
            get { return mCurrentValue; }
        }


        private bool HasChangedSinceLastCache
        {
            get
            {
                return mCacheStartValue != mStart ||
                    mCacheVerticalBarIncrement != mVerticalBarIncrement ||
                    mCacheValueWidth != mValueWidth;
            }
        }


        public bool HasScrollBar
        {
            get { return mScrollBar != null; }
            set
            {
                if (value != HasScrollBar)
                {
                    if (value)
                    {
                        CreateScrollBar();
                    }
                    else
                    {
                        DestroyScrollBar();
                    }

                    UpdatePositionBarYValues();
                }
            }
        }


        public override bool IsWindowOrChildrenReceivingInput
        {
            get
            {
                return base.IsWindowOrChildrenReceivingInput || mCursor.mWindowGrabbed == this.positionBar ;
            }
        }

        #region XML Docs
        /// <summary>
        /// The minimum value on the TimeLine.
        /// </summary>
        #endregion
        public double MinimumValue
		{
			get{ return mMinimumValue;}
			set
            { 
                mMinimumValue = value;
                FixStartAndCurrentValue();

                UpdateScrollBarToRange();
            }
        }

        #region XML Docs
        /// <summary>
        /// The maximum value on the TimeLine
        /// </summary>
        #endregion
        public double MaximumValue
		{
			get{	return mMaximumValue;}
			set
            {	
                mMaximumValue = value;

                FixStartAndCurrentValue();
                UpdateScrollBarToRange();
            }
        }

        #region XML Docs
        /// <summary>
        /// The number of decimal points that the TimeLine rounds its CurrentValue to.
        /// </summary>
        #endregion
        public int Precision
        {
            get { return mPrecision; }
            set { mPrecision = value; }
        }

        #region XML Docs
        /// <summary>
        /// The Y scale of the TimeLine.
        /// </summary>
        #endregion
        public new float ScaleY
		{
			get{	return mScaleY;	}
			set
			{
				mScaleY = value;

                UpdatePositionBarYValues();
			}
        }

        #region XML Docs
        /// <summary>
        /// The X scale of the TimeLine.
        /// </summary>
        #endregion
        public new float ScaleX
		{
			get{	return mScaleX; }
			set
			{
                base.ScaleX = value;
				CurrentValue = CurrentValue;
				SetBarToValue();
			}
        }

        #region XML Docs
        /// <summary>
        /// The first visible value in the TimeLine.
        /// </summary>
        #endregion
        public double Start
		{
			get{	return mStart;}
			set
			{	
				mStart = value;	
				if(mStart < mMinimumValue)
					mStart = mMinimumValue;

                positionBar.WorldUnitRelativeX = ValueToPositionRelative(mCurrentValue);
                UpdatePositionBarVisibility();

                if (mScrollBar != null)
                {
                    mScrollBar.RatioDown = (float)((this.Start - MinimumValue) / (MaximumValue - MinimumValue));
                }
				//SetBarToValue();
			}
        }



        #region XML Docs
        /// <summary>
        /// The TimeUnit that the PropertyGrid displays.
        /// </summary>
        #endregion
        public TimeUnit TimeUnitDisplayed
        {
            get { return mTimeUnitDisplayed; }
            set { mTimeUnitDisplayed = value; }
        }

        /// <summary>
        /// The displayed range.  The last visible value equals Start + ValueWidth.
        /// </summary>
		public double ValueWidth
		{
			get{	return mValueWidth;}
			set
            {	
                mValueWidth = value; 	
                SetBarToValue();
            }
        }

        #region XML Docs
        /// <summary>
        /// The distance between the larger markings on the TimeLine.
        /// </summary>
        #endregion
        public double VerticalBarIncrement
        {
            get { return mVerticalBarIncrement; }
            set { mVerticalBarIncrement = value; }
        }

        #region XML Docs
        /// <summary>
        /// The distance between the smaller markings on the TimeLine.
        /// </summary>
        #endregion
        public double SmallVerticalBarIncrement
        {
            get { return mSmallVerticalBarIncrement; }
            set { mSmallVerticalBarIncrement = value; }
        }

        #region XML Docs
        /// <summary>
        /// Whether values (as text) are drawn above the TimeLine.
        /// </summary>
        #endregion
        public bool ShowValues
        {
            get { return mShowValues; }
            set { mShowValues = value; }
        }

		#endregion
		
		#region Events

        #region XML Docs
        /// <summary>
        /// Raised when the user changes the CurrentValue with the Cursor - by dragging
        /// the position bar or clicking to reposition the position bar.
        /// </summary>
        #endregion
        public event GuiMessage GuiChange = null;

		#endregion

        #region Event Methods

        #region XML Docs
        /// <summary>
        /// Method raised when the user clicks on the Window.  This moves
        /// the position bar to the appropriate location.
        /// </summary>
        /// <param name="callingWindow">The window raising this event.</param>
        #endregion
        protected virtual void ClickEvent(Window callingWindow)
        {
            mBeforeChangeValue = mCurrentValue;

            #region SpriteFrame Drawn
            if (GuiManagerDrawn == false)
            {
                float x = 0;
                float y = 0;

                mCursor.GetCursorPosition(out x, out y, SpriteFrame.Z);

                CurrentValue = PositionToValueAbsolute(x);


                FixBar();

                mCurrentValue = PositionToValueRelative((float)positionBar.WorldUnitRelativeX);

                if (this.GuiChange != null)
                    this.GuiChange(this);
            }
            #endregion

            #region GuiManager Drawn
            else
            {
                // make sure the cursor is not on the move bar
                if (IsCursorOnMoveBar(mCursor) == false)
                {
                    double newValue = PositionToValueAbsolute((float)(mCursor.XForUI));

                    // make sure the value isn't outside of what's visible.
                    newValue = System.Math.Max(mStart, newValue);
                    newValue = System.Math.Min(mStart + mValueWidth, newValue);

                    CurrentValue = newValue;

                    FixBar();
                    if (this.GuiChange != null)
                        this.GuiChange(this);
                }
            }
            #endregion
        }

        private void PositionBarDrag(Window callingWindow)
        {

            FixBar();

            mCurrentValue = PositionToValueRelative((float)positionBar.WorldUnitRelativeX);

            if (this.GuiChange != null)
                this.GuiChange(this);
        }

        private void ScrollBarChanged(Window callingWindow)
        {
            double range = MaximumValue - MinimumValue;

            if (!double.IsPositiveInfinity(range) && !double.IsNegativeInfinity(range))
            {

                Start = MinimumValue + range * mScrollBar.RatioDown;
            }
        }

        #endregion

        #region Methods

        #region Constructors

        #region XML Docs
        /// <summary>
        /// Creates a new TimeLine;
        /// </summary>
        /// <param name="cursor">The Cursor that will interact with the TimeLine.</param>
        #endregion
        public TimeLine(Cursor cursor) : 
            base(cursor)
		{
			mMinimumValue = 0;
			mMaximumValue = double.PositiveInfinity;;
			mStart = 0;
			mValueWidth = 100;

            mScaleX = 10;
            mScaleY = 1.5f;

            positionBar = new Button(mCursor);
            AddWindow(positionBar);

			positionBar.ScaleX = .5f;
			positionBar.ScaleY = 1;
			positionBar.Dragging += new GuiMessage(PositionBarDrag);
			positionBar.SetPositionTL(0, ScaleY);

            positionBar.overlayTL = new FlatRedBall.Math.Geometry.Point(0, 225 / 256.0f);
            positionBar.overlayTR = new FlatRedBall.Math.Geometry.Point(1 / 256.0f, 225 / 256.0f);
            positionBar.overlayBL = new FlatRedBall.Math.Geometry.Point(1 / 256.0f, 226 / 256.0f);
            positionBar.overlayBR = new FlatRedBall.Math.Geometry.Point(0, 226 / 256.0f);

            positionBar.Name = "TimeLine position bar";

	
			SetBarToValue();

			this.Click += new GuiMessage(ClickEvent);
        }

        #region XML Docs
        /// <summary>
        /// Creates a new TimeLine using the argument GuiSkin to control the visible representation.
        /// </summary>
        /// <param name="guiSkin">The GuiSkin to use.</param>
        /// <param name="cursor">The Cursor that will interact with the TimeLine.</param>
        #endregion
        public TimeLine(GuiSkin guiSkin, Cursor cursor) :
            base (guiSkin, cursor)
        {
            mMinimumValue = 0;
            mMaximumValue = double.PositiveInfinity;
            mStart = 0;

            mValueWidth = 100;

            //positionBar = AddButton(positionBarSpriteFrame);
            positionBar.Dragging += new GuiMessage(PositionBarDrag);
            positionBar.SetPositionTL(0, ScaleY);

            SetBarToValue();

            this.Click += new GuiMessage(ClickEvent);
        }
        #endregion

        #region Public Methods


        #region XML Docs
        /// <summary>
        /// Automatically sets the VerticalBarIncrement and SmallVerticalBarIncrement
        /// based off of the ValueWidth property.
        /// </summary>
        #endregion
        public void AutoCalculateVerticalLineSpacing()
		{
			double maxWidth = mValueWidth/10;


			int numDigits = 0;

			float widthToUse;

			if(ValueWidth > 10)
			{

				numDigits = ((int)maxWidth).ToString().Length - 1;
				widthToUse = (float)System.Math.Pow(10, numDigits);
			}

			else
			{
				widthToUse = 1;
				int numDecimals = 0;

				while(widthToUse > maxWidth)
				{
					widthToUse *= .1f;
					numDecimals++;
				}


				widthToUse = (float) System.Math.Round(widthToUse, numDecimals);
			}
			
			if(widthToUse < maxWidth/4)
				widthToUse *= 5;

			if(widthToUse < maxWidth/2)
				widthToUse *= 2;

			this.mVerticalBarIncrement = widthToUse;
			this.mSmallVerticalBarIncrement = widthToUse/5;



		}

        /// <summary>
        /// Raises the events which raise when the GUI changes.
        /// </summary>
		public void CallOnGUIChange()
		{
			PositionBarDrag(null);
        }

        #region XML Docs
        /// <summary>
        /// Clears all events.
        /// </summary>
        #endregion
        public override void ClearEvents()
        {
            base.ClearEvents();
            GuiChange = null;
        }

        #region XML Docs
        /// <summary>
        /// Keeps the position bar within the visible range of the TimeLine.
        /// </summary>
        #endregion
        public void FixBar()
        {
            //			if(ScaleY > ScaleX)
            //			{
            //				if(positionBar.si.RelativeY - positionBar.si.ScaleY < - si.ScaleY + 2)
            //					positionBar.si.RelativeY = - si.ScaleY + 2 + positionBar.si.ScaleY;
            //				if(positionBar.si.RelativeY + positionBar.si.ScaleY > si.ScaleY - 2)
            //					positionBar.si.RelativeY =  si.ScaleY - 2 - positionBar.si.ScaleY;
            //			}
            //			else
            //			{
            if (positionBar.WorldUnitRelativeX < -ScaleX + 1)
                positionBar.WorldUnitRelativeX = -ScaleX + 1;
            if (positionBar.WorldUnitRelativeX > ScaleX - 1)
                positionBar.WorldUnitRelativeX = ScaleX - 1;
            //			}

            UpdatePositionBarYValues();

            positionBar.UpdateDependencies();
        }


        #endregion

        #region Internal Methods

        float[] mLargeIncrementPositions = new float[1];
        float[] mLargeIncrementValues = new float[1];

        internal override void DrawSelfAndChildren(Camera camera)
		{
            if (GuiManagerDrawn == false) return;


            base.DrawSelfAndChildren(camera);

            float xToUse = WorldUnitX;
			float yToUse = positionBar.WorldUnitY;

            #region Set the color and Z of the vertices once since they'll always remain the same.
            StaticVertices[0].Color = StaticVertices[1].Color = StaticVertices[2].Color = StaticVertices[3].Color = StaticVertices[4].Color = StaticVertices[5].Color = mColor;

			StaticVertices[0].Position.Z = StaticVertices[1].Position.Z = StaticVertices[2].Position.Z = 
                StaticVertices[3].Position.Z = StaticVertices[4].Position.Z = StaticVertices[5].Position.Z = 
                camera.Z + FlatRedBall.Math.MathFunctions.ForwardVector3.Z * 100;
            #endregion


            // give the window a 1 unit border for the elements


			#region draw the horizontal bar
			StaticVertices[0].Position.X = xToUse - ScaleX+1;
			StaticVertices[0].Position.Y = yToUse - .1f;
			StaticVertices[0].TextureCoordinate.X = .59f;
			StaticVertices[0].TextureCoordinate.Y = .84f;

			StaticVertices[1].Position.X = xToUse - ScaleX + 1;
			StaticVertices[1].Position.Y = yToUse + .1f;
			StaticVertices[1].TextureCoordinate.X = .59f;
			StaticVertices[1].TextureCoordinate.Y = .835f;

			StaticVertices[2].Position.X = xToUse + this.ScaleX - 1;
			StaticVertices[2].Position.Y = yToUse + .1f;
			StaticVertices[2].TextureCoordinate.X = .591f;
			StaticVertices[2].TextureCoordinate.Y = .835f;
			
			StaticVertices[3] = StaticVertices[0];
			StaticVertices[4] = StaticVertices[2];
			
            StaticVertices[5].Position.X = xToUse + this.ScaleX - 1;
			StaticVertices[5].Position.Y = yToUse - .1f;
			StaticVertices[5].TextureCoordinate.X = .591f;
			StaticVertices[5].TextureCoordinate.Y = .84f;

            GuiManager.WriteVerts(StaticVertices);

			#endregion

			#region draw the larger increments
			double incrementsPassed = mStart/mVerticalBarIncrement;
			if(incrementsPassed != (int)mStart/mVerticalBarIncrement)
				incrementsPassed = (int)incrementsPassed + 1;

			double xPosOfVerticalBar = incrementsPassed * mVerticalBarIncrement;

			// now that we know where the first xPosOfVerticalBar is, we can calculate how many large increment lines/values we'll have.

			float offset = (float)((incrementsPassed * mVerticalBarIncrement) - mStart);

			if(mLargeIncrementPositions.Length !=  1 + (int)((mValueWidth-offset)/mVerticalBarIncrement))
                mLargeIncrementPositions = new float[ 1 + (int)((mValueWidth-offset)/mVerticalBarIncrement)];

            // This is handled in the 
            //if( mLargeIncrementValues.Length !=  1 + (int)((mValueWidth-offset)/mVerticalBarIncrement))
            //    mLargeIncrementValues = new float[ 1 + (int)((mValueWidth-offset)/mVerticalBarIncrement)];

			int i = 0;

			float barX;

            float barY = yToUse;

			while(xPosOfVerticalBar <= mStart + mValueWidth && i < mLargeIncrementPositions.Length)
			{
				barX = WorldUnitX + ValueToPositionRelative(xPosOfVerticalBar);


				StaticVertices[0].Position.X = barX - .2f;
				StaticVertices[0].Position.Y = barY - .7f;
				StaticVertices[0].TextureCoordinate.X = .59f;
				StaticVertices[0].TextureCoordinate.Y = .84f;

				StaticVertices[1].Position.X = barX - .2f;
				StaticVertices[1].Position.Y = barY + .7f;
				StaticVertices[1].TextureCoordinate.X = .59f;
				StaticVertices[1].TextureCoordinate.Y = .835f;

				StaticVertices[2].Position.X = barX + .2f;
				StaticVertices[2].Position.Y = barY + .7f;
				StaticVertices[2].TextureCoordinate.X = .591f;
				StaticVertices[2].TextureCoordinate.Y = .835f;

				StaticVertices[3] = StaticVertices[0];
				StaticVertices[4] = StaticVertices[2];

				StaticVertices[5].Position.X = barX + .2f;
				StaticVertices[5].Position.Y = barY - .7f;
				StaticVertices[5].TextureCoordinate.X = .591f;
				StaticVertices[5].TextureCoordinate.Y = .84f;

                GuiManager.WriteVerts(StaticVertices);

				mLargeIncrementPositions[i] = barX;

				switch(this.mTimeUnitDisplayed)
				{
					case TimeUnit.Millisecond:
						mLargeIncrementValues[i] = (float)(xPosOfVerticalBar * 1000);
						break;
					case TimeUnit.Second:
						mLargeIncrementValues[i] = (float)(xPosOfVerticalBar);
						break;
			
				}
				xPosOfVerticalBar += mVerticalBarIncrement;
				i++;
			}

			#endregion

			#region draw the small increments
			incrementsPassed = mStart/mSmallVerticalBarIncrement;
			if(incrementsPassed != (int)mStart/mSmallVerticalBarIncrement)
				incrementsPassed = (int)incrementsPassed + 1;


            barY = yToUse;


			xPosOfVerticalBar = incrementsPassed * mSmallVerticalBarIncrement;
			while(xPosOfVerticalBar <= mStart + mValueWidth)
			{
                barX = WorldUnitX + ValueToPositionRelative(xPosOfVerticalBar);

				StaticVertices[0].Position.X = barX - .1f;
				StaticVertices[0].Position.Y = barY - .5f;
				StaticVertices[0].TextureCoordinate.X = .59f;
				StaticVertices[0].TextureCoordinate.Y = .84f;

				StaticVertices[1].Position.X = barX - .1f;
				StaticVertices[1].Position.Y = barY + .5f;
				StaticVertices[1].TextureCoordinate.X = .59f;
				StaticVertices[1].TextureCoordinate.Y = .835f;

				StaticVertices[2].Position.X = barX + .1f;
				StaticVertices[2].Position.Y = barY + .5f;
				StaticVertices[2].TextureCoordinate.X = .591f;
				StaticVertices[2].TextureCoordinate.Y = .835f;

				StaticVertices[3] = StaticVertices[0];
				StaticVertices[4] = StaticVertices[2];

				StaticVertices[5].Position.X = barX + .1f;
				StaticVertices[5].Position.Y = barY - .5f;
				StaticVertices[5].TextureCoordinate.X = .591f;
				StaticVertices[5].TextureCoordinate.Y = .84f;

                GuiManager.WriteVerts(StaticVertices);

				xPosOfVerticalBar += mSmallVerticalBarIncrement;

			}
			#endregion

			#region draw the large increment text

            if (mShowValues)
            {
                float yPos = WorldUnitY + ScaleY + GuiManager.TextHeight / 2.0f - .2f;

                TextManager.mYForVertexBuffer = yPos;

#if FRB_MDX
            TextManager.mZForVertexBuffer = camera.Position.Z + 100;
            TextManager.mRedForVertexBuffer = 20;
            TextManager.mGreenForVertexBuffer = 20;
            TextManager.mBlueForVertexBuffer = 20;
            TextManager.mAlphaForVertexBuffer = 255;

#else
                TextManager.mZForVertexBuffer = camera.Position.Z - 100;
                TextManager.mRedForVertexBuffer = .1f;
                TextManager.mGreenForVertexBuffer = .1f;
                TextManager.mBlueForVertexBuffer = .1f;
                TextManager.mAlphaForVertexBuffer = 1;


#endif
                TextManager.mSpacingForVertexBuffer = GuiManager.TextSpacing;
                TextManager.mScaleForVertexBuffer = GuiManager.TextHeight / 2.0f;

                for (i = 0; i < this.mCachedIncrementValues.Count; i++)
                {
                    TextManager.mXForVertexBuffer = mLargeIncrementPositions[i];
                    TextManager.mRedForVertexBuffer = TextManager.mGreenForVertexBuffer =
                        TextManager.mBlueForVertexBuffer = 20;

                    string asString = mCachedIncrementValues[i];

                    TextManager.Draw(ref asString);

                }
            }
			#endregion

//			if(CurrentValue >= Start && CurrentValue <= Start + ValueWidth)
  //              positionBar.Draw(camera);

        }
                                                                                                                            
        internal override int GetNumberOfVerticesToDraw()                                       
		{
            if (GuiManagerDrawn)
            {
                int numVertices = base.GetNumberOfVerticesToDraw(); // takes care of the base window and the position bar which is a button

                numVertices += 6; // the horizontal bar

                double incrementsPassed = mStart / mVerticalBarIncrement;
                if (incrementsPassed != (int)mStart / mVerticalBarIncrement)
                    incrementsPassed = (int)incrementsPassed + 1;

                double xPosOfVerticalBar = incrementsPassed * mVerticalBarIncrement;

                // now that we know where the first xPosOfVerticalBar is, we can calculate how many large increment lines/values we'll have.

                float offset = (float)((incrementsPassed * mVerticalBarIncrement) - mStart);

                if (mLargeIncrementValues.Length != 1 + (int)((mValueWidth - offset) / mVerticalBarIncrement))
                    mLargeIncrementValues = new float[1 + (int)((mValueWidth - offset) / mVerticalBarIncrement)];

                int i = 0;

                while (xPosOfVerticalBar <= mStart + mValueWidth && i < mLargeIncrementValues.Length)
                {
                    numVertices += 6;

                    switch (this.mTimeUnitDisplayed)
                    {
                        case TimeUnit.Millisecond:
                            mLargeIncrementValues[i] = (float)(xPosOfVerticalBar) * 1000;
                            break;
                        case TimeUnit.Second:
                            mLargeIncrementValues[i] = (float)(xPosOfVerticalBar);
                            break;

                    }
                    i++;
                    xPosOfVerticalBar += mVerticalBarIncrement;
                }

                incrementsPassed = mStart / mSmallVerticalBarIncrement;
                if (incrementsPassed != (int)mStart / mSmallVerticalBarIncrement)
                    incrementsPassed = (int)incrementsPassed + 1;


                xPosOfVerticalBar = incrementsPassed * mSmallVerticalBarIncrement;
                while (xPosOfVerticalBar <= mStart + mValueWidth)
                {
                    numVertices += 6;
                    xPosOfVerticalBar += mSmallVerticalBarIncrement;
                }

                #region Include the number of text objects to write

                if (HasChangedSinceLastCache)
                {
                    UpdateIncrementValueCache(mLargeIncrementValues);
                }

                if (mShowValues)
                {
                    for (i = 0; i < mCachedIncrementValues.Count; i++)
                    {
                        numVertices += 6 * mCachedIncrementValues[i].Length;

                    }
                }

                #endregion

                return numVertices;
            }
            else
                return 0;

		}

		public override void TestCollision(Cursor cursor)
		{
			
			base.TestCollision(cursor);

			if(cursor.PrimaryPush && cursor.WindowOver == positionBar)
			{
                mBeforeChangeValue = mCurrentValue;

				cursor.GrabWindow(positionBar);
			}
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Converts an absolute value to visible position on the bar
        /// </summary>
        /// <param name="valueToGet">The absolute value to convert to position.</param>
        /// <returns>The position on the bar.</returns>
		protected double GetPosOnBar(double valueToGet)
		{
            return mWorldUnitX + ValueToPositionRelative(valueToGet);

        }

        #region XML Docs
        /// <summary>
	    /// Returns the precision-adjusted value of the time line given an absolute position.
	    /// </summary>
	    /// <param name="pos">The absolute X position.</param>
        /// <returns>The precision-adjusted value.</returns>
        #endregion
        protected double PositionToValueAbsolute(float pos)
		{
			return  
                System.Math.Round(
                mStart + mValueWidth * (pos  - 1 - (mWorldUnitX - ScaleX))/(2*ScaleX -2),
                mPrecision) ;
        }

        #region XML Docs
        /// <summary>
		/// Converts the argument relative-to-TimeLine argument to a value that it represents.
		/// </summary>
        /// <remarks>
        /// This mehod is used to convert the position bar's relative X to an actual value.
        /// </remarks>
		/// <param name="worldUnitRelativeX">The relative-to-Timeline position.</param>
        /// <returns>The value that corresponds to the argument position.</returns>
        #endregion
        protected double PositionToValueRelative(float worldUnitRelativeX)
		{
            return mStart + mValueWidth * (worldUnitRelativeX - 1 + ScaleX) / (2 * ScaleX - 2);
        }

        #region XML Docs
        /// <summary>
        /// Converts the value to the corresponding relative X position on the timeline.
        /// </summary>
        /// <param name="value">The value to convert to a relative position.</param>
        /// <returns>The corresponding relative position.</returns>
        #endregion
        protected float ValueToPositionRelative(double value)
        {
            return -ScaleX + 1 + (float)(2 * (value - mStart) * (ScaleX - 1) / mValueWidth);

        }


        #endregion

        #region Private Methods

        private void CreateScrollBar()
        {
            mScrollBar = new ScrollBar(mCursor);
            AddWindow(mScrollBar);
            const float ScrollBarBorder = .5f;

            mScrollBar.X = this.ScaleX;
            mScrollBar.ScaleX = this.ScaleX - ScrollBarBorder;

            mScrollBar.Alignment = ScrollBar.ScrollBarAlignment.Horizontal;
            mScrollBar.ScaleY = 1;
            mScrollBar.Y = 2 * this.ScaleY - mScrollBar.ScaleY - ScrollBarBorder;

            UpdateScrollBarToRange();

            mScrollBar.UpButtonClick += ScrollBarChanged;
            mScrollBar.DownButtonClick += ScrollBarChanged;
            mScrollBar.PositionBarMove += ScrollBarChanged;

            mScrollBar.Name = "TimeLine ScrollBar";

            mScrollBar.RatioDown = (float)((this.Start - MinimumValue) / (MaximumValue - MinimumValue));
        }

        private void DestroyScrollBar()
        {
            GuiManager.RemoveWindow(mScrollBar);
            mScrollBar = null;
        }

        private void FixStartAndCurrentValue()
        {
            if (mStart < mMinimumValue)
            {
                Start = mMinimumValue;
            }
            if (mCurrentValue < mMinimumValue)
            {
                CurrentValue = mMinimumValue;
            }

            
        }

        private void SetBarToValue()
		{
			positionBar.WorldUnitRelativeX = ValueToPositionRelative(mCurrentValue);

            if (mCurrentValue > mStart + mValueWidth)
            {
                mStart = mCurrentValue - mValueWidth / 2.0f ;
            }
            if (mCurrentValue < mStart)
            {
                mStart = mCurrentValue;
            }
        }

        private void UpdateIncrementValueCache(float[] largeIncrementValues)
        {
            mCachedIncrementValues.Clear();
            
            for (int i = 0; i < largeIncrementValues.Length - 1; i++)
            {
                mCachedIncrementValues.Add(largeIncrementValues[i].ToString());//.Replace(" ", "").Length;
            }

            mCacheValueWidth = mValueWidth;
            mCacheVerticalBarIncrement = mVerticalBarIncrement;
            mCacheStartValue = mStart;
        }

        private void UpdatePositionBarVisibility()
        {

            if (positionBar.X < positionBar.ScaleX || positionBar.X > ScaleX * 2 - positionBar.ScaleX)
            {
                positionBar.Visible = false;
            }
            else
            {
                positionBar.Visible = true;
            }
        }

        private void UpdatePositionBarYValues()
        {
            float bottomBuffer = .4f;
            float topBuffer = .4f;

            if (HasScrollBar)
            {
                bottomBuffer += mScrollBar.ScaleY * 2;
            }

            positionBar.Y = ScaleY + topBuffer / 2.0f - bottomBuffer / 2.0f;

            positionBar.ScaleY = ScaleY - (topBuffer + bottomBuffer) / 2.0f ;
        }

        private void UpdateScrollBarToRange()
        {
            if (HasScrollBar)
            {
                double totalRange = (this.mMaximumValue - this.mMinimumValue);

                mScrollBar.View = System.Math.Min(1, this.ValueWidth / totalRange);
                mScrollBar.Sensitivity = (this.ValueWidth * .1) / totalRange;
            }
        }

        #endregion
		#endregion
	}
}
