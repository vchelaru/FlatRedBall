using FlatRedBall.Forms.Controls.Primitives;
using FlatRedBall.Gui;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms.Controls
{
    public class CheckBox : ToggleButton
    {
        #region Fields/Properties

        private GraphicalUiElement textComponent;

        private RenderingLibrary.Graphics.Text coreTextObject;

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

        public CheckBox() : base() { }

        public CheckBox(GraphicalUiElement visual) : base(visual) { }

        protected override void ReactToVisualChanged()
        {
            // text component is optional:
            textComponent = base.Visual.GetGraphicalUiElementByName("TextInstance");

            base.ReactToVisualChanged();

            // In case the check is visible - the checkbox starts in a IsChecked = false state:
            UpdateState();
        }

        #endregion

        #region UpdateTo Methods

        protected override void UpdateState()
        {
            if (Visual == null) //don't try to update the UI when the UI is not set yet, mmmmkay?
                return;

            var cursor = GuiManager.Cursor;

            const string category = "CheckBoxCategoryState";

            if (IsChecked == true)
            {
                if (IsEnabled == false)
                {
                    Visual.SetProperty(category, "DisabledOn");
                }
                else if (IsFocused)
                {
                    if (cursor.WindowPushed == Visual && cursor.PrimaryDown)
                    {
                        Visual.SetProperty(category, "PushedOn");
                    }
                    // Even if the cursor is reported as being over the button, if the
                    // cursor got its input from a touch screen then the cursor really isn't
                    // over anything. Therefore, we only show the highlighted state if the cursor
                    // is a physical on-screen cursor
                    else if (GetIfIsOnThisOrChildVisual(cursor) &&
                        cursor.LastInputDevice != InputDevice.TouchScreen)
                    {
                        Visual.SetProperty(category, "HighlightedFocusedOn");
                    }
                    else
                    {
                        Visual.SetProperty(category, "FocusedOn");
                    }
                }
                else if (GetIfIsOnThisOrChildVisual(cursor))
                {
                    if (cursor.WindowPushed == Visual && cursor.PrimaryDown)
                    {
                        Visual.SetProperty(category, "PushedOn");
                    }
                    else if (cursor.LastInputDevice != InputDevice.TouchScreen)
                    {
                        Visual.SetProperty(category, "HighlightedOn");
                    }
                    else
                    {
                        Visual.SetProperty(category, "EnabledOn");
                    }
                }
                else
                {
                    Visual.SetProperty(category, "EnabledOn");
                }
            }
            else if (IsChecked == false)
            {
                if (IsEnabled == false)
                {
                    Visual.SetProperty(category, "DisabledOff");
                }
                else if (IsFocused)
                {
                    if (cursor.WindowPushed == Visual && cursor.PrimaryDown)
                    {
                        Visual.SetProperty(category, "PushedOff");
                    }
                    // Even if the cursor is reported as being over the button, if the
                    // cursor got its input from a touch screen then the cursor really isn't
                    // over anything. Therefore, we only show the highlighted state if the cursor
                    // is a physical on-screen cursor
                    else if (GetIfIsOnThisOrChildVisual(cursor) &&
                        cursor.LastInputDevice != InputDevice.TouchScreen)
                    {
                        Visual.SetProperty(category, "HighlightedFocusedOff");
                    }
                    else
                    {
                        Visual.SetProperty(category, "FocusedOff");
                    }
                }
                else if (Visual.HasCursorOver(cursor))
                {
                    if (cursor.WindowPushed == Visual && cursor.PrimaryDown)
                    {
                        Visual.SetProperty(category, "PushedOff");
                    }
                    else if (cursor.LastInputDevice != InputDevice.TouchScreen)
                    {
                        Visual.SetProperty(category, "HighlightedOff");
                    }
                    else
                    {
                        Visual.SetProperty(category, "EnabledOff");
                    }
                }
                else
                {
                    Visual.SetProperty(category, "EnabledOff");
                }
            }
            else
            {
                // todo - handle the indeterminate state here
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
                    "This button was created with a Gum component that does not have an instance called 'text'. A 'text' instance must be added to modify the radio button's Text property.");
            }
        }
#endif

        #endregion
    }
}
