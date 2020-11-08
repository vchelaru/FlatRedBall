using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Instructions.Pause
{
    class InstructionListUnpause : UnpauseInstruction<InstructionList>
    {

        public InstructionList TemporaryInstructions { get; private set; } = new InstructionList();

        #region Methods

        public InstructionListUnpause(InstructionList instructions) 
            : base(instructions)
        {
            foreach (Instruction instruction in instructions)
            {
                TemporaryInstructions.Add(instruction);
            }

        }

        public override void Execute()
        {
            double timeToAdd = TimeManager.CurrentTime - mCreationTime;

            foreach (Instruction instruction in TemporaryInstructions)
            {
                instruction.TimeToExecute += timeToAdd;
                mTarget.Add(instruction);
            }
        }

        public override void ExecuteOn(object target)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        #endregion
    }
}
