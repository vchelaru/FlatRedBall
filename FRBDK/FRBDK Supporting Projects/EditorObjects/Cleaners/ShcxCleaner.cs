using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Content.Polygon;
using FlatRedBall.Math.Geometry;

namespace EditorObjects.Cleaners
{
    public class ShcxCleaner
    {
        public static PolygonSave PolygonSave = new PolygonSave();
        public static Point Point = new Point();

        public static Type PolygonSaveType = typeof(PolygonSave);
        public static Type PointType = typeof(Point);
    }
}
