using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.SetVariable
{
    // Made public for unit tests
    public class StateSaveSetVariableLogic
    {
        public void ReactToStateSaveChangedValue(StateSave stateSave, StateSaveCategory category, string changedMember, object oldValue, GlueElement stateOwner, ref bool updateTreeView)
        {
            if (changedMember != "Name")
            {
                updateTreeView = false;
            }

            if (changedMember == "Name")
            {
                string whyItIsntValid;
                if (!NameVerifier.IsStateNameValid(stateSave.Name, 
                    GlueState.Self.CurrentElement, GlueState.Self.CurrentStateSaveCategory, GlueState.Self.CurrentStateSave, out whyItIsntValid))
                {
                    stateSave.Name = (string)oldValue;
                    updateTreeView = false;
                    AutomatedGlue.GlueGui.ShowMessageBox(whyItIsntValid);

                }
                else
                {
                    ReactToStateNameChange((string)oldValue, stateSave, category, stateOwner);
                }
            }
        }

        private static void ReactToStateNameChange(string oldValue, StateSave stateSave, StateSaveCategory category, GlueElement stateOwner)
        {
            PluginManager.ReactToStateNameChange(stateOwner, oldValue, stateSave.Name);

            string name = stateOwner.Name;

            string typeToMatch = "VariableState";
            if (category != null)
            {
                typeToMatch = "Current" + category.Name + "State";
            }

            var matchingVariables = from variable in stateOwner.CustomVariables
                                    where (variable.DefaultValue as string) == oldValue &&
                                    variable.Type == typeToMatch
                                    select variable;

            foreach (CustomVariable variable in matchingVariables)
            {
                variable.DefaultValue = stateSave.Name;
            }

            string variableName = stateSave.GetExposedVariableName(stateOwner);
            string variableType = stateSave.GetEnumTypeName(stateOwner);

            var objectsReferencingElement = ObjectFinder.Self.GetAllNamedObjectsThatUseElement(stateOwner);

            HashSet<GlueElement> objectsToRegenerate = new HashSet<GlueElement>();

            foreach (var nos in objectsReferencingElement)
            {
                var shouldRegenerate = false;
                if (nos.CurrentState == oldValue)
                {
                    nos.CurrentState = stateSave.Name;
                    shouldRegenerate = true;
                }
                foreach (var variable in nos.InstructionSaves.Where(item => item.Member == variableName && (item.Value as string) == oldValue))
                {
                    variable.Value = stateSave.Name;
                    shouldRegenerate = true;
                }
                if(shouldRegenerate)
                {
                    objectsToRegenerate.Add(ObjectFinder.Self.GetElementContaining(nos));
                }
            }

            foreach(var element in objectsToRegenerate)
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
            }

        }
    }
}
