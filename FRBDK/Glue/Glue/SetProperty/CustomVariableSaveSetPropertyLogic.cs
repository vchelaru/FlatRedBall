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
using GlueFormsCore.Managers;
using System.Threading.Tasks;
using FlatRedBall.Glue.Managers;

namespace FlatRedBall.Glue.SetVariable
{
    public class CustomVariableSaveSetPropertyLogic
    {
        public async Task ReactToCustomVariableChangedValue(string changedMember, CustomVariable customVariable, object oldValue)
        {
            var element = ObjectFinder.Self.GetElementContaining(customVariable);

            #region Name

            if (changedMember == nameof(CustomVariable.Name))
            {
                await ReactToChangedCustomVariableNameAsync((string)oldValue, customVariable);
            }
            #endregion

            #region SetByDerived
            if (changedMember == nameof(CustomVariable.SetByDerived))
            {
                HandleSetByDerivedChanged(customVariable, oldValue, element);
            }
            #endregion

            #region IsShared

            else if (changedMember == nameof(CustomVariable.IsShared))
            {
                HandleIsSharedVariableSet(customVariable, oldValue);
            }
            #endregion

            #region Scope

            else if(changedMember == nameof(CustomVariable.Scope))
            {
                HandleScopeSet(customVariable, oldValue);
            }

            #endregion

            #region SouceObjectProperty

            else if (changedMember == nameof(CustomVariable.SourceObjectProperty))
            {
                // See if there is already a NOS that uses this SourceObject/SourceObjectProperty combo
                IElement currentElement = GlueState.Self.CurrentElement;
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

            else if (changedMember == nameof(CustomVariable.DefaultValue))
            {
                customVariable.FixEnumerationTypes();

                var currentElement = GlueState.Self.CurrentElement;

                if (!string.IsNullOrEmpty(customVariable.SourceObject))
                {

                    // See if the source NamedObjectSave has
                    // this variable exposed, and if so, set that 
                    // variable too so the two mirror each other...
                    // or make it null if this is a recasted variable.
                    NamedObjectSave nos = currentElement?.GetNamedObjectRecursively(customVariable.SourceObject);

                    if (nos != null)
                    {
                        var cvino = nos.GetCustomVariable(customVariable.SourceObjectProperty);

                        var variableDefinition = nos.GetAssetTypeInfo()
                            ?.VariableDefinitions.Find(item => item.Name == customVariable.SourceObjectProperty);

                        if (variableDefinition?.CustomVariableSet != null)
                        {
                            variableDefinition.CustomVariableSet(
                                element, nos, customVariable.Name, customVariable.DefaultValue);
                        }
                        else
                        {
                            // If the cvino is null, that means that the NOS doesn't have this exposed, so we don't
                            // need to do anything.
                            // Update June 12, 2022
                            // Actually, if we don't
                            // set this, then changing
                            // the tunneled value will not
                            // change the SourceObject value
                            // which can cause confusion.



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
                            else
                            {
                                // This is a new add June 12, 2022. Not sure if we should globally
                                // add this value, or if it should only be for NOS's which have an ATI
                                // with a variable definition. Let's be safe and require ATIs for now:
                                if (variableDefinition != null)
                                {
                                    GlueCommands.Self.GluxCommands.SetVariableOn(nos, customVariable.SourceObjectProperty, customVariable.DefaultValue, false, false);
                                }
                            }
                        }
                    }
                }

                Plugins.PluginManager.ReactToElementVariableChange(currentElement, customVariable);
            }

            #endregion

            #region HasAccompanyingVelocityProperty
            else if (changedMember == nameof(CustomVariable.HasAccompanyingVelocityProperty))
            {
                ReactToChangedHasAccompanyingVelocityProperty(customVariable);
            }
            #endregion

            #region OverridingPropertyType

            else if (changedMember == nameof(CustomVariable.OverridingPropertyType))
            {
                if (customVariable.OverridingPropertyType != null)
                {
                    customVariable.SetDefaultValueAccordingToType(customVariable.OverridingPropertyType);
                }
                GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();
            }

            #endregion

            #region Type
            else if (changedMember == nameof(CustomVariable.Type))
            {
                await HandleChangeVariableType(customVariable, oldValue);
            }
            #endregion

            #region Category
            else if (changedMember == nameof(CustomVariable.Category))
            {
                HandleChangedCategory(customVariable);
            }
            #endregion
        }

        #region Name

        private static async Task ReactToChangedCustomVariableNameAsync(string oldName, CustomVariable customVariable)
        {
            var currentElement = GlueState.Self.CurrentElement;
            var currentVariable = GlueState.Self.CurrentCustomVariable;

            await TaskManager.Self.AddAsync(() =>
            {
                /////////////////////Early Out///////////////////////////////
                string whyItIsntValid = "";
                bool isNameValid = NameVerifier.IsCustomVariableNameValid(customVariable.Name, customVariable, currentElement, ref whyItIsntValid);
                string newName = currentVariable.Name;

                if (customVariable.GetIsVariableState() && oldName != newName)
                {
                    // This is only the case if the state is inside the entity itself. Otherwise, it can be renamed just fine...

                    var elementDefiningCategory = ObjectFinder.Self.GetElementDefiningStateCategory(customVariable.Type);
                    var isDefinedInDifferentElement = elementDefiningCategory != null &&
                        elementDefiningCategory != currentElement &&
                        ObjectFinder.Self.GetAllBaseElementsRecursively(currentElement).Contains(elementDefiningCategory) == false;

                    if (isDefinedInDifferentElement == false)
                    {
                        whyItIsntValid += "\nState variables cannot be renamed - they require specific names to function properly.";
                    }
                }

                if (!string.IsNullOrEmpty(whyItIsntValid))
                {
                    GlueCommands.Self.DialogCommands.ShowMessageBox(whyItIsntValid);
                    customVariable.Name = oldName;
                    return;
                }

                //////////////End Early Out/////////////////////////////////

                var elementsToGenerate = new HashSet<GlueElement>();
                var elementsToSearchForTunneledVariablesIn = new List<GlueElement>();

                #region Change any states that use this variable
                foreach (StateSave stateSave in currentElement.AllStates)
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
                List<NamedObjectSave> nosList = ObjectFinder.Self.GetAllNamedObjectsThatUseElement(currentElement);
                foreach (var nos in nosList)
                {
                    var container = ObjectFinder.Self.GetElementContaining(nos);

                    if (!elementsToSearchForTunneledVariablesIn.Contains(container))
                    {
                        elementsToSearchForTunneledVariablesIn.Add(container);
                    }

                    if (nos.RenameVariable(oldName, newName))
                    {
                        elementsToGenerate.Add(container);
                    }
                }

                #endregion

                #region Change any CustomVaribles that tunnel in to this variable
                foreach (var elementToCheck in elementsToSearchForTunneledVariablesIn)
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

                                elementsToGenerate.Add(elementToCheck);
                            }
                        }
                    }
                }

                #endregion

                #region Change all events that reference this variable

                foreach (var eventResponse in currentElement.Events)
                {
                    if (eventResponse.SourceVariable == oldName)
                    {
                        eventResponse.SourceVariable = newName;
                        Plugins.PluginManager.ReceiveOutput("Changing event " + eventResponse.EventName + " to use variable " + newName);
                    }
                }

                #endregion

                var derivedElements = ObjectFinder.Self.GetAllElementsThatInheritFrom(currentElement);
                foreach (var derivedElement in derivedElements)
                {
                    foreach (var variable in derivedElement.CustomVariables)
                    {
                        if (variable.DefinedByBase && variable.Name == oldName)
                        {
                            variable.Name = newName;
                        }
                    }
                    elementsToGenerate.Add(derivedElement);
                }

                foreach (var toRegenerate in elementsToGenerate)
                {
                    CodeWriter.GenerateCode(toRegenerate);
                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(toRegenerate);
                }

            }, nameof(ReactToChangedCustomVariableNameAsync));


        }

        #endregion

        #region SetByDerived

        private static void HandleSetByDerivedChanged(CustomVariable customVariable, object oldValue, GlueElement element)
        {
            bool didErrorOccur = false;

            if (customVariable.SetByDerived && customVariable.IsShared)
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox("Variables that are IsShared cannot be SetByDerived");
                didErrorOccur = true;
            }

            if (didErrorOccur)
            {
                customVariable.SetByDerived = (bool)oldValue;
            }
            else
            {
                // This variable should not be included in derived categories by default, this could cause unexpected behavior so let's exclude it
                var derivedElements = ObjectFinder.Self.GetAllDerivedElementsRecursive(element);
                foreach(var derivedElement in derivedElements)
                {
                    foreach(var category in derivedElement.StateCategoryList)
                    {
                        if(!category.ExcludedVariables.Contains(customVariable.Name))
                        {
                            category.ExcludedVariables.Add(customVariable.Name);
                        }
                    }

                }

                InheritanceManager.UpdateAllDerivedElementFromBaseValues(true, element);
            }
        }

        #endregion

        #region IsShared

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

        #endregion

        #region Scope

        private void HandleScopeSet(CustomVariable customVariable, object oldValue)
        {
            var owner = GlueState.Self.CurrentElement;

            var newScope = customVariable.Scope;

            SetDerivedElementVariables(owner, customVariable.Name, newScope);

        }

        private void SetDerivedElementVariables(IElement owner, string name, Scope newScope)
        {
            var instancesThatUseThis = ObjectFinder.Self.GetAllNamedObjectsThatUseElement(owner);
            foreach (var instance in instancesThatUseThis)
            {
                instance.UpdateCustomProperties();
            }

            var elementsThatDerive = ObjectFinder.Self.GetAllElementsThatInheritFrom(owner);

            foreach(var derivedElement in elementsThatDerive)
            {
                var customVariable = derivedElement.CustomVariables.Find(item => item.Name == name);
                if(customVariable != null)
                {
                    customVariable.Scope = newScope;
                }

                SetDerivedElementVariables(derivedElement, name, newScope);
            }
        }

        #endregion

        #region HasAccompanyingVelocityProperty

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
                        CustomVariableHelper.GetInterpolationCharacteristic(customVariable, GlueState.Self.CurrentElement);
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

        #endregion

        #region Type

        private static async Task HandleChangeVariableType(CustomVariable customVariable, object oldValue)
        {
            var currentValue = customVariable.DefaultValue;
            var oldType = (string)oldValue;

            List<CustomVariable> derivedVariablesToUpdate = null;

            HashSet<GlueElement> elementsToRefresh = new HashSet<GlueElement>();

            var shouldProceed = true;

            if (customVariable.SetByDerived)
            {
                var variableContainer = ObjectFinder.Self.GetElementContaining(customVariable);

                List<GlueElement> derivedList = null;

                if (variableContainer != null)
                {
                    derivedList = ObjectFinder.Self.GetAllDerivedElementsRecursive(variableContainer);
                }

                if (derivedList?.Count > 0)
                {
                    derivedVariablesToUpdate = new List<CustomVariable>();
                    foreach (var derived in derivedList)
                    {
                        var variableInDerived = derived.GetCustomVariable(customVariable.Name);

                        if (variableInDerived?.DefinedByBase == true)
                        {
                            derivedVariablesToUpdate.Add(variableInDerived);
                            elementsToRefresh.Add(derived);
                        }
                    }
                }

                if (derivedVariablesToUpdate != null)
                {
                    string message = "Changing this type will also change the type on the following variables. Would you like to continue?\n\n";

                    foreach (var item in derivedVariablesToUpdate)
                    {
                        message += item.ToString() + "\n";
                    }

                    var result = GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(message);

                    shouldProceed = result == System.Windows.MessageBoxResult.Yes;
                }

            }

            if (shouldProceed)
            {
                await TaskManager.Self.AddAsync(() =>
                {
                    ConvertVariableValueToCurrentType(customVariable, currentValue, oldType);

                    if(derivedVariablesToUpdate != null)
                    {
                        foreach(var derived in derivedVariablesToUpdate)
                        {
                            var oldDerivedValue = derived.DefaultValue;
                            var oldDerivedType = derived.Type;
                            derived.Type = customVariable.Type;
                            ConvertVariableValueToCurrentType(derived, oldDerivedValue, oldDerivedType);
                        }
                    }
                }, "Converting variables according to type change");

                foreach(var element in elementsToRefresh)
                {
                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element, Plugins.ExportedInterfaces.CommandInterfaces.TreeNodeRefreshType.CustomVariables);
                }
            }
            else
            {
                customVariable.Type = oldType;
            }


            // If the type changed, the Property Grid needs to be re-made so that the new
            // grid will have the right type for the DefaultValue cell:
            GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();
        }

        private static void ConvertVariableValueToCurrentType(CustomVariable customVariable, object currentValue, string oldType)
        {
            // This could migrate to the TypeManager but I haven't yet because the cod e here uses both old type and new type strings, which
            // the TypeManager doesn't yet handle. But we should unify that code
            bool wasAbleToConvert = false;

            if (currentValue != null)
            {
                if (oldType == "int")
                {
                    var valueAsInt = (int)currentValue;

                    wasAbleToConvert = TypeManager.TryCastValue(customVariable.Type, valueAsInt, out object convertedValue);

                    switch (customVariable.Type)
                    {
                        case "float":
                        case "float?":
                            customVariable.DefaultValue = (float)valueAsInt;
                            wasAbleToConvert = true;
                            break;
                        case "decimal":
                        case "decimal?":
                            customVariable.DefaultValue = (decimal)valueAsInt;
                            wasAbleToConvert = true;
                            break;
                        case "double":
                            customVariable.DefaultValue = (double)valueAsInt;
                            wasAbleToConvert = true;
                            break;
                        case "string":
                            customVariable.DefaultValue = valueAsInt.ToString();
                            break;
                    }
                }
                else if(oldType == "decimal")
                {
                    var valueAsDecimal = (decimal)currentValue;

                    switch (customVariable.Type)
                    {
                        case "float":
                        case "float?":
                            customVariable.DefaultValue = (float)valueAsDecimal;
                            wasAbleToConvert = true;
                            break;
                        case "int":
                        case "int?":
                            customVariable.DefaultValue = (int)valueAsDecimal;
                            wasAbleToConvert = true;
                            break;
                        case "decimal":
                        case "decimal?":
                            customVariable.DefaultValue = (decimal)valueAsDecimal;
                            wasAbleToConvert = true;
                            break;
                        case "double":
                            customVariable.DefaultValue = (double)valueAsDecimal;
                            wasAbleToConvert = true;
                            break;
                        case "string":
                            customVariable.DefaultValue = valueAsDecimal.ToString();
                            wasAbleToConvert = true;
                            break;
                    }

                }
                else if (oldType == "float")
                {
                    var valueAsFloat = (float)currentValue;

                    switch (customVariable.Type)
                    {
                        case "float?":
                            // is this necessary?
                            customVariable.DefaultValue = (float?)valueAsFloat;
                            break;
                        case "decimal":
                        case "decimal?":
                            customVariable.DefaultValue = (decimal)valueAsFloat;
                            wasAbleToConvert = true;
                            break;
                        case "int":
                            customVariable.DefaultValue = (int)valueAsFloat;
                            wasAbleToConvert = true;
                            break;
                        case "double":
                            customVariable.DefaultValue = (double)valueAsFloat;
                            wasAbleToConvert = true;
                            break;
                        case "string":
                            customVariable.DefaultValue = valueAsFloat.ToString();
                            wasAbleToConvert = true;
                            break;
                    }
                }
                else if (oldType == "double")
                {
                    var valueAsDouble = (double)currentValue;

                    switch (customVariable.Type)
                    {
                        case "int":
                            customVariable.DefaultValue = (int)valueAsDouble;
                            wasAbleToConvert = true;
                            break;
                        case "float":
                        case "float?":
                            customVariable.DefaultValue = (float)valueAsDouble;
                            wasAbleToConvert = true;
                            break;
                        case "decimal":
                        case "decimal?":
                            customVariable.DefaultValue = (decimal)valueAsDouble;
                            wasAbleToConvert = true;
                            break;
                        case "string":
                            customVariable.DefaultValue = valueAsDouble.ToString();
                            wasAbleToConvert = true;
                            break;
                    }
                }
                else if (oldType == "string")
                {
                    var valueAsString = (string)currentValue;

                    switch (customVariable.Type)
                    {
                        case "int":
                            {
                                if (int.TryParse(valueAsString, out int result))
                                {
                                    customVariable.DefaultValue = result;
                                }
                            }
                            break;
                        case "float":
                            {
                                if (float.TryParse(valueAsString, out float result))
                                {
                                    customVariable.DefaultValue = result;
                                }
                            }
                            break;
                        case "decimal":
                            {
                                if (decimal.TryParse(valueAsString, out decimal result))
                                {
                                    customVariable.DefaultValue = result;
                                }
                            }
                            break;
                        case "double":
                            {
                                if (double.TryParse(valueAsString, out double result))
                                {
                                    customVariable.DefaultValue = result;
                                }
                            }
                            break;
                    }
                }
            }

            if (wasAbleToConvert == false)
            {
                customVariable.SetDefaultValueAccordingToType(customVariable.Type);
            }
        }

        #endregion

        #region Category

        private void HandleChangedCategory(CustomVariable customVariable)
        {
            // usually this will be the current variable, but not always
            var element = ObjectFinder.Self.GetElementContaining(customVariable);
            var shouldPropagateToChildren = customVariable.SetByDerived;
            //////////////// Early Out ////////////////
            if(element == null)
            {
                return;
            }
            if(shouldPropagateToChildren == false)
            {
                return;
            }
            //////////////End Early Out////////////////

            var allDerived = ObjectFinder.Self.GetAllElementsThatInheritFrom(element);

            foreach(var derived in allDerived)
            {
                var matchingVariable = derived.CustomVariables.FirstOrDefault(item => item.Name == customVariable.Name);

                if(matchingVariable != null)
                {
                    matchingVariable.Category = customVariable.Category;
                }
            }

        }

        #endregion
    }
}
