using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Graphics.Animation3D;

namespace EditorObjects.Gui
{
    public class Animation3DControllerPropertyGrid : PropertyGrid<Animation3DController>
    {
        public Animation3DControllerPropertyGrid(Cursor cursor)
            : base(cursor)
        {

        }
    }
}
