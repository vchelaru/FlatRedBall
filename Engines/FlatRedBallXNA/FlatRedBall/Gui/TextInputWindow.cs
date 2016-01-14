using System;

#if FRB_MDX
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Direct3D=Microsoft.DirectX.Direct3D;
#else

#endif
using FlatRedBall.Graphics;
using System.Collections.Generic;


namespace FlatRedBall.Gui
{
	/// <summary>
	/// Summary description for TextInputWindow.
	/// </summary>
	public class TextInputWindow : Window
	{
		#region Fields

		internal Button mOkButton;
		Button mCancelButton;
		internal TextBox mTextBox;
		TextField mTextDisplay;

		#endregion

		#region Properties

		public TextBox.FormatTypes Format
		{
			set
			{
				mTextBox.Format = value;
			}
		}


		public string Text
		{
			set
			{
				mTextBox.Text = value;
			}
			get
			{
                return mTextBox.Text;
			}
		}

		#endregion

		#region Events


		public event GuiMessage OkClick = null;


		#endregion

        #region Delegate Methods

        void OkButtonClick(Window callingWindow)
        {
            if (OkClick != null)
                OkClick(this);

            Visible = false;
            this.mCursor.WindowClosing = this;
        }

        #endregion

        #region Methods

        #region Constructor

        public TextInputWindow(Cursor cursor) : 
            base(cursor)
		{
            mCancelButton = new Button(mCursor);
            AddWindow(mCancelButton);

            mOkButton = new Button(mCursor);
            AddWindow(mOkButton);

            mTextBox = new TextBox(mCursor);
            AddWindow(mTextBox);

			mTextBox.EnterPressed += new GuiMessage(OkButtonClick);
			this.HasMoveBar = true;
            this.HasCloseButton = true;
            Visible = false;
			mTextDisplay = new TextField();

        }

        #endregion

        public void Activate(string textToDisplay, string Name)
		{
            Visible = true;
			mName = Name;
			ScaleX = 12;
			ScaleY = 9;

            mTextDisplay.TextHeight = GuiManager.TextHeight;
			mTextDisplay.SetDimensions(-2, -8, 1, 23, 0);
			mTextDisplay.mZ = 100;
			mTextDisplay.WindowParent = this;


			mTextDisplay.DisplayText = textToDisplay;

			mOkButton.ScaleX = 5;
			mOkButton.ScaleY = 1.5f;
            mOkButton.Text = "Ok";
            mOkButton.Click += OkButtonClick;
			mOkButton.SetPositionTL(5.2f, 2*ScaleY - 2);
			
			mCancelButton.ScaleX = 5;
			mCancelButton.ScaleY = 1.5f;
            mCancelButton.Text = "Cancel";
			mCancelButton.SetPositionTL(2*ScaleX - 5.2f, 2*ScaleY - 2);

			mTextBox.ScaleX = ScaleX - 2;
			mTextBox.ScaleY = 1.4f;
			mTextBox.SetPositionTL(ScaleX, 2*ScaleY - 5);
		}


        public override void ClearEvents()
        {
            base.ClearEvents();
            OkClick = null;
        }


        public void HighlightInputText()
        {
            mTextBox.HighlightCompleteText();
        }


        public void SetOptions(List<string> options)
        {
            mTextBox.SetOptions(options);
        }


        internal override void DrawSelfAndChildren(Camera camera)
		{
#if !SILVERLIGHT
            if (Visible == false)
				return;
            base.DrawSelfAndChildren(camera);

            TextManager.mRedForVertexBuffer = TextManager.mGreenForVertexBuffer = TextManager.mBlueForVertexBuffer = 20;


            TextManager.mScaleForVertexBuffer = GuiManager.TextHeight / 2.0f;
            TextManager.mSpacingForVertexBuffer = GuiManager.TextSpacing;
            TextManager.mNewLineDistanceForVertexBuffer = GuiManager.TextHeight;

			TextManager.Draw(mTextDisplay);
#endif
		}


        internal override int GetNumberOfVerticesToDraw()
		{
            return base.GetNumberOfVerticesToDraw() + mTextDisplay.DisplayText.Replace("\n", "").Replace(" ", "").Length * 6;

		}


        public override void TestCollision(Cursor cursor)
		{
			base.TestCollision(cursor);
			if(cursor.PrimaryClick)
			{
				if (cursor.WindowOver == mCancelButton)
				{
                    Visible = false;
					cursor.WindowClosing = this;
				}
			}
		}

		
		#endregion
	}
}
