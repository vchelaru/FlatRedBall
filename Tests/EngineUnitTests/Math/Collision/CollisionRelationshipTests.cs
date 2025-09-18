using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall;
using FlatRedBall.Math.Collision;
using FlatRedBall.Math.Geometry;
using Shouldly;

namespace EngineUnitTests.Math.Collision;

public class CollisionRelationshipTests
{
    [Fact]
    public void RectangleVsShapeCollection_ShouldNotSnag()
    {
        var rectangle = new AxisAlignedRectangle();
        rectangle.Width = 10;
        rectangle.Height = 1;

        Player player = new Player();
        player.Collision.Add(rectangle);
        rectangle.AttachTo(player);

        var rectangleShapeCollection = new ShapeCollection();
        rectangleShapeCollection.AxisAlignedRectangles.Add(rectangle);

        var polygon = new Polygon();
        polygon.Points = new List<Point>
        {
            new Point(-10, 10),
            new Point(0, 0),
            new Point(-10, 0),
            new Point(-10, 10)
        };

        var polygon2 = new Polygon();
        polygon2.Points = new List<Point>
        {
            new Point(0, 0),
            new Point(10, -10),
            new Point(0, -10),
            new Point(0, 0)
        };

        var polygonShapeCollection = new ShapeCollection();
        polygonShapeCollection.Polygons.Add(polygon);
        polygonShapeCollection.Polygons.Add(polygon2);

        player.X = 5 - .1f;
        player.XVelocity = -10;

        var relationship = CollisionManager.Self.CreateRelationship(player, polygonShapeCollection);
        relationship.SetBounceCollision(0, 1, 0);

        relationship.DoCollisions();

        player.XVelocity.ShouldBeLessThan(0, "because the rectangle hit the ShapeCollection at a slope");
    }

    [Fact]
    void RectangleVsPolygonAndVsCompositePolygon_Identical()
    {
        var rectangle = new AxisAlignedRectangle();
        rectangle.Width = 10;
        rectangle.Height = 1;

        Player player = new Player();
        player.Collision.Add(rectangle);
        rectangle.AttachTo(player);

        var rectangleShapeCollection = new ShapeCollection();
        rectangleShapeCollection.AxisAlignedRectangles.Add(rectangle);

        var polygon = new Polygon();
        polygon.Points = new List<Point>
        {
            new Point(-10, 10),
            new Point(0, 0),
            new Point(-10, 0),
            new Point(-10, 10)
        };

        var polygon2 = new Polygon();
        polygon2.Points = new List<Point>
        {
            new Point(0, 0),
            new Point(10, -10),
            new Point(0, -10),
            new Point(0, 0)
        };

        var polygonShapeCollection = new ShapeCollection();
        polygonShapeCollection.Polygons.Add(polygon);
        polygonShapeCollection.Polygons.Add(polygon2);

        player.X = 5 - .1f;
        player.XVelocity = -10;

        var relationship = CollisionManager.Self.CreateRelationship(player, polygonShapeCollection);
        relationship.SetBounceCollision(0, 1, 0);

        relationship.DoCollisions();

        // save resulting player position
        var compositeCollisionX = player.X;
        var compositeCollisionY = player.Y;
        
        rectangle = new AxisAlignedRectangle();
        rectangle.Width = 10;
        rectangle.Height = 1;

        player = new Player();
        player.Collision.Add(rectangle);
        rectangle.AttachTo(player);

        // ramp with the same incline as the shape collection
        var largePolygon = new Polygon();
        largePolygon.Points = new List<Point>
        {
            new Point(-10, 10),
            new Point(10, -10),
            new Point(-10, -10),
            new Point(-10, 10)
        };

        polygonShapeCollection = new ShapeCollection();
        polygonShapeCollection.Polygons.Add(largePolygon);

        player.X = 5 - .1f;
        player.XVelocity = -10;

        relationship = CollisionManager.Self.CreateRelationship(player, polygonShapeCollection);
        relationship.SetBounceCollision(0, 1, 0);

        relationship.DoCollisions();

        player.X.ShouldBe(compositeCollisionX);
        player.Y.ShouldBe(compositeCollisionY);
    }

    class Player : PositionedObject, ICollidable
    {
        public ShapeCollection Collision { get; set; } = new();

        HashSet<string> _itemsCollidedAgainst = new ();
        public HashSet<string> ItemsCollidedAgainst => _itemsCollidedAgainst;

        public HashSet<string> _lastFrameItemsCollidedAgainst;
        public HashSet<string> LastFrameItemsCollidedAgainst => _lastFrameItemsCollidedAgainst;


        HashSet<object> _objectsCollidedAgainst = new();
        public HashSet<object> ObjectsCollidedAgainst => _objectsCollidedAgainst;

        HashSet<object> _lastFrameObjectsCollidedAgainst = new();
        public HashSet<object> LastFrameObjectsCollidedAgainst => _lastFrameObjectsCollidedAgainst;
    }

}
