using System;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.Controls;

namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Split out of <c>StateSaveExtensionMethods</c> (in <c>Glue.csproj</c>, net8.0-windows): the
    /// name/category lookups that relate a <see cref="StateSave"/> to its containing element. They
    /// take the container as a parameter or, for <see cref="StateSaveToString"/>, resolve it through
    /// <see cref="IObjectFinderCore"/> instead of reaching for <c>ObjectFinder.Self</c>. Lives here
    /// (net8.0, no WPF) so it and its tests can build and run on Linux/macOS. See #2276. Named
    /// differently from the original class (not a forwarding stub) to avoid a duplicate-type clash now
    /// that both assemblies are visible together via <c>Glue.csproj</c>'s <c>ProjectReference</c> to
    /// <c>GlueCommon</c>; the two non-extension static calls
    /// (<see cref="GetStateTypeFromCurrentVariableName"/>, <see cref="StateSaveToString"/>) had their
    /// call sites updated to the new class name. <see cref="SetValue"/> asks its "set in other
    /// categories" confirm through <see cref="IErrorReportingCore"/> instead of <c>DialogService</c>.
    /// </summary>
    public static class StateSaveElementExtensions
    {
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

        public static string StateSaveToString(StateSave stateSave)
        {
            return stateSave.Name + "(State in " + ObjectFinderCore.Self.GetElementContaining(stateSave) + ")";
        }

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
                    ErrorReportingCore.Self.ShowConfirm("The variable " + withoutSpace + " is set in other categories that do not share states.  Are you sure you want to set it?");

                if (result == DialogButton.Yes)
                {
                    variableName = withoutSpace;
                }
            }

            bool wasFound = false;

            var container = ObjectFinderCore.Self.GetElementContaining(stateSave);
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
