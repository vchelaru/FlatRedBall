using FlatRedBall;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Text;

namespace GumCoreShared.FlatRedBall.Embedded
{
    public interface IGumCollidable : global::FlatRedBall.Math.Geometry.ICollidable
    {
        List<GumToFrbShapeRelationship> GumToFrbShapeRelationships { get; set; }
        PositionedObjectGueWrapper GumWrapper { get; set; }
    }

    public static class GumCollidableExtensions
    {
        public static void InitializeCollision(this IGumCollidable collidable, PositionedObjectGueWrapper gumWrapper)
        {
            collidable.GumWrapper = gumWrapper;
            collidable.Collision.RemoveFromManagers(clearThis: true);


            var gumObject = gumWrapper.GumObject;

            foreach (var gumRect in gumObject.ContainedElements)
            {
                if (gumRect.RenderableComponent is RenderingLibrary.Math.Geometry.LineRectangle)
                {
                    gumRect.Visible = false;

                    var frbRect = new AxisAlignedRectangle();
                    // This is required so that collisions force the enemy to move,
                    // but it does mean we'll have to position this relative to the Gum
                    // object, but translate that to a relative position in FRB coordinates
                    frbRect.AttachTo(collidable as PositionedObject);

                    var relationship = new GumToFrbShapeRelationship();
                    relationship.FrbRect = frbRect;
                    relationship.GumRect = gumRect;
                    frbRect.Name = gumRect.Name + "_Frb";

                    collidable.Collision.Add(frbRect);

                    collidable.GumToFrbShapeRelationships.Add(relationship);
                }
            }
        }

        public static void UpdateFrbRectanglePositionsFromGum(this IGumCollidable collidable)
        {
            if (collidable.GumWrapper != null)
            {

                var parentX = collidable.GumWrapper.GumObject.GetAbsoluteX();
                var parentY = collidable.GumWrapper.GumObject.GetAbsoluteY();

                var gumObjectAsIpso = collidable.GumWrapper.GumObject as IPositionedSizedObject;

                foreach (var relationship in collidable.GumToFrbShapeRelationships)
                {
                    var gumRect = relationship.GumRect;
                    var frbRect = relationship.FrbRect;

                    frbRect.Width = gumRect.GetAbsoluteWidth();
                    frbRect.Height = gumRect.GetAbsoluteHeight();


                    var gumRectX = gumRect.GetAbsoluteX();
                    var gumRectY = gumRect.GetAbsoluteY();

                    var rectLeftOffset = gumRectX - parentX;
                    var rectTopOffset = gumRectY - parentY;

                    var frbOffset = new Vector3(frbRect.Width / 2.0f, -frbRect.Height / 2.0f, 0);

                    var gumRectangleRotation = gumRect.GetAbsoluteRotation();

                    global::FlatRedBall.Math.MathFunctions.RotatePointAroundPoint(Vector3.Zero, ref frbOffset,
                        MathHelper.ToRadians(gumRectangleRotation));

                    frbRect.X = collidable.GumWrapper.FrbObject.X + gumObjectAsIpso.X + rectLeftOffset;
                    frbRect.Y = collidable.GumWrapper.FrbObject.Y - gumObjectAsIpso.Y - rectTopOffset;


                    frbRect.Position += frbOffset;

                    if(frbRect.Parent != null)
                    {
                        frbRect.SetRelativeFromAbsolute();
                    }
                }
            }
        }
    }
}
