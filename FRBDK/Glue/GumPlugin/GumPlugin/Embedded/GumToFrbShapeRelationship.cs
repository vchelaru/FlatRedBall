using FlatRedBall.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace GumCoreShared.FlatRedBall.Embedded
{
    public class GumToFrbShapeRelationship
    {
        public AxisAlignedRectangle FrbRect;
        public Gum.Wireframe.GraphicalUiElement GumRect;

        public Circle FrbCircle;
        public Gum.Wireframe.GraphicalUiElement GumCircle;
    }
}
