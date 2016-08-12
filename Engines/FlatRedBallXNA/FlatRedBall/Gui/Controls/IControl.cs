using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Gui.Controls
{
    public interface IControl : IWindow
    {
        void SetState(string stateName);


    }
}
