using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Math.Geometry
{
    #region Enums

    internal enum ContactType
    {
        PointOnPoint,
        SegmentIntersection,
        PointOnSegment
    }

    #endregion

    #region Structs

    internal struct ContactPoint
    {
        public int ThisIndex;
        public int OtherIndex;

        public int ThisEndpoint;
        public int OtherEndpoint;

        public Vector3 Position;
        public ContactType ContactType;
    }

    #endregion

    public static class ShapeMerger
    {
        #region Public Methods

        public static void Merge(Polygon polygon, AxisAlignedRectangle rectangle)
        {
            List<ContactPoint> contactPoints = GetContactPoints(polygon, rectangle);

            int firstPointToStartAt = GetPointToStartAt(polygon, rectangle);
        
            if (firstPointToStartAt == -1)
            {
                throw new NotImplementedException();
                // return a polygon that is the same shape as the rectangle
            }

            List<Vector3> thisVertices = GetAbsoluteVertices(polygon);
            List<Vector3> otherVertices = GetAbsoluteVertices(rectangle);

            SetPointsFromContactPointsAndVertices(polygon, null, contactPoints, firstPointToStartAt, thisVertices, otherVertices);
        }

        #region XML Docs
        /// <summary>
        /// Modifies the first Polygon so that it is the result of both merged polygons.  
        /// This method assumes that the polygons collide and that both are drawn
        /// clockwise.
        /// </summary>
        /// <param name="polygon">The first polygon.  This one will be modified.</param>
        /// <param name="otherPolygon">The second polygon which will not be modified.</param>
        #endregion
        public static void Merge(Polygon polygon, Polygon otherPolygon)
        {
            // Vic says:  This is useful for debugging merging.  Don't remove it!!!
            bool shouldDebug = false;
            Segment[] firstSegments;
            Segment[] secondSegments;
            List<ContactPoint> contactPoints = GetContactPoints(polygon, otherPolygon, out firstSegments, out secondSegments);

#if !MONOGAME

            if (shouldDebug)
            {
                ShapeCollection sc = new ShapeCollection();
                sc.Polygons.Add(polygon);
                sc.Polygons.Add(otherPolygon);

                for (int i = 0; i < contactPoints.Count; i++)
                {
                    Circle circle = new Circle();
                    circle.Radius = .5f;
                    circle.Position = contactPoints[i].Position;

                    if (contactPoints[i].ContactType == ContactType.SegmentIntersection)
                    {
                        circle.Color = Color.Orange;
                    }
                    else
                    {
                        circle.Color = Color.Green;
                    }

                    sc.Circles.Add(circle);

                }



                FlatRedBall.Content.Math.Geometry.ShapeCollectionSave scs =
                    new FlatRedBall.Content.Math.Geometry.ShapeCollectionSave();

                scs.AddPolygonList(sc.Polygons);
                scs.AddCircleList(sc.Circles);

                string fileName = 
                    FlatRedBall.IO.FileManager.MyDocuments + "mergeTest.shcx";

                scs.Save(fileName);

                sc.Clear();
            }
#endif

            int firstPointToStartAt = GetPointToStartAt(polygon, otherPolygon);

            if (firstPointToStartAt == -1)
            {
                throw new NotImplementedException();
                // return a polygon that is the same shape as the rectangle
            }

            List<Vector3> thisVertices = GetAbsoluteVertices(firstSegments);
            List<Vector3> otherVertices = GetAbsoluteVertices(secondSegments);

            SetPointsFromContactPointsAndVertices(polygon, otherPolygon, contactPoints, firstPointToStartAt, thisVertices, otherVertices);
        }

        public static void MergePoints(Polygon polygon, float maximumDistanceForMerge)
        {
            double maximumDistanceSquared = maximumDistanceForMerge * maximumDistanceForMerge;

            List<Point> points = new List<Point>(polygon.Points);

            // Normally it's i > -1, but we're doing > 0 so that we don't process the last point
            for (int i = points.Count - 1; i > 0; i--)
            {
                Point distancePoint = (points[i] - points[i - 1]);

                double distanceSquared = distancePoint.X * distancePoint.X + distancePoint.Y * distancePoint.Y;

                if (distanceSquared < maximumDistanceSquared)
                {

                    points.RemoveAt(i);

                    if (i == points.Count)
                    {
                        points[0] = points[points.Count - 1];
                    }
                }
            }

            if (points.Count != polygon.Points.Count)
            {
                polygon.Points = points;
            }
        }

        public static void EliminateButts(Polygon polygon, float maximumDistanceForSamePoint)
        {
           // return;
            // EARLY OUT

            if (polygon.Points.Count < 2)
            {
                return;
            }


            // END EARLY OUT
            float maxDistanceSquared = maximumDistanceForSamePoint * maximumDistanceForSamePoint;

            List<Point> points = new List<Point>(polygon.Points);
            for (int i = polygon.Points.Count; i > -1; i--)
            {
                int index1 = LoopPoint(points, i);
                int index2 = LoopPoint(points, i + 1);
                int index3 = LoopPoint(points, i + 2);

                Point point1 = points[index1];
                Point point2 = points[index2];
                Point point3 = points[index3];

                if ((point2 - point1).LengthSquared() < maxDistanceSquared)
                {
                    if (index2 != 0 && index2 != points.Count - 1)
                    {
                        points.RemoveAt(index2);
                        i++;
                        continue;
                    }
                }
                if ((point2 - point3).LengthSquared() < maxDistanceSquared)
                {
                    if (index2 != 0 && index2 != points.Count - 1)
                    {
                        points.RemoveAt(index2);
                        i++;
                        continue;
                    }
                }

                // See if the angle from point2 to point 1 is the same as point 3 to point 1.  If so, it's a butt
                double angleFrom2To1 = System.Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
                double angleFrom2To3 = System.Math.Atan2(point2.Y - point3.Y, point2.X - point3.X);
                if (double.IsNaN(angleFrom2To1) || double.IsNaN(angleFrom2To3))
                {
                    // Not sure if this code is used (I'm doing warning cleanup)
                    // but the m value causes warnings.
                    //int m = 3;
                }

                const float epsilon = .01f;

                if(System.Math.Abs(MathFunctions.AngleToAngle(angleFrom2To1, angleFrom2To3)) < epsilon)
                {
                    points.RemoveAt(index2);

                    if (index2 == points.Count)
                    {
                        points[0] = points[points.Count - 1];
                    }
                    if (index2 == 0 && (points[points.Count - 1] - point2).LengthSquared() < epsilon)
                    {
                        if ((point1 - point3).LengthSquared() < epsilon)
                        {
                            points.RemoveAt(points.Count - 1);
                        }
                        else
                        {
                            // we should move to the closest
                            double distanceTo1 = (point2 - point1).LengthSquared();
                            double distanceTo3 = (point2 - point3).LengthSquared();

                            if (distanceTo1 < distanceTo3)
                            {
                                points[points.Count - 1] = point1;
                            }
                            else
                            {
                                points[points.Count - 1] = point3;
                            }

                        }
                    }
                }

            }

            if (points.Count != polygon.Points.Count)
            {
                polygon.Points = points;

                EliminateButts(polygon, maximumDistanceForSamePoint);
            }

        }

        private static int LoopPoint(List<Point> points, int index)
        {
            if (index >= points.Count - 1)
            {
                return index - (points.Count - 1);
            }
            return index;

        }

        #endregion

        #region Private Methods

        private static bool DoesContactPointsHaveThisIndex(int index, List<ContactPoint> contactPoints)
        {
            for (int i = 0; i < contactPoints.Count; i++)
            {
                if (contactPoints[i].ThisIndex == index)
                    return true;
            }

            return false;
        }

        private static bool DoesContactPointsHaveOtherIndex(int index, List<ContactPoint> contactPoints)
        {
            for (int i = 0; i < contactPoints.Count; i++)
            {
                if (contactPoints[i].OtherIndex == index)
                    return true;
            }

            return false;
        }

        private static List<Vector3> GetAbsoluteVertices(AxisAlignedRectangle rectangle)
        {
            List<Vector3> otherVertices = new List<Vector3>(4);
            otherVertices.Add(new Vector3(rectangle.Left, rectangle.Top, 0));
            otherVertices.Add(new Vector3(rectangle.Right, rectangle.Top, 0));
            otherVertices.Add(new Vector3(rectangle.Right, rectangle.Bottom, 0));
            otherVertices.Add(new Vector3(rectangle.Left, rectangle.Bottom, 0));
            return otherVertices;
        }

        private static List<Vector3> GetAbsoluteVertices(Polygon polygon)
        {
            List<Vector3> thisVertices = new List<Vector3>(polygon.mVertices.Length);
            for (int i = 0; i < polygon.mVertices.Length; i++)
            {
                thisVertices.Add(polygon.mVertices[i].Position);
            }
            return thisVertices;
        }

        private static List<Vector3> GetAbsoluteVertices(Segment[] segments)
        {
            List<Vector3> thisVertices = new List<Vector3>(segments.Length + 1);

            thisVertices.Add(new Vector3((float)segments[0].Point1.X, (float)segments[0].Point1.Y, 0));

            for (int i = 0; i < segments.Length; i++)
            {
                thisVertices.Add(new Vector3((float)segments[i].Point2.X, (float)segments[i].Point2.Y, 0));
            }
            return thisVertices;


        }

        private static ContactPoint GetContactPointAtThisIndex(int index, List<ContactPoint> contactPoints, Vector3 closestPoint)
        {
            ContactPoint closestContact = new ContactPoint();

            float closestDistance = float.PositiveInfinity;

            for (int i = 0; i < contactPoints.Count; i++)
            {
                if (contactPoints[i].ThisIndex == index)
                {
                    float distanceSquared = (contactPoints[i].Position - closestPoint).LengthSquared();

                    if (distanceSquared < closestDistance)
                    {
                        closestContact = contactPoints[i];
                        closestDistance = distanceSquared;
                    }

                }
            }

            if (float.IsPositiveInfinity(closestDistance))
            {
                throw new Exception();
            }
            else
            {
                return closestContact;
            }

        }

        private static ContactPoint GetContactPointAtOtherIndex(int index, List<ContactPoint> contactPoints, Vector3 closestPoint)
        {
            ContactPoint closestContact = new ContactPoint();

            float closestDistance = float.PositiveInfinity;


            for (int i = 0; i < contactPoints.Count; i++)
            {
                if (contactPoints[i].OtherIndex == index)
                {

                    float distanceSquared = (contactPoints[i].Position - closestPoint).LengthSquared();
                    if (distanceSquared < closestDistance)
                    {
                        closestContact = contactPoints[i];
                        closestDistance = distanceSquared;
                    }
                }
            }
            if (float.IsPositiveInfinity(closestDistance))
            {
                throw new Exception();
            }
            else
            {
                return closestContact;
            }
        }

        private static List<ContactPoint> GetContactPoints(Polygon polygon, AxisAlignedRectangle rectangle)
        {
            Segment[] firstSegments = GetSegments(polygon);
            Segment[] secondSegments = GetSegments(rectangle);

            List<ContactPoint> contactPoints = GetContactPoints(firstSegments, secondSegments);

            return contactPoints;
        }

        private static void ShiftEndpointsToEliminatePointOnSegment(Segment[] firstSegments, Segment[] secondSegments)
        {
            const double error = .1f ;

            for (int i = 0; i < firstSegments.Length; i++)
            {
                for (int j = 0; j < secondSegments.Length; j++)
                {
                    Point firstPoint = firstSegments[i].Point1;
                    Point secondPoint = firstSegments[i].Point2;

                    if (secondSegments[j].DistanceTo(firstPoint) < error)
                    {
                        Vector3 shiftVector = GetShift(secondSegments, j);

                        Segment segment = firstSegments[i];
                        segment.Point1.X += shiftVector.X;
                        segment.Point1.Y += shiftVector.Y;

                        firstSegments[i] = segment;

                        int index = i - 1;
                        if (i == 0)
                        {
                            index = firstSegments.Length - 1;
                        }
                        segment = firstSegments[index];
                        segment.Point2 = firstSegments[i].Point1;
                        firstSegments[index] = segment;

                    }

                    if (secondSegments[j].DistanceTo(secondPoint) < error)
                    {
                        Vector3 shiftVector = GetShift(secondSegments, j);

                        Segment segment = firstSegments[i];
                        segment.Point2.X += shiftVector.X;
                        segment.Point2.Y += shiftVector.Y;

                        firstSegments[i] = segment;

                        int index = i + 1;

                        if (i == firstSegments.Length - 1)
                        {
                            index = 0;
                        }

                        segment = firstSegments[index];
                        segment.Point1 = firstSegments[i].Point2;
                        firstSegments[index] = segment;
                    }

                    firstPoint = secondSegments[j].Point1;
                    secondPoint = secondSegments[j].Point2;


                    if (firstSegments[i].DistanceTo(firstPoint) < error)
                    {
                        Vector3 shiftVector = GetShift(firstSegments, i);

                        Segment segment = secondSegments[j];
                        segment.Point1.X += shiftVector.X;
                        segment.Point1.Y += shiftVector.Y;

                        secondSegments[j] = segment;

                        int index = j - 1;

                        if (j == 0)
                        {
                            index = secondSegments.Length - 1;
                        }

                        segment = secondSegments[index];
                        segment.Point2 = secondSegments[j].Point1;
                        secondSegments[index] = segment;

                    }

                    if (firstSegments[i].DistanceTo(secondPoint) < error)
                    {
                        Vector3 shiftVector = GetShift(firstSegments, i);

                        Segment segment = secondSegments[j];
                        segment.Point2.X += shiftVector.X;
                        segment.Point2.Y += shiftVector.Y;

                        secondSegments[j] = segment;

                        int index = j + 1;

                        if (j == secondSegments.Length - 1)
                        {
                            index = 0;
                        }

                        segment = secondSegments[index];
                        segment.Point1 = secondSegments[j].Point2;
                        secondSegments[index] = segment;
                    }
                }
            }
        }

        public static float ShiftAmount = .3f;

        private static Vector3 GetShift(Segment[] segmentsTestingAgainst, int index)
        {
            Segment secondSegment = segmentsTestingAgainst[index];

            Vector3 secondAsVector = secondSegment.ToVector3();

            secondAsVector.Normalize();

            float temp = secondAsVector.X;
            secondAsVector.X = -secondAsVector.Y;
            secondAsVector.Y = temp;

            secondAsVector *= ShiftAmount;

            return secondAsVector;
        }

        private static List<ContactPoint> GetContactPoints(Polygon firstPolygon, Polygon secondPolygon,
            out Segment[] firstSegments, out Segment[] secondSegments)
        {
            firstSegments = GetSegments(firstPolygon);
            secondSegments = GetSegments(secondPolygon);

            List<ContactPoint> contactPoints = GetContactPoints(firstSegments, secondSegments);

            return contactPoints;
        }


        static bool shouldEliminatePointOnEnd = true;

        private static List<ContactPoint> GetContactPoints(Segment[] firstSegments, Segment[] secondSegments)
        {

            if (shouldEliminatePointOnEnd)
            {
                ShiftEndpointsToEliminatePointOnSegment(firstSegments, secondSegments);
            }


            Point intersectionPoint = new Point();
            List<ContactPoint> contactPoints = new List<ContactPoint>();

            Segment firstSegment = new Segment();
            Segment secondSegment = new Segment();
            const double allowedError = .0005;

            bool debug = false;

#if !MONOGAME
            if (debug)
            {
                FlatRedBall.Content.Polygon.PolygonSaveList psl = new FlatRedBall.Content.Polygon.PolygonSaveList();

                psl.PolygonSaves.Add(
                    FlatRedBall.Content.Polygon.PolygonSave.FromPolygon(
                        Polygon.FromSegments(firstSegments)));

                psl.PolygonSaves.Add(
                    FlatRedBall.Content.Polygon.PolygonSave.FromPolygon(
                        Polygon.FromSegments(secondSegments)));

                psl.Save(FlatRedBall.IO.FileManager.MyDocuments + "modifiedPolygons.plylstx");

            }
#endif

            #region Handle the two-parallel segment special case

            List<ContactPoint> parallelContactPoints = new List<ContactPoint>();

            for (int firstIndex = 0; firstIndex < firstSegments.Length; firstIndex++)
            {
                firstSegment = firstSegments[firstIndex];
                for (int secondIndex = 0; secondIndex < secondSegments.Length; secondIndex++)
                {
                    secondSegment = secondSegments[secondIndex];

                    Point parallelIntersectionPoint;

                    if (firstSegment.IsParallelAndTouching(secondSegment, out parallelIntersectionPoint))
                    {
                        ContactPoint cp = new ContactPoint();

                        cp.ThisIndex = firstIndex;
                        cp.OtherIndex = secondIndex;

                        // Technically this is a point on segment intersection,
                        // but we treat it like a segment intersection because we're
                        // going to eliminate duplicates
                        cp.ContactType = ContactType.SegmentIntersection;
                        cp.Position = new Vector3((float)parallelIntersectionPoint.X, (float)parallelIntersectionPoint.Y, 0);

                        parallelContactPoints.Add(cp);
                    }
                }
            }

            #endregion

            #region else, do regular intersection tests
            List<int> indexesToRemove = new List<int>();


            for (int firstSegmentIndex = 0; firstSegmentIndex < firstSegments.Length; firstSegmentIndex++)
            {
                firstSegment = firstSegments[firstSegmentIndex];

                for (int secondSegmentIndex = 0; secondSegmentIndex < secondSegments.Length; secondSegmentIndex++)
                {
                    secondSegment = secondSegments[secondSegmentIndex];

                    bool intersects = firstSegment.Intersects(secondSegment, out intersectionPoint);

                    if ( intersects ||
                        firstSegment.IsParallelAndTouching(secondSegment, out intersectionPoint))
                    {
                        ContactPoint cp = new ContactPoint();
                        cp.Position = new Vector3((float)intersectionPoint.X, (float)intersectionPoint.Y, 0);
                        cp.ThisIndex = firstSegmentIndex;
                        cp.OtherIndex = secondSegmentIndex;

                        cp.ThisEndpoint = -1;
                        cp.OtherEndpoint = -1;

                        if(intersectionPoint.EqualdWithin(firstSegment.Point1, allowedError))
                        {
                            cp.ThisEndpoint = firstSegmentIndex;
                        }
                        else if(intersectionPoint.EqualdWithin(firstSegment.Point2, allowedError))
                        {
                            cp.ThisEndpoint = firstSegmentIndex + 1;
                        }
                        else if(intersectionPoint.EqualdWithin(secondSegment.Point1, allowedError))
                        {
                            cp.OtherEndpoint = secondSegmentIndex;
                        }
                        else if(intersectionPoint.EqualdWithin(secondSegment.Point2, allowedError))
                        {
                            cp.OtherEndpoint = secondSegmentIndex + 1;
                        }

                        if (!intersects ||
                             cp.ThisEndpoint != -1 ||
                            cp.OtherEndpoint != -1
                            )
                        {
                            if (shouldEliminatePointOnEnd)
                            {
                                throw new Exception();
                            }
                            cp.ContactType = ContactType.PointOnSegment;
                        }
                        else
                        {
                            cp.ContactType = ContactType.SegmentIntersection;
                        }

                        if (!intersects)
                        {
                            indexesToRemove.Add(contactPoints.Count);
                        }

                        contactPoints.Add(cp);
                    }
                }
            }

#if false // Not sure why this is here, but it causes warnings and we don't use it
            if (false && parallelContactPoints.Count == 2)
            {
                const float minimumLength = .01f;

                bool isParallelContactPoint = false;
                for (int i = 0; i < contactPoints.Count; i++)
                {
                    float closestToPair = float.PositiveInfinity;

                    Vector3 position = contactPoints[i].Position;

                    for (int parallelIndex = 0; parallelIndex < parallelContactPoints.Count; parallelIndex++)
                    {
                        Segment segment1 = firstSegments[parallelContactPoints[parallelIndex].ThisIndex];
                        Segment segment2 = secondSegments[parallelContactPoints[parallelIndex].OtherIndex];

                        float firstDistance = segment1.DistanceTo(position.X, position.Y);
                        float secondDistance = segment2.DistanceTo(position.X, position.Y);

                        float furthestAway = System.Math.Max(firstDistance, secondDistance);

                        closestToPair = System.Math.Min(furthestAway, closestToPair);
                    }

                    if (closestToPair > minimumLength)
                    {
                        isParallelContactPoint = true;
                        break;
                    }
                }

                if (!isParallelContactPoint)
                {
                    return parallelContactPoints;
                }
            }
#endif

            int numberKilled = 0;

            List<int> intersectionIndexes = new List<int>();

            for (int i = 0; i < firstSegments.Length; i++)
            {
                intersectionIndexes.Clear();

                for (int cpIndex = 0; cpIndex < contactPoints.Count; cpIndex++)
                {
                    if (contactPoints[cpIndex].ThisIndex == i)
                    {
                        intersectionIndexes.Add(cpIndex);
                    }
                }

                if (intersectionIndexes.Count > 2 && intersectionIndexes.Count % 2 == 1)
                {
                    contactPoints.RemoveAt(intersectionIndexes[1]);
                }
            }

            for (int i = 0; i < secondSegments.Length; i++)
            {
                intersectionIndexes.Clear();

                for (int cpIndex = 0; cpIndex < contactPoints.Count; cpIndex++)
                {
                    if (contactPoints[cpIndex].OtherIndex == i)
                    {
                        intersectionIndexes.Add(cpIndex);
                    }
                }

                if (intersectionIndexes.Count > 2 && intersectionIndexes.Count % 2 == 1)
                {
                    contactPoints.RemoveAt(intersectionIndexes[1]);
                }
            }


            // See if any contact points are the same.  If so, kill them
            for (int firstCP = 0; firstCP < contactPoints.Count - 1; firstCP++)
            {
                for (int secondCP = 1; secondCP < contactPoints.Count; secondCP++)
                {
                    if (firstCP == secondCP)
                    {
                        continue;
                    }

                    float distanceApart = (contactPoints[firstCP].Position - contactPoints[secondCP].Position).Length();

                    if (distanceApart < .002)
                    {
                        contactPoints.RemoveAt(secondCP);
                        firstCP = -1; // start over.
                        numberKilled++;
                        break;
                    }
                }
            }

            if (contactPoints.Count == 1)
            {
                // Is this bad?  What do we do?
                //int m = 3;
            }

            if (contactPoints.Count % 2 != 0 && contactPoints.Count > 1)
            {
                // Oh, no, an odd number of contact points?  Let's kill the closest pair

                ReactToOddContactPointCount(contactPoints);
            }
            #endregion

            return contactPoints;
        }

        private static void ReactToOddContactPointCount(List<ContactPoint> contactPoints)
        {
            #region First let's see if there is only one PointOnSegment.  If so, kill that one and be on our way.

            int numberOfPointOnSegments = 0;
            int lastPointOnSegmentIndex = -1;
            for (int i = 0; i < contactPoints.Count; i++)
            {
                if (contactPoints[i].ContactType == ContactType.PointOnSegment)
                {
                    numberOfPointOnSegments++;
                    lastPointOnSegmentIndex = i;
                }
            }

            if (numberOfPointOnSegments == 1)
            {
                contactPoints.RemoveAt(lastPointOnSegmentIndex);
                return;
            }

            #endregion

            // Looks like there's more than one PointOnSegment, so remove one in the closest pair.

            int firstClosest = -1;
            int secondClosest = -1;

            float closestPairDistanceSquared = float.PositiveInfinity;

            for (int i = 0; i < contactPoints.Count - 1; i++)
            {
                for (int j = 1; j < contactPoints.Count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    float distanceSquared = (contactPoints[i].Position - contactPoints[j].Position).LengthSquared();

                    if (distanceSquared < closestPairDistanceSquared)
                    {
                        firstClosest = i;
                        secondClosest = j;
                        closestPairDistanceSquared = distanceSquared;
                    }
                }
            }

            // now kill the 2nd one in the closest pair
            contactPoints.RemoveAt(firstClosest);
        }

        private static int GetPointToStartAt(Polygon polygon, AxisAlignedRectangle rectangle)
        {
            int firstPointToStartAt = -1;
            for (int i = 0; i < polygon.mVertices.Length - 1; i++)
            {
                if (!rectangle.IsPointInside(ref polygon.mVertices[i].Position))
                {
                    firstPointToStartAt = i;
                    break;
                }
            }
            return firstPointToStartAt;
        }

        private static int GetPointToStartAt(Polygon polygon, Polygon otherPolygon)
        {
            int firstPointToStartAt = 0;

            double furthestAwayDistance = otherPolygon.VectorFrom(polygon.mVertices[0].Position.X, polygon.mVertices[0].Position.Y).LengthSquared();


            for (int i = 1; i < polygon.mVertices.Length - 1; i++)
            {
                double distance = otherPolygon.VectorFrom(polygon.mVertices[i].Position.X, polygon.mVertices[i].Position.Y).LengthSquared();

                if(distance > furthestAwayDistance &&
                    !otherPolygon.IsPointInside(ref polygon.mVertices[i].Position))
                {
                    firstPointToStartAt = i;
                    furthestAwayDistance = distance;
                }
            }
            return firstPointToStartAt;
        }

        private static Segment[] GetSegments(Polygon polygon)
        {
            polygon.FillVertexArray(false);
            Segment polygonSegment;

            Segment[] segments = new Segment[polygon.Points.Count - 1];

            for (int i = 0; i < polygon.Points.Count - 1; i++)
            {
                Vector3 vectorAtI = polygon.AbsolutePointPosition(i);

                polygonSegment = new Segment(vectorAtI, polygon.AbsolutePointPosition(i + 1));

                segments[i] = polygonSegment;
            }

            return segments;

        }

        private static Segment[] GetSegments(AxisAlignedRectangle rectangle)
        {
            Segment[] rectangleSegments = new Segment[4];

            rectangleSegments[0] = new Segment(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Top); // top
            rectangleSegments[1] = new Segment(rectangle.Right, rectangle.Top, rectangle.Right, rectangle.Bottom); // right
            rectangleSegments[2] = new Segment(rectangle.Right, rectangle.Bottom, rectangle.Left, rectangle.Bottom); // bottom
            rectangleSegments[3] = new Segment(rectangle.Left, rectangle.Bottom, rectangle.Left, rectangle.Top); // left
            return rectangleSegments;
        }

        private static void SetPointsFromContactPointsAndVertices(Polygon polygon, Polygon otherPolygon, List<ContactPoint> contactPoints, int polygonPointToStartAt, List<Vector3> thisVertices, List<Vector3> otherVertices)
        {
            Polygon currentPolygon = polygon;

            List<Vector3> newPolygonPoints = new List<Vector3>();
            int otherIndex = 0;
            int otherIndexToStartAt = -1;
            int thisIndex = polygonPointToStartAt;
            while (true)
            {
                if (newPolygonPoints.Count > 3 * (polygon.Points.Count + otherPolygon.Points.Count))
                {
                    // just break here for now
                    break;
                }

                bool isThereAContactPoint = false;

                if (currentPolygon == polygon)
                {
                    
                    newPolygonPoints.Add(thisVertices[thisIndex]);
                    isThereAContactPoint = DoesContactPointsHaveThisIndex(thisIndex, contactPoints);
                }
                else
                {
                    newPolygonPoints.Add(otherVertices[otherIndex]);
                    isThereAContactPoint = DoesContactPointsHaveOtherIndex(otherIndex, contactPoints);
                }

                if (isThereAContactPoint)
                {
                    ContactPoint cp = new ContactPoint();
                    if (currentPolygon == polygon)
                    {
                        cp = GetContactPointAtThisIndex(thisIndex, contactPoints, thisVertices[thisIndex]);
                    }
                    else
                    {
                        cp = GetContactPointAtOtherIndex(otherIndex, contactPoints, otherVertices[otherIndex]);
                    }

                    #region If it's a segment intersection, drop the point and swap the current polygon

                    if (cp.ContactType == ContactType.SegmentIntersection)
                    {

                        if (currentPolygon == polygon)
                        {
                            currentPolygon = otherPolygon;
                            otherIndex = cp.OtherIndex + 1;

                            if (otherIndex == otherVertices.Count - 1)
                            {
                                otherIndex = 0;
                            }
                            if (otherIndex == otherIndexToStartAt)
                            {
                                newPolygonPoints.Add(newPolygonPoints[0]);
                                break;
                            }

                            if (otherIndexToStartAt == -1)
                            {
                                otherIndexToStartAt = otherIndex;
                            }
                        }
                        else
                        {
                            currentPolygon = polygon;
                            thisIndex = cp.ThisIndex + 1;

                            if (thisIndex == polygon.Points.Count - 1)
                            {
                                thisIndex = 0;
                            }
                            if (thisIndex == polygonPointToStartAt)
                            {
                                // close off the polygon and stop adding points
                                newPolygonPoints.Add(newPolygonPoints[0]);
                                break;
                            }
                        }

                        newPolygonPoints.Add(cp.Position);


                    }
                    #endregion
                    
                    else if(cp.ContactType == ContactType.PointOnSegment)
                    {
                        // See which next point is the furthest away, and decide based off of that.

                        int nextOtherIndex = cp.OtherIndex + 1;
                        if (cp.OtherEndpoint != -1)
                        {
                            nextOtherIndex = cp.OtherEndpoint + 1;
                        }
                        if (nextOtherIndex >= otherVertices.Count - 1)
                        {
                            nextOtherIndex -= (otherVertices.Count - 1);
                        }


                        int nextThisIndex = cp.ThisIndex + 1;
                        if (cp.ThisEndpoint != -1)
                        {
                            nextThisIndex = cp.ThisEndpoint + 1;
                        }
                        if (nextThisIndex >= thisVertices.Count - 1)
                        {
                            nextThisIndex -= (thisVertices.Count - 1);
                        }

                        Vector3 nextThisVector = thisVertices[nextThisIndex] - cp.Position;
                        Vector3 nextOtherVector = otherVertices[nextOtherIndex] - cp.Position;
                        double distanceAwayFromThis = 0;
                        double distanceAwayFromOther = 0;

                        bool doWhile = true;

                        int numberOfExtraDoWhiles = 0;

                        int thisIndexBeforeDoWhile = nextThisIndex;
                        int otherIndexBeforeDoWhile = nextOtherIndex;

                        // The doWhile section will find out which path will take us
                        // furtest away from the other Polygon.  This works most of the time
                        // but in some cases (like if the initial two paths are parallel), we'll
                        // want to special case which we use
                        bool forceUseThis = false;
                        bool forceUseOther = false;

                        while (doWhile)
                        {

                            if (nextThisVector.Length() < .0001f)
                            {
                                nextThisIndex++;
                                if (nextThisIndex == thisVertices.Count - 1)
                                {
                                    nextThisIndex = 0;
                                }
                                nextThisVector = thisVertices[nextThisIndex] - cp.Position;
                            }

                            if (nextOtherVector.Length() < .0001f)
                            {
                                nextOtherIndex++;
                                if (nextOtherIndex == otherVertices.Count - 1)
                                {
                                    nextOtherIndex = 0;
                                }
                                nextOtherVector = otherVertices[nextOtherIndex] - cp.Position;
                            }

                            float thisVectorLength = nextThisVector.Length();
                            float otherVectorLength = nextOtherVector.Length();

                            float smallestDistance = System.Math.Min(thisVectorLength, otherVectorLength);

                            nextThisVector.Normalize();
                            nextOtherVector.Normalize();

                            nextThisVector *= smallestDistance / 2.0f;
                            nextOtherVector *= smallestDistance / 2.0f;

                            nextThisVector += cp.Position;
                            nextOtherVector += cp.Position;

                            if (nextThisVector == nextOtherVector)
                            {

                                forceUseThis = thisVectorLength < otherVectorLength;
                                forceUseOther = !forceUseThis;

                                break;
                            }



                            double minimumDistance = .00001;

                            if (polygon.IsPointInside(ref nextOtherVector))
                            {
                                distanceAwayFromThis = 0;
                            }
                            else
                            {
                                distanceAwayFromThis = polygon.VectorFrom(nextOtherVector.X, nextOtherVector.Y).Length();

                                if (distanceAwayFromThis < minimumDistance)
                                {
                                    distanceAwayFromThis = 0;
                                }
                            }

                            if (otherPolygon.IsPointInside(ref nextThisVector))
                            {
                                distanceAwayFromOther = 0;
                            }
                            else
                            {
                                distanceAwayFromOther = otherPolygon.VectorFrom(nextThisVector.X, nextThisVector.Y).Length();

                                if (distanceAwayFromOther < minimumDistance)
                                {
                                    distanceAwayFromOther = 0;
                                }
                            }

                            if (distanceAwayFromOther == distanceAwayFromThis)
                            {
                                // We need a tiebreaker.  Let's move an extra index and see what happens, shall we?
                                nextThisIndex++;
                                if (nextThisIndex == thisVertices.Count - 1)
                                {
                                    nextThisIndex = 0;
                                }
                                nextThisVector = thisVertices[nextThisIndex] - cp.Position;

                                nextOtherIndex++;
                                if (nextOtherIndex == otherVertices.Count - 1)
                                {
                                    nextOtherIndex = 0;
                                }
                                nextOtherVector = otherVertices[nextOtherIndex] - cp.Position;

                                numberOfExtraDoWhiles++;
                            }
                            else
                            {
                                doWhile = false;
                            }
                        }

                        nextThisIndex = thisIndexBeforeDoWhile;
                        nextOtherIndex = otherIndexBeforeDoWhile;

                        bool useThis = distanceAwayFromThis < distanceAwayFromOther;

                        if (forceUseThis)
                            useThis = true;
                        else if (forceUseOther)
                            useThis = false;

                        if (useThis && currentPolygon == polygon)
                        {
                            // make sure there are no other contact points on the current segment on this
                            for (int i = 0; i < contactPoints.Count; i++)
                            {
                                ContactPoint otherCp = contactPoints[i];

                                if (otherCp.Position != cp.Position && otherCp.ThisIndex == cp.ThisIndex)
                                {
                                    useThis = false;
                                    break;
                                }
                            }
                        }

                        else if (!useThis && currentPolygon == otherPolygon)
                        {
                            // make sure there are no other contact points on the current segment on this
                            for (int i = 0; i < contactPoints.Count; i++)
                            {
                                ContactPoint otherCp = contactPoints[i];

                                if (otherCp.Position != cp.Position && otherCp.OtherIndex == cp.OtherIndex)
                                {
                                    useThis = true;
                                    break;
                                }
                            }
                        }

                        newPolygonPoints.Add(cp.Position);

                        if (distanceAwayFromThis == distanceAwayFromOther)
                        {
                            useThis = currentPolygon == polygon;

                        }
                        if (useThis)
                        {
                            currentPolygon = polygon;

                            thisIndex = nextThisIndex;

                            if (thisIndex == polygonPointToStartAt)
                            {
                                // close off the polygon and stop adding points
                                newPolygonPoints.Add(newPolygonPoints[0]);
                                break;
                            }
                        }
                        else
                        {
                            currentPolygon = otherPolygon;

                            otherIndex = nextOtherIndex;

                            if (otherIndex == otherIndexToStartAt)
                            {
                                newPolygonPoints.Add(newPolygonPoints[0]);
                                break;
                            }                            
                            
                            if (otherIndexToStartAt == -1)
                            {
                                otherIndexToStartAt = otherIndex;
                            }


                        }


                    }
                }
                else
                {
                    if (currentPolygon == polygon)
                    {

                        thisIndex++;
                        if (thisIndex == polygon.Points.Count - 1)
                        {
                            thisIndex = 0;
                        }
                        if (thisIndex == polygonPointToStartAt)
                        {
                            // close off the polygon and stop adding points
                            newPolygonPoints.Add(newPolygonPoints[0]);

                            break;
                        }
                    }
                    else
                    {
                        otherIndex++;
                        if (otherIndex == otherPolygon.Points.Count - 1)
                        {
                            otherIndex = 0;
                        }
                        if (otherIndex == otherIndexToStartAt)
                        {
                            // close off the polygon and stop adding points
                            newPolygonPoints.Add(newPolygonPoints[0]);

                            break;
                        }
                    }
                }
            }

            SetPolygonPoints(polygon, newPolygonPoints);
        }

        private static void SetPolygonPoints(Polygon polygon, List<Vector3> newPolygonPoints)
        {
            Point[] newPoints = new Point[newPolygonPoints.Count];
            Matrix inverseRotationMatrix = Matrix.Invert(polygon.RotationMatrix);
            for (int i = 0; i < newPoints.Length; i++)
            {
                newPolygonPoints[i] -= polygon.Position;

                newPolygonPoints[i] = MathFunctions.TransformVector(newPolygonPoints[i], inverseRotationMatrix);

                newPoints[i] = new Point(newPolygonPoints[i].X, newPolygonPoints[i].Y);
            }

            polygon.Points = newPoints;
        }        

        #endregion
    }
}
