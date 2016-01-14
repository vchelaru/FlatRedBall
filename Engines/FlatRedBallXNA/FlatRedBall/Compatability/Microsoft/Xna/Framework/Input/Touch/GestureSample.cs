using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Xna.Framework.Input.Touch
{
    // Summary:
    //     A representation of data from a multitouch gesture over a span of time.
    public struct GestureSample
    {
        //public GestureSample(GestureType gestureType, TimeSpan timestamp, Vector2 position, Vector2 position2, Vector2 delta, Vector2 delta2)
        //{

        //}

        // Summary:
        //     Holds delta information about the first touchpoint in a multitouch gesture.
        public Vector2 Delta { get; set; }
        //
        // Summary:
        //     Holds delta information about the second touchpoint in a multitouch gesture.
        public Vector2 Delta2 { get; set;  }
        //
        // Summary:
        //     The type of gesture in a multitouch gesture sample.
        //public GestureType GestureType { get; }
        //
        // Summary:
        //     Holds the current position of the first touchpoint in this gesture sample.
        public Vector2 Position { get; set; }
        //
        // Summary:
        //     Holds the current position of the the second touchpoint in this gesture sample.
        public Vector2 Position2 { get; set; }
        //
        // Summary:
        //     Holds the starting time for this touch gesture sample.
        public TimeSpan Timestamp { get; set; }
    }
}