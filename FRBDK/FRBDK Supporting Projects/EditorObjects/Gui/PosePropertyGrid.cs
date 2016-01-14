using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;

using FlatRedBall.Gui;

using FlatRedBall.ManagedSpriteGroups;

namespace EditorObjects.Gui
{
    public class PosePropertyGrid : PropertyGrid<Pose>
    {
        public PosePropertyGrid(Cursor cursor)
            : base(cursor)
        {
            ((UpDown)GetUIElementForMember("Time")).MinValue = 0;
        }
    }
}
