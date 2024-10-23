using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FlatRedBall.Glue.Themes.Converters;

public static class ScrollThrottleBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(ScrollThrottleBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(ScrollViewer obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(ScrollViewer obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewer scrollViewer
            )
        {
            if ((bool)e.NewValue)
            {
                scrollViewer.PreviewMouseWheel += OnMouseWheel;
            }
            else
            {
                scrollViewer.PreviewMouseWheel -= OnMouseWheel;
            }
        }
    }

    private static void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer && System.Math.Abs(e.Delta) != 120)
        {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + ((double)e.Delta /120));
            e.Handled = true;
        }
    }
}
