using System;
using System.Windows;
using System.Windows.Input;
using WpfDataUi.Controls;

namespace OfficialPlugins.Common.Controls
{
    /// <summary>
    /// Adds click-and-drag numeric value scrubbing to a label-like FrameworkElement (e.g. an "X:" TextBlock
    /// or Label sitting next to a numeric TextBox). Reuses WpfDataUi's own
    /// TextBoxDisplayLogic.SnapDraggedValue so the drag feel (1px = 1 unit, snapped to whole numbers)
    /// matches every other numeric field in Glue.
    /// </summary>
    public class LabelDragScrubber
    {
        readonly FrameworkElement label;
        readonly IInputElement positionReference;
        readonly Func<float> getValue;
        readonly Action<float> setValue;

        double? dragStartX;
        double dragUnroundedValue;

        public LabelDragScrubber(FrameworkElement label, IInputElement positionReference, Func<float> getValue, Action<float> setValue)
        {
            this.label = label;
            this.positionReference = positionReference;
            this.getValue = getValue;
            this.setValue = setValue;

            label.Cursor = Cursors.SizeWE;
            label.MouseDown += HandleMouseDown;
            label.MouseMove += HandleMouseMove;
            label.MouseUp += HandleMouseUp;
        }

        void HandleMouseDown(object sender, MouseButtonEventArgs e)
        {
            dragStartX = e.GetPosition(positionReference).X;
            dragUnroundedValue = getValue();

            Mouse.Capture(label);
        }

        void HandleMouseMove(object sender, MouseEventArgs e)
        {
            if (dragStartX == null) return;

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                if (Mouse.Captured == label)
                {
                    Mouse.Capture(null);
                }
                dragStartX = null;
                return;
            }

            var newX = e.GetPosition(positionReference).X;
            var pixelDifference = newX - dragStartX.Value;
            dragStartX = newX;

            if (pixelDifference == 0) return;

            dragUnroundedValue += pixelDifference;
            setValue(SnapDraggedValue(dragUnroundedValue));
        }

        void HandleMouseUp(object sender, MouseButtonEventArgs e)
        {
            dragStartX = null;
            if (Mouse.Captured == label)
            {
                Mouse.Capture(null);
            }
        }

        // Internal (not private) so GlueUnitTests can pin the snapping behavior without driving real WPF mouse events.
        internal static float SnapDraggedValue(double unroundedValue) =>
            (float)TextBoxDisplayLogic.SnapDraggedValue(unroundedValue, rounding: 1);
    }
}
