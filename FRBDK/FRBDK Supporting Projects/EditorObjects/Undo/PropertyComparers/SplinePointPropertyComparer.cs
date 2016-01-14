using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Math.Splines;
using Microsoft.Xna.Framework;

namespace EditorObjects.Undo.PropertyComparers
{
    public class SplinePointPropertyComparer : PropertyComparer<SplinePoint>
    {
        public SplinePointPropertyComparer()
            : base()
        {
            AddMemberWatching<Vector3>("Position");
        }
    }
}
