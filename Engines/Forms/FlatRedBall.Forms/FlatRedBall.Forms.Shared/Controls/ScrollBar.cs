using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;

namespace FlatRedBall.Forms.Controls
{
    public class ScrollBar : FrameworkElement
    {
        #region Fields/Properties

        Button upButton;
        Button downButton;
        Button thumb;

        GraphicalUiElement track;

        public double Minimum { get; set; }
        public double Maximum { get; set; }

        double value;
        public double Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;

                UpdateThumbPosition();

            }

        }

        public double ViewportSize { get; set; }
        public double LargeChange { get; set; }
        public double SmallChange { get; set; }

        #endregion

        #region Events

        public event EventHandler ValueChanged;

        #endregion

        protected override void ReactToVisualChanged()
        {
            upButton = new Button();
            upButton.Visual = this.Visual.GetGraphicalUiElementByName("UpButtonInstance");
            upButton.Click += (not, used) => this.Value -= this.SmallChange;

            downButton = new Button();
            downButton.Visual = this.Visual.GetGraphicalUiElementByName("DownButtonInstance");
            downButton.Click += (not, used) => this.Value += this.SmallChange;

            thumb = new Button();
            thumb.Visual = this.Visual.GetGraphicalUiElementByName("ThumbInstance");

            track = thumb.Visual.ParentGue;
            track.Click += HandleTrackClicked;

            // read the height values and infer the Value and ViewportSize based on a 0 - 100

            Minimum = 0;
            Maximum = 100;
            SmallChange = 10;

            var visibleTrackSpace = track.Height - upButton.ActualHeight - downButton.ActualHeight;
            var thumbHeight = thumb.ActualHeight;

            var thumbRatio = thumbHeight / visibleTrackSpace;
            ViewportSize = (Maximum - Minimum) * thumbRatio;
            LargeChange = ViewportSize;

            base.ReactToVisualChanged();
        }

        private void HandleTrackClicked(IWindow window)
        {

        }

        private void UpdateThumbPosition()
        {
            float minThumbPosition = upButton.ActualHeight + thumb.GetAbsoluteHeight() / 2;
            float maxThumbPosition =
                track.GetAbsoluteHeight() - downButton.ActualHeight - thumb.GetAbsoluteHeight() / 2;

            var ratioDown = (value - Minimum) / (Maximum - Minimum);

            thumb.Y = Microsoft.Xna.Framework.MathHelper.Lerp(minThumbPosition, maxThumbPosition, (float)ratioDown);

        }
        
    }
}
