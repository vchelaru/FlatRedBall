using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Gui
{
    public interface IClickable
    {
        bool HasCursorOver(Cursor cursor);
    }
}
