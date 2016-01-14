using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Input.Recording
{
    public abstract class InputRecordBase<EventType, EventValue> : IInputRecord
    {
        List<InputEvent<EventType, EventValue>> mInputEvents = new List<InputEvent<EventType, EventValue>>();
        List<InputEvent<EventType, EventValue>> mEventsRecordedThisFrame =
            new List<InputEvent<EventType, EventValue>>();

        bool mIsRecording;
        bool mPlayingBack;

        double mTimeRecordingStarted;
        double mTimePlaybackStarted;

        public bool IsPlayingBack
        {
            get { return mPlayingBack; }
            set
            {
                mPlayingBack = value;
                if (mPlayingBack)
                {
                    mTimePlaybackStarted = TimeManager.CurrentTime;
                }
            }
        }

        public bool IsRecording
        {
            get { return mIsRecording; }
            set
            {
                mIsRecording = value;
                if (mIsRecording)
                {
                    mTimeRecordingStarted = TimeManager.CurrentTime;
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// Gets and sets the time that playback started.  Setting this value simulates changing
        /// when the playback started.
        /// </summary>
        #endregion
        public double TimePlaybackStarted
        {
            get { return mTimePlaybackStarted; }
            set { mTimePlaybackStarted = value; }
        }

        public double TimeRecordingStarted
        {
            get { return mTimeRecordingStarted; }
            set { mTimeRecordingStarted = value; }
        }

        public List<InputEvent<EventType, EventValue>> InputEvents
        {
            get { return mInputEvents; }
        }

        public List<InputEvent<EventType, EventValue>> EventsRecordedThisFrame
        {
            get { return mEventsRecordedThisFrame; }
        }

        public void ClearAfter(double time)
        {
            int firstIndexAfter = GetIndexAfter(time);

            while (mInputEvents.Count > firstIndexAfter)
            {
                mInputEvents.RemoveAt(firstIndexAfter);
            }

        }

        public void CopyTo(InputRecordBase<EventType, EventValue> otherRecord, double startTime, double duration)
        {
            int thisStartIndex = this.GetIndexAfter(startTime);
            int thisEndIndex = this.GetIndexAfter(startTime + duration);

            for (int thisIndex = thisStartIndex; thisIndex <= thisEndIndex; thisIndex++)
            {
                otherRecord.mInputEvents.Add(mInputEvents[thisIndex]);
            }
            otherRecord.mInputEvents.Sort();
        }


        public void ClearRange(double startTime, double duration)
        {
            int startIndexAfter = GetIndexAfter(startTime);
            int endIndexAfter = GetIndexAfter(startTime + duration);

            for (int i = 0; i <= endIndexAfter - startIndexAfter; i++)
            {
                mInputEvents.RemoveAt(startIndexAfter);
            }

        }


        public int GetIndexAfter(double time)
        {
            // If there are no events after the given time,
            // return the last event
            int firstIndexAfter = mInputEvents.Count - 1;

            for (int i = 0; i < mInputEvents.Count; i++)
            {
                if (mInputEvents[i].Time > time)
                {
                    firstIndexAfter = i;
                    break;
                }
            }

            return firstIndexAfter;
        }


        public double Length
        {
            get
            {
                if (InputEvents.Count == 0)
                {
                    return 0;
                }

                else
                {
                    return InputEvents[InputEvents.Count - 1].Time;
                }
            }
        }

        public abstract void Update();
    }
}
