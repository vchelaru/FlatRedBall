using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Math.Geometry;

namespace GlueControl.Runtime
{
    public class DynamicEntity : PositionedObject, IDestroyable, ICollidable
    {
        public string EditModeType { get; set; }

        public ShapeCollection Collision
        {
            get; private set;
        } = new ShapeCollection();

        public void Destroy()
        {
            // needs to loop through children and destroy?
            RemoveSelfFromListsBelongingTo();


            for (int i = Children.Count - 1; i > -1; i--)
            {
                var child = Children[i];

                if (child is IDestroyable destroyable)
                {
                    destroyable.Destroy();
                }
                else if (child is Circle circle)
                {
                    ShapeManager.Remove(circle);
                }
                else if (child is Polygon polygon)
                {
                    ShapeManager.Remove(polygon);
                }
                else if (child is AxisAlignedRectangle rectangle)
                {
                    ShapeManager.Remove(rectangle);
                }
                else if (child is Line line)
                {
                    ShapeManager.Remove(line);
                }
                else if (child is Sprite sprite)
                {
                    SpriteManager.RemoveSprite(sprite);
                }
                else if (child is PositionedObject positionedObject)
                {
                    positionedObject.RemoveSelfFromListsBelongingTo();
                }
            }
        }
    }
}