using FlatRedBall;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeefballCodeOnly.Entities;

class Puck : PositionedObject, ICollidable
{
    Circle Circle;

    public ShapeCollection Collision { get; private set; } = new();

    public HashSet<string> ItemsCollidedAgainst { get; set; } = new();
    public HashSet<string> LastFrameItemsCollidedAgainst { get; set; } = new();
    public HashSet<object> ObjectsCollidedAgainst { get; set; } = new();
    public HashSet<object> LastFrameObjectsCollidedAgainst { get; set; } = new();

    public Puck()
    {
        Drag = .4f;

        Circle = new Circle();
        Circle.Radius = 6;
        Circle.Color = Color.Red;
        Circle.AttachTo(this, false);
        ShapeManager.AddCircle(Circle);
        Collision.Circles.Add(Circle);

        SpriteManager.AddPositionedObject(this);
    }

    public void Destroy()
    {
        Collision.Circles.Remove(Circle);
        SpriteManager.RemovePositionedObject(this);
    }
}
