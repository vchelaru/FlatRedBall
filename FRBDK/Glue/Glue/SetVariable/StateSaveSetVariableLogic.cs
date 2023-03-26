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
                    ReactToStateRenamed((string)oldValue, stateSave, category, stateOwner);
                }
            }
        }

        private static void ReactToStateRenamed(string oldValue, StateSave stateSave, StateSaveCategory category, GlueElement stateOwner)
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

            // Other elements may reference this
            var glueProject = GlueState.Self.CurrentGlueProject;
            var qualifiedType = stateOwner.Name.Replace("//", ".").Replace("\\", ".");
            if(category != null)
            {
                qualifiedType += "." + category.Name;
            }
            else
            {
                qualifiedType += ".VariableState";
            }

            foreach (var element in glueProject.Entities)
            {
                if(element == stateOwner) continue; 

                if(HandleRenamedState(element, qualifiedType, oldValue, stateSave))
                {
                    objectsToRegenerate.Add(element);
                }
            }
            foreach(var element in glueProject.Screens)
            {
                if (element == stateOwner) continue;

                if (HandleRenamedState(element, qualifiedType, oldValue, stateSave))
                {
                    objectsToRegenerate.Add(element);
                }
            }

            foreach(var element in objectsToRegenerate)
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
            }

        }

        private static bool HandleRenamedState(GlueElement element, string qualifiedType, string oldName, StateSave stateSave)
        {
            var didChange = false;

            foreach(var variable in element.CustomVariables)
            {
                var variableType = variable.Type;

                if(variableType == qualifiedType && (variable.DefaultValue as string) == oldName)
                {
                    variable.DefaultValue = stateSave.Name;
                    didChange = true;
                }
            }

            foreach(var state in element.AllStates)
            {
                foreach(var variable in state.InstructionSaves)
                {
                    if(variable.Type == qualifiedType && (variable.Value as string) == oldName)
                    {
                        variable.Value = stateSave.Name;
                        didChange = true;
                    }
                }
            }

            return didChange;
        }
    }
}
