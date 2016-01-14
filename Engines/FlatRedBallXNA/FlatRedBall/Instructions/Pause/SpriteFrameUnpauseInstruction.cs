using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Instructions.Pause
{
    class SpriteFrameUnpauseInstruction : PositionedObjectUnpauseInstruction<FlatRedBall.ManagedSpriteGroups.SpriteFrame>
    {
        #region Fields

        float mScaleXVelocity;
        float mScaleYVelocity;
        #endregion

        #region Properties

        public SpriteFrameUnpauseInstruction(FlatRedBall.ManagedSpriteGroups.SpriteFrame spriteFrame)
            :
            base(spriteFrame)
        {
            mScaleXVelocity = spriteFrame.ScaleXVelocity;
            mScaleYVelocity = spriteFrame.ScaleYVelocity;
        }

        public override void Stop(FlatRedBall.ManagedSpriteGroups.SpriteFrame positionedObject)
        {
            mTarget.ScaleXVelocity = 0;
            mTarget.ScaleYVelocity = 0;

            base.Stop(positionedObject);
        }

        public override void Execute()
        {
            mTarget.ScaleXVelocity = mScaleXVelocity;
            mTarget.ScaleYVelocity = mScaleYVelocity;

            base.Execute();
        }
        #endregion
    }
}
