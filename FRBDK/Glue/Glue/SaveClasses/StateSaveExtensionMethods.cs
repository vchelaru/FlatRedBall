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
        public static void SetValue(this StateSave stateSave, string variableName, object valueToSet)
        {
#if GLUE
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
#endif
            bool wasFound = false;

            // See if there is an instructionD

            #region Set the existing instruction's value if there is one already

            foreach (InstructionSave instructionSave in stateSave.InstructionSaves)
            {
                if (instructionSave.Member == variableName)
                {
                    wasFound = true;
                    instructionSave.Value = valueToSet;
                    break;
                }
            }

            #endregion

            if (!wasFound)
            {
                IElement container = ObjectFinder.Self.GetElementContaining(stateSave);

                CustomVariable variable = null;

                foreach (CustomVariable containedVariable in container.CustomVariables)
                {
                    if (containedVariable.Name == variableName)
                    {
                        variable = containedVariable;
                        break;
                    }
                }



                InstructionSave instructionSave = new InstructionSave();
                instructionSave.Value = valueToSet; // make it the default

                instructionSave.Type = valueToSet.GetType().Name;
                instructionSave.Member = variableName;
			    // Create a new instruction

                stateSave.InstructionSaves.Add(instructionSave);

                stateSave.SortInstructionSaves(container.CustomVariables);
            }
        }


        public static void FixEnumerationTypes(this StateSave instance)
        {
            foreach (InstructionSave instructionSave in instance.InstructionSaves)
            {
                Type type = TypeManager.GetTypeFromString(instructionSave.Type);

                if (type != null && type.IsEnum && instructionSave.Value.GetType() == typeof(int) )
                {
                    Array array = Enum.GetValues(type);

                    instructionSave.Value = array.GetValue((int)instructionSave.Value);
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
                    if (category.SharesVariablesWithOtherCategories)
                    {
                        return "CurrentState";
                    }
                    else
                    {
                        return "Current" + category.Name + "State";
                    }
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
                    if (category.SharesVariablesWithOtherCategories)
                    {
                        return "VariableState";
                    }
                    else
                    {
                        return category.Name;
                    }
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
