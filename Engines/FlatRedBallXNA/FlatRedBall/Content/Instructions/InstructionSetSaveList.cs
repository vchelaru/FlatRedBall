using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.IO;
using FlatRedBall.Instructions;
using FlatRedBall.Instructions.ScriptedAnimations;

namespace FlatRedBall.Content.Instructions
{
    #region XML Docs
    /// <summary>
    /// An XML serializable list of InstructionSetSaves.
    /// </summary>
    #endregion
    public class InstructionSetSaveList
    {
        #region Fields

        public List<InstructionSetSave> InstructionSetSaves = new List<InstructionSetSave>();

        public List<AnimationSequenceSave> AnimationSequenceSaves = new List<AnimationSequenceSave>();

        public string SceneFileName;

        #endregion

        #region Methods

        public static InstructionSetSaveList FromFile(string fileName)
        {
            return FileManager.XmlDeserialize<InstructionSetSaveList>(fileName);
        }

        public void Save(string fileName)
        {
            FileManager.XmlSerialize(this, fileName);
        }

        public List<InstructionSet> ToInstructionSetList(FlatRedBall.Scene scene)
        {
            List<InstructionSet> instructionSetList = new List<InstructionSet>(InstructionSetSaves.Count);

            foreach (InstructionSetSave instructionSetSave in InstructionSetSaves)
            {
                instructionSetList.Add(instructionSetSave.ToInstructionSet(scene));
            }

            return instructionSetList;
        }

        public List<InstructionSet> ToInstructionSetList(IInstructable iinstructable)
        {
            List<InstructionSet> instructionSetList = new List<InstructionSet>(InstructionSetSaves.Count);

            foreach (InstructionSetSave instructionSetSave in InstructionSetSaves)
            {
                instructionSetList.Add(instructionSetSave.ToInstructionSet(iinstructable));
            }

            return instructionSetList;
        }

        public List<AnimationSequence> ToAnimationSequenceList(List<InstructionSet> instructionSets)
        {
            List<AnimationSequence> animationSequenceList = new List<AnimationSequence>();

            foreach (AnimationSequenceSave sequenceSave in AnimationSequenceSaves)
            {
                AnimationSequence animationSequence = sequenceSave.ToAnimationSequence(instructionSets);

                animationSequenceList.Add(animationSequence);

            }

            return animationSequenceList;
        }

        #endregion


    }
}
