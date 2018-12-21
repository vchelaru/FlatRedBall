using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlatRedBall.SpecializedXnaControls.Scrolling
{
    public class ScrollBarControlLogic
    {
        #region Fields

        ScrollBar mVerticalScrollBar;
        ScrollBar mHorizontalScrollBar;

        int displayedAreaWidth = 2048;
        int displayedAreaHeight = 2048;

        float zoomPercentage = 100;

        Panel mPanel;

        #endregion

        #region Properties

        public float ZoomPercentage
        {
            get
            {
                return zoomPercentage;
            }
            set
            {
                zoomPercentage = value;
                UpdateScrollBars();
            }
        }

        public SystemManagers Managers
        {
            get;
            set;
        }

        #endregion

        public ScrollBarControlLogic(Panel panel)
        {
            mPanel = panel;

            mVerticalScrollBar = new VScrollBar();
            mVerticalScrollBar.Dock = DockStyle.Right;
            //mVerticalScrollBar.Scroll += HandleVerticalScroll;
            mVerticalScrollBar.ValueChanged += HandleVerticalScroll;
            panel.Controls.Add(mVerticalScrollBar);

            mHorizontalScrollBar = new HScrollBar();
            mHorizontalScrollBar.Dock = DockStyle.Bottom;

            mHorizontalScrollBar.ValueChanged += HandleHorizontalScroll;
            panel.Controls.Add(mHorizontalScrollBar);

            SetDisplayedArea(2048, 2048);

            mPanel.Resize += HandlePanelResize;

        }
        
        void HandlePanelResize(object sender, EventArgs e)
        {
            UpdateScrollBars();
        }
        
        private void HandleVerticalScroll(object sender, EventArgs e)
        {
            Managers.Renderer.Camera.Y = mVerticalScrollBar.Value;
        }

        private void HandleHorizontalScroll(object sender, EventArgs e)
        {
            Managers.Renderer.Camera.X = mHorizontalScrollBar.Value;

        }

        public void UpdateScrollBarsToCameraPosition()
        {
            mVerticalScrollBar.Value = (int)Managers.Renderer.Camera.Y;
            mHorizontalScrollBar.Value = (int)Managers.Renderer.Camera.X;
        }

        public void SetDisplayedArea(int width, int height)
        {
            displayedAreaWidth = width;
            displayedAreaHeight = height;

            UpdateScrollBars();


        }

        public void UpdateScrollBars()
        {
            if (Managers != null && Managers.Renderer != null)
            {
                // This clamps the scroll bar, but we don't want to adjust the position of the camera when this is called
                // because the user may manually move the camera beyond the bounds:
                var x = Managers.Renderer.Camera.X;
                var horizontalValue = System.Math.Max(x, mHorizontalScrollBar.Minimum);
                horizontalValue = System.Math.Min(horizontalValue, mHorizontalScrollBar.Maximum);
                mHorizontalScrollBar.Value = (int)horizontalValue;

                var y = Managers.Renderer.Camera.Y;
                var verticalValue = System.Math.Max(y, mVerticalScrollBar.Minimum);
                verticalValue = System.Math.Min(verticalValue, mVerticalScrollBar.Maximum);
                mVerticalScrollBar.Value = (int)verticalValue;

                // now preserve the values:
                Managers.Renderer.Camera.X = x;
                Managers.Renderer.Camera.Y = y;
            }


            mVerticalScrollBar.Minimum = -20;
            mVerticalScrollBar.Maximum = displayedAreaHeight + 55;

            mHorizontalScrollBar.Minimum = -20;
            mHorizontalScrollBar.Maximum = displayedAreaWidth + 35;

            float multiplier = 100 / zoomPercentage;

            mHorizontalScrollBar.LargeChange = (int)(mPanel.Width * multiplier);
            mVerticalScrollBar.LargeChange = (int)(mPanel.Height * multiplier);
        }

        
    }
}
