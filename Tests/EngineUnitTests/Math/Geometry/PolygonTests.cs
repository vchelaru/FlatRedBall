using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Math.Geometry;
using Shouldly;

namespace EngineUnitTests.Math.Geometry;
public class PolygonTests
{
    [Fact]
    public void CollideAgainstMove_ShouldRepositionRectangleCorrectly()
    {
        var rectangle = new AxisAlignedRectangle();

        rectangle.X = 416;
        rectangle.Width = 7;

        rectangle.Y = -545;
        rectangle.Height = 32;

        var polygon = new Polygon();
        polygon.Y = -568;
        polygon.X = 424;

        polygon.Points = new List<Point>
        {
            new Point(-8, 8),
            new Point(8, 0),
            new Point(8, -8),
            new Point(-8, -8),
            new Point(-8, 8),
        };

        rectangle.CollideAgainstMove(polygon, 0, 1);

        rectangle.Y.ShouldBe(-544);
    }
}
