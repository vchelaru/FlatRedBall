using System;

using FlatRedBall.Graphics;

using FlatRedBall.ManagedSpriteGroups;


namespace FlatRedBall.Gui
{
	/// <summary>
	/// Summary description for MultiButtonMessageBox.
	/// </summary>
	public class MultiButtonMessageBox : Window
	{
		#region Fields

		WindowArray buttonArray;
		TextField textField;

        // This field is never used, but maybe will be eventually so we're leaving the comment in.
        //float mSpacing = 1;
        float mTextScale = 1;

        #region Non GUIMan Drawn objects

        // This was never assigned
        // and caused a warning, so I'm removing
        // it.
        //FlatRedBall.Graphics.Text mTextObject;
        
        #endregion

        #endregion

        #region Properties

        public string Text
		{
			set
			{
				textField.DisplayText = value;
			}

		}

        public override float ScaleX
        {
            get
            {
                return base.ScaleX;
            }
            set
            {
                base.ScaleX = value;
                ResizeBox(value, true);
            }
        }
		#endregion

		#region Delegate and delegate calling methods

        void RemoveThis(Window callingWindow)
        {
            this.CloseWindow();            
            GuiManager.RemoveWindow(this);

        }

		public void MakeInvisible(Window callingWindow)
		{
            Visible = false;

		}

        void Move(Window callingWindow)
        {
            textField.SetDimensions(callingWindow);
        }

		#endregion

		#region Methods

        #region Constructor

        public MultiButtonMessageBox(Cursor cursor) : 
            base(cursor)
		{
			buttonArray = new WindowArray();
			this.HasMoveBar = true;
			textField = new TextField();
            textField.WindowParent = this;

			textField.TextHeight = GuiManager.TextHeight;
            
			this.ScaleX = 5;

            GuiManagerDrawn = true;
//			this.ScaleY = 5;

            this.Dragging += Move;

		}

        //public MultiButtonMessageBox(SpriteFrame baseWindow, SpriteFrame buttonBlueprint,
        //    Cursor cursor) :
        //    base(baseWindow, cursor)
        //{
        //    buttonArray = new WindowArray();

        //    this.textObject = TextManager.AddText("");

        //    this.buttonBlueprint = buttonBlueprint;

        //    SpriteManager.RemoveSpriteFrame(buttonBlueprint);

        //    GuiManagerDrawn = false;

        //}

        #endregion

        public void AddButton(string buttonText, GuiMessage message)
		{
            Button buttonAdded;

            if (this.GuiManagerDrawn)
            {
                buttonAdded = new Button(mCursor);
                this.AddWindow(buttonAdded);

            }
            else
            {
                throw new NotImplementedException("Haven't implemented GuiSkin support for MultiButtonMessageBoxes yet");
                //buttonAdded = base.AddButton(this.buttonBlueprint.Clone());
                //buttonAdded.SpriteFrame.Z = this.SpriteFrame.Z - .01f;
                //SpriteManager.AddSpriteFrame(buttonAdded.SpriteFrame);
            }


            buttonAdded.Text = buttonText;

            AddButton(buttonAdded);

            // This used to be made invisible - I don't know why - we should close it!
            buttonAdded.Click += new GuiMessage(RemoveThis);
            buttonAdded.Click += message;
        }

        public void AddButton(Button buttonToAdd)
        {
            buttonToAdd.ScaleX = TextManager.GetWidth(buttonToAdd.Text, GuiManager.TextSpacing) / 2.0f + .5f;
            buttonToAdd.ScaleY = 1.5f;
            buttonArray.Add(buttonToAdd);

            ResizeBox();

        }

        public Button GetButton(string buttonText)
        {
            foreach (Button button in buttonArray)
            {
                if (button.Text == buttonText)
                {
                    return button;
                }
            }

            return null;
        }

        public void RemoveButton(Button buttonToRemove)
        {
            buttonArray.Remove(buttonToRemove);

            this.RemoveWindow(buttonToRemove);

            ResizeBox();
        }

        #region Internal Methods

        internal override void DrawSelfAndChildren(Camera camera)
		{
            base.DrawSelfAndChildren(camera);

			textField.mZ = 100;

            TextManager.mXForVertexBuffer = X;
            TextManager.mYForVertexBuffer = Y;
            
            //textField.SetDimensions(
            //    (float)(WorldUnitY + ScaleY - .5f),
            //    (float)(WorldUnitY - ScaleY + 4),
            //    (float)(WorldUnitX - ScaleX + 1),
            //    (float)(WorldUnitX + ScaleX - 1), 100);

			textField.RelativeToCamera = false;
			TextManager.Draw(textField);

		}

        internal override int GetNumberOfVerticesToDraw()
		{
			return base.GetNumberOfVerticesToDraw () + textField.DisplayText.Replace("\n", "").Replace(" ", "").Length * 6;
        }

        #endregion

        private void ResizeBox()
		{
            ResizeBox(float.NaN, true);
        }

        protected virtual void ResizeBox(float newScaleX, bool autoSetScaleY)
        {
            if (float.IsNaN(newScaleX))
            {
                foreach (Window button in buttonArray)
                {
                    this.mScaleX = System.Math.Max(this.ScaleX, button.ScaleX + .5f);
                    // top border + text displayed ScaleY + space + button position
                }

                ResizeBox(mScaleX, autoSetScaleY);
                return;
            }
            mScaleX = newScaleX;
            int textLines = 0;

            if (GuiManagerDrawn)
            {
                textField.SetDimensions((Window)this);
                textField.FillLines();
                if (autoSetScaleY)
                {
                    this.ScaleY = (2 + textField.mLines.Count * mTextScale * 2.0f + 2.5f + buttonArray.Count * 3) / 2.0f;
                }
                textField.SetDimensions((Window)this);
                textField.FillLines();

                textLines = textField.mLines.Count;
            }
			
			int i = 0;
			foreach(Window button in buttonArray)
			{
				button.ScaleX = this.ScaleX - .5f;
				button.SetPositionTL(this.ScaleX + .001f, 2 * ScaleY - 2 - 3 * (buttonArray.Count - i - 1));
				i++;
			}
		}
		
		#endregion

	}
}
