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
        rectangle.Width = 14;

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


    [Fact]
    public void CollideAgainstMove_ShouldRepositionRectangleCorrectly2()
    {
        var rectangle = new AxisAlignedRectangle();

        rectangle.X = 409;
        rectangle.Y = -544;
        rectangle.Width = 14;

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


    [Fact]
    public void CollideAgainstMove_ShouldRespectMass0()
    {
        var first = Polygon.CreateRectangle(50, 50);
        var second = Polygon.CreateRectangle(50, 50);
        second.X = 90;

        first.CollideAgainstMove(second, 0, 1);

        first.Y.ShouldBe(0);
        second.Y.ShouldBe(0);
        first.X.ShouldBe(-10, "because this has 0 mass, so it should move fully");
        second.X.ShouldBe(90, "because second has non-0 mass, first has 0 should move the first to the left");
    }

    [Fact]
    public void CollideAgainstMove_ShouldRespectOtherMass0()
    {
        var first = Polygon.CreateRectangle(50, 50);
        var second = Polygon.CreateRectangle(50, 50);
        second.X = 90;

        first.CollideAgainstMove(second, 1, 0);

        first.X.ShouldBe(0);
        second.X.ShouldBe(100);
    }

    [Fact]
    public void CollideAgainstMove_ShouldRespectEqualMass()
    {
        var first = Polygon.CreateRectangle(50, 50);
        var second = Polygon.CreateRectangle(50, 50);
        second.X = 90;

        first.CollideAgainstMove(second, 1, 1);

        first.X.ShouldBe(-5);
        second.X.ShouldBe(95);
    }

    [Fact]
    public void CollideAgainstMove_ShouldRespectAsymmetricMass()
    {
        var first = Polygon.CreateRectangle(50, 50);
        var second = Polygon.CreateRectangle(50, 50);
        second.X = 90;

        first.CollideAgainstMove(second, .25f, .75f);

        first.X.ShouldBe(-7.5f);
        second.X.ShouldBe(92.5f);
    }

    private Polygon CreatePolygonRectangle()
    {
        var polygon = Polygon.CreateRectangle(50, 50);

        return polygon;
    }
}
