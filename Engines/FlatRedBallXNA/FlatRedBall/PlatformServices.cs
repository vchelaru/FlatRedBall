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

        public static StateManager State
        {
            get
            {
                return StateManager.Current;
            }
        }

        public static bool BackStackEnabled { get; set; }

        public static void Initialize()
        {
            // default value is back stack enabled false
            // Update Oct 15, 2012
            // This has caused some 
            // confusing behavior and
            // its default behavior is
            // not what I want in Baron
            // so I'm going to turn it off
            // by default.  This is better than
            // having another thing to maintain which
            // I didn't write.
            //BackStackEnabled = true;
            BackStackEnabled = false;

#if !SILVERLIGHT
            #region Windows Phone Vibration System
            mVibrateMotor = new VibrateMotor();
            mVibrateMotor.Initialize();
            #endregion
#endif
        }

    }
}
