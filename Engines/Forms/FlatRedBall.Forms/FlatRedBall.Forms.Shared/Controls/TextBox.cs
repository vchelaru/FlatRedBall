using FlatRedBall.Gui;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Input;

namespace FlatRedBall.Forms.Controls
{
    public class TextBox : FrameworkElement, IInputReceiver
    {
        #region Fields/Properties

        bool hasFocus;
        private bool HasFocus
        {
            get { return hasFocus; }
            set { hasFocus = value; UpdateToHasFocus(); }
        }

        GraphicalUiElement textComponent;
        RenderingLibrary.Graphics.Text coreTextObject;

        GraphicalUiElement caretComponent;

        public string Text
        {
            get { return coreTextObject.RawText;  }
            set
            {
                // go through the component instead of the core text object to force a layout refresh if necessary
                textComponent.SetProperty("Text", value);
            }
        }

        int caretIndex;

        public event FocusUpdateDelegate FocusUpdate;

        public int CaretIndex
        {
            get { return caretIndex; }
            set { caretIndex = value; UpdateToCaretIndex(); }
        }

        public List<Keys> IgnoredKeys => null;

        public bool TakingInput => true;

        public IInputReceiver NextInTabSequence { get; set; }

        #endregion


        #region Initialize Methods

        protected override void ReactToVisualChanged()
        {
            textComponent = base.Visual.GetGraphicalUiElementByName("Text");
            coreTextObject = textComponent.RenderableComponent as RenderingLibrary.Graphics.Text;
            caretComponent = base.Visual.GetGraphicalUiElementByName("Cursor");

#if DEBUG
            if (textComponent == null) throw new Exception("Gum object must have an object called \"Text\"");
            if (coreTextObject == null) throw new Exception("The Text instance must be of type Text");
            if (caretComponent == null) throw new Exception("Gum object must have an object called \"Cursor\"");

#endif

            Visual.Click += this.HandleClick;

            base.ReactToVisualChanged();

            HasFocus = false;
        }



        #endregion

        #region Event Handler Methods

        private void HandleClick(IWindow window)
        {
            Input.InputManager.InputReceiver = this;

            UpdateCarrotIndexFromCursor();

        }

        private void UpdateCarrotIndexFromCursor()
        {
            var cursorScreenX = GuiManager.Cursor.ScreenX;
            var leftOfText = this.textComponent.AbsoluteX;

            var offset = cursorScreenX - leftOfText;

            var index = Text.Length;
            float distanceMeasuredSoFar = 0;
            var bitmapFont = this.coreTextObject.BitmapFont;

            for (int i = 0; i < this.Text.Length; i++)
            {
                char character = Text[i];
                RenderingLibrary.Graphics.BitmapCharacterInfo characterInfo = bitmapFont.GetCharacterInfo(character);

                if (characterInfo != null)
                {
                    distanceMeasuredSoFar += characterInfo.GetXAdvanceInPixels(coreTextObject.BitmapFont.LineHeightInPixels);
                }

                // This should find which side of the character you're closest to, but for now it's good enough...
                if (distanceMeasuredSoFar > offset)
                {
                    index = i;
                    break;
                }
            }

            CaretIndex = index;
        }

        public void HandleKeyDown(Microsoft.Xna.Framework.Input.Keys key)
        {
            if(hasFocus)
            {
                switch (key)
                {
                    case Microsoft.Xna.Framework.Input.Keys.Left:
                        if (caretIndex > 0)
                        {
                            caretIndex--;
                        }
                        break;
                    case Microsoft.Xna.Framework.Input.Keys.Right:
                        if (caretIndex < Text.Length)
                        {
                            caretIndex++;
                        }
                        break;
                    case Microsoft.Xna.Framework.Input.Keys.Delete:
                        if (caretIndex < Text.Length)
                        {
                            this.Text =
                            this.Text.Remove(caretIndex, 1);
                        }

                        break;
                }
                UpdateToCaretIndex();
            }
        }

        public void HandleCharEntered(char character)
        {
            if(hasFocus)
            {
                if (character == '\b')
                {
                    if (caretIndex > 0)
                    {
                        this.Text =
                            this.Text.Remove(caretIndex - 1, 1);
                        caretIndex--;
                    }
                }
                else if (character == '\r')
                {
                    this.Text += '\n';
                }
                else
                {
                    this.Text = this.Text.Insert(caretIndex, "" + character);
                    caretIndex++;
                }
                UpdateToCaretIndex();
            }
        }


        public void OnFocusUpdate()
        {
        }

        public void OnGainFocus()
        {
            HasFocus = true;
        }

        public void LoseFocus()
        {
            HasFocus = false;

        }

        public void ReceiveInput()
        {

        }

        #endregion

        private void UpdateToCaretIndex()
        {
            var substring = Text.Substring(0, caretIndex);
            var measure = this.coreTextObject.BitmapFont.MeasureString(substring);
            

            caretComponent.X = measure + this.textComponent.X;
        }


        private void UpdateToHasFocus()
        {
            caretComponent.Visible = hasFocus;
        }

    }
}
