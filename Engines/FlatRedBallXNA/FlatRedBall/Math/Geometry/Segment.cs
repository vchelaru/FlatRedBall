using System;
using System.Collections.Generic;
using System.Text;

#if FRB_MDX
using Microsoft.DirectX;
using FlatRedBall.Graphics;
#else// FRB_XNA || SILVERLIGHT
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using VertexPositionColor = Microsoft.Xna.Framework.Graphics.VertexPositionColor;
using Microsoft.Xna.Framework;
#endif

namespace FlatRedBall.Math.Geometry
{
    public struct Segment
    {
        #region Fields

        static Point mPoint;// for speeding up reference calls

        /// <summary>
        /// The first point of the segment.  
        /// </summary>
        public Point Point1;

        #region XML Docs
        /// <summary>
        /// The second point of the segment.
        /// </summary>
        #endregion
        public Point Point2;

        public static Segment ZeroLength = new Segment(0, 0, 0, 0);
        #endregion

        #region Properties

        public double Angle
        {
            get
            {
                return System.Math.Atan2(Point2.Y - Point1.Y, Point2.X - Point1.X);
            }
        }

        #region XML Docs
        /// <summary>
        /// Returns the geometric slope of the segment.
        /// </summary>
        #endregion
        public double Slope
        { 
            get 
            { 
                return (Point2.Y - Point1.Y) / (Point2.X - Point1.X); 
            } 
        }

        #region XML Docs
        /// <summary>
        /// Returns the y intercept of the slope.
        /// </summary>
        /// <remarks>
        /// This method treats the segment as a line, so this will return a value even
        /// though the segment may not cross the x=0 line.
        /// </remarks>
        #endregion
        public double YIntercept
        {
            get 
            { 
                return -Slope * Point1.X + Point1.Y; 
            }
        }

        #endregion

        #region Methods

        #region Constructors
        #region XML Docs
        /// <summary>
        /// Creates a new Segment with the argument points as the endpoints.
        /// </summary>
        /// <param name="point1">The first Point.</param>
        /// <param name="point2">The second Point.</param>
        #endregion
        public Segment(Point point1, Point point2)
        {
            this.Point1 = point1;
            this.Point2 = point2;
        }

        public Segment(ref  VertexPositionColor point1, ref VertexPositionColor point2)
        {
            this.Point1 = new Point(point1.Position.X, point1.Position.Y);
            this.Point2 = new Point(point2.Position.X, point2.Position.Y);
        }

        public Segment( Vector3 point1, Vector3 point2)
        {
            this.Point1 = new Point(point1.X, point1.Y);
            this.Point2 = new Point(point2.X, point2.Y);
        }

        public Segment(double point1X, double point1Y, double point2X, double point2Y)
        {
            this.Point1 = new Point(point1X, point1Y);
            this.Point2 = new Point(point2X, point2Y);
        }

        #endregion

        #region Public Methods

        public Ray AsRay()
        {
            return new Ray(
                Point1.ToVector3(), Point2.ToVector3() - Point1.ToVector3());

        }

        public bool CollideAgainst(Polygon polygon)
        {
            polygon.UpdateDependencies(TimeManager.CurrentTime);

            // Check if one of the segment's endpoints is inside the Polygon
            if (polygon.IsPointInside(Point1.X, Point1.Y))
            {
                polygon.mLastCollisionPoint = Point1;
                return true;
            }                                                  

            if (polygon.IsPointInside(Point2.X, Point2.Y))
            {
                polygon.mLastCollisionPoint = Point2;
                return true;
            }

            Point intersectionPoint;

            // Check if one of the polygon's edges intersects the line segment
            for (int i = 0; i < polygon.Points.Count - 1; i++)
            {
                if (Intersects(new Segment(
                      new Point(polygon.Position.X + polygon.Points[i].X,
                                polygon.Position.Y + polygon.Points[i].Y),
                      new Point(polygon.Position.X + polygon.Points[i + 1].X,
                                polygon.Position.Y + polygon.Points[i + 1].Y)), out intersectionPoint))
                {
                    polygon.mLastCollisionPoint = intersectionPoint;
                    return true;
                }
            }

            // No collision
            return false;
        }

        public Point ClosestPointTo(Point point)
        {
            Vector2 unitSegment = new Vector2((float)(this.Point2.X - this.Point1.X), (float)(Point2.Y - Point1.Y));
            unitSegment.Normalize();

            Vector2 pointVector = new Vector2((float)(point.X - this.Point1.X), (float)(point.Y - Point1.Y));
            //			Segment pointSegment = new Segment( new Point(0,0),
            //				new Point(point.x - this.Point1.X, point.y - Point1.Y));

            float l = Vector2.Dot(pointVector, unitSegment);

            if (l < 0)
            {
                return this.Point1;
            }
            else if (l * l > this.GetLengthSquared())
            {
                return this.Point2;
            }
            else
            {
                Point newPoint = new Point(Point1.X + l * unitSegment.X, Point1.Y + l * unitSegment.Y);
                return newPoint;
            }

        }

        public float DistanceTo(Point point)
        {
            return DistanceTo(point.X, point.Y);
        }
        
        public float DistanceTo(Polygon polygon)
        {
            float finalDistance = float.MaxValue;
            //loop to check all segment vs. polygon vertices.
            foreach (VertexPositionColor v in polygon.Vertices)
            {
                finalDistance = System.Math.Min(finalDistance, DistanceTo(v.Position.X, v.Position.Y));
            }
            //loop to check all segment endpoints vs. polygon edges.
            finalDistance = System.Math.Min(finalDistance, (float)polygon.VectorFrom(Point1.X, Point1.Y).Length());
            finalDistance = System.Math.Min(finalDistance, (float)polygon.VectorFrom(Point2.X, Point2.Y).Length());
            //take the minimum distance and return.
            return finalDistance;
        }

        public float DistanceTo(AxisAlignedRectangle rectangle)
        {
            float finalDistance = float.MaxValue;

            finalDistance = System.Math.Min(finalDistance, DistanceTo(rectangle.Left, rectangle.Top));
            finalDistance = System.Math.Min(finalDistance, DistanceTo(rectangle.Left, rectangle.Bottom));
            finalDistance = System.Math.Min(finalDistance, DistanceTo(rectangle.Right, rectangle.Top));
            finalDistance = System.Math.Min(finalDistance, DistanceTo(rectangle.Right, rectangle.Bottom));

            Segment rectSegment;

            rectSegment = new Segment(rectangle.Left, rectangle.Top, rectangle.Left, rectangle.Bottom);
            finalDistance = System.Math.Min(finalDistance, DistanceTo(rectSegment));
            rectSegment = new Segment(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Top);
            finalDistance = System.Math.Min(finalDistance, DistanceTo(rectSegment));
            rectSegment = new Segment(rectangle.Right, rectangle.Top, rectangle.Right, rectangle.Bottom);
            finalDistance = System.Math.Min(finalDistance, DistanceTo(rectSegment));
            rectSegment = new Segment(rectangle.Left, rectangle.Bottom, rectangle.Right, rectangle.Bottom);
            finalDistance = System.Math.Min(finalDistance, DistanceTo(rectSegment));

            return finalDistance;
        }

        public float DistanceTo(Circle circle)
        {
            float distanceToCenter = DistanceTo(circle.Position);

            return distanceToCenter - circle.Radius;
        }

		public float DistanceTo(Vector3 vector)
		{
			return DistanceTo(vector.X, vector.Y);
		}

        public float DistanceTo(double x, double y)
        {
            float segmentLength = (float)this.GetLength();

            Vector2 normalizedLine = new Vector2(
                (float)(Point2.X - Point1.X) / segmentLength,
                (float)(Point2.Y - Point1.Y) / segmentLength);

            Vector2 pointVector = new Vector2((float)(x - Point1.X), (float)(y - Point1.Y));

            float length = Vector2.Dot(pointVector, normalizedLine);

            if (length < 0)
            {
                return (float)(new Segment(x, y, Point1.X, Point1.Y).GetLength());
            }
            else if (length > segmentLength)
            {
                return (float)(new Segment(x, y, Point2.X, Point2.Y).GetLength());
            }
            else
            {
                normalizedLine.X *= length;
                normalizedLine.Y *= length;

                float xDistanceSquared = pointVector.X - normalizedLine.X;
                xDistanceSquared = xDistanceSquared * xDistanceSquared;

                float yDistanceSquared = pointVector.Y - normalizedLine.Y;
                yDistanceSquared = yDistanceSquared * yDistanceSquared;

                return (float)System.Math.Sqrt(xDistanceSquared + yDistanceSquared);
            }
        }   

        public float DistanceTo(Point point, out Segment connectingSegment)
        {
            double segmentLength = GetLength();

            Point normalizedLine = new Point(
                (Point2.X - Point1.X) / segmentLength,
                (Point2.Y - Point1.Y) / segmentLength);

            Point pointVector = new Point(point.X - Point1.X, point.Y - Point1.Y);

            double length = Point.Dot(pointVector, normalizedLine);

            connectingSegment.Point1 = point;
            if (length < 0)
            {
                connectingSegment.Point2 = Point1;
            }
            else if (length > segmentLength)
            {
                connectingSegment.Point2 = Point2;
            }
            else
            {
                connectingSegment.Point2 = new Point(Point1.X + length * normalizedLine.X,
                              Point1.Y + length * normalizedLine.Y);
            }

            return (float)connectingSegment.GetLength();
        }

        #region XML Docs
        /// <summary>
        /// Returns the distance to the argument point as well as
        /// the connectin Vector3 from the Point to this.
        /// </summary>
        /// <param name="point">The point to get the distance to.</param>
        /// <param name="connectingVector">The connecting vector from the argument Pointn to this.</param>
        /// <returns>The distance between this and the argument Point.</returns>
        #endregion
        public float DistanceTo(Point point, out Vector3 connectingVector)
        {
            connectingVector = new Vector3();
            float segmentLength = (float)GetLength();

            Vector2 normalizedLine = new Vector2(
                (float)(Point2.X - Point1.X) / segmentLength,
                (float)(Point2.Y - Point1.Y) / segmentLength);

            Vector2 pointVector = new Vector2((float)(point.X - Point1.X), (float)(point.Y - Point1.Y));

            float length = Vector2.Dot(pointVector, normalizedLine);

            if (length < 0)
            {
                connectingVector.X = (float)(Point1.X - point.X);
                connectingVector.Y = (float)(Point1.Y - point.Y);
                connectingVector.Z = 0;

                return connectingVector.Length();
            }
            else if (length > segmentLength)
            {
                connectingVector.X = (float)(Point2.X - point.X);
                connectingVector.Y = (float)(Point2.Y - point.Y);
                connectingVector.Z = 0;
                return connectingVector.Length();
            }
            else
            {
                Point tempPoint = new Point(Point1.X + length * normalizedLine.X,
                              Point1.Y + length * normalizedLine.Y);

                connectingVector.X = (float)(tempPoint.X - point.X);
                connectingVector.Y = (float)(tempPoint.Y - point.Y);
                connectingVector.Z = 0;

                return connectingVector.Length();
            }
        }


        public float DistanceTo(Segment otherSegment)
        {
            if (otherSegment.Intersects(this))
            {
                return 0;
            }

            else
            {
                float minDistance = float.PositiveInfinity;

                minDistance = System.Math.Min(minDistance, this.DistanceTo(otherSegment.Point1));
                minDistance = System.Math.Min(minDistance, this.DistanceTo(otherSegment.Point2));

                minDistance = System.Math.Min(minDistance, otherSegment.DistanceTo(this.Point1));
                minDistance = System.Math.Min(minDistance, otherSegment.DistanceTo(this.Point2));

                return minDistance;
            }
        }

        public float DistanceToSquared(ref Vector3 vector, out Segment connectingSegment)
        {
            float segmentLength = (float)this.GetLength();

            Vector2 normalizedLine = new Vector2(
                (float)(Point2.X - Point1.X) / segmentLength,
                (float)(Point2.Y - Point1.Y) / segmentLength);

            Vector2 pointVector = new Vector2((float)(vector.X - Point1.X), (float)(vector.Y - Point1.Y));

            float length = Vector2.Dot(pointVector, normalizedLine);

            if (length < 0)
            {
                connectingSegment.Point1 = new Point(ref vector);
                connectingSegment.Point2 = Point1;

                return (float) connectingSegment.GetLengthSquared();
            }
            else if (length > segmentLength)
            {
                connectingSegment.Point1 = new Point(ref vector);
                connectingSegment.Point2 = Point2;

                return (float) connectingSegment.GetLengthSquared();
            }
            else
            {
                connectingSegment.Point1 = new Point(ref vector);
                connectingSegment.Point2 = new Point(Point1.X + length * normalizedLine.X,
                              Point1.Y + length * normalizedLine.Y);

                return (float)connectingSegment.GetLengthSquared();
            }
        }

        #region XML Docs
        /// <summary>
        /// Returns the length of the segment.
        /// </summary>
        #endregion
        public double GetLength()
        {
            return System.Math.Sqrt((Point2.X - Point1.X) * (Point2.X - Point1.X) + (Point2.Y - Point1.Y) * (Point2.Y - Point1.Y));
        }

        public double GetLengthSquared()
        {
            return (Point2.X - Point1.X) * (Point2.X - Point1.X) + (Point2.Y - Point1.Y) * (Point2.Y - Point1.Y);
        }


        static Vector2 sUnitSegmentForIsClosestPointOnEndpoint;
        static Vector2 sPointVectorForIsClosestPointOnEndpoint;
        #region XML Docs
        /// <summary>
        /// Determines whether the closest point on the segment lies on one of the endpoints.
        /// </summary>
        /// <param name="point">The point to test to.</param>
        /// <returns>Whether the closest point on this segment to the argument point lies on the endpoints.</returns>
        #endregion
        public bool IsClosestPointOnEndpoint(ref Point point)
        {
            sUnitSegmentForIsClosestPointOnEndpoint.X = (float)(this.Point2.X - this.Point1.X);
            sUnitSegmentForIsClosestPointOnEndpoint.Y = (float)(Point2.Y - Point1.Y);
            sUnitSegmentForIsClosestPointOnEndpoint.Normalize();

            sPointVectorForIsClosestPointOnEndpoint.X = (float)(point.X - this.Point1.X);
            sPointVectorForIsClosestPointOnEndpoint.Y = (float)(point.Y - Point1.Y);

#if FRB_MDX
            float l = Vector2.Dot(sPointVectorForIsClosestPointOnEndpoint, sUnitSegmentForIsClosestPointOnEndpoint);
#else
            float l;
            Vector2.Dot(ref sPointVectorForIsClosestPointOnEndpoint, ref sUnitSegmentForIsClosestPointOnEndpoint, out l);
#endif
            return l < 0 || l*l > this.GetLengthSquared();
        }


        public bool IsParallelAndTouching(Segment s2, out Point intersectionPoint)
        {
            double thisAngle = this.Angle;
            double otherAngle = s2.Angle;

            double distance = this.DistanceTo(s2);

            const float maximumAngleVariation = .00001f;
            const double maximumDistance = .00001f;

            intersectionPoint = new Point(double.NaN, double.NaN);

            if (System.Math.Abs(thisAngle - otherAngle) < maximumAngleVariation &&
                distance < maximumDistance)
            {
                if (s2.DistanceTo(this.Point1) < maximumDistance)
                {
                    intersectionPoint = Point1;
                }
                else if (s2.DistanceTo(this.Point2) < maximumDistance)
                {
                    intersectionPoint = Point2;
                }
                else if (this.DistanceTo(s2.Point1) < maximumDistance)
                {
                    intersectionPoint = s2.Point1;
                }
                else// if (this.DistanceTo(s2.Point2) < maximumDistance)
                {
                    intersectionPoint = s2.Point2;
                }

                // They're parallel and touching
                return true;

            }

            return false;
        }
        

        #region XML Docs
        /// <summary>
        /// Determines whether this segment intersects the argument segment.
        /// </summary>
        /// <param name="s2">The segment to test for intersection.</param>
        /// <returns>Whether the segments intersect (whether they cross).</returns>
        #endregion
        public bool Intersects(Segment s2)
        {
            IntersectionPoint(ref s2, out mPoint);

            return !double.IsNaN(mPoint.X);

        }


        public bool Intersects(Segment s2, out Point intersectionPoint)
        {
            IntersectionPoint(ref s2, out intersectionPoint);

            return !double.IsNaN(intersectionPoint.X);
        }

        #region XML Docs
        /// <summary>
        /// Returns the point where this segment intersects the argument segment.
        /// </summary>
        /// <param name="s2">The segment to test for intersection.</param>
        /// <returns>The point where this segment intersects the
        /// argument segment.  If the two segments do not touch, the point
        /// will have both values be double.NAN.
        /// </returns>
        #endregion
        public void IntersectionPoint(ref Segment s2, out Point intersectionPoint)
        {          
            // code borrowed from 
            // http://www.topcoder.com/tc?module=Static&d1=tutorials&d2=geometry2
            double A1 = Point2.Y - Point1.Y;
            double B1 = Point1.X - Point2.X;
            double C1 = A1 * Point1.X + B1 * Point1.Y;


            double A2 = s2.Point2.Y - s2.Point1.Y;
            double B2 = s2.Point1.X - s2.Point2.X;
            double C2 = A2 * s2.Point1.X + B2 * s2.Point1.Y;

            double det = A1 * B2 - A2 * B1;
            if (det == 0)
            {
                //Lines are parallel
                intersectionPoint.X = double.NaN;
                intersectionPoint.Y = double.NaN;
            }
            else
            {
                intersectionPoint.X = (B2 * C1 - B1 * C2) / det;
                intersectionPoint.Y = (A1 * C2 - A2 * C1) / det;

                if (!this.IsClosestPointOnEndpoint(ref intersectionPoint) && 
                    !s2.IsClosestPointOnEndpoint(ref intersectionPoint))
                {
                    // do nothing
                    ;
                }
                else
                {
                    // The closest point is on an endpoint, but we may have a situation where
                    // one segment is touching another one like a T.  If that's the case,
                    // let's still consider it an intersection.
                    double distanceFromThis = this.DistanceTo(intersectionPoint);
                    double distanceFromOther = s2.DistanceTo(intersectionPoint);

                    if (distanceFromOther > .000000001 ||
                        distanceFromThis >  .000000001)
                    {

                        intersectionPoint.X = float.NaN;
                        intersectionPoint.Y = float.NaN;
                    }
                }
            }
             
            
        }


        #region XML Docs
        /// <summary>
        /// Shifts the segment by moving both points by the argument x,y values.
        /// </summary>
        /// <param name="x">The number of units to shift the segment by on the x axis.</param>
        /// <param name="y">The number of units to shift the segment by on the y axis.</param>
        #endregion
        public void MoveBy(float x, float y)
        {
            Point1.X += x;
            Point1.Y += y;
            Point2.X += x;
            Point2.Y += y;
        }


        #region XML Docs
        /// <summary>
        /// Sets the length of the segment to 1 unit by moving the 2nd point.
        /// </summary>
        /// <remarks>
        /// If the segment has 0 length (the endpoints are equal), the method
        /// does not change the segment; length will remain 0.
        /// </remarks>
        #endregion
        public void Normalize()
        {
            float segmentLength =
                (float)GetLength();

            if (segmentLength != 0)
            {
                this.Point2.X = Point1.X + (Point2.X - Point1.X) / segmentLength;
                this.Point2.Y = Point1.Y + (Point2.Y - Point1.Y) / segmentLength;
            }
        }

        public void SetPoints(ref VertexPositionColor point1, ref VertexPositionColor point2)
        {
            this.Point1.X = point1.Position.X;
            this.Point1.Y = point1.Position.Y;

            this.Point2.X = point2.Position.X;
            this.Point2.Y = point2.Position.Y;
        }

        public void SetPoints(float x1, float y1, float x2, float y2)
        {
            this.Point1.X = x1;
            this.Point1.Y = y1;

            this.Point2.X = x2;
            this.Point2.Y = y2;
        }

        public void ScaleBy(float amountToScaleBy)
        {
            Point midpoint = (Point1 + Point2) / 2.0f;

            Point newPoint = Point1 - midpoint;
            newPoint *= amountToScaleBy;
            Point1 = midpoint + newPoint;

            newPoint = Point2 - midpoint;
            newPoint *= amountToScaleBy;
            Point2 = midpoint + newPoint;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(
                 (float)(Point2.X - Point1.X),
                 (float)(Point2.Y - Point1.Y),
                 0);
        }

        public override string ToString()
        {
            return string.Format("Point1: {0}, Point2: {1}", Point1.ToString(), Point2.ToString());
        }

        #endregion // PUBLIC Methods

        #endregion
    }
}
