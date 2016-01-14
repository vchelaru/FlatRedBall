using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Input
{
    public class Xbox360ButtonReference : IPressableInput
    {
        public FlatRedBall.Input.Xbox360GamePad.Button Button { get; set; }

        public Xbox360GamePad GamePad { get; set; }

        bool IPressableInput.IsDown
        {
            get { return GamePad.ButtonDown(this.Button); }
        }

        bool IPressableInput.WasJustPressed
        {
            get { return GamePad.ButtonPushed(this.Button); }
        }

        bool IPressableInput.WasJustReleased
        {
            get { return GamePad.ButtonReleased(this.Button); }
        }
    }
}
