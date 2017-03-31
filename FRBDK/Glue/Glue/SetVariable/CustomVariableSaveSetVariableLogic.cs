using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SaveClasses.Helpers;

namespace FlatRedBall.Glue.SetVariable
{
    public class CustomVariableSaveSetVariableLogic
    {

        public void ReactToCustomVariableChangedValue(string changedMember, CustomVariable customVariable, object oldValue)
        {

            #region Name

            if (changedMember == "Name")
            {
                ReactToChangedCustomVariableName((string)oldValue, customVariable);
            }
            #endregion

            #region SetByDerived
            if (changedMember == "SetByDerived")
            {






                bool didErrorOccur = false;

                if (customVariable.SetByDerived && customVariable.IsShared)
                {
                    MessageBox.Show("Variables that are IsShared cannot be SetByDerived");
                    didErrorOccur = true;
                }

                if (didErrorOccur)
                {
                    customVariable.SetByDerived = (bool)oldValue;
                }
                else
                {
                    ProjectManager.UpdateAllDerivedElementFromBaseValues(false, true);
                }
            }
            #endregion

            #region IsShared

            else if (changedMember == "IsShared")
            {
                HandleIsSharedVariableSet(customVariable, oldValue);
            }
            #endregion

            #region SouceObjectProperty

            else if (changedMember == "SourceObjectProperty")
            {
                // See if there is already a NOS that uses this SourceObject/SourceObjectProperty combo
                IElement currentElement = EditorLogic.CurrentElement;
                CustomVariable currentVariable = customVariable;

                if (!string.IsNullOrEmpty(currentVariable.SourceObject) && !string.IsNullOrEmpty(currentVariable.SourceObjectProperty))
                {



                    foreach (CustomVariable variableInLoop in currentElement.CustomVariables)
                    {

                        if (variableInLoop != currentVariable &&
                            !string.IsNullOrEmpty(variableInLoop.SourceObject) && currentVariable.SourceObject == variableInLoop.SourceObject &&
                            !string.IsNullOrEmpty(variableInLoop.SourceObjectProperty) && currentVariable.SourceObjectProperty == variableInLoop.SourceObjectProperty)
                        {
                            MessageBox.Show("There is already a variable that is modifying " + currentVariable.SourceObjectProperty + " on " + currentVariable.SourceObject);

                            currentVariable.SourceObjectProperty = (string)oldValue;
                        }
                    }
                }
            }

            #endregion

            #region DefaultValue

            else if (changedMember == "DefaultValue")
            {
                customVariable.FixEnumerationTypes();

                if (!string.IsNullOrEmpty(customVariable.SourceObject))
                {
                    // See if the source NamedObjectSave has
                    // this variable exposed, and if so, set that 
                    // variable too so the two mirror each other...
                    // or make it null if this is a recasted variable.
                    NamedObjectSave nos = EditorLogic.CurrentElement.GetNamedObjectRecursively(customVariable.SourceObject);

                    if (nos != null)
                    {
                        CustomVariableInNamedObject cvino = nos.GetCustomVariable(customVariable.SourceObjectProperty);

                        // If the cvino is null, that means that the NOS doesn't have this exposed, so we don't
                        // need to do anything.
                        if (cvino != null)
                        {
                            if (string.IsNullOrEmpty(customVariable.OverridingPropertyType))
                            {
                                cvino.Value = customVariable.DefaultValue;
                            }
                            else
                            {
                                cvino.Value = null;
                            }
                        }

                    }
                }
            }

            #endregion

            #region HasAccompanyingVelocityProperty
            else if (changedMember == "HasAccompanyingVelocityProperty")
            {
                ReactToChangedHasAccompanyingVelocityProperty(customVariable);
            }
            #endregion

            #region OverridingPropertyType

            else if (changedMember == "OverridingPropertyType")
            {
                if (customVariable.OverridingPropertyType != null)
                {
                    customVariable.SetDefaultValueAccordingToType(customVariable.OverridingPropertyType);
                }
                GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();
            }

            #endregion

            #region Type
            else if (changedMember == "Type")
            {
                customVariable.SetDefaultValueAccordingToType(customVariable.Type);
            }
            #endregion
        }

        private bool GetIfCanBeRenamed(CustomVariable customVariable)
        {
            if (customVariable.GetIsVariableState())
            {
                return false;
            }


            return true;
        }

        private static void HandleIsSharedVariableSet(CustomVariable customVariable, object oldValue)
        {
            // July 11, 2011
            // We used to loop
            // through all derived
            // elements and set all
            // variables with the same
            // name to be IsShared as well,
            // however, this is bad because now
            // we discourage same-named variables
            // that are not SetByDerived. 
            //if (EditorLogic.CurrentEntitySave != null)
            //{
            //    List<EntitySave> derivedEntities = ObjectFinder.Self.GetAllEntitiesThatInheritFrom(
            //        EditorLogic.CurrentEntitySave.Name);
            //    foreach (EntitySave entity in derivedEntities)
            //    {
            //        foreach (CustomVariable cv in entity.CustomVariables)
            //        {
            //            if (cv.Name == customVariable.Name)
            //            {
            //                cv.IsShared = customVariable.IsShared;
            //            }
            //        }
            //    }
            //}
            bool didErrorOccur = false;
            if (customVariable.SetByDerived && customVariable.IsShared)
            {
                MessageBox.Show("Variables which are SetByDerived cannot set IsShared to true");
                didErrorOccur = true;
            }

            if(customVariable.GetIsExposingVariable(GlueState.Self.CurrentElement) && customVariable.IsShared)
            {
                MessageBox.Show("Exposed variables cannot set IsShared to true");
                didErrorOccur = true;
            }

            if (didErrorOccur)
            {
                customVariable.IsShared = (bool)oldValue;
            }

            if (!didErrorOccur)
            {

                if (customVariable.IsShared && customVariable.GetIsCsv())
                {
                    MessageBox.Show("Shared CSV variables are not assigned until either an instance of this object is created, or until LoadStaticContent is called.  Until then, the variable will equal null");

                }
            }
        }

        private static void ReactToChangedHasAccompanyingVelocityProperty(CustomVariable customVariable)
        {
            if (customVariable.HasAccompanyingVelocityProperty)
            {
                // The user just
                // set this to true,
                // but we should check
                // if this is a good idea
                // or not - there may already
                // be a velocity variable for this.
                if (string.IsNullOrEmpty(customVariable.SourceObject))
                {
                    // todo:  fill this in
                }
                else
                {
                    // We want to set the accompanying to false before checking this, then back to true.
                    customVariable.HasAccompanyingVelocityProperty = false;
                    InterpolationCharacteristic characteristic =
                        CustomVariableHelper.GetInterpolationCharacteristic(customVariable, EditorLogic.CurrentElement);
                    customVariable.HasAccompanyingVelocityProperty = true;

                    if (characteristic == InterpolationCharacteristic.CantInterpolate)
                    {
                        MessageBox.Show("The variable " + customVariable.SourceObjectProperty + " cannot be interpolated.");
                        customVariable.HasAccompanyingVelocityProperty = false;
                    }
                    else if (characteristic == InterpolationCharacteristic.CanInterpolate)
                    {
                        string velocityMember =
                            FlatRedBall.Instructions.InstructionManager.GetVelocityForState(customVariable.SourceObjectProperty);
                        MessageBox.Show("The variable " + customVariable.SourceObjectProperty + " already has a built-in " +
                            "velocity member named " + velocityMember + "\n\nThere is no need to set an accompanying velocity property. " +
                            "Glue will undo this change now.");

                        customVariable.HasAccompanyingVelocityProperty = false;
                    }
                }

            }
        }

        private static void ReactToChangedCustomVariableName(string oldName, CustomVariable customVariable)
        {
            string whyItIsntValid = "";
            bool isNameValid = NameVerifier.IsCustomVariableNameValid(customVariable.Name, customVariable, GlueState.Self.CurrentElement, ref whyItIsntValid);
            string newName = EditorLogic.CurrentCustomVariable.Name;

            if (customVariable.GetIsVariableState() && oldName != newName)
            {
                whyItIsntValid += "\nState variables cannot be renamed - they require specific names to function properly.";
            }

            if (!string.IsNullOrEmpty(whyItIsntValid))
            {
                MessageBox.Show(whyItIsntValid);
                customVariable.Name = oldName;
                // handle invalid names here
            }
            else
            {
                IElement element = EditorLogic.CurrentElement;

                List<IElement> elementsToGenerate = new List<IElement>();
                List<IElement> elementsToSearchForTunneledVariablesIn = new List<IElement>();

                #region Change any states that use this variable
                foreach (StateSave stateSave in element.AllStates)
                {
                    foreach (InstructionSave instructionSave in stateSave.InstructionSaves)
                    {
                        if (instructionSave.Member == oldName)
                        {
                            instructionSave.Member = newName;
                        }
                    }
                }

                #endregion

                #region Change any NOS that uses this as its source
                List<NamedObjectSave> nosList = ObjectFinder.Self.GetAllNamedObjectsThatUseElement(element);
                foreach (NamedObjectSave nos in nosList)
                {
                    IElement container = ObjectFinder.Self.GetElementContaining(nos);

                    if (!elementsToSearchForTunneledVariablesIn.Contains(container))
                    {
                        elementsToSearchForTunneledVariablesIn.Add(container);
                    }

                    if (nos.RenameVariable(oldName, newName))
                    {
                        if (!elementsToGenerate.Contains(container))
                        {
                            elementsToGenerate.Add(container);
                        }
                    }
                }

                #endregion

                #region Change any CustomVaribles that tunnel in to this variable
                foreach (IElement elementToCheck in elementsToSearchForTunneledVariablesIn)
                {
                    foreach (CustomVariable variableToCheck in elementToCheck.CustomVariables)
                    {
                        if (!string.IsNullOrEmpty(variableToCheck.SourceObject) && !string.IsNullOrEmpty(variableToCheck.SourceObjectProperty) &&
                            variableToCheck.SourceObjectProperty == oldName)
                        {
                            NamedObjectSave nos = elementToCheck.GetNamedObjectRecursively(variableToCheck.SourceObject);

                            // just to be safe
                            if (nos != null && nosList.Contains(nos))
                            {
                                variableToCheck.SourceObjectProperty = newName;

                                if (!elementsToGenerate.Contains(elementToCheck))
                                {
                                    elementsToGenerate.Add(elementToCheck);
                                }
                            }
                        }
                    }
                }

                #endregion

                #region Change all events that reference this variable

                foreach (var eventResponse in element.Events)
                {
                    if (eventResponse.SourceVariable == oldName)
                    {
                        eventResponse.SourceVariable = newName;
                        Plugins.PluginManager.ReceiveOutput("Changing event " + eventResponse.EventName + " to use variable " + newName);
                    }
                }

                #endregion

                foreach (IElement toRegenerate in elementsToGenerate)
                {
                    CodeWriter.GenerateCode(toRegenerate);

                }

            }
        }

    }
}
