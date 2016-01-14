using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.SetVariable
{
    // Made public for unit tests
    public class StateSaveSetVariableLogic
    {
        public void ReactToStateSaveChangedValue(StateSave stateSave, StateSaveCategory category, string changedMember, object oldValue, IElement parentObject, ref bool updateTreeView)
        {
            if (changedMember != "Name")
            {
                updateTreeView = false;
            }

            // See if this is an Unmodified object
            // We don't support NamedObjectPropertyOverrides anymore
            //NamedObjectPropertyOverride propertyOverride = parentObject as NamedObjectPropertyOverride;
            //if (propertyOverride != null)
            //{
            //    if (stateSave.NamedObjectPropertyOverrides.Contains(propertyOverride))
            //    {
            //        if (changedMember == "SourceFile" && propertyOverride.SourceFile == AvailableFileStringConverter.UseDefaultString)
            //        {
            //            propertyOverride.SourceFile = null;
            //        }

            //        if (propertyOverride.IsNulledOut)
            //        {
            //            stateSave.NamedObjectPropertyOverrides.Remove(propertyOverride);
            //        }
            //    }
            //    else
            //    {
            //        switch (changedMember)
            //        {
            //            case "SourceFile":
            //                propertyOverride.SourceFile = NamedObjectPropertyOverride.SourceFileBuffer;
            //                break;
            //        }
            //        stateSave.NamedObjectPropertyOverrides.Add(propertyOverride);
            //    }
            //}

            if (changedMember == "Name")
            {
                string whyItIsntValid;
                if (!NameVerifier.IsStateNameValid(stateSave.Name, EditorLogic.CurrentElement, EditorLogic.CurrentStateSaveCategory, EditorLogic.CurrentStateSave, out whyItIsntValid))
                {
                    stateSave.Name = (string)oldValue;
                    updateTreeView = false;
                    AutomatedGlue.GlueGui.ShowMessageBox(whyItIsntValid);

                }
                else
                {
                    ReactToStateNameChange(oldValue, stateSave, category, parentObject);
                }
            }
        }

        private static void ReactToStateNameChange(object oldValue, StateSave stateSave, StateSaveCategory category, IElement parentObject)
        {
            PluginManager.ReactToStateNameChange(parentObject as IElement, (string)oldValue, stateSave.Name);


            IElement parentAsElement = parentObject as IElement;

            string name = parentObject.Name;

            string typeToMatch = "VariableState";
            if (category != null && category.SharesVariablesWithOtherCategories == false)
            {
                typeToMatch = "Current" + category.Name + "State";
            }

            var matchingVariables = from variable in parentAsElement.CustomVariables
                                    where variable.DefaultValue == oldValue &&
                                    variable.Type == typeToMatch
                                    select variable;

            foreach (CustomVariable variable in matchingVariables)
            {
                variable.DefaultValue = stateSave.Name;
            }

            string variableName = stateSave.GetExposedVariableName(parentObject);
            string variableType = stateSave.GetEnumTypeName(parentObject);

            // Find any NOSs that use the ParentObject as their type
            foreach (ScreenSave screenSave in ObjectFinder.Self.GlueProject.Screens)
            {
                var customQuery = from nos in screenSave.AllNamedObjects
                                    where nos.SourceType == SourceType.Entity &&
                                    nos.SourceClassType == name
                                    select nos;

                foreach (var nos in customQuery)
                {
                    if (nos.CurrentState == (oldValue as string))
                    {
                        nos.CurrentState = stateSave.Name;
                    }
                    foreach (var variable in nos.InstructionSaves.Where(item => item.Member == variableName && (item.Value as string) == (oldValue as string)))
                    {
                        variable.Value = stateSave.Name;
                    }
                }
            }

            foreach (EntitySave entitySave in ObjectFinder.Self.GlueProject.Entities)
            {
                var customQuery = from nos in entitySave.AllNamedObjects
                                    where nos.SourceType == SourceType.Entity &&
                                    nos.SourceClassType == name
                                    select nos;

                foreach (var nos in customQuery)
                {
                    if (nos.CurrentState == (oldValue as string))
                    {

                        nos.CurrentState = stateSave.Name;
                    }
                    foreach (var variable in nos.InstructionSaves.Where(item => item.Member == variableName && (item.Value as string) == (oldValue as string)))
                    {
                        variable.Value = stateSave.Name;
                    }
                }
            }
        }


    }
}
