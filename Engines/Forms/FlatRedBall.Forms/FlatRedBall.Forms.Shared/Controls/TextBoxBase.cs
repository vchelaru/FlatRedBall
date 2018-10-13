using FlatRedBall.Gui;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms.Controls
{
    public abstract class TextBoxBase : FrameworkElement, IInputReceiver
    {
        #region Fields/Properties

        protected bool hasFocus;
        public bool HasFocus
        {
            get { return hasFocus; }
            set
            {
                hasFocus = value && IsEnabled;
                UpdateToHasFocus();
            }
        }


        protected GraphicalUiElement textComponent;
        protected RenderingLibrary.Graphics.Text coreTextObject;

        GraphicalUiElement caretComponent;

        public event FocusUpdateDelegate FocusUpdate;

        protected int caretIndex;
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
                if (!IsEnabled)
                {
                    HasFocus = false;
                }
                UpdateState();
            }
        }

        protected abstract string DisplayedText { get; }

        #endregion

        #region Initialize Methods

        public TextBoxBase() : base() { }

        public TextBoxBase(GraphicalUiElement visual) : base(visual) { }

        protected override void ReactToVisualChanged()
        {
            textComponent = base.Visual.GetGraphicalUiElementByName("TextInstance");
            caretComponent = base.Visual.GetGraphicalUiElementByName("CaretInstance");

            coreTextObject = textComponent.RenderableComponent as RenderingLibrary.Graphics.Text;
#if DEBUG
            if (textComponent == null) throw new Exception("Gum object must have an object called \"Text\"");
            if (coreTextObject == null) throw new Exception("The Text instance must be of type Text");
            if (caretComponent == null) throw new Exception("Gum object must have an object called \"Caret\"");
#endif

            Visual.Click += this.HandleClick;
            Visual.RollOn += this.HandleRollOn;
            Visual.RollOff += this.HandleRollOff;
            Visual.SizeChanged += HandleVisualSizeChanged;

            base.ReactToVisualChanged();

            OffsetTextToKeepCaretInView();

            HasFocus = false;
        }

        #endregion

        #region Event Handler Methods

        private void HandleVisualSizeChanged(object sender, EventArgs e)
        {
            OffsetTextToKeepCaretInView();
        }

        private void HandleClick(IWindow window)
        {
            Input.InputManager.InputReceiver = this;

            UpdateCarrotIndexFromCursor();
        }


        private void HandleClickOff()
        {
            if (GuiManager.Cursor.WindowOver != Visual)
            {
                HasFocus = false;
            }
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

            var index = DisplayedText?.Length ?? 0;
            float distanceMeasuredSoFar = 0;
            var bitmapFont = this.coreTextObject.BitmapFont;

            for (int i = 0; i < (DisplayedText?.Length ?? 0); i++)
            {
                char character = DisplayedText[i];
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
            if (hasFocus)
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
                        caretIndex = (DisplayedText?.Length ?? 0);
                        break;
                    case Keys.Back:
                        HandleBackspace(isCtrlDown);
                        break;
                    case Microsoft.Xna.Framework.Input.Keys.Right:
                        if (caretIndex < (DisplayedText?.Length ?? 0))
                        {
                            caretIndex++;
                        }
                        break;
                    case Microsoft.Xna.Framework.Input.Keys.Delete:
                        if (caretIndex < (DisplayedText?.Length ?? 0))
                        {
                            HandleDelete();
                        }

                        break;
                }
                if (oldIndex != caretIndex)
                {
                    UpdateToCaretIndex();
                    OffsetTextToKeepCaretInView();
                }
            }
        }

        protected abstract void HandleBackspace(bool isCtrlDown);

        protected abstract void HandleDelete();

        public abstract void HandleCharEntered(char character);

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
            var cursor = GuiManager.Cursor;

            if (IsEnabled == false)
            {
                Visual.SetProperty("TextBoxCategoryState", "Disabled");
            }
            else if (HasFocus)
            {
                Visual.SetProperty("TextBoxCategoryState", "Selected");
            }
            else if (cursor.LastInputDevice != InputDevice.TouchScreen && Visual.HasCursorOver(cursor))
            {
                Visual.SetProperty("TextBoxCategoryState", "Highlighted");
            }
            else
            {
                Visual.SetProperty("TextBoxCategoryState", "Enabled");
            }
        }

        protected void UpdateToCaretIndex()
        {
            // make sure we measure a valid string
            var stringToMeasure = DisplayedText ?? "";

            var substring = stringToMeasure.Substring(0, caretIndex);
            var measure = this.coreTextObject.BitmapFont.MeasureString(substring);
            caretComponent.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
            caretComponent.X = measure + this.textComponent.X;
        }

        private void UpdateToHasFocus()
        {
            caretComponent.Visible = hasFocus;
            UpdateState();

            if (hasFocus)
            {
                GuiManager.AddNextClickAction(HandleClickOff);

#if ANDROID
                FlatRedBall.Input.InputManager.Keyboard.ShowKeyboard();
#endif

            }
            else if (!hasFocus && Input.InputManager.InputReceiver == this)
            {
                Input.InputManager.InputReceiver = null;
#if ANDROID
                FlatRedBall.Input.InputManager.Keyboard.HideKeyboard();
#endif
            }
        }

        protected void OffsetTextToKeepCaretInView()
        {
            this.textComponent.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
            this.caretComponent.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

            float leftOfCaret = caretComponent.AbsoluteX;
            float rightOfCaret = caretComponent.AbsoluteX + caretComponent.GetAbsoluteWidth();

            float leftOfParent = caretComponent.EffectiveParentGue.AbsoluteX;
            float rightOfParent = caretComponent.EffectiveParentGue.AbsoluteX +
                caretComponent.EffectiveParentGue.GetAbsoluteWidth();

            float shiftAmount = 0;
            const float padding = 5;
            if (rightOfCaret > rightOfParent)
            {
                shiftAmount = rightOfParent - rightOfCaret - padding;
            }
            if (leftOfCaret < leftOfParent)
            {
                shiftAmount = leftOfParent - leftOfCaret + padding;
            }

            if (shiftAmount != 0)
            {
                this.textComponent.X += shiftAmount;
                this.caretComponent.X += shiftAmount;
            }
        }


        #endregion
    }
}
