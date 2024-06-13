using FlatRedBall;
using FlatRedBall.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeefballCodeOnly.Entities;

class Goal : PositionedObject, ICollidable
{
    AxisAlignedRectangle AxisAlignedRectangleInstance;

    public ShapeCollection Collision { get; private set; } = new();

    public HashSet<string> ItemsCollidedAgainst { get; set; } = new();
    public HashSet<string> LastFrameItemsCollidedAgainst { get; set; } = new();
    public HashSet<object> ObjectsCollidedAgainst { get; set; } = new();
    public HashSet<object> LastFrameObjectsCollidedAgainst { get; set; } = new();

    public Goal()
    {
        AxisAlignedRectangleInstance = new AxisAlignedRectangle();
        AxisAlignedRectangleInstance.Width = 32;
        AxisAlignedRectangleInstance.Height = 200;
        AxisAlignedRectangleInstance.AttachTo(this, false);
        ShapeManager.AddAxisAlignedRectangle(AxisAlignedRectangleInstance);
        Collision.AxisAlignedRectangles.Add(AxisAlignedRectangleInstance);

        SpriteManager.AddPositionedObject(this);
    }

    public void Destroy()
    {
        Collision.AxisAlignedRectangles.Remove(AxisAlignedRectangleInstance);
        SpriteManager.RemovePositionedObject(this);
    }
}
