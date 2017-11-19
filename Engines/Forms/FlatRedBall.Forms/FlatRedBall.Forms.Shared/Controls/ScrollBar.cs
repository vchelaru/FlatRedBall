using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using RenderingLibrary;
using FlatRedBall.Forms.GumExtensions;

namespace FlatRedBall.Forms.Controls
{
    public class ScrollBar : FrameworkElement
    {
        #region Fields/Properties

        Button upButton;
        Button downButton;
        Button thumb;

        GraphicalUiElement track;

        double minimum;
        public double Minimum
        {
            get { return minimum; }
            set
            {
                minimum = value;
                UpdateThumbSize();
                UpdateThumbPositionAccordingToValue();
            }
        }

        double maximum;
        public double Maximum
        {
            get { return maximum; }
            set
            {
                maximum = value;
                UpdateThumbSize();
                UpdateThumbPositionAccordingToValue();
            }
        }

        double value;
        public double Value
        {
            get
            {
                return value;
            }
            set
            {
#if DEBUG
                if(double.IsNaN(value))
                {
                    throw new InvalidOperationException("Can't set the ScrollBar Value to NaN");
                }
#endif
                this.value = value;

                this.value = System.Math.Min(this.value, Maximum);
                this.value = System.Math.Max(this.value, Minimum);

                UpdateThumbPositionAccordingToValue();

                ValueChanged?.Invoke(this, null);
            }
        }

        double viewportSize;
        public double ViewportSize
        {
            get { return viewportSize; }
            set
            {
                viewportSize = value;

                UpdateThumbSize();
                UpdateThumbPositionAccordingToValue();

            }
        }
        public double LargeChange { get; set; }
        public double SmallChange { get; set; }

        float cursorGrabOffsetRelativeToThumb = 0;

        float MinThumbPosition => upButton.ActualHeight;
        float MaxThumbPosition => track.GetAbsoluteHeight() - downButton.ActualHeight - thumb.ActualHeight;

        #endregion

        #region Events

        public event EventHandler ValueChanged;

        #endregion

        #region Initialize

        protected override void ReactToVisualChanged()
        {
            upButton = new Button();
            var upButtonVisual = this.Visual.GetGraphicalUiElementByName("UpButtonInstance");
#if DEBUG
            if (upButtonVisual == null)
            {
                throw new Exception("The ScrollBar Gum object must have a button called UpButtonInstance");
            }
#endif
            upButton.Visual = upButtonVisual;
            upButton.Push += (not, used) => this.Value -= this.SmallChange;

            downButton = new Button();
            var downButtonVisual = this.Visual.GetGraphicalUiElementByName("DownButtonInstance");
#if DEBUG
            if(downButtonVisual == null)
            {
                throw new Exception("The ScrollBar Gum object must have a button called DownButtonInstance");
            }
#endif
            downButton.Visual = downButtonVisual;
            downButton.Push += (not, used) => this.Value += this.SmallChange;

            thumb = new Button();
            thumb.Visual = this.Visual.GetGraphicalUiElementByName("ThumbInstance");
            thumb.Push += HandleThumbPush;
            thumb.Visual.RollOver += HandleThumbRollOver;
            // do this before assigning any values like Minimum, Maximum
            var thumbHeight = thumb.ActualHeight;

            track = thumb.Visual.ParentGue;
            track.Push += HandleTrackPush;
            track.RollOver += HandleTrackRollOver;

            // read the height values and infer the Value and ViewportSize based on a 0 - 100

            Minimum = 0;
            Maximum = 100;
            SmallChange = 10;


            var visibleTrackSpace = track.Height - upButton.ActualHeight - downButton.ActualHeight;

            var thumbRatio = thumbHeight / visibleTrackSpace;
            ViewportSize = (Maximum - Minimum) * thumbRatio;
            LargeChange = ViewportSize;

            Value = 0;
            base.ReactToVisualChanged();
        }

        #endregion

        #region Event Handlers

        private void HandleThumbPush(object sender, EventArgs e)
        {
            var topOfThumb = this.thumb.ActualY;
            var cursorScreen = GuiManager.Cursor.ScreenY;

            cursorGrabOffsetRelativeToThumb = cursorScreen - topOfThumb;
        }

        private void HandleThumbRollOver(IWindow obj)
        {
            var cursor = GuiManager.Cursor;

            if (cursor.WindowPushed == thumb.Visual)
            {
                UpdateThumbPositionToCursorDrag(cursor);
            }
        }

        private void HandleTrackRollOver(IWindow window)
        {
            var cursor = GuiManager.Cursor;

            if (cursor.WindowPushed == thumb.Visual)
            {
                UpdateThumbPositionToCursorDrag(cursor);
            }
        }

        private void HandleTrackPush(IWindow window)
        {
            if(GuiManager.Cursor.ScreenY < thumb.ActualY)
            {
                Value -= LargeChange;
            }
            else if(GuiManager.Cursor.ScreenY > thumb.ActualY + thumb.ActualHeight)
            {
                Value += LargeChange;
            }
        }

        #endregion

        private void UpdateThumbPositionAccordingToValue()
        {
            var ratioDown = (value - Minimum) / (Maximum - Minimum);
            if(Maximum <= Minimum)
            {
                ratioDown = 0;
            }

            thumb.Y = Microsoft.Xna.Framework.MathHelper.Lerp(MinThumbPosition, MaxThumbPosition, 
                (float)ratioDown);

        }

        private void UpdateThumbPositionToCursorDrag(Cursor cursor)
        {
            var cursorScreenY = cursor.ScreenY;
            var cursorYRelativeToTrack = cursorScreenY - (track.GetTop() + upButton.ActualHeight);

            thumb.Y = cursorYRelativeToTrack - cursorGrabOffsetRelativeToThumb;

            float range = MaxThumbPosition - MinThumbPosition;

            if(range != 0)
            {
                var ratio = (thumb.Y) / range;

                Value = Minimum + (Maximum - Minimum) * ratio;
            }
            else
            {
                Value = Minimum;
            }
        }
        
        private void UpdateThumbSize()
        {
            if(ViewportSize == 0)
            {
                thumb.Height = 0;
            }
            else
            {
                float trackHeight = (track.GetAbsoluteHeight() - downButton.ActualHeight) - MinThumbPosition;

                var valueRange = (Maximum - Minimum) + ViewportSize;

                var thumbRatio = ViewportSize / valueRange;

                thumb.Height = (float)(trackHeight * thumbRatio);
            }
        }
    }
}
