using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Instructions;

namespace FlatRedBall.Content.Instructions
{
    #region XML Docs
    /// <summary>
    /// A save class containing a list of InstructionSaves.
    /// </summary>
    #endregion
    public class InstructionBlueprintListSave
    {
        #region Fields
        public string Name;
        public List<InstructionSave> Instructions;
        #endregion

        #region Methods

        public InstructionBlueprintListSave()
        {
            Instructions = new List<InstructionSave>();
        }

        public static InstructionBlueprintListSave FromInstructionBlueprintList(InstructionBlueprintList InstructionBlueprintList){
            InstructionBlueprintListSave itls = new InstructionBlueprintListSave();

            foreach (InstructionBlueprint template in InstructionBlueprintList)
            {
                itls.Instructions.Add(InstructionSave.FromInstructionBlueprint(template));
            }

            return itls;
        }

        public InstructionBlueprintList ToInstructionBlueprintList()
        {
            InstructionBlueprintList itl = new InstructionBlueprintList();


            foreach (InstructionSave save in Instructions)
            {
                itl.Add(save.ToInstructionBlueprint());
            }

            return itl;
        }

        #endregion


    }
}
