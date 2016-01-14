using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Graphics;

namespace FlatRedBall.Instructions.Pause
{
    class TextUnpauseInstruction : PositionedObjectUnpauseInstruction<Text>
    {
        #region Fields

        float mAlphaRate;
        float mRedRate;
        float mGreenRate;
        float mBlueRate;

        float mSpacingVelocity;
        float mScaleVelocity;

        #endregion

        #region Methods

        public TextUnpauseInstruction(Text text)
            : base(text)
        {
            mAlphaRate = text.AlphaRate;
            mRedRate = text.RedRate;
            mGreenRate = text.GreenRate;
            mBlueRate = text.BlueRate;

            mSpacingVelocity = text.SpacingVelocity;
            mScaleVelocity = text.ScaleVelocity;
        }

        public override void Stop(Text text)
        {
            text.AlphaRate = 0;
            text.RedRate = 0;
            text.GreenRate = 0;
            text.BlueRate = 0;

            text.SpacingVelocity = 0;
            text.ScaleVelocity = 0;

            base.Stop(text);
        }

        public override void Execute()
        {
            mTarget.AlphaRate = mAlphaRate;
            mTarget.RedRate = mRedRate;
            mTarget.GreenRate = mGreenRate;
            mTarget.BlueRate = mBlueRate;

            mTarget.SpacingVelocity = mSpacingVelocity;
            mTarget.ScaleVelocity = mScaleVelocity;

            base.Execute();
        }

        #endregion

    }
}
