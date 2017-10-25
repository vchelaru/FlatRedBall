using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Gui.Controls
{
    // should this inherit from control? Not sure...
    public interface IToggle : IControl
    {
        bool IsOn { get; set; }

        event EventHandler IsOnChanged;
    }
}
