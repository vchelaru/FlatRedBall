using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.Xml.Serialization;
using FlatRedBall.Instructions;

using FlatRedBall.Utilities;

namespace FlatRedBall.Content.Instructions
{
    public class KeyframeListSave
    {
        public List<KeyframeSave> SceneKeyframes = new List<KeyframeSave>();

        public string Name;

        public void AddList(InstructionList list)
        {
            KeyframeSave keyframe = new KeyframeSave();
            foreach (Instruction instruction in list)
            {
                if (instruction is GenericInstruction)
                {
                    GenericInstruction asGenericInstruction = instruction as GenericInstruction;
                    InstructionSave instructionSave = InstructionSave.FromInstruction(asGenericInstruction);

                    keyframe.InstructionSaves.Add(instructionSave);
                }
                else
                {
                    throw new NotImplementedException("This list contains a type of instruction that cannot be saved.");
                }
            }
            keyframe.Name = list.Name;
            SceneKeyframes.Add(keyframe);
        }
    }
}
