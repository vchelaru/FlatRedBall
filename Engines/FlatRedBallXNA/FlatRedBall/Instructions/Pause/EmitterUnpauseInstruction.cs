using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Instructions.Pause
{
    [Obsolete]
    class EmitterUnpauseInstruction : PositionedObjectUnpauseInstruction<FlatRedBall.Graphics.Particle.Emitter>
    {
        bool mTimedEmission;

        public EmitterUnpauseInstruction(FlatRedBall.Graphics.Particle.Emitter emitter)
            :
            base(emitter)
        {
            mTimedEmission = emitter.TimedEmission;
        }

        public override void Stop(FlatRedBall.Graphics.Particle.Emitter positionedObject)
        {
            base.Stop(positionedObject);

            mTarget.TimedEmission = false;
        }

        public override void Execute()
        {
            mTarget.TimedEmission = mTimedEmission;

            base.Execute();
        }

    }
}
