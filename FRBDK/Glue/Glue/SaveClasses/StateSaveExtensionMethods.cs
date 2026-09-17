using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Parsing;

namespace FlatRedBall.Glue.SaveClasses
{
    public static class StateSaveExtensionMethods
    {
        public static void RemoveVariable(this StateSave stateSave, string variableName)
        {
            var found = stateSave.InstructionSaves.FirstOrDefault(item => item.Member == variableName);

            if(found != null)
            {
                stateSave.InstructionSaves.Remove(found);
            }
        }

        public static void SetValue(this StateSave stateSave, string variableName, object valueToSet)
        {
            if(stateSave == null)
            {
                throw new ArgumentNullException(nameof(stateSave));
            }

            if (variableName.Contains(" set in "))
            {
                string withoutSpace = variableName.Substring(0, variableName.IndexOf(' '));

                var result =
                    DialogService.ShowConfirm("The variable " + withoutSpace + " is set in other categories that do not share states.  Are you sure you want to set it?");

                if (result == DialogButton.Yes)
                {
                    variableName = withoutSpace;
                }
            }

            bool wasFound = false;

            var container = ObjectFinder.Self.GetElementContaining(stateSave);
            CustomVariable variable = container.CustomVariables.FirstOrDefault(item => item.Name == variableName);
            var variableType = variable?.Type ?? valueToSet?.GetType().Name;

            // This was commented on commit 88915fcac8b236ed729b7063c353094ceeed2dc7
            // Commit 88915fcac8b236ed729b7063c353094ceeed2dc7
            // Author: Victor Chelaru<VicChelaru@gmail.com>
            //Date: Saturday, January 22, 2022 11:29 AM
            //Parent: 5a3487f2
            //Fixed assignment of nullables on states in state data tab.
            // Why?
            // Update - because this is assigned by the StateCategoryViewModel.cs Convert method
            //if (variableType != "string" && valueToSet is string valueAsString)
            //{
            //    valueToSet = Instructions.Reflection.PropertyValuePair.ConvertStringToType(valueAsString, variableType);
            //}

            #region Set the existing instruction's value if there is one already

            var foundInstruction = stateSave.InstructionSaves.FirstOrDefault(item => item.Member == variableName);
            if(foundInstruction != null)
            {
                wasFound = true;
                foundInstruction.Value = valueToSet;
            }

            #endregion

            if (!wasFound)
            {


                InstructionSave instructionSave = new InstructionSave();
                instructionSave.Value = valueToSet; // make it the default

                instructionSave.Type = variableType;
                instructionSave.Member = variableName;
			    // Create a new instruction

                stateSave.InstructionSaves.Add(instructionSave);

                stateSave.SortInstructionSaves(container.CustomVariables);
            }
        }
    }
}
