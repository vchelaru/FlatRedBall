using System;
using System.Collections.Generic;
using System.Text;

using Side = FlatRedBall.Math.Collision.CollisionEnumerations.Side;
using FlatRedBall.Graphics;
using FlatRedBall.Input;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;

using Microsoft.Xna.Framework.Graphics;

using Color = Microsoft.Xna.Framework.Color;


namespace FlatRedBall.Math.Geometry
{
    /// <summary>
    /// A rectangle which can be used to perform collison checks against other shapes. The name "axis aligned"
    /// implies that the rectangle cannot be rotated. Its sides will always be aligned (parallel) with the X and Y
    /// axes. The AxisAlignedRectangle is a shape commonly used in FlatRedBall Entities to perform collision against
    /// other entities and TileShapeCollections.
    /// </summary>
    public class AxisAlignedRectangle : PositionedObject,
        IPositionedSizedObject, IScalable, IEquatable<AxisAlignedRectangle>, IMouseOver, IVisible
    {
        #region Fields
        internal float mScaleX;
        internal float mScaleY;

        float mScaleXVelocity;
        float mScaleYVelocity;

        bool mVisible;

        float mBoundingRadius;

        internal Vector2 mLastMoveCollisionReposition;

        internal Color mColor;

        internal Layer mLayerBelongingTo;

        /// <summary>
        /// Whether collision reposition should consider the full size of the AxisAlignedRectangle. If false,
        /// repositions along one of the directions will only start at the halfway point and move outward.
        /// </summary>
        /// <remarks>
        /// This property is primarily used for "L-shaped" collision areas on tile maps. Tiles which make up the corner
        /// of the L should have this value set to true. Otherwise, collisions may result in an object tunneling through the collision.
        /// </remarks>
        public bool RepositionHalfSize;

        #endregion

        #region Properties

        /// <summary>
        /// The absolute left edge of the AxisAlignedRectangle, calculated by subtracting half of its width from its position;
        /// Setting this value will change its Position.
        /// </summary>
        public float Left
        {
            get => Position.X - mScaleX;
            set => Position.X = value + mScaleX;
        }

        public float Right
        {
            get => Position.X + mScaleX;
            set => Position.X = value - mScaleX;
        }

        public float Top
        {
            get => Position.Y + mScaleY;
            set => Position.Y = value - mScaleY;
        }

        public float Bottom
        {
            get => Position.Y - mScaleY;
            set => Position.Y = value + mScaleY;
        }

        public float RelativeLeft
        {
            get { return RelativePosition.X - mScaleX; }
            set { RelativePosition.X = value + mScaleX; }
        }

        public float RelativeRight
        {
            get { return RelativePosition.X + mScaleX; }
            set { RelativePosition.X = value - mScaleX; }
        }

        public float RelativeTop
        {
            get { return RelativePosition.Y + mScaleY; }
            set { RelativePosition.Y = value - mScaleY; }
        }

        public float RelativeBottom
        {
            get { return RelativePosition.Y - mScaleY; }
            set { RelativePosition.Y = value + mScaleY; }
        }

        public float Width
        {
            get
            {
                return ScaleX * 2;
            }
            set
            {
                ScaleX = value / 2.0f;
            }
        }

        public float Height
        {
            get
            {
                return ScaleY * 2;
            }
            set
            {
                ScaleY = value / 2.0f;
            }
        }

        public float ScaleX
        {
            get { return mScaleX; }
            set
            {
#if DEBUG
                if (value < 0)
                {
                    throw new ArgumentException("Cannot set a negative scale value.  This will cause collision to behave weird.");
                }
#endif
                mScaleX = value;
                mBoundingRadius = (float)(System.Math.Sqrt((mScaleX * mScaleX) + (mScaleY * mScaleY)));
            }
        }

        public float ScaleY
        {
            get { return mScaleY; }
            set
            {
#if DEBUG
                if (value < 0)
                {
                    throw new ArgumentException("Cannot set a negative scale value.  This will cause collision to behave weird.");
                }
#endif
                mScaleY = value;
                mBoundingRadius = (float)(System.Math.Sqrt((mScaleX * mScaleX) + (mScaleY * mScaleY)));
            }
        }

        public float ScaleXVelocity
        {
            get { return mScaleXVelocity; }
            set { mScaleXVelocity = value; }
        }

        public float ScaleYVelocity
        {
            get { return mScaleYVelocity; }
            set { mScaleYVelocity = value; }
        }

        public bool Visible
        {
            get { return mVisible; }
            set
            {
                // This if statement messes up tile map visibility in Glue
                //if (value != mVisible)
                {
                    mVisible = value;
                    ShapeManager.NotifyOfVisibilityChange(this);
                }
            }
        }

        public float BoundingRadius
        {
            get { return mBoundingRadius; }
        }

        public Color Color
        {
            set
            {
                mColor = value;
            }

            get
            {
                return mColor;
            }
        }

        public float Red
        {
            get
            {
                return mColor.R / 255.0f;
            }
            set
            {
#if FRB_MDX
                Color newColor = Color.FromArgb(mColor.A, (byte)(value * 255.0f), mColor.G, mColor.B);
#else
                Color newColor = mColor;
                newColor.R = (byte)(value * 255.0f);
#endif
                mColor = newColor;
            }
        }

        public float Green
        {
            get
            {
                return mColor.G / 255.0f;
            }
            set
            {
#if FRB_MDX
                Color newColor = Color.FromArgb(mColor.A, mColor.R, (byte)(value * 255.0f), mColor.B);
#else
                Color newColor = mColor;
                newColor.G = (byte)(value * 255.0f);
#endif
                mColor = newColor;
            }
        }

        public float Blue
        {
            get
            {
                return mColor.B / 255.0f;
            }
            set
            {
#if FRB_MDX
                Color newColor = Color.FromArgb(mColor.A, mColor.R, mColor.G, (byte)(value * 255.0f));
#else
                Color newColor = mColor;
                newColor.B = (byte)(value * 255.0f);
#endif
                mColor = newColor;
            }
        }

        public RepositionDirections RepositionDirections { get; set; }

        public Vector2 LastMoveCollisionReposition
        {
            get { return mLastMoveCollisionReposition; }
        }

        #region IMouseOver
        bool IMouseOver.IsMouseOver(FlatRedBall.Gui.Cursor cursor)
        {
            return cursor.IsOn3D(this);
        }

        public bool IsMouseOver(FlatRedBall.Gui.Cursor cursor, Layer layer)
        {
            return cursor.IsOn3D(this, layer);
        }
        #endregion

        #endregion

        #region Methods

        #region Constructor

        public AxisAlignedRectangle()
        {
            ScaleX = 1;
            ScaleY = 1;
            mColor = Color.White;
            RepositionDirections = Geometry.RepositionDirections.All;
        }

        public AxisAlignedRectangle(float scaleX, float scaleY)
        {
            RepositionDirections = Geometry.RepositionDirections.All;
            ScaleX = scaleX;
            ScaleY = scaleY;
            mColor = Color.White;

        }

        #endregion

        #region Public Methods

        public AxisAlignedRectangle Clone()
        {
            AxisAlignedRectangle newRectangle = this.Clone<AxisAlignedRectangle>();
            newRectangle.mVisible = false;
            newRectangle.mLayerBelongingTo = null;
            return newRectangle;
        }

        #region CollideAgainst

        /// <summary>
        /// Returns whether this instance collides against the argument Circle.
        /// </summary>
        /// <param name="circle">The Circle to test collision against.</param>
        /// <returns>Whether collision has occurred.</returns>
        public bool CollideAgainst(Circle circle)
        {
            return circle.CollideAgainst(this);
        }

        /// <summary>
        /// Returns whether this instance collides against the argument AxisAlignedRectangle.
        /// </summary>
        /// <param name="rectangle">The AxisAlignedRectangle to test collision against.</param>
        /// <returns>Whether collision has occurred.</returns>
        public bool CollideAgainst(AxisAlignedRectangle rectangle)
        {
            UpdateDependencies(TimeManager.CurrentTime);
            rectangle.UpdateDependencies(TimeManager.CurrentTime);

            return (X - mScaleX < rectangle.X + rectangle.mScaleX &&
                X + mScaleX > rectangle.X - rectangle.mScaleX &&
                Y - mScaleY < rectangle.Y + rectangle.mScaleY &&
                Y + mScaleY > rectangle.Y - rectangle.mScaleY);
        }

        /// <summary>
        /// Returns whether this instance collides against the argument Polygon.
        /// </summary>
        /// <param name="polygon">The Polygon to test collision against.</param>
        /// <returns>Whether collision has occurred.</returns>
        public bool CollideAgainst(Polygon polygon)
        {
            return (polygon.CollideAgainst(this));
        }


        /// <summary>
        /// Returns whether this instance collides against the argument Line.
        /// </summary>
        /// <param name="line">The Line to test collision against.</param>
        /// <returns>Whether collision has occurred.</returns>
        public bool CollideAgainst(Line line)
        {
            return line.CollideAgainst(this);
        }


        /// <summary>
        /// Returns whether this instance collides against the argument Capsule2D.
        /// </summary>
        /// <param name="capsule">The Capsule2D to test collision against.</param>
        /// <returns>Whether collision has occurred.</returns>
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

        /// <summary>
        /// Returns whether this instance collides against the argument ShapeCollection.
        /// </summary>
        /// <param name="shapeCollection">The ShapeCollection to test collision against.</param>
        /// <returns>Whether collision has occurred.</returns>
        public bool CollideAgainst(ShapeCollection shapeCollection)
        {
            return shapeCollection.CollideAgainst(this);
        }

        /// <summary>
        /// Returns whether this intance collides against the argument ICollidable.
        /// </summary>
        /// <param name="collidable">The ICollidable to test collision against.</param>
        /// <returns>Whether collision has occurred.</returns>
        public bool CollideAgainst(ICollidable collidable) => CollideAgainst(collidable.Collision);

        public bool CollideAgainstMove(Circle circle, float thisMass, float otherMass)
        {
#if DEBUG
            if (thisMass == 0 && otherMass == 0)
            {
                throw new ArgumentException("Both masses cannot be 0.  For equal masses pick a non-zero value");
            }
#endif
            if (circle.CollideAgainst(this))
            {
                Point circleCenter = new Point(circle.X, circle.Y);

                if (IsPointOnOrInside(ref circleCenter))
                {
                    double xDistanceToMoveCircle = 0;
                    double yDistanceToMoveCircle = 0;

                    float smallestDistance = float.PositiveInfinity;


                    if ((this.RepositionDirections & Geometry.RepositionDirections.Right) == Geometry.RepositionDirections.Right)
                    {
                        smallestDistance = Right - circle.X;
                        xDistanceToMoveCircle = smallestDistance + circle.Radius;
                    }

                    if ((this.RepositionDirections & Geometry.RepositionDirections.Left) == Geometry.RepositionDirections.Left &&
                        circle.X - Left < smallestDistance)
                    {
                        smallestDistance = circle.X - Left;
                        xDistanceToMoveCircle = -smallestDistance - circle.Radius;
                    }

                    if ((this.RepositionDirections & Geometry.RepositionDirections.Up) == Geometry.RepositionDirections.Up &&
                        Top - circle.Y < smallestDistance)
                    {
                        smallestDistance = Top - circle.Y;
                        xDistanceToMoveCircle = 0;
                        yDistanceToMoveCircle = smallestDistance + circle.Radius;
                    }

                    if ((this.RepositionDirections & Geometry.RepositionDirections.Down) == Geometry.RepositionDirections.Down &&
                        circle.Y - Bottom < smallestDistance)
                    {
                        smallestDistance = circle.Y - Bottom;
                        xDistanceToMoveCircle = 0;
                        yDistanceToMoveCircle = -smallestDistance - circle.Radius;
                    }

                    var shouldApply = RepositionHalfSize == false;
                    if(RepositionHalfSize)
                    {
                        if(RepositionDirections == RepositionDirections.Right || RepositionDirections == RepositionDirections.Left)
                        {
                            shouldApply = System.Math.Abs(smallestDistance) < (circle.Radius + Width / 2);
                        }
                        if(RepositionDirections == RepositionDirections.Up || RepositionDirections == RepositionDirections.Down)
                        {
                            shouldApply = System.Math.Abs(smallestDistance) < (circle.Radius + Height / 2);
                        }

                    }

                    if(shouldApply)
                    {
                        float amountToMoveThis = otherMass / (thisMass + otherMass);

                        mLastMoveCollisionReposition.X = (float)(-xDistanceToMoveCircle * amountToMoveThis);
                        mLastMoveCollisionReposition.Y = (float)(-yDistanceToMoveCircle * amountToMoveThis);

                        TopParent.X += mLastMoveCollisionReposition.X;
                        TopParent.Y += mLastMoveCollisionReposition.Y;

                        circle.LastMoveCollisionReposition.X = (float)(xDistanceToMoveCircle * (1 - amountToMoveThis));
                        circle.LastMoveCollisionReposition.Y = (float)(yDistanceToMoveCircle * (1 - amountToMoveThis));
                        circle.mLastCollisionTangent = new Point(circle.LastMoveCollisionReposition.Y,
                            -circle.LastMoveCollisionReposition.X);

                        circle.TopParent.Position.X += circle.LastMoveCollisionReposition.X;
                        circle.TopParent.Position.Y += circle.LastMoveCollisionReposition.Y;

                        ForceUpdateDependencies();
                        circle.ForceUpdateDependencies();
                    }

                    return true;
                }
                else
                {

                    Segment collisionSegment = new Segment();
                    // top
                    Segment edge = new Segment();
                    float smallestDistance = float.PositiveInfinity;
#if FRB_MDX
                    Vector2 amountToMove = Vector2.Empty;
#else
                    Vector2 amountToMove = Vector2.Zero;
#endif
                    bool isAmountToMoveSet = false;

                    if ((this.RepositionDirections & Geometry.RepositionDirections.Up) == Geometry.RepositionDirections.Up)
                    {
                        bool shouldUseInfiniteSegment = (circleCenter.X < Position.X &&
                            (this.RepositionDirections & Geometry.RepositionDirections.Left) != Geometry.RepositionDirections.Left) ||
                            (circleCenter.X > Position.X &&
                            (this.RepositionDirections & Geometry.RepositionDirections.Right) != Geometry.RepositionDirections.Right);

                        if (shouldUseInfiniteSegment)
                        {
                            float candidate = Top + circle.Radius - circle.Position.Y;
                            if (this.RepositionHalfSize)
                            {
                                var maxMove = circle.Radius + Height / 2;
                                if (System.Math.Abs(candidate) > maxMove)
                                {
                                    // no movement allowed
                                    candidate = float.PositiveInfinity;
                                }
                            }

                            if (System.Math.Abs(candidate) < System.Math.Abs(smallestDistance))

                            {
                                smallestDistance = candidate;


                                isAmountToMoveSet = true;

                                amountToMove.X = 0;
                                amountToMove.Y = -smallestDistance;

                            }
                        }
                        else
                        {
                            // Maybe we can save by not calling "new"
                            edge = new Segment(
                                new Point(this.Left, this.Top),
                                new Point(this.Right, this.Top));
                            smallestDistance = edge.DistanceTo(circleCenter, out collisionSegment);
                        }
                    }

                    if ((this.RepositionDirections & Geometry.RepositionDirections.Down) == Geometry.RepositionDirections.Down)
                    {

                        bool shouldUseInfiniteSegment = (circleCenter.X < Position.X &&
                            (this.RepositionDirections & Geometry.RepositionDirections.Left) != Geometry.RepositionDirections.Left) ||
                            (circleCenter.X > Position.X &&
                            (this.RepositionDirections & Geometry.RepositionDirections.Right) != Geometry.RepositionDirections.Right);


                        if (shouldUseInfiniteSegment)
                        {
                            float candidate = Bottom - circle.Radius - circle.Position.Y;

                            if (this.RepositionHalfSize)
                            {
                                var maxMove = circle.Radius + Height / 2;
                                if (System.Math.Abs(candidate) > maxMove)
                                {
                                    // no movement allowed
                                    candidate = float.PositiveInfinity;
                                }
                            }


                            if (System.Math.Abs(candidate) < System.Math.Abs(smallestDistance))
                            {

                                smallestDistance = candidate;
                                isAmountToMoveSet = true;

                                amountToMove.X = 0;
                                amountToMove.Y = -smallestDistance;
                            }
                        }
                        else
                        {
                            // bottom
                            edge = new Segment(
                                new Point(Left, Bottom),
                                new Point(Right, Bottom));

                            if (edge.DistanceTo(circleCenter) < smallestDistance)
                            {
                                smallestDistance = (float)edge.DistanceTo(circleCenter, out collisionSegment);
                            }
                        }
                    }


                    if ((this.RepositionDirections & Geometry.RepositionDirections.Left) == Geometry.RepositionDirections.Left)
                    {
                        bool shouldUseInfiniteSegment = (circleCenter.Y < Position.Y &&
                            (this.RepositionDirections & Geometry.RepositionDirections.Down) != Geometry.RepositionDirections.Down) ||
                            (circleCenter.Y > Position.Y &&
                            (this.RepositionDirections & Geometry.RepositionDirections.Up) != Geometry.RepositionDirections.Up);

                        if (shouldUseInfiniteSegment)
                        {
                            float candidate = Left - circle.Radius - circle.Position.X;


                            if (this.RepositionHalfSize)
                            {
                                var maxMove = circle.Radius + Width / 2;
                                if (System.Math.Abs(candidate) > maxMove)
                                {
                                    // no movement allowed
                                    candidate = float.PositiveInfinity;
                                }
                            }

                            if (System.Math.Abs(candidate) < System.Math.Abs(smallestDistance))
                            {
                                smallestDistance = candidate;

                                isAmountToMoveSet = true;
                                amountToMove.Y = 0;
                                amountToMove.X = -smallestDistance;
                            }
                        }
                        else
                        {

                            // left
                            edge = new Segment(
                                new Point(Left, Top),
                                new Point(Left, Bottom));
                            if (edge.DistanceTo(circleCenter) < smallestDistance)
                            {
                                smallestDistance = (float)edge.DistanceTo(circleCenter, out collisionSegment);
                            }
                        }
                    }

                    if ((this.RepositionDirections & Geometry.RepositionDirections.Right) == Geometry.RepositionDirections.Right)
                    {
                        bool shouldUseInfiniteSegment = (circleCenter.Y < Position.Y &&
                            (this.RepositionDirections & Geometry.RepositionDirections.Down) != Geometry.RepositionDirections.Down) ||
                            (circleCenter.Y > Position.Y &&
                            (this.RepositionDirections & Geometry.RepositionDirections.Up) != Geometry.RepositionDirections.Up);

                        if (shouldUseInfiniteSegment)
                        {
                            float candidate = Right + circle.Radius - circle.Position.X;

                            if (System.Math.Abs(candidate) < System.Math.Abs(smallestDistance))
                            {

                                smallestDistance = candidate;

                                isAmountToMoveSet = true;
                                amountToMove.Y = 0;
                                amountToMove.X = -smallestDistance;
                            }

                        }
                        else
                        {

                            // right
                            edge = new Segment(
                                new Point(Right, Top),
                                new Point(Right, Bottom));
                            if (edge.DistanceTo(circleCenter) < smallestDistance)
                            {
                                smallestDistance = (float)edge.DistanceTo(circleCenter, out collisionSegment);
                                //				edgeClosestTo = "right";
                            }
                        }
                    }

                    if (smallestDistance <= circle.Radius)
                    {
                        float remainingDistance = (float)circle.Radius - smallestDistance;
                        if (!isAmountToMoveSet)
                        {
                            amountToMove = new Vector2((float)(collisionSegment.Point2.X - collisionSegment.Point1.X),
                                (float)(collisionSegment.Point2.Y - collisionSegment.Point1.Y));
                            amountToMove.Normalize();
                            amountToMove = amountToMove * remainingDistance;
                        }

                        float amountToMoveThis = otherMass / (thisMass + otherMass);

                        mLastMoveCollisionReposition.X = amountToMove.X * amountToMoveThis;
                        mLastMoveCollisionReposition.Y = amountToMove.Y * amountToMoveThis;

                        TopParent.X += mLastMoveCollisionReposition.X;
                        TopParent.Y += mLastMoveCollisionReposition.Y;

                        circle.LastMoveCollisionReposition.X = -amountToMove.X * (1 - amountToMoveThis);
                        circle.LastMoveCollisionReposition.Y = -amountToMove.Y * (1 - amountToMoveThis);

                        circle.TopParent.Position.X += circle.LastMoveCollisionReposition.X;
                        circle.TopParent.Position.Y += circle.LastMoveCollisionReposition.Y;

                        ForceUpdateDependencies();
                        circle.ForceUpdateDependencies();

                        return true;
                    }
                    else
                        return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns whether this AxisAlignedRectangle and the argument AxisAlignedRectangle overkap,
        /// and reposition sthe two according ot their relative masses.
        /// </summary>
        /// <param name="rectangle">The other rectangle to collide against.</param>
        /// <param name="thisMass">This mass relative to the other rectangle.</param>
        /// <param name="otherMass">The other rectangle's mass relative to this.</param>
        /// <returns>Whether collision has occurred.</returns>
        public bool CollideAgainstMove(AxisAlignedRectangle rectangle, float thisMass, float otherMass)
        {
#if DEBUG
            if (thisMass == 0 && otherMass == 0)
            {
                throw new ArgumentException("Both masses cannot be 0.  For equal masses pick a non-zero value");
            }
#endif
            if (CollideAgainst(rectangle))
            {
                Side side = Side.Left; // set it to left first
                float smallest = float.PositiveInfinity;
                float currentDistance;

                if ((this.RepositionDirections & Geometry.RepositionDirections.Right) == Geometry.RepositionDirections.Right &&
                    (rectangle.RepositionDirections & Geometry.RepositionDirections.Left) == Geometry.RepositionDirections.Left)
                {
                    smallest = System.Math.Abs(X + mScaleX - (rectangle.X - rectangle.mScaleX));
                }

                if ((this.RepositionDirections & Geometry.RepositionDirections.Left) == Geometry.RepositionDirections.Left &&
                    (rectangle.RepositionDirections & Geometry.RepositionDirections.Right) == Geometry.RepositionDirections.Right)
                {
                    currentDistance = System.Math.Abs(X - mScaleX - (rectangle.X + rectangle.mScaleX));
                    if (currentDistance < smallest) { smallest = currentDistance; side = Side.Right; }
                }

                if ((this.RepositionDirections & Geometry.RepositionDirections.Down) == Geometry.RepositionDirections.Down &&
                    (rectangle.RepositionDirections & Geometry.RepositionDirections.Up) == Geometry.RepositionDirections.Up)

                {
                    currentDistance = Y - mScaleY - (rectangle.Y + rectangle.mScaleY);
                    if (currentDistance < 0) currentDistance *= -1;
                    if (currentDistance < smallest) { smallest = currentDistance; side = Side.Top; }
                }

                if ((this.RepositionDirections & Geometry.RepositionDirections.Up) == Geometry.RepositionDirections.Up &&
                    (rectangle.RepositionDirections & Geometry.RepositionDirections.Down) == Geometry.RepositionDirections.Down)
                {

                    currentDistance = Y + mScaleY - (rectangle.Y - rectangle.mScaleY);
                    if (currentDistance < 0) currentDistance *= -1;
                    if (currentDistance < smallest) { smallest = currentDistance; side = Side.Bottom; }
                }


                bool shouldApplyReposition = float.IsPositiveInfinity(smallest) == false;
                // this is kinda crappy but addresses this bug:
                // https://trello.com/c/twwOTKFz/411-l-shaped-corners-can-cause-entities-to-teleport-through-despite-using-proper-reposition-directions
                float? maxMovement = null;
                // Update Feb 14, 2021
                // The fix for the trello card above causes problems with objects inside long strips. For example, consider 
                // a collision as follows:
                // OOOOOOOOOOOOOOOOOOOOO
                //      X
                // OOOOOOOOOOOOOOOOOOOOO
                // In this case, the rectangle in the middle would never get pushed out because the rectangles
                // above and below only allow moving a max of half the width or height 
                // The card above is specifically about L-shaped collision areas when colliding with the corner
                // Therefore, we could restrict this to only L shaped reposition rectangles. 
                // Eventually we may even want L shaped corners to push out fully, like in the case of a solid block
                // that always pushes out. But that's a bigger change that requires modifying the RepositionDirections to have
                // an extra value for whether it's a full or half movement, and then modifying the tile shape collection reposition
                // assigning code.
                // Update March 31, 2021
                // Determining if a collision is "l-shaped" based on just RepositionDirection is not sufficient. The reason is that there
                // can be identical reposition directions where one should be L shaped and one shouldn't. For example, consider collision X:
                // OO
                // XO
                // OO
                // This would have a reposition of Left, and that would not be L Shaped
                // However, if we remove the top right block, then X becomes L-Shaped:
                // O
                // XO
                // OO
                // However, its reposition direction remains only Left. Therefore, to accurately determine if collision is L-shaped, we need the
                // context of surrounding collision rather than just the RepositionDirections of the collisioni itself. Therefore, we will promote
                // isLShaped to a RepositionHalfSize property

                //var isLShaped =
                //    this.RepositionDirections == (RepositionDirections.Left | RepositionDirections.Up) ||
                //    this.RepositionDirections == (RepositionDirections.Right | RepositionDirections.Up) ||

                //    this.RepositionDirections == (RepositionDirections.Left | RepositionDirections.Down) ||
                //    this.RepositionDirections == (RepositionDirections.Right | RepositionDirections.Down) ||

                //    rectangle.RepositionDirections == (RepositionDirections.Left | RepositionDirections.Up) ||
                //    rectangle.RepositionDirections == (RepositionDirections.Right | RepositionDirections.Up) ||

                //    rectangle.RepositionDirections == (RepositionDirections.Left | RepositionDirections.Down) ||
                //    rectangle.RepositionDirections == (RepositionDirections.Right | RepositionDirections.Down);


                //if(isLShaped)
                if (RepositionHalfSize || rectangle.RepositionHalfSize)
                {
                    if (side == Side.Left || side == Side.Right)
                    {
                        maxMovement = rectangle.Width / 2.0f + Width / 2.0f;
                    }
                    else
                    {
                        maxMovement = rectangle.Height / 2.0f + Height / 2.0f;
                    }
                }
                shouldApplyReposition &= (maxMovement == null || System.Math.Abs(smallest) < maxMovement);

                if (shouldApplyReposition)
                {
                    float amountToMoveThis = 1;
                    if (!float.IsPositiveInfinity(otherMass))
                    {
                        amountToMoveThis = otherMass / (thisMass + otherMass);
                    }
                    Vector2 movementVector = new Vector2();

                    // Victor Chelaru
                    // December 26, 2016
                    // I'm not sure why we
                    // have a maxMovement variable
                    // here. It used to be set to the 
                    // sum of the two rectangles' scales
                    // (half width), but that prevents the
                    // rectangles from separating when using
                    // reposition directions that cause them to
                    // move across the entire rectangle. I'm going
                    // to use Scale*2 for each, because I'm not sure
                    // if I should remove the maxMovement condition yet
                    // Update: I think this was in place for cloud collision,
                    // so only half of the rectangle would trigger a collision.
                    // This can cause confusing behavior, especially when creating
                    // tile-based collision, so I'm removing maxMovement:
                    //float maxMovement = float.PositiveInfinity;

                    switch (side)
                    {
                        case Side.Left:
                            movementVector.X = rectangle.X - rectangle.mScaleX - mScaleX - X;
                            break;
                        case Side.Right:
                            movementVector.X = rectangle.X + rectangle.mScaleX + mScaleX - X;
                            break;
                        case Side.Top:
                            movementVector.Y = rectangle.Y + rectangle.mScaleY + mScaleY - Y;
                            break;
                        case Side.Bottom:
                            movementVector.Y = rectangle.Y - rectangle.mScaleY - mScaleY - Y;
                            break;
                    }

                    mLastMoveCollisionReposition.X = movementVector.X * amountToMoveThis;
                    mLastMoveCollisionReposition.Y = movementVector.Y * amountToMoveThis;

                    TopParent.X += mLastMoveCollisionReposition.X;
                    TopParent.Y += mLastMoveCollisionReposition.Y;


                    rectangle.mLastMoveCollisionReposition.X = -movementVector.X * (1 - amountToMoveThis);
                    rectangle.mLastMoveCollisionReposition.Y = -movementVector.Y * (1 - amountToMoveThis);


                    rectangle.TopParent.X += rectangle.mLastMoveCollisionReposition.X;
                    rectangle.TopParent.Y += rectangle.mLastMoveCollisionReposition.Y;

                    ForceUpdateDependencies();
                    rectangle.ForceUpdateDependencies();
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// Performs a "move" collision (a collision that adjusts the Position of the calling objects 
        /// to separate them if they overlap), moving each object according to its respective mass.
        /// </summary>
        /// <param name="polygon">The polygon to collide against.</param>
        /// <param name="thisMass">The mass of the AxisAlignedRectangle. 
        /// If value this is 0, the AxisAlignedRectangle will act as if it has no mass - the Polygon will not be pushed or stopped.</param>
        /// <param name="otherMass">The mass of the Polygon. 
        /// If this value is 0, The Polygon will act as if it has no mass - the AxisAlignedRectangle will not be pushed or stopped.</param>
        /// <returns>Whether the caller collides against the argument Polygon.</returns>
        /// <example>
        /// // This shows how to perform a collision where the polygon is static - a common
        /// // situation if the polygon is part of the static environment:
        /// axisAlignedRectangle.CollideAgainstMove(polygon, 0, 1);
        /// </example>
        public bool CollideAgainstMove(Polygon polygon, float thisMass, float otherMass)
        {
            return polygon.CollideAgainstMove(this, otherMass, thisMass);
        }


        public bool CollideAgainstMove(Line line, float thisMass, float otherMass)
        {
            // Vic says: The math for this is kinda complex and I don't know if it's even going to be
            // that common, so I'm going to not write this yet.
            throw new NotImplementedException();
            /*
#if DEBUG
            if (thisMass == 0 && otherMass == 0)
            {
                throw new ArgumentException("Both masses cannot be 0.  For equal masses pick a non-zero value");
            }
#endif
            if (line.CollideAgainst(this))
            {

                Segment asSegment = line.AsSegment();

                // Either we have a point inside the AxisAlignedRectangle OR we have the line intersecting two sides

                #region Count segment intersection

                Point intersectionPoint;
                bool hasCollisionOccurred;

                int firstSegment = 0;
                int secondSegment = 0;
                int intersectionCount = 0;

                // Check if the segment intersects any of the rectangle's edges
                // Here, prepare rectangle's corner points
                Point tl = new Point(
                    Position.X - ScaleX,
                    Position.Y + ScaleY);
                Point tr = new Point(
                    Position.X + ScaleX,
                    Position.Y + ScaleY);
                Point bl = new Point(
                    Position.X - ScaleX,
                    Position.Y - ScaleY);
                Point br = new Point(
                    Position.X + ScaleX,
                    Position.Y - ScaleY);

                Segment top = new Segment(tl, tr);
                Segment right = new Segment(tr, br);
                Segment bottom = new Segment(br, bl);
                Segment left = new Segment(bl, tl);

                // Test if any of the edges intersect the segment
                // (this will short-circuit on the first true test)
                if (asSegment.Intersects(top))
                {
                    if (asSegment.Intersects(right))
                    {
                        intersectionPoint = tr;
                    }
                    else if (asSegment.Intersects(left))
                    {
                        intersectionPoint = tl;
                    }
                    else if (asSegment.Intersects(bottom))
                    {
                        // Figure this out here
                    }
                }




                #endregion

            }
        */
        }


        public bool CollideAgainstMove(Capsule2D capsule2D, float thisMass, float otherMass)
        {
            throw new NotImplementedException("This method is not implemented. Capsules are intended only for CollideAgainst - use Polygons for CollideAgainstMove and CollideAgainstBounce");
        }


        public bool CollideAgainstMove(ShapeCollection shapeCollection, float thisMass, float otherMass)
        {
            return shapeCollection.CollideAgainstMove(this, otherMass, thisMass);

        }

        /// <summary>
        /// Returns whether this AxisAlignedRectangle and the argument AxisAlignedRectangle overlap, 
        /// and repositions the two according to their relative masses and the depth of the overlap.
        /// The more overlap, the faster the two objects will separate.
        /// </summary>
        /// <param name="rectangle">The other rectangle to collide against.</param>
        /// <param name="thisMass">This mass relative to the other rectangle.</param>
        /// <param name="otherMass">The other rectangle's mass relative to this.</param>
        /// <param name="separationVelocity">The separation velocity in units per second. This value is 
        /// multiplied by the overlap amount to result in a velocity. For example, if separationVelocity is 2 and
        /// the objects overlap by 100 units, then the total separation velocity will be 2*100 = 200. 
        /// This total separation velocity will be applied proportionally to this and the other rectangle according
        /// to their relative masses. Increasing this value will make the separation happen more quickly.</param>
        /// <returns>Whether collision has occurred.</returns>
        public bool CollideAgainstMoveSoft(AxisAlignedRectangle rectangle, float thisMass, float otherMass, float separationVelocity)
        {
#if DEBUG
            if (thisMass == 0 && otherMass == 0)
            {
                throw new ArgumentException("Both masses cannot be 0.  For equal masses pick a non-zero value");
            }
#endif
            if (CollideAgainst(rectangle))
            {
                Side side = Side.Left; // set it to left first
                float smallest = System.Math.Abs(X + mScaleX - (rectangle.X - rectangle.mScaleX));

                float currentDistance = System.Math.Abs(X - mScaleX - (rectangle.X + rectangle.mScaleX));
                if (currentDistance < smallest) { smallest = currentDistance; side = Side.Right; }

                currentDistance = Y - mScaleY - (rectangle.Y + rectangle.mScaleY);
                if (currentDistance < 0) currentDistance *= -1;
                if (currentDistance < smallest) { smallest = currentDistance; side = Side.Top; }

                currentDistance = Y + mScaleY - (rectangle.Y - rectangle.mScaleY);
                if (currentDistance < 0) currentDistance *= -1;
                if (currentDistance < smallest) { smallest = currentDistance; side = Side.Bottom; }

                float amountToMoveThis = otherMass / (thisMass + otherMass);
                Vector2 movementVector = new Vector2();

                switch (side)
                {
                    case Side.Left: movementVector.X = rectangle.X - rectangle.mScaleX - mScaleX - X; break;
                    case Side.Right: movementVector.X = rectangle.X + rectangle.mScaleX + mScaleX - X; break;
                    case Side.Top: movementVector.Y = rectangle.Y + rectangle.mScaleY + mScaleY - Y; break;
                    case Side.Bottom: movementVector.Y = rectangle.Y - rectangle.mScaleY - mScaleY - Y; break;
                }

                TopParent.XVelocity += movementVector.X * amountToMoveThis * separationVelocity * TimeManager.SecondDifference;
                TopParent.YVelocity += movementVector.Y * amountToMoveThis * separationVelocity * TimeManager.SecondDifference;

                rectangle.TopParent.XVelocity += -movementVector.X * (1 - amountToMoveThis) * separationVelocity * TimeManager.SecondDifference;
                rectangle.TopParent.YVelocity += -movementVector.Y * (1 - amountToMoveThis) * separationVelocity * TimeManager.SecondDifference;

                ForceUpdateDependencies();
                rectangle.ForceUpdateDependencies();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns whether this AxisAlignedRectangle and the argument AxisAlignedRectangle overlap, 
        /// and repositions the two according to their relative masses and the depth of the overlap.
        /// The more overlap, the faster the two objects will separate.
        /// </summary>
        /// <param name="rectangle">The other rectangle to collide against.</param>
        /// <param name="thisMass">This mass relative to the other rectangle.</param>
        /// <param name="otherMass">The other rectangle's mass relative to this.</param>
        /// <param name="separationVelocity">The separation velocity in units per second. This value is 
        /// multiplied by the overlap amount to result in a velocity. For example, if separationVelocity is 2 and
        /// the objects overlap by 100 units, then the total separation velocity will be 2*100 = 200. 
        /// This total separation velocity will be applied proportionally to this and the other rectangle according
        /// to their relative masses. Increasing this value will make the separation happen more quickly.</param>
        /// <returns>Whether collision has occurred.</returns>
        public bool CollideAgainstMovePositionSoft(AxisAlignedRectangle rectangle, float thisMass, float otherMass, float separationVelocity)
        {
#if DEBUG
            if (thisMass == 0 && otherMass == 0)
            {
                throw new ArgumentException("Both masses cannot be 0.  For equal masses pick a non-zero value");
            }
#endif
            if (CollideAgainst(rectangle))
            {
                Side side = Side.Left; // set it to left first
                float smallest = System.Math.Abs(X + mScaleX - (rectangle.X - rectangle.mScaleX));

                float currentDistance = System.Math.Abs(X - mScaleX - (rectangle.X + rectangle.mScaleX));
                if (currentDistance < smallest) { smallest = currentDistance; side = Side.Right; }

                currentDistance = Y - mScaleY - (rectangle.Y + rectangle.mScaleY);
                if (currentDistance < 0) currentDistance *= -1;
                if (currentDistance < smallest) { smallest = currentDistance; side = Side.Top; }

                currentDistance = Y + mScaleY - (rectangle.Y - rectangle.mScaleY);
                if (currentDistance < 0) currentDistance *= -1;
                if (currentDistance < smallest) { smallest = currentDistance; side = Side.Bottom; }

                float amountToMoveThis = otherMass / (thisMass + otherMass);
                Vector2 movementVector = new Vector2();

                switch (side)
                {
                    case Side.Left: movementVector.X = rectangle.X - rectangle.mScaleX - mScaleX - X; break;
                    case Side.Right: movementVector.X = rectangle.X + rectangle.mScaleX + mScaleX - X; break;
                    case Side.Top: movementVector.Y = rectangle.Y + rectangle.mScaleY + mScaleY - Y; break;
                    case Side.Bottom: movementVector.Y = rectangle.Y - rectangle.mScaleY - mScaleY - Y; break;
                }

                TopParent.X += movementVector.X * amountToMoveThis * separationVelocity * TimeManager.SecondDifference;
                TopParent.Y += movementVector.Y * amountToMoveThis * separationVelocity * TimeManager.SecondDifference;

                rectangle.TopParent.X += -movementVector.X * (1 - amountToMoveThis) * separationVelocity * TimeManager.SecondDifference;
                rectangle.TopParent.Y += -movementVector.Y * (1 - amountToMoveThis) * separationVelocity * TimeManager.SecondDifference;

                // The other "soft" repositions don't do this.
                //ForceUpdateDependencies();
                //rectangle.ForceUpdateDependencies();

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
                    collisionNormal = new Vector2(-polygon.LastMoveCollisionReposition.X, -polygon.LastMoveCollisionReposition.Y);
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

                Vector2 collisionNormal = LastMoveCollisionReposition;

                if (otherMass == 0)
                {
                    collisionNormal = new Vector2(-rectangle.LastMoveCollisionReposition.X, -rectangle.LastMoveCollisionReposition.Y);
                }

                ShapeManager.ApplyBounce(thisTopParent, otherTopParent, thisMass, otherMass, elasticity, ref collisionNormal);
                return true;
            }

            return false;
        }


        public bool CollideAgainstBounce(Capsule2D capsule2D, float thisMass, float otherMass, float elasticity)
        {
            throw new NotImplementedException("This method is not implemented.  Capsules are intended only for CollideAgainst - use Polygons for CollideAgainstMove and CollideAgainstBounce.");
        }


        public bool CollideAgainstBounce(Circle circle, float thisMass, float otherMass, float elasticity)
        {
            return circle.CollideAgainstBounce(this, otherMass, thisMass, elasticity);
        }


        public bool CollideAgainstBounce(Line line, float thisMass, float otherMass, float elasticity)
        {
            throw new NotImplementedException();
        }


        public bool CollideAgainstBounce(ShapeCollection shapeCollection, float thisMass, float otherMass, float elasticity)
        {
            return shapeCollection.CollideAgainstBounce(this, otherMass, thisMass, elasticity);
        }

        #endregion


        public bool Intersects(Segment segment, out Point intersectionPoint)
        {
            Segment top = new Segment(this.Left, this.Top, this.Right, this.Top);
            Segment bottom = new Segment(this.Left, this.Bottom, this.Right, this.Bottom);
            Segment left = new Segment(this.Left, this.Top, this.Left, this.Bottom);
            Segment right = new Segment(this.Right, this.Top, this.Right, this.Bottom);

            intersectionPoint = new Point();

            Point tempPoint = new Point();

            if (top.Intersects(segment, out tempPoint))
            {
                intersectionPoint = tempPoint;
                return true;
            }

            if (bottom.Intersects(segment, out tempPoint))
            {
                intersectionPoint = tempPoint;
                return true;
            }

            if (left.Intersects(segment, out tempPoint))
            {
                intersectionPoint = tempPoint;
                return true;
            }

            if (right.Intersects(segment, out tempPoint))
            {
                intersectionPoint = tempPoint;
                return true;
            }

            return false;

        }


        #region IsPointInside

        public bool IsPointInside(float x, float y)
        {
            return x < Position.X + mScaleX && x > Position.X - ScaleX &&
                y < Position.Y + mScaleY && y > Position.Y - mScaleY;
        }


        public bool IsPointInside(ref Vector3 point)
        {
            return point.X < Position.X + mScaleX && point.X > Position.X - ScaleX &&
                point.Y < Position.Y + mScaleY && point.Y > Position.Y - mScaleY;

        }


        public bool IsPointOnOrInside(float x, float y)
        {
            return x <= X + mScaleX && x >= X - ScaleX &&
                y <= Y + mScaleY && y >= Y - mScaleY;
        }


        public bool IsPointOnOrInside(ref Point point)
        {
            return point.X <= X + mScaleX && point.X >= X - ScaleX &&
                point.Y <= Y + mScaleY && point.Y >= Y - mScaleY;
        }


        #endregion


        public Vector3 GetRandomPositionInThis()
        {
            return new Vector3(
                Position.X - mScaleX + (float)(FlatRedBallServices.Random.NextDouble() * 2 * mScaleX),
                Position.Y - mScaleY + (float)(FlatRedBallServices.Random.NextDouble() * 2 * mScaleY),
                0);
        }


        public void KeepThisInsideOf(AxisAlignedRectangle otherAxisAlignedRectangle)
        {
            if (this.X - this.ScaleX < otherAxisAlignedRectangle.X - otherAxisAlignedRectangle.ScaleX)
            {
                TopParent.X = otherAxisAlignedRectangle.X - otherAxisAlignedRectangle.ScaleX + this.ScaleX;
            }

            if (this.X + this.ScaleX > otherAxisAlignedRectangle.X + otherAxisAlignedRectangle.ScaleX)
            {
                TopParent.X = otherAxisAlignedRectangle.X + otherAxisAlignedRectangle.ScaleX - this.ScaleX;
            }

            if (this.Y - this.ScaleY < otherAxisAlignedRectangle.Y - otherAxisAlignedRectangle.ScaleY)
            {
                TopParent.Y = otherAxisAlignedRectangle.Y - otherAxisAlignedRectangle.ScaleY + this.ScaleY;
            }

            if (this.Y + this.ScaleY > otherAxisAlignedRectangle.Y + otherAxisAlignedRectangle.ScaleY)
            {
                TopParent.Y = otherAxisAlignedRectangle.Y + otherAxisAlignedRectangle.ScaleX - this.ScaleY;
            }
        }


        public override void Pause(FlatRedBall.Instructions.InstructionList instructions)
        {
            FlatRedBall.Instructions.Pause.AxisAlignedRectangleUnpauseInstruction instruction =
                new FlatRedBall.Instructions.Pause.AxisAlignedRectangleUnpauseInstruction(this);

            instruction.Stop(this);

            instructions.Add(instruction);
        }


        public void SetFromAbsoluteEdges(float top, float bottom, float left, float right)
        {
            Position.X = (left + right) / 2.0f;
            Position.Y = (top + bottom) / 2.0f;

            this.ScaleX = (right - left) / 2.0f;
            this.ScaleY = (top - bottom) / 2.0f;
        }


        public void SetScale(IScalable scaleToSetFrom)
        {
            mScaleX = scaleToSetFrom.ScaleX;

            ScaleY = scaleToSetFrom.ScaleY;
        }


        public override void TimedActivity(float secondDifference,
            double secondDifferenceSquaredDividedByTwo, float secondsPassedLastFrame)
        {
            base.TimedActivity(secondDifference, secondDifferenceSquaredDividedByTwo, secondsPassedLastFrame);


            mScaleX += (float)(mScaleXVelocity * secondDifference);

            ScaleY = mScaleY + (float)(mScaleYVelocity * secondDifference); // to force recalculation of radius
        }


        public Point3D VectorFrom(double x, double y, double z)
        {
            if (x < Left)
            {
                if (y > Top)
                {
                    return new Point3D(Left - x, Top - y, 0);
                }
                else if (y < Bottom)
                {
                    return new Point3D(Left - x, Bottom - y, 0);
                }
                else
                {
                    return new Point3D(Left - x, 0, 0);
                }
            }
            else if (x > Right)
            {
                if (y > Top)
                {
                    return new Point3D(Right - x, Top - y, 0);
                }
                else if (y < Bottom)
                {
                    return new Point3D(Right - x, y, 0);
                }
                else
                {
                    return new Point3D(Right - x, 0, 0);
                }
            }
            else
            {
                double distanceFromRight = Right - x;
                double distanceFromLeft = x - Left;
                double distanceFromTop = Top - y;
                double distanceFromBottom = y - Bottom;

                if (x > X)
                {
                    if (y > Y)
                    {
                        if (distanceFromRight > distanceFromTop)
                            return new Point3D(0, distanceFromTop, 0);
                        else
                            return new Point3D(distanceFromRight, 0, 0);
                    }
                    else
                    {
                        if (distanceFromRight > distanceFromBottom)
                            return new Point3D(0, -distanceFromBottom, 0);
                        else
                            return new Point3D(distanceFromRight, 0, 0);
                    }
                }
                else
                {
                    if (y > Y)
                    {
                        if (distanceFromLeft > distanceFromTop)
                            return new Point3D(0, distanceFromTop, 0);
                        else
                            return new Point3D(-distanceFromLeft, 0, 0);
                    }
                    else
                    {
                        if (distanceFromLeft > distanceFromBottom)
                            return new Point3D(0, -distanceFromBottom, 0);
                        else
                            return new Point3D(-distanceFromLeft, 0, 0);
                    }
                }
            }
        }

        public Point3D VectorFrom(Point3D vector)
        {
            return VectorFrom(vector.X, vector.Y, vector.Z);
        }

        public FloatRectangle AsFloatRectangle()
        {
            return new FloatRectangle(Top, Bottom, Left, Right);
        }

        public override string ToString()
        {
            return $"Left:{Left} Right:{Right} Top:{Top} Bottom:{Bottom}";
        }

        #endregion

        #region Protected Methods


        #endregion

        #endregion

        #region IEquatable<AxisAlignedRectangle> Members

        bool IEquatable<AxisAlignedRectangle>.Equals(AxisAlignedRectangle other)
        {
            return this == other;
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
