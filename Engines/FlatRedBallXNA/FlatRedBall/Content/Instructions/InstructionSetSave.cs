using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

using System.Xml;
using System.Xml.Serialization;

using FlatRedBall.Instructions;
using FlatRedBall.Instructions.Reflection;

using FlatRedBall.IO;

using FlatRedBall.Utilities;


namespace FlatRedBall.Content.Instructions
{
    public class   InstructionSetSave
    {
        #region Fields

        public List<KeyframeListSave> Instructions;

        // Left in for Leo.
        public string SceneFileName;
        
        #region XML Docs
        /// <summary>
        /// The file that this InstructionSetSave was deserialized from.
        /// </summary>
        #endregion
        [XmlIgnore]
        string mFileName;

        public string Target;

        #endregion

        #region Methods

        #region Constructors and "FromXXXX" files
        public InstructionSetSave()
        {
            Instructions = new List<KeyframeListSave>();


        }

        public static InstructionSetSave FromFile(string fileName)
        {
            InstructionSetSave instructionSet =
                FileManager.XmlDeserialize<InstructionSetSave>(fileName);

            instructionSet.mFileName = fileName;

            return instructionSet;
        }

        #endregion

        #region Public Methods

        public void AddInstructions(IList<InstructionList> listToAdd, string name)
        {
            KeyframeListSave keyframes = new KeyframeListSave();
            foreach (InstructionList instructionList in listToAdd)
            {
                keyframes.AddList(instructionList);
                
            }
            keyframes.Name = name;
            Instructions.Add(keyframes);
        }

        public void Save(string fileName)
        {
            FileManager.XmlSerialize(this, fileName);
        }

        public InstructionSet ToInstructionSet(FlatRedBall.Scene scene)
        {
            InstructionSet instructionSet = new InstructionSet();
            instructionSet.Name = this.Target;

            INameable nameable = null;
            

            foreach (KeyframeListSave keyframeList in Instructions)
            {
                KeyframeList keyframes = new KeyframeList();
                keyframes.Name = keyframeList.Name;

                instructionSet.Add(keyframes);
                foreach (KeyframeSave keyframe in keyframeList.SceneKeyframes)
                {
                    InstructionList list = new InstructionList();
                    list.Name = keyframe.Name;
                    keyframes.Add(list);

                    foreach (InstructionSave instructionSave in keyframe.InstructionSaves)
                    {
                        if (nameable == null || nameable.Name != instructionSave.TargetName)
                        {
                            // We don't have a nameable yet, or the current instruction is 
                            // not modifying the one referenced by nameable.
                            nameable = scene.Sprites.FindByName(instructionSave.TargetName);

                            if (nameable == null)
                            {
                                nameable = scene.SpriteFrames.FindByName(instructionSave.TargetName);
                            }

                        }

                        if (nameable == null)
                        {
                            throw new NullReferenceException("Could not find an object of instance " + instructionSave.Type + " with the name " + instructionSave.TargetName);
                        }

                        list.Add(instructionSave.ToInstruction(nameable));
                    }
                }

            }

            return instructionSet;
        }


        public InstructionSet ToInstructionSet(IInstructable instructable)
        {
            InstructionSet instructionSet = new InstructionSet();
            instructionSet.Name = this.Target;

            //INameable nameable = null;


            foreach (KeyframeListSave keyframeList in Instructions)
            {
                KeyframeList keyframes = new KeyframeList();
                keyframes.Name = keyframeList.Name;

                instructionSet.Add(keyframes);
                foreach (KeyframeSave keyframe in keyframeList.SceneKeyframes)
                {
                    InstructionList list = new InstructionList();
                    list.Name = keyframe.Name;
                    keyframes.Add(list);

                    foreach (InstructionSave instructionSave in keyframe.InstructionSaves)
                    {
                        list.Add(instructionSave.ToInstruction(instructable));
                    }
                }

            }

            return instructionSet;
        }


        #endregion

        #endregion

    }
}
