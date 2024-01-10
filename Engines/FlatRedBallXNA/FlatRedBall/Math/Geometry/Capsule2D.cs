using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Graphics;

#if FRB_MDX
using System.Drawing;
using Microsoft.DirectX;
#else //if FRB_XNA || SILVERLIGHT || WINDOWS_PHONE
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
#endif

namespace FlatRedBall.Math.Geometry
{
    public class Capsule2D : PositionedObject, IEquatable<Capsule2D>
    {
        #region Fields

        internal static Circle CollisionCircle = new Circle();
        internal static Polygon CollisionPolygon = Polygon.CreateRectangle(1, 1);


        // The following two members are internal so the Renderer can access them for speed during rendering.
        internal float mEndpointRadius = .5f;
        internal float mScale = 1;

        bool mVisible;
        Color mColor = Color.White;

        internal Layer mLayerBelongingTo;

        #endregion

        #region Properties

        public float BoundingRadius
        {
            get { return mScale + mEndpointRadius; }
        }

        public Color Color
        {
            get { return mColor; }
            set { mColor = value; }
        }

        public float EndpointRadius
        {
            get { return mEndpointRadius; }
            set { mEndpointRadius = value; }
        }

        public Vector3 Endpoint1Position
        {
            get
            {
                Vector3 returnValue = new Vector3(mScale - mEndpointRadius, 0, 0);
                FlatRedBall.Math.MathFunctions.TransformVector(ref returnValue, ref mRotationMatrix);

                returnValue += Position;

                return returnValue;
            }
        }

        public Vector3 Endpoint2Position
        {
            get
            {
                Vector3 returnValue = new Vector3 ( -mScale + mEndpointRadius, 0, 0);
                FlatRedBall.Math.MathFunctions.TransformVector(ref returnValue, ref mRotationMatrix);

                returnValue += Position;

                return returnValue;
            }
        }

        public float Scale
        {
            get { return mScale; }
            set 
            {
#if DEBUG
                if (value < 0)
                {
                    throw new ArgumentException("Cannot set a negative scale value.  This will cause collision to behave weird.");
                }
#endif
                mScale = value; 
            }
        }

        public float BodyWidth
        {
            get => mScale * 2;
            set => mScale = value * 2;
        }

        public Segment TopSegment
        {
            get
            {
                Point rightPoint = new Point(mScale - mEndpointRadius, mEndpointRadius);
                Point leftPoint = new Point(-mScale + mEndpointRadius, mEndpointRadius);

                FlatRedBall.Math.MathFunctions.TransformPoint(ref leftPoint, ref mRotationMatrix);
                FlatRedBall.Math.MathFunctions.TransformPoint(ref rightPoint, ref mRotationMatrix);

                rightPoint.X += Position.X;
                rightPoint.Y += Position.Y;

                leftPoint.X += Position.X;
                leftPoint.Y += Position.Y;

                return new Segment(leftPoint, rightPoint);
            }
        }

        public Segment BottomSegment
        {
            get
            {
                Point rightPoint = new Point(mScale - mEndpointRadius, -mEndpointRadius);
                Point leftPoint = new Point(-mScale + mEndpointRadius, -mEndpointRadius);

                FlatRedBall.Math.MathFunctions.TransformPoint(ref leftPoint, ref mRotationMatrix);
                FlatRedBall.Math.MathFunctions.TransformPoint(ref rightPoint, ref mRotationMatrix);

                leftPoint.X += Position.X;
                leftPoint.Y += Position.Y;

                rightPoint.X += Position.X;
                rightPoint.Y += Position.Y;

                return new Segment(leftPoint, rightPoint);
            }
        }

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

        #endregion

        #region Methods

        #region Public Methods

		public Capsule2D Clone()
		{
			Capsule2D clonedCapsule = this.Clone<Capsule2D>();

			clonedCapsule.mVisible = false;
			clonedCapsule.mLayerBelongingTo = null;

			return clonedCapsule;
		}

        public bool CollideAgainst(Capsule2D otherCapsule)
        {
            this.UpdateDependencies(TimeManager.CurrentTime);
            otherCapsule.UpdateDependencies(TimeManager.CurrentTime);

            #region Create some initial variables which will be used below
            Vector3 thisEndpoint1 = this.Endpoint1Position;
            Vector3 thisEndpoint2 = this.Endpoint2Position;

            Vector3 otherEndpoint1 = otherCapsule.Endpoint1Position;
            Vector3 otherEndpoint2 = otherCapsule.Endpoint2Position;
            #endregion


            #region Point vs. Point (circle collision)

            float radiiCombinedSquared = (mEndpointRadius + otherCapsule.mEndpointRadius) * (mEndpointRadius + otherCapsule.mEndpointRadius);

            Vector3 difference1 = thisEndpoint1 - otherEndpoint1;
            Vector3 difference2 = thisEndpoint1 - otherEndpoint2;
            Vector3 difference3 = thisEndpoint2 - otherEndpoint1;
            Vector3 difference4 = thisEndpoint2 - otherEndpoint2;
            if (

#if FRB_MDX
                difference1.LengthSq() < radiiCombinedSquared ||
                difference2.LengthSq() < radiiCombinedSquared ||
                difference3.LengthSq() < radiiCombinedSquared ||
                difference4.LengthSq() < radiiCombinedSquared)
#else //if FRB_XNA || SILVERLIGHT || WINDOWS_PHONE
                difference1.LengthSquared() < radiiCombinedSquared ||
                difference2.LengthSquared() < radiiCombinedSquared ||
                difference3.LengthSquared() < radiiCombinedSquared ||
                difference4.LengthSquared() < radiiCombinedSquared)
#endif
            {
                return true;
            }
            #endregion

            #region Segment vs. Segment (bandaid collision)
            Segment thisTopSegment = this.TopSegment;
            Segment thisBottomSegment = this.BottomSegment;

            Segment otherTopSegment = otherCapsule.TopSegment;
            Segment otherBottomSegment = otherCapsule.BottomSegment;
            if (
                thisTopSegment.Intersects(otherTopSegment) ||
                thisTopSegment.Intersects(otherBottomSegment) ||
                thisBottomSegment.Intersects(otherTopSegment) ||
                thisBottomSegment.Intersects(otherBottomSegment))
            {
                return true;
            }
            #endregion

            #region Point vs. Segment (T collision)
            if (
                thisTopSegment.DistanceTo(otherEndpoint1.X, otherEndpoint1.Y) < otherCapsule.mEndpointRadius ||
                thisTopSegment.DistanceTo(otherEndpoint2.X, otherEndpoint2.Y) < otherCapsule.mEndpointRadius ||
                thisBottomSegment.DistanceTo(otherEndpoint1.X, otherEndpoint1.Y) < otherCapsule.mEndpointRadius ||
                thisBottomSegment.DistanceTo(otherEndpoint2.X, otherEndpoint2.Y) < otherCapsule.mEndpointRadius)
            {
                return true;
            }

            if (
                otherTopSegment.DistanceTo(thisEndpoint1.X, thisEndpoint1.Y) < this.mEndpointRadius ||
                otherTopSegment.DistanceTo(thisEndpoint2.X, thisEndpoint2.Y) < this.mEndpointRadius ||
                otherBottomSegment.DistanceTo(thisEndpoint1.X, thisEndpoint1.Y) < this.mEndpointRadius ||
                otherBottomSegment.DistanceTo(thisEndpoint2.X, thisEndpoint2.Y) < this.mEndpointRadius)
            {
                return true;
            }
            #endregion

            #region Endpoint inside Shape (Fully contained collision)

            Point thisEndpoint1Point = new Point(ref thisEndpoint1);
            Point thisEndpoint2Point = new Point(ref thisEndpoint2);

            Point otherEndpoint1Point = new Point(ref otherEndpoint1);
            Point otherEndpoint2Point = new Point(ref otherEndpoint2);

            if (
                MathFunctions.IsPointInsideRectangle(thisEndpoint1Point,
                    otherTopSegment.Point1, otherTopSegment.Point2, otherBottomSegment.Point2, otherBottomSegment.Point1) ||
                MathFunctions.IsPointInsideRectangle(thisEndpoint2Point,
                    otherTopSegment.Point1, otherTopSegment.Point2, otherBottomSegment.Point2, otherBottomSegment.Point1) ||

                MathFunctions.IsPointInsideRectangle(otherEndpoint1Point,
                    thisTopSegment.Point1, thisTopSegment.Point2, thisBottomSegment.Point2, thisBottomSegment.Point1) ||
                MathFunctions.IsPointInsideRectangle(otherEndpoint2Point,
                    thisTopSegment.Point1, thisTopSegment.Point2, thisBottomSegment.Point2, thisBottomSegment.Point1)
                )
            {
                return true;
            }
            #endregion

            // If we got here then the two do not touch.
            return false;
        }

		public bool CollideAgainst(AxisAlignedRectangle axisAlignedRectangle)
		{
			return axisAlignedRectangle.CollideAgainst(this);
		}

		public bool CollideAgainst(Circle circle)
		{
			return circle.CollideAgainst(this);
		}

		public bool CollideAgainst(Line line)
		{
			return line.CollideAgainst(this);
		}

		public bool CollideAgainst(Polygon polygon)
		{
			return polygon.CollideAgainst(this);
		}

        public bool CollideAgainstMove(Capsule2D otherCapsule, float thisMass, float otherMass)
        {
            throw new NotImplementedException("This method is not implemented. Capsules are intended only for CollideAgainst - use Polygons for CollideAgainstMove and CollideAgainstBounce");
        }

		public bool CollideAgainstMove(AxisAlignedRectangle axisAlignedRectangle, float thisMass, float otherMass)
		{
			return axisAlignedRectangle.CollideAgainstMove(this, otherMass, thisMass);
		}

		public bool CollideAgainstMove(Circle circle, float thisMass, float otherMass)
		{
			return circle.CollideAgainstMove(this, otherMass, thisMass);
		}

		public bool CollideAgainstMove(Line line, float thisMass, float otherMass)
		{
			return line.CollideAgainstMove(this, otherMass, thisMass);
		}

		public bool CollideAgainstMove(Polygon polygon, float thisMass, float otherMass)
		{
			return polygon.CollideAgainstMove(this, otherMass, thisMass);
		}

		public bool CollideAgainstBounce(Capsule2D capsule2D, float thisMass, float otherMass, float elasticity)
		{
			throw new NotImplementedException("This method is not implemented. Capsules are intended only for CollideAgainst - use Polygons for CollideAgainstMove and CollideAgainstBounce");
		}

		public bool CollideAgainstBounce(AxisAlignedRectangle axisAlignedRectangle, float thisMass, float otherMass, float elasticity)
		{
			return axisAlignedRectangle.CollideAgainstBounce(this, otherMass, thisMass, elasticity);
		}

		public bool CollideAgainstBounce(Circle circle, float thisMass, float otherMass, float elasticity)
		{
			return circle.CollideAgainstBounce(this, otherMass, thisMass, elasticity);
		}

		public bool CollideAgainstBounce(Line line, float thisMass, float otherMass, float elasticity)
		{
			return line.CollideAgainstBounce(this, otherMass, thisMass, elasticity);
		}

		public bool CollideAgainstBounce(Polygon polygon, float thisMass, float otherMass, float elasticity)
		{
			return polygon.CollideAgainstBounce(this, otherMass, thisMass, elasticity);
		}

        #endregion

        #endregion

        #region IEquatable<Capsule2D> Members

        bool IEquatable<Capsule2D>.Equals(Capsule2D other)
        {
            return this == other;
        }

        #endregion
    }
}
