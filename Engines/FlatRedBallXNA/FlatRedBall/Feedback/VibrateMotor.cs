using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if WINDOWS_PHONE
using Microsoft.Devices;
#endif

namespace FlatRedBall.Feedback
{
    public class VibrateMotor
    {
        private bool mEnabled = true;

        [Obsolete("Use IsEnabled")]
        public bool CanVibrate
        {
            get { return IsEnabled; }
            set 
            {
                IsEnabled = value;
            }
        }

        public bool IsEnabled
        {
            get { return mEnabled; }
            set
            {
                mEnabled = value;
                if (mEnabled == false)
                    Stop();
            }
        }

        public void Initialize()
        {
        }

        public void Vibrate(float duration)
        {
#if WINDOWS_PHONE
            if (mEnabled)
                VibrateController.Default.Start(TimeSpan.FromSeconds(duration));
#endif
        }

        public void Stop()
        {
#if WINDOWS_PHONE
            VibrateController.Default.Stop();
#endif
        }
    }
}
