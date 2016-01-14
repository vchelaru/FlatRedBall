using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Utilities;

namespace FlatRedBall.Instructions
{
    #region XML Docs
    /// <summary>
    /// A list of timed keyframes which can be used to play an animation.
    /// </summary>
    #endregion
    public class TimedKeyframeList : INameable
    {
        #region Fields

        double mTimeToExecute;

        KeyframeList mKeyframeList;

        string mName;
        string mTargetName;

        #endregion

        #region Properties

        public KeyframeList KeyframeList
        {
            get { return mKeyframeList; }
        }

        public double Length
        {
            get
            {
                if (mKeyframeList == null || mKeyframeList.Count == 0)
                {
                    return 0;
                }
                else
                {
                    return mKeyframeList[mKeyframeList.Count - 1][0].TimeToExecute;
                }
            }
        }

        public string Name
        {
            get { return mName; }
            set { mName = value; }
        }

        public string NameOfReferencedKeyframeList
        {
            get { return mKeyframeList.Name; }
        }

        public string TargetName
        {
            get { return mTargetName; }
        }

        public double TimeToExecute
        {
            get { return mTimeToExecute; }
            set { mTimeToExecute = value; }
        }

        #endregion

        #region Methods

        #region Static Methods

        public static void SortTimeToExecuteAscending(List<TimedKeyframeList> keyframeList)
        {
            keyframeList.Sort(CompareStartingTime);
        }

        public static void SortEndTimeAscending(List<TimedKeyframeList> keyframeList)
        {
            keyframeList.Sort(CompareEndTime);
        }

        private static int CompareStartingTime(TimedKeyframeList first, TimedKeyframeList second)
        {
            return System.Math.Sign(first.TimeToExecute - second.TimeToExecute);
        }

        private static int CompareEndTime(TimedKeyframeList first, TimedKeyframeList second)
        {
            return System.Math.Sign((first.TimeToExecute + first.Length) - 
                                    (second.TimeToExecute + second.Length));
        }

        #endregion

        #region Public Methods

        public TimedKeyframeList(KeyframeList keyframeList, string targetName)
        {
            mKeyframeList = keyframeList;
            mTargetName = targetName;
            mName = keyframeList.Name;
        }

        #endregion

        #endregion
    }
}
