using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

using FlatRedBall;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Utilities;

#if FRB_XNA || SILVERLIGHT || WINDOWS_PHONE
using Microsoft.Xna.Framework;
#elif FRB_MDX
using Microsoft.DirectX;
#endif

namespace FlatRedBall.AI.Pathfinding
{
    public static class PathfindingFunctions
    {
        #region XML Docs
        /// <summary>
        /// Tests if two vector positions are within line of sight given a collision map.
        /// </summary>
        /// <param name="position1">The first world-coordinate position.</param>
        /// <param name="position2">The second world-coordinate position.</param>
        /// <param name="collisionMap">The list of polygons used to test if the two positions are within line of sight.</param>
        /// <returns></returns>
        #endregion
        public static bool IsInLineOfSight(Vector3 position1, Vector3 position2, PositionedObjectList<Polygon> collisionMap)
        {
            return IsInLineOfSight(position1, position2, 0f, collisionMap);
        }

        #region XML Docs
        /// <summary>
        /// Tests if two vector positions are within line of sight given a collision map.
        /// </summary>
        /// <param name="position1">The first world-coordinate position.</param>
        /// <param name="position2">The second world-coordinate position.</param>
        /// <param name="collisionThreshold">Distance from position2 to the polygon it's colliding against.
        /// If a polygon is within this threshold, this will return false.</param>
        /// <param name="collisionMap">The list of polygons used to test if the two positions are within line of sight.</param>
        /// <returns></returns>
        #endregion
        public static bool IsInLineOfSight(Vector3 position1, Vector3 position2, float collisionThreshold, PositionedObjectList<Polygon> collisionMap)
        {
            Segment segment = new Segment(new FlatRedBall.Math.Geometry.Point(ref position1),
                new FlatRedBall.Math.Geometry.Point(ref position2));

            foreach (Polygon polygon in collisionMap)
            {
                if (polygon.CollideAgainst(segment) ||
                    (collisionThreshold > 0 && segment.DistanceTo(polygon) < collisionThreshold))
                {
                    return false;
                }
            }
            
            return true;
        }

        public static bool IsInLineOfSight(Vector3 position1, Vector3 position2, float collisionThreshold, ShapeCollection shapeCollection)
        {
            Segment segment = new Segment(new FlatRedBall.Math.Geometry.Point(ref position1),
                new FlatRedBall.Math.Geometry.Point(ref position2));

            for(int i = 0; i < shapeCollection.Polygons.Count; i++)
            {
                Polygon polygon = shapeCollection.Polygons[i];

                if (polygon.CollideAgainst(segment) ||
                    (collisionThreshold > 0 && segment.DistanceTo(polygon) < collisionThreshold))
                {
                    return false;
                }
            }

            for (int i = 0; i < shapeCollection.AxisAlignedRectangles.Count; i++)
            {
                AxisAlignedRectangle rectangle = shapeCollection.AxisAlignedRectangles[i];

                FlatRedBall.Math.Geometry.Point throwaway;

                if (rectangle.Intersects(segment, out throwaway) ||
                    (collisionThreshold > 0 && segment.DistanceTo(rectangle) < collisionThreshold))
                {
                    return false;
                }
            }

            for (int i = 0; i < shapeCollection.Circles.Count; i++)
            {
                Circle circle = shapeCollection.Circles[i];               

                if (segment.DistanceTo(circle) < collisionThreshold)
                {
                    return false;
                }
            }

#if DEBUG
            if (shapeCollection.Capsule2Ds.Count != 0)
            {
                throw new Exception("IsInLineOfSight does not support ShapeCollections with Capsule2Ds");
            }
#endif

            for (int i = 0; i < shapeCollection.Lines.Count; i++)
            {
                Line line = shapeCollection.Lines[i];

                if (segment.DistanceTo(line.AsSegment()) < collisionThreshold)
                {
                    return false;
                }
            }

            return true;

        }

        #region XML Docs
        /// <summary>
        /// Returns the midpoint between two Vector3s.
        /// </summary>
        /// <param name="position1">The first position.</param>
        /// <param name="position2">The connecting position.</param>
        /// <returns>The midpoint between the two positions.</returns>
        #endregion
        public static Vector3 Midpoint(Vector3 position1, Vector3 position2)
        {
            Vector3 midpoint = (position1 + position2);

            midpoint.X = midpoint.X / 2.0f;
            midpoint.Y = midpoint.Y / 2.0f;
            midpoint.Z = midpoint.Z / 2.0f;

            return midpoint;
        }

        #region XML Docs
        /// <summary>
        /// Calculates the closest visible point to outOfSightPosition given the currentPosition.
        /// </summary>
        /// <param name="currentPosition">The position with which to test Line Of Sight</param>
        /// <param name="inSightPosition">The connector to the outOfSightPosition with which to find midpoint optimizations.</param>
        /// <param name="outOfSightPosition">The guide to find the optimal in sight position.</param>
        /// <param name="numberOfOptimizations">The number of times we will midpoint optimize, higher means closer to optimal.</param>
        /// <param name="collisionThreshold">Usually the object using the path will be larger than 0, use the size of the collision for testing line of sight.</param>
        /// <param name="collisionMap">Polygon list which we will use for collision (without it, everything is straight line of sight).</param>
        /// <returns></returns>
        #endregion
        public static Vector3 OptimalVisiblePoint(Vector3 currentPosition,
            Vector3 inSightPosition, Vector3 outOfSightPosition, int numberOfOptimizations,
            float collisionThreshold, PositionedObjectList<Polygon> collisionMap)
        {
            Vector3 midpoint = Midpoint(inSightPosition, outOfSightPosition);

            while (numberOfOptimizations > 0)
            {
                if (IsInLineOfSight(currentPosition, midpoint, collisionThreshold, collisionMap))
                { //We see the midpoint
                    inSightPosition = new Vector3(midpoint.X, midpoint.Y, midpoint.Z);
                    midpoint = Midpoint(midpoint, outOfSightPosition);
                }
                else
                { //We can't see the midpoint
                    outOfSightPosition = new Vector3(midpoint.X, midpoint.Y, midpoint.Z);
                    midpoint = Midpoint(inSightPosition, outOfSightPosition);
                }
                numberOfOptimizations--;
            }

            return inSightPosition;
        }

        #region Steering Behavior Methods

        //Find cutoff vector (used for Pursuit steering behavior).
        

        #endregion


    }
}
