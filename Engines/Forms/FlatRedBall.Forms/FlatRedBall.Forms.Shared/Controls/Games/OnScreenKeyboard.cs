using FlatRedBall.Gui;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
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
            //this.GotFocus += HandleGotFocus;
        }

        //private void HandleGotFocus(object sender, EventArgs args)
        //{
        //    //// This can't have focus, so pass it along to the 1 key
        //    //var child = this.Visual.GetChildByNameRecursively("Key1");
        //    //if (child is GraphicalUiElement gue)
        //    //{
        //    //    var key = gue.FormsControlAsObject;
        //    //    if (key is FrameworkElement keyFrameworkElement)
        //    //    {
        //    //        keyFrameworkElement.IsFocused = true;
        //    //    }
        //    //}
        //}



        protected override void ReactToVisualChanged()
        {

            UpdateKeyEvents(this.Visual.Children);

            base.ReactToVisualChanged();
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
                    if(AssociatedTextBox != null)
                    {
                        AssociatedTextBox.HandleBackspace();
                    }
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
        }

        public void OnGainFocus()
        {

        }

        public void LoseFocus()
        {

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
    }
}
