using System;

#if FRB_MDX
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Direct3D=Microsoft.DirectX.Direct3D;
using Keys = Microsoft.DirectX.DirectInput.Key;

#else
using Microsoft.Xna.Framework.Input;
#endif
using FlatRedBall.Graphics;
using FlatRedBall.Input;
using System.Collections.Generic;

namespace FlatRedBall.Gui
{
	/// <summary>
	/// Summary description for TextInputWindow.
	/// </summary>
	public class MessageBox : Window, IInputReceiver
	{
		#region Fields

		internal Button okButton;
		

		TextField textDisplay;

        //float mTextRed;
        //float mTextGreen;
        //float mTextBlue;

        IInputReceiver mNextInTabSequence;

        List<Keys> mIgnoredKeys = new List<Keys>();


		#endregion

        #region Properties

        public List<Keys> IgnoredKeys
        {
            get { return mIgnoredKeys; }
        }

        public IInputReceiver NextInTabSequence
        {
            get { return mNextInTabSequence; }
            set { mNextInTabSequence = value; }
        }

        #endregion

        #region Events

        public event GuiMessage GainFocus;

        public event FocusUpdateDelegate FocusUpdate;
		public event GuiMessage OkClick;

        #endregion

        #region Event-calling methods
        public void OnFocusUpdate()
        {
            if (FocusUpdate != null)
                FocusUpdate(this);
        }
        #endregion

        #region Methods

        public MessageBox(Cursor cursor) : 
            base(cursor)
		{
            okButton = new Button(mCursor);
            AddWindow(okButton);
			this.HasMoveBar = true;
            this.HasCloseButton = true;
            Visible = false;
			textDisplay = new TextField();

			textDisplay.mAlignment = HorizontalAlignment.Left;

            
            //mTextRed = mTextGreen = mTextBlue = 20;

		}


		public void Activate(string textToDisplay, string Name)
		{
            Visible = true;
			mName = Name;
			ScaleX = 19;
			ScaleY = 9;

			textDisplay.SetDimensions((Window)this);
			textDisplay.DisplayText = textToDisplay;
			textDisplay.mZ = 100;
            textDisplay.WindowParent = this;


			okButton.ScaleX = 5;
			okButton.ScaleY = 1.5f;
            okButton.Text = "Ok";
			okButton.SetPositionTL(ScaleX, 2*ScaleY - 2);

		    textDisplay.FillLines();

			while(13f + textDisplay.mLines.Count*2 > this.ScaleY*2)
			{
                this.ScaleX = System.Math.Min(52, ScaleX + 2) ;
                this.ScaleY += 2;
                textDisplay.SetDimensions((Window)this);
                textDisplay.mLines.Clear();
				textDisplay.FillLines();
				okButton.SetPositionTL(ScaleX, 2*ScaleY - 2);

			}
			textDisplay.SetDimensions((Window)this, 1);
		}


        public override void ClearEvents()
        {
            base.ClearEvents();
            OkClick = null;
        }


        internal override void DrawSelfAndChildren(Camera camera)
		{
#if !SILVERLIGHT
            if (Visible == false)
				return;

            // TODO:  Set TextManager vertex drawing fields here.
            textDisplay.TextHeight = GuiManager.TextHeight;

            base.DrawSelfAndChildren(camera);
			TextManager.Draw(textDisplay);
#endif

		}


        internal override int GetNumberOfVerticesToDraw()
		{
            return base.GetNumberOfVerticesToDraw() + textDisplay.DisplayText.Replace("\n", "").Replace(" ", "").Length * 6;
		}


        public void OnGainFocus()
        {
            if (GainFocus != null)
                GainFocus(this);
        }

        public override void TestCollision(Cursor cursor)
		{
			base.TestCollision(cursor);
			if(cursor.PrimaryClick)
			{
				if(cursor.WindowOver == okButton)
				{
					if(OkClick != null)
						OkClick(this);

                    Visible = false;
					cursor.WindowClosing = this;
					
				}
			}
		}

		
		void onEnter(Window callingWindow)
		{
			if(OkClick != null)
				this.OkClick(this);

            if (InputManager.ReceivingInput == this)
                InputManager.ReceivingInput = null;

            Visible = false;
		}


		#region IInputReceiver Members

        public void LoseFocus()
        { }

		public void ReceiveInput()
		{
#if FRB_MDX
			if(InputManager.Keyboard.KeyPushed(Keys.Return) || InputManager.Keyboard.KeyPushed(Keys.NumPadEnter))
#else
			if(InputManager.Keyboard.KeyPushed(Keys.Enter))

#endif
			{
				onEnter(this);
			}
		}

		public bool TakingInput
		{
			get
			{
				return true;
			}
		}

		#endregion

        #endregion
    }
}
