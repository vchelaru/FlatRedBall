using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Instructions.Pause
{
    class SpriteUnpauseInstruction : PositionedObjectUnpauseInstruction<Sprite>
    {

        float mScaleXVelocity;
        float mScaleYVelocity;

        bool mAnimate;

        // October 17, 2011
        // Not sure why we need
        // to set mTimeIntoAnimation
        // when we unpause.  This isn't
        // a rate value, it responds to animation
        // values.
        //double mTimeIntoAnimation;

        float mAlphaRate;
        float mRedRate;
        float mGreenRate;
        float mBlueRate;

        public SpriteUnpauseInstruction(Sprite sprite)
            : base(sprite)
        {
            mScaleXVelocity = sprite.ScaleXVelocity;
            mScaleYVelocity = sprite.ScaleYVelocity;

            mAnimate = sprite.Animate;

            // See comment in Fields section
            //mTimeIntoAnimation = sprite.TimeIntoAnimation;

            mAlphaRate = sprite.AlphaRate;
            mRedRate = sprite.RedRate;
            mGreenRate = sprite.GreenRate;
            mBlueRate = sprite.BlueRate;
        }

        public override void Stop(Sprite positionedObject)
        {
            base.Stop(positionedObject);
            mTarget.ScaleXVelocity = 0;
            mTarget.ScaleYVelocity = 0;

            mTarget.Animate = false;

            // no need to set the mTimeUntilNextFrame

            mTarget.AlphaRate = 0;
            mTarget.RedRate = 0;
            mTarget.GreenRate = 0;
            mTarget.BlueRate = 0;

            // I found that we are calling base.Stop twice - why?
            // That doesn't seem right, so I commented this out
            //base.Stop(positionedObject);
        }

        public override void Execute()
        {
            mTarget.ScaleXVelocity = mScaleXVelocity;
            mTarget.ScaleYVelocity = mScaleYVelocity;

            mTarget.Animate = mAnimate;
            // See comments in Fields section on why this was removed
            //mTarget.TimeIntoAnimation = mTimeIntoAnimation;

            mTarget.AlphaRate = mAlphaRate;
            mTarget.RedRate = mRedRate;
            mTarget.GreenRate = mGreenRate;
            mTarget.BlueRate = mBlueRate;

            base.Execute();
        }
    }
}
