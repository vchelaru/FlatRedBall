using System;
using System.Globalization;

#if FRB_MDX
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Direct3D=Microsoft.DirectX.Direct3D;
using Keys = Microsoft.DirectX.DirectInput.Key;
#elif FRB_XNA || SILVERLIGHT || WINDOWS_PHONE
using Microsoft.Xna.Framework.Input;
#endif
using FlatRedBall.Input;
using FlatRedBall.ManagedSpriteGroups;
using System.Collections.Generic;


namespace FlatRedBall.Gui
{
	/// <summary>
	/// Summary description for UpDown.
	/// </summary>
	public class UpDown : Window, IInputReceiver
	{
		#region Fields
		private float mMaxValue;
        private float mMinValue;

        private float mSensitivity = .1f;

        private float mChange;
        private int mPrecision = 8;

        private float mRoundTo = 0;
        private float mRoundToOffset = 0;

		private float mCurrentValue;
        private float mBeforeChangeValue;
		internal TextBox mTextBox;
        private Button mUpDownButton;

		internal static float topUp = .5703125f;
		internal static float bottomUp = .640625f;
		internal static float leftUp = .4023438f;
		internal static float rightUp = .44921875f;

		internal static float rightDown = .4960937f;

        List<Keys> mIgnoredKeys = new List<Keys>();
		#endregion
		
		#region Properties

        public float BeforeChangeValue
        {
            get 
            {
                if (mRoundTo == 0)
                {
                    return (float)System.Math.Round(mBeforeChangeValue, mPrecision);
                }
                else
                {
                    // Subtract the offset value, then add to it
                    float value = mBeforeChangeValue - mRoundToOffset;
                    value = FlatRedBall.Math.MathFunctions.RoundFloat(value, mRoundTo);

                    value += RoundToOffset;

                    return value;
                }
            
            }
        }

        #region XML Docs
        /// <summary>
        /// Gets the value change for this frame.
        /// </summary>
        #endregion
        public float change
		{
			get
			{	return mChange;		}
		}


		public float CurrentValue
		{
			get 
            {
                if (mRoundTo == 0)
                {
                    return (float)System.Math.Round(mCurrentValue, mPrecision);
                }
                else
                {
                    float value = mCurrentValue - mRoundToOffset;
                    value = FlatRedBall.Math.MathFunctions.RoundFloat(value, mRoundTo);
                    value += mRoundToOffset;

                    return value;
                
                }
            }
			set	
			{
                // Calling ToString can generate a considerable amount
                // of garbage when used in PropertyGrids.  This saves us
                // some of that.
                if (value != mCurrentValue || string.IsNullOrEmpty(mTextBox.Text))
                {
                    float oldValue = mCurrentValue;


                    mCurrentValue = value;
                    if (mCurrentValue > MaxValue) mCurrentValue = MaxValue;
                    if (mCurrentValue < MinValue) mCurrentValue = MinValue;

                    this.mChange = mCurrentValue - oldValue;

                    if (mRoundTo == 0)
                    {
                        mTextBox.Text = ((float)System.Math.Round(mCurrentValue, mPrecision)).ToString();
                    }
                    else
                    {
                        float displayFloat = mCurrentValue - mRoundToOffset;

                        displayFloat = FlatRedBall.Math.MathFunctions.RoundFloat(displayFloat, mRoundTo);
                        displayFloat += mRoundToOffset;

                        mTextBox.Text = displayFloat.ToString();
                    }
                }
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
                if (mTextBox != null)
                {
                    mTextBox.Enabled = value;
                }
            }
        }


        public List<Keys> IgnoredKeys
        {
            get { return mIgnoredKeys; }
        }


        public override bool IsWindowOrChildrenReceivingInput
        {
            get
            {
                return base.IsWindowOrChildrenReceivingInput || 
                    mCursor.WindowPushed == this.UpDownButton;
            }
        }


        public float MinValue
        {
            get { return mMinValue; }
            set { mMinValue = value; }
        }


        public float MaxValue
        {
            get { return mMaxValue; }
            set { mMaxValue = value; }
        }


        public IInputReceiver NextInTabSequence
        {
            get { return mTextBox.NextInTabSequence; }
            set { mTextBox.NextInTabSequence = value; }
        }


        public int Precision
        {
            get{ return mPrecision;}
            set{ mPrecision = value;}
        }


        public float RoundTo
        {
            get { return mRoundTo; }
            set { mRoundTo = value; }
        }


        public float RoundToOffset
        {
            get { return mRoundToOffset; }
            set { mRoundToOffset = value; }
        }


		public override float ScaleX
		{
			get{ return (float)base.ScaleX;	}
			set
			{
				mScaleX = value;
				mUpDownButton.SetPositionTL(2*value - .6f, ScaleY);
				mTextBox.SetPositionTL(value- 0.5f, ScaleY);
				mTextBox.ScaleX = value - .6f;
			}
        }

        #region XML Docs
        /// <summary>
        /// The speed at which dragging over the button changes the value.  Default is .1f.
        /// </summary>
        #endregion
        public float Sensitivity
        {
            get { return mSensitivity; }
            set { mSensitivity = value; }
        }


        public bool TakingInput
        {
            get { return mTextBox.TakingInput; }
            set { mTextBox.TakingInput = value; }
        }


        public TextBox textBox
        {
            get
            {
                return mTextBox;
            }
        }


        public float UnroundedCurrentValue
        {
            get { return mCurrentValue; }
        }


        public Button UpDownButton
        {
            get { return mUpDownButton; }
        }

		#endregion

		#region Events

		public event GuiMessage ValueChanged;

        #region XML Docs
        /// <summary>
        /// This event is raised whenever the user types in a
        /// new value in the TextBox or when the user has finished
        /// changing the value by clicking and dragging on the button.
        /// </summary>
        #endregion
        public event GuiMessage AfterValueChanged;

        public event GuiMessage GainFocus;

        public event FocusUpdateDelegate FocusUpdate;


		#endregion

		#region Delegate and delegate calling methods

		public void callOnGUIChange()
		{
			if(ValueChanged != null)
				ValueChanged(this);
		}


        public void OnGainFocus()
        {
            if (mCursor.IsOn((IWindow)mUpDownButton) && mCursor.PrimaryClick)
            {
                // The user finished dragging the up/down throught the updown button.
                // In this case we don't want the UI to gain input so set it properly
                InputManager.ReceivingInput = null;
            }
            else
            {

                if (GainFocus != null)
                    GainFocus(this);

                InputManager.ReceivingInput = mTextBox;
            }
        }

        public void LoseFocus()
        {
            mTextBox.LoseFocus();
        }

        public void OnFocusUpdate()
        {
            if (FocusUpdate != null)
                FocusUpdate(this);

            mTextBox.OnFocusUpdate();
        }

        void UpDownButtonPush(Window callingWindow)
        {
            // mBeforeValueChange is the value that was shown in 
            // the UpDown before the user made a change (either through
            // typing in a new value or dragging on the UpDown Button).

            // This value should be set either when the user clicks on the
            // TextBox to enter a new value or when the user first pushes on
            // the UpDown button to drag and change.  When either the TextBox
            // that is used to enter the value loses focus, or when the user releases
            // the UpDown Button, the mBeforeChangeValue may be used in the AfterValueChanged
            // event.

            // The problem is that in a given frame button push events are always raised before
            // focus loss events.  Therefore, if the user has the TextBox as the receiving input
            // element, but clicks on the UpDown button, the mBeforeChangeValue will update and be
            // the same as mCurrentValue when the TextBox raises the AfterValueChange.  Therefore,
            // the solution is to only change the mBeforeChangeValue if the TextBox is not selected.

            // If the TextBox is selected, it will handle setting the mBeforeChangeValue variable after
            // calling the AfterValueChanged from its Push event.

            if (InputManager.ReceivingInput != this.mTextBox)
            {
                mBeforeChangeValue = mCurrentValue;
            }
        }


		public void UpDownButtonClick(Window callingWindow)
		{
			mCursor.StaticPosition = false;
			OnClick();

            // November 4 2009: Vic says - do we need this anymore?  The TextBox seems to be taking care of it, 
            // and putting this in causes a double-call to some methods.
            //if ( CurrentValue != BeforeChangeValue && AfterValueChanged != null)
            //    AfterValueChanged(this);
            //mBeforeChangeValue = mCurrentValue;
		}


		public void UpDownButtonDrag(Window callingWindow)
		{
			mCursor.StaticPosition = true;
			CurrentValue = mCurrentValue + mCursor.YVelocity * Sensitivity;

			if(ValueChanged != null) 
				ValueChanged(this);
		}

/*
        void TextBoxPush(Window callingWindow)
        {
            // It's possible that the user clicked
            // on the UpDown Button while this TextBox
            // was selected.  In that case, the UpDownButtonPush
            // event did not set the mBeforeChangeValue variable so
            // this method could raise the AfterValueChanged event and
            // use this variable.  This is because button Push events are
            // always called before TextBox losing focus events.  Therefore,
            // call the AfterValueChange event and then don't forget to update
            // the mBeforeChangeValue.

 //           if (AfterValueChanged != null)
   //         {
     //           AfterValueChanged(this);
       //     }

            mBeforeChangeValue = mCurrentValue;
        }

        */

		internal void textBoxChangeValue(IWindow callingWindow)
		{
			if(mTextBox.Text == "" || mTextBox.Text == "-." || mTextBox.Text == "-")
				mTextBox.Text = "0";

            try
            {
                if (mTextBox.HasTextChangedSinceLastGainFocus)
                {
                    CurrentValue = float.Parse(mTextBox.Text);
                }
            }
            catch(Exception e)
            {
#if XBOX360 || SILVERLIGHT || WINDOWS_PHONE || MONODROID
                throw e;
#else
                // We need the exception for the other platforms, so we're going to do this
                // to avoid having a warning
                e.ToString();
                System.Windows.Forms.MessageBox.Show("Attempting to parse " + mTextBox.Text + 
                    " in UpDown.textBoxChangeValue");
#endif
            }

			mTextBox.RegulateCursorPosition();

			
			if(ValueChanged != null)
				this.ValueChanged(this);

            if ( mCurrentValue != mBeforeChangeValue && AfterValueChanged != null)
            {
                AfterValueChanged(this);
            }
		}


		private void CallOnPush(Window callingWindow)
		{
			OnPush();
		}


		private void CallOnClick(Window callingWindow)
		{
			OnClick();
		}

		
		private void CallOnLosingFocus(Window callingWindow)
		{
			this.OnLosingFocus();
		}


        private void TextBoxGainFocus(Window callingWindow)
        {
            mBeforeChangeValue = mCurrentValue;
        }


        private void MouseWheelEvent(Window callingWindow)
        {
            if (Precision == 0)
            {
                if (mCursor.ZVelocity > 0)
                {
                    CurrentValue++;
                }
                else if (mCursor.ZVelocity < 0)
                {
                    CurrentValue--;
                }
            }
            else
            {
                CurrentValue = mCurrentValue + mCursor.ZVelocity * Sensitivity;
            }
            if (ValueChanged != null)
                ValueChanged(this);
        }

		#endregion

		#region Methods

        #region Constructor/Initialize

        public UpDown(Cursor cursor) : 
            base(cursor)
		{
            if (GuiManager.guiTexture == null)
            {
                throw new System.NullReferenceException("GuiTexture not set - cannot add UpDown");
            }

			mScaleX = 4.2f;
			mScaleY = 1.3f;
			MaxValue = float.PositiveInfinity;
			MinValue = float.NegativeInfinity;
			mCurrentValue = 0;
			mChange = 0;
			Sensitivity = .1f;

            mUpDownButton = new Button(mCursor);
            AddWindow(mUpDownButton);
			mUpDownButton.SetPositionTL(ScaleX + 2.2f, ScaleY);
			mUpDownButton.ScaleX = .6f;
            mUpDownButton.ScaleY = 1;
            mUpDownButton.MouseWheelScroll += MouseWheelEvent;

            mTextBox = new TextBox(mCursor);
            AddWindow(mTextBox);
			mTextBox.SetPositionTL(ScaleX - 1.0f, ScaleY);
			mTextBox.ScaleX = 2.8f;
			mTextBox.Format = TextBox.FormatTypes.Decimal;
			mTextBox.Text = mCurrentValue.ToString();
            mTextBox.MouseWheelScroll += MouseWheelEvent;

            SetInternalEvents();
			//this.onLosingFocus += new GuiMessage(textBoxChangeValue);
		}


        //public UpDown(SpriteFrame baseSF, SpriteFrame textBoxSpriteFrame, 
        //    string buttonTexture,
        //    Cursor cursor, Camera camera, string contentManagerName)
        //    : base(baseSF, cursor)
        //{
        //    MaxValue = 999999;
        //    MinValue = -999999;
        //    mCurrentValue = 0;
        //    mChange = 0;
        //    Sensitivity = .1f;

        //    mUpDownButton = base.AddButton(buttonTexture, GuiManager.InternalGuiContentManagerName);
        //    mUpDownButton.SetPositionTL(ScaleX + 2.2f, ScaleY);
        //    mUpDownButton.ScaleX = .6f;
        //    mUpDownButton.ScaleY = 1;


        //    mTextBox = base.AddTextBox(textBoxSpriteFrame, "redball.bmp",
        //        camera, contentManagerName);
        //    mTextBox.SetPositionTL(ScaleX - 1.0f, ScaleY);
        //    mTextBox.ScaleX = 2.8f;
        //    mTextBox.format = TextBox.FormatTypes.DECIMAL;
        //    mTextBox.Text = mCurrentValue.ToString();

        //    SetInternalEvents();

        //    //this.onLosingFocus += new GuiMessage(textBoxChangeValue);

        //    ScaleX = ScaleX;
        //    ScaleY = ScaleY;

        //}


        void SetInternalEvents()
        {
            mTextBox.LosingFocus += new GuiMessage(textBoxChangeValue);
            mTextBox.Push += new GuiMessage(CallOnPush);
            mTextBox.LosingFocus += new GuiMessage(CallOnLosingFocus);
            mUpDownButton.Click += new GuiMessage(UpDownButtonClick);
            mUpDownButton.Dragging += new GuiMessage(UpDownButtonDrag);
            mUpDownButton.Push += new GuiMessage(CallOnPush);

            mUpDownButton.Push += this.UpDownButtonPush;

            mTextBox.GainFocus += this.TextBoxGainFocus;


        }
        #endregion

        #region Public Methods

        public void Clear()
        {
            mTextBox.Text = "";
        }

        public override void ClearEvents()
        {
            base.ClearEvents();
            ValueChanged = null;
        }

        #endregion

        #region Internal Methods

#if !SILVERLIGHT
        internal override void DrawSelfAndChildren(Camera camera)
		{
            if (Visible == false) return;

            mTextBox.DrawSelfAndChildren(camera);
			
			#region draw the custom UpDown button

			float xToUse = (mUpDownButton.WorldUnitX);
			float yToUse = (mUpDownButton.WorldUnitY);
#if FRB_MDX
			float zToUse = (camera.Z + 100);
#else
            float zToUse = camera.Z - 100;
#endif

			StaticVertices[0].Color = StaticVertices[1].Color = StaticVertices[2].Color = StaticVertices[3].Color = StaticVertices[4].Color = StaticVertices[5].Color = mColor;
            StaticVertices[0].Position.Z = StaticVertices[1].Position.Z = StaticVertices[2].Position.Z = StaticVertices[3].Position.Z = StaticVertices[4].Position.Z = StaticVertices[5].Position.Z = zToUse;


			if(mUpDownButton.ButtonPushedState == ButtonPushedState.Down)
			{
				StaticVertices[0].Position.X = xToUse - mUpDownButton.ScaleX; 
				StaticVertices[0].Position.Y = yToUse - mUpDownButton.ScaleY;
				StaticVertices[0].TextureCoordinate.X = rightUp;	
				StaticVertices[0].TextureCoordinate.Y = bottomUp;

				StaticVertices[1].Position.X = xToUse - mUpDownButton.ScaleX; 
				StaticVertices[1].Position.Y = yToUse + mUpDownButton.ScaleY;
				StaticVertices[1].TextureCoordinate.X = rightUp;	
				StaticVertices[1].TextureCoordinate.Y = topUp;

				StaticVertices[2].Position.X = xToUse + mUpDownButton.ScaleX; 
				StaticVertices[2].Position.Y = yToUse + mUpDownButton.ScaleY;
				StaticVertices[2].TextureCoordinate.X = rightDown;	
				StaticVertices[2].TextureCoordinate.Y = topUp;

				StaticVertices[3] = StaticVertices[0];
				StaticVertices[4] = StaticVertices[2];

				StaticVertices[5].Position.X = xToUse + mUpDownButton.ScaleX; 
				StaticVertices[5].Position.Y = yToUse - mUpDownButton.ScaleY;
				StaticVertices[5].TextureCoordinate.X = rightDown;	
				StaticVertices[5].TextureCoordinate.Y = bottomUp;

                GuiManager.WriteVerts(StaticVertices);
			}
			else
			{
				StaticVertices[0].Position.X = xToUse - mUpDownButton.ScaleX; 
				StaticVertices[0].Position.Y = yToUse - mUpDownButton.ScaleY;
				StaticVertices[0].TextureCoordinate.X = leftUp;	
				StaticVertices[0].TextureCoordinate.Y = bottomUp;

				StaticVertices[1].Position.X = xToUse - mUpDownButton.ScaleX; 
				StaticVertices[1].Position.Y = yToUse + mUpDownButton.ScaleY;
				StaticVertices[1].TextureCoordinate.X = leftUp;	
				StaticVertices[1].TextureCoordinate.Y = topUp;

				StaticVertices[2].Position.X = xToUse + mUpDownButton.ScaleX; 
				StaticVertices[2].Position.Y = yToUse + mUpDownButton.ScaleY;
				StaticVertices[2].TextureCoordinate.X = rightUp;	
				StaticVertices[2].TextureCoordinate.Y = topUp;

				StaticVertices[3] = StaticVertices[0];
				StaticVertices[4] = StaticVertices[2];

				StaticVertices[5].Position.X = xToUse + mUpDownButton.ScaleX; 
				StaticVertices[5].Position.Y = yToUse - mUpDownButton.ScaleY;
				StaticVertices[5].TextureCoordinate.X = rightUp;	
				StaticVertices[5].TextureCoordinate.Y = bottomUp;

                GuiManager.WriteVerts(StaticVertices);
			}

			#endregion


		}

        internal override int GetNumberOfVerticesToDraw()
		{
			return mTextBox.GetNumberOfVerticesToDraw() + 6;
        }
#endif

        #endregion

        public bool CurrentlyReceivingInput()
		{
            return InputManager.ReceivingInput == mTextBox;
		}


        public void ReceiveInput()
        {
            mTextBox.ReceiveInput();
        }

		public override void TestCollision(Cursor cursor)
		{

            base.TestCollision(cursor);

			if(this.mChildren.Contains(cursor.WindowOver))
				cursor.WindowOver = this;

		}

        public override string ToString()
        {
            return (mCurrentValue.ToString());
        }
		
		public void UpdateValue()
		{
			CurrentValue = float.Parse(mTextBox.Text );
		}


        internal void ForceUpdateBeforeChangedValue()
        {
            mBeforeChangeValue = mCurrentValue;
        }

		#endregion
	}
}