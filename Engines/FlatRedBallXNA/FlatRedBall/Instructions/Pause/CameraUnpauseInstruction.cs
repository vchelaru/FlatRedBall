using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Instructions.Pause
{
    class CameraUnpauseInstruction : PositionedObjectUnpauseInstruction<Camera>
    {
        #region Fields

        float mTopDestinationVelocity;
        float mBottomDestinationVelocity;
        float mLeftDestinationVelocity;
        float mRightDestinationVelocity;

        #endregion

        public CameraUnpauseInstruction(Camera camera) 
            : base(camera)
        {

            mTopDestinationVelocity = camera.TopDestinationVelocity;
            mBottomDestinationVelocity = camera.BottomDestinationVelocity;
            mLeftDestinationVelocity = camera.LeftDestinationVelocity;
            mRightDestinationVelocity = camera.RightDestinationVelocity;


        }

        public override void Stop(Camera camera)
        {
            base.Stop(camera);


            camera.TopDestinationVelocity = 0;
            camera.BottomDestinationVelocity = 0;
            camera.LeftDestinationVelocity = 0;
            camera.RightDestinationVelocity = 0;
        }

        public override void Execute()
        {
            mTarget.TopDestinationVelocity = mTopDestinationVelocity;
            mTarget.BottomDestinationVelocity = mBottomDestinationVelocity;
            mTarget.LeftDestinationVelocity = mLeftDestinationVelocity;
            mTarget.RightDestinationVelocity = mRightDestinationVelocity;

            base.Execute();
        }
    }
}
