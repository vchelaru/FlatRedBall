using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Utilities;

namespace FlatRedBall.Instructions.ScriptedAnimations
{
    #region XML Docs
    /// <summary>
    /// A list of TimedKeyframeLists which represents an Animation which can be
    /// played.
    /// </summary>
    /// <remarks>
    /// This class interfaces with the InstructionSetSaveList class when saving/loading.
    /// </remarks>
    #endregion
    public class AnimationSequence : List<TimedKeyframeList>, INameable
    {
        #region Fields

        string mName;

        #endregion

        #region Properties

        public string Name
        {
            get { return mName; }
            set { mName = value; }
        }

        public double Length
        {
            get
            {
                double returnValue = 0;

                foreach(TimedKeyframeList timedKeyframeList in this)
                {
                    returnValue = System.Math.Max(
                        returnValue, 
                        timedKeyframeList.TimeToExecute + timedKeyframeList.Length);
                }

                return returnValue;

            }

        }

        #endregion

        #region Methods

        public void Play()
        {
            Play(0);
        }

        public void Play(double timeToStartAt)
        {
            Play(timeToStartAt, false);
        }

        public void Play(double timeToStartAt, bool cycle)
        {
            // There are 3 categories of TimedKeyframeLists:
            // 1:  Ones that begin and end before timeToStartAt
            // 2:  Ones that begin before timeToStartAt but end after - in other words
            //     timeToStartAt is in the middle of these TimedKeyframeLists
            // 3:  Ones that have not started yet.

            // Category 1:
            List<TimedKeyframeList> keyframesBefore = new List<TimedKeyframeList>();

            // Category 2:
            List<TimedKeyframeList> keyframesDuring = new List<TimedKeyframeList>();

            // Category 3:
            List<TimedKeyframeList> keyframesAfter = new List<TimedKeyframeList>();

            #region Sort all timed keyframes in the lists
            foreach (TimedKeyframeList timedKeyframeList in this)
            {
                if (timedKeyframeList.TimeToExecute < timeToStartAt)
                {
                    if (timedKeyframeList.TimeToExecute + timedKeyframeList.Length < timeToStartAt)
                    {
                        keyframesBefore.Add(timedKeyframeList);
                    }
                    else
                    {
                        keyframesDuring.Add(timedKeyframeList);
                    }
                }
                else
                {
                    keyframesAfter.Add(timedKeyframeList);
                }
            }
            #endregion


            InstructionList instructionsToExecute = new InstructionList();

            #region Add the keyframes before

            TimedKeyframeList.SortEndTimeAscending(keyframesBefore);

            foreach (TimedKeyframeList keyframeList in keyframesBefore)
            {
                // TODO
            }


            #endregion

            foreach (TimedKeyframeList keyframeList in keyframesAfter)
            {
                InstructionList instructionsToAdd = 
                    keyframeList.KeyframeList.CreateVelocityListAtTime(timeToStartAt);

                instructionsToAdd.ShiftTime(keyframeList.TimeToExecute);

                instructionsToExecute.AddRange(instructionsToAdd );
            }

            #region If cycle == true

            if (cycle == true)
            {

                foreach (Instruction instruction in instructionsToExecute)
                {
                    instruction.CycleTime = this.Length;
                }

            }
            #endregion
            instructionsToExecute.ShiftTime(TimeManager.CurrentTime);

            instructionsToExecute.ExecuteAndRemoveOrCyclePassedInstructions(
                timeToStartAt + TimeManager.CurrentTime);


            InstructionManager.Instructions.AddRange(instructionsToExecute);
        }

        #endregion
    }
}
