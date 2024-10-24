using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Input;
using Side = FlatRedBall.Math.Collision.CollisionEnumerations.Side3D;
using FlatRedBall.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Math.Geometry
{
    public class AxisAlignedCube : PositionedObject, IScalable3D, IScalable, IEquatable<AxisAlignedCube>, IMouseOver
    {
        #region Fields

        internal float mScaleX = 1;
        internal float mScaleY = 1;
        internal float mScaleZ = 1;

        float mScaleXVelocity;
        float mScaleYVelocity;
        float mScaleZVelocity;

        bool mVisible;

        float mBoundingRadius;

        internal Vector3 mLastMoveCollisionReposition;

        public Color mColor = Color.White;

        internal Layer mLayerBelongingTo;

        #endregion

        #region Properties

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
                mBoundingRadius = (float)(System.Math.Sqrt((mScaleX * mScaleX) + (mScaleY * mScaleY) + (mScaleZ * mScaleZ)));
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
                mBoundingRadius = (float)(System.Math.Sqrt((mScaleX * mScaleX) + (mScaleY * mScaleY) + (mScaleZ * mScaleZ)));
            }
        }

        public float ScaleZ
        {
            get { return mScaleZ; }
            set
            {
#if DEBUG
                if (value < 0)
                {
                    throw new ArgumentException("Cannot set a negative scale value.  This will cause collision to behave weird.");
                }
#endif
                mScaleZ = value;
                mBoundingRadius = (float)(System.Math.Sqrt((mScaleX * mScaleX) + (mScaleY * mScaleY) + (mScaleZ * mScaleZ)));
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

        public float ScaleZVelocity
        {
            get { return mScaleZVelocity; }
            set { mScaleZVelocity = value; }
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

        public Vector3 LastMoveCollisionReposition
        {
            get { return mLastMoveCollisionReposition; }
        }

        #endregion

        #region Methods

        #region Constructor

        public AxisAlignedCube()
        { }

        #endregion

        #region Public Methods

        public AxisAlignedCube Clone()
        {
            AxisAlignedCube clonedCube = this.Clone<AxisAlignedCube>();
            clonedCube.mVisible = false;
            clonedCube.mLayerBelongingTo = null;

            return clonedCube;
        }

        public override void TimedActivity(float secondDifference,
            double secondDifferenceSquaredDividedByTwo, float secondsPassedLastFrame)
        {
            base.TimedActivity(secondDifference, secondDifferenceSquaredDividedByTwo, secondsPassedLastFrame);

            mScaleX += (float)(mScaleXVelocity * secondDifference);
            mScaleY += (float)(mScaleYVelocity * secondDifference);
            ScaleZ += (float)(mScaleZVelocity * secondDifference); // to force recalculation of radius
        }

        public BoundingBox AsBoundingBox()
        {
            BoundingBox boundingBox = new BoundingBox(
                new Vector3(Position.X - mScaleX, Position.Y - mScaleY, Position.Z - mScaleZ),
                new Vector3(Position.X + mScaleX, Position.Y + mScaleY, Position.Z + mScaleZ));

            return boundingBox;
        }

        #region CollideAgainst
        public bool CollideAgainst(AxisAlignedCube cube)
        {
            UpdateDependencies(TimeManager.CurrentTime);
            cube.UpdateDependencies(TimeManager.CurrentTime);

            return (X - mScaleX < cube.X + cube.mScaleX &&
                    X + mScaleX > cube.X - cube.mScaleX &&
                    Y - mScaleY < cube.Y + cube.mScaleY &&
                    Y + mScaleY > cube.Y - cube.mScaleY &&
                    Z - mScaleZ < cube.Z + cube.mScaleZ &&
                    Z + mScaleZ > cube.Z - cube.mScaleZ);
        }

        public bool CollideAgainstMove(AxisAlignedCube other, float thisMass, float otherMass)
        {
            if (CollideAgainst(other))
            {
                Side side = Side.Left; // set it to left first
                float smallest = System.Math.Abs(X + mScaleX - (other.X - other.mScaleX));

                float currentDistance = System.Math.Abs(X - mScaleX - (other.X + other.mScaleX));
                if (currentDistance < smallest) { smallest = currentDistance; side = Side.Right; }

                currentDistance = Y - mScaleY - (other.Y + other.mScaleY);
                if (currentDistance < 0) currentDistance *= -1;
                if (currentDistance < smallest) { smallest = currentDistance; side = Side.Top; }

                currentDistance = Y + mScaleY - (other.Y - other.mScaleY);
                if (currentDistance < 0) currentDistance *= -1;
                if (currentDistance < smallest) { smallest = currentDistance; side = Side.Bottom; }

                currentDistance = Z - mScaleZ - (other.Z + other.mScaleZ);
                if (currentDistance < 0) currentDistance *= -1;
                if (currentDistance < smallest) { smallest = currentDistance; side = Side.Front; }

                currentDistance = Z + mScaleZ - (other.Z - other.mScaleZ);
                if (currentDistance < 0) currentDistance *= -1;
                if (currentDistance < smallest) { smallest = currentDistance; side = Side.Back; }


                float amountToMoveThis = otherMass / (thisMass + otherMass);
                Vector3 movementVector = new Vector3();


                switch (side)
                {
                    case Side.Left: movementVector.X = other.X - other.mScaleX - mScaleX - X; break;
                    case Side.Right: movementVector.X = other.X + other.mScaleX + mScaleX - X; break;
                    case Side.Top: movementVector.Y = other.Y + other.mScaleY + mScaleY - Y; break;
                    case Side.Bottom: movementVector.Y = other.Y - other.mScaleY - mScaleY - Y; break;
                    case Side.Front: movementVector.Z = other.Z + other.mScaleZ + mScaleZ - Z; break;
                    case Side.Back: movementVector.Z = other.Z - other.mScaleZ - mScaleZ - Z; break;
                }

                //Should this be a Vector3 instead?
                mLastMoveCollisionReposition.X = movementVector.X * amountToMoveThis;
                mLastMoveCollisionReposition.Y = movementVector.Y * amountToMoveThis;
                mLastMoveCollisionReposition.Z = movementVector.Z * amountToMoveThis;

                TopParent.X += mLastMoveCollisionReposition.X;
                TopParent.Y += mLastMoveCollisionReposition.Y;
                TopParent.Z += mLastMoveCollisionReposition.Z;

                other.mLastMoveCollisionReposition.X = -movementVector.X * (1 - amountToMoveThis);
                other.mLastMoveCollisionReposition.Y = -movementVector.Y * (1 - amountToMoveThis);
                other.mLastMoveCollisionReposition.Z = -movementVector.Z * (1 - amountToMoveThis);

                other.TopParent.X += other.mLastMoveCollisionReposition.X;
                other.TopParent.Y += other.mLastMoveCollisionReposition.Y;
                other.TopParent.Z += other.mLastMoveCollisionReposition.Z;

                ForceUpdateDependencies();
                other.ForceUpdateDependencies();

                return true;
            }
            return false;
        }

        public bool CollideAgainst(Sphere sphere)
        {
            return sphere.CollideAgainst(this);
        }

        public bool CollideAgainstMove(Sphere sphere, float thisMass, float otherMass)
        {
            return sphere.CollideAgainstMove(this, otherMass, thisMass);
        }

        public bool CollideAgainstBounce(AxisAlignedCube other, float thisMass, float otherMass, float elasticity)
        {
            if (this.CollideAgainstMove(other, thisMass, otherMass))
            {
                // Get the relative velocity of this circle to the argument circle:
                Vector3 relativeVelocity = new Vector3(
                    this.TopParent.XVelocity - other.TopParent.XVelocity,
                    this.TopParent.YVelocity - other.TopParent.YVelocity,
                    this.TopParent.ZVelocity - other.TopParent.ZVelocity);

                Vector3 reposition = mLastMoveCollisionReposition;

                if (otherMass == 0)
                {
                    reposition = -other.mLastMoveCollisionReposition;
                }
                float velocityNormalDotResult;
                Vector3.Dot(ref relativeVelocity, ref reposition, out velocityNormalDotResult);

                if (velocityNormalDotResult >= 0)
                {
                    return true;
                }

                Vector3 reverseNormal = -reposition;
                reverseNormal.Normalize();

                float length = Vector3.Dot(relativeVelocity, reverseNormal);
                Vector3 velocityOnNormal;
                Vector3.Multiply(ref reverseNormal, length, out velocityOnNormal);

                other.TopParent.Velocity.X += (1 + elasticity) * thisMass / (thisMass + otherMass) * velocityOnNormal.X;
                other.TopParent.Velocity.Y += (1 + elasticity) * thisMass / (thisMass + otherMass) * velocityOnNormal.Y;
                other.TopParent.Velocity.Z += (1 + elasticity) * thisMass / (thisMass + otherMass) * velocityOnNormal.Z;

                this.TopParent.Velocity.X -= (1 + elasticity) * otherMass / (thisMass + otherMass) * velocityOnNormal.X;
                this.TopParent.Velocity.Y -= (1 + elasticity) * otherMass / (thisMass + otherMass) * velocityOnNormal.Y;
                this.TopParent.Velocity.Z -= (1 + elasticity) * otherMass / (thisMass + otherMass) * velocityOnNormal.Z;

                return true;

            }
            return false;
        }

        public bool CollideAgainstBounce(Sphere sphere, float thisMass, float otherMass, float elasticity)
        {
            return sphere.CollideAgainstBounce(this, otherMass, thisMass, elasticity);
        }


        #endregion

        public bool IsPointInside(Vector3 absolutePosition)
        {
            return absolutePosition.X > this.Position.X - this.mScaleX &&
                absolutePosition.X < this.Position.X + this.mScaleX &&
                absolutePosition.Y > this.Position.Y - this.mScaleY &&
                absolutePosition.Y < this.Position.Y + this.mScaleY &&
                absolutePosition.Z > this.Position.Z - this.mScaleZ &&
                absolutePosition.Z < this.Position.Z + this.mScaleZ;

        }

        #endregion

        #endregion


        #region IEquatable<AxisAlignedCube> Members

        public bool Equals(AxisAlignedCube other)
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
    }
}
