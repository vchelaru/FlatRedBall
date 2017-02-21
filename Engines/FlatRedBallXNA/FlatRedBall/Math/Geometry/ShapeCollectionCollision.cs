
using System;

namespace FlatRedBall.Math.Geometry
{	
	internal static class ShapeCollectionCollision
	{
        // The CollideShapeAgainstThis method needs to update dependencies on the shape being passed in to the method.
        // The reason is because deep collision performs an update dependencies so that a a shape will be positioned relative
        // to its parent, but these methods perform partitioning, which may prevent deep collision from happening at all. The partitioning
        // depends on the position of the shape to collide against, so we need that to be up to date before performing partitioning.

		internal static bool CollideShapeAgainstThis(ShapeCollection thisShapeCollection, AxisAlignedRectangle shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse)
		{
            thisShapeCollection.ClearCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndex;

            #endregion
            #region Get the boundStartPosition

            float boundStartPosition = 0;

            switch (axisToUse)
            {
                case Axis.X:
					boundStartPosition = shapeToCollideAgainstThis.X;
                    break;
                case Axis.Y:
					boundStartPosition = shapeToCollideAgainstThis.Y;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                // break;
            }
            #endregion

		    #region vs. AxisAlignedRectangles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedRectanglesRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mAxisAlignedRectangles[i]))
                {
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Circles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mCircles[i]))
                {
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Polygons

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mPolygons[i]))
                {
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Lines

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mLines[i]))
                {
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Capsule2Ds

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mCapsule2Ds[i]))
                {
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion

            return returnValue;
		}
		internal static bool CollideShapeAgainstThis(ShapeCollection thisShapeCollection, Circle shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse)
		{
            thisShapeCollection.ClearCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndex;

            #endregion
            #region Get the boundStartPosition

            float boundStartPosition = 0;

            switch (axisToUse)
            {
                case Axis.X:
					boundStartPosition = shapeToCollideAgainstThis.X;
                    break;
                case Axis.Y:
					boundStartPosition = shapeToCollideAgainstThis.Y;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                // break;
            }
            #endregion

		    #region vs. AxisAlignedRectangles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedRectanglesRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mAxisAlignedRectangles[i]))
                {
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Circles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mCircles[i]))
                {
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Polygons

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mPolygons[i]))
                {
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Lines

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mLines[i]))
                {
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Capsule2Ds

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mCapsule2Ds[i]))
                {
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion

            return returnValue;
		}
		internal static bool CollideShapeAgainstThis(ShapeCollection thisShapeCollection, Polygon shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse)
		{
            thisShapeCollection.ClearCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndex;

            #endregion
            #region Get the boundStartPosition

            float boundStartPosition = 0;

            switch (axisToUse)
            {
                case Axis.X:
					boundStartPosition = shapeToCollideAgainstThis.X;
                    break;
                case Axis.Y:
					boundStartPosition = shapeToCollideAgainstThis.Y;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                // break;
            }
            #endregion

		    #region vs. AxisAlignedRectangles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedRectanglesRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mAxisAlignedRectangles[i]))
                {
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Circles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mCircles[i]))
                {
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Polygons

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mPolygons[i]))
                {
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Lines

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mLines[i]))
                {
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Capsule2Ds

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mCapsule2Ds[i]))
                {
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion

            return returnValue;
		}
		internal static bool CollideShapeAgainstThis(ShapeCollection thisShapeCollection, Line shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse)
		{
            thisShapeCollection.ClearCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndex;

            #endregion
            #region Get the boundStartPosition

            float boundStartPosition = 0;

            switch (axisToUse)
            {
                case Axis.X:
					boundStartPosition = shapeToCollideAgainstThis.X;
                    break;
                case Axis.Y:
					boundStartPosition = shapeToCollideAgainstThis.Y;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                // break;
            }
            #endregion

		    #region vs. AxisAlignedRectangles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedRectanglesRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mAxisAlignedRectangles[i]))
                {
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Circles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mCircles[i]))
                {
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Polygons

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mPolygons[i]))
                {
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Lines

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mLines[i]))
                {
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Capsule2Ds

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mCapsule2Ds[i]))
                {
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion

            return returnValue;
		}
		internal static bool CollideShapeAgainstThis(ShapeCollection thisShapeCollection, Capsule2D shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse)
		{
            thisShapeCollection.ClearCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndex;

            #endregion
            #region Get the boundStartPosition

            float boundStartPosition = 0;

            switch (axisToUse)
            {
                case Axis.X:
					boundStartPosition = shapeToCollideAgainstThis.X;
                    break;
                case Axis.Y:
					boundStartPosition = shapeToCollideAgainstThis.Y;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                // break;
            }
            #endregion

		    #region vs. AxisAlignedRectangles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedRectanglesRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mAxisAlignedRectangles[i]))
                {
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Circles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mCircles[i]))
                {
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Polygons

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mPolygons[i]))
                {
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Lines

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mLines[i]))
                {
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Capsule2Ds

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mCapsule2Ds[i]))
                {
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion

            return returnValue;
		}
		internal static bool CollideShapeAgainstThis(ShapeCollection thisShapeCollection, Sphere shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse)
		{
            thisShapeCollection.ClearCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndex;

            #endregion
            #region Get the boundStartPosition

            float boundStartPosition = 0;

            switch (axisToUse)
            {
                case Axis.X:
					boundStartPosition = shapeToCollideAgainstThis.X;
                    break;
                case Axis.Y:
					boundStartPosition = shapeToCollideAgainstThis.Y;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                // break;
            }
            #endregion

		    #region vs. Spheres

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxSpheresRadius, 
			    thisShapeCollection.mSpheres
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mSpheres[i]))
                {
                    thisShapeCollection.mLastCollisionSpheres.Add(thisShapeCollection.mSpheres[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. AxisAlignedCubes

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedCubesRadius, 
			    thisShapeCollection.mAxisAlignedCubes
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mAxisAlignedCubes[i]))
                {
                    thisShapeCollection.mLastCollisionAxisAlignedCubes.Add(thisShapeCollection.mAxisAlignedCubes[i]);
                    returnValue = true;
                }
            }

            #endregion

            return returnValue;
		}
		internal static bool CollideShapeAgainstThis(ShapeCollection thisShapeCollection, AxisAlignedCube shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse)
		{
            thisShapeCollection.ClearCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndex;

            #endregion
            #region Get the boundStartPosition

            float boundStartPosition = 0;

            switch (axisToUse)
            {
                case Axis.X:
					boundStartPosition = shapeToCollideAgainstThis.X;
                    break;
                case Axis.Y:
					boundStartPosition = shapeToCollideAgainstThis.Y;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                // break;
            }
            #endregion

		    #region vs. Spheres

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxSpheresRadius, 
			    thisShapeCollection.mSpheres
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mSpheres[i]))
                {
                    thisShapeCollection.mLastCollisionSpheres.Add(thisShapeCollection.mSpheres[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. AxisAlignedCubes

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedCubesRadius, 
			    thisShapeCollection.mAxisAlignedCubes
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mAxisAlignedCubes[i]))
                {
                    thisShapeCollection.mLastCollisionAxisAlignedCubes.Add(thisShapeCollection.mAxisAlignedCubes[i]);
                    returnValue = true;
                }
            }

            #endregion

            return returnValue;
		}

		internal static bool CollideShapeAgainstThisMove(ShapeCollection thisShapeCollection, AxisAlignedRectangle shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass)
		{
            thisShapeCollection.ClearCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndex;

            #endregion
            #region Get the boundStartPosition

            float boundStartPosition = 0;

            switch (axisToUse)
            {
                case Axis.X:
					boundStartPosition = shapeToCollideAgainstThis.X;
                    break;
                case Axis.Y:
					boundStartPosition = shapeToCollideAgainstThis.Y;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                // break;
            }
            #endregion

		    #region vs. AxisAlignedRectangles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedRectanglesRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mAxisAlignedRectangles[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Circles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mCircles[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Polygons

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mPolygons[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Lines

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mLines[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Capsule2Ds

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mCapsule2Ds[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion

            return returnValue;
		}
		internal static bool CollideShapeAgainstThisMove(ShapeCollection thisShapeCollection, Circle shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass)
		{
            thisShapeCollection.ClearCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndex;

            #endregion
            #region Get the boundStartPosition

            float boundStartPosition = 0;

            switch (axisToUse)
            {
                case Axis.X:
					boundStartPosition = shapeToCollideAgainstThis.X;
                    break;
                case Axis.Y:
					boundStartPosition = shapeToCollideAgainstThis.Y;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                // break;
            }
            #endregion

		    #region vs. AxisAlignedRectangles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedRectanglesRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mAxisAlignedRectangles[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Circles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mCircles[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Polygons

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mPolygons[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Lines

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mLines[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Capsule2Ds

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mCapsule2Ds[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion

            return returnValue;
		}
		internal static bool CollideShapeAgainstThisMove(ShapeCollection thisShapeCollection, Polygon shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass)
		{
            thisShapeCollection.ClearCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndex;

            #endregion
            #region Get the boundStartPosition

            float boundStartPosition = 0;

            switch (axisToUse)
            {
                case Axis.X:
					boundStartPosition = shapeToCollideAgainstThis.X;
                    break;
                case Axis.Y:
					boundStartPosition = shapeToCollideAgainstThis.Y;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                // break;
            }
            #endregion

		    #region vs. AxisAlignedRectangles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedRectanglesRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mAxisAlignedRectangles[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Circles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mCircles[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Polygons

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mPolygons[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Lines

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mLines[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Capsule2Ds

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mCapsule2Ds[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion

            return returnValue;
		}
		internal static bool CollideShapeAgainstThisMove(ShapeCollection thisShapeCollection, Line shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass)
		{
            thisShapeCollection.ClearCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndex;

            #endregion
            #region Get the boundStartPosition

            float boundStartPosition = 0;

            switch (axisToUse)
            {
                case Axis.X:
					boundStartPosition = shapeToCollideAgainstThis.X;
                    break;
                case Axis.Y:
					boundStartPosition = shapeToCollideAgainstThis.Y;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                // break;
            }
            #endregion

		    #region vs. AxisAlignedRectangles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedRectanglesRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mAxisAlignedRectangles[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Circles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mCircles[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Polygons

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mPolygons[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Lines

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mLines[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Capsule2Ds

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mCapsule2Ds[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion

            return returnValue;
		}
		internal static bool CollideShapeAgainstThisMove(ShapeCollection thisShapeCollection, Capsule2D shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass)
		{
            thisShapeCollection.ClearCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndex;

            #endregion
            #region Get the boundStartPosition

            float boundStartPosition = 0;

            switch (axisToUse)
            {
                case Axis.X:
					boundStartPosition = shapeToCollideAgainstThis.X;
                    break;
                case Axis.Y:
					boundStartPosition = shapeToCollideAgainstThis.Y;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                // break;
            }
            #endregion

		    #region vs. AxisAlignedRectangles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedRectanglesRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mAxisAlignedRectangles[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Circles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mCircles[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Polygons

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mPolygons[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Lines

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mLines[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Capsule2Ds

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mCapsule2Ds[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion

            return returnValue;
		}
		internal static bool CollideShapeAgainstThisMove(ShapeCollection thisShapeCollection, Sphere shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass)
		{
            thisShapeCollection.ClearCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndex;

            #endregion
            #region Get the boundStartPosition

            float boundStartPosition = 0;

            switch (axisToUse)
            {
                case Axis.X:
					boundStartPosition = shapeToCollideAgainstThis.X;
                    break;
                case Axis.Y:
					boundStartPosition = shapeToCollideAgainstThis.Y;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                // break;
            }
            #endregion

		    #region vs. Spheres

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxSpheresRadius, 
			    thisShapeCollection.mSpheres
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mSpheres[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionSpheres.Add(thisShapeCollection.mSpheres[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. AxisAlignedCubes

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedCubesRadius, 
			    thisShapeCollection.mAxisAlignedCubes
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mAxisAlignedCubes[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedCubes.Add(thisShapeCollection.mAxisAlignedCubes[i]);
                    returnValue = true;
                }
            }

            #endregion

            return returnValue;
		}
		internal static bool CollideShapeAgainstThisMove(ShapeCollection thisShapeCollection, AxisAlignedCube shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass)
		{
            thisShapeCollection.ClearCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndex;

            #endregion
            #region Get the boundStartPosition

            float boundStartPosition = 0;

            switch (axisToUse)
            {
                case Axis.X:
					boundStartPosition = shapeToCollideAgainstThis.X;
                    break;
                case Axis.Y:
					boundStartPosition = shapeToCollideAgainstThis.Y;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                // break;
            }
            #endregion

		    #region vs. Spheres

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxSpheresRadius, 
			    thisShapeCollection.mSpheres
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mSpheres[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionSpheres.Add(thisShapeCollection.mSpheres[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. AxisAlignedCubes

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedCubesRadius, 
			    thisShapeCollection.mAxisAlignedCubes
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mAxisAlignedCubes[i], shapeMass, collectionMass))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedCubes.Add(thisShapeCollection.mAxisAlignedCubes[i]);
                    returnValue = true;
                }
            }

            #endregion

            return returnValue;
		}

		internal static bool CollideShapeAgainstThisBounce(ShapeCollection thisShapeCollection, AxisAlignedRectangle shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass, float elasticity)
		{
            thisShapeCollection.ClearCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndex;

            #endregion
            #region Get the boundStartPosition

            float boundStartPosition = 0;

            switch (axisToUse)
            {
                case Axis.X:
					boundStartPosition = shapeToCollideAgainstThis.X;
                    break;
                case Axis.Y:
					boundStartPosition = shapeToCollideAgainstThis.Y;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                // break;
            }
            #endregion

		    #region vs. AxisAlignedRectangles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedRectanglesRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mAxisAlignedRectangles[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Circles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mCircles[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Polygons

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mPolygons[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Lines

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mLines[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Capsule2Ds

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mCapsule2Ds[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion

            return returnValue;
		}
		internal static bool CollideShapeAgainstThisBounce(ShapeCollection thisShapeCollection, Circle shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass, float elasticity)
		{
            thisShapeCollection.ClearCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndex;

            #endregion
            #region Get the boundStartPosition

            float boundStartPosition = 0;

            switch (axisToUse)
            {
                case Axis.X:
					boundStartPosition = shapeToCollideAgainstThis.X;
                    break;
                case Axis.Y:
					boundStartPosition = shapeToCollideAgainstThis.Y;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                // break;
            }
            #endregion

		    #region vs. AxisAlignedRectangles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedRectanglesRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mAxisAlignedRectangles[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Circles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mCircles[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Polygons

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mPolygons[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Lines

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mLines[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Capsule2Ds

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mCapsule2Ds[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion

            return returnValue;
		}
		internal static bool CollideShapeAgainstThisBounce(ShapeCollection thisShapeCollection, Polygon shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass, float elasticity)
		{
            thisShapeCollection.ClearCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndex;

            #endregion
            #region Get the boundStartPosition

            float boundStartPosition = 0;

            switch (axisToUse)
            {
                case Axis.X:
					boundStartPosition = shapeToCollideAgainstThis.X;
                    break;
                case Axis.Y:
					boundStartPosition = shapeToCollideAgainstThis.Y;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                // break;
            }
            #endregion

		    #region vs. AxisAlignedRectangles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedRectanglesRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mAxisAlignedRectangles[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Circles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mCircles[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Polygons

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mPolygons[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Lines

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mLines[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Capsule2Ds

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mCapsule2Ds[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion

            return returnValue;
		}
		internal static bool CollideShapeAgainstThisBounce(ShapeCollection thisShapeCollection, Line shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass, float elasticity)
		{
            thisShapeCollection.ClearCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndex;

            #endregion
            #region Get the boundStartPosition

            float boundStartPosition = 0;

            switch (axisToUse)
            {
                case Axis.X:
					boundStartPosition = shapeToCollideAgainstThis.X;
                    break;
                case Axis.Y:
					boundStartPosition = shapeToCollideAgainstThis.Y;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                // break;
            }
            #endregion

		    #region vs. AxisAlignedRectangles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedRectanglesRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mAxisAlignedRectangles[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Circles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mCircles[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Polygons

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mPolygons[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Lines

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mLines[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Capsule2Ds

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mCapsule2Ds[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion

            return returnValue;
		}
		internal static bool CollideShapeAgainstThisBounce(ShapeCollection thisShapeCollection, Capsule2D shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass, float elasticity)
		{
            thisShapeCollection.ClearCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndex;

            #endregion
            #region Get the boundStartPosition

            float boundStartPosition = 0;

            switch (axisToUse)
            {
                case Axis.X:
					boundStartPosition = shapeToCollideAgainstThis.X;
                    break;
                case Axis.Y:
					boundStartPosition = shapeToCollideAgainstThis.Y;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                // break;
            }
            #endregion

		    #region vs. AxisAlignedRectangles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedRectanglesRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mAxisAlignedRectangles[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Circles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mCircles[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Polygons

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mPolygons[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Lines

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mLines[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. Capsule2Ds

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mCapsule2Ds[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion

            return returnValue;
		}
		internal static bool CollideShapeAgainstThisBounce(ShapeCollection thisShapeCollection, Sphere shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass, float elasticity)
		{
            thisShapeCollection.ClearCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndex;

            #endregion
            #region Get the boundStartPosition

            float boundStartPosition = 0;

            switch (axisToUse)
            {
                case Axis.X:
					boundStartPosition = shapeToCollideAgainstThis.X;
                    break;
                case Axis.Y:
					boundStartPosition = shapeToCollideAgainstThis.Y;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                // break;
            }
            #endregion

		    #region vs. Spheres

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxSpheresRadius, 
			    thisShapeCollection.mSpheres
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mSpheres[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionSpheres.Add(thisShapeCollection.mSpheres[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. AxisAlignedCubes

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedCubesRadius, 
			    thisShapeCollection.mAxisAlignedCubes
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mAxisAlignedCubes[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedCubes.Add(thisShapeCollection.mAxisAlignedCubes[i]);
                    returnValue = true;
                }
            }

            #endregion

            return returnValue;
		}
		internal static bool CollideShapeAgainstThisBounce(ShapeCollection thisShapeCollection, AxisAlignedCube shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass, float elasticity)
		{
            thisShapeCollection.ClearCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndex;

            #endregion
            #region Get the boundStartPosition

            float boundStartPosition = 0;

            switch (axisToUse)
            {
                case Axis.X:
					boundStartPosition = shapeToCollideAgainstThis.X;
                    break;
                case Axis.Y:
					boundStartPosition = shapeToCollideAgainstThis.Y;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                // break;
            }
            #endregion

		    #region vs. Spheres

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxSpheresRadius, 
			    thisShapeCollection.mSpheres
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mSpheres[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionSpheres.Add(thisShapeCollection.mSpheres[i]);
                    returnValue = true;
                }
            }

            #endregion

		    #region vs. AxisAlignedCubes

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedCubesRadius, 
			    thisShapeCollection.mAxisAlignedCubes
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mAxisAlignedCubes[i], shapeMass, collectionMass, elasticity))
                {
                    thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedCubes.Add(thisShapeCollection.mAxisAlignedCubes[i]);
                    returnValue = true;
                }
            }

            #endregion

            return returnValue;
		}

		internal static bool CollideShapeAgainstThis(ShapeCollection thisShapeCollection, ShapeCollection shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse)
		{
			thisShapeCollection.mSuppressLastCollisionClear = true;
            bool returnValue = false;


            for (int i = 0; i < shapeToCollideAgainstThis.AxisAlignedRectangles.Count; i++)
			{
                AxisAlignedRectangle shape = shapeToCollideAgainstThis.AxisAlignedRectangles[i];

                returnValue |= ShapeCollectionCollision.CollideShapeAgainstThis(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse);
            }

            for (int i = 0; i < shapeToCollideAgainstThis.Circles.Count; i++)
			{
                Circle shape = shapeToCollideAgainstThis.Circles[i];

                returnValue |= ShapeCollectionCollision.CollideShapeAgainstThis(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse);
            }

            for (int i = 0; i < shapeToCollideAgainstThis.Polygons.Count; i++)
			{
                Polygon shape = shapeToCollideAgainstThis.Polygons[i];

                returnValue |= ShapeCollectionCollision.CollideShapeAgainstThis(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse);
            }

            for (int i = 0; i < shapeToCollideAgainstThis.Lines.Count; i++)
			{
                Line shape = shapeToCollideAgainstThis.Lines[i];

                returnValue |= ShapeCollectionCollision.CollideShapeAgainstThis(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse);
            }

            for (int i = 0; i < shapeToCollideAgainstThis.Capsule2Ds.Count; i++)
			{
                Capsule2D shape = shapeToCollideAgainstThis.Capsule2Ds[i];

                returnValue |= ShapeCollectionCollision.CollideShapeAgainstThis(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse);
            }

            for (int i = 0; i < shapeToCollideAgainstThis.Spheres.Count; i++)
			{
                Sphere shape = shapeToCollideAgainstThis.Spheres[i];

                returnValue |= ShapeCollectionCollision.CollideShapeAgainstThis(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse);
            }

            for (int i = 0; i < shapeToCollideAgainstThis.AxisAlignedCubes.Count; i++)
			{
                AxisAlignedCube shape = shapeToCollideAgainstThis.AxisAlignedCubes[i];

                returnValue |= ShapeCollectionCollision.CollideShapeAgainstThis(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse);
            }

			thisShapeCollection.mSuppressLastCollisionClear = false;
            return returnValue;
		}
		internal static bool CollideShapeAgainstThisMove(ShapeCollection thisShapeCollection, ShapeCollection shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass)
		{
			thisShapeCollection.mSuppressLastCollisionClear = true;
            bool returnValue = false;


            for (int i = 0; i < shapeToCollideAgainstThis.AxisAlignedRectangles.Count; i++)
			{
                AxisAlignedRectangle shape = shapeToCollideAgainstThis.AxisAlignedRectangles[i];

                returnValue |= ShapeCollectionCollision.CollideShapeAgainstThisMove(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse, shapeMass, collectionMass);
            }

            for (int i = 0; i < shapeToCollideAgainstThis.Circles.Count; i++)
			{
                Circle shape = shapeToCollideAgainstThis.Circles[i];

                returnValue |= ShapeCollectionCollision.CollideShapeAgainstThisMove(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse, shapeMass, collectionMass);
            }

            for (int i = 0; i < shapeToCollideAgainstThis.Polygons.Count; i++)
			{
                Polygon shape = shapeToCollideAgainstThis.Polygons[i];

                returnValue |= ShapeCollectionCollision.CollideShapeAgainstThisMove(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse, shapeMass, collectionMass);
            }

            for (int i = 0; i < shapeToCollideAgainstThis.Lines.Count; i++)
			{
                Line shape = shapeToCollideAgainstThis.Lines[i];

                returnValue |= ShapeCollectionCollision.CollideShapeAgainstThisMove(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse, shapeMass, collectionMass);
            }

            for (int i = 0; i < shapeToCollideAgainstThis.Capsule2Ds.Count; i++)
			{
                Capsule2D shape = shapeToCollideAgainstThis.Capsule2Ds[i];

                returnValue |= ShapeCollectionCollision.CollideShapeAgainstThisMove(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse, shapeMass, collectionMass);
            }

            for (int i = 0; i < shapeToCollideAgainstThis.Spheres.Count; i++)
			{
                Sphere shape = shapeToCollideAgainstThis.Spheres[i];

                returnValue |= ShapeCollectionCollision.CollideShapeAgainstThisMove(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse, shapeMass, collectionMass);
            }

            for (int i = 0; i < shapeToCollideAgainstThis.AxisAlignedCubes.Count; i++)
			{
                AxisAlignedCube shape = shapeToCollideAgainstThis.AxisAlignedCubes[i];

                returnValue |= ShapeCollectionCollision.CollideShapeAgainstThisMove(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse, shapeMass, collectionMass);
            }

			thisShapeCollection.mSuppressLastCollisionClear = false;
            return returnValue;
		}
		internal static bool CollideShapeAgainstThisBounce(ShapeCollection thisShapeCollection, ShapeCollection shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass, float elasticity)
		{
			thisShapeCollection.mSuppressLastCollisionClear = true;
            bool returnValue = false;


            for (int i = 0; i < shapeToCollideAgainstThis.AxisAlignedRectangles.Count; i++)
			{
                AxisAlignedRectangle shape = shapeToCollideAgainstThis.AxisAlignedRectangles[i];

                returnValue |= ShapeCollectionCollision.CollideShapeAgainstThisBounce(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse, shapeMass, collectionMass, elasticity);
            }

            for (int i = 0; i < shapeToCollideAgainstThis.Circles.Count; i++)
			{
                Circle shape = shapeToCollideAgainstThis.Circles[i];

                returnValue |= ShapeCollectionCollision.CollideShapeAgainstThisBounce(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse, shapeMass, collectionMass, elasticity);
            }

            for (int i = 0; i < shapeToCollideAgainstThis.Polygons.Count; i++)
			{
                Polygon shape = shapeToCollideAgainstThis.Polygons[i];

                returnValue |= ShapeCollectionCollision.CollideShapeAgainstThisBounce(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse, shapeMass, collectionMass, elasticity);
            }

            for (int i = 0; i < shapeToCollideAgainstThis.Lines.Count; i++)
			{
                Line shape = shapeToCollideAgainstThis.Lines[i];

                returnValue |= ShapeCollectionCollision.CollideShapeAgainstThisBounce(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse, shapeMass, collectionMass, elasticity);
            }

            for (int i = 0; i < shapeToCollideAgainstThis.Capsule2Ds.Count; i++)
			{
                Capsule2D shape = shapeToCollideAgainstThis.Capsule2Ds[i];

                returnValue |= ShapeCollectionCollision.CollideShapeAgainstThisBounce(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse, shapeMass, collectionMass, elasticity);
            }

            for (int i = 0; i < shapeToCollideAgainstThis.Spheres.Count; i++)
			{
                Sphere shape = shapeToCollideAgainstThis.Spheres[i];

                returnValue |= ShapeCollectionCollision.CollideShapeAgainstThisBounce(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse, shapeMass, collectionMass, elasticity);
            }

            for (int i = 0; i < shapeToCollideAgainstThis.AxisAlignedCubes.Count; i++)
			{
                AxisAlignedCube shape = shapeToCollideAgainstThis.AxisAlignedCubes[i];

                returnValue |= ShapeCollectionCollision.CollideShapeAgainstThisBounce(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse, shapeMass, collectionMass, elasticity);
            }

			thisShapeCollection.mSuppressLastCollisionClear = false;
            return returnValue;
		}

		private static void GetStartAndEnd<T>(bool considerAxisBasedPartitioning, Axis axisToUse,
			out int startIndex, out int endIndex, float boundStartPosition, float individualShapeRadius, float listMaxRadius, PositionedObjectList<T> list) where T : PositionedObject
		{
			if (considerAxisBasedPartitioning)
			{
                if (list.Count > 0)
                {
                    float combinedRadii = individualShapeRadius + listMaxRadius;

                    startIndex = list.GetFirstAfter(
                        boundStartPosition - combinedRadii,
                        axisToUse,
                        0,
                        list.Count);

                    endIndex = list.GetFirstAfter(
                        boundStartPosition + combinedRadii,
                        axisToUse,
                        0,
                        list.Count);
                }
                else
                {
                    startIndex = endIndex = 0;
                }
			}
			else
			{
				startIndex = 0;
				endIndex = list.Count;
			}
		}

/*
// Copy this code into ShapeCollection
		#region Generated collision calling code
		
		public bool CollideAgainst(AxisAlignedRectangle axisAlignedRectangle)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThis(this, axisAlignedRectangle, false, Axis.X);

		}

		public bool CollideAgainst(AxisAlignedRectangle axisAlignedRectangle, bool considerPartitioning, Axis axisToUse)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThis(this, axisAlignedRectangle, considerPartitioning, axisToUse);

		}

		
		public bool CollideAgainst(Circle circle)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThis(this, circle, false, Axis.X);

		}

		public bool CollideAgainst(Circle circle, bool considerPartitioning, Axis axisToUse)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThis(this, circle, considerPartitioning, axisToUse);

		}

		
		public bool CollideAgainst(Polygon polygon)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThis(this, polygon, false, Axis.X);

		}

		public bool CollideAgainst(Polygon polygon, bool considerPartitioning, Axis axisToUse)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThis(this, polygon, considerPartitioning, axisToUse);

		}

		
		public bool CollideAgainst(Line line)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThis(this, line, false, Axis.X);

		}

		public bool CollideAgainst(Line line, bool considerPartitioning, Axis axisToUse)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThis(this, line, considerPartitioning, axisToUse);

		}

		
		public bool CollideAgainst(Capsule2D capsule2D)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThis(this, capsule2D, false, Axis.X);

		}

		public bool CollideAgainst(Capsule2D capsule2D, bool considerPartitioning, Axis axisToUse)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThis(this, capsule2D, considerPartitioning, axisToUse);

		}

		
		public bool CollideAgainst(Sphere sphere)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThis(this, sphere, false, Axis.X);

		}

		public bool CollideAgainst(Sphere sphere, bool considerPartitioning, Axis axisToUse)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThis(this, sphere, considerPartitioning, axisToUse);

		}

		
		public bool CollideAgainst(AxisAlignedCube axisAlignedCube)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThis(this, axisAlignedCube, false, Axis.X);

		}

		public bool CollideAgainst(AxisAlignedCube axisAlignedCube, bool considerPartitioning, Axis axisToUse)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThis(this, axisAlignedCube, considerPartitioning, axisToUse);

		}

		

		public bool CollideAgainstMove(AxisAlignedRectangle axisAlignedRectangle, float thisMass, float otherMass)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, axisAlignedRectangle, false, Axis.X, otherMass, thisMass);

		}

		public bool CollideAgainstMove(AxisAlignedRectangle axisAlignedRectangle, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, axisAlignedRectangle, considerPartitioning, axisToUse, otherMass, thisMass);

		}

		

		public bool CollideAgainstMove(Circle circle, float thisMass, float otherMass)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, circle, false, Axis.X, otherMass, thisMass);

		}

		public bool CollideAgainstMove(Circle circle, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, circle, considerPartitioning, axisToUse, otherMass, thisMass);

		}

		

		public bool CollideAgainstMove(Polygon polygon, float thisMass, float otherMass)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, polygon, false, Axis.X, otherMass, thisMass);

		}

		public bool CollideAgainstMove(Polygon polygon, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, polygon, considerPartitioning, axisToUse, otherMass, thisMass);

		}

		

		public bool CollideAgainstMove(Line line, float thisMass, float otherMass)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, line, false, Axis.X, otherMass, thisMass);

		}

		public bool CollideAgainstMove(Line line, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, line, considerPartitioning, axisToUse, otherMass, thisMass);

		}

		

		public bool CollideAgainstMove(Capsule2D capsule2D, float thisMass, float otherMass)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, capsule2D, false, Axis.X, otherMass, thisMass);

		}

		public bool CollideAgainstMove(Capsule2D capsule2D, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, capsule2D, considerPartitioning, axisToUse, otherMass, thisMass);

		}

		

		public bool CollideAgainstMove(Sphere sphere, float thisMass, float otherMass)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, sphere, false, Axis.X, otherMass, thisMass);

		}

		public bool CollideAgainstMove(Sphere sphere, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, sphere, considerPartitioning, axisToUse, otherMass, thisMass);

		}

		

		public bool CollideAgainstMove(AxisAlignedCube axisAlignedCube, float thisMass, float otherMass)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, axisAlignedCube, false, Axis.X, otherMass, thisMass);

		}

		public bool CollideAgainstMove(AxisAlignedCube axisAlignedCube, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, axisAlignedCube, considerPartitioning, axisToUse, otherMass, thisMass);

		}

		
		public bool CollideAgainstBounce(AxisAlignedRectangle axisAlignedRectangle, float thisMass, float otherMass, float elasticity)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, axisAlignedRectangle, false, Axis.X, otherMass, thisMass, elasticity);

		}

		public bool CollideAgainstBounce(AxisAlignedRectangle axisAlignedRectangle, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass, float elasticity)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, axisAlignedRectangle, considerPartitioning, axisToUse, otherMass, thisMass, elasticity);

		}

		
		public bool CollideAgainstBounce(Circle circle, float thisMass, float otherMass, float elasticity)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, circle, false, Axis.X, otherMass, thisMass, elasticity);

		}

		public bool CollideAgainstBounce(Circle circle, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass, float elasticity)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, circle, considerPartitioning, axisToUse, otherMass, thisMass, elasticity);

		}

		
		public bool CollideAgainstBounce(Polygon polygon, float thisMass, float otherMass, float elasticity)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, polygon, false, Axis.X, otherMass, thisMass, elasticity);

		}

		public bool CollideAgainstBounce(Polygon polygon, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass, float elasticity)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, polygon, considerPartitioning, axisToUse, otherMass, thisMass, elasticity);

		}

		
		public bool CollideAgainstBounce(Line line, float thisMass, float otherMass, float elasticity)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, line, false, Axis.X, otherMass, thisMass, elasticity);

		}

		public bool CollideAgainstBounce(Line line, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass, float elasticity)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, line, considerPartitioning, axisToUse, otherMass, thisMass, elasticity);

		}

		
		public bool CollideAgainstBounce(Capsule2D capsule2D, float thisMass, float otherMass, float elasticity)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, capsule2D, false, Axis.X, otherMass, thisMass, elasticity);

		}

		public bool CollideAgainstBounce(Capsule2D capsule2D, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass, float elasticity)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, capsule2D, considerPartitioning, axisToUse, otherMass, thisMass, elasticity);

		}

		
		public bool CollideAgainstBounce(Sphere sphere, float thisMass, float otherMass, float elasticity)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, sphere, false, Axis.X, otherMass, thisMass, elasticity);

		}

		public bool CollideAgainstBounce(Sphere sphere, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass, float elasticity)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, sphere, considerPartitioning, axisToUse, otherMass, thisMass, elasticity);

		}

		
		public bool CollideAgainstBounce(AxisAlignedCube axisAlignedCube, float thisMass, float otherMass, float elasticity)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, axisAlignedCube, false, Axis.X, otherMass, thisMass, elasticity);

		}

		public bool CollideAgainstBounce(AxisAlignedCube axisAlignedCube, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass, float elasticity)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, axisAlignedCube, considerPartitioning, axisToUse, otherMass, thisMass, elasticity);

		}

		
		public bool CollideAgainst(ShapeCollection shapeCollection)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThis(this, shapeCollection, false, Axis.X);

		}

		public bool CollideAgainst(ShapeCollection shapeCollection, bool considerPartitioning, Axis axisToUse)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThis(this, shapeCollection, considerPartitioning, axisToUse);

		}

		

		public bool CollideAgainstMove(ShapeCollection shapeCollection, float thisMass, float otherMass)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, shapeCollection, false, Axis.X, otherMass, thisMass);

		}

		public bool CollideAgainstMove(ShapeCollection shapeCollection, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, shapeCollection, considerPartitioning, axisToUse, otherMass, thisMass);

		}

		
		public bool CollideAgainstBounce(ShapeCollection shapeCollection, float thisMass, float otherMass, float elasticity)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, shapeCollection, false, Axis.X, otherMass, thisMass, elasticity);

		}

		public bool CollideAgainstBounce(ShapeCollection shapeCollection, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass, float elasticity)
		{
			return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, shapeCollection, considerPartitioning, axisToUse, otherMass, thisMass, elasticity);

		}

		#endregion
*/
	}
}
