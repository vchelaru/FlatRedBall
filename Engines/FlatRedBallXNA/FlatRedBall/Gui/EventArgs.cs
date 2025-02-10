using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Gui;

public class RoutedEventArgs : EventArgs
{
    public bool Handled { get; set; }
}

public class InputEventArgs : RoutedEventArgs
{
    /// <summary>
    /// The input device which was responsible for this event, such as the Gamepad.
    /// </summary>
    public object? InputDevice { get; set; }
}
