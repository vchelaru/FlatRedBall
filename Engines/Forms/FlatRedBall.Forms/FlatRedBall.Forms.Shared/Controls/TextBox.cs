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
            set
            {
                hasFocus = value && IsEnabled;
                UpdateToHasFocus();
            }
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


                CaretIndex = System.Math.Min(CaretIndex, value?.Length ?? 0);
            }
        }

        int caretIndex;

        public event FocusUpdateDelegate FocusUpdate;

        public int CaretIndex
        {
            get { return caretIndex; }
            set
            {
                caretIndex = value;
                UpdateToCaretIndex();
                OffsetTextToKeepCaretInView();
            }
        }

        public List<Keys> IgnoredKeys => null;

        public bool TakingInput => true;

        public IInputReceiver NextInTabSequence { get; set; }

        public override bool IsEnabled
        {
            get
            {
                return base.IsEnabled;
            }
            set
            {
                base.IsEnabled = value;
                if(!IsEnabled)
                {
                    HasFocus = false;
                }
                UpdateState();
            }
        }

        #endregion

        #region Initialize Methods

        protected override void ReactToVisualChanged()
        {
            textComponent = base.Visual.GetGraphicalUiElementByName("TextInstance");
            coreTextObject = textComponent.RenderableComponent as RenderingLibrary.Graphics.Text;
            caretComponent = base.Visual.GetGraphicalUiElementByName("CaretInstance");

#if DEBUG
            if (textComponent == null) throw new Exception("Gum object must have an object called \"Text\"");
            if (coreTextObject == null) throw new Exception("The Text instance must be of type Text");
            if (caretComponent == null) throw new Exception("Gum object must have an object called \"Caret\"");

#endif

            Visual.Click += this.HandleClick;
            Visual.RollOn += this.HandleRollOn;
            Visual.RollOff += this.HandleRollOff;

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

        private void HandleRollOn(IWindow window)
        {
            UpdateState();
        }

        private void HandleRollOff(IWindow window)
        {
            UpdateState();
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

        public void HandleKeyDown(Microsoft.Xna.Framework.Input.Keys key, bool isShiftDown, bool isAltDown, bool isCtrlDown)
        {
            if(hasFocus)
            {
                var oldIndex = caretIndex;
               switch (key)
                {
                    case Microsoft.Xna.Framework.Input.Keys.Left:
                        if (caretIndex > 0)
                        {
                            caretIndex--;
                        }
                        break;
                    case Keys.Home:
                        caretIndex = 0;
                        break;
                    case Keys.End:
                        caretIndex = Text.Length;
                        break;
                    case Keys.Back:
                        HandleBackspace(isCtrlDown);
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
                            this.Text = this.Text.Remove(caretIndex, 1);
                        }

                        break;
                }
                if(oldIndex != caretIndex)
                {
                    UpdateToCaretIndex();
                    OffsetTextToKeepCaretInView();
                }
            }
        }

        public void HandleCharEntered(char character)
        {
            if(hasFocus)
            {
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
                OffsetTextToKeepCaretInView();

            }
        }

        private void HandleBackspace(bool isCtrlDown)
        {
            if (caretIndex > 0)
            {
                if(isCtrlDown)
                {
                    var indexBeforeNullable = GetSpaceIndexBefore(caretIndex);

                    var indexToDeleteTo = indexBeforeNullable ?? 0;

                    this.Text = Text.Remove(indexToDeleteTo, caretIndex - indexToDeleteTo);

                    caretIndex = indexToDeleteTo;
                }
                else
                {
                    this.Text =
                        this.Text.Remove(caretIndex - 1, 1);
                    caretIndex--;
                }
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

        #region UpdateTo Methods


        private void UpdateState()
        {
            if(IsEnabled == false)
            {
                Visual.SetProperty("TextBoxCategoryState", "Disabled");
            }
            else if(HasFocus)
            {
                Visual.SetProperty("TextBoxCategoryState", "Selected");
            }
            else if(Visual.HasCursorOver(GuiManager.Cursor))
            {
                Visual.SetProperty("TextBoxCategoryState", "Highlighted");
            }
            else
            {
                Visual.SetProperty("TextBoxCategoryState", "Enabled");
            }
        }


        private void UpdateToCaretIndex()
        {
            var substring = Text.Substring(0, caretIndex);
            var measure = this.coreTextObject.BitmapFont.MeasureString(substring);
            

            caretComponent.X = measure + this.textComponent.X;
        }

        private void UpdateToHasFocus()
        {
            caretComponent.Visible = hasFocus;
            UpdateState();
        }

        private void OffsetTextToKeepCaretInView()
        {
            float leftOfCaret = caretComponent.AbsoluteX;
            float rightOfCaret = caretComponent.AbsoluteX + caretComponent.GetAbsoluteWidth();

            float leftOfParent = caretComponent.ParentGue.AbsoluteX;
            float rightOfParent = caretComponent.ParentGue.AbsoluteX + caretComponent.ParentGue.GetAbsoluteWidth();

            float shiftAmount = 0;
            const float padding = 5;
            if(rightOfCaret > rightOfParent)
            {
                shiftAmount = rightOfParent - rightOfCaret - padding;
            }
            if(leftOfCaret < leftOfParent)
            {
                shiftAmount = leftOfParent - leftOfCaret + padding;
            }

            if(shiftAmount != 0)
            {
                this.textComponent.X += shiftAmount;
                this.caretComponent.X += shiftAmount;
            }
        }




        #endregion

        #region Utilities

        int? GetSpaceIndexBefore(int index)
        {
            for(int i = index - 1; i > 0; i--)
            {
                var isSpace = Char.IsWhiteSpace(Text[i]);

                if(isSpace)
                {
                    return i;
                }
            }
            return null;
        }

        int? GetSpaceIndexAfter(int index)
        {
            for (int i = index; i < Text.Length; i++)
            {
                var isSpace = Char.IsWhiteSpace(Text[i]);

                if (isSpace)
                {
                    return i;
                }
            }
            return null;
        }

        #endregion
    }
}
