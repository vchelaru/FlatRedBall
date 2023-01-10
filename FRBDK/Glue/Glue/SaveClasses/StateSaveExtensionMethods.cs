using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Content.Instructions;
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

                DialogResult result = 
                    MessageBox.Show("The variable " + withoutSpace + " is set in other categories that do not share states.  Are you sure you want to set it?", "Set variable?", MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
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

        public static void FixAllTypes(this StateSave instance, GlueElement owner)
        {
            instance.FixEnumerationTypes();

            foreach (InstructionSave instruction in instance.InstructionSaves)
            {
                if (!string.IsNullOrEmpty(instruction.Type) && instruction.Value != null)
                {
                    object variableValue = instruction.Value;

                    var matchingVariable = owner.GetCustomVariable(instruction.Member);
                    if( !string.IsNullOrWhiteSpace( matchingVariable?.Type ) && matchingVariable.Type != instruction.Type)
                    {
                        // The variable type has been changed, so let's update the state type:
                        instruction.Type = matchingVariable.Type;
                    }

                    var type = instruction.Type;
                    variableValue = CustomVariableExtensionMethods.FixValue(variableValue, type);
                    instruction.Value = variableValue;
                }
            }
        }

        public static void FixEnumerationTypes(this StateSave instance)
        {
            foreach (InstructionSave instructionSave in instance.InstructionSaves)
            {
                Type type = TypeManager.GetTypeFromString(instructionSave.Type);

                if (type != null && type.IsEnum && instructionSave.Value?.GetType() != type)
                {
                    int valueAsInt = 0;
                    if(instructionSave.Value is int asInt)
                    {
                        valueAsInt = asInt;
                    }
                    else if(instructionSave.Value is long asLong)
                    {
                        valueAsInt = (int)asLong;
                    }
                    Array array = Enum.GetValues(type);

                    instructionSave.Value = array.GetValue(valueAsInt);
                }
            }
        }

        public static void ConvertEnumerationValuesToInts(this StateSave instance)
        {
            foreach (InstructionSave instructionSave in instance.InstructionSaves)
            {
                if(instructionSave.Value?.GetType()?.IsEnum == true)
                {
                    instructionSave.Value = (int)instructionSave.Value;
                }
            }
        }

        // This function incorrectly
        // assumes that all variables
        // that represent states will be
        // named Current<StateType>State. This
        // is not true if the variable is tunneled,
        // so we really shouldn't use this in most places.
        public static string GetStateTypeFromCurrentVariableName(string memberName)
        {
            if (memberName == "CurrentState")
            {
                return "VariableState";
            }
            else
            {
                string possibleCategory = memberName.Substring("Current".Length);

                possibleCategory = possibleCategory.Substring(0, possibleCategory.Length - "State".Length);
                return possibleCategory;
            }
        }

        public static bool ContainsCategoryName(this List<StateSaveCategory> categoryList, string name)
        {
            foreach (StateSaveCategory category in categoryList)
            {
                if (category.Name == name)
                {
                    return true;
                }
            }
            return false;
        }

        public static string GetExposedVariableName(this StateSave stateSave, IElement container)
        {
            if (container.States.Contains(stateSave))
            {
                return "CurrentState";
            }
            else
            {
                foreach (var category in container.StateCategoryList.Where(item => item.States.Contains(stateSave)))
                {
                    return "Current" + category.Name + "State";
                }
            }

            return "CurrentState";
        }

        public static string GetEnumTypeName(this StateSave stateSave, IElement container)
        {
            if (container.States.Contains(stateSave))
            {
                return "VariableState";
            }
            else
            {
                foreach (var category in container.StateCategoryList.Where(item => item.States.Contains(stateSave)))
                {
                    return category.Name;
                }
            }

            return "VariableState";

        }

        internal static string StateSaveToString(StateSave stateSave)
        {

            return stateSave.Name + "(State in " + ObjectFinder.Self.GetElementContaining(stateSave) + ")";

            throw new NotImplementedException();
        }
    }
}
