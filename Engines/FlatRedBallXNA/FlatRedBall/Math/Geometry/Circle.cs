using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Graphics;

using FlatRedBall.Gui;
using FlatRedBall.Input;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Color = Microsoft.Xna.Framework.Color;

namespace FlatRedBall.Math.Geometry
{
    public class Circle : PositionedObject, IEquatable<Circle> , IVisible, IMouseOver
    {
        #region Fields

        // made public for performance.
        public float Radius;
        float mRadiusVelocity;
        bool mVisible;
        Color mColor;
        internal Color mPremultipliedColor;

        // Marks the tangent of the surface where
        // the circle last collided against.
        internal Point mLastCollisionTangent;

        public Vector2 LastMoveCollisionReposition;

        internal Layer mLayerBelongingTo;

        #endregion

        #region Properties

		// Used by ShapeCollection
		internal float BoundingRadius
		{
			get { return Radius; }
		}

        #region XML Docs
        /// <summary>
        /// Returns the tangent (in other words the vector parallel with the surface) where the last collision occurred.
        /// </summary>
        #endregion
        public Point LastCollisionTangent
        {
            get
            {
                return mLastCollisionTangent;
            }
        }

        /// <summary>
        /// The (premultiplied alpha) color used to draw this circle.
        /// </summary>
        /// 
        public Color Color
        {
            get { return mColor; }
            set
            {
                mColor = value;
                if(value.A != 255)
                {
                    mPremultipliedColor.A = mColor.A;
                    mPremultipliedColor.R = (byte)(mColor.R * mColor.A / 255);
                    mPremultipliedColor.G = (byte)(mColor.G * mColor.A / 255);
                    mPremultipliedColor.B = (byte)(mColor.B * mColor.A / 255);

                }
                else
                {
                    mPremultipliedColor = mColor;
                }
            }
        }

        public float RadiusVelocity
        {
            get { return mRadiusVelocity; }
            set { mRadiusVelocity = value; }
        }

        public bool Visible
        {
            get { return mVisible; }
            set
            {
                mVisible = value;
                ShapeManager.NotifyOfVisibilityChange(this);
            }
        }

        #endregion

        #region Methods

        #region Constructor

        public Circle()
        {
            Radius = 1;
            Color = Color.White;
        }

        #endregion

        #region Public Methods

		public Circle Clone()
		{
			Circle newCircle = this.Clone<Circle>();

			newCircle.mVisible = false;
			newCircle.mLayerBelongingTo = null;


			return newCircle;
		}

        #region CollideAgainst
        public bool CollideAgainst(Circle circle)
        {
            UpdateDependencies(TimeManager.CurrentTime);
            circle.UpdateDependencies(TimeManager.CurrentTime);

            float differenceX = X - circle.X;
            float differenceY = Y - circle.Y;

            double differenceSquared = differenceX * differenceX + differenceY * differenceY;
            
            return (System.Math.Pow(differenceSquared, .5) - Radius - circle.Radius < 0);
        }

        public bool CollideAgainst(AxisAlignedRectangle rectangle)
        {
            if (mLastDependencyUpdate != TimeManager.CurrentTime)
            {
                UpdateDependencies(TimeManager.CurrentTime);
            }

            if (rectangle.LastDependencyUpdate != TimeManager.CurrentTime)
            {
                rectangle.UpdateDependencies(TimeManager.CurrentTime);
            }
            // first perform a quick test to see if the Circle is too far
            // away from the rectangle
            if ((Position.X + Radius < rectangle.X - rectangle.mScaleX ||
                Position.X - Radius > rectangle.X + rectangle.mScaleX ||
                Position.Y + Radius < rectangle.Y - rectangle.mScaleY ||
                Position.Y - Radius > rectangle.Y + rectangle.mScaleY))
            {
                return false;
            }

            // The simple bounding box test from above will eliminate most
            // cases.  If we get this far, then it is likely that we have a collision
            // and a more in-depth test can be performed.

            // quick test to see if the circle is inside of the rectangle
            if (rectangle.IsPointInside(Position.X, Position.Y))
            {
                mLastCollisionTangent.X = -(Position.Y - rectangle.Position.Y);
                mLastCollisionTangent.Y = (Position.X - rectangle.Position.X);

                return true;

            }

            // if we got this far, the bounding box of the circle touches the rectangle
            // and the center of the circle is not inside of the rectangle.  Perform
            // the most expensive checks now.
            
            Point centerPoint = new Point(Position.X, Position.Y);
            Segment connectingSegment = new Segment();

            int numberOfEndpointsClosestTo = 0;

            // top segment
            Segment segment = new Segment(
                new Point(rectangle.Left, rectangle.Top),
                new Point(rectangle.Right, rectangle.Top));
            if (segment.DistanceTo(centerPoint, out connectingSegment) < Radius)
            {
                if (segment.IsClosestPointOnEndpoint(ref centerPoint))
                {
                    mLastCollisionTangent.X = -(connectingSegment.Point2.Y - Y);
                    mLastCollisionTangent.Y = (connectingSegment.Point2.X - X);
                    numberOfEndpointsClosestTo++;
                }
                else
                {
                    mLastCollisionTangent.X = 1;
                    mLastCollisionTangent.Y = 0;
                    return true;
                }
            }

            // bottom segment
            segment = new Segment(
                new Point(rectangle.Left, rectangle.Bottom),
                new Point(rectangle.Right, rectangle.Bottom));
            if (segment.DistanceTo(centerPoint, out connectingSegment) < Radius)
            {
                if (segment.IsClosestPointOnEndpoint(ref centerPoint))
                {
                    mLastCollisionTangent.X = -(connectingSegment.Point2.Y - Position.Y);
                    mLastCollisionTangent.Y = (connectingSegment.Point2.X - Position.X);

                    numberOfEndpointsClosestTo++;
                }
                else
                {
                    mLastCollisionTangent.X = 1;
                    mLastCollisionTangent.Y = 0;
                    return true;
                }
            }

            // left segment
            segment = new Segment(
                new Point(rectangle.Left, rectangle.Top),
                new Point(rectangle.Left, rectangle.Bottom));
            if (segment.DistanceTo(centerPoint, out connectingSegment) < Radius)
            {
                if (segment.IsClosestPointOnEndpoint(ref centerPoint))
                {
                    mLastCollisionTangent.X = -(connectingSegment.Point2.Y - Position.Y);
                    mLastCollisionTangent.Y = (connectingSegment.Point2.X - Position.X);

                    numberOfEndpointsClosestTo++;
                }
                else
                {
                    mLastCollisionTangent.X = 0;
                    mLastCollisionTangent.Y = 1; 
                    return true;
                }

            }

            // right segment
            segment = new Segment(
                new Point(rectangle.Right, rectangle.Top),
                new Point(rectangle.Right, rectangle.Bottom));
            if (segment.DistanceTo(centerPoint, out connectingSegment) < Radius)
            {
                if (segment.IsClosestPointOnEndpoint(ref centerPoint))
                {
                    mLastCollisionTangent.X = -(connectingSegment.Point2.Y - Position.Y);
                    mLastCollisionTangent.Y = (connectingSegment.Point2.X - Position.X);

                    numberOfEndpointsClosestTo++;
                }
                else
                {
                    mLastCollisionTangent.X = 0;
                    mLastCollisionTangent.Y = 1;
                    return true;
                }
            }

            if (numberOfEndpointsClosestTo > 0)
                return true;
            else // well, this is rare, but it can happen.  Collision failed!
                return false;
        }

        public bool CollideAgainst(Polygon polygon)
        {
            return polygon.CollideAgainst(this);
        }

        public bool CollideAgainst(Line line)
        {
            return line.CollideAgainst(this);
        }

		public bool CollideAgainst(Capsule2D capsule)
		{
            UpdateDependencies(TimeManager.CurrentTime);
            capsule.UpdateDependencies(TimeManager.CurrentTime);

            Circle circle = Capsule2D.CollisionCircle;

            circle.Position = capsule.Endpoint1Position;
            circle.Radius = capsule.EndpointRadius;

            bool collision = circle.CollideAgainst(this);

            if (collision)
            {
                return true;
            }

            circle.Position = capsule.Endpoint2Position;

            collision = circle.CollideAgainst(this);

            if (collision)
            {
                return true;
            }

            Polygon polygon = Capsule2D.CollisionPolygon;

            float right = capsule.Scale - capsule.EndpointRadius;
            float top = capsule.EndpointRadius;

            polygon.SetPoint(0, -right, top);
            polygon.SetPoint(1, right, top);
            polygon.SetPoint(2, right, -top);
            polygon.SetPoint(3, -right, -top);
            polygon.SetPoint(4, -right, top);

            polygon.Position = capsule.Position;
            polygon.RotationMatrix = capsule.RotationMatrix;

            polygon.UpdateDependencies(TimeManager.CurrentTime);

            return this.CollideAgainst(polygon);
        }

        public bool CollideAgainst(ShapeCollection shapeCollection)
        {
            return shapeCollection.CollideAgainst(this);
        }

        public bool CollideAgainstMoveSoft(Circle circle, float thisMass, float otherMass, float separationVelocity)
        {
#if DEBUG
            if (thisMass == 0 && otherMass == 0)
            {
                throw new ArgumentException("Both masses cannot be 0.  For equal masses pick a non-zero value");
            }

            if (this == circle)
            {
                throw new Exception("The circle shouldn't collide with itself!");
            }
#endif
            if (mLastDependencyUpdate != TimeManager.CurrentTime)
            {
                UpdateDependencies(TimeManager.CurrentTime);
            }
            if (circle.mLastDependencyUpdate != TimeManager.CurrentTime)
            {
                circle.UpdateDependencies(TimeManager.CurrentTime);
            }

            float differenceX = circle.Position.X - Position.X;
            float differenceY = circle.Position.Y - Position.Y;

            float differenceSquared =
                differenceX * differenceX + differenceY * differenceY;

            if (differenceSquared < (Radius + circle.Radius) * (Radius + circle.Radius))
            {

                double angle =
                    System.Math.Atan2(Position.Y - circle.Position.Y, Position.X - circle.Position.X);

                double distanceToMove = Radius + circle.Radius - System.Math.Sqrt(differenceSquared);
                float amountToMoveThis = otherMass / (thisMass + otherMass);

                var thisMoveX = (float)(System.Math.Cos(angle) * distanceToMove * amountToMoveThis);
                var thisMoveY = (float)(System.Math.Sin(angle) * distanceToMove * amountToMoveThis);
                var otherMoveX = -(float)(System.Math.Cos(angle) * distanceToMove * (1 - amountToMoveThis));
                var otherMoveY = -(float)(System.Math.Sin(angle) * distanceToMove * (1 - amountToMoveThis));

                if (mParent != null)
                {
                    TopParent.Velocity.X += thisMoveX * separationVelocity * TimeManager.SecondDifference;
                    TopParent.Velocity.Y += thisMoveY * separationVelocity * TimeManager.SecondDifference;
                    ForceUpdateDependencies();
                }
                else
                {
                    Velocity.X += thisMoveX * separationVelocity * TimeManager.SecondDifference;
                    Velocity.Y += thisMoveY * separationVelocity * TimeManager.SecondDifference;
                }

                if (circle.mParent != null)
                {
                    circle.TopParent.Velocity.X += otherMoveX * separationVelocity * TimeManager.SecondDifference;
                    circle.TopParent.Velocity.Y += otherMoveY * separationVelocity * TimeManager.SecondDifference;
                    circle.ForceUpdateDependencies();
                }
                else
                {
                    circle.Velocity.X += otherMoveX * separationVelocity * TimeManager.SecondDifference;
                    circle.Velocity.Y += otherMoveY * separationVelocity * TimeManager.SecondDifference;
                }

                return true;

            }
            else
                return false;
        }

        public bool CollideAgainstMove(Circle circle, float thisMass, float otherMass)
        {
#if DEBUG
            if (thisMass == 0 && otherMass == 0)
            {
                throw new ArgumentException("Both masses cannot be 0.  For equal masses pick a non-zero value");
            }

            if (this == circle)
            {
                throw new Exception("The circle shouldn't collide with itself!");
            }
#endif
            if (mLastDependencyUpdate != TimeManager.CurrentTime)
            {
                UpdateDependencies(TimeManager.CurrentTime);
            }
            if (circle.mLastDependencyUpdate != TimeManager.CurrentTime)
            {
                circle.UpdateDependencies(TimeManager.CurrentTime);
            }

            float differenceX = circle.Position.X - Position.X;
            float differenceY = circle.Position.Y - Position.Y;

            float differenceSquared =
                differenceX * differenceX + differenceY * differenceY;

            if (differenceSquared < (Radius + circle.Radius) * (Radius + circle.Radius))
            {
                
                double angle =
                    System.Math.Atan2(Position.Y - circle.Position.Y, Position.X - circle.Position.X);

                double distanceToMove = Radius + circle.Radius - System.Math.Sqrt(differenceSquared);
                float amountToMoveThis = otherMass / (thisMass + otherMass);

                LastMoveCollisionReposition.X = (float)(System.Math.Cos(angle) * distanceToMove * amountToMoveThis);
                LastMoveCollisionReposition.Y = (float)(System.Math.Sin(angle) * distanceToMove * amountToMoveThis);
                circle.LastMoveCollisionReposition.X = -(float)(System.Math.Cos(angle) * distanceToMove * (1 - amountToMoveThis));
                circle.LastMoveCollisionReposition.Y = -(float)(System.Math.Sin(angle) * distanceToMove * (1 - amountToMoveThis));

                if (mParent != null)
                {
                    TopParent.Position.X += LastMoveCollisionReposition.X;
                    TopParent.Position.Y += LastMoveCollisionReposition.Y;
                    ForceUpdateDependencies();
                }
                else
                {
                    Position.X += LastMoveCollisionReposition.X;
                    Position.Y += LastMoveCollisionReposition.Y;
                }

                if (circle.mParent != null)
                {
                    circle.TopParent.Position.X += circle.LastMoveCollisionReposition.X;
                    circle.TopParent.Position.Y += circle.LastMoveCollisionReposition.Y;
                    circle.ForceUpdateDependencies();
                }
                else
                {
                    circle.Position.X += circle.LastMoveCollisionReposition.X;
                    circle.Position.Y += circle.LastMoveCollisionReposition.Y;
                }
                
                return true;
                  
            }
            else
                return false;
        }

        public bool CollideAgainstMove(AxisAlignedRectangle rectangle, float thisMass, float otherMass)
        {
            // just use the rectangle's call, but reverse the otherMass and thisMass
            return rectangle.CollideAgainstMove(this, otherMass, thisMass);
        }

        public bool CollideAgainstMove(Polygon polygon, float thisMass, float otherMass)
        {
            // use the Polygon's call, but reverse the otherMass and thisMass
            return polygon.CollideAgainstMove(this, otherMass, thisMass);
        }

        public bool CollideAgainstMove(Line line, float thisMass, float otherMass)
        {
            // Use the Line's CollideAgainstMove to avoid code duplication.
            return line.CollideAgainstMove(this, otherMass, thisMass);
        }

		public bool CollideAgainstMove(Capsule2D capsule2D, float thisMass, float otherMass)
		{
            throw new NotImplementedException("This method is not implemented. Capsules are intended only for CollideAgainst - use Polygons for CollideAgainstMove and CollideAgainstBounce");
		}

        public bool CollideAgainstMove(ShapeCollection shapeCollection, float thisMass, float otherMass)
        {
            return shapeCollection.CollideAgainstMove(this, otherMass, thisMass);

        }


		public bool CollideAgainstBounce(Circle circle, float thisMass, float otherMass, float elasticity)
        {
#if DEBUG
            if (thisMass == 0 && otherMass == 0)
            {
                throw new ArgumentException("Both masses cannot be 0.  For equal masses pick a non-zero value");
            }

#endif
            if (CollideAgainstMove(circle, thisMass, otherMass))
            {
                PositionedObject thisTopParent = this.TopParent;
                PositionedObject otherTopParent = circle.TopParent;

                Vector2 collisionNormal = LastMoveCollisionReposition;

                if (otherMass == 0)
                {
                    collisionNormal = -circle.LastMoveCollisionReposition;
                }

                ShapeManager.ApplyBounce(thisTopParent, otherTopParent, thisMass, otherMass, elasticity, ref collisionNormal);

                return true;
            }

            return false;
        }

        public bool CollideAgainstBounce(AxisAlignedRectangle rectangle, float thisMass, float otherMass, float elasticity)
        {
#if DEBUG
            if (thisMass == 0 && otherMass == 0)
            {
                throw new ArgumentException("Both masses cannot be 0.  For equal masses pick a non-zero value");
            }
#endif
            if (CollideAgainstMove(rectangle, thisMass, otherMass))
            {
                PositionedObject thisTopParent = this.TopParent;
                PositionedObject otherTopParent = rectangle.TopParent;

                Vector2 collisionNormal = this.LastMoveCollisionReposition;

                if (otherMass == 0)
                {
                    collisionNormal = rectangle.LastMoveCollisionReposition * -1;
                }

                ShapeManager.ApplyBounce(thisTopParent, otherTopParent, thisMass, otherMass, elasticity, ref collisionNormal);


                return true;
            }
            return false;
        }

        public bool CollideAgainstBounce(Polygon polygon, float thisMass, float otherMass, float elasticity)
        {
#if DEBUG
            if (thisMass == 0 && otherMass == 0)
            {
                throw new ArgumentException("Both masses cannot be 0.  For equal masses pick a non-zero value");
            }
#endif
            if (CollideAgainstMove(polygon, thisMass, otherMass))
            {
                PositionedObject thisTopParent = this.TopParent;
                PositionedObject otherTopParent = polygon.TopParent;

                Vector2 collisionNormal = LastMoveCollisionReposition;
                if (otherMass == 0)
                {
                    collisionNormal.X = -polygon.LastMoveCollisionReposition.X;
                    collisionNormal.Y = -polygon.LastMoveCollisionReposition.Y;
                }

                ShapeManager.ApplyBounce(thisTopParent, otherTopParent, thisMass, otherMass, elasticity, ref collisionNormal);

                return true;
            }

            return false;
        }

        public bool CollideAgainstBounce(ShapeCollection shapeCollection, float thisMass, float otherMass, float elasticity)
        {
            bool didCollide = false;
            for (int i = 0; i < shapeCollection.AxisAlignedRectangles.Count; i++)
            {
                didCollide |= this.CollideAgainstBounce(shapeCollection.AxisAlignedRectangles[i], thisMass, otherMass, elasticity);
            }

#if DEBUG
            // Vic says:  DO THIS!!!!
            if (shapeCollection.Capsule2Ds.Count != 0)
            {
                throw new NotImplementedException();
            }
#endif
            for (int i = 0; i < shapeCollection.Capsule2Ds.Count; i++)
            {
                //this.CollideAgainstBounce(shapeCollection.Capsule2Ds[i], thisMass, otherMass, elasticity);
            }

            for (int i = 0; i < shapeCollection.Circles.Count; i++)
            {
                didCollide |= this.CollideAgainstBounce(shapeCollection.Circles[i], thisMass, otherMass, elasticity);
            }

#if DEBUG
            if (shapeCollection.Lines.Count != 0)
            {
                throw new NotImplementedException();
            }
#endif
            for (int i = 0; i < shapeCollection.Lines.Count; i++)
            {
                //didCollide |= this.CollideAgainstBounce(shapeCollection.Lines[i], thisMass, otherMass, elasticity);
            }

            for (int i = 0; i < shapeCollection.Polygons.Count; i++)
            {
                didCollide |= this.CollideAgainstBounce(shapeCollection.Polygons[i], thisMass, otherMass, elasticity);
            }

            return didCollide;
        }

		public bool CollideAgainstBounce(Line line, float thisMass, float otherMass, float elasticity)
		{
			throw new NotImplementedException();
		}

		public bool CollideAgainstBounce(Capsule2D capsule2D, float thisMass, float otherMass, float elasticity)
		{
			throw new NotImplementedException("This method is not implemented. Capsules are intended only for CollideAgainst - use Polygons for CollideAgainstMove and CollideAgainstBounce");
		}

        #endregion

        #region XML Docs
        /// <summary>
        /// Returns the distance from this to the argument Line.  If this and the Line
        /// are colliding, then the value will be 0 or negative.  The smallest the 
        /// return value can be is -this.Radius.
        /// </summary>
        /// <param name="line">The line to test distance from.</param>
        /// <returns>The distance from the circle to the argument line.  Will be 0 or
        /// negative if the two are colliding.</returns>
        #endregion
        public float DistanceTo(Line line)
        {
            Segment segment = line.AsSegment();

            float distance = segment.DistanceTo(this.X, this.Y);

            return distance - Radius;

        }

        #region IsPointInside

        public bool IsPointInside(ref Vector3 point)
        {
            if (mLastDependencyUpdate != TimeManager.CurrentTime)
            {
                UpdateDependencies(TimeManager.CurrentTime);
            }

            return (point.X - Position.X) * (point.X - Position.X) +
                (point.Y - Position.Y) * (point.Y - Position.Y) <
                Radius * Radius;
        }

        public bool IsPointInside(float x, float y)
        {
            if (mLastDependencyUpdate != TimeManager.CurrentTime)
            {
                UpdateDependencies(TimeManager.CurrentTime);
            }

            return (x - Position.X) * (x - Position.X) +
                (y - Position.Y) * (y - Position.Y) <
                Radius * Radius;
        }

        #endregion

        /// <summary>
        /// Adjusts the calling Circle's position (or its parent if attached) so that the circle is fully-contained
        /// in the argument AxisAlignedRectangle.
        /// </summary>
        /// <param name="otherAxisAlignedRectangle">The rectangle to keep the circle inside of.</param>
        public void KeepThisInsideOf(AxisAlignedRectangle otherAxisAlignedRectangle)
        {
            Vector2 repositionVector;
            repositionVector.X = 0;
            repositionVector.Y = 0;

            this.ForceUpdateDependencies();

            if (this.X - this.Radius < otherAxisAlignedRectangle.X - otherAxisAlignedRectangle.ScaleX)
            {
                repositionVector.X = (otherAxisAlignedRectangle.X - otherAxisAlignedRectangle.ScaleX + this.Radius) - this.X;
            }

            if (this.X + this.Radius > otherAxisAlignedRectangle.X + otherAxisAlignedRectangle.ScaleX)
            {
                repositionVector.X = (otherAxisAlignedRectangle.X + otherAxisAlignedRectangle.ScaleX - this.Radius) - this.X;
            }

            if (this.Y - this.Radius < otherAxisAlignedRectangle.Y - otherAxisAlignedRectangle.ScaleY)
            {
                repositionVector.Y = (otherAxisAlignedRectangle.Y - otherAxisAlignedRectangle.ScaleY + this.Radius) - this.Y;
            }

            if (this.Y + this.Radius > otherAxisAlignedRectangle.Y + otherAxisAlignedRectangle.ScaleY)
            {
                repositionVector.Y = (otherAxisAlignedRectangle.Y + otherAxisAlignedRectangle.ScaleY - this.Radius) - this.Y;
            }

            PositionedObject topParent = this.TopParent;

            topParent.Position.X += repositionVector.X;
            topParent.Position.Y += repositionVector.Y;

        }

        public void KeepThisInsideOf(Polygon polygon)
        {
            UpdateDependencies(TimeManager.CurrentTime);
            polygon.UpdateDependencies(TimeManager.CurrentTime);
            
            Point3D fromCircleToThis = polygon.VectorFrom(Position.X, Position.Y);

            // The fromCircleToThis will be less than circle.Radius units in length.
            // However much less it is is how far the objects should be moved.

            double length = fromCircleToThis.Length();

            double distanceToMove = Radius - length;

            if (!polygon.IsPointInside(ref Position))
            {
                // If the circle falls inside of the shape, then it should be moved
                // outside.  That means moving to the edge of the polygon then also moving out
                // the distance of the radius.
                distanceToMove = -(Radius + length);
            }
            else if (System.Math.Abs(length) > Radius)
            {
                return;
            }

            double amountToMoveOnX = distanceToMove * fromCircleToThis.X / length;
            double amountToMoveOnY = distanceToMove * fromCircleToThis.Y / length;

            float thisMass = 0;
            float otherMass = 1;

            float totalMass = thisMass + otherMass;

            LastMoveCollisionReposition.X = -(float)(amountToMoveOnX * otherMass / totalMass);
            LastMoveCollisionReposition.Y = -(float)(amountToMoveOnY * otherMass / totalMass);

            TopParent.Position.X += LastMoveCollisionReposition.X;
            TopParent.Position.Y += LastMoveCollisionReposition.Y;

            polygon.mLastMoveCollisionReposition.X = (float)((thisMass / totalMass) * amountToMoveOnX);
            polygon.mLastMoveCollisionReposition.Y = (float)((thisMass / totalMass) * amountToMoveOnY);

            polygon.TopParent.Position.X += polygon.mLastMoveCollisionReposition.X;
            polygon.TopParent.Position.Y += polygon.mLastMoveCollisionReposition.Y;

            ForceUpdateDependencies();
            polygon.ForceUpdateDependencies();
        }

        public override void Pause(FlatRedBall.Instructions.InstructionList instructions)
        {
            FlatRedBall.Instructions.Pause.CircleUnpauseInstruction instruction =
                new FlatRedBall.Instructions.Pause.CircleUnpauseInstruction(this);

            instruction.Stop(this);

            instructions.Add(instruction);
        }

        public void ProjectParentVelocityOnLastMoveCollisionTangent()
        {
            ProjectParentVelocityOnLastMoveCollisionTangent(0);
        }

        public void ProjectParentVelocityOnLastMoveCollisionTangent(float minimumVectorLengthSquared)
        {
            if (LastMoveCollisionReposition.LengthSquared() > minimumVectorLengthSquared &&
                Vector2.Dot(
                    new Vector2(TopParent.Velocity.X, TopParent.Velocity.Y), LastMoveCollisionReposition) < 0)
            {
                Vector2 collisionAdjustmentNormalized = LastMoveCollisionReposition;
                collisionAdjustmentNormalized.Normalize();
                float temporaryFloat = collisionAdjustmentNormalized.X;
                collisionAdjustmentNormalized.X = -collisionAdjustmentNormalized.Y;
                collisionAdjustmentNormalized.Y = temporaryFloat;

                float length = Vector2.Dot(
                    new Vector2(TopParent.Velocity.X, TopParent.Velocity.Y), collisionAdjustmentNormalized);
                TopParent.Velocity.X = collisionAdjustmentNormalized.X* length;
                TopParent.Velocity.Y = collisionAdjustmentNormalized.Y * length;
            }
        }

        public override void TimedActivity(float secondDifference,
            double secondDifferenceSquaredDividedByTwo, float lastSecondDifference)
        {
            base.TimedActivity(secondDifference, secondDifferenceSquaredDividedByTwo, lastSecondDifference);

            Radius += (float)(mRadiusVelocity * secondDifference);
        }

        #endregion

        #region Protected Methods



        #endregion

        #endregion


        #region IEquatable<Circle> Members

        bool IEquatable<Circle>.Equals(Circle other)
        {
            return this == other;
        }

        #endregion

        #region IMouseOver Implementation

        public bool IsMouseOver(Cursor cursor)
        {
            return cursor.IsOn3D(this);
        }

        public bool IsMouseOver(Cursor cursor, Layer layer)
        {
            return cursor.IsOn3D(this, layer);
        }

        #endregion

        #region IVisible implementation
        IVisible IVisible.Parent
        {
            get
            {
                return mParent as IVisible;
            }
        }

        public bool AbsoluteVisible
        {
            get
            {
                IVisible iVisibleParent = ((IVisible)this).Parent;
                return mVisible && (iVisibleParent == null || IgnoresParentVisibility || iVisibleParent.AbsoluteVisible);
            }
        }

        public bool IgnoresParentVisibility
        {
            get;
            set;
        }
        #endregion
    }
}
