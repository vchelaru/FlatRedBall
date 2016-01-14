using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;

using FlatRedBall.Graphics;

using FlatRedBall.Gui;

namespace EditorObjects.Gui
{
    public class GraphicsOptionsPropertyGrid : PropertyGrid<GraphicsOptions>
    {
        public GraphicsOptionsPropertyGrid(Cursor cursor)
            : base(cursor)
        {
        }
    }
}
