using System;
using System.Collections.Generic;
using System.Text;

#if FRB_MDX
using Microsoft.DirectX;
#else // FRB_XNA || ZUNE || SILVERLIGHT || WINDOWS_PHONE
using Microsoft.Xna.Framework;
#endif

namespace FlatRedBall.Instructions.Pause
{
    class PositionedObjectUnpauseInstruction<T> : UnpauseInstruction<T> where T : FlatRedBall.PositionedObject
    {
        #region Fields
        // All values for the PositionedObject before the pause
        Vector3 mVelocity;
        Vector3 mRelativeVelocity;
        Vector3 mAcceleration;
        Vector3 mRelativeAcceleration;
        Vector3 mRealVelocity;
        Vector3 mRealAcceleration;

        float mDrag;

        float mRotationXVelocity;
        float mRotationYVelocity;
        float mRotationZVelocity;

        float mRelativeRotationXVelocity;
        float mRelativeRotationYVelocity;
        float mRelativeRotationZVelocity;
        
        protected InstructionList mInstructions = new InstructionList();

        #endregion

        #region Methods
        public PositionedObjectUnpauseInstruction(T positionedObject)
            : base(positionedObject)
        {

            mVelocity = positionedObject.Velocity;
            mRelativeVelocity = positionedObject.RelativeVelocity;
            mAcceleration = positionedObject.Acceleration;
            mRelativeAcceleration = positionedObject.RelativeAcceleration;

            mRealVelocity = positionedObject.RealVelocity;
            mRealAcceleration = positionedObject.RealAcceleration;

            mDrag = positionedObject.Drag;

            mRotationXVelocity = positionedObject.RotationXVelocity;
            mRotationYVelocity = positionedObject.RotationYVelocity;
            mRotationZVelocity = positionedObject.RotationZVelocity;

            mRelativeRotationXVelocity = positionedObject.RelativeRotationXVelocity;
            mRelativeRotationYVelocity = positionedObject.RelativeRotationYVelocity;
            mRelativeRotationZVelocity = positionedObject.RelativeRotationZVelocity;

            foreach (Instruction instruction in positionedObject.Instructions)
            {
                mInstructions.Add(instruction);
            }
        }

        public virtual void Stop(T positionedObject)
        {
            positionedObject.Velocity = new Vector3();
            positionedObject.RelativeVelocity = new Vector3();
            positionedObject.Acceleration = new Vector3();
            positionedObject.RelativeAcceleration = new Vector3();

            positionedObject.RealVelocity = new Vector3();
            positionedObject.RealAcceleration = new Vector3();

            positionedObject.Drag = 0;

            positionedObject.RotationXVelocity = 0;
            positionedObject.RotationYVelocity = 0;
            positionedObject.RotationZVelocity = 0;

            positionedObject.RelativeRotationXVelocity = 0;
            positionedObject.RelativeRotationYVelocity = 0;
            positionedObject.RelativeRotationZVelocity = 0;


            positionedObject.Instructions.Clear();

        }

        public override void Execute()
        {
            mTarget.Velocity = mVelocity;
            mTarget.RelativeVelocity = mRelativeVelocity;
            mTarget.Acceleration = mAcceleration;
            mTarget.RelativeAcceleration = mRelativeAcceleration;

            mTarget.RealVelocity = mRealVelocity;
            mTarget.RealAcceleration = mRealAcceleration;

            mTarget.Drag = mDrag;

            mTarget.RotationXVelocity = mRotationXVelocity;
            mTarget.RotationYVelocity = mRotationYVelocity;
            mTarget.RotationZVelocity = mRotationZVelocity;

            mTarget.RelativeRotationXVelocity = mRelativeRotationXVelocity;
            mTarget.RelativeRotationYVelocity = mRelativeRotationYVelocity;
            mTarget.RelativeRotationZVelocity = mRelativeRotationZVelocity;


            double timeToAdd = TimeManager.CurrentTime - mCreationTime;

            foreach (Instruction instruction in mInstructions)
            {
                instruction.TimeToExecute += timeToAdd;
                mTarget.Instructions.Add(instruction);
            }
        }

        #endregion
    }
}
