using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;

using FlatRedBall.Gui;

using FlatRedBall.Input;

#if FRB_MDX
using Keys = Microsoft.DirectX.DirectInput.Key;
#else
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

#endif


namespace EditorObjects.Gui
{
    public class ToolsWindow : CollapseWindow
    {
        #region Enums

        public enum ToolsButton
        {
            Attach,
            Copy,
            Detach,
            Move,
            Play,
            Rotate,
            Scale,
            Stop,
            Rewind
        }

        #endregion

        #region Fields

        float mNumberOfRows = 3;
        float mChildrenButtonScale = 1.3f;
        
        float mBorderWidth = .5f;

        WindowArray mButtons = new WindowArray();

        // This dicationary holds elements which have keyboard keys associated with them
        Dictionary<Keys, Button> mShortcutAssociation = 
            new Dictionary<Keys, Button>();

        #endregion

        #region Properties

        public float ExtraSpacingBetweenSameRowButtons
        {
            get;
            set;
        }

        public float NumberOfRows
        {
            get { return mNumberOfRows; }
            set 
            { 
                mNumberOfRows = value;
                UpdateDimensions();
            }
        }

        #region XML Docs
        /// <summary>
        /// Controls the distance from the edge of the ToolWindow to the edge of Buttons.
        /// </summary>
        #endregion
        public float BorderWidth
        {
            get { return mBorderWidth; }
            set 
            { 
                // Right now this needs to be set before buttons are added - but we could potentially
                // reposition all buttons when this value is set if we want to allow users to change this
                // after buttons are positioned.
                mBorderWidth = value; 
            
            }
        }

        #endregion

        #region Methods

        #region Constructor

        #region XML Docs
        /// <summary>
        /// Creates a new ToolsWindow and adds it to the GuiManager.
        /// </summary>
        #endregion
        public ToolsWindow()
            : base(GuiManager.Cursor)
        {
            GuiManager.AddWindow(this);
            base.mName = "Tools";
            UpdateDimensions();

            this.X = SpriteManager.Camera.XEdge * 2 - this.ScaleX;
        }

        #endregion

        #region Public Methods

        public Button AddButton()
        {
            Button button = new Button(mCursor);
            AddWindow(button);
            button.ScaleX = button.ScaleY = mChildrenButtonScale;
            SetPositionForNewUIElement(button);

            mButtons.Add(button);

            UpdateDimensions();

            return button;
        }


        public Button AddButton(Keys shortcutKey)
        {
            Button button = this.AddButton();

            mShortcutAssociation.Add(shortcutKey, button);

            return button;
        }


        public Button AddButton(ToolsButton toolsButton)
        {
            // since MDX doesn't have the .None enum for Key, code duplication is happening:

            Button button = this.AddButton();

            int row, column;
            string text;

            GetRowAndColumnFor(toolsButton, out row, out column, out text);

            button.Text = text;
            button.SetOverlayTextures(column, row);

            return button;
        }


        public Button AddButton(ToolsButton toolsButton, Keys shortcutKey)
        {
            Button button = button = this.AddButton(toolsButton);

#if FRB_XNA
            if (shortcutKey != Keys.None)
#endif
            {
                mShortcutAssociation.Add(shortcutKey, button);
            }

            return button;
        }


        public ToggleButton AddToggleButton()
        {
            ToggleButton toggleButton = new ToggleButton(mCursor);
            AddWindow(toggleButton);
            toggleButton.ScaleX = toggleButton.ScaleY = mChildrenButtonScale;
            SetPositionForNewUIElement(toggleButton);

            mButtons.Add(toggleButton);

            UpdateDimensions();

            return toggleButton;
        }


        public ToggleButton AddToggleButton(string textureToUse, string contentManagerName)
        {
            ToggleButton button = this.AddToggleButton();
            Texture2D newTexture = FlatRedBallServices.Load<Texture2D>(textureToUse, contentManagerName);

            button.SetOverlayTextures(
                newTexture, newTexture);

            return button;
        }


        public ToggleButton AddToggleButton(Keys shortcutKey)
        {
            ToggleButton button = this.AddToggleButton();

            mShortcutAssociation.Add(shortcutKey, button);

            return button;
        }


        public ToggleButton AddToggleButton(ToolsButton toolsButton)
        {
            // since MDX doesn't have the .None enum for Key, code duplication is happening:

            ToggleButton button = this.AddToggleButton();

            int row, column;
            string text;

            GetRowAndColumnFor(toolsButton, out row, out column, out text);

            button.Text = text;
            button.SetOverlayTextures(column, row);

            return button;
        }


        public ToggleButton AddToggleButton(ToolsButton toolsButton, Keys shortcutKey)
        {
            ToggleButton button = this.AddToggleButton(toolsButton);

#if FRB_XNA
            if (shortcutKey != Keys.None)
#endif
            {
                mShortcutAssociation.Add(shortcutKey, button);
            }

            return button;
        }


        public void ListenForShortcuts()
        {

            foreach (KeyValuePair<Keys, Button> kvp in mShortcutAssociation)
            {
                if (InputManager.Keyboard.KeyPushedConsideringInputReceiver(kvp.Key))
                {
                    kvp.Value.Press();

                }
            }

        }

        #endregion

        #region Private Methods

        private void GetRowAndColumnFor(ToolsButton toolsButton, out int row, out int column, out string buttonText)
        {

            switch (toolsButton)
            {
                case ToolsButton.Move:
                    row = 0;
                    column = 2;
                    buttonText = "Move";
                    break;
                case ToolsButton.Rotate:
                    row = 0;
                    column = 0;
                    buttonText = "Rotate";
                    break;
                case ToolsButton.Scale:
                    row = 0;
                    column = 1;
                    buttonText = "Scale";
                    break;
                case ToolsButton.Attach:
                    row = 0;
                    column = 7;
                    buttonText = "Attach";
                    break;
                case ToolsButton.Detach:
                    row = 0;
                    column = 10;
                    buttonText = "Detach";
                    break;
                case ToolsButton.Copy:
                    row = 0;
                    column = 9;
                    buttonText = "Duplicate";
                    break;
                case ToolsButton.Play:
                    row = 1;
                    column = 9;
                    buttonText = "Play";
                    break;
                case ToolsButton.Stop:
                    row = 1;
                    column = 11;
                    buttonText = "Stop";
                    break;
                case ToolsButton.Rewind:
                    row = 1;
                    column = 10;
                    buttonText = "Rewind";
                    break;
                default:
                    row = 0;
                    column = 0;
                    buttonText = "";
                    break;
            }
        }

        private void SetPositionForNewUIElement(Window window)
        {
            float x = mBorderWidth + mChildrenButtonScale;
            float y = mBorderWidth + mChildrenButtonScale;

            // If there are already buttons here then place the new button appropriately
            if (mButtons.Count != 0)
            {
                if (mButtons[mButtons.Count - 1].X > 2 * ScaleX - mBorderWidth - mChildrenButtonScale * 2)
                {
                    // Place the new button on a new row
                    y = mButtons[mButtons.Count - 1].Y + 2 * mChildrenButtonScale;
                }
                else
                {
                    // place the button on the same row, column to the right
                    x = mButtons[mButtons.Count - 1].X + 2 * mChildrenButtonScale + ExtraSpacingBetweenSameRowButtons;
                    y = mButtons[mButtons.Count - 1].Y;

                }
            }

            window.X = x;
            window.Y = y;
        }

        private void UpdateDimensions()
        {
            float bottomY = 0;
            float largestScaleY = 0;

            foreach (Window window in mChildren)
            {
                bottomY = Math.Max(bottomY, window.Y);
                largestScaleY = Math.Max(largestScaleY, window.ScaleY);
            }

            SetScaleTL(mBorderWidth + mChildrenButtonScale * mNumberOfRows + (ExtraSpacingBetweenSameRowButtons * (mNumberOfRows - 1)), (bottomY + largestScaleY + mBorderWidth) / 2.0f);

            //this.MinimumScaleX = ScaleX;
            //this.MinimumScaleY = ScaleY;

 //           mScaleY = (bottomY + largestScaleY + mBorderWidth) / 2.0f;

        }

        #endregion

        #endregion

    }
}
