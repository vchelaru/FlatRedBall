using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall;
using FlatRedBall.Math.Geometry;
using Shouldly;

namespace EngineUnitTests.Math.Geometry;

public class CircleTests
{
    [Fact]
    public void CollideAgainstBounce_ShouldInvertVelocity()
    {
        var circle = new Circle();
        circle.Radius = 10;
        circle.X = -10 + 1;
        circle.XVelocity = 10;

        var rectangle = new AxisAlignedRectangle();
        rectangle.X = 5;
        rectangle.Width = 10;
        rectangle.Height = 10;

        circle.CollideAgainstBounce(rectangle, 0, 1, 1);

        circle.XVelocity.ShouldBe(-10, "because collision should bounce this");

    }

    [Fact]
    public void CollideAgainstBounce_ShouldNotOverAssignYVelocity_WithAcceleration()
    {
        var circle = new Circle();
        circle.Radius = 10;
        circle.Y = 10f;
        circle.YAcceleration = -1000;


        var rectangle = new AxisAlignedRectangle();
        rectangle.Y = -10;
        rectangle.Width = 20;
        rectangle.Height = 20;

        // let acceleration pull this down naturally by one frame:

        var secondDifference = 1 / 60f;
        var squaredDividedBy2 = secondDifference * secondDifference / 2;

        TimeManager.Initialize();
        TimeManager.Update(new Microsoft.Xna.Framework.GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(secondDifference)));
        circle.TimedActivity(secondDifference, squaredDividedBy2, secondDifference);

        // now the circle should be overlapping the rectangle, so collide:
        circle.CollideAgainstBounce(rectangle, 0, 1, 1);

        circle.Y.ShouldBe(10);
        // this won't be exact, but very close:
        circle.YVelocity.ShouldBeInRange(-.001f, .001f);
    }

    [Fact]
    public void CollideAgainstBounce_Inverted_ShouldNotOverAssignYVelocity_WithAcceleration()
    {
        var circle = new Circle();
        circle.Radius = 10;
        circle.Y = 10f;


        var rectangle = new AxisAlignedRectangle();
        rectangle.Y = -10;
        rectangle.YAcceleration = 1000;
        rectangle.Width = 20;
        rectangle.Height = 20;

        // let acceleration pull this down naturally by one frame:

        var secondDifference = 1 / 60f;
        var squaredDividedBy2 = secondDifference * secondDifference / 2;

        TimeManager.Initialize();
        TimeManager.Update(new Microsoft.Xna.Framework.GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(secondDifference)));
        rectangle.TimedActivity(secondDifference, squaredDividedBy2, secondDifference);

        // now the circle should be overlapping the rectangle, so collide:
        circle.CollideAgainstBounce(rectangle, 1, 0, 1);

        rectangle.Y.ShouldBe(-10);
        // this won't be exact, but very close:
        rectangle.YVelocity.ShouldBeInRange(-.1f, .1f);
    }

    [Fact]
    public void CollideAgainstBounce_ShouldNotOverAssignXVelocity_WithAcceleration()
    {
        var circle = new Circle();
        circle.Radius = 10;
        circle.X = -10f;
        circle.XAcceleration = 1000;


        var rectangle = new AxisAlignedRectangle();
        rectangle.X = 10;
        rectangle.Width = 20;
        rectangle.Height = 20;

        // let acceleration pull this down naturally by one frame:

        var secondDifference = 1 / 60f;
        var squaredDividedBy2 = secondDifference * secondDifference / 2;

        TimeManager.Initialize();
        TimeManager.Update(new Microsoft.Xna.Framework.GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(secondDifference)));
        circle.TimedActivity(secondDifference, squaredDividedBy2, secondDifference);

        // now the circle should be overlapping the rectangle, so collide:
        circle.CollideAgainstBounce(rectangle, 0, 1, 1);

        circle.X.ShouldBe(-10);
        // this won't be exact, but very close:
        circle.XVelocity.ShouldBeInRange(-.001f, .001f);
    }
}
