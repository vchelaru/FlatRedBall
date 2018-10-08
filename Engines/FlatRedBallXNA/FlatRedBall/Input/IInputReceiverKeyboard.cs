using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Input
{
    public interface IInputReceiverKeyboard
    {
        bool IsShiftDown { get; }
        bool IsCtrlDown { get; }
        bool IsAltDown { get; }

        IReadOnlyCollection<Microsoft.Xna.Framework.Input.Keys> KeysTyped { get; }

        string GetStringTyped();
    }
}
