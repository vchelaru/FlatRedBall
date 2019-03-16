using FlatRedBall.Forms.Input;
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

        protected GraphicalUiElement selectionInstance;

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

        /// <summary>
        /// The cursor index where the cursor was last pushed, used for drag+select
        /// </summary>
        private int? indexPushed;

        protected int selectionStart;
        public int SelectionStart
        {
            get { return selectionStart; }
            set
            {
                if (selectionStart != value)
                {
                    selectionStart = value;
                    UpdateToSelection();
                }
            }
        }

        protected int selectionLength;
        public int SelectionLength
        {
            get { return selectionLength; }
            set
            {
                if (selectionLength != value)
                {
                    if(value < 0)
                    {
                        throw new Exception($"Value cannot be less than 0, but is {value}");
                    }
                    selectionLength = value;
                    UpdateToSelection();
                    UpdateCaretVisibility();
                }
            }
        }

        // todo - this could move to the base class, if the base objects became input receivers
        public event Action<object, KeyEventArgs> KeyDown;

        #endregion

        #region Initialize Methods

        public TextBoxBase() : base() { }

        public TextBoxBase(GraphicalUiElement visual) : base(visual) { }

        protected override void ReactToVisualChanged()
        {
            textComponent = base.Visual.GetGraphicalUiElementByName("TextInstance");
            caretComponent = base.Visual.GetGraphicalUiElementByName("CaretInstance");
            // optional:
            selectionInstance = base.Visual.GetGraphicalUiElementByName("SelectionInstance");

            coreTextObject = textComponent.RenderableComponent as RenderingLibrary.Graphics.Text;
#if DEBUG
            if (textComponent == null) throw new Exception("Gum object must have an object called \"Text\"");
            if (coreTextObject == null) throw new Exception("The Text instance must be of type Text");
            if (caretComponent == null) throw new Exception("Gum object must have an object called \"Caret\"");
#endif

            Visual.Click += this.HandleClick;
            Visual.Push += this.HandlePush;
            Visual.RollOn += this.HandleRollOn;
            Visual.RollOver += this.HandleRollOver;
            Visual.DragOver += this.HandleDrag;
            Visual.RollOff += this.HandleRollOff;
            Visual.SizeChanged += HandleVisualSizeChanged;

            this.textComponent.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

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

        private void HandlePush(IWindow window)
        {
            indexPushed = GetCaretIndexAtCursor();

        }

        private void HandleClick(IWindow window)
        {
            FlatRedBall.Input.InputManager.InputReceiver = this;

            if(GuiManager.Cursor.PrimaryDoubleClick)
            {
                selectionStart = 0;
                SelectionLength = DisplayedText?.Length ?? 0;
            }
            else if(GuiManager.Cursor.PrimaryClickNoSlide)
            {
                UpdateCarrotIndexFromCursor();
            }
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

        private void HandleRollOver(IWindow window)
        {
            if(GuiManager.Cursor.LastInputDevice == InputDevice.Mouse)
            {
                if(GuiManager.Cursor.WindowPushed == this.Visual && indexPushed != null && GuiManager.Cursor.PrimaryDown)
                {
                    var currentIndex = GetCaretIndexAtCursor();

                    var minIndex = System.Math.Min(currentIndex, indexPushed.Value);

                    var maxIndex = System.Math.Max(currentIndex, indexPushed.Value);

                    selectionStart = minIndex;
                    SelectionLength = maxIndex - minIndex;
                }
            }
        }

        private void HandleDrag(IWindow window)
        {
            if (GuiManager.Cursor.LastInputDevice == InputDevice.TouchScreen)
            {
                if (GuiManager.Cursor.WindowPushed == this.Visual && GuiManager.Cursor.PrimaryDown)
                {
                    var xChange = GuiManager.Cursor.ScreenXChange / RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom;


                    var bitmapFont = this.coreTextObject.BitmapFont;
                    var stringLength = bitmapFont.MeasureString(DisplayedText);

                    var minimumShift = System.Math.Min(
                        edgeToTextPadding, 
                        textComponent.Parent.Width - stringLength - edgeToTextPadding);

                    var maximumShift = edgeToTextPadding;
                    var newTextValue = System.Math.Min(
                        textComponent.X + xChange, 
                        maximumShift);

                    newTextValue = System.Math.Max(newTextValue, minimumShift);

                    var amountToShift = newTextValue - textComponent.X;
                    textComponent.X += amountToShift;
                    caretComponent.X += amountToShift;
                }
            }
        }

        private void HandleRollOff(IWindow window)
        {
            UpdateState();
        }

        private void UpdateCarrotIndexFromCursor()
        {
            int index = GetCaretIndexAtCursor();

            CaretIndex = index;
        }

        private int GetCaretIndexAtCursor()
        {
            var cursorScreenX = GuiManager.Cursor.GumX();
            var leftOfText = this.textComponent.AbsoluteX;
            var cursorOffset = cursorScreenX - leftOfText;

            var index = DisplayedText?.Length ?? 0;
            float distanceMeasuredSoFar = 0;
            var bitmapFont = this.coreTextObject.BitmapFont;

            for (int i = 0; i < (DisplayedText?.Length ?? 0); i++)
            {
                char character = DisplayedText[i];
                RenderingLibrary.Graphics.BitmapCharacterInfo characterInfo = bitmapFont.GetCharacterInfo(character);

                int advance = 0;

                if (characterInfo != null)
                {
                    advance = characterInfo.GetXAdvanceInPixels(coreTextObject.BitmapFont.LineHeightInPixels);
                }

                distanceMeasuredSoFar += advance;

                // This should find which side of the character you're closest to, but for now it's good enough...
                if (distanceMeasuredSoFar > cursorOffset)
                {
                    var halfwayPoint = distanceMeasuredSoFar - (advance / 2.0f);
                    if (halfwayPoint > cursorOffset)
                    {
                        index = i;
                    }
                    else
                    {
                        index = i + 1;
                    }
                    break;
                }
            }

            return index;
        }

        public void HandleKeyDown(Microsoft.Xna.Framework.Input.Keys key, bool isShiftDown, bool isAltDown, bool isCtrlDown)
        {
            if (hasFocus)
            {
                var oldIndex = caretIndex;

                switch (key)
                {
                    case Microsoft.Xna.Framework.Input.Keys.Left:
                        if(selectionLength != 0 && isShiftDown == false)
                        {
                            caretIndex = selectionStart;
                            SelectionLength = 0;
                        }
                        else if (caretIndex > 0)
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
                        if(selectionLength != 0 && isShiftDown == false)
                        {
                            caretIndex = selectionStart + selectionLength;
                            SelectionLength = 0;
                        }
                        else if (caretIndex < (DisplayedText?.Length ?? 0))
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
                    case Keys.C:
                        if(isCtrlDown)
                        {
                            HandleCopy();
                        }
                        break;
                    case Keys.X:
                        if (isCtrlDown)
                        {
                            HandleCut();
                        }
                        break;
                    case Keys.V:
                        if (isCtrlDown)
                        {
                            HandlePaste();
                        }
                        break;
                }


                if (oldIndex != caretIndex)
                {
                    UpdateToCaretChanged(oldIndex, caretIndex, isShiftDown);
                    UpdateToCaretIndex();
                    OffsetTextToKeepCaretInView();
                }

                var keyEventArg = new KeyEventArgs();
                keyEventArg.Key = key;
                KeyDown?.Invoke(this, keyEventArg);


            }
        }

        protected virtual void HandleCopy()
        {

        }

        protected virtual void HandleCut()
        {

        }

        protected virtual void HandlePaste()
        {

        }

        protected virtual void UpdateToCaretChanged(int oldIndex, int newIndex, bool isShiftDown)
        {

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
            UpdateCaretVisibility();
            UpdateState();

            if (hasFocus)
            {
                GuiManager.AddNextClickAction(HandleClickOff);

#if ANDROID
                FlatRedBall.Input.InputManager.Keyboard.ShowKeyboard();
#endif

            }
            else if (!hasFocus && FlatRedBall.Input.InputManager.InputReceiver == this)
            {
                FlatRedBall.Input.InputManager.InputReceiver = null;
#if ANDROID
                FlatRedBall.Input.InputManager.Keyboard.HideKeyboard();
#endif
            }
        }

        private void UpdateCaretVisibility()
        {
            caretComponent.Visible = hasFocus && selectionLength == 0;
        }

        protected void UpdateToSelection()
        {
            if (selectionInstance != null && selectionLength > 0 && DisplayedText?.Length > 0)
            {
                selectionInstance.Visible = true;

                selectionInstance.XUnits =
                    global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

                var substring = DisplayedText.Substring(0, selectionStart);
                var firstMeasure = this.coreTextObject.BitmapFont.MeasureString(substring);
                selectionInstance.X = this.textComponent.X + firstMeasure;

                substring = DisplayedText.Substring(0, selectionStart + selectionLength);
                selectionInstance.Width = 1 +
                    this.coreTextObject.BitmapFont.MeasureString(substring) - firstMeasure;

            }
            else if (selectionInstance != null)
            {
                selectionInstance.Visible = false;
            }
        }

        /// <summary>
        /// The maximum distance between the edge of the control and the text.
        /// Either we will want to make this customizable at some point, or remove
        /// this value and base it on some value of a parent, like we do for the scroll
        /// bar. This would require the Text to have a custom parent specifically defining
        /// the range of the text object.
        /// </summary>
        const float edgeToTextPadding = 5;

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
            if (rightOfCaret > rightOfParent)
            {
                shiftAmount = rightOfParent - rightOfCaret - edgeToTextPadding;
            }
            if (leftOfCaret < leftOfParent)
            {
                shiftAmount = leftOfParent - leftOfCaret + edgeToTextPadding;
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
