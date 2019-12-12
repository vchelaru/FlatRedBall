using FlatRedBall.Gui;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Input;

namespace FlatRedBall.Forms.Controls
{
    public class TextBox : TextBoxBase
    {
        #region Fields/Properties

        protected override string DisplayedText
        {
            get 
            { 
                return Text; 
            }
        }

        public string Text
        {
            get
            {
                return coreTextObject.RawText;
            }
            set
            {
                if(value != Text)
                {
                    // go through the component instead of the core text object to force a layout refresh if necessary
                    textComponent.SetProperty("Text", value);


                    CaretIndex = System.Math.Min(CaretIndex, value?.Length ?? 0);

                    TextChanged?.Invoke(this, null);
                }
            }
        }

        #endregion

        #region Events

        public event EventHandler TextChanged;

        #endregion 

        #region Initialize Methods

        public TextBox() : base() { }

        public TextBox(GraphicalUiElement visual) : base(visual) { }

        protected override void ReactToVisualChanged()
        {
            base.ReactToVisualChanged();

            if(selectionInstance != null)
            {
                selectionInstance.Visible = false;
            }
        }

        #endregion

        #region Event Handler Methods

        public override void HandleCharEntered(char character)
        {
            if(hasFocus)
            {
                if(selectionLength != 0)
                {
                    DeleteSelection();
                }

                // If text is null force it to be an empty string so we can add characters
                Text = Text ?? "";

                // Do we want to handle backspace here or should it be in the Keys handler?
                if (character == '\b'  
                    // I think CTRL Backspace?
                    || character == (char)127 
                    // esc
                    || character == (char)27)
                {
                    // do nothing, handled with a backspace above
                //    HandleBackspace();
                }
                else if (character == '\r' || character == '\n')
                {
                    // We don't support multiline yet
                    //this.Text += '\n';
                }
                else
                {
                    this.Text = this.Text.Insert(caretIndex, "" + character);
                    caretIndex++;
                }
                UpdateCaretPositionToCaretIndex();
                OffsetTextToKeepCaretInView();

            }
        }

        protected override void HandleBackspace(bool isCtrlDown)
        {
            if (hasFocus && caretIndex > 0 && Text != null)
            {
                if(selectionLength > 0)
                {
                    DeleteSelection();
                }
                else if (isCtrlDown)
                {
                    var indexBeforeNullable = GetSpaceIndexBefore(caretIndex);

                    var indexToDeleteTo = indexBeforeNullable ?? 0;

                    this.Text = Text.Remove(indexToDeleteTo, caretIndex - indexToDeleteTo);

                    caretIndex = indexToDeleteTo;
                }
                else
                {
                    var whereToRemoveFrom = caretIndex - 1;
                    // Move the care to the left one before removing from the text. Otherwise, if the
                    // caret is at the end of the word, modifying the word will shift the caret to the left, 
                    // and that could cause it to shift over two times.
                    caretIndex--;
                    this.Text = this.Text.Remove(whereToRemoveFrom, 1);
                }
            }
        }

        protected override void HandleDelete()
        {
            if (selectionLength > 0)
            {
                DeleteSelection();
            }
            else if (caretIndex < (Text?.Length ?? 0))
            {
                this.Text = this.Text.Remove(caretIndex, 1);
            }
        }

        protected override void HandleCopy()
        {
            if(selectionLength != 0)
            {
                var whatToCopy = DisplayedText.Substring(
                    selectionStart, selectionLength);
                Clipboard.ClipboardImplementation.PushStringToClipboard(
                    whatToCopy);
            }
        }

        protected override void HandleCut()
        {
            if (selectionLength != 0)
            {
                var whatToCopy = DisplayedText.Substring(
                    selectionStart, selectionLength);
                Clipboard.ClipboardImplementation.PushStringToClipboard(
                    whatToCopy);

                DeleteSelection();
            }
        }

        protected override void HandlePaste()
        {
            var whatToPaste = Clipboard.ClipboardImplementation.GetText();

            if(!string.IsNullOrEmpty(whatToPaste))
            {
                if(selectionLength != 0)
                {
                    DeleteSelection();
                }
                foreach(var character in whatToPaste)
                {
                    this.Text = this.Text.Insert(caretIndex, "" + character);
                    caretIndex++;
                }
                UpdateCaretPositionToCaretIndex();
                OffsetTextToKeepCaretInView();
            }
        }

        private void DeleteSelection()
        {
            this.Text = Text.Remove(selectionStart, selectionLength);
            CaretIndex = selectionStart;
            SelectionLength = 0;
        }

        #endregion

        #region UpdateTo

        protected override void UpdateToCaretChanged(int oldIndex, int newIndex, bool isShiftDown)
        {
            if(isShiftDown)
            {
                var change = oldIndex - newIndex;

                if(SelectionLength == 0)
                {
                    // set the field (doesn't update the selection visuals)...
                    selectionStart = System.Math.Min(oldIndex, newIndex);
                    // ...now set the property to update the visuals.
                    SelectionLength = System.Math.Abs(oldIndex - newIndex);
                }
                else
                {
                    int leftMost = 0;
                    int rightMost = 0;
                    if(oldIndex == selectionStart)
                    {
                        leftMost = System.Math.Min(selectionStart + selectionLength, newIndex);
                        rightMost = System.Math.Max(selectionStart + selectionLength, newIndex);
                    }
                    else
                    {
                        leftMost = System.Math.Min(selectionStart, newIndex);
                        rightMost = System.Math.Max(selectionStart, newIndex);
                    }

                    selectionStart = leftMost;
                    SelectionLength = rightMost - leftMost;
                }
            }
            else
            {
                SelectionLength = 0;
            }
        }
        
        #endregion

        #region Utilities

        int? GetSpaceIndexBefore(int index)
        {
            if (DisplayedText != null)
            {
                for (int i = index - 1; i > 0; i--)
                {
                    var isSpace = Char.IsWhiteSpace(DisplayedText[i]);

                    if (isSpace)
                    {
                        return i;
                    }
                }
            }

            return null;
        }

        int? GetSpaceIndexAfter(int index)
        {
            if (DisplayedText != null)
            {
                for (int i = index; i < DisplayedText.Length; i++)
                {
                    var isSpace = Char.IsWhiteSpace(DisplayedText[i]);

                    if (isSpace)
                    {
                        return i;
                    }
                }
            }

            return null;
        }


        #endregion
    }
}
