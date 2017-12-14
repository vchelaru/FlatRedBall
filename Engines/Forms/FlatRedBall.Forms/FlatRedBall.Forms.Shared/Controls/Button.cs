using FlatRedBall.Forms.Controls.Primitives;
using FlatRedBall.Gui;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms.Controls
{

    public class Button : ButtonBase
    {
        #region Fields/Properties

        GraphicalUiElement textComponent;

        RenderingLibrary.Graphics.Text coreTextObject;

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
                    // todo - to add focus eventually
                    //HasFocus = false;
                }
                UpdateState();
            }
        }

        #endregion

        #region Initialize Methods

        protected override void ReactToVisualChanged()
        {
            // text component is optional:
            textComponent = base.Visual.GetGraphicalUiElementByName("TextInstance");

            base.ReactToVisualChanged();
        }


        #endregion

        #region UpdateTo Methods

        protected override void UpdateState()
        {
            var cursor = GuiManager.Cursor;

            if (IsEnabled == false)
            {
                Visual.SetProperty("ButtonCategoryState", "Disabled");
            }
            //else if (HasFocus)
            //{
            //}
            else if (GetIfIsOnThisOrChildVisual(cursor))
            {
                if (cursor.WindowPushed == Visual && cursor.PrimaryDown)
                {
                    Visual.SetProperty("ButtonCategoryState", "Pushed");
                }
                else
                {
                    Visual.SetProperty("ButtonCategoryState", "Highlighted");
                }
            }
            else
            {
                Visual.SetProperty("ButtonCategoryState", "Enabled");
            }
        }


        #endregion

        #region Utilities

#if DEBUG
        private void ReportMissingTextInstance()
        {
            if (textComponent == null)
            {
                throw new Exception(
                    "This button was created with a Gum component that does not have an instance called 'text'. A 'text' instance must be added to modify the button's Text property.");
            }
        }
#endif

        #endregion
    }
}
