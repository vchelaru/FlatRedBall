using FlatRedBall.Gui;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms.Controls
{
    public class ListBoxItem : FrameworkElement
    {
        #region Fields/Properties

        bool isSelected;
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if(value != isSelected)
                {
                    isSelected = value;

                    if(isSelected)
                    {
                        Selected?.Invoke(this, null);
                    }
                    UpdateState();
                }
            }
        }

        GraphicalUiElement text;
        protected RenderingLibrary.Graphics.Text coreText;

        internal bool IsHighlightSuppressed { get; set; } = false;

        #endregion

        #region Events

        public event EventHandler Selected;
        public event EventHandler Clicked;

        #endregion

        #region Initialize

        public ListBoxItem() : base() { }

        public ListBoxItem(GraphicalUiElement visual) : base(visual) { }

        protected override void ReactToVisualChanged()
        {
            Visual.Push += this.HandlePush;
            Visual.Click += this.HandleClick;
            Visual.RollOn += this.HandleRollOn;
            Visual.RollOff += this.HandleRollOff;
            Visual.RollOver += this.HandleRollOver;

            // optional
            text = Visual.GetGraphicalUiElementByName("TextInstance");
            coreText = text?.RenderableComponent as RenderingLibrary.Graphics.Text;

            // Just in case it needs to set the state to "enabled"
            UpdateState();


            base.ReactToVisualChanged();
        }


        #endregion

        #region Event Handlers

        bool isHighlighted;
        public bool IsHighlighted
        {
            get => isHighlighted;
            set
            {
                if(isHighlighted != value)
                {
                    isHighlighted = value;
                    UpdateState();
                }
            }
        }

        private void HandleRollOn(IWindow window)
        {
            var cursor = GuiManager.Cursor;

            if (cursor.ScreenXChange != 0 || cursor.ScreenYChange != 0)
            {
                UpdateIsHighlightedFromCursor(cursor);
            }

            UpdateState();
        }


        private void HandleRollOver(IWindow window)
        {
            var cursor = GuiManager.Cursor;

            if (cursor.ScreenXChange != 0 || cursor.ScreenYChange != 0)
            {
                UpdateIsHighlightedFromCursor(cursor);
            }

            UpdateState();
        }

        private void UpdateIsHighlightedFromCursor(Cursor cursor)
        {
            IsHighlighted = cursor.LastInputDevice != InputDevice.TouchScreen &&
                GetIfIsOnThisOrChildVisual(cursor) && IsEnabled;
        }

        private void HandleRollOff(IWindow window)
        {
            IsHighlighted = false;

            UpdateState();
        }

        private void HandlePush(IWindow window)
        {
            if(GuiManager.Cursor.LastInputDevice == InputDevice.Mouse)
            {
                IsSelected = true;

                Clicked?.Invoke(this, null);
            }
        }

        private void HandleClick(IWindow window)
        {
            if(GuiManager.Cursor.LastInputDevice == InputDevice.TouchScreen &&
                GuiManager.Cursor.PrimaryClickNoSlide)
            {
                IsSelected = true;

                Clicked?.Invoke(this, null);
            }
        }

        #endregion

        #region Update To

        public virtual void UpdateToObject(object o)
        {
            if(coreText != null)
            {
                coreText.RawText = o?.ToString();
            }
        }

        protected override void UpdateState()
        {
            var cursor = GuiManager.Cursor;

            const string category = "ListBoxItemCategoryState";

            //if(IsEnabled == false)
            //{
            //    // todo?
            //}
            
            if(IsFocused)
            {
                Visual.SetProperty(category, "Focused");
            }
            else if (IsSelected)
            {
                Visual.SetProperty(category, "Selected");
            }
            else if (IsHighlighted)
            {
                // If the cursor has moved, highlight. This prevents highlighting from
                // happening when the cursor is not moving, and the user is moving the focus
                // with the gamepad. 
                // Vic says - I'm not sure if this is the solution that I like, but let's start with it...
                if(cursor.ScreenXChange != 0 || cursor.ScreenYChange != 0)
                {
                    Visual.SetProperty(category, "Highlighted");
                }
                // otherwise - do nothing?
            }
            else
            {
                Visual.SetProperty(category, "Enabled");
            }
        }

        #endregion

        #region Utilities

        public override string ToString()
        {
            return coreText.RawText;
        }

        #endregion

    }
}
