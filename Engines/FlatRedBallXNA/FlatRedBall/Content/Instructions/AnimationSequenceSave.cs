using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Instructions.ScriptedAnimations;
using FlatRedBall.Instructions;

namespace FlatRedBall.Content.Instructions
{
    #region XML Docs
    /// <summary>
    /// Save class which stores AnimationSequence information.
    /// </summary>
    #endregion
    public class AnimationSequenceSave
    {
        #region Fields

        public List<TimedKeyframeListSave> TimedKeyframeListSaves = new List<TimedKeyframeListSave>();

        public string Name;

        #endregion

        #region Methods

        public static AnimationSequenceSave FromAnimationSequence(AnimationSequence animationSequence)
        {
            AnimationSequenceSave sequenceSave = new AnimationSequenceSave();
            sequenceSave.Name = animationSequence.Name;

            foreach (TimedKeyframeList tkl in animationSequence)
            {
                sequenceSave.TimedKeyframeListSaves.Add(TimedKeyframeListSave.FromTimedKeyframeList(tkl));
            }
            return sequenceSave;
        }

        public AnimationSequence ToAnimationSequence(List<InstructionSet> instructionSetList)
        {
            AnimationSequence animationSequence = new AnimationSequence();
            animationSequence.Name = this.Name;
            foreach (TimedKeyframeListSave tkls in TimedKeyframeListSaves)
            {
                animationSequence.Add(tkls.ToTimedKeyframeList(instructionSetList));
            }

            return animationSequence;
        }

        #endregion
    }
}
