using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Math.Geometry
{
    #region XML Docs
    /// <summary>
    /// Represents a segment with defined endpoints which can be used to
    /// graphically display lines or to perform segment collision.
    /// </summary>
    #endregion
    public class Line : PositionedObject, IEquatable<Line>
    {
        #region Fields

        // Victor Chelaru May 17 2008
        // At the time of this writing there are no velocity or rate
        // fields in the Line class that don't exist in the PositionedObject class.
        // Therefore, the Line Pause instruction will simply use the PositionedObject's.
        // If any rate values are added to this class, the Pause method should be updated.  Likely
        // a new Unpause instruction will need to be created.

        // Vic says:  Made internal so that the ShapeManager can perform a very fast removal
        internal bool mVisible;
        Color mColor;

        #region XML Docs
        /// <summary>
        /// The first point used to define the line.  This point is relative to the Line's position and rotation.
        /// <remarks>
        /// If a line moves or rotates it will visibly change on screen and its collisions will be modified, but the 
        /// RelativePoint fields will not be modified.
        /// </remarks>
        /// </summary>
        #endregion
        public Point3D RelativePoint1;

        #region XML Docs
        /// <summary>
        /// The second point used to define the line.  This point is relative to the Line's position and rotation.
        /// </summary>
        /// <remarks>
        /// If a line moves or rotates it will visibly change on screen and its collisions will be modifed, but the
        /// RelativePoint fields will not be modified.
        /// </remarks>
        #endregion
        public Point3D RelativePoint2;

        Point mLastCollisionPoint;

        internal Layer mLayerBelongingTo;

        #endregion

        #region Properties

        public Point3D AbsolutePoint1
        {
            get
            {
                Point3D returnPoint = RelativePoint1;

                FlatRedBall.Math.MathFunctions.TransformVector(
                    ref returnPoint, ref mRotationMatrix);

                returnPoint.X += this.Position.X;
                returnPoint.Y += this.Position.Y;

                return returnPoint;
            }
        }

        public Point3D AbsolutePoint2
        {
            get
            {
                Point3D returnPoint = RelativePoint2;

                FlatRedBall.Math.MathFunctions.TransformVector(
                    ref returnPoint, ref mRotationMatrix);

                returnPoint.X += this.Position.X;
                returnPoint.Y += this.Position.Y;

                return returnPoint;
            }
        }

		public float BoundingRadius
		{
			get
			{
				return (float)System.Math.Max(
				   RelativePoint1.Length(),
				   RelativePoint2.Length());
			}
        }

        #region XML Docs
        /// <summary>
        /// Returns the absolute position of the last collision point.  This will not 
        /// necessarily return the intersection point of the line with the last collided
        /// shape.  It may return a point inside the shape.
        /// </summary>
        #endregion
        public Point LastCollisionPoint
        {
            get { return mLastCollisionPoint; }
            set { mLastCollisionPoint = value; }
        }

        #region Visibility Properties

        #region XML Docs
        /// <summary>
        /// Gets or sets the visibility of this line segment
        /// </summary>
        #endregion
        public bool Visible
        {
            get { return mVisible; }
            set
            {
                if (value != mVisible)
                {
                    mVisible = value;
                    ShapeManager.NotifyOfVisibilityChange(this);
                }
            }
        }


        /// <summary>
        /// Gets or sets the color of this line segment. This color is pre-multiplied, so the R,G,B values must be adjusted accordingly if not fully opaque.
        /// </summary>
        public Color Color
        {
            get => mColor; 
            set=> mColor = value;
        }

        #endregion

        public Vector3 SurfaceVector
        {
            get
            {
                Vector3 returnVector = 
                    new Vector3(
                        (float)(RelativePoint2.X - RelativePoint1.X),
                        (float)(RelativePoint2.Y - RelativePoint1.Y),
                        0);

                FlatRedBall.Math.MathFunctions.TransformVector(
                    ref returnVector, ref mRotationMatrix);

                return returnVector;
            }
        }

        #endregion

        #region Methods

        #region Constructor

        #region XML Docs
        /// <summary>
        /// Instantiates a new Line.
        /// </summary>
        /// <remarks>
        /// Lines created through this method will not be visible or managed.  To add management, add
        /// the line to the ShapeManager through the ShapeManager's AddLine method.
        /// </remarks>
        #endregion
        public Line()
        {
            mColor = Color.White;

            if (SpriteManager.Camera.Orthogonal)
            {
                RelativePoint1.X = -100;
                RelativePoint1.Y = 0;

                RelativePoint2.X = 100;
                RelativePoint2.Y = 0;

            }
            else
            {
                RelativePoint1.X = -1;
                RelativePoint1.Y = 0;

                RelativePoint2.X = 1;
                RelativePoint2.Y = 0;
            }
        }

        #endregion

        #region Public Methods

        #region XML Docs
        /// <summary>
        /// Returns a Segment which represents the line.
        /// </summary>
        /// <returns>The Segment representing the line.</returns>
        #endregion
        public Segment AsSegment()
        {
            Point transformedPoint1 = new Point(RelativePoint1.X, RelativePoint1.Y);
            Point transformedPoint2 = new Point(RelativePoint2.X, RelativePoint2.Y);

            FlatRedBall.Math.MathFunctions.TransformPoint(ref transformedPoint1, ref mRotationMatrix);
            FlatRedBall.Math.MathFunctions.TransformPoint(ref transformedPoint2, ref mRotationMatrix);

            return new Segment(
                new Point(Position.X + transformedPoint1.X,
                          Position.Y + transformedPoint1.Y),
                new Point(Position.X + transformedPoint2.X,
                          Position.Y + transformedPoint2.Y));
        }

        public Ray AsRay()
        {
            Ray ray = new Ray(
                this.AbsolutePoint1.ToVector3(), 
                (this.AbsolutePoint2 - AbsolutePoint1).ToVector3());

            ray.Direction.Normalize();

            return ray;
        }

		public Line Clone()
		{
			Line line = this.Clone<Line>();

			line.mVisible = false;
			line.mLayerBelongingTo = null;

			return line;
		}

        #region CollideAgainst Methods



        #region XML Docs
        /// <summary>
        /// Returns whether this instance collides with the argument Circle.
        /// </summary>
        /// <param name="circle">The Circle to test collision against.</param>
        /// <returns>Whether a collision has occurred.</returns>
        #endregion
        public bool CollideAgainst(Circle circle)
        {
            UpdateDependencies(TimeManager.CurrentTime);
            circle.UpdateDependencies(TimeManager.CurrentTime);

            Segment asSegment = this.AsSegment();
            Segment connectingSegment;

            float distance = asSegment.DistanceTo(
                new Point(ref circle.Position), 
                out connectingSegment);


            if (distance <= circle.Radius)
            {
                // Update Decmeber 17, 2022
                // We are going to do a "closest point" collision
                // since Lines can be used for laser weapons and we 
                // don't want the weapon to poke through the enemy and 
                // hit multiple enemies which overlap:
                // Is the first point inside the circle?
                var circleRadiusSquared = circle.Radius * circle.Radius;

                if((this.Position - circle.Position).LengthSquared() < circleRadiusSquared)
                {
                    this.mLastCollisionPoint.X = this.Position.X;
                    this.mLastCollisionPoint.Y = this.Position.Y;
                }
                else
                {
                    var backAlongLength = (float)System.Math.Sqrt( circleRadiusSquared - (distance * distance)  );
                    var directionBack = (this.RelativePoint2 - this.RelativePoint1).ToVector3().NormalizedOrZero();
                    var collisionPoint = connectingSegment.Point2.ToVector3() - (backAlongLength * directionBack);
                    this.mLastCollisionPoint.X = collisionPoint.X;
                    this.mLastCollisionPoint.Y = collisionPoint.Y;
                }

                Vector3 tangent = asSegment.Point1.ToVector3() - circle.Position;

                circle.mLastCollisionTangent = new Point(tangent.Y, -tangent.X);

                return true;
            }
            else
            {
                return false;
            }

        }

        public bool CollideAgainst(AxisAlignedCube axisAlignedCube)
        {
            BoundingBox boundingBox = axisAlignedCube.AsBoundingBox();


            float? result = this.AsRay().Intersects(boundingBox);

            return result.HasValue && result.Value < this.GetLength();
        }


        /// <summary>
        /// Returns whether this instance collides with the argument AxisAlignedRectangle.
        /// </summary>
        /// <param name="rectangle">The AxisAlignedRectangle to test collision against.</param>
        /// <returns>Whether a collision has occurred.</returns>
        public bool CollideAgainst(AxisAlignedRectangle rectangle)
        {
            UpdateDependencies(TimeManager.CurrentTime);
            rectangle.UpdateDependencies(TimeManager.CurrentTime);

            // Get world-positioned segment
            Segment a = AsSegment();

            // Check if one of the segment's endpoints is inside the rectangle
            Vector3 endpoint = new Vector3(
                (float)a.Point1.X, (float)a.Point1.Y, 0f);
            if (rectangle.IsPointInside(ref endpoint))
            {
                mLastCollisionPoint = new Point(ref endpoint);
                return true;
            }

            endpoint = new Vector3(
                (float)a.Point2.X, (float)a.Point2.Y, 0f);
            if (rectangle.IsPointInside(ref endpoint))
            {
                mLastCollisionPoint = new Point(ref endpoint);
                return true;
            }

            // Check if the segment intersects any of the rectangle's edges
            // Here, prepare rectangle's corner points
            Point tl = new Point(
                rectangle.Position.X - rectangle.ScaleX,
                rectangle.Position.Y + rectangle.ScaleY);
            Point tr = new Point(
                rectangle.Position.X + rectangle.ScaleX,
                rectangle.Position.Y + rectangle.ScaleY);
            Point bl = new Point(
                rectangle.Position.X - rectangle.ScaleX,
                rectangle.Position.Y - rectangle.ScaleY);
            Point br = new Point(
                rectangle.Position.X + rectangle.ScaleX,
                rectangle.Position.Y - rectangle.ScaleY);

            Point tempPoint;


            // Update November 27
            // When a line collides
            // with a rectangle, we often
            // want to know the collision point
            // which should be the "closest". Previously,
            // The order was Top, Left, Bottom, Right. But
            // if the line is below the rectangle, then the
            // top segment gets tested before the bottom segment.
            // To make this slightly better we'll do some simple position
            // checks. This won't solve everything but it's a good middle-ground
            if(rectangle.Position.Y > this.Position.Y)
            {
                // Rectangle is above, so test bottom rectangle edges first
                if(rectangle.Position.X > this.Position.X)
                {
                    if (
                        a.Intersects(new Segment(bl, br), out tempPoint) ||
                        a.Intersects(new Segment(tl, bl), out tempPoint) ||
                        a.Intersects(new Segment(tr, br), out tempPoint) ||
                        a.Intersects(new Segment(tl, tr), out tempPoint) 
                        )
                    {
                        mLastCollisionPoint = tempPoint;
                        return true;
                    }

                }
                else
                {
                    if (
                        a.Intersects(new Segment(bl, br), out tempPoint) ||
                        a.Intersects(new Segment(tr, br), out tempPoint) ||
                        a.Intersects(new Segment(tl, bl), out tempPoint) ||
                        a.Intersects(new Segment(tl, tr), out tempPoint)
                        )
                    {
                        mLastCollisionPoint = tempPoint;
                        return true;
                    }
                }
            }
            else
            {
                // Rectangle is below, so test top rectangle edges first

                if (rectangle.Position.X > this.Position.X)
                {
                    if (a.Intersects(new Segment(tl, tr), out tempPoint) ||
                        a.Intersects(new Segment(tl, bl), out tempPoint) ||
                        a.Intersects(new Segment(tr, br), out tempPoint) ||
                        a.Intersects(new Segment(bl, br), out tempPoint)
                    )
                    {
                        mLastCollisionPoint = tempPoint;
                        return true;
                    }
                }
                else
                {
                    if (a.Intersects(new Segment(tl, tr), out tempPoint) ||
                        a.Intersects(new Segment(tr, br), out tempPoint) ||
                        a.Intersects(new Segment(tl, bl), out tempPoint) ||
                        a.Intersects(new Segment(bl, br), out tempPoint)
                    )
                    {
                        mLastCollisionPoint = tempPoint;
                        return true;
                    }
                }
            }

            // No collision
            return false;
        }

        #region XML Docs
        /// <summary>
        /// Returns whether this instance collides with the argument Line.
        /// </summary>
        /// <param name="line">The Line to test collision against.</param>
        /// <returns>Whether a collision has occurred.</returns>
        #endregion
        public bool CollideAgainst(Line line)
        {
            UpdateDependencies(TimeManager.CurrentTime);
            line.UpdateDependencies(TimeManager.CurrentTime);

            // Get world-positioned segment
            Segment a = AsSegment();

            // Get world-positioned segment
            Segment b = line.AsSegment();
            
            

            // Perform the intersection test
            bool result = a.Intersects(b, out mLastCollisionPoint);
            line.mLastCollisionPoint = mLastCollisionPoint;

            return result;
        }

        static Matrix mIdentityMatrix = Matrix.Identity;
        #region XML Docs
        /// <summary>
        /// Returns whether this instance collides with the argument Polygon.
        /// </summary>
        /// <param name="polygon">The Polygon to test collision against.</param>
        /// <returns>Whether a collision has occurred.</returns>
        #endregion
        public bool CollideAgainst(Polygon polygon)
        {
            UpdateDependencies(TimeManager.CurrentTime);
            polygon.UpdateDependencies(TimeManager.CurrentTime);

            Point transformedPoint1 = new Point(RelativePoint1.X, RelativePoint1.Y);
            Point transformedPoint2 = new Point(RelativePoint2.X, RelativePoint2.Y);


            FlatRedBall.Math.MathFunctions.TransformPoint(ref transformedPoint1, ref mRotationMatrix);
            FlatRedBall.Math.MathFunctions.TransformPoint(ref transformedPoint2, ref mRotationMatrix);

            Point tp1 = new Point(transformedPoint1.X, transformedPoint1.Y);
            Point tp2 = new Point(transformedPoint2.X, transformedPoint2.Y);

            // Get world-positioned segment
            Segment a = new Segment(
                new Point(Position.X + transformedPoint1.X,
                          Position.Y + transformedPoint1.Y),
                new Point(Position.X + transformedPoint2.X,
                          Position.Y + transformedPoint2.Y));

            // Check if one of the segment's endpoints is inside the Polygon
            if (polygon.IsPointInside(ref tp1, ref Position, ref mIdentityMatrix))
            {
                polygon.mLastCollisionPoint = tp1;
                mLastCollisionPoint = tp1;
                return true;
            }
            if (polygon.IsPointInside(ref tp2, ref Position, ref mIdentityMatrix))
            {
                polygon.mLastCollisionPoint = tp2;
                mLastCollisionPoint = tp2;
                return true;
            }

            Point intersectionPoint;

            // Check if one of the polygon's edges intersects the line segment
            for (int i = 0; i < polygon.Points.Count - 1; i++)
            {
                int indexAfter = i + 1;

                var point1 = new Point(polygon.Vertices[i].Position.X,
                                polygon.Vertices[i].Position.Y);

                var point2 = new Point(polygon.Vertices[indexAfter].Position.X,
                                polygon.Vertices[indexAfter].Position.Y);

                if (a.Intersects(new Segment(point1, point2), out intersectionPoint))
                {
                    mLastCollisionPoint = intersectionPoint;
                    polygon.mLastCollisionPoint = intersectionPoint;
                    return true;
                }
            }

            // No collision
            return false;
        }


		public bool CollideAgainst(Capsule2D capsule)
		{
			throw new NotImplementedException("This method hasn't been implemented yet.  Please complain on the FlatRedBall forums.");
		}


		public bool CollideAgainst(ShapeCollection shapeCollection)
		{
			bool didCollide = false;
			for (int i = 0; i < shapeCollection.AxisAlignedRectangles.Count; i++)
			{
				didCollide |= this.CollideAgainst(shapeCollection.AxisAlignedRectangles[i]);
			}

#if DEBUG
            // Vic says:  DO THIS!!!!.......eventually
            if (shapeCollection.Capsule2Ds.Count != 0)
            {
                throw new NotImplementedException();
            }
#endif
			for (int i = 0; i < shapeCollection.Capsule2Ds.Count; i++)
			{
				//this.CollideAgainstMove(shapeCollection.Capsule2Ds[i], thisMass, otherMass);
			}

			for (int i = 0; i < shapeCollection.Circles.Count; i++)
			{
				didCollide |= this.CollideAgainst(shapeCollection.Circles[i]);
			}

			for (int i = 0; i < shapeCollection.Lines.Count; i++)
			{
				didCollide |= this.CollideAgainst(shapeCollection.Lines[i]);
			}

			for (int i = 0; i < shapeCollection.Polygons.Count; i++)
			{
				didCollide |= this.CollideAgainst(shapeCollection.Polygons[i]);
			}

			return didCollide;
		}


		public bool CollideAgainstMove(Capsule2D capsule2D, float thisMass, float otherMass)
		{
            throw new NotImplementedException("This method is not implemented. Capsules are intended only for CollideAgainst - use Polygons for CollideAgainstMove and CollideAgainstBounce");
		}

        #region XML Docs
        /// <summary>
        /// Returns whether this instance collides with the argument Circle.  The two
        /// shapes are also repositioned so they do not collide after the method is called.
        /// </summary>
        /// <param name="circle">The Circle to test collision against.</param>
        /// <param name="thisMass">The mass of this instance.</param>
        /// <param name="otherMass">The mass of the other instance.</param>
        /// <returns>Whether a collision has occurred.</returns>
        #endregion
        public bool CollideAgainstMove(Circle circle, float thisMass, float otherMass)
        {
#if DEBUG
            if (thisMass == 0 && otherMass == 0)
            {
                throw new ArgumentException("Both masses cannot be 0.  For equal masses pick a non-zero value");
            }
#endif
            if (this.CollideAgainst(circle))
            {
                Segment asSegment = AsSegment();
                Vector3 connectingVector;
                
                float distanceTo = asSegment.DistanceTo(
                    new Point(circle.Position.X, circle.Position.Y), 
                    out connectingVector);

                connectingVector.Normalize();

                float amountToMove = circle.Radius - distanceTo;

                this.Position.X += (connectingVector.X * amountToMove) * otherMass / (thisMass + otherMass);
                this.Position.Y += (connectingVector.Y * amountToMove) * otherMass / (thisMass + otherMass);

                circle.LastMoveCollisionReposition.X = -connectingVector.X * amountToMove * thisMass / (thisMass + otherMass);
                circle.LastMoveCollisionReposition.Y = -connectingVector.Y * amountToMove * thisMass / (thisMass + otherMass);

                circle.Position.X += circle.LastMoveCollisionReposition.X;
                circle.Position.Y += circle.LastMoveCollisionReposition.Y;

                return true;
            }

            return false;
        }


		public bool CollideAgainstMove(AxisAlignedRectangle axisAlignedRectangle, float thisMass, float otherMass)
		{
			return axisAlignedRectangle.CollideAgainstMove(this, otherMass, thisMass);
		}


		public bool CollideAgainstMove(Line line, float thisMass, float otherMass)
		{
			throw new NotImplementedException("This method is not implemented.  Complain on the FlatRedBall Forums");
		}

		public bool CollideAgainstMove(Polygon polygon, float thisMass, float otherMass)
		{
			return polygon.CollideAgainstMove(this, otherMass, thisMass);
		}

		public bool CollideAgainstBounce(Line line, float thisMass, float otherMass, float elasticity)
		{
			throw new NotImplementedException();
		}

		public bool CollideAgainstBounce(Capsule2D capsule2D, float thisMass, float otherMass, float elasticity)
		{
			throw new NotImplementedException("This method is not implemented. Capsules are intended only for CollideAgainst - use Polygons for CollideAgainstMove and CollideAgainstBounce");
		}

		public bool CollideAgainstBounce(AxisAlignedRectangle axisAlignedRectangle, float thisMass, float otherMass, float elasticity)
		{
			return axisAlignedRectangle.CollideAgainstBounce(this, otherMass, thisMass, elasticity);
		}

		public bool CollideAgainstBounce(Polygon polygon, float thisMass, float otherMass, float elasticity)
		{
			return polygon.CollideAgainstBounce(this, otherMass, thisMass, elasticity);
		}

		public bool CollideAgainstBounce(Circle circle, float thisMass, float otherMass, float elasticity)
		{
			return circle.CollideAgainstBounce(this, otherMass, thisMass, elasticity);
		}

        #endregion

        public float GetLength()
        {
            double xDifference = RelativePoint2.X - RelativePoint1.X;
            double yDifference = RelativePoint2.Y - RelativePoint1.Y;
            double zDifference = RelativePoint2.Z - RelativePoint1.Z;

            return (float)
                System.Math.Sqrt(
                    xDifference * xDifference + yDifference * yDifference + zDifference * zDifference);

        }


        #region XML Docs
        /// <summary>
        /// Scales the line by a specified amount
        /// </summary>
        /// <param name="scale">The scaling factor</param>
        #endregion
        public void ScaleBy(float scale)
        {
            RelativePoint1.X *= scale;
            RelativePoint1.Y *= scale;
            RelativePoint2.X *= scale;
            RelativePoint2.Y *= scale;
        }


        /// <summary>
        /// Sets the Line's Position to the average of the two given endpoints and
        /// it's RelativePoints to be the distance from the new Position to each of the given Vectors.
        /// </summary>
        /// <param name="endpoint1">The first endpoint for which to find the midpoint</param>
        /// <param name="endpoint2">The second endpoint for which to find the midpoint</param>
        public void SetFromAbsoluteEndpoints(Vector3 endpoint1, Vector3 endpoint2)
        {
            Position = new Vector3((endpoint2.X + endpoint1.X) / 2.0f,
                (endpoint2.Y + endpoint1.Y) / 2.0f,
                (endpoint2.Z + endpoint1.Z) / 2.0f); //Z will most likely always be the same value.

            RelativePoint1 = new Point3D(endpoint1 - Position);
            RelativePoint2 = new Point3D(endpoint2 - Position);
            RotationZ = 0f;
        }

        public enum SetFromEndpointStyle { PositionInCenter, PositionAtEndpoint1 }
        public void SetFromAbsoluteEndpoints(Vector3 endpoint1, Vector3 endpoint2, SetFromEndpointStyle setFromEndpointStyle)
        {
            if(setFromEndpointStyle == SetFromEndpointStyle.PositionInCenter)
            {
                // this uses a center position
                SetFromAbsoluteEndpoints(endpoint1, endpoint2);
            }
            else // PositionAtendpoint1
            {
                RotationZ = 0;
                Position = endpoint1;
                RelativePoint1 = new Point3D();
                RelativePoint2 = new Point3D(endpoint2.X - endpoint1.X, endpoint2.Y - endpoint1.Y);
            }
        }

        public void SetFromAbsoluteEndpoints(Point3D endpoint1, Point3D endpoint2)
        {
            Position = new Vector3((float)(endpoint2.X + endpoint1.X) / 2.0f,
                (float)(endpoint2.Y + endpoint1.Y) / 2.0f,
                (float)(endpoint2.Z + endpoint1.Z) / 2.0f); //Z will most likely always be the same value.

            RelativePoint1 = (endpoint1 - Position);
            RelativePoint2 = (endpoint2 - Position);
            RotationZ = 0f;
        }

        public void FlipRelativePointsHorizontally()
        {
            RelativePoint1.X = -RelativePoint1.X;
            RelativePoint2.X = -RelativePoint2.X;
        }

        // If any line-specific velocity code is added to the Line class then this method
        // should be uncommented and the appropriate code should be added.
        //public override void TimedActivity(float secondDifference,
        //   double secondDifferenceSquaredDividedByTwo, float secondsPassedLastFrame)
        //{
        //    base.TimedActivity(secondDifference, secondDifferenceSquaredDividedByTwo, secondsPassedLastFrame);
        //}


        #endregion

        #endregion

        #region IEquatable<Line> Members

        bool IEquatable<Line>.Equals(Line other)
        {
            return this == other;
        }

        #endregion
    }
}
