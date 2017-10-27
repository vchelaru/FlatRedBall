using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Input
{
    public interface IEventKeyboard
    {
        event Action<char> CharacterTyped;
        event Action<Microsoft.Xna.Framework.Input.Keys> KeyPressed;
    }
}
