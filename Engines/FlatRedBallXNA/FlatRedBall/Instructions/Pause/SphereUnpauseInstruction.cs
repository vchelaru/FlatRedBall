using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Math.Geometry;

namespace FlatRedBall.Instructions.Pause
{
    class SphereUnpauseInstruction : PositionedObjectUnpauseInstruction<Sphere>
    {
        float mRadiusVelocity;

        public SphereUnpauseInstruction(Sphere sphere)
            : base(sphere)
        {
            mRadiusVelocity = sphere.RadiusVelocity;
        }

        public override void Stop(Sphere sphere)
        {
            sphere.RadiusVelocity = 0;
            base.Stop(sphere);
        }

        public override void Execute()
        {
            mTarget.RadiusVelocity = mRadiusVelocity;
            base.Execute();
        }
    }
}
