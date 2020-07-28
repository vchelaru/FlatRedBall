using FlatRedBall;
using FlatRedBall.Math.Geometry;
using Gum.Wireframe;
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
        List<PositionedObjectGueWrapper> GumWrappers { get; set; }
    }

    public static class GumCollidableExtensions
    {
        public static void AddCollision(this IGumCollidable collidable, GraphicalUiElement graphicalUiElement, 
            bool offsetForScreenCollision = false, Func<GraphicalUiElement, bool> inclusionRequirement = null)
        {
            var parent = new PositionedObject();
            var gumWrapper = new PositionedObjectGueWrapper(parent, graphicalUiElement);
            if(offsetForScreenCollision)
            {
                parent.X -= global::FlatRedBall.Camera.Main.OrthogonalWidth / 2.0f;
                parent.Y += global::FlatRedBall.Camera.Main.OrthogonalHeight / 2.0f;
            }

            collidable.AddCollision(gumWrapper, inclusionRequirement);
        }

        public static void AddCollision(this IGumCollidable collidable, PositionedObjectGueWrapper gumWrapper, 
            Func<GraphicalUiElement, bool> inclusionRequirement = null)
        {
            if (collidable.GumWrappers == null)
            {
                collidable.GumWrappers = new List<PositionedObjectGueWrapper>();
            }
            collidable.GumWrappers.Add(gumWrapper);
            
            AddCollision(gumWrapper, collidable.Collision, collidable.GumToFrbShapeRelationships, collidable as PositionedObject,
                inclusionRequirement);
        }

        public static void AddCollision(PositionedObjectGueWrapper gumWrapper, ShapeCollection shapeCollection, 
            List<GumToFrbShapeRelationship> gumToFrbShapeRelationships, PositionedObject frbShapeParent,
            Func<GraphicalUiElement, bool> inclusionRequirement = null)
        {

            // why do we clear?
            //collidable.Collision.RemoveFromManagers(clearThis: true);


            var gumObject = gumWrapper.GumObject;

            foreach (var bomRectGue in gumObject.ContainedElements)
            {
                var renderableComponentAsLineRectangle =
                    bomRectGue.RenderableComponent as RenderingLibrary.Math.Geometry.LineRectangle;
                var shouldInclude = renderableComponentAsLineRectangle != null;

                if(shouldInclude && inclusionRequirement != null)
                {
                    shouldInclude = inclusionRequirement(bomRectGue);
                }

                if (shouldInclude)
                {
                    bomRectGue.Visible = false;

                    var frbRect = new AxisAlignedRectangle();
                    // This is required so that collisions force the enemy to move,
                    // but it does mean we'll have to position this relative to the Gum
                    // object, but translate that to a relative position in FRB coordinates
                    frbRect.AttachTo(frbShapeParent);

                    frbRect.Color = renderableComponentAsLineRectangle.Color;

                    var relationship = new GumToFrbShapeRelationship();
                    relationship.FrbRect = frbRect;
                    relationship.GumRect = bomRectGue;
                    frbRect.Name = bomRectGue.Name + "_Frb";

                    shapeCollection.Add(frbRect);

                    gumToFrbShapeRelationships.Add(relationship);
                }
            }
        }

        public static void UpdateFrbRectanglePositionsFromGum(this IGumCollidable collidable)
        {
            if (collidable.GumWrappers?.Count > 0)
            {
                foreach(var gumWrapper in collidable.GumWrappers)
                {
                    var parentX = gumWrapper.GumObject.GetAbsoluteX();
                    var parentY = gumWrapper.GumObject.GetAbsoluteY();

                    if(gumWrapper.FrbObject == null)
                    {
                        throw new InvalidOperationException("Need to set the FRB object for the gum wrapper");
                    }

                    var gumObjectAsIpso = gumWrapper.GumObject as IPositionedSizedObject;

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

                        frbRect.X = gumWrapper.FrbObject.X + gumObjectAsIpso.X + rectLeftOffset;
                        frbRect.Y = gumWrapper.FrbObject.Y - gumObjectAsIpso.Y - rectTopOffset;


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
}
