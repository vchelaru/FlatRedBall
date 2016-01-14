using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Math.Splines;

namespace EditorObjects.Gui
{
    public class SplinePointPropertyGrid : PropertyGrid<SplinePoint>
    {
        public SplinePointPropertyGrid(Cursor cursor)
            : base(cursor)
        {
            ExcludeMember("Acceleration");

        }
    }
}
