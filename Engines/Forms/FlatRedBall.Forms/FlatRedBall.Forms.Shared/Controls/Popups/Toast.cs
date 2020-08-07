using Gum.Wireframe;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms.Controls.Popups
{
    public class Toast : FrameworkElement
    {
        protected GraphicalUiElement textComponent;
        protected RenderingLibrary.Graphics.Text coreTextObject;

        public string Text
        {
            get
            {
#if DEBUG
                ReportMissingTextInstance();
#endif
                return coreTextObject.RawText;
            }
            set
            {
#if DEBUG
                ReportMissingTextInstance();
#endif
                // go through the component instead of the core text object to force a layout refresh if necessary
                textComponent.SetProperty("Text", value);
            }
        }

        public Toast() : base() { }

        public Toast(GraphicalUiElement visual) : base(visual) { }

        protected override void ReactToVisualChanged()
        {
            textComponent = base.Visual.GetGraphicalUiElementByName("TextInstance");

            base.ReactToVisualChanged();
        }

#if DEBUG
        private void ReportMissingTextInstance()
        {
            if (textComponent == null)
            {
                throw new Exception(
                    $"This label was created with a Gum component ({Visual?.ElementSave}) " +
                    "that does not have an instance called 'text'. A 'text' instance must be added to modify the button's Text property.");
            }
        }
#endif
    }
}
