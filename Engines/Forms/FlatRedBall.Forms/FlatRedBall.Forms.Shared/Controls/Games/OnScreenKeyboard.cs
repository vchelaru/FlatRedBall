using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace FlatRedBall.Forms.Controls.Games
{
    public class OnScreenKeyboard : Controls.FrameworkElement
    {

        public TextBox AssociatedTextBox { get; set; }

        public OnScreenKeyboard() : base() { }

        public OnScreenKeyboard(GraphicalUiElement visual) : base(visual)
        {

        }

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
    }
}
