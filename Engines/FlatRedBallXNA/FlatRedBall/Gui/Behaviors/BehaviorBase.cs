using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui.Controls;

namespace FlatRedBall.Gui.Behaviors
{
    public abstract class BehaviorBase
    {
        public abstract void ApplyTo(IControl control);
    }
}
