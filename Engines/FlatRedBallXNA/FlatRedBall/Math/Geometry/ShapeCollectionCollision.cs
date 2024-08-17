
using Microsoft.Xna.Framework;
using System;

namespace FlatRedBall.Math.Geometry
{	
	internal static class ShapeCollectionCollision
	{
        // The CollideShapeAgainstThis method needs to update dependencies on the shape being passed in to the method.
        // The reason is because deep collision performs an update dependencies so that a a shape will be positioned relative
        // to its parent, but these methods perform partitioning, which may prevent deep collision from happening at all. The partitioning
        // depends on the position of the shape to collide against, so we need that to be up to date before performing partitioning.

		private static void CheckAndReportNaN(PositionedObject objectToCheck)
		{
			if(float.IsNaN(objectToCheck.Position.X))
			{
				throw new ArgumentException("The shape has an X of NaN, which is invalid for collision");
			}
			if (float.IsNaN(objectToCheck.Position.Y))
			{
				throw new ArgumentException("The shape has a Y of NaN, which is invalid for collision");
			}
		}

		internal static bool CollideShapeAgainstThis(ShapeCollection thisShapeCollection, AxisAlignedRectangle shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse)
		{
#if DEBUG
			CheckAndReportNaN(shapeToCollideAgainstThis);
#endif
			thisShapeCollection.ClearLastCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndexExclusive;

            #endregion

            float boundStartPosition = 0;
            float rectangleRadius = 0;

            switch (axisToUse)
            {
                case Axis.X:
                    boundStartPosition = shapeToCollideAgainstThis.X;
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusX;
                    break;
                case Axis.Y:
                    boundStartPosition = shapeToCollideAgainstThis.Y;
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusY;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                    // break;
            }

            #region vs. AxisAlignedRectangles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
                // SET THIS:
                rectangleRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mAxisAlignedRectangles[i]))
                {
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

			#endregion
			thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
		    #region vs. Circles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mCircles[i]))
                {
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Polygons

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mPolygons[i]))
                {
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Lines

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mLines[i]))
                {
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Capsule2Ds

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mCapsule2Ds[i]))
                {
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            return returnValue;
		}

		internal static bool CollideShapeAgainstThis(ShapeCollection thisShapeCollection, Circle shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse)
		{
#if DEBUG
			CheckAndReportNaN(shapeToCollideAgainstThis);
#endif
			thisShapeCollection.ClearLastCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndexExclusive;

            #endregion

            #region Get the boundStartPosition

            float boundStartPosition = 0;
            float rectangleRadius = 0;

            switch (axisToUse)
            {
                case Axis.X:
					boundStartPosition = shapeToCollideAgainstThis.X;
					rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusX;
                    break;
                case Axis.Y:
					boundStartPosition = shapeToCollideAgainstThis.Y;
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusY;
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
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    rectangleRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mAxisAlignedRectangles[i]))
                {
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Circles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mCircles[i]))
                {
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Polygons

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mPolygons[i]))
                {
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Lines

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mLines[i]))
                {
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Capsule2Ds

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mCapsule2Ds[i]))
                {
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            return returnValue;
		}
		internal static bool CollideShapeAgainstThis(ShapeCollection thisShapeCollection, Polygon shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse)
		{
#if DEBUG
			CheckAndReportNaN(shapeToCollideAgainstThis);
#endif
			thisShapeCollection.ClearLastCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex=0;
            int endIndexExclusive=0;

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

			if(shapeToCollideAgainstThis.Vertices.Length > 0)
            {
				float min;
				float max;
                float rectangleRadius = 0;

                if (axisToUse == Axis.X)
                {
					min = shapeToCollideAgainstThis.Vertices[0].Position.X;
					max = shapeToCollideAgainstThis.Vertices[0].Position.X;
					rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusX;

                }
				else // if(axisToUse == Axis.Y)
                {
					min = shapeToCollideAgainstThis.Vertices[0].Position.Y;
					max = shapeToCollideAgainstThis.Vertices[0].Position.Y;
					rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusY;
                }

                for (int vertexIndex = 1; vertexIndex < shapeToCollideAgainstThis.Vertices.Length; vertexIndex++)
				{
					var position = shapeToCollideAgainstThis.Vertices[vertexIndex].Position;

					if (axisToUse == Axis.X)
					{
						min = position.X < min ? position.X : min;
						max = position.X > max ? position.X : max;
					}
					else // if(axisToUse == Axis.Y)
					{
						min = position.Y < min ? position.Y : min;
						max = position.Y > max ? position.Y : max;
					}
				}

				var averagePosition = (min + max) / 2.0f;
				var width = max - min;

				GetStartAndEnd(
					considerAxisBasedPartitioning, 
					axisToUse, 
					out startIndex, 
					out endIndexExclusive, 
					averagePosition, 
					width,
                    // SET THIS:
                    rectangleRadius, 
					thisShapeCollection.mAxisAlignedRectangles
					// END OF SET
					);

				var doOld = false;
				if(doOld)
                {
					for (int i = startIndex; i < endIndexExclusive; i++)
					{
						var rectangle = thisShapeCollection.mAxisAlignedRectangles[i];
						if (shapeToCollideAgainstThis.CollideAgainst(rectangle))
						{
							thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(rectangle);
							returnValue = true;
						}
					}
                }
				else
                {
					var segment = new Segment();
					for(int vertexIndex = 0; vertexIndex < shapeToCollideAgainstThis.mVertices.Length-1; vertexIndex++)
                    {
                        var firstPosition = shapeToCollideAgainstThis.mVertices[vertexIndex].Position;
                        var secondPosition = shapeToCollideAgainstThis.mVertices[vertexIndex + 1].Position;

                        segment.Point1.X = firstPosition.X;
                        segment.Point1.Y = firstPosition.Y;


                        segment.Point2.X = secondPosition.X;
                        segment.Point2.Y = secondPosition.Y;

                        var newValue = DoSegmentVsRectangles(thisShapeCollection, startIndex, endIndexExclusive, segment);

						if (newValue)
						{
							returnValue = true;
						}
                    }
                }

            }


            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Circles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mCircles[i]))
                {
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Polygons

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mPolygons[i]))
                {
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Lines

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mLines[i]))
                {
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Capsule2Ds

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mCapsule2Ds[i]))
                {
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            return returnValue;
		}

        private static bool DoSegmentVsRectangles(ShapeCollection thisShapeCollection, 
			int startIndex, int endIndex, Segment segment)
        {
			var returnValue = false;

			var difference = (segment.Point2 - segment.Point1).ToVector3();

			var isHorizontal = System.Math.Abs(difference.X) > System.Math.Abs(difference.Y);

			Vector3 leftPoint = segment.Point2.ToVector3();
			Vector3 rightPoint = segment.Point1.ToVector3();
			if (segment.Point2.X > segment.Point1.X)
			{
				leftPoint = segment.Point1.ToVector3();
				rightPoint = segment.Point2.ToVector3();
			}

			Vector3 bottomPoint = segment.Point2.ToVector3();
			Vector3 topPoint = segment.Point1.ToVector3();
			if (segment.Point2.Y > segment.Point1.Y)
			{
				bottomPoint = segment.Point1.ToVector3();
				topPoint = segment.Point2.ToVector3();
			}

			for (int i = startIndex; i < endIndex; i++)
            {
                var rectangle = thisShapeCollection.mAxisAlignedRectangles[i];

				// first do bounding box. If this fails, we can exit
				if (bottomPoint.Y > rectangle.Top || topPoint.Y < rectangle.Bottom ||
					leftPoint.X > rectangle.Right || rightPoint.X < rectangle.Left)
				{
					continue;
				}

				var collided = false;

				if (isHorizontal)
				{

					if (leftPoint.X > rectangle.Right) collided = false;
					else if (rightPoint.X < rectangle.Left) collided = false;
					else
					{
						// they overlap on the X
						var slope = (rightPoint.Y - leftPoint.Y) / (rightPoint.X - leftPoint.X);

						var leftX = System.Math.Max(leftPoint.X, rectangle.Left);
						var rightX = System.Math.Min(rightPoint.X, rectangle.Right);

						var leftY = leftPoint.Y + slope * (leftX - leftPoint.X);
						var rightY = leftPoint.Y + slope * (rightX - leftPoint.X);

						collided = (slope < 0 && leftY > rectangle.Bottom && rightY < rectangle.Top) ||
							(leftY < rectangle.Top && rightY > rectangle.Bottom);
					}
				}
				else
				{

					if (bottomPoint.Y > rectangle.Top) collided = false;
					else if (topPoint.Y < rectangle.Bottom) collided = false;
					else
					{
						// they overlap on the Y
						var invertSlope = (topPoint.X - bottomPoint.X) / (topPoint.Y - bottomPoint.Y);

						var bottomY = System.Math.Max(bottomPoint.Y, rectangle.Bottom);
						var topY = System.Math.Min(topPoint.Y, rectangle.Top);

						var bottomX = bottomPoint.X + invertSlope * (bottomY - bottomPoint.Y);
						var topX = bottomPoint.X + invertSlope * (topY - bottomPoint.Y);

						collided = (invertSlope < 0 && bottomX > rectangle.Left && topX < rectangle.Right) ||
							(bottomX < rectangle.Right && topX > rectangle.Bottom);
					}
				}


				if (collided)
                {
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(rectangle);
                    returnValue = true;
                }
            }

            return returnValue;
        }

        internal static bool CollideShapeAgainstThis(ShapeCollection thisShapeCollection, Line shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse)
		{
#if DEBUG
			CheckAndReportNaN(shapeToCollideAgainstThis);
#endif
			thisShapeCollection.ClearLastCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndexExclusive;

            #endregion



			float rectangleRadius = 0;
            float lineStartPosition = 0;
            float lineHalfWidthOrHeight = 0;

            switch (axisToUse)
            {
                case Axis.X:
					var left = System.Math.Min(shapeToCollideAgainstThis.AbsolutePoint1.X, shapeToCollideAgainstThis.AbsolutePoint2.X);
					var right = System.Math.Max(shapeToCollideAgainstThis.AbsolutePoint1.X, shapeToCollideAgainstThis.AbsolutePoint2.X);
					lineHalfWidthOrHeight = (float)(right - left) / 2.0f;
					lineStartPosition = (float)(left + right) / 2.0f;
					rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusX;
                    break;
                case Axis.Y:
					var bottom = System.Math.Min(shapeToCollideAgainstThis.AbsolutePoint1.Y, shapeToCollideAgainstThis.AbsolutePoint2.Y);
					var top = System.Math.Max(shapeToCollideAgainstThis.AbsolutePoint1.Y, shapeToCollideAgainstThis.AbsolutePoint2.Y);
					lineHalfWidthOrHeight = (float)(top - bottom) / 2.0f;
					lineStartPosition = (float)((top + bottom) / 2.0f);
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusY;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                // break;
            }
            

		    #region vs. AxisAlignedRectangles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    lineStartPosition,
				lineHalfWidthOrHeight,
			    // SET THIS:
			    rectangleRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mAxisAlignedRectangles[i]))
                {
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Circles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    lineStartPosition,
				lineHalfWidthOrHeight,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mCircles[i]))
                {
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Polygons

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    lineStartPosition,
				lineHalfWidthOrHeight,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mPolygons[i]))
                {
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Lines

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    lineStartPosition,
				lineHalfWidthOrHeight,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mLines[i]))
                {
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Capsule2Ds

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    lineStartPosition,
				lineHalfWidthOrHeight,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mCapsule2Ds[i]))
                {
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            return returnValue;
		}
		internal static bool CollideShapeAgainstThis(ShapeCollection thisShapeCollection, Capsule2D shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse)
		{
#if DEBUG
			CheckAndReportNaN(shapeToCollideAgainstThis);
#endif
			thisShapeCollection.ClearLastCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndexExclusive;

            #endregion

            float boundStartPosition = 0;
            float rectangleRadius = 0;

            switch (axisToUse)
            {
                case Axis.X:
                    boundStartPosition = shapeToCollideAgainstThis.X;
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusX;
                    break;
                case Axis.Y:
                    boundStartPosition = shapeToCollideAgainstThis.Y;
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusY;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                    // break;
            }

            #region vs. AxisAlignedRectangles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    rectangleRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mAxisAlignedRectangles[i]))
                {
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Circles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mCircles[i]))
                {
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Polygons

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mPolygons[i]))
                {
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Lines

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mLines[i]))
                {
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Capsule2Ds

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mCapsule2Ds[i]))
                {
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            return returnValue;
		}
		internal static bool CollideShapeAgainstThis(ShapeCollection thisShapeCollection, Sphere shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse)
		{
#if DEBUG
			CheckAndReportNaN(shapeToCollideAgainstThis);
#endif
			thisShapeCollection.ClearLastCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndexExclusive;

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
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxSpheresRadius, 
			    thisShapeCollection.mSpheres
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mSpheres[i]))
                {
                    thisShapeCollection.mLastCollisionSpheres.Add(thisShapeCollection.mSpheres[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. AxisAlignedCubes

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedCubesRadius, 
			    thisShapeCollection.mAxisAlignedCubes
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mAxisAlignedCubes[i]))
                {
                    thisShapeCollection.mLastCollisionAxisAlignedCubes.Add(thisShapeCollection.mAxisAlignedCubes[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            return returnValue;
		}
		internal static bool CollideShapeAgainstThis(ShapeCollection thisShapeCollection, AxisAlignedCube shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse)
		{
#if DEBUG
			CheckAndReportNaN(shapeToCollideAgainstThis);
#endif
			thisShapeCollection.ClearLastCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndexExclusive;

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
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxSpheresRadius, 
			    thisShapeCollection.mSpheres
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mSpheres[i]))
                {
                    thisShapeCollection.mLastCollisionSpheres.Add(thisShapeCollection.mSpheres[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. AxisAlignedCubes

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedCubesRadius, 
			    thisShapeCollection.mAxisAlignedCubes
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainst(thisShapeCollection.mAxisAlignedCubes[i]))
                {
                    thisShapeCollection.mLastCollisionAxisAlignedCubes.Add(thisShapeCollection.mAxisAlignedCubes[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            return returnValue;
		}

		internal static bool CollideShapeAgainstThisMove(ShapeCollection thisShapeCollection, AxisAlignedRectangle shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass)
		{
#if DEBUG
			CheckAndReportNaN(shapeToCollideAgainstThis);
#endif
			thisShapeCollection.ClearLastCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndexExclusive;

			#endregion
			var positionBefore = shapeToCollideAgainstThis.Position;

            float boundStartPosition = 0;
            float rectangleRadius = 0;

            switch (axisToUse)
            {
                case Axis.X:
                    boundStartPosition = shapeToCollideAgainstThis.X;
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusX;
                    break;
                case Axis.Y:
                    boundStartPosition = shapeToCollideAgainstThis.Y;
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusY;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                    // break;
            }

            #region vs. AxisAlignedRectangles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    rectangleRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mAxisAlignedRectangles[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mAxisAlignedRectangles[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Circles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mCircles[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mCircles[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Polygons

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mPolygons[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mPolygons[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Lines

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mLines[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mLines[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Capsule2Ds

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mCapsule2Ds[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mCapsule2Ds[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            shapeToCollideAgainstThis.mLastMoveCollisionReposition = (shapeToCollideAgainstThis.Position - positionBefore).ToVector2();

			return returnValue;
		}
		internal static bool CollideShapeAgainstThisMove(ShapeCollection thisShapeCollection, Circle shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass)
		{
#if DEBUG
			CheckAndReportNaN(shapeToCollideAgainstThis);
#endif
			thisShapeCollection.ClearLastCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndexExclusive;

			#endregion
			var positionBefore = shapeToCollideAgainstThis.Position;

            float boundStartPosition = 0;
            float rectangleRadius = 0;

            switch (axisToUse)
            {
                case Axis.X:
                    boundStartPosition = shapeToCollideAgainstThis.X;
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusX;
                    break;
                case Axis.Y:
                    boundStartPosition = shapeToCollideAgainstThis.Y;
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusY;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                    // break;
            }

            #region vs. AxisAlignedRectangles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    rectangleRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mAxisAlignedRectangles[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mAxisAlignedRectangles[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Circles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mCircles[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mCircles[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Polygons

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mPolygons[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mPolygons[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Lines

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mLines[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mLines[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Capsule2Ds

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mCapsule2Ds[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mCapsule2Ds[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            shapeToCollideAgainstThis.LastMoveCollisionReposition = (shapeToCollideAgainstThis.Position - positionBefore).ToVector2();

			return returnValue;
		}
		internal static bool CollideShapeAgainstThisMove(ShapeCollection thisShapeCollection, Polygon shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass)
		{
#if DEBUG
			CheckAndReportNaN(shapeToCollideAgainstThis);
#endif
			thisShapeCollection.ClearLastCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndexExclusive;

			#endregion
			var positionBefore = shapeToCollideAgainstThis.Position;

            float boundStartPosition = 0;
            float rectangleRadius = 0;

            switch (axisToUse)
            {
                case Axis.X:
                    boundStartPosition = shapeToCollideAgainstThis.X;
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusX;
                    break;
                case Axis.Y:
                    boundStartPosition = shapeToCollideAgainstThis.Y;
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusY;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                    // break;
            }

            #region vs. AxisAlignedRectangles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    rectangleRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mAxisAlignedRectangles[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mAxisAlignedRectangles[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Circles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mCircles[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mCircles[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Polygons

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mPolygons[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mPolygons[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Lines

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mLines[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mLines[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

			#endregion
			shapeToCollideAgainstThis.mLastMoveCollisionReposition = (shapeToCollideAgainstThis.Position - positionBefore);
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Capsule2Ds

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mCapsule2Ds[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mCapsule2Ds[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            return returnValue;
		}
		internal static bool CollideShapeAgainstThisMove(ShapeCollection thisShapeCollection, Line shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass)
		{
#if DEBUG
			CheckAndReportNaN(shapeToCollideAgainstThis);
#endif
			thisShapeCollection.ClearLastCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndexExclusive;

            #endregion

            float boundStartPosition = 0;
            float rectangleRadius = 0;

            switch (axisToUse)
            {
                case Axis.X:
                    boundStartPosition = shapeToCollideAgainstThis.X;
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusX;
                    break;
                case Axis.Y:
                    boundStartPosition = shapeToCollideAgainstThis.Y;
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusY;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                    // break;
            }

            #region vs. AxisAlignedRectangles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    rectangleRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mAxisAlignedRectangles[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mAxisAlignedRectangles[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Circles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mCircles[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mCircles[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Polygons

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mPolygons[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mPolygons[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Lines

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mLines[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mLines[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Capsule2Ds

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mCapsule2Ds[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mCapsule2Ds[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            return returnValue;
		}
		internal static bool CollideShapeAgainstThisMove(ShapeCollection thisShapeCollection, Capsule2D shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass)
		{
#if DEBUG
			CheckAndReportNaN(shapeToCollideAgainstThis);
#endif
			thisShapeCollection.ClearLastCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndexExclusive;

            #endregion

            float boundStartPosition = 0;
            float rectangleRadius = 0;

            switch (axisToUse)
            {
                case Axis.X:
                    boundStartPosition = shapeToCollideAgainstThis.X;
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusX;
                    break;
                case Axis.Y:
                    boundStartPosition = shapeToCollideAgainstThis.Y;
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusY;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                    // break;
            }

            #region vs. AxisAlignedRectangles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    rectangleRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mAxisAlignedRectangles[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mAxisAlignedRectangles[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Circles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mCircles[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mCircles[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Polygons

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mPolygons[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mPolygons[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Lines

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mLines[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mLines[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Capsule2Ds

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mCapsule2Ds[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mCapsule2Ds[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            return returnValue;
		}
		internal static bool CollideShapeAgainstThisMove(ShapeCollection thisShapeCollection, Sphere shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass)
		{
#if DEBUG
			CheckAndReportNaN(shapeToCollideAgainstThis);
#endif
			thisShapeCollection.ClearLastCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndexExclusive;

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
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxSpheresRadius, 
			    thisShapeCollection.mSpheres
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mSpheres[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mSpheres[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionSpheres.Add(thisShapeCollection.mSpheres[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. AxisAlignedCubes

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedCubesRadius, 
			    thisShapeCollection.mAxisAlignedCubes
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mAxisAlignedCubes[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mAxisAlignedRectangles[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedCubes.Add(thisShapeCollection.mAxisAlignedCubes[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            return returnValue;
		}
		internal static bool CollideShapeAgainstThisMove(ShapeCollection thisShapeCollection, AxisAlignedCube shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass)
		{
#if DEBUG
			CheckAndReportNaN(shapeToCollideAgainstThis);
#endif
			thisShapeCollection.ClearLastCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndexExclusive;

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
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxSpheresRadius, 
			    thisShapeCollection.mSpheres
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mSpheres[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mSpheres[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionSpheres.Add(thisShapeCollection.mSpheres[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. AxisAlignedCubes

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedCubesRadius, 
			    thisShapeCollection.mAxisAlignedCubes
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstMove(thisShapeCollection.mAxisAlignedCubes[i], shapeMass, collectionMass))
                {
                    if (thisShapeCollection.mAxisAlignedCubes[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedCubes.Add(thisShapeCollection.mAxisAlignedCubes[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            return returnValue;
		}

		internal static bool CollideShapeAgainstThisBounce(ShapeCollection thisShapeCollection, AxisAlignedRectangle shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass, float elasticity)
		{
#if DEBUG
			CheckAndReportNaN(shapeToCollideAgainstThis);
#endif
			thisShapeCollection.ClearLastCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndexExclusive;

			#endregion

			var positionBefore = shapeToCollideAgainstThis.Position;

            float boundStartPosition = 0;
            float rectangleRadius = 0;

            switch (axisToUse)
            {
                case Axis.X:
                    boundStartPosition = shapeToCollideAgainstThis.X;
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusX;
                    break;
                case Axis.Y:
                    boundStartPosition = shapeToCollideAgainstThis.Y;
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusY;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                    // break;
            }

            #region vs. AxisAlignedRectangles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    rectangleRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mAxisAlignedRectangles[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mAxisAlignedRectangles[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Circles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mCircles[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mCircles[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Polygons

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mPolygons[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mPolygons[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Lines

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mLines[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mLines[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Capsule2Ds

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mCapsule2Ds[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mCapsule2Ds[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            shapeToCollideAgainstThis.mLastMoveCollisionReposition = (shapeToCollideAgainstThis.Position - positionBefore).ToVector2();

            return returnValue;
		}
		internal static bool CollideShapeAgainstThisBounce(ShapeCollection thisShapeCollection, Circle shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass, float elasticity)
		{
#if DEBUG
			CheckAndReportNaN(shapeToCollideAgainstThis);
#endif
			thisShapeCollection.ClearLastCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndexExclusive;

			#endregion

			var positionBefore = shapeToCollideAgainstThis.Position;

            float boundStartPosition = 0;
            float rectangleRadius = 0;

            switch (axisToUse)
            {
                case Axis.X:
                    boundStartPosition = shapeToCollideAgainstThis.X;
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusX;
                    break;
                case Axis.Y:
                    boundStartPosition = shapeToCollideAgainstThis.Y;
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusY;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                    // break;
            }

            #region vs. AxisAlignedRectangles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    rectangleRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mAxisAlignedRectangles[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mAxisAlignedRectangles[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Circles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mCircles[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mCircles[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Polygons

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mPolygons[i], shapeMass, collectionMass, elasticity))
                {
                    if(thisShapeCollection.mPolygons[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Lines

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mLines[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mLines[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Capsule2Ds

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mCapsule2Ds[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mCapsule2Ds[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            shapeToCollideAgainstThis.LastMoveCollisionReposition = (shapeToCollideAgainstThis.Position - positionBefore).ToVector2();

			return returnValue;
		}
		internal static bool CollideShapeAgainstThisBounce(ShapeCollection thisShapeCollection, Polygon shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass, float elasticity)
		{
#if DEBUG
			CheckAndReportNaN(shapeToCollideAgainstThis);
#endif
			thisShapeCollection.ClearLastCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndexExclusive;

			#endregion

			var positionBefore = shapeToCollideAgainstThis.Position;

            float boundStartPosition = 0;
            float rectangleRadius = 0;

            switch (axisToUse)
            {
                case Axis.X:
                    boundStartPosition = shapeToCollideAgainstThis.X;
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusX;
                    break;
                case Axis.Y:
                    boundStartPosition = shapeToCollideAgainstThis.Y;
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusY;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                    // break;
            }

            #region vs. AxisAlignedRectangles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    rectangleRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mAxisAlignedRectangles[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mAxisAlignedRectangles[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Circles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mCircles[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mCircles[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Polygons

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mPolygons[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mPolygons[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Lines

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mLines[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mLines[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Capsule2Ds

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mCapsule2Ds[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mCapsule2Ds[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            shapeToCollideAgainstThis.mLastMoveCollisionReposition = (shapeToCollideAgainstThis.Position - positionBefore);

			return returnValue;
		}
		internal static bool CollideShapeAgainstThisBounce(ShapeCollection thisShapeCollection, Line shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass, float elasticity)
		{
#if DEBUG
			CheckAndReportNaN(shapeToCollideAgainstThis);
#endif
			thisShapeCollection.ClearLastCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndexExclusive;

            #endregion

            float boundStartPosition = 0;
            float rectangleRadius = 0;

            switch (axisToUse)
            {
                case Axis.X:
                    boundStartPosition = shapeToCollideAgainstThis.X;
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusX;
                    break;
                case Axis.Y:
                    boundStartPosition = shapeToCollideAgainstThis.Y;
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusY;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                    // break;
            }

            #region vs. AxisAlignedRectangles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    rectangleRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mAxisAlignedRectangles[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mAxisAlignedRectangles[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Circles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mCircles[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mCircles[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Polygons

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mPolygons[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mPolygons[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Lines

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mLines[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mLines[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Capsule2Ds

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mCapsule2Ds[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mCapsule2Ds[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            return returnValue;
		}
		internal static bool CollideShapeAgainstThisBounce(ShapeCollection thisShapeCollection, Capsule2D shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass, float elasticity)
		{
#if DEBUG
			CheckAndReportNaN(shapeToCollideAgainstThis);
#endif
			thisShapeCollection.ClearLastCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndexExclusive;

            #endregion

            float boundStartPosition = 0;
			float rectangleRadius = 0;

            switch (axisToUse)
            {
                case Axis.X:
					boundStartPosition = shapeToCollideAgainstThis.X;
					rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusX;
                    break;
                case Axis.Y:
					boundStartPosition = shapeToCollideAgainstThis.Y;
                    rectangleRadius = thisShapeCollection.mMaxAxisAlignedRectanglesRadiusY;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                // break;
            }


		    #region vs. AxisAlignedRectangles

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
                // SET THIS:
                rectangleRadius, 
			    thisShapeCollection.mAxisAlignedRectangles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mAxisAlignedRectangles[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mAxisAlignedRectangles[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedRectangles.Add(thisShapeCollection.mAxisAlignedRectangles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Circles

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCirclesRadius, 
			    thisShapeCollection.mCircles
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mCircles[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mCircles[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCircles.Add(thisShapeCollection.mCircles[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Polygons

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxPolygonsRadius, 
			    thisShapeCollection.mPolygons
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mPolygons[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mPolygons[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionPolygons.Add(thisShapeCollection.mPolygons[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Lines

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxLinesRadius, 
			    thisShapeCollection.mLines
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mLines[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mLines[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionLines.Add(thisShapeCollection.mLines[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. Capsule2Ds

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxCapsule2DsRadius, 
			    thisShapeCollection.mCapsule2Ds
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mCapsule2Ds[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mCapsule2Ds[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionCapsule2Ds.Add(thisShapeCollection.mCapsule2Ds[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            return returnValue;
		}
		internal static bool CollideShapeAgainstThisBounce(ShapeCollection thisShapeCollection, Sphere shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass, float elasticity)
		{
#if DEBUG
			CheckAndReportNaN(shapeToCollideAgainstThis);
#endif
			thisShapeCollection.ClearLastCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndexExclusive;

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
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxSpheresRadius, 
			    thisShapeCollection.mSpheres
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mSpheres[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mSpheres[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionSpheres.Add(thisShapeCollection.mSpheres[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. AxisAlignedCubes

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedCubesRadius, 
			    thisShapeCollection.mAxisAlignedCubes
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mAxisAlignedCubes[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mAxisAlignedCubes[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedCubes.Add(thisShapeCollection.mAxisAlignedCubes[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            return returnValue;
		}
		internal static bool CollideShapeAgainstThisBounce(ShapeCollection thisShapeCollection, AxisAlignedCube shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass, float elasticity)
		{
#if DEBUG
			CheckAndReportNaN(shapeToCollideAgainstThis);
#endif
			thisShapeCollection.ClearLastCollisionLists();
            shapeToCollideAgainstThis.UpdateDependencies(TimeManager.CurrentTime);
            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndexExclusive;

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
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxSpheresRadius, 
			    thisShapeCollection.mSpheres
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mSpheres[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mSpheres[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionSpheres.Add(thisShapeCollection.mSpheres[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            #region vs. AxisAlignedCubes

            GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndexExclusive, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMaxAxisAlignedCubesRadius, 
			    thisShapeCollection.mAxisAlignedCubes
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndexExclusive; i++)
            {
                if (shapeToCollideAgainstThis.CollideAgainstBounce(thisShapeCollection.mAxisAlignedCubes[i], shapeMass, collectionMass, elasticity))
                {
                    if (thisShapeCollection.mAxisAlignedCubes[i].Parent != null) thisShapeCollection.ResetLastUpdateTimes();
                    thisShapeCollection.mLastCollisionAxisAlignedCubes.Add(thisShapeCollection.mAxisAlignedCubes[i]);
                    returnValue = true;
                }
            }

            #endregion
            thisShapeCollection.LastCollisionCallDeepCheckCount += endIndexExclusive - startIndex;
            return returnValue;
		}

		internal static bool CollideShapeAgainstThis(ShapeCollection thisShapeCollection, ShapeCollection shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse)
		{
			thisShapeCollection.ClearLastCollisionLists();
			shapeToCollideAgainstThis.ClearLastCollisionLists();
			thisShapeCollection.mSuppressLastCollisionClear = true;
            bool returnValue = false;


            for (int i = 0; i < shapeToCollideAgainstThis.AxisAlignedRectangles.Count; i++)
			{
                AxisAlignedRectangle shape = shapeToCollideAgainstThis.AxisAlignedRectangles[i];

				if(ShapeCollectionCollision.CollideShapeAgainstThis(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse))
                {
					returnValue = true;
                }
            }

            for (int i = 0; i < shapeToCollideAgainstThis.Circles.Count; i++)
			{
                Circle shape = shapeToCollideAgainstThis.Circles[i];

                if( ShapeCollectionCollision.CollideShapeAgainstThis(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse))
                {
					shapeToCollideAgainstThis.LastCollisionCircles.Add(shape);
					returnValue = true;
                }
            }

            for (int i = 0; i < shapeToCollideAgainstThis.Polygons.Count; i++)
			{
                Polygon shape = shapeToCollideAgainstThis.Polygons[i];

                if(ShapeCollectionCollision.CollideShapeAgainstThis(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse))
                {
					shapeToCollideAgainstThis.LastCollisionPolygons.Add(shape);
					returnValue = true;
				}
            }

            for (int i = 0; i < shapeToCollideAgainstThis.Lines.Count; i++)
			{
                Line shape = shapeToCollideAgainstThis.Lines[i];

                if(ShapeCollectionCollision.CollideShapeAgainstThis(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse))
                {
					shapeToCollideAgainstThis.LastCollisionLines.Add(shape);
					returnValue = true;
                }
            }

            for (int i = 0; i < shapeToCollideAgainstThis.Capsule2Ds.Count; i++)
			{
                Capsule2D shape = shapeToCollideAgainstThis.Capsule2Ds[i];

                if(ShapeCollectionCollision.CollideShapeAgainstThis(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse))
                {
					shapeToCollideAgainstThis.LastCollisionCapsule2Ds.Add(shape);
					returnValue = true;
                }
            }

            for (int i = 0; i < shapeToCollideAgainstThis.Spheres.Count; i++)
			{
                Sphere shape = shapeToCollideAgainstThis.Spheres[i];

                if(ShapeCollectionCollision.CollideShapeAgainstThis(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse))
                {
					shapeToCollideAgainstThis.LastCollisionSpheres.Add(shape);
					returnValue = true;
                }
            }

            for (int i = 0; i < shapeToCollideAgainstThis.AxisAlignedCubes.Count; i++)
			{
                AxisAlignedCube shape = shapeToCollideAgainstThis.AxisAlignedCubes[i];

                if(ShapeCollectionCollision.CollideShapeAgainstThis(thisShapeCollection, shape, considerAxisBasedPartitioning, axisToUse))
                {
					shapeToCollideAgainstThis.LastCollisionAxisAlignedCubes.Add(shape);
					returnValue = true;
                }
            }
            shapeToCollideAgainstThis.LastCollisionCallDeepCheckCount = thisShapeCollection.LastCollisionCallDeepCheckCount;

            thisShapeCollection.mSuppressLastCollisionClear = false;
            return returnValue;
		}
		internal static bool CollideShapeAgainstThisMove(ShapeCollection thisShapeCollection, ShapeCollection shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass)
		{
			thisShapeCollection.ClearLastCollisionLists();
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
            shapeToCollideAgainstThis.LastCollisionCallDeepCheckCount = thisShapeCollection.LastCollisionCallDeepCheckCount;

            thisShapeCollection.mSuppressLastCollisionClear = false;
            return returnValue;
		}
		internal static bool CollideShapeAgainstThisBounce(ShapeCollection thisShapeCollection, ShapeCollection shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse, float shapeMass, float collectionMass, float elasticity)
		{
			thisShapeCollection.ClearLastCollisionLists();
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
            shapeToCollideAgainstThis.LastCollisionCallDeepCheckCount = thisShapeCollection.LastCollisionCallDeepCheckCount;

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
