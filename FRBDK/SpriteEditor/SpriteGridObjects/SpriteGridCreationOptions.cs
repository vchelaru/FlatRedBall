using System;
using System.Collections.Generic;
using System.Text;

namespace SpriteEditor.SpriteGridObjects
{
    public class SpriteGridCreationOptions
    {
        public float GridSpacing;

        public float XLeftBound;
        public float XRightBound;
        public float YBottomBound;
        public float YTopBound;

        public FlatRedBall.ManagedSpriteGroups.SpriteGrid.Plane Plane;

        public float ZCloseBound;
        public float ZFarBound;

    }
}
