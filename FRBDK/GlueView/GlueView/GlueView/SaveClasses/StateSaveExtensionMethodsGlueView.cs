using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Content.Instructions;

namespace GlueView.SaveClasses
{
    public static class StateSaveExtensionMethodsGlueView
    {
        public static InstructionSave GetInstruction(this StateSave stateSave, string memberName)
        {
            foreach (InstructionSave instructionSave in stateSave.InstructionSaves)
            {
                if (instructionSave.Member == memberName)
                {
                    return instructionSave;
                }
            }
            return null;
        }

        public static StateSave CreateCombinedState(StateSave firstState, StateSave secondState, float interpolationValue)
        {
            StateSave stateSave = new StateSave();


            float firstPercentage = 1 - interpolationValue;// PercentageTrackBar.Value / 100.0f;
            float secondPercentage = interpolationValue;

            foreach (InstructionSave instruction in firstState.InstructionSaves)
            {
                // Does the 2nd also have this?
                InstructionSave matchingInstruction = secondState.GetInstruction(instruction.Member);
                
                if (matchingInstruction != null)
                {
                    InstructionSave combinedInstruction = instruction.Clone();

                    object value = null;

                    if (combinedInstruction.Value is int)
                    {
                        value = (int)  ((int)instruction.Value * firstPercentage + (int)matchingInstruction.Value * secondPercentage);
                    }
                    else if (combinedInstruction.Value is float)
                    {
                        value = (float)((float)instruction.Value * firstPercentage + (float)matchingInstruction.Value * secondPercentage);
                    }
                    else if (combinedInstruction.Value is double)
                    {
                        value = (double)((double)instruction.Value * firstPercentage + (double)matchingInstruction.Value * secondPercentage);
                    }
                    else if (combinedInstruction.Value is long)
                    {
                        value = (long)((long)instruction.Value * firstPercentage + (long)matchingInstruction.Value * secondPercentage);
                    }
                    else
                    {
                        if (secondPercentage == 1.0f)
                        {
                            value = matchingInstruction.Value;
                        }
                        else
                        {
                            value = instruction.Value;
                        }

                    }

                    combinedInstruction.Value = value;

                    stateSave.InstructionSaves.Add(combinedInstruction);
                }

            }



            return stateSave;
        }



    }
}
