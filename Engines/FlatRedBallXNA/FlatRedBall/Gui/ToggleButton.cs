using System;

#if FRB_MDX
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Direct3D=Microsoft.DirectX.Direct3D;
#else
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
#endif
using FlatRedBall.Graphics;



namespace FlatRedBall.Gui
{
	/// <summary>
	/// Summary description for ToggleButton.
	/// </summary>
	/// 
	public class ToggleButton : Button
	{
		#region Fields
		bool oneAlwaysDown;
		string unpressedText;
		string pressedText;
		public WindowArray radioGroup;

        Texture2D mUpTexture;
        Texture2D mDownTexture;

        ButtonPushedState mStateBeforePrimaryPush = ButtonPushedState.Up;

		#endregion

		#region Properties

        public bool IsPressed
        {
            get { return ButtonPushedState == ButtonPushedState.Down; }
            set
            {
                if (value)
                {
                    Press();
                }
                else
                {
                    Unpress();
                }
            }
        }

		public override string Text
		{
			set
			{
                base.Text = pressedText = unpressedText = value;
			}

			get
			{
				if(IsPressed)
					return pressedText;
				else
					return unpressedText;
			}

		}

		#endregion

        #region Event Methods

        void RestoreToStateBefore(Window callingWindow)
        {
            if (this.mCursor.PrimaryDown && this.mCursor.WindowPushed == this)
            {
                this.ButtonPushedState = mStateBeforePrimaryPush;
            }
        }

        void UndoRestoreToStateBefore(Window callingWindow)
        {
            if (this.mCursor.PrimaryDown && this.mCursor.WindowPushed == this)
            {
                if (mStateBeforePrimaryPush == ButtonPushedState.Up)
                    ButtonPushedState = ButtonPushedState.Down;
                else
                    ButtonPushedState = ButtonPushedState.Up;
            }
        }

        #endregion


        #region Methods
        public ToggleButton(Cursor cursor) : base(cursor)
		{
			
			//pressed = false;
			radioGroup = new WindowArray();
			//pressedText = "";

            ButtonPushedState = ButtonPushedState.Up;

            this.RollingOff += RestoreToStateBefore;
            this.RollingOn += UndoRestoreToStateBefore;


		}

		
		public void AddToRadioGroup(ToggleButton buttonToAdd)
		{
			buttonToAdd.radioGroup.AddUnique(this);
			buttonToAdd.radioGroup.AddUnique(this.radioGroup);

			foreach(ToggleButton b in buttonToAdd.radioGroup)
			{
				b.radioGroup.AddUnique(this);
				b.radioGroup.AddUnique(this.radioGroup);
			}

			this.radioGroup.AddUnique(buttonToAdd);
			this.radioGroup.AddUnique(buttonToAdd.radioGroup);

			foreach(ToggleButton b in radioGroup)
			{
				b.radioGroup.AddUnique(buttonToAdd);
				b.radioGroup.AddUnique(buttonToAdd.radioGroup);
			}
		}
		

		/// <summary>
		/// Presses the ToggleButton down and calls the onClick event.
		/// </summary>
		public override void Press()
		{
            PressNoCall();

			OnClick();
		}


		/// <summary>
		/// Presses the ToggleButton down but does not call the onClick event.
		/// </summary>
		public void PressNoCall()
		{
			if(Enabled == false)	return;

            ButtonPushedState = ButtonPushedState.Down;

			for(int i = 0; i < radioGroup.Count; i++)
			{
				if(radioGroup[i] != this)
				{
                    ((ToggleButton)radioGroup[i]).ButtonPushedState = ButtonPushedState.Up;
				}
			}

            base.Text = this.pressedText;

            if (IsPressed && mDownTexture != null)
            {
                mOverlayTexture = mDownTexture;
            }
            else if (!IsPressed && mUpTexture != null)
            {
                mOverlayTexture = mUpTexture;
            }
		}


		public void SetOneAlwaysDown(bool OneAlwaysDown)
		{
			oneAlwaysDown = OneAlwaysDown;
			for(int i = 0; i < radioGroup.Count; i++)
			{
				((ToggleButton)radioGroup[i]).oneAlwaysDown = oneAlwaysDown;
			}
		}

        public override void SetOverlayTextures(Texture2D upTexture, Texture2D downTexture)
        {
            mUpTexture = upTexture;
            mDownTexture = downTexture;

            if (IsPressed && mDownTexture != null)
            {
                mOverlayTexture = mDownTexture;
            }
            else if (!IsPressed && mUpTexture != null)
            {
                mOverlayTexture = mUpTexture;
            }
        }

		
		public void SetText(string unpressedText, string PressedText)
		{
            base.Text = this.unpressedText = unpressedText;
			pressedText = PressedText;
		}


		public void Toggle()
		{
			if(!Enabled)	return;

			if(IsPressed)	
			{
				Unpress();
				OnClick();
			}
			else
				Press();
		}


		public void Unpress()
		{
            ButtonPushedState = ButtonPushedState.Up;

			if(unpressedText != null)
                base.Text = this.unpressedText;
			else
                base.Text = pressedText;

		}


        public override void TestCollision(Cursor cursor)
		{
			if(cursor.PrimaryPush)
			{
                mStateBeforePrimaryPush = ButtonPushedState;

                if (ButtonPushedState == ButtonPushedState.Up)
                {
                    ButtonPushedState = ButtonPushedState.Down;
                }
                else
                {
                    ButtonPushedState = ButtonPushedState.Up;
                }
			}
           

			if(cursor.WindowPushed == this && cursor.PrimaryClick)
			{
                if (ButtonPushedState == ButtonPushedState.Up && oneAlwaysDown == false)
                {
                    ButtonPushedState = ButtonPushedState.Up;
                }
                else
                {
                    ButtonPushedState = ButtonPushedState.Down;
                }

				if(IsPressed)
                    base.Text = this.pressedText;
				else
                    base.Text = this.unpressedText;

				for(int i = 0; i < radioGroup.Count; i++)
				{
					if(radioGroup[i] != this)
                        ((ToggleButton)radioGroup[i]).Unpress();

				}


                if (IsPressed && mDownTexture != null)
                {
                    mOverlayTexture = mDownTexture;
                }
                else if (!IsPressed && mUpTexture != null)
                {
                    mOverlayTexture = mUpTexture;
                }
			}

            // The base class is Button - we don't want that behavior.
            TestCollisionBase(cursor);

			if (cursor.WindowOver == this && ShowsToolTip)
			{
				GuiManager.ToolTipText = this.Text;
			}

		}

		
		#endregion
	}
}
