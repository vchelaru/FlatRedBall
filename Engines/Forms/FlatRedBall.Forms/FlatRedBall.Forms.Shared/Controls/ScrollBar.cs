using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using RenderingLibrary;
using FlatRedBall.Forms.GumExtensions;
using FlatRedBall.Forms.Controls.Primitives;

namespace FlatRedBall.Forms.Controls
{
    public class ScrollBar : RangeBase
    {
        #region Fields/Properties

        Button upButton;
        Button downButton;

        public float MinimumThumbSize { get; set; } = 16;
        
        double viewportSize;
        public double ViewportSize
        {
            get { return viewportSize; }
            set
            {
#if DEBUG
                if(double.IsNaN(value))
                {
                    throw new Exception("ScrollBar ViewportSize cannot be float.NaN");
                }
#endif
                viewportSize = value;

                UpdateThumbSize();
                UpdateThumbPositionAccordingToValue();

            }
        }


        float MinThumbPosition => 0;
        float MaxThumbPosition => Track.GetAbsoluteHeight() - thumb.ActualHeight;


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

            downButton = new Button();
            var downButtonVisual = this.Visual.GetGraphicalUiElementByName("DownButtonInstance");
#if DEBUG
            if(downButtonVisual == null)
            {
                throw new Exception("The ScrollBar Gum object must have a button called DownButtonInstance");
            }
#endif
            downButton.Visual = downButtonVisual;


            base.ReactToVisualChanged();

            var thumbHeight = thumb.ActualHeight;

            upButton.Push += (not, used) => this.Value -= this.SmallChange;
            downButton.Push += (not, used) => this.Value += this.SmallChange;
            Track.Push += HandleTrackPush;
            Visual.SizeChanged += HandleVisualSizeChange;



            var visibleTrackSpace = Track.Height - upButton.ActualHeight - downButton.ActualHeight;

            if(visibleTrackSpace != 0)
            {

                var thumbRatio = thumbHeight / visibleTrackSpace;
                ViewportSize = (Maximum - Minimum) * thumbRatio;
                LargeChange = ViewportSize;

                Value = Minimum;
            }
            else
            {
                ViewportSize = 10;
                LargeChange = 10;
                SmallChange = 2;
            }
        }


        #endregion

        #region Event Handlers
        protected override void HandleThumbPush(object sender, EventArgs e)
        {
            var topOfThumb = this.thumb.ActualY;
            var cursorScreen = GuiManager.Cursor.ScreenY;

            cursorGrabOffsetRelativeToThumb = cursorScreen - topOfThumb;
        }

        private void HandleTrackPush(IWindow window)
        {
            if (GuiManager.Cursor.ScreenY < thumb.ActualY)
            {
                Value -= LargeChange;
            }
            else if (GuiManager.Cursor.ScreenY > thumb.ActualY + thumb.ActualHeight)
            {
                Value += LargeChange;
            }
        }

        private void HandleVisualSizeChange(object sender, EventArgs e)
        {
            UpdateThumbPositionAccordingToValue();
            UpdateThumbSize();
        }

        protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
        {
            base.OnMinimumChanged(oldMinimum, newMinimum);

            UpdateThumbSize();
            UpdateThumbPositionAccordingToValue();
        }

        protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
        {
            base.OnMaximumChanged(oldMaximum, newMaximum);

            UpdateThumbSize();
            UpdateThumbPositionAccordingToValue();
        }

        protected override void OnValueChanged(double oldValue, double newValue)
        {
            base.OnValueChanged(oldValue, newValue);

            UpdateThumbPositionAccordingToValue();
        }

        #endregion

        #region UpdateTo Methods

        private void UpdateThumbPositionAccordingToValue()
        {
            var ratioDown = (Value - Minimum) / (Maximum - Minimum);
            if(Maximum <= Minimum)
            {
                ratioDown = 0;
            }

            thumb.Y = Microsoft.Xna.Framework.MathHelper.Lerp(MinThumbPosition, MaxThumbPosition, 
                (float)ratioDown);

        }

        protected override void UpdateThumbPositionToCursorDrag(Cursor cursor)
        {
            var cursorScreenY = cursor.ScreenY;
            var cursorYRelativeToTrack = cursorScreenY - Track.GetTop();

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
            var desiredHeight = MinimumThumbSize;
            if(ViewportSize != 0)
            {
                float trackHeight = Track.GetAbsoluteHeight();

                var valueRange = (Maximum - Minimum) + ViewportSize;

                var thumbRatio = ViewportSize / valueRange;

                thumb.Height = System.Math.Max(MinimumThumbSize, (float)(trackHeight * thumbRatio));
            }
        }

        #endregion
    }
}
