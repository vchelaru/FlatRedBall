using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Graphics.Animation3D;

namespace EditorObjects.Gui
{
    public class Animation3DInstancePropertyGrid : PropertyGrid<Animation3DInstance>
    {
        public Animation3DInstancePropertyGrid(Cursor cursor)
            : base(cursor)
        {
            ExcludeAllMembers();

			IncludeMember("AnimationLength");

            IncludeMember("Name");
			IncludeMember("AnimationLooping");
			IncludeMember("AnimationLoopPoint");
        }
    }
}
