using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;

namespace FlatRedBall.Forms.Controls
{
    public class ScrollViewer : FrameworkElement
    {
        #region Fields/Properties

        bool reactToInnerPanelPositionOrSizeChanged = true;

        ScrollBar verticalScrollBar;

        GraphicalUiElement innerPanel;
        public GraphicalUiElement InnerPanel => innerPanel;

        GraphicalUiElement clipContainer;

        #endregion

        #region Initialize

        protected override void ReactToVisualChanged()
        {
            verticalScrollBar = new ScrollBar();
            verticalScrollBar.Visual = Visual.GetGraphicalUiElementByName("VerticalScrollBarInstance");
            verticalScrollBar.ValueChanged += HandleVerticalScrollBarValueChanged;

            innerPanel = Visual.GetGraphicalUiElementByName("InnerPanelInstance");
            innerPanel.SizeChanged += HandleInnerPanelSizeChanged;
            innerPanel.PositionChanged += HandleInnerPanelPositionChanged;
            clipContainer = Visual.GetGraphicalUiElementByName("ClipContainerInstance");

            Visual.MouseWheelScroll += HandleMouseWheelScroll;

            UpdateVerticalScrollBarValues();

            base.ReactToVisualChanged();
        }

        private void HandleMouseWheelScroll(IWindow window, FlatRedBall.Gui.RoutedEventArgs args)
        {
            var valueBefore = verticalScrollBar.Value;

            const float scrollMultiplier = 12;

            // Do we want to use the small change? Or have some separate value that the user can set?
            verticalScrollBar.Value -= GuiManager.Cursor.ZVelocity * verticalScrollBar.SmallChange;

            args.Handled = verticalScrollBar.Value != valueBefore;
        }


        #endregion

        #region Event Handlers

        private void HandleVerticalScrollBarValueChanged(object sender, EventArgs e)
        {
            reactToInnerPanelPositionOrSizeChanged = false;
            innerPanel.Y = -(float)verticalScrollBar.Value;
            reactToInnerPanelPositionOrSizeChanged = true;
        }

        private void HandleInnerPanelSizeChanged(object sender, EventArgs e)
        {
            if(reactToInnerPanelPositionOrSizeChanged)
            {
                UpdateVerticalScrollBarValues();
            }
        }

        private void HandleInnerPanelPositionChanged(object sender, EventArgs e)
        {
            if(reactToInnerPanelPositionOrSizeChanged)
            {
                UpdateVerticalScrollBarValues();
            }
        }
        #endregion

        #region UpdateTo methods

        // Currently this is public because Gum objects don't have events
        // when positions and sizes change. Eventually, we'll have this all
        // handled internally and this can be made private.
        public void UpdateVerticalScrollBarValues()
        {
            verticalScrollBar.Minimum = 0;
            verticalScrollBar.ViewportSize = clipContainer.GetAbsoluteHeight();
            var maxValue = innerPanel.GetAbsoluteHeight() - clipContainer.GetAbsoluteHeight();

            maxValue = System.Math.Max(0, maxValue);

            verticalScrollBar.Maximum = maxValue;

            verticalScrollBar.SmallChange = 10;
            verticalScrollBar.LargeChange = verticalScrollBar.ViewportSize;


        }

        #endregion
    }
}
