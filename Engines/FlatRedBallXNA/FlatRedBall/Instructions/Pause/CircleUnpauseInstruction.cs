using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Math.Geometry;

namespace FlatRedBall.Instructions.Pause
{
    class CircleUnpauseInstruction : PositionedObjectUnpauseInstruction<Circle>
    {
        float mRadiusVelocity;

        public CircleUnpauseInstruction(Circle circle)
            : base(circle)
        {
            mRadiusVelocity = circle.RadiusVelocity;
        }

        public override void Stop(Circle circle)
        {
            circle.RadiusVelocity = 0;
            base.Stop(circle);
        }

        public override void Execute()
        {
            mTarget.RadiusVelocity = mRadiusVelocity;
            base.Execute();
        }
    }
}
