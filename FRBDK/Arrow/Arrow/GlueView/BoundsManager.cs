using EditorObjects;
using FlatRedBall.Arrow.Managers;
using FlatRedBall.Content.Scene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Arrow.GlueView
{
    public class BoundsManager : Singleton<BoundsManager>
    {
        CameraBounds mCameraBounds;

        public void UpdateTo(CameraSave cameraSave)
        {
            if (mCameraBounds == null)
            {
                mCameraBounds = new CameraBounds(cameraSave);
            }

            mCameraBounds.CameraSave = cameraSave;
            mCameraBounds.UpdateBounds(0);
        }
    }
}
