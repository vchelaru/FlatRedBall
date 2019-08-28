using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Input
{
    public interface IInputDevice
    {
        I2DInput Default2DInput { get; }

        IPressableInput DefaultUpPressable { get; }
        IPressableInput DefaultDownPressable { get; }
        IPressableInput DefaultLeftPressable { get; }
        IPressableInput DefaultRightPressable { get; }

        I1DInput DefaultHorizontalInput { get; }
        I1DInput DefaultVerticalInput { get; }
        IPressableInput DefaultPrimaryActionInput { get; }
        IPressableInput DefaultSecondaryActionInput { get; }
        IPressableInput DefaultConfirmInput { get; }
        IPressableInput DefaultJoinInput { get;  }
        IPressableInput DefaultPauseInput { get;  }
        IPressableInput DefaultBackInput { get;  }
    }
}
