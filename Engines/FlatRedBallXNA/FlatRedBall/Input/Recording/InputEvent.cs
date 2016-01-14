using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall.Utilities;

namespace FlatRedBall.Input.Recording
{
    #region XML Docs
    /// <summary>
    /// Information about an input event that has occurred.  This class can be used for playbacks
    /// or for saving input information.
    /// </summary>
    /// <typeparam name="EventType">The event type.  This can be enum values defined by the end user or enums
    /// provided by other libraries, such as a Keys value representing which key experienced the event.</typeparam>
    /// <typeparam name="EventValue">The type of actions that can be performed on the given EventType.  For example
    /// this could be an enum defining whether a key was pressed or released.</typeparam>
    #endregion
    public class InputEvent<EventType, EventValue> : IComparable< InputEvent<EventType, EventValue> >, ITimed
    {
        #region Fields

        double mTime;
        EventType mType;
        EventValue mValue;

        #endregion

        #region Properties

        public double Time
        {
            get { return mTime; }
            set { mTime = value; }
        }

        public EventType Type
        {
            get { return mType; }
            set { mType = value; }
        }

        public EventValue Value
        {
            get { return mValue; }
            set { mValue = value; }
        }

        #endregion

        #region Methods

        public InputEvent()
        {
            mTime = 0;
            mType = default(EventType);
            mValue = default(EventValue);
        }

        public InputEvent(double time, EventType eventType, EventValue eventValue)
        {
            mTime = time;
            mType = eventType;
            mValue = eventValue;
        }


        public int CompareTo(InputEvent<EventType, EventValue> other)
        {
            return mTime.CompareTo(other.mTime);
        }


        public override string ToString()
        {
            return Time.ToString() + " " + mType.ToString() + " " + mValue.ToString();
        }

        #endregion

    }
}
