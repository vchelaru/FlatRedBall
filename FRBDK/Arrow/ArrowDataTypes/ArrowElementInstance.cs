using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Content.Instructions;

namespace FlatRedBall.Arrow.DataTypes
{
    public class ArrowElementInstance
    {
        public string Type;
        public string Name;
        
        public List<InstructionSave> Variables = new List<InstructionSave>();
        public bool ShouldSerializeVariables()
        {
            return Variables != null && Variables.Count != 0;
        }

        public void SetVariable(string variableName, object value)
        {
            InstructionSave instructionSave = Variables.FirstOrDefault(item => item.Member == variableName);

            if (instructionSave == null)
            {
                instructionSave = new InstructionSave();
                instructionSave.Member = variableName;
                Variables.Add(instructionSave);

                instructionSave.Type = value.GetType().Name;
            }

            instructionSave.Value = value;
        }

        public object GetVariable(string variableName)
        {
            InstructionSave instructionSave = Variables.FirstOrDefault(item => item.Member == variableName);

            if (instructionSave != null)
            {
                return instructionSave.Value;
            }

            return null;
        }

    }
}
