using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Feedback;
using FlatRedBall.IO;

namespace FlatRedBall
{
    public static class PlatformServices
    {
        private static VibrateMotor mVibrateMotor;
        public static VibrateMotor VibrateMotor
        {
            get 
            {
                if (mVibrateMotor == null)
                    throw new InvalidOperationException("Trying to get VibrateMotor, but the system has not been initialized.");
                return mVibrateMotor; 
            }
        }

        public static void Initialize()
        {
            #region Windows Phone Vibration System
            mVibrateMotor = new VibrateMotor();
            mVibrateMotor.Initialize();
            #endregion
        }

    }
}
