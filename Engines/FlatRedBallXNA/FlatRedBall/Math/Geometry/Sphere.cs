using System;
using System.Collections.Generic;
using System.Text;


using FlatRedBall.Gui;
using FlatRedBall.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;


using FlatRedBall.Instructions.Pause;
using FlatRedBall.Instructions;
using FlatRedBall.Graphics;

namespace FlatRedBall.Math.Geometry
{
    public class Sphere : PositionedObject, IEquatable<Sphere>, IMouseOver
    {
        #region Fields

        // made public for performance.
        public float Radius;
        float mRadiusVelocity;
        bool mVisible;
        Color mColor;

        public Vector3 LastMoveCollisionReposition;

        internal Layer mLayerBelongingTo;

        #endregion

        #region Properties

		// For ShapeCollection
		internal float BoundingRadius
		{
			get { return Radius; }
		}

        /// <summary>
        /// The Sphere's Color to use when being rendered.
        /// </summary>
        public Color Color
        {
            get { return mColor; }
            set { mColor = value; }
        }

        /// <summary>
        /// The rate at which the velocity is increasing - this is 0 by default.
        /// </summary>
        public float RadiusVelocity
        {
            get { return mRadiusVelocity; }
            set { mRadiusVelocity = value; }
        }

        /// <summary>
        /// Whether this instance is visible.  Setting this to true will add this instance to the ShapeManager's drawn list.
        /// </summary>
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

        #region Constructor / Initialization

        public Sphere()
        {
            Radius = 1f;
            mColor = Color.Cyan;
        }

        #endregion

        #region Methods

        #region Public Methods

		public Sphere Clone()
		{
			Sphere clonedSphere = this.Clone<Sphere>();
			clonedSphere.mVisible = false;
			clonedSphere.mLayerBelongingTo = null;
			return clonedSphere;
		}

        /// <summary>
        /// Pauses this instance and stores the unpause instructions in the argument InstructionList
        /// </summary>
        /// <param name="instructions">The list to fill</param>
        public override void Pause(InstructionList instructions)
        {
            SphereUnpauseInstruction instruction = new SphereUnpauseInstruction(this);

            instruction.Stop(this);

            instructions.Add(instruction);
        }

        /// <summary>
        /// Applies the every-frame activity for moving the object.  This is automatically called every frame by the engine if this object is part of the ShapeManager.
        /// </summary>
        /// <param name="secondDifference">The number of seconds that have passed since last frame</param>
        /// <param name="secondDifferenceSquaredDividedByTwo">SecondDifference * secondDifference / 2 - used for integrating acceleration.</param>
        /// <param name="lastSecondDifference">How much time passed last frame.</param>
        public override void TimedActivity(float secondDifference,
            double secondDifferenceSquaredDividedByTwo, float lastSecondDifference)
        {
            base.TimedActivity(secondDifference, secondDifferenceSquaredDividedByTwo, lastSecondDifference);

            Radius += (float)(mRadiusVelocity * secondDifference);
        }

        /// <summary>
        /// Constructs a BoundingSphere instance form this.
        /// </summary>
        /// <returns>The created instance</returns>
        public BoundingSphere AsBoundingSphere()
        {
            BoundingSphere boundingSphere = new BoundingSphere(
                this.Position,
                this.Radius);

            return boundingSphere;
        }

        #region CollideAgainst

        /// <summary>
        /// Returns whether this instance overlaps the argument Sphere.
        /// </summary>
        /// <param name="sphere">The other Sphere to test against.</param>
        /// <returns>Whether collision has occurred.</returns>
        public bool CollideAgainst(Sphere sphere)
        {
            UpdateDependencies(TimeManager.CurrentTime);
            sphere.UpdateDependencies(TimeManager.CurrentTime);

            float differenceX = X - sphere.X;
            float differenceY = Y - sphere.Y;
            float differenceZ = Z - sphere.Z;

            double differenceSquared = differenceX * differenceX + differenceY * differenceY + differenceZ * differenceZ;

            return (System.Math.Pow(differenceSquared, .5) - Radius - sphere.Radius < 0);
        }

        #region XML Docs
        /// <summary>
        /// Collision method that returns whether collision has occurred and repositions this and the
        /// argument Sphere to prevent overlap.
        /// </summary>
        /// <param name="sphere">The other Sphere to collide against.</param>
        /// <param name="thisMass">The mass of the calling Sphere.  This value is used relative to "otherMass".  Both cannot be 0.</param>
        /// <param name="otherMass">The mass of the argument Sphere.  This value is used relative to "thisMass".  Both cannot be 0.</param>
        /// <returns>Whether the calling Sphere and the argument Sphere are touching.</returns>
        #endregion
        public bool CollideAgainstMove(Sphere sphere, float thisMass, float otherMass)
        {
#if DEBUG
            if (thisMass == 0 && otherMass == 0)
            {
                throw new ArgumentException("Both masses cannot be 0.  For equal masses pick a non-zero value");
            }
#endif
            if (mLastDependencyUpdate != TimeManager.CurrentTime)
            {
                UpdateDependencies(TimeManager.CurrentTime);
            }
            if (sphere.mLastDependencyUpdate != TimeManager.CurrentTime)
            {
                sphere.UpdateDependencies(TimeManager.CurrentTime);
            }

            float differenceX = sphere.Position.X - Position.X;
            float differenceY = sphere.Position.Y - Position.Y;
            float differenceZ = sphere.Position.Z - Position.Z;

            float differenceSquared =
                differenceX * differenceX + differenceY * differenceY + differenceZ * differenceZ;

            if (differenceSquared < (Radius + sphere.Radius) * (Radius + sphere.Radius))
            {

                double distanceToMove = Radius + sphere.Radius - System.Math.Sqrt(differenceSquared);
                Vector3 moveVector = new Vector3(differenceX, differenceY, differenceZ);
                moveVector.Normalize();

                float amountToMoveThis = otherMass / (thisMass + otherMass);

                LastMoveCollisionReposition = moveVector * (float)(-distanceToMove * amountToMoveThis);

                sphere.LastMoveCollisionReposition = moveVector * (float)(distanceToMove * (1 - amountToMoveThis));

                if (mParent != null)
                {
                    TopParent.Position += LastMoveCollisionReposition;
                    
                    ForceUpdateDependencies();
                }
                else
                {
                    Position += LastMoveCollisionReposition;
                }

                if (sphere.mParent != null)
                {
                    sphere.TopParent.Position += sphere.LastMoveCollisionReposition;

                    sphere.ForceUpdateDependencies();
                }
                else
                {
                    sphere.Position += sphere.LastMoveCollisionReposition;
                }

                return true;

            }
            else
                return false;
        }

        /// <summary>
        /// Returns whether this instance overlaps the argument AxisAlignedCube
        /// </summary>
        /// <param name="cube">The instance AxisAlignedCube to test against.</param>
        /// <returns>Whether collision has occurred</returns>
        public bool CollideAgainst(AxisAlignedCube cube)
        {
            UpdateDependencies(TimeManager.CurrentTime);
            cube.UpdateDependencies(TimeManager.CurrentTime);

            Vector3 relativeCenter = Position - cube.Position;

            if (System.Math.Abs(relativeCenter.X) - Radius > cube.ScaleX ||
                System.Math.Abs(relativeCenter.Y) - Radius > cube.ScaleY ||
                System.Math.Abs(relativeCenter.Z) - Radius > cube.ScaleZ)
            {
                return false;
            }

            Vector3 nearest = GetNearestPoint(cube, relativeCenter);

            float distance = (nearest - relativeCenter).LengthSquared();
            return (distance <= Radius * Radius);
        }

        /// <summary>
        /// Returns whether this instance overlaps the argument cube, and separates the two instances according to their relative masses.
        /// </summary>
        /// <param name="cube">The cube to perform collision against.</param>
        /// <param name="thisMass">The mass of this instance.</param>
        /// <param name="otherMass">The mass of the cube.</param>
        /// <returns>Whether the objects were overlapping before the reposition.</returns>
        public bool CollideAgainstMove(AxisAlignedCube cube, float thisMass, float otherMass)
        {
#if DEBUG
            if (thisMass == 0 && otherMass == 0)
            {
                throw new ArgumentException("Both masses cannot be 0.  For equal masses pick a non-zero value");
            }
#endif
            if (LastDependencyUpdate != TimeManager.CurrentTime)
            {
                UpdateDependencies(TimeManager.CurrentTime);
            }
            if (cube.LastDependencyUpdate != TimeManager.CurrentTime)
            {
                cube.UpdateDependencies(TimeManager.CurrentTime);
            }


            Vector3 relativeCenter = Position - cube.Position;

            if (System.Math.Abs(relativeCenter.X) - Radius > cube.ScaleX ||
                System.Math.Abs(relativeCenter.Y) - Radius > cube.ScaleY ||
                System.Math.Abs(relativeCenter.Z) - Radius > cube.ScaleZ)
            {
                return false;
            }

            Vector3 nearest = GetNearestPoint(cube, relativeCenter);

            float distance = (nearest - relativeCenter).LengthSquared();

            if (distance > Radius * Radius) return false;

            double distanceToMove = Radius - System.Math.Sqrt((double)distance);
            Vector3 moveVector = relativeCenter - nearest;
            moveVector.Normalize();

            float amountToMoveThis = otherMass / (thisMass + otherMass);

            LastMoveCollisionReposition = moveVector * (float)(distanceToMove * amountToMoveThis);

            Vector3 cubeReposition = moveVector * (float)(-distanceToMove * (1 - amountToMoveThis));
            cube.mLastMoveCollisionReposition = cubeReposition;

            if (mParent != null)
            {
                TopParent.Position += LastMoveCollisionReposition;
                ForceUpdateDependencies();
            }
            else
            {
                Position += LastMoveCollisionReposition;
            }

            if (cube.mParent != null)
            {
                cube.TopParent.Position += cube.LastMoveCollisionReposition;
                ForceUpdateDependencies();
            }
            else
            {
                cube.Position += cube.LastMoveCollisionReposition;
            }

            return true;
        }

        /// <summary>
        /// Performs a bounce collision (a collision which modifies velocity and acceleration), and separates the objects if so.
        /// </summary>
        /// <param name="cube">The cube to perform collision against.</param>
        /// <param name="thisMass">The mass of this instance.</param>
        /// <param name="otherMass">Th e mass of the argument cube.</param>
        /// <param name="elasticity">The ratio of velocity to preserve.</param>
        /// <returns>Whether a collision occurred.</returns>
        public bool CollideAgainstBounce(AxisAlignedCube cube, float thisMass, float otherMass, float elasticity)
        {
#if DEBUG
            if (thisMass == 0 && otherMass == 0)
            {
                throw new ArgumentException("Both masses cannot be 0.  For equal masses pick a non-zero value");
            }
#endif
            if (CollideAgainstMove(cube, thisMass, otherMass))
            {

                // Get the relative velocity of this circle to the argument circle:
                Vector3 relativeVelocity = new Vector3(
                    this.TopParent.XVelocity - cube.TopParent.XVelocity,
                    this.TopParent.YVelocity - cube.TopParent.YVelocity,
                    this.TopParent.ZVelocity - cube.TopParent.ZVelocity);

                Vector3 reposition = LastMoveCollisionReposition;

                if (otherMass == 0)
                {
                    reposition = -cube.mLastMoveCollisionReposition;
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
 
                cube.TopParent.Velocity.X += (1 + elasticity) * thisMass / (thisMass + otherMass) * velocityOnNormal.X;
                cube.TopParent.Velocity.Y += (1 + elasticity) * thisMass / (thisMass + otherMass) * velocityOnNormal.Y;
                cube.TopParent.Velocity.Z += (1 + elasticity) * thisMass / (thisMass + otherMass) * velocityOnNormal.Z;

                this.TopParent.Velocity.X -= (1 + elasticity) * otherMass / (thisMass + otherMass) * velocityOnNormal.X;
                this.TopParent.Velocity.Y -= (1 + elasticity) * otherMass / (thisMass + otherMass) * velocityOnNormal.Y;
                this.TopParent.Velocity.Z -= (1 + elasticity) * otherMass / (thisMass + otherMass) * velocityOnNormal.Z;

                return true;
            }
            return false;
        }

        /// <summary>
        /// Performs a bounce collision (a collision which modifies velocity and acceleration), and separates the objects if so.
        /// </summary>
        /// <param name="sphere">The Sphere to perform collision against.</param>
        /// <param name="thisMass">The mass of this instance.</param>
        /// <param name="otherMass">Th e mass of the argument cube.</param>
        /// <param name="elasticity">The ratio of velocity to preserve.</param>
        /// <returns>Whether a collision occurred.</returns>
        public bool CollideAgainstBounce(Sphere sphere, float thisMass, float otherMass, float elasticity)
        {
#if DEBUG
            if (thisMass == 0 && otherMass == 0)
            {
                throw new ArgumentException("Both masses cannot be 0.  For equal masses pick a non-zero value");
            }
#endif
            if (CollideAgainstMove(sphere, thisMass, otherMass))
            {

                // Get the relative velocity of this circle to the argument circle:
                Vector3 relativeVelocity = new Vector3(
                    this.TopParent.XVelocity - sphere.TopParent.XVelocity,
                    this.TopParent.YVelocity - sphere.TopParent.YVelocity,
                    this.TopParent.ZVelocity - sphere.TopParent.ZVelocity);


                float velocityNormalDotResult;
                Vector3.Dot(ref relativeVelocity, ref LastMoveCollisionReposition, out velocityNormalDotResult);

                if (velocityNormalDotResult >= 0)
                {
                    return true;
                }

                Vector3 reverseNormal = -LastMoveCollisionReposition;
                reverseNormal.Normalize();

                float length = Vector3.Dot(relativeVelocity, reverseNormal);
                Vector3 velocityOnNormal;
                Vector3.Multiply(ref reverseNormal, length, out velocityOnNormal);

                sphere.TopParent.Velocity.X += (1 + elasticity) * thisMass / (thisMass + otherMass) * velocityOnNormal.X;
                sphere.TopParent.Velocity.Y += (1 + elasticity) * thisMass / (thisMass + otherMass) * velocityOnNormal.Y;
                sphere.TopParent.Velocity.Z += (1 + elasticity) * thisMass / (thisMass + otherMass) * velocityOnNormal.Z;

                this.TopParent.Velocity.X -= (1 + elasticity) * otherMass / (thisMass + otherMass) * velocityOnNormal.X;
                this.TopParent.Velocity.Y -= (1 + elasticity) * otherMass / (thisMass + otherMass) * velocityOnNormal.Y;
                this.TopParent.Velocity.Z -= (1 + elasticity) * otherMass / (thisMass + otherMass) * velocityOnNormal.Z;

                return true;
            }
            return false;
        }


        #endregion

        #region IsPointInside

        /// <summary>
        /// Returns whether the argument point is inside this instance.
        /// </summary>
        /// <param name="point">The point in world coordinates.</param>
        /// <returns>Whether the point is inside this.</returns>
        public bool IsPointInside(ref Vector3 point)
        {
            return 
                (Position.X - point.X) * (Position.X - Position.X) +
                (Position.Y - point.Y) * (Position.Y - Position.Y) +
                (Position.Z - point.Z) * (Position.Z - Position.Z) < Radius * Radius;
        }

        #endregion

        #endregion

        #region Protected Methods



        #endregion

        #region Private Methods

        private Vector3 GetNearestPoint(AxisAlignedCube cube, Vector3 relativeCenter)
        {
            float distance;
            Vector3 nearest = new Vector3();

            distance = relativeCenter.X;
            if (distance > cube.ScaleX) distance = cube.ScaleX;
            if (distance < -cube.ScaleX) distance = -cube.ScaleX;
            nearest.X = distance;

            distance = relativeCenter.Y;
            if (distance > cube.ScaleY) distance = cube.ScaleY;
            if (distance < -cube.ScaleY) distance = -cube.ScaleY;
            nearest.Y = distance;

            distance = relativeCenter.Z;
            if (distance > cube.ScaleZ) distance = cube.ScaleZ;
            if (distance < -cube.ScaleZ) distance = -cube.ScaleZ;
            nearest.Z = distance;

            return nearest;
        }

        #endregion

        #endregion

        #region IEquatable<Sphere> Members

        bool IEquatable<Sphere>.Equals(Sphere other)
        {
            return this == other;
        }

        #endregion

        #region IMouseOver Implementation
        /// <summary>
        /// Returns whether the argument cursor is over this instance.
        /// </summary>
        /// <param name="cursor">The cursor to check.</param>
        /// <returns></returns>
        public bool IsMouseOver(Cursor cursor)
        {
            return cursor.IsOn3D(this);
        }

        /// <summary>
        /// Returns whether the argument cursor is over this instance considering Layer coordinates.
        /// </summary>
        /// <param name="cursor">The cursor to check.</param>
        /// <param name="layer">The layer that this instance sits on.</param>
        /// <returns>Whether the mouse is over this.</returns>
        public bool IsMouseOver(Cursor cursor, Layer layer)
        {
            return cursor.IsOn3D(this, layer);
        }
        #endregion
    }
}
