using System;// Test comment
using System.Collections.Generic;
using FlatRedBall.Math;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Microsoft.Xna.Framework;
using FlatRedBall.Instructions;
using System.Collections;
using System.ComponentModel;

namespace FlatRedBall
{
    /// <summary>
    /// Represents an object with position, rotation, ability to 
    /// store instructions, and attachment abilities.
    /// </summary>
    /// <remarks>
    /// The PositionedObject is a common object in FlatRedBall.  It is the root class
    /// for a number of FlatRedBall classes including Sprites, Texts, SpriteFrames, PositionedModels,
    /// and all Shapes.
    /// <para>
    /// The PositionedObject class is also used as the base class for Glue entities.
    /// </para>
    /// <para>
    /// PositionedObjects can be automatically managed by being added to the SpriteManager.
    /// </para>
    /// </remarks>
    public partial class PositionedObject : IAttachable, IPositionable, IRotatable, IInstructable, IEquatable<PositionedObject>
    {
        #region Fields

        /// <summary>
        /// The absolute world position of the instance.
        /// </summary>
        /// <remarks>
        /// This mirrors the X, Y, and Z properties.  This value essentially becomes
        /// read-only if this object has a Parent.
        /// </remarks>
        public Vector3 Position;

        /// <summary>
        /// The position of the object relative to its Parent in its Parent's coordinate space.
        /// </summary>
        /// <remarks>
        /// These values become the dominant controller of absolute Position if this
        /// instance has a Parent.  These values have no impact if the instance does not
        /// have a Parent.
        /// </remarks>
        public Vector3 RelativePosition;

        /// <summary>
        /// The absolute world velocity of the instance measured in units per second.
        /// </summary>
        /// <remarks>
        /// This mirrors the XVelocity, YVelocity, and ZVelocity properties.  This value is essentially
        /// invalid if this instance has a Parent.
        /// </remarks>
        public Vector3 Velocity;

        /// <summary>
        /// The velocity of the object relative to its Parent in its Parent's cooridnate space.
        /// </summary>
        /// <remarks>
        /// This value modifies the RelativePosition field even if the object does not have a Parent.
        /// </remarks>
        public Vector3 RelativeVelocity;

        /// <summary>
        /// The absolute world acceleration of the instance measured in units per second per second.
        /// </summary>
        /// <remarks>
        /// This mirrors the XAcceleration, YAcceleration, and ZAcceleration properties.  This value is
        /// essentially invalid if the instance has a Parent; however, it can still build up the Velocity
        /// value.  
        /// <para>
        /// If an object with a parent has a non-zero Acceleration for a significant amount of time, then is
        /// detached, the Velocity will likely be non-zero and may be very large causing the object to move
        /// rapidly.
        /// </para>
        /// </remarks>
        public Vector3 Acceleration;

        /// <summary>
        /// The acceleration of the object relative to its Parent in its Parent's coordinate space.
        /// </summary>
        /// <remarks>
        /// This value modifies the RelativeVelocity field even if the object does not have a Parent.
        /// </remarks>
        public Vector3 RelativeAcceleration;

        bool mKeepTrackOfReal;

        /// <summary>
        /// The actual velocity of the object from the last frame to this frame.  This value is only valid
        /// if the KeepTrackOfReal property has been true for over one frame.
        /// </summary>
        public Vector3 RealVelocity;

        /// <summary>
        /// The actualy acceleration of the object from the last frame to this frame.  This value is only valid
        /// if the KeepTrackOfReal property has been set to true for over two frames.
        /// </summary>
        public Vector3 RealAcceleration;

        #region XML Docs
        /// <summary>
        /// The last Position of the object - used to calculate the RealVelocity field.  This is
        /// valid only if the KeepTrackOfReal property is true.
        /// </summary>
        #endregion
        protected Vector3 LastPosition;

        #region XML Docs
        /// <summary>
        /// The last Velocity of the object - this is used to calculate the RealAcceleration field.  This is
        /// valid only if the KeepTrackOfReal property is true.
        /// </summary>
        #endregion
        protected Vector3 LastVelocity;

        #region XML Docs
        /// <summary>
        /// The drag of the instance.
        /// </summary>
        /// <remarks>
        /// <seealso cref="FlatRedBall.PositionedObject.Drag"/>
        /// </remarks>
        #endregion
        protected float mDrag;

        #region XML Docs
        /// <summary>
        /// The name of the instance.
        /// </summary>
        #endregion
        protected string mName;

        #region XML Docs
        /// <summary>
        /// The X component of the object's absolute rotation.
        /// </summary>
        #endregion
        protected internal float mRotationX;

        #region XML Docs
        /// <summary>
        /// The Y component of the object's absolute rotation.
        /// </summary>
        #endregion
        protected internal float mRotationY;

        #region XML Docs
        /// <summary>
        /// The Z component of the object's absolute rotation.
        /// </summary>
        #endregion
        protected internal float mRotationZ;

        #region XML Docs
        /// <summary>
        /// The X component of the object's absolute rotational velocity.
        /// </summary>
        #endregion
        protected float mRotationXVelocity;

        #region XML Docs
        /// <summary>
        /// The Y component of the object's absolute rotational velocity.
        /// </summary>
        #endregion
        protected float mRotationYVelocity;

        #region XML Docs
        /// <summary>
        /// The Z component of the object's absolute rotational velocity.
        /// </summary>
        #endregion
        protected float mRotationZVelocity;

        #region XML Docs
        /// <summary>
        /// The X component of the object's relative rotation.
        /// </summary>
        #endregion
        protected float mRelativeRotationX;

        #region XML Docs
        /// <summary>
        /// The Y component of the object's relative rotation.
        /// </summary>
        #endregion
        protected float mRelativeRotationY;

        /// <summary>
        /// The z component of relative rotation in radians
        /// </summary>
        protected float mRelativeRotationZ;

        #region XML Docs
        /// <summary>
        /// The X component of the object's relative rotational velocity.
        /// </summary>
        #endregion
        protected float mRelativeRotationXVelocity;

        #region XML Docs
        /// <summary>
        /// The Y component of the object's relative rotational velocity.
        /// </summary>
        #endregion
        protected float mRelativeRotationYVelocity;

        #region XML Docs
        /// <summary>
        /// The Z component of the object's relative rotational velocity.
        /// </summary>
        #endregion
        protected float mRelativeRotationZVelocity;

        #region XML Docs
        /// <summary>
        /// The matrix representing the absolute orientation of the instance.
        /// </summary>
        #endregion
        protected Matrix mRotationMatrix;

        #region XML Docs
        /// <summary>
        /// The matrix representing the relative orientation of the object in parent space.  This
        /// has no impact if the Parent is null.
        /// </summary>
        #endregion
        protected Matrix mRelativeRotationMatrix;

        #region XML Docs
        /// <summary>
        /// The lists that this instance belongs to.  This is how two-way relationships are implemented.
        /// </summary>
        #endregion
        protected internal List<IAttachableRemovable> mListsBelongingTo;

        #region XML Docs
        /// <summary>
        /// The PositionedObject that this is attached to.  If it is null then this does not
        /// follow any relative properties.
        /// </summary>
        #endregion
        protected internal PositionedObject mParent; // made internal for performance reasons

        #region XML docs
        /// <summary>
        /// The objects that are attached to this instance.
        /// </summary>
        #endregion
        protected AttachableList<PositionedObject> mChildren;

        bool mParentRotationChangesPosition;
        bool mParentRotationChangesRotation;

        /// <summary>
        /// Controls whether this instance ignores parent positioning. This is false by default, which means Relative values are used
        /// to determine this instance's absolute positions when it has a parent. If this is set to true, then the positioning of this
        /// object behaves as if it was not attached. Rotation and other properties which may be affected by a Parent still apply.
        /// </summary>
        public bool IgnoreParentPosition
        {
            get;
            set;
        }

        #region XML Docs
        /// <summary>
        /// The value that was last used when calling UpdateDependencies.
        /// </summary>
        #endregion
        protected double mLastDependencyUpdate;

        #region XML Docs
        /// <summary>
        /// The instructions that belong to this instance.
        /// </summary>
        #endregion
        protected InstructionList mInstructions;

        #endregion

        #region Properties

        public int HierarchyDepth
        {
            get
            {
                if (mParent == null)
                {
                    return 0;
                }
                else
                {
                    return mParent.HierarchyDepth + 1;
                }
            }
        }


        #region XML Docs
        /// <summary>
        /// The list of PositionedObjects that are attached to this instance.
        /// </summary>
        #endregion
        public AttachableList<PositionedObject> Children
        {
            get { return mChildren; }
        }


        IList IAttachable.ChildrenAsIAttachables
        {
            get { return mChildren; }
        }

        /// <summary>
        /// The List of lists that this belongs to. This member is how two-way relationships are established.
        /// If an object is added to a list using a "one-way" add, then that list will not be in the 
        /// object's ListsBelongingTo.
        /// </summary>
        /// <remarks>
        /// PositionedObjects are typically added to managers, children, and Glue lists using two-way relationships, so this
        /// member provides information about where this object is referenced.
        /// </remarks>
        public List<IAttachableRemovable> ListsBelongingTo
        {
            get { return mListsBelongingTo; }
        }

        #region XML Docs
        /// <summary>
        /// The instance's name.
        /// </summary>
        #endregion
        public string Name
        {
            get { return mName; }
            set { mName = value; this.Children.Name = value + " Children"; }
        }

        /// <summary>
        /// The PositionedObject that this instance is attached to.
        /// </summary>
        public PositionedObject Parent
        {
            get { return mParent; }
        }

        IAttachable IAttachable.ParentAsIAttachable
        {
            get { return mParent; }
        }

        [Obsolete("Was used when we supported 3D, but FRB has moved away from that")]
        public string ParentBone
        {
            get;
            protected set;
        }

        /// <summary>
        /// Whether the parent's rotation should change the object's position.
        /// </summary>
        public bool ParentRotationChangesPosition
        {
            get { return mParentRotationChangesPosition; }
            set { mParentRotationChangesPosition = value; }
        }

        /// <summary>
        /// Gets and sets whether the parent's rotation should change the object's rotation. The default is true, which means
        /// if this is attached to a parent, it will rotate along with the parent, and it can have additional rotation applied through
        /// its relative rotation values.
        /// If this value is false, then the relative values are applied directly to the absolute values, ignoring the parent's rotation.
        /// </summary>
        public bool ParentRotationChangesRotation
        {
            get { return mParentRotationChangesRotation; }
            set { mParentRotationChangesRotation = value; }
        }

        /// <summary>
        /// Gets and sets the X position relative to the instance's parent.
        /// </summary>
        /// <remarks>
        /// If the instance does not have a parent this property has no effect.
        /// </remarks>
        public float RelativeX
        {
            get { return RelativePosition.X; }
            set
            {
                RelativePosition.X = value;

#if DEBUG
                if (float.IsNaN(RelativePosition.X))
                {
                    throw new NaNException("The RelativePosition X value has been set to an invalid number (float.NaN)", "RelativeX");
                }
                if (float.IsPositiveInfinity(RelativePosition.X))
                {
                    throw new Exception("Value can't be positive infinity");
                }
                if (float.IsNegativeInfinity(RelativePosition.X))
                {
                    throw new Exception("Value can't be negative infinity");
                }
#endif
            }
        }

        /// <summary>
        /// Gets and sets the Y position relative to the instance's parent.
        /// </summary>
        /// <remarks>
        /// If the instance does not have a parent this property has no effect.
        /// </remarks>
        public float RelativeY
        {
            get { return RelativePosition.Y; }
            set 
            { 
                RelativePosition.Y = value;
#if DEBUG
                if (float.IsNaN(RelativePosition.Y))
                {
                    throw new NaNException("The RelativePosition Y value has been set to an invalid number (float.NaN)", "RelativeY");
                }
                if (float.IsPositiveInfinity(RelativePosition.Y))
                {
                    throw new Exception("Value can't be positive infinity");
                }
                if (float.IsNegativeInfinity(RelativePosition.Y))
                {
                    throw new Exception("Value can't be negative infinity");
                }
#endif
            }
        }

        /// <summary>
        /// Gets and sets the Z position relative to the instance's parent.
        /// </summary>
        /// <remarks>
        /// If the instance does not have a parent this property has no effect.
        /// </remarks>
        public float RelativeZ
        {
            get { return RelativePosition.Z; }
            set
            { 
                RelativePosition.Z = value;

#if DEBUG
                if (float.IsNaN(RelativePosition.Z))
                {
                    throw new NaNException("The RelativePosition Z value has been set to an invalid number (float.NaN)", "RelativeZ");
                }
#endif
            }
        }

        /// <summary>
        /// Gets and sets the rate of change of the RelativeX property in units per second. 
        /// </summary>
        public float RelativeXVelocity
        {
            get { return RelativeVelocity.X; }
            set { RelativeVelocity.X = value; }
        }

        /// <summary>
        /// Gets and sets the rate of change of the RelativeY property in units per second.
        /// </summary>
        public float RelativeYVelocity
        {
            get { return RelativeVelocity.Y; }
            set { RelativeVelocity.Y = value; }
        }

        /// <summary>
        /// Gets and sets the rate of change of the RelativeZ property in units per second.
        /// </summary>
        public float RelativeZVelocity
        {
            get { return RelativeVelocity.Z; }
            set { RelativeVelocity.Z = value; }
        }

        /// <summary>
        /// Gets and sets the rate of change of the RelativeXVelocity property in units per second.
        /// </summary>
        public float RelativeXAcceleration
        {
            get { return RelativeAcceleration.X; }
            set { RelativeAcceleration.X = value; }
        }

        /// <summary>
        /// Gets and sets the rate of change of the RelativeYVelocity property in units per second.
        /// </summary>
        public float RelativeYAcceleration
        {
            get { return RelativeAcceleration.Y; }
            set { RelativeAcceleration.Y = value; }
        }

        /// <summary>
        /// Gets and sets the rate of change of the RelativeZVelocity property in units per second.
        /// </summary>
        public float RelativeZAcceleration
        {
            get { return RelativeAcceleration.Z; }
            set { RelativeAcceleration.Z = value; }
        }

        /// <summary>
        /// Gets and sets the rotation on the X axis relative to the instance's parent.
        /// </summary>
        public float RelativeRotationX
        {
            get { return mRelativeRotationX; }
            set
            {
                mRelativeRotationX = value;
                FlatRedBall.Math.MathFunctions.RegulateAngle(ref mRelativeRotationX);

                mRelativeRotationMatrix = Matrix.CreateRotationX((float)mRelativeRotationX);
                mRelativeRotationMatrix *= Matrix.CreateRotationY((float)mRelativeRotationY);
                mRelativeRotationMatrix *= Matrix.CreateRotationZ((float)mRelativeRotationZ);
            }
        }

        /// <summary>
        /// Gets and sets the rotation on the Y axis relative to the instance's parent.
        /// </summary>
        public float RelativeRotationY
        {
            get { return mRelativeRotationY; }
            set
            {
                mRelativeRotationY = value;
                FlatRedBall.Math.MathFunctions.RegulateAngle(ref mRelativeRotationY);

                mRelativeRotationMatrix = Matrix.CreateRotationX((float)mRelativeRotationX);
                mRelativeRotationMatrix *= Matrix.CreateRotationY((float)mRelativeRotationY);
                mRelativeRotationMatrix *= Matrix.CreateRotationZ((float)mRelativeRotationZ);
            }
        }

        /// <summary>
        /// The z rotation of a PositionedObject relative to its parent in radians. Only applies if this instance is attached to a parent.
        /// </summary>
        /// <remarks>
        /// This rotates the PositionedObject about the Z axis if it is attached to a parent.  Rotation is represented in 
        /// radians.  Angles will always be greater than or equal to 0 and less than
        /// two times PI.  Values outside of these bounds will be regulated by the
        /// set property.
        /// 
        /// RelativeRotationZ can be used to "spin" a PositionedObject, with a positive variable spinning
        /// counterclockwise.  
        /// </remarks>
        public float RelativeRotationZ
        {
            get { return mRelativeRotationZ; }
            set
            {
                mRelativeRotationZ = value;

                FlatRedBall.Math.MathFunctions.RegulateAngle(ref mRelativeRotationZ);

                mRelativeRotationMatrix = Matrix.CreateRotationX((float)mRelativeRotationX);
                mRelativeRotationMatrix *= Matrix.CreateRotationY((float)mRelativeRotationY);
                mRelativeRotationMatrix *= Matrix.CreateRotationZ((float)mRelativeRotationZ);
            }
        }

        /// <summary>
        /// The rotation representing the orientation relative to the instance's Parent.
        /// </summary>
        /// <remarks>
        /// If the instance does not have a Parent then this property has no effect.
        /// </remarks>
        public Matrix RelativeRotationMatrix
        {
            get { return mRelativeRotationMatrix; }
            set
            {
                mRelativeRotationMatrix = value;

                mRelativeRotationX = (float)System.Math.Atan2(mRelativeRotationMatrix.M23, mRelativeRotationMatrix.M33);
                mRelativeRotationZ = (float)System.Math.Atan2(mRelativeRotationMatrix.M12, mRelativeRotationMatrix.M11);
                mRelativeRotationY = -(float)System.Math.Asin(mRelativeRotationMatrix.M13);
                if (mRelativeRotationX < 0)
                    mRelativeRotationX += MathHelper.TwoPi;
                if (mRelativeRotationY < 0)
                    mRelativeRotationY += MathHelper.TwoPi;
                if (mRelativeRotationZ < 0)
                    mRelativeRotationZ += MathHelper.TwoPi;

#if DEBUG
#if FRB_XNA
                if (FlatRedBall.Math.MathFunctions.IsOrthonormal(ref mRotationMatrix) == false)
                {
                    string message = "Matrix is not orthonormal.  ";
                    float epsilon = FlatRedBall.Math.MathFunctions.MatrixOrthonormalEpsilon;



                    if (System.Math.Abs(mRotationMatrix.Right.LengthSquared() - 1) > epsilon)
                    {
                        message += "The Right Vector is not of unit length. ";
                    }
                    if (System.Math.Abs(mRotationMatrix.Up.LengthSquared() - 1) > epsilon)
                    {
                        message += "The Up Vector is not of unit length. ";
                    }
                    if (System.Math.Abs(mRotationMatrix.Forward.LengthSquared() - 1) > epsilon)
                    {
                        message += "The Forward Vector is not of unit length. ";
                    }
                    if (Vector3.Dot(mRotationMatrix.Right, mRotationMatrix.Up) > epsilon)
                    {
                        message += "The Right and Up Vectors are not perpendicular.  Their Dot is non-zero. ";
                    }
                    if (Vector3.Dot(mRotationMatrix.Right, mRotationMatrix.Forward) > epsilon)
                    {
                        message += "The Right and Forward Vectors are not perpendicular.  Their dot is non-zero. ";
                    }
                    if (Vector3.Dot(mRotationMatrix.Up, mRotationMatrix.Forward) > epsilon)
                    {
                        message += "The Up and Forward Vectors are not perpendicular.  Their dot is non-zero. ";
                    }

                    throw new ArgumentException(message);
                }
#endif
#endif


            }

        }

        /// <summary>
        /// Gets and sets the rate of change of the RelativeRotationX property in units per second.
        /// </summary>
        public float RelativeRotationXVelocity
        {
            get { return mRelativeRotationXVelocity; }
            set { mRelativeRotationXVelocity = value; }
        }

        /// <summary>
        /// Gets and sets the rate of change of the RelativeRotationY property in units per second.
        /// </summary>
        public float RelativeRotationYVelocity
        {
            get { return mRelativeRotationYVelocity; }
            set { mRelativeRotationYVelocity = value; }
        }

        /// <summary>
        /// Gets and sets the rate of change of the RelativeRotationZ property in units per second.
        /// </summary>
        public float RelativeRotationZVelocity
        {
            get { return mRelativeRotationZVelocity; }
            set { mRelativeRotationZVelocity = value; }
        }

        /// <summary>
        /// The last time Update was called.
        /// </summary>
        /// <remarks>
        /// This value is set through the TimeManager's CurrentTime property.
        /// </remarks>
        public double LastDependencyUpdate
        {
            get { return mLastDependencyUpdate; }
            internal set { mLastDependencyUpdate = value; }
        }

        /// <summary>
        /// The x rotation of a PositionedObject
        /// </summary>
        /// <remarks>
        /// This rotates the PositionedObject about the X axis.  Rotation is represented in 
        /// radians.  Angles will always be greater than or equal to 0 and less than
        /// two times PI.  Values outside of these bounds will be regulated by the
        /// set property.
        /// 
        /// RotationX can be used to flip an image of a PositionedObject, but a PositionedObject should not 
        /// be flipped during animation (if it is a Sprite).  AnimationFrames can be flipped without 
        /// setting a Sprite's Rotation values.
        /// </remarks>
        public float RotationX
        {
            get
            {
                return mRotationX;
            }
            set
            {
                mRotationX = value;
                FlatRedBall.Math.MathFunctions.RegulateAngle(ref mRotationX);

                mRotationMatrix = Matrix.CreateRotationX(mRotationX);
                mRotationMatrix *= Matrix.CreateRotationY(mRotationY);
                mRotationMatrix *= Matrix.CreateRotationZ(mRotationZ);
            }
        }

        /// <summary>
        /// The y rotation of a PositionedObject
        /// </summary>
        /// <remarks>
        /// This rotates the PositionedObject about the Y axis.  Rotation is represented in 
        /// radians.  Angles will always be greater than or equal to 0 and less than
        /// two times PI.  Values outside of these bounds will be regulated by the
        /// set property.
        /// 
        /// RotationY can be used to flip an image and set it upside down, but a PositionedObject
        /// should not be flipped during animation (if it is a Sprite).  AnimationFrames can be flipped without
        /// setting a Sprite's Rotation values.
        /// </remarks>
        public float RotationY
        {
            get
            {
                return mRotationY;
            }
            set
            {

                mRotationY = value;
                FlatRedBall.Math.MathFunctions.RegulateAngle(ref mRotationY);

                Matrix xRotationMatrix = Matrix.CreateRotationX(mRotationX);
                Matrix yRotationMatrix = Matrix.CreateRotationY(mRotationY);
                mRotationMatrix = xRotationMatrix;
                mRotationMatrix *= yRotationMatrix;

                Matrix zRotationMatrix = Matrix.CreateRotationZ(mRotationZ);
                mRotationMatrix *= zRotationMatrix;

            }
        }

        /// <summary>
        /// The z rotation of a PositionedObject in radians.
        /// </summary>
        /// <remarks>
        /// This rotates the PositionedObject about the Z axis.  Rotation is represented in 
        /// radians.  Angles will always be greater than or equal to 0 and less than
        /// two times PI.  Values outside of these bounds will be regulated by the
        /// set property.
        /// 
        /// RotationZ can be used to "spin" a PositionedObject, with a positive variable spinning
        /// counterclockwise.  
        /// </remarks>
        public float RotationZ
        {
            get
            {
                return mRotationZ;
            }
            set
            {

                mRotationZ = value;
                FlatRedBall.Math.MathFunctions.RegulateAngle(ref mRotationZ);

                if(mRotationX == 0 && mRotationY == 0)
                {
                    mRotationMatrix = Matrix.CreateRotationZ(mRotationZ);
                }
                else
                {
                    mRotationMatrix = Matrix.CreateRotationX(mRotationX);
                    mRotationMatrix *= Matrix.CreateRotationY(mRotationY);
                    mRotationMatrix *= Matrix.CreateRotationZ(mRotationZ);
                }
            }
        }

        /// <summary>
        /// The matrix applied to the object resulting in its final orientation.
        /// </summary>
        /// <remarks>
        /// The RotationMatrix and RotationX, RotationY, RotationZ reflect eachother.  Changing one will change the other.
        /// <seealso cref="RotationX"/>
        /// <seealso cref="RotationY"/>
        /// <seealso cref="RotationZ"/>
        /// </remarks>
        public Matrix RotationMatrix
        {
            get
            {
                return mRotationMatrix;
            }
            set
            {
                mRotationMatrix = value;

                mRotationX = (float)System.Math.Atan2(mRotationMatrix.M23, mRotationMatrix.M33);
                mRotationZ = (float)System.Math.Atan2(mRotationMatrix.M12, mRotationMatrix.M11);
                // Not sure how we got by with this for so long, but it is wrong.  This fails in
                // the unit tests when there is a rotation value set after 1.5 (like 1.7 or 2)
                //mRotationY = -(float)System.Math.Asin(mRotationMatrix.M13);
                mRotationY = (float)System.Math.Atan2(-mRotationMatrix.M13, mRotationMatrix.M11);
                if (mRotationX < 0)
                    mRotationX += MathHelper.TwoPi;
                if (mRotationY < 0)
                    mRotationY += MathHelper.TwoPi;
                if (mRotationZ < 0)
                    mRotationZ += MathHelper.TwoPi;

#if DEBUG
                // Make sure matrix is invertable
                if (mRotationMatrix.M44 == 0)
                {
                    throw new ArgumentException("M44 of the Camera is 0 - this makes the rotation non-invertable.");
                }

                // Make sure the matrix doesn't have translation
                if (mRotationMatrix.M41 != 0 ||
                    mRotationMatrix.M42 != 0 ||
                    mRotationMatrix.M43 != 0)
                {
                     throw new ArgumentException("The translation on the matrix is not 0.  It is " + value.Translation);

                }

                if (FlatRedBall.Math.MathFunctions.IsOrthonormal(ref mRotationMatrix) == false)
                {
                    string message = "Matrix is not orthonormal.  ";
                    float epsilon = FlatRedBall.Math.MathFunctions.MatrixOrthonormalEpsilon;


                    if (System.Math.Abs(mRotationMatrix.Right.LengthSquared() - 1) > epsilon)
                    {
                        message += "The Right Vector is not of unit length. ";
                    }
                    if (System.Math.Abs(mRotationMatrix.Up.LengthSquared() - 1) > epsilon)
                    {
                        message += "The Up Vector is not of unit length. ";
                    }
                    if (System.Math.Abs(mRotationMatrix.Forward.LengthSquared() - 1) > epsilon)
                    {
                        message += "The Forward Vector is not of unit length. ";
                    }
                    if (Vector3.Dot(mRotationMatrix.Right, mRotationMatrix.Up) > epsilon)
                    {
                        message += "The Right and Up Vectors are not perpendicular.  Their Dot is non-zero. ";
                    }
                    if (Vector3.Dot(mRotationMatrix.Right, mRotationMatrix.Forward) > epsilon)
                    {
                        message += "The Right and Forward Vectors are not perpendicular.  Their dot is non-zero. ";
                    }
                    if (Vector3.Dot(mRotationMatrix.Up, mRotationMatrix.Forward) > epsilon)
                    {
                        message += "The Up and Forward Vectors are not perpendicular.  Their dot is non-zero. ";
                    }


                    throw new ArgumentException(message);
                }
#endif
            }



        }

        /// <summary>
        /// Gets or sets the overall transformation of this object
        /// </summary>
        /// <remarks>
        /// Changing the transformation matrix will change rotation, position and scaling
        /// </remarks>
        public virtual Matrix TransformationMatrix
        {
            get
            {
                return RotationMatrix * Matrix.CreateTranslation(Position);
            }
            set
            {
                // Get position
                Position = value.Translation;

                // Get scale
                Vector3 finalScale = new Vector3(
                    (float)System.Math.Sqrt((double)(
                        value.M11 * value.M11 +
                        value.M12 * value.M12 +
                        value.M13 * value.M13)),
                    (float)System.Math.Sqrt((double)(
                        value.M21 * value.M21 +
                        value.M22 * value.M22 +
                        value.M23 * value.M23)),
                    (float)System.Math.Sqrt((double)(
                        value.M31 * value.M31 +
                        value.M32 * value.M32 +
                        value.M33 * value.M33)));

                // Get rotation
                Vector3 finalRot = new Vector3(
                     (float)System.Math.Atan2(value.M23 / finalScale.Y, value.M33 / finalScale.Z),
                     -(float)System.Math.Asin(value.M13 / finalScale.X),
                     (float)System.Math.Atan2(value.M12 / finalScale.X, value.M11 / finalScale.X));

                if (finalRot.X < 0) finalRot.X += MathHelper.TwoPi;
                if (finalRot.Y < 0) finalRot.Y += MathHelper.TwoPi;
                if (finalRot.Z < 0) finalRot.Z += MathHelper.TwoPi;

                mRotationX = finalRot.X;
                mRotationY = finalRot.Y;
                // Forces the rotation matrix to update
                RotationZ = finalRot.Z;
            }
        }

        /// <summary>
        /// Gets or sets the overall transformation of this object, relative to its parent
        /// </summary>
        /// <remarks>
        /// Changing the transformation matrix will change rotation, position and scaling
        /// </remarks>
        public virtual Matrix RelativeTransformationMatrix
        {
            get { return RelativeRotationMatrix * Matrix.CreateTranslation(RelativePosition); }
            set
            {
                // Get position
                RelativePosition = value.Translation;

                // Get scale
                Vector3 finalScale = new Vector3(
                    (float)System.Math.Sqrt((double)(
                        value.M11 * value.M11 +
                        value.M12 * value.M12 +
                        value.M13 * value.M13)),
                    (float)System.Math.Sqrt((double)(
                        value.M21 * value.M21 +
                        value.M22 * value.M22 +
                        value.M23 * value.M23)),
                    (float)System.Math.Sqrt((double)(
                        value.M31 * value.M31 +
                        value.M32 * value.M32 +
                        value.M33 * value.M33)));

                // Get rotation
                Vector3 finalRot = new Vector3(
                     (float)System.Math.Atan2(value.M23 / finalScale.Y, value.M33 / finalScale.Z),
                     -(float)System.Math.Asin(value.M13 / finalScale.X),
                     (float)System.Math.Atan2(value.M12 / finalScale.X, value.M11 / finalScale.X));

                if (finalRot.X < 0) finalRot.X += MathHelper.TwoPi;
                if (finalRot.Y < 0) finalRot.Y += MathHelper.TwoPi;
                if (finalRot.Z < 0) finalRot.Z += MathHelper.TwoPi;

                mRelativeRotationX = finalRot.X;
                mRelativeRotationY = finalRot.Y;
                // Forces the relative rotation matrix to update
                RelativeRotationZ = finalRot.Z;
            }
        }

        /// <summary>
        /// The absolute X rotation speed measured in radians per second
        /// </summary>
        /// <remarks>
        /// The RotationXVelocity variable is how fast a PositionedObject is rotating on the X axis. It is
        /// measured in radians per second.
        /// </remarks>
        public float RotationXVelocity
        {
            get => mRotationXVelocity; 
            set => mRotationXVelocity = value; 
        }

        /// <summary>
        /// The absolute Y rotation speed measured in radians per second
        /// </summary>
        /// <remarks>
        /// The RotationYVelocity variable is how fast a PositionedObject is rotating on the Y axis. It is
        /// measured in radians per second.
        /// </remarks>
        public float RotationYVelocity
        {
            get => mRotationYVelocity; 
            set => mRotationYVelocity = value; 
        }

        /// <summary>
        /// The absolute Z rotation speed measured in radians per second
        /// </summary>
        /// <remarks>
        /// The RotationZVelocity variable is how fast a PositionedObject is rotating on the Z axis. It is
        /// measured in radians per second.
        /// </remarks>
        public float RotationZVelocity
        {
            get { return mRotationZVelocity; }
            set
            {
#if DEBUG
                if (float.IsInfinity(value) || float.IsNegativeInfinity(value))
                {
                    throw new Exception("Invalid RotationZVelocity value");
                }
#endif
                mRotationZVelocity = value;
            }
        }

        /// <summary>
        /// Returns the top node in the attachment hierarchical relationship
        /// </summary>
        public PositionedObject TopParent
        {
            get
            {
                if (this.mParent != null) return mParent.TopParent;
                else return this;
            }
        }

        /// <summary>
        /// The absolute X position.
        /// </summary>
        [CategoryAttribute("Position")]
        public float X
        {
            get { return Position.X; }
            set 
            { 
                Position.X = value; 

#if DEBUG
                if (float.IsNaN(Position.X))
                {
                    throw new NaNException("The X value has been set to an invalid number (float.NaN)", "X");
                }
#endif
            
            }
        }

        /// <summary>
        /// The absolute Y position.
        /// </summary>
        [CategoryAttribute("Position")]
        public float Y
        {
            get { return Position.Y; }
            set 
            { 
                Position.Y = value;

#if DEBUG
                if (float.IsNaN(Position.Y))
                {
                    throw new NaNException("The Y value has been set to an invalid number (float.NaN)", "Y");
                }
#endif
            }
        }

        /// <summary>
        /// The absolute Z position.
        /// </summary>
        [CategoryAttribute("Position")]
        public float Z
        {
            get { return Position.Z; }
            set 
            { 
                Position.Z = value;

#if DEBUG
                if (float.IsNaN(Position.Z))
                {
                    throw new NaNException("The Z value has been set to an invalid number (float.NaN)", "Z");
                }
#endif
            }
        }

        /// <summary>
        /// Gets and sets the absolute X Velocity.  Measured in units per second.
        /// </summary>
        public float XVelocity
        {
            get { return Velocity.X; }
            set { Velocity.X = value; }
        }

        /// <summary>
        /// Gets and sets the absolute Y Velocity.  Measured in units per second.
        /// </summary>
        public float YVelocity
        {
            get { return Velocity.Y; }
            set { Velocity.Y = value; }
        }

        /// <summary>
        /// Gets and sets the absolute Z Velocity.  Measured in units per second.
        /// </summary>
        public float ZVelocity
        {
            get { return Velocity.Z; }
            set { Velocity.Z = value; }
        }

        /// <summary>
        /// Gets and sets the absolute X Acceleration.  Measured in units per second.
        /// </summary>
        public float XAcceleration
        {
            get { return Acceleration.X; }
            set { Acceleration.X = value; }
        }

        /// <summary>
        /// Gets and sets the absolute Y Acceleration.  Measured in units per second.
        /// </summary>
        public float YAcceleration
        {
            get { return Acceleration.Y; }
            set { Acceleration.Y = value; }
        }

        /// <summary>
        /// Gets and sets the absolute Z Acceleration.  Measured in units per second.
        /// </summary>
        public float ZAcceleration
        {
            get { return Acceleration.Z; }
            set { Acceleration.Z = value; }
        }

        /// <summary>
        /// Whether the PositionedObject's RealVelocity and RealAcceleration are
        /// updated every frame.
        /// </summary>
        public bool KeepTrackOfReal
        {
            get { return mKeepTrackOfReal; }
            set
            {
                mKeepTrackOfReal = value;
            }
        }

        /// <summary>
        /// Linear approximation of drag.  This reduces the Velocity of the
        /// instance according to its absolute Velocity.  
        /// </summary>
        /// <remarks>
        /// The following formula is applied to apply Drag:
        /// <para>
        /// Velocity -= Velocity * Drag * TimeManager.SecondDifference;
        /// Note that a very large Drag value may result in an object moving in the opposite 
        /// direction.
        /// </para>
        /// </remarks>
        public float Drag
        {
            get { return mDrag; }
            set { mDrag = value; }
        }

        /// <summary>
        /// The list of Instructions that this instance owns.  These instructions usually
        /// will execute on this instance; however, this is not a requirement.
        /// </summary>
        public InstructionList Instructions
        {
            get { return mInstructions; }
        }

        /// <summary>
        /// Where the PositionedObject was created. This can be Glue, Tiled, or other tools. It is used
        /// when the game is in edit mode to determine if the PositionedObject can be edited.
        /// </summary>
        public string CreationSource
        {
            get;set;
        }

        #endregion

        #region Methods

        #region Constructor

        #region XML Docs
        /// <summary>
        /// Creates a new PositionedObject instance.
        /// </summary>
        #endregion
        public PositionedObject()
        {
            this.mListsBelongingTo = new List<IAttachableRemovable>();
            mRotationMatrix = Matrix.Identity;
            mRelativeRotationMatrix = Matrix.Identity;
            mChildren = new AttachableList<PositionedObject>();

            mParentRotationChangesPosition = true;
            mParentRotationChangesRotation = true;

            mInstructions = new InstructionList();

            // the first frame of the game's time is 0.  So that objects
            // update during the first frame, set the time to -1.
            mLastDependencyUpdate = -1;
        }

        #endregion

        #region Public Static Methods

        #region XML Docs
        /// <summary>
        /// Determines whether the tree structure created by attachments of two PositionedObjects are identical.
        /// </summary>
        /// <remarks>
        /// This method does not investigate the PositionedObjects any more than looking at their children.  It traverses
        /// through the trees multiple times, and can be a very slow method if the PositionedObjects have large subtrees.  
        /// The subtrees are small enough in most cases
        /// where there won't be a performance issue.
        /// </remarks>
        /// <param name="s1">The first PositionedObject.</param>
        /// <param name="s2">The second PositionedObject.</param>
        /// <returns>Whether the tree structure created by attachments is the same.</returns>
        #endregion
        public static bool AreSubHierarchiesIdentical(PositionedObject s1, PositionedObject s2)
        {
            if (s1.mChildren.Count != s2.mChildren.Count)
                return false;

            for (int i = 0; i < s1.Children.Count; i++)
            {
                if (AreSubHierarchiesIdentical(
                                                (s1.mChildren[i]),
                                                (s2.mChildren[i])) == false)
                {
                    return false;
                }
            }
            return true;


        }


        #endregion

        #region Public Methods

        // Not sure why AttachTo is virtual, it just makes things slower
        public void AttachTo(PositionedObject newParent)
        {
            this.AttachTo(newParent, changeRelative: false);
        }

        /// <summary>
        /// Attaches this PositionedObject to the argument newParent.
        /// </summary>
        /// <remarks>
        /// <para>A useful way to understand the affect of changeRelative is to consider that it is the opposite of whether 
        /// absolute values change.  That is, if the relative values do not change upon attachment, the absolute values
        /// will change.</para>
        /// <para>
        /// For an example, consider a situation where a child has an absolute x value of 5 and a relative value of 0 and
        /// a parent has an absolute x value of 0.  If the child is attached to the parent
        /// and relative is not changed (relative x remains at 0), then the child's x will be 
        /// parent.x + child.relX, or 0 + 0 = 0.  We see that the relative value didn't 
        /// change, so the absolute did.
        /// </para>
        /// <para>
        /// If, on the other hand, the relative was changed, the absolute position would be the same.
        /// Since the parent's absolute position was 0 and the absolute position of the 
        /// child should remain at 5, then the relX value would change to 5 (assuming 
        /// the parent isn't rotated).
        /// </para>
        /// </remarks>
        /// <param name="newParent">The PositionedObject to attach to.</param>
        /// <param name="changeRelative">Whether relative values should change so that absolute values stay the same.</param>
        public virtual void AttachTo(PositionedObject newParent, bool changeRelative)
        {
            // make sure we don't attach this PO to its parent.  Doing this would make the parent
            // have two copies of the same PO in its Children array.
            // Update June 9, 2011
            // Not sure what the above
            // comment means, but setting
            // mParent to null causes an accumulation
            // of mChildren if calling AttachTo on the
            // current parent.  This also seems to not be
            // a useful method given that we change mParent
            // in all cases below.
            //mParent = null;
            // Update October 24, 2012
            // I now know what the above
            // comment was all about - it
            // was an effort to prevent circular
            // dependencies.  But just setting the
            // mParent to null seems like it wasn't
            // the best way to go about that.  Instead
            // we want to do a proper circular dependency
            // check.  I'm going to do it in DEBUG because
            // it could be inefficient.
#if DEBUG
            if (newParent == this)
            {
                throw new ArgumentException("Can't attach an object to itself.");
            }

            if (newParent != null && this.IsParentOf(newParent))
            {
                throw new Exception("This attachment cannot be performed because it would result in a circular reference - ultimately causing a stack overflow exception");
            }

#endif

            if (newParent == null)
            {
                Detach();
                return;
            }

            if (newParent == mParent)
            {
                if (!newParent.mChildren.Contains(this))
                {
                    newParent.mChildren.Add(this);
                }
            }
            else
            {
                // First clean up the parent's old relationship
                if (mParent != null) mParent.mChildren.Remove(this);

                // finally reassign the dependentOn and establish a 2 way relationship
                mParent = newParent;
                newParent.mChildren.Add(this);
            }
            if (changeRelative)
            {
                SetRelativeFromAbsolute();
            }
        }


        /// <summary>
        /// Detaches this PositionedObject from its parent and detaches all of the PositionedObject's 
        /// Children.
        /// </summary>
        public void ClearRelationships()
        {// first we tell any sprites that this is attached to to let go
            if (mParent != null)
            {
                mParent.mChildren.Remove(this);
                mParent = null;
            }
            // now we make this sprite's children let go
            for (int i = 0; i < mChildren.Count; i++)
            {
                mChildren[i].Detach();
            }
            mChildren.Clear();
        }

        /// <summary>
        /// Copies most internal fields of this instance to the argument PositionedObjects.
        /// </summary>
        /// <remarks>
        /// The following fields are not copied:
        /// <para>* Name</para>
        /// <para>* ListsBelongingTo</para>
        /// <para>* Parent</para>
        /// <para>* Children</para>
        /// <para>* Instructions</para>
        /// </remarks>
        /// <param name="positionedObject"></param>
        public void CopyFieldsTo(PositionedObject positionedObject)
        {
            positionedObject.Position = Position;
            positionedObject.RelativePosition = RelativePosition;

            positionedObject.Velocity = Velocity;
            positionedObject.RelativeVelocity = RelativeVelocity;

            positionedObject.Acceleration = Acceleration;
            positionedObject.RelativeAcceleration = RelativeAcceleration;

            positionedObject.mKeepTrackOfReal = mKeepTrackOfReal;
            positionedObject.RealVelocity = RealVelocity;
            positionedObject.RealAcceleration = RealAcceleration;

            positionedObject.LastPosition = LastPosition;
            positionedObject.LastVelocity = LastVelocity;

            positionedObject.mRotationX = mRotationX;
            positionedObject.mRotationY = mRotationY;

            positionedObject.RotationZ = RotationZ;

            positionedObject.mRelativeRotationX = mRelativeRotationX;
            positionedObject.mRelativeRotationY = mRelativeRotationY;
            // use the property for the last setting to set the mRelativeRotationMatrix
            positionedObject.RelativeRotationZ = mRelativeRotationZ;

            positionedObject.mParentRotationChangesPosition = mParentRotationChangesPosition;
            positionedObject.mParentRotationChangesRotation = mParentRotationChangesRotation;

        }

        /// <summary>
        /// Copies the absolute position and rotation values to the relative values.
        /// </summary>
        public void CopyAbsoluteToRelative()
        {
            RelativePosition = Position;
            RelativeRotationMatrix = RotationMatrix;

            // Do we want to copy velocities over?
            //positionedObject.RelativeVelocity = positionedObject.Velocity;
            //positionedObject.RelativeAcceleration = positionedObject.Acceleration;


            //positionedObject.RelativeRotationXVelocity = positionedObject.RotationXVelocity;
            //positionedObject.RelativeRotationYVelocity = positionedObject.RotationYVelocity;
            //positionedObject.RelativeRotationZVelocity = positionedObject.RotationZVelocity;
        }

        /// <summary>
        /// Creates a clone of this instance typed as a T.
        /// </summary>
        /// <typeparam name="T">The type of the new object.</typeparam>
        /// <returns>The newly created instance.</returns>
        public virtual T Clone<T>() where T : PositionedObject, new()
        {
            if (this is T == false)
            {
                throw new InvalidCastException("Type passed in Clone method does not match the type of object calling the method");

            }

            T newObject = (T)(this.MemberwiseClone());

            newObject.mParent = mParent;

            newObject.mListsBelongingTo = new List<IAttachableRemovable>();
            newObject.mChildren = new AttachableList<PositionedObject>();
            newObject.mInstructions = new FlatRedBall.Instructions.InstructionList();

            return newObject;
        }

        /// <summary>
        /// Creates a new Children PositionedObjectList.
        /// </summary>
        /// <remarks>
        /// This method is only necessary if a PositionedObject
        /// is manually cloned by calling MemberwiseClone.  This
        /// should not be called to detach children as the two-way
        /// relationship between PositionedObject and PositionedObjectList
        /// will keep the old Children PositionedObjectList in scope resulting
        /// in a memory leak.
        /// </remarks>
        public void CreateNewChildrenList()
        {
            mChildren = new AttachableList<PositionedObject>();
        }

        /// <summary>
        /// Creates a new InstructionList.
        /// </summary>
        /// <remarks>
        /// This method is only necessary if a PositionedObject
        /// is manually cloned by calling MemberwiseClone.  This
        /// should not be called to clear the Instructions property
        /// as it will create a new instance and get rid fo the old one
        /// resulting in unnecessary garbage collection.
        /// </remarks>
        public void CreateNewInstructionsList()
        {
            mInstructions = new InstructionList();
        }

        /// <summary>
        /// Detaches the PositionedObject from its parent PositionedObject.
        /// </summary>
        /// <remarks>
        /// This method cleans up the two way relationship between parent and child.
        /// </remarks>
        public virtual void Detach()
        {
            if (mParent == null)
                return;
            mParent.mChildren.Remove(this);
            mParent = null;

        }

        /// <summary>
        /// Executes instructions according to the argument currentTime and cycles and reorders
        /// Instructions as necessary.
        /// </summary>
        /// <param name="currentTime">The current time to compare the instruction's TimeToExecute against.</param>
        public void ExecuteInstructions(double currentTime)
        {
            InstructionManager.ExecuteInstructionsOnConsideringTime(this, currentTime);
        }

        /// <summary>
        /// Forces an update of the PositionedObject's absolute position and rotation values 
        /// according to its attachment and relative values.
        /// </summary>
        /// <remarks>
        /// The absolute positions and rotations of Sprites are updated in the 
        /// Sprite.UpdateDependencies method which is
        /// called in the SpriteManager.UpdateDependencies.  The SpriteManager.UpdateDependencies is called
        /// once per frame by default in the Sprite's regular activity.  This method only needs to be called 
        /// if changes are made after
        /// the UpdateDependencies method has been called for that particular frame or if updated 
        /// positions are needed
        /// immediately after relative values or attachments have been changed.
        /// 
        /// <para>This method will recur up the hierarchical PositionedObject struture stopping 
        /// when it hits the top parent.</para>
        /// </remarks>
        public virtual void ForceUpdateDependencies()
        {
            if (mParent != null)
            {
                mParent.ForceUpdateDependencies();


                #region Apply dependency update

                    if (mParentRotationChangesRotation)
                    {
                        // Set the property RotationMatrix rather than the field mRotationMatrix
                        // so the individual rotation values get updated.
                        RotationMatrix = mRelativeRotationMatrix * mParent.mRotationMatrix;
                    }
                    else
                    {
                        RotationMatrix = mRelativeRotationMatrix;
                    }

                    if (!IgnoreParentPosition)
                    {
                        if (mParentRotationChangesPosition)
                        {

                            Position.X = mParent.Position.X +
                                mParent.mRotationMatrix.M11 * RelativePosition.X +
                                mParent.mRotationMatrix.M21 * RelativePosition.Y +
                                mParent.mRotationMatrix.M31 * RelativePosition.Z;

                            Position.Y = mParent.Position.Y +
                                mParent.mRotationMatrix.M12 * RelativePosition.X +
                                mParent.mRotationMatrix.M22 * RelativePosition.Y +
                                mParent.mRotationMatrix.M32 * RelativePosition.Z;

                            Position.Z = mParent.Position.Z +
                                mParent.mRotationMatrix.M13 * RelativePosition.X +
                                mParent.mRotationMatrix.M23 * RelativePosition.Y +
                                mParent.mRotationMatrix.M33 * RelativePosition.Z;

                        }
                        else
                        {
                            Position = RelativePosition + mParent.Position;
                        }
                    }
#if DEBUG
                    if (float.IsNaN(Position.Z))
                    {
                        string error = "The PositionedObject of type " + this.GetType() + " has a " +
                            "NaN on its Z property.  Its name is \"" + this.Name + "\".  ";

                        if (this.Parent != null)
                        {
                            error += "Its parent is of type " + this.Parent.GetType() + " and its name is \"" + this.Parent.Name + "\".";
                        }
                        else
                        {
                            error += "This object does not have a parent";
                        }
                        throw new Exception(error);
                    }
#endif

                    #endregion
            }
        }



        public virtual void ForceUpdateDependenciesDeep()
        {

            if (mParent != null)
            {

                #region Apply dependency update

                    if (mParentRotationChangesRotation)
                    {
                        // Set the property RotationMatrix rather than the field mRotationMatrix
                        // so the individual rotation values get updated.
                        RotationMatrix = mRelativeRotationMatrix * mParent.mRotationMatrix;
                    }
                    else
                    {
                        RotationMatrix = mRelativeRotationMatrix;
                    }

                    if (!IgnoreParentPosition)
                    {
                        if (mParentRotationChangesPosition)
                        {

                            Position.X = mParent.Position.X +
                                mParent.mRotationMatrix.M11 * RelativePosition.X +
                                mParent.mRotationMatrix.M21 * RelativePosition.Y +
                                mParent.mRotationMatrix.M31 * RelativePosition.Z;

                            Position.Y = mParent.Position.Y +
                                mParent.mRotationMatrix.M12 * RelativePosition.X +
                                mParent.mRotationMatrix.M22 * RelativePosition.Y +
                                mParent.mRotationMatrix.M32 * RelativePosition.Z;

                            Position.Z = mParent.Position.Z +
                                mParent.mRotationMatrix.M13 * RelativePosition.X +
                                mParent.mRotationMatrix.M23 * RelativePosition.Y +
                                mParent.mRotationMatrix.M33 * RelativePosition.Z;

                        }
                        else
                        {
                            Position = RelativePosition + mParent.Position;
                        }
                    }
#if DEBUG
                    if (float.IsNaN(Position.Z))
                    {
                        string error = "The PositionedObject of type " + this.GetType() + " has a " +
                            "NaN on its Z property.  Its name is \"" + this.Name + "\".  ";

                        if (this.Parent != null)
                        {
                            error += "Its parent is of type " + this.Parent.GetType() + " and its name is \"" + this.Parent.Name + "\".";
                        }
                        else
                        {
                            error += "This object does not have a parent";
                        }
                        throw new Exception(error);
                    }
#endif

                    #endregion

            }


            for (int i = mChildren.Count - 1; i > -1; i--)
            {
                mChildren[i].ForceUpdateDependenciesDeep();
            }
        }

        /// <summary>
        /// Fills the argument list with the instance's parent, grandparent, etc. recursively.
        /// </summary>
        /// <param name="positionedObjects">The list to fill.</param>
        public void GetAllDescendantsOneWay(AttachableList<PositionedObject> positionedObjects)
        {
            for (int i = 0; i < mChildren.Count; i++)
            {
                PositionedObject po = mChildren[i];
                positionedObjects.AddOneWay(po);
                po.GetAllDescendantsOneWay(positionedObjects);
            }
        }

        /// <summary>
        /// Resets all properties to their default values and clears the ListsBelongingTo property.
        /// </summary>
        public virtual void Initialize()
        {
            Initialize(true);
        }

        /// <summary>
        /// Resets all properties to their default values.
        /// </summary>
        /// <param name="clearListsBelongingTo">Whether the instance should clear its ListsBelongingTo property.</param>
        public virtual void Initialize(bool clearListsBelongingTo)
        {
            Position.X = 0;
            Position.Y = 0;
            Position.Z = 0;

            RelativePosition.X = 0;
            RelativePosition.Y = 0;
            RelativePosition.Z = 0;

            Velocity.X = 0;
            Velocity.Y = 0;
            Velocity.Z = 0;

            RelativeVelocity.X = 0;
            RelativeVelocity.Y = 0;
            RelativeVelocity.Z = 0;

            Acceleration.X = 0;
            Acceleration.Y = 0;
            Acceleration.Z = 0;

            RelativeAcceleration.X = 0;
            RelativeAcceleration.Y = 0;
            RelativeAcceleration.Z = 0;

            LastPosition.X = 0;
            LastPosition.Y = 0;
            LastPosition.Z = 0;

            LastVelocity.X = 0;
            LastVelocity.Y = 0;
            LastVelocity.Z = 0;

            mKeepTrackOfReal = false;

            mRotationMatrix = Matrix.Identity;
            mRotationX = 0;
            mRotationY = 0;
            mRotationZ = 0;

            mRotationXVelocity = 0;
            mRotationYVelocity = 0;
            mRotationZVelocity = 0;

            mRelativeRotationX = 0;
            mRelativeRotationY = 0;
            mRelativeRotationZ = 0;

            mRelativeRotationMatrix = Matrix.Identity;

            mRelativeRotationXVelocity = 0;
            mRelativeRotationYVelocity = 0;
            mRelativeRotationZVelocity = 0;

            if (mParent != null)
                Detach();

            mParentRotationChangesPosition = true;
            mParentRotationChangesRotation = true;

            mName = "";


            mInstructions.Clear();

            if (clearListsBelongingTo)
            {
                mListsBelongingTo.Clear();
            }
        }

        /// <summary>
        /// Determines whether this is a parent (or grandparent of any level) of the argument
        /// PositionedObject
        /// </summary>
        /// <param name="attachable">The PositionedObject to test whether it is lower 
        /// in the same hiearchical structure.</param>
        /// <returns>Whether the attachable argument is a child of this instance.</returns>
        public bool IsParentOf(IAttachable attachable)
        {
            if (attachable == this)
                return false;

            for (int i = 0; i < mChildren.Count; i++)
            {
                if (mChildren[i] == attachable || mChildren[i].IsParentOf(attachable))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Stops all automatic behavior and stores the necessary instructions to 
        /// resume activity in the argument InstructionList.
        /// </summary>
        /// <param name="instructions">The List to store instructions which are executed to
        /// resume activity.</param>
        public virtual void Pause(InstructionList instructions)
        {
            FlatRedBall.Instructions.Pause.PositionedObjectUnpauseInstruction<PositionedObject> instruction =
                new FlatRedBall.Instructions.Pause.PositionedObjectUnpauseInstruction<PositionedObject>(this);

            instruction.Stop(this);

            instructions.Add(instruction);
        }

        /// <summary>
        /// Removes this instance from all Lists that it shares two-way. In practice this removes 
        /// all objects from their Parent, managers, and any lists that store this in custom code.
        /// relationships with.
        /// </summary>
        /// <remarks>
        /// FlatRedBall managers use this method in Remove methods.
        /// </remarks>
        public void RemoveSelfFromListsBelongingTo()
        {
            int index = mListsBelongingTo.Count - 1;

            for (int i = index; i > -1; i--)
            {

                this.mListsBelongingTo[i].RemoveGuaranteedContain(this);
            }
        }

        /// <summary>
        /// Sets the internal storage of the last frame's position and velocity
        /// so that the next frame's real velocity and acceleration values will be
        /// Vector3.Zero.
        /// </summary>
        public void ResetRealValues()
        {
            LastPosition = Position;
            LastVelocity = Velocity;
        }

        /// <summary>
        /// Copies all values related to "Real" values from the argument
        /// PositionedObject to this instance.
        /// </summary>
        /// <param name="positionedObject">The PositionedObject to copy Real values from.</param>
        public void SetRealValuesFrom(PositionedObject positionedObject)
        {
            mKeepTrackOfReal = positionedObject.mKeepTrackOfReal;
            RealVelocity = positionedObject.RealVelocity;
            RealAcceleration = positionedObject.RealAcceleration;

            LastPosition = positionedObject.LastPosition;
            LastVelocity = positionedObject.LastVelocity;
        }

        /// <summary>
        /// Uses the object's absolute position and orientation along with its
        /// Parent's orientation and position to update its relative position and orientation.
        /// </summary>
        public void SetRelativeFromAbsolute()
        {
#if DEBUG
            if (mParent == null)
            {
                throw new InvalidOperationException("This method can only be called if the object has a valid Parent");
            }
#endif

            Vector3 tempVector = Position - mParent.Position;
            // TODO:  Is this right?
            Matrix invertedMatrix = Matrix.Invert(mParent.mRotationMatrix);

            RelativePosition.X =
                invertedMatrix.M11 * tempVector.X +
                invertedMatrix.M21 * tempVector.Y +
                invertedMatrix.M31 * tempVector.Z;

            RelativePosition.Y =
                invertedMatrix.M12 * tempVector.X +
                invertedMatrix.M22 * tempVector.Y +
                invertedMatrix.M32 * tempVector.Z;

            RelativePosition.Z =
                invertedMatrix.M13 * tempVector.X +
                invertedMatrix.M23 * tempVector.Y +
                invertedMatrix.M33 * tempVector.Z;


            RelativeRotationMatrix = mRotationMatrix * invertedMatrix;

            // forces a recalculation of the relative rotation matrix to prevent
            // floating point problems
            RelativeRotationX = RelativeRotationX;
        }

        #region XML Docs
        /// <summary>
        /// Performs the every-frame position and rotation changing activity.
        /// </summary>
        /// <remarks>
        /// This method does not need to be explicitly called
        /// for managed objects such as Sprites and collision shapes.
        /// This method is exposed for custom PositionedObjects which
        /// are not added to a Manager.
        /// </remarks>
        /// <param name="secondDifference">The amount of time since last frame.</param>
        /// <param name="secondDifferenceSquaredDividedByTwo">Pre-calculated ((secondDifference*secondDifference) ^2) / 2.</param>
        /// <param name="secondsPassedLastFrame">The last frame secondDifference.</param>
        #endregion
        public virtual void TimedActivity(float secondDifference,
            double secondDifferenceSquaredDividedByTwo, float secondsPassedLastFrame)
        {
            #region real variables
            if (mKeepTrackOfReal)
            {

                LastVelocity = RealVelocity;

                if (secondsPassedLastFrame != 0)
                {
                    RealVelocity = (Position - LastPosition) / secondsPassedLastFrame;
                    RealAcceleration = (RealVelocity - LastVelocity) / secondsPassedLastFrame;
                }
                LastPosition = Position;
            }
            #endregion

            Position.X += (float)(Velocity.X * secondDifference + Acceleration.X * secondDifferenceSquaredDividedByTwo);
            Position.Y += (float)(Velocity.Y * secondDifference + Acceleration.Y * secondDifferenceSquaredDividedByTwo);
            Position.Z += (float)(Velocity.Z * secondDifference + Acceleration.Z * secondDifferenceSquaredDividedByTwo);

            Velocity.X += (float)(Acceleration.X * secondDifference);
            Velocity.Y += (float)(Acceleration.Y * secondDifference);
            Velocity.Z += (float)(Acceleration.Z * secondDifference);

            if (mRotationZVelocity != 0 || mRotationXVelocity != 0 || mRotationYVelocity != 0)
            {
                mRotationX += (float)(mRotationXVelocity * secondDifference);
                mRotationY += (float)(mRotationYVelocity * secondDifference);
                FlatRedBall.Math.MathFunctions.RegulateAngle(ref mRotationX);
                FlatRedBall.Math.MathFunctions.RegulateAngle(ref mRotationY);
                // set the rotation Z property - quick way to have the property called which updates the rotation matrix
                RotationZ += (float)(mRotationZVelocity * secondDifference);
            }

            Velocity.X -= Velocity.X * mDrag * secondDifference;
            Velocity.Y -= Velocity.Y * mDrag * secondDifference;
            Velocity.Z -= Velocity.Z * mDrag * secondDifference;

            TimedActivityRelative(secondDifference, secondDifferenceSquaredDividedByTwo);
        }
        #region XML Docs
        /// <summary>
        /// Performs the every-frame relative position and relative rotation changing activity.
        /// </summary>
        /// <remarks>
        /// This method does not need to be explicitly called
        /// for managed objects such as Sprites and collision shapes.
        /// This method is exposed for custom PositionedObjects which
        /// are not added to a Manager.
        /// </remarks>
        /// <param name="secondDifference">The amount of time since last frame.</param>
        /// <param name="secondDifferenceSquaredDividedByTwo">Pre-calculated ((secondDifference*secondDifference) ^2) / 2.</param>
        #endregion
        void TimedActivityRelative(float secondDifference,
            double secondDifferenceSquaredDividedByTwo)
        {

            RelativePosition.X += (float)(RelativeVelocity.X * secondDifference + RelativeAcceleration.X * secondDifferenceSquaredDividedByTwo);
            RelativePosition.Y += (float)(RelativeVelocity.Y * secondDifference + RelativeAcceleration.Y * secondDifferenceSquaredDividedByTwo);
            RelativePosition.Z += (float)(RelativeVelocity.Z * secondDifference + RelativeAcceleration.Z * secondDifferenceSquaredDividedByTwo);

            RelativeVelocity.X += (float)(RelativeAcceleration.X * secondDifference);
            RelativeVelocity.Y += (float)(RelativeAcceleration.Y * secondDifference);
            RelativeVelocity.Z += (float)(RelativeAcceleration.Z * secondDifference);

            if (mRelativeRotationZVelocity != 0 || mRelativeRotationXVelocity != 0 || mRelativeRotationYVelocity != 0)
            {
                mRelativeRotationX += (float)(mRelativeRotationXVelocity * secondDifference);
                mRelativeRotationY += (float)(mRelativeRotationYVelocity * secondDifference);
                // set the rotation Z property - quick way to have the property called which updates the rotation matrix
                RelativeRotationZ += (float)(mRelativeRotationZVelocity * secondDifference);
            }
        }
        #region XML Docs
        /// <summary>
        /// Returns a string containing common information about the PositionedObject.
        /// </summary>
        /// <returns>The string containing the information about this object.</returns>
        #endregion
        public override string ToString()
        {
            try
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                sb.Append("Name: ").Append(mName);
                sb.Append(" \nPosition: ").Append(Position);

                return sb.ToString();
            }
            catch
            {
                return "Error getting string";
            }
        }

        #region XML Docs
        /// <summary>
        /// Updates the absolute position and rotation using relative values and the Parent PositionedObject.
        /// </summary>
        /// <remarks>
        /// This method recurs up the hierarchical chain calling UpdateDependencies so that the entire family of 
        /// PositionedObjects is positioned appropriately.
        /// </remarks>
        #endregion
        public virtual void UpdateDependencies(double currentTime)
        {

            if (mLastDependencyUpdate == currentTime)
            {
                return;
            }
            else
            {
                mLastDependencyUpdate = currentTime;
            }

            if (mParent != null)
            {
                mParent.UpdateDependencies(currentTime);


                #region Apply dependency update

                if (mParentRotationChangesRotation)
                {
#if DEBUG
                    if(mParent == null)
                    {
                        string message = "This object's Parent property is null, but was not null before calling UpdateDependencies on " +
                            "its parent. Setting an object's Parent to null in UpdateDepdencies can cause unexpected behavior so it is not " +
                            "supported.";

                        throw new NullReferenceException(message);
                    }
#endif
                    // Set the property RotationMatrix rather than the field mRotationMatrix
                    // so the individual rotation values get updated.
                    RotationMatrix = mRelativeRotationMatrix * mParent.mRotationMatrix;
                }
                else
                {
                    RotationMatrix = mRelativeRotationMatrix;
                }

                if (!IgnoreParentPosition)
                {
                    if (mParentRotationChangesPosition)
                    {
                        Position.X = mParent.Position.X +
                            mParent.mRotationMatrix.M11 * RelativePosition.X +
                            mParent.mRotationMatrix.M21 * RelativePosition.Y +
                            mParent.mRotationMatrix.M31 * RelativePosition.Z;

                        Position.Y = mParent.Position.Y +
                            mParent.mRotationMatrix.M12 * RelativePosition.X +
                            mParent.mRotationMatrix.M22 * RelativePosition.Y +
                            mParent.mRotationMatrix.M32 * RelativePosition.Z;

                        Position.Z = mParent.Position.Z +
                            mParent.mRotationMatrix.M13 * RelativePosition.X +
                            mParent.mRotationMatrix.M23 * RelativePosition.Y +
                            mParent.mRotationMatrix.M33 * RelativePosition.Z;

                    }
                    else
                    {
                        Position = RelativePosition + mParent.Position;
                    }
                }
#if DEBUG
                if (float.IsNaN(Position.Z))
                {
                    string error = "The PositionedObject of type " + this.GetType() + " has a " +
                        "NaN on its Z property.  Its name is \"" + this.Name + "\".  ";

                    if (this.Parent != null)
                    {
                        error += "Its parent is of type " + this.Parent.GetType() + " and its name is \"" + this.Parent.Name + "\".";
                    }
                    else
                    {
                        error += "This object does not have a parent";
                    }
                    throw new Exception(error);
                }
#endif

                    #endregion

            }
        }


        #endregion

        #region Protected Methods

        #region XML Docs
        /// <summary>
        /// Sets the absolute rotation values according to the object's RotationMatrix.
        /// </summary>
        #endregion
        protected internal void UpdateRotationValuesAccordingToMatrix()
        {
            UpdateRotationValuesAccordingToMatrix(mRotationMatrix);
        }

        protected internal void UpdateRotationValuesAccordingToMatrix(Matrix rotationMatrix)
        {
            mRotationX = (float)System.Math.Atan2(rotationMatrix.M23, rotationMatrix.M33);
            mRotationZ = (float)System.Math.Atan2(rotationMatrix.M12, rotationMatrix.M11);
            mRotationY = -(float)System.Math.Asin(rotationMatrix.M13);
            if (mRotationX < 0)
                mRotationX += (float)System.Math.PI * 2;
            if (mRotationY < 0)
                mRotationY += (float)System.Math.PI * 2;
            if (mRotationZ < 0)
                mRotationZ += (float)System.Math.PI * 2;

            mRotationMatrix = rotationMatrix;
        }

        #endregion

        #region Internal Methods

        internal void InvertHandedness()
        {
            Z = -Z;
            RelativeZ = -RelativeZ;

            RotationX = -RotationX;
            RotationY = -RotationY;

            RelativeRotationY = -RotationY;
            RelativeRotationX = -RelativeRotationX;
        }

        #endregion

        #region Private Methods


        public static Matrix GetRotationOnly(ref Matrix jointMatrix)
        {
            Matrix rotationMatrix = jointMatrix;
            rotationMatrix.M14 = 0;
            rotationMatrix.M24 = 0;
            rotationMatrix.M34 = 0;
            rotationMatrix.M41 = 0;
            rotationMatrix.M42 = 0;
            rotationMatrix.M43 = 0;
            return rotationMatrix;
        }

        #endregion

        #endregion

        #region IEquatable<PositionedObject> Members

        bool IEquatable<PositionedObject>.Equals(PositionedObject other)
        {
            return this == other;
        }

        #endregion
    }
}
