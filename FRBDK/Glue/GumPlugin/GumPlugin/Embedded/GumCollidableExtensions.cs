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

            foreach (var gumShapeGue in gumObject.ContainedElements)
            {
                AddCollisionFromGumShape(shapeCollection, gumToFrbShapeRelationships, frbShapeParent, inclusionRequirement, gumShapeGue);
            }

            foreach (var relationship in gumToFrbShapeRelationships)
            {
                relationship.FrbRect?.ForceUpdateDependencies();
                relationship.FrbCircle?.ForceUpdateDependencies();
            }
        }

        public static PositionedObject AddToCollision(this IGumCollidable collidable, GraphicalUiElement gumShape)
        {
            return AddCollisionFromGumShape(collidable.Collision, collidable.GumToFrbShapeRelationships, collidable as PositionedObject, null, gumShape);
        }

        private static PositionedObject AddCollisionFromGumShape(ShapeCollection shapeCollection, List<GumToFrbShapeRelationship> gumToFrbShapeRelationships, PositionedObject frbShapeParent, Func<GraphicalUiElement, bool> inclusionRequirement, GraphicalUiElement gumShapeGue)
        {
            PositionedObject toReturn = null;

            if (gumShapeGue.RenderableComponent is RenderingLibrary.Math.Geometry.LineRectangle renderableComponentAsLineRectangle)
            {
                var shouldInclude = true;

                if (inclusionRequirement != null)
                {
                    shouldInclude = inclusionRequirement(gumShapeGue);
                }

                if (shouldInclude)
                {
                    gumShapeGue.Visible = false;

                    var frbRect = new AxisAlignedRectangle();
                    // This is required so that collisions force the enemy to move,
                    // but it does mean we'll have to position this relative to the Gum
                    // object, but translate that to a relative position in FRB coordinates
                    frbRect.AttachTo(frbShapeParent);

                    frbRect.Color = renderableComponentAsLineRectangle.Color;

                    var relationship = new GumToFrbShapeRelationship();
                    relationship.FrbRect = frbRect;
                    relationship.GumRect = gumShapeGue;
                    frbRect.Name = gumShapeGue.Name + "_Frb";

                    shapeCollection.Add(frbRect);

                    gumToFrbShapeRelationships.Add(relationship);

                    toReturn = frbRect;
                }
            }
            else if (gumShapeGue.RenderableComponent is RenderingLibrary.Math.Geometry.LineCircle renderableComponentAsLineCircle)
            {
                var shouldInclude = true;

                if (inclusionRequirement != null)
                {
                    shouldInclude = inclusionRequirement(gumShapeGue);
                }

                if (shouldInclude)
                {
                    gumShapeGue.Visible = false;

                    var frbCircle = new Circle();
                    // This is required so that collisions force the enemy to move,
                    // but it does mean we'll have to position this relative to the Gum
                    // object, but translate that to a relative position in FRB coordinates
                    frbCircle.AttachTo(frbShapeParent);

                    frbCircle.Color = renderableComponentAsLineCircle.Color;

                    var relationship = new GumToFrbShapeRelationship();
                    relationship.FrbCircle = frbCircle;
                    relationship.GumCircle = gumShapeGue;
                    frbCircle.Name = gumShapeGue.Name + "_Frb";

                    shapeCollection.Add(frbCircle);

                    gumToFrbShapeRelationships.Add(relationship);

                    toReturn = frbCircle;
                }
            }
            return toReturn;
        }

        public static void UpdateShapePositionsFromGum(this IGumCollidable collidable)
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
                        if(relationship.GumRect != null)
                        {
                            UpdateRectFromGum(gumWrapper, parentX, parentY, gumObjectAsIpso, relationship);
                        }
                        else if(relationship.GumCircle != null)
                        {
                            UpdateCircleFromGum(gumWrapper, parentX, parentY, gumObjectAsIpso, relationship);
                        }
                    }
                }
            }
        }

        private static void UpdateRectFromGum(PositionedObjectGueWrapper gumWrapper, float parentX, float parentY, IPositionedSizedObject gumObjectAsIpso, GumToFrbShapeRelationship relationship)
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

            if (frbRect.Parent != null)
            {
                frbRect.SetRelativeFromAbsolute();
            }
        }

        private static void UpdateCircleFromGum(PositionedObjectGueWrapper gumWrapper, float parentX, float parentY, IPositionedSizedObject gumObjectAsIpso, GumToFrbShapeRelationship relationship)
        {
            var gumCircle = relationship.GumCircle;
            var frbCircle = relationship.FrbCircle;

            frbCircle.Radius = gumCircle.GetAbsoluteWidth()/2.0f;
            // make width dominant?

            var gumCircleX = gumCircle.GetAbsoluteX();
            var gumCircleY = gumCircle.GetAbsoluteY();

            var circleLeftOffset = gumCircleX - parentX;
            var circleTopOffset = gumCircleY - parentY;

            var frbOffset = new Vector3(frbCircle.Radius, -frbCircle.Radius, 0);

            var gumCircleRotation = gumCircle.GetAbsoluteRotation();

            global::FlatRedBall.Math.MathFunctions.RotatePointAroundPoint(Vector3.Zero, ref frbOffset,
                MathHelper.ToRadians(gumCircleRotation));

            frbCircle.X = gumWrapper.FrbObject.X + gumObjectAsIpso.X + circleLeftOffset;
            frbCircle.Y = gumWrapper.FrbObject.Y - gumObjectAsIpso.Y - circleTopOffset;

            frbCircle.Position += frbOffset;

            if (frbCircle.Parent != null)
            {
                frbCircle.SetRelativeFromAbsolute();
            }
        }

        // Vic asks = do we still need this? Is it obsolete?
        public static void FillFrom(this ShapeCollection shapeCollection, IEnumerable<GraphicalUiElement> gueList)
        {
            foreach (var gumRect in gueList)
            {
                var frbRect = new AxisAlignedRectangle();

                frbRect.Width = gumRect.GetAbsoluteWidth();
                frbRect.Height = gumRect.GetAbsoluteHeight();


                var gumRectX = gumRect.GetAbsoluteX();
                var gumRectY = gumRect.GetAbsoluteY();

                var rectLeftOffset = gumRectX - 0;
                var rectTopOffset = -gumRectY - 0;

                var frbOffset = new Vector3(frbRect.Width / 2.0f, -frbRect.Height / 2.0f, 0);

                var gumRectangleRotation = gumRect.GetAbsoluteRotation();

                global::FlatRedBall.Math.MathFunctions.RotatePointAroundPoint(Vector3.Zero, ref frbOffset,
                    MathHelper.ToRadians(gumRectangleRotation));

                frbRect.X = rectLeftOffset;
                frbRect.Y = rectTopOffset;

                frbRect.Visible = true;


                frbRect.Position += frbOffset;

                if (frbRect.Parent != null)
                {
                    frbRect.SetRelativeFromAbsolute();
                }

                shapeCollection.AxisAlignedRectangles.Add(frbRect);

                gumRect.RemoveFromManagers();
            }
        }
    }
}
