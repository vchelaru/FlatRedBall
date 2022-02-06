using FlatRedBall.Gui;
using FlatRedBall.Math.Geometry;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace FlatRedBall.Forms.Controls.Games
{
    public class OnScreenKeyboard : Controls.FrameworkElement, IInputReceiver
    {
        #region Fields/Properties

        public TextBox AssociatedTextBox { get; set; }

        public List<Keys> IgnoredKeys => throw new NotImplementedException();

        public bool TakingInput => true;

        public IInputReceiver NextInTabSequence
        { get; set; }

        GraphicalUiElement FocusIndicator;
        GraphicalUiElement Key1;

        List<GraphicalUiElement> selectableItems = new List<GraphicalUiElement>();

        #endregion

        public OnScreenKeyboard() : base() 
        {
            Initialize();
        }

        public OnScreenKeyboard(GraphicalUiElement visual) : base(visual)
        {
            Initialize();
        }

        public event FocusUpdateDelegate FocusUpdate;

        private void Initialize()
        {
        }





        protected override void ReactToVisualChanged()
        {
            FocusIndicator = this.Visual.GetChildByNameRecursively("HighlightRectangle") as GraphicalUiElement;

            Key1 = this.Visual.GetChildByNameRecursively("Key1") as GraphicalUiElement;

            FillWithSelectableItemsRecursive(this.Visual);

            UpdateKeyEvents(this.Visual.Children);

            base.ReactToVisualChanged();
        }

        private void FillWithSelectableItemsRecursive(GraphicalUiElement parent)
        {
            foreach (var child in parent.Children)
            {
                // We don't know any type info here, so the only way to know if it's selectable
                // is to look at the name
                if(child.Name?.StartsWith("Key") == true)
                {
                    selectableItems.Add(child as GraphicalUiElement);
                }
                else
                {
                    FillWithSelectableItemsRecursive(child as GraphicalUiElement);
                }

            }
        }

        private void UpdateKeyEvents(IList<IRenderableIpso> children)
        {
            foreach(var child in children)
            {
                var gue = child as GraphicalUiElement;
                if(gue.FormsControlAsObject is Button button)
                {
                    button.Click += HandleButtonClick;
                }
                else if(gue.RenderableComponent is InvisibleRenderable)
                {
                    // it's a container, so loop through its children
                    UpdateKeyEvents(child.Children);
                }
            }
        }

        private void HandleButtonClick(object sender, EventArgs e)
        {
            if(AssociatedTextBox == null)
            {
                throw new InvalidOperationException("You must first set the AssociatedTextBox before any input events are handled");
            }

            var button = sender as Button;
            var visual = button.Visual;

            var visualName = visual.Name;

            switch(visualName)
            {
                case "KeyBackspace":
                    AssociatedTextBox?.HandleBackspace();
                    break;
                case "KeyReturn":

                    break;
                case "KeyLeft":
                    if(AssociatedTextBox != null && AssociatedTextBox.CaretIndex > 0)
                    {
                        AssociatedTextBox.CaretIndex--;
                    }
                    break;
                case "KeyRight":
                    if(AssociatedTextBox != null && AssociatedTextBox.CaretIndex < AssociatedTextBox.Text.Length)
                    {
                        AssociatedTextBox.CaretIndex++;
                    }
                    break;
                case "KeySpace":
                    if(AssociatedTextBox != null)
                    {
                        AssociatedTextBox.HandleCharEntered(' ');
                    }
                    break;
                default:
                    var text = button.Text;

                    if(!string.IsNullOrWhiteSpace(text))
                    {
                        AssociatedTextBox.HandleCharEntered(text[0]);
                    }
                    break;
            }

            // check for special names here to perform custom activities
        }

        public void OnFocusUpdate()
        {
            foreach(var gamepad in GuiManager.GamePadsForUiControl)
            {
                if (gamepad.ButtonRepeatRate(FlatRedBall.Input.Xbox360GamePad.Button.DPadDown) ||
                    gamepad.LeftStick.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Down))
                {
                    HandleMove(RepositionDirections.Down);
                }
                else if (gamepad.ButtonRepeatRate(FlatRedBall.Input.Xbox360GamePad.Button.DPadUp) ||
                         gamepad.LeftStick.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Up))
                {
                    HandleMove(RepositionDirections.Up);
                }
                else if (gamepad.ButtonRepeatRate(FlatRedBall.Input.Xbox360GamePad.Button.DPadLeft) ||
                    gamepad.LeftStick.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Left))
                {
                    HandleMove(RepositionDirections.Left);
                }
                else if (gamepad.ButtonRepeatRate(FlatRedBall.Input.Xbox360GamePad.Button.DPadRight) ||
                         gamepad.LeftStick.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Right))
                {
                    HandleMove(RepositionDirections.Right);
                }


                if (gamepad.ButtonPushed(FlatRedBall.Input.Xbox360GamePad.Button.A))
                {
                    HandleLetterSelect();
                }
            }
        }

        private void HandleLetterSelect()
        {
            var selected = FocusIndicator.Parent;

            var wordAfterKey = String.Empty;
            if(selected.Name.Length > 2)
            {
                wordAfterKey = selected.Name.Substring(3);
            }

            if (wordAfterKey == "Backspace")
            {
                AssociatedTextBox?.HandleBackspace();
            }
            else if (wordAfterKey == "Return")
            {
                // do something?

            }
            else if (wordAfterKey == "Left")
            {
                if (AssociatedTextBox != null && AssociatedTextBox.CaretIndex > 0)
                {
                    AssociatedTextBox.CaretIndex--;
                }
            }
            else if (wordAfterKey == "Right")
            {
                if (AssociatedTextBox != null && AssociatedTextBox.CaretIndex < AssociatedTextBox.Text.Length)
                {
                    AssociatedTextBox.CaretIndex++;
                }
            }
            else
            {
                char selectedLetter = wordAfterKey[0];
                if(wordAfterKey == "Underscore")
                {
                    selectedLetter = '_';
                }
                else if(wordAfterKey == "Comma")
                {
                    selectedLetter = ',';
                }
                else if(wordAfterKey == "Period")
                {
                    selectedLetter = '.';
                }
                else if(wordAfterKey == "Hyphen")
                {
                    selectedLetter = '-';
                }
                else if (wordAfterKey == "ParenLeft")
                {
                    selectedLetter = '(';
                }
                else if (wordAfterKey == "ParenRight")
                {
                    selectedLetter = '(';
                }
                else if (wordAfterKey == "ParenRight")
                {
                    selectedLetter = '(';
                }
                else if (wordAfterKey == "Space")
                {
                    selectedLetter = ' ';
                }
                else if(wordAfterKey == "Question")
                {
                    selectedLetter = '?';
                }
                else if (wordAfterKey == "Bang")
                {
                    selectedLetter = '!';
                }
                else if (wordAfterKey == "Ampersand")
                {
                    selectedLetter = '&';
                }

                AssociatedTextBox?.HandleCharEntered(selectedLetter);

            }
        }

        public void OnGainFocus()
        {
            if (FocusIndicator != null)
            {
                FocusIndicator.Visible = true;
            }
        }

        public void LoseFocus()
        {
            if (FocusIndicator != null)
            {
                FocusIndicator.Visible = false;
            }
        }

        public void ReceiveInput()
        {

        }

        public void HandleKeyDown(Keys key, bool isShiftDown, bool isAltDown, bool isCtrlDown)
        {

        }

        public void HandleCharEntered(char character)
        {

        }

        private void HandleMove(RepositionDirections direction)
        {
            var selected = FocusIndicator.Parent as IRenderableIpso;

            float x = 0;
            float y = 0;

            var keyWidth = Key1.GetAbsoluteWidth();
            var keyHeight = Key1.GetAbsoluteHeight();

            switch (direction)
            {
                case RepositionDirections.Left:
                    x = selected.GetAbsoluteLeft() - keyWidth / 4.0f;
                    y = selected.GetAbsoluteTop() + keyHeight / 4.0f;
                    break;
                case RepositionDirections.Right:
                    x = selected.GetAbsoluteRight() + keyWidth / 4.0f;
                    y = selected.GetAbsoluteTop() + keyHeight / 4.0f;
                    break;
                case RepositionDirections.Up:
                    x = selected.GetAbsoluteCenterX();
                    y = selected.GetAbsoluteTop() - keyHeight / 4.0f;

                    break;
                case RepositionDirections.Down:
                    x = selected.GetAbsoluteCenterX();
                    y = selected.GetAbsoluteBottom() + keyHeight / 4.0f;
                    break;
            }

            var newItem = GetKeyOrButtonAt(x, y);
            if (newItem != null)
            {
                FocusIndicator.Parent = newItem;
            }
        }

        private GraphicalUiElement GetKeyOrButtonAt(float x, float y)
        {
            foreach (GraphicalUiElement item in selectableItems)
            {
                if (item.Visible)
                {
                    var widthHalf = item.GetAbsoluteWidth() / 2.0f;
                    var heightHalf = item.GetAbsoluteHeight() / 2.0f;

                    var absoluteX = item.GetAbsoluteCenterX();
                    var absoluteY = item.GetAbsoluteCenterY();

                    if (x > absoluteX - widthHalf && x < absoluteX + widthHalf &&
                        y > absoluteY - heightHalf && y < absoluteY + heightHalf)
                    {
                        return item;
                    }
                }
            }

            return null;
        }
    }
}
