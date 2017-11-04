using FlatRedBall.Gui;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms.Controls
{

    public class Button : FrameworkElement
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

        public event EventHandler Click;

        #region Initialize Methods

        protected override void ReactToVisualChanged()
        {
            // text component is optional:
            textComponent = base.Visual.GetGraphicalUiElementByName("TextInstance");

            Visual.Click += this.HandleClick;
            Visual.Push += this.HandlePush;
            Visual.LosePush += this.HandleLosePush;
            Visual.RollOn += this.HandleRollOn;
            Visual.RollOff += this.HandleRollOff;

            base.ReactToVisualChanged();
        }


        #endregion

        #region Event Handler Methods

        private void HandleClick(IWindow window)
        {
            Click?.Invoke(this, null);

            UpdateState();
        }

        public void HandlePush(IWindow window)
        {
            UpdateState();
        }

        public void HandleLosePush(IWindow window)
        {
            UpdateState();
        }

        public void HandleRollOn(IWindow window)
        {
            UpdateState();
        }

        public void HandleRollOff(IWindow window)
        {
            UpdateState();
        }

        #endregion

        #region UpdateTo Methods

        private void UpdateState()
        {
            var cursor = GuiManager.Cursor;
            if(cursor.PrimaryClick)
            {
                int m = 3;

            }
            if (IsEnabled == false)
            {
                Visual.SetProperty("ButtonCategoryState", "Disabled");
            }
            //else if (HasFocus)
            //{
            //    Visual.SetProperty("TextBoxCategoryState", "Selected");
            //}
            else if (Visual.HasCursorOver(cursor))
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
