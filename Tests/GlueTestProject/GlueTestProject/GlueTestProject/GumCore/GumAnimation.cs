using FlatRedBall;
using FlatRedBall.Instructions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Gum.Animation
{
    public class GumAnimation
    {
        public IEnumerable<Instruction> Instructions { get; set; }

        public float Length
        {
            get;
            private set;
        }

        public GumAnimation(float length)
        {
            this.Length = length;
        }

        public void Play()
        {
            InstructionManager.Instructions.AddRange(this.Instructions);
        }

        public void PlayAfter(float delay)
        {
            DelegateInstruction instruction = new DelegateInstruction(() =>
            {
                InstructionManager.Instructions.AddRange(this.Instructions);
            });
            instruction.TimeToExecute = TimeManager.CurrentTime + delay;
            InstructionManager.Instructions.Add(instruction);

        }
    }
}
