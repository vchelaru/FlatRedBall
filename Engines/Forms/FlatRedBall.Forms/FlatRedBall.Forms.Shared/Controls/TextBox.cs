using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms.Controls
{
    public class TextBox : FrameworkElement
    {
        #region Fields/Properties

        GraphicalUiElement textComponent;
        RenderingLibrary.Graphics.Text coreTextObject;

        GraphicalUiElement cursorComponent;

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
        public int CaretIndex
        {
            get { return caretIndex; }
            set { caretIndex = value; UpdateToCaretIndex(); }
        }

        #endregion

        protected override void ReactToVisualChanged()
        {
            textComponent = base.Visual.GetGraphicalUiElementByName("Text");
            coreTextObject = textComponent.RenderableComponent as RenderingLibrary.Graphics.Text;
            cursorComponent = base.Visual.GetGraphicalUiElementByName("Cursor");

            base.ReactToVisualChanged();
        }

        public void HandleKeyDown(Microsoft.Xna.Framework.Input.Keys key)
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

        public void HandleCharEntered(char character)
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

        private void UpdateToCaretIndex()
        {
            var substring = Text.Substring(0, caretIndex);
            var measure = this.coreTextObject.BitmapFont.MeasureString(substring);
            cursorComponent.X = measure;
        }

    }
}
