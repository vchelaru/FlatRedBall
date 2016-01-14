using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Network
{
    internal class DelayedByteArray
    {
        private byte[] mByteArray;
        private double mTimeToSend;

        public byte[] ByteArray
        {
            get { return mByteArray; }
            set { mByteArray = value; }
        }

        public double TimeToSend
        {
            get { return mTimeToSend; }
            set { mTimeToSend = value; }
        }

        public DelayedByteArray(byte[] byteArray, double timeToSend)
        {
            this.ByteArray = byteArray;
            this.TimeToSend = timeToSend;
        }

    }
}
