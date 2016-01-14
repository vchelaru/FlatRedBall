using FlatRedBall.Content.AnimationChain;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlatRedBall.AnimationEditorForms.Controls
{
    public class ScrollBarControlLogic
    {
        #region Fields

        ScrollBar mVerticalScrollBar;
        ScrollBar mHorizontalScrollBar;

        int mImageWidth = 2048;
        int mImageHeight = 2048;

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

            UpdateToImage(2048, 2048);

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

        public void UpdateToImage(int width, int height)
        {
            mImageWidth = width;
            mImageHeight = height;

            UpdateScrollBars();


        }

        public void UpdateScrollBars()
        {
            if (Managers != null && Managers.Renderer != null)
            {
                int horizontalValue = (int)Managers.Renderer.Camera.X;
                horizontalValue = System.Math.Max(horizontalValue, mHorizontalScrollBar.Minimum);
                horizontalValue = System.Math.Min(horizontalValue, mHorizontalScrollBar.Maximum);
                mHorizontalScrollBar.Value = horizontalValue;

                int verticalValue = (int)Managers.Renderer.Camera.Y;
                verticalValue = System.Math.Max(verticalValue, mVerticalScrollBar.Minimum);
                verticalValue = System.Math.Min(verticalValue, mVerticalScrollBar.Maximum);
                mVerticalScrollBar.Value = verticalValue;
            }


            mVerticalScrollBar.Minimum = -20;
            mVerticalScrollBar.Maximum = mImageHeight + 55;

            mHorizontalScrollBar.Minimum = -20;
            mHorizontalScrollBar.Maximum = mImageWidth + 35;

            float multiplier = 100 / zoomPercentage;

            mHorizontalScrollBar.LargeChange = (int)(mPanel.Width * multiplier);
            mVerticalScrollBar.LargeChange = (int)(mPanel.Height * multiplier);
        }

        
    }
}
