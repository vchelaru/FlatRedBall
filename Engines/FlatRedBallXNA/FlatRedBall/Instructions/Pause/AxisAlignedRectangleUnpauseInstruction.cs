using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Math.Geometry;

namespace FlatRedBall.Instructions.Pause
{
    class AxisAlignedRectangleUnpauseInstruction : PositionedObjectUnpauseInstruction<AxisAlignedRectangle>
    {
        float mScaleXVelocity;
        float mScaleYVelocity;

        public AxisAlignedRectangleUnpauseInstruction(AxisAlignedRectangle rectangle)
            :
            base(rectangle)
        {
            mScaleXVelocity = rectangle.ScaleXVelocity ;
            mScaleYVelocity = rectangle.ScaleYVelocity;
        }

        public override void Stop(AxisAlignedRectangle rectangle)
        {
            rectangle.ScaleXVelocity = 0;
            rectangle.ScaleYVelocity = 0;
            base.Stop(rectangle);
        }

        public override void Execute()
        {
            mTarget.ScaleXVelocity = mScaleXVelocity;
            mTarget.ScaleYVelocity = mScaleYVelocity;
            base.Execute();
        }
    }
}
