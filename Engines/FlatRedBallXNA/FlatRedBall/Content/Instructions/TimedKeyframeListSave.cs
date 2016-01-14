using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Instructions;

namespace FlatRedBall.Content.Instructions
{
    #region XML Docs
    /// <summary>
    /// A savable reference to a TimedKeyframe.  This does not actually store the contents
    /// of the TimedKeyframe; instead it stores a string reference.
    /// </summary>
    #endregion
    public class TimedKeyframeListSave
    {
        #region Fields

        public string Name;

        public string NameOfReferencedKeyframeList;
        public string Target;

        public double Time;

        #endregion

        #region Methods

        public static TimedKeyframeListSave FromTimedKeyframeList(TimedKeyframeList timedKeyframeList)
        {
            TimedKeyframeListSave tkls = new TimedKeyframeListSave();
            tkls.Name = timedKeyframeList.Name;
            tkls.Target = timedKeyframeList.TargetName;
            tkls.NameOfReferencedKeyframeList = timedKeyframeList.NameOfReferencedKeyframeList;
            tkls.Time = timedKeyframeList.TimeToExecute;

            return tkls;
        }

        public TimedKeyframeList ToTimedKeyframeList(List<InstructionSet> instructionSetList)
        {
            InstructionSet targetInstructionSet = null;

            foreach (InstructionSet instructionSet in instructionSetList)
            {
                if (instructionSet.Name == this.Target)
                {
                    targetInstructionSet = instructionSet;
                    break;
                }
            }

            // Now that we know which InstructionSet we're working with, find the keyframe with the
            // matching name
            KeyframeList targetKeyframeList = null;
            foreach (KeyframeList keyframeList in targetInstructionSet)
            {
                if (keyframeList.Name == this.NameOfReferencedKeyframeList)
                {
                    targetKeyframeList = keyframeList;
                    break;
                }
            }

            TimedKeyframeList timedKeyframeList = new TimedKeyframeList(targetKeyframeList, Target);

            timedKeyframeList.TimeToExecute = Time;

            return timedKeyframeList;
        }

        #endregion

    }
}
