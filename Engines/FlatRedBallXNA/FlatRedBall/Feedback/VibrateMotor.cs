using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        }

        public void Stop()
        {

        }
    }
}
