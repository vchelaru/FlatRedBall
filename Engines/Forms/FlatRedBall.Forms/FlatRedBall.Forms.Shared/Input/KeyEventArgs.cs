using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms.Input
{
    public class KeyEventArgs : EventArgs
    {
        public Microsoft.Xna.Framework.Input.Keys Key { get; set; }
    }
}
