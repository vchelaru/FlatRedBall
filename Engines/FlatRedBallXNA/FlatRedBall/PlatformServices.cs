using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !SILVERLIGHT
using FlatRedBall.Feedback;
#endif
using FlatRedBall.IO;

namespace FlatRedBall
{
    public static class PlatformServices
    {
#if !SILVERLIGHT

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

#endif

        public static void Initialize()
        {


#if !SILVERLIGHT
            #region Windows Phone Vibration System
            mVibrateMotor = new VibrateMotor();
            mVibrateMotor.Initialize();
            #endregion
#endif
        }

    }
}
