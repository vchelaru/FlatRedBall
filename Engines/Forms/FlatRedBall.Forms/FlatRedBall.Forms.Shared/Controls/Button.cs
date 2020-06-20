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

        public Button() : base() { }

        public Button(GraphicalUiElement visual) : base(visual) { }

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
            else if (IsFocused)
            {
                if (cursor.WindowPushed == Visual && cursor.PrimaryDown)
                {
                    Visual.SetProperty("ButtonCategoryState", "Pushed");
                }
                // Even if the cursor is reported as being over the button, if the
                // cursor got its input from a touch screen then the cursor really isn't
                // over anything. Therefore, we only show the highlighted state if the cursor
                // is a physical on-screen cursor
                else if (GetIfIsOnThisOrChildVisual(cursor) && 
                    cursor.LastInputDevice != InputDevice.TouchScreen)
                {
                    Visual.SetProperty("ButtonCategoryState", "HighlightedFocused");
                }
                else
                {
                    Visual.SetProperty("ButtonCategoryState", "Focused");
                }
            }
            else if (GetIfIsOnThisOrChildVisual(cursor))
            {
                if (cursor.WindowPushed == Visual && cursor.PrimaryDown)
                {
                    Visual.SetProperty("ButtonCategoryState", "Pushed");
                }
                // Even if the cursor is reported as being over the button, if the
                // cursor got its input from a touch screen then the cursor really isn't
                // over anything. Therefore, we only show the highlighted state if the cursor
                // is a physical on-screen cursor
                else if(cursor.LastInputDevice != InputDevice.TouchScreen)
                {
                    Visual.SetProperty("ButtonCategoryState", "Highlighted");
                }
                else
                {
                    Visual.SetProperty("ButtonCategoryState", "Enabled");
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
                    $"This button was created with a Gum component ({Visual?.ElementSave}) " +
                    "that does not have an instance called 'text'. A 'text' instance must be added to modify the button's Text property.");
            }
        }
#endif

        #endregion
    }
}
