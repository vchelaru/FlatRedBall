using System.Collections.Generic;
using System.Text;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.IO;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Instructions;
using FlatRedBall.Glue.SaveClasses.Helpers;
using System;
using System.Linq;
using FlatRedBall.Glue.Elements;

namespace FlatRedBall.Glue.CodeGeneration
{
    public partial class StateCodeGenerator : ElementComponentCodeGenerator
    {
        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, SaveClasses.IElement element)
        {


            var currentBlock = codeBlock;

            if (element.HasStates)
            {
                List<StateSave> statesForThisCategory = GetSharedVariableStates(element);

                const string enumName = "VariableState";

                currentBlock = AppendEnum(currentBlock, statesForThisCategory, enumName, element);
                GenerateCurrentStateProperty(element, codeBlock, "VariableState", statesForThisCategory);

                //Build State Categories
                var stateCategories = GetAllStateCategoryNames(element, false);

                foreach (var stateCategory in stateCategories)
                {
                    var states = GetAllStatesForCategory(element, stateCategory);

                    AppendEnum(currentBlock, states, stateCategory, element);
                    GenerateCurrentStateProperty(element, codeBlock, stateCategory, states);
                }
            }

            return codeBlock;
        }

        public static List<StateSave> GetAllStatesForCategory(IElement element, string stateCategory)
        {
            var states = new List<StateSave>();

            if (!string.IsNullOrEmpty(element.BaseElement))
            {
                IElement baseElement = ObjectFinder.Self.GetIElement(element.BaseElement);

                if (baseElement != null)
                {
                    states.AddRange(GetAllStatesForCategory(baseElement, stateCategory));
                }
            }

            var category =
                element.StateCategoryList.Find(
                    cat => cat.Name == stateCategory && !cat.SharesVariablesWithOtherCategories);

            if(category != null)
                states.AddRange(category.States);

            return states;
        }

        private static IEnumerable<string> GetAllStateCategoryNames(IElement element, bool includeInheritance)
        {
            var names = new Dictionary<string, string>();
            if (element != null)
            {
                if (!string.IsNullOrEmpty(element.BaseElement) && includeInheritance)
                {
                    foreach (var name in GetAllStateCategoryNames(ObjectFinder.Self.GetIElement(element.BaseElement), includeInheritance).Where(name => !names.ContainsKey(name)))
                    {
                        names.Add(name, name);
                    }
                }

                foreach (var category in element.StateCategoryList.Where(category => !category.SharesVariablesWithOtherCategories && !names.ContainsKey(category.Name)))
                {
                    names.Add(category.Name, category.Name);
                }
            }
            return new List<string>(names.Keys);
        }

        public static List<StateSave> GetSharedVariableStates(SaveClasses.IElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element argument cannot be null");
            }

            var statesForThisCategory = new List<StateSave>();

            var currentElement = element;

            //Get states for parent entities
            if (!string.IsNullOrEmpty(currentElement.BaseElement))
            {
                IElement baseElement = ObjectFinder.Self.GetIElement(currentElement.BaseElement);
                if (baseElement != null)
                {
                    statesForThisCategory.AddRange(GetSharedVariableStates(baseElement));
                }
            }

            
            for (int i = 0; i < element.States.Count; i++)
            {
                statesForThisCategory.Add(element.States[i]);
            }

            statesForThisCategory.AddRange(element.StateCategoryList.Where(category => category.SharesVariablesWithOtherCategories).SelectMany(category => category.States));
            return statesForThisCategory;
        }

        private static ICodeBlock AppendEnum(ICodeBlock currentBlock, List<StateSave> statesForThisCategory, string enumName, IElement element)
        {
            if (statesForThisCategory.Count != 0)
            {
                string prefix = "public";

                if (ShouldUseNewKeyword(element, enumName))
                {
                    prefix += " new";
                }

                currentBlock = currentBlock
                    .Enum(prefix, enumName)
                        .Line("Uninitialized = 0, //This exists so that the first set call actually does something")
                        .Line("Unknown = 1, //This exists so that if the entity is actually a child entity and has set a child state, you will get this");


                for (int i = 0; i < statesForThisCategory.Count; i++)
                {
                    string whatToAppend = "";

                    if (i != statesForThisCategory.Count - 1)
                    {
                        whatToAppend += ", ";
                    }

                    currentBlock.Line(statesForThisCategory[i].Name + " = " + (i + 2) + whatToAppend);
                }
                currentBlock = currentBlock.End();
            }
            return currentBlock;
        }

        private static bool ShouldUseNewKeyword(IElement element, string enumName)
        {
            bool toReturn = false;
            if (string.IsNullOrEmpty(element.BaseElement))
            {
                toReturn = false;
            }
            else
            {
                IElement baseElement = ObjectFinder.Self.GetIElement(element.BaseElement);

                if (baseElement != null)
                {
                    toReturn = baseElement.DefinesCategoryEnumRecursive(enumName);
                }
            }
            return toReturn;
        }

        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, SaveClasses.IElement element)
        {
            //throw new NotImplementedException();
            return codeBlock;
        }

        public override ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, SaveClasses.IElement element)
        {
            //throw new NotImplementedException();
            return codeBlock;
        }

        public override ICodeBlock GenerateDestroy(ICodeBlock codeBlock, SaveClasses.IElement element)
        {
            //throw new NotImplementedException();
            return codeBlock;
        }

        public override ICodeBlock GenerateActivity(ICodeBlock codeBlock, SaveClasses.IElement element)
        {
            //throw new NotImplementedException();
            return codeBlock;
        }

        public override ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, SaveClasses.IElement element)
        {
            if (element.HasStates)
            {

                List<StateSave> sharedVariableStates = GetSharedVariableStates(element);

                if (sharedVariableStates.Count != 0)
                {
                    #region Create the static state that should be set prior to loading

                    string propertyPrefix = "public static ";

                    if (!string.IsNullOrEmpty(element.BaseElement))
                    {
                        IElement baseElement = ObjectFinder.Self.GetIElement(element.BaseElement);
                        if (baseElement != null && baseElement.DefinesCategoryEnumRecursive("VariableState"))
                        {
                            propertyPrefix += "new ";
                        }
                    }

                    codeBlock
                        .Line("static VariableState mLoadingState = VariableState.Uninitialized;")
                        .Property(propertyPrefix + "VariableState", "LoadingState")
                            .Get()
                                .Line("return mLoadingState;")
                            .End()
                            .Set()
                                .Line("mLoadingState = value;")
                            .End()
                        .End();

                    #endregion
                }




                GenerateInterpolationAdditionalMethods(codeBlock, element, sharedVariableStates);

                GeneratePreloadStateContent(codeBlock, element, sharedVariableStates);
            }

            return codeBlock;
        }

        private void GeneratePreloadStateContent(ICodeBlock codeBlock, IElement element, List<StateSave> sharedVariableStates)
        {
            // For now we'll only support Entities because they have a ContentManagerName variable.  Screen content can't be shared
            // between Screens because of the hard-coded content manager name.  We'll have to revisit that if we need to share content
            // between screens programatically.
            if (element is EntitySave)
            {
                // This method is used to preload content for certain states.  States may use content that is
                // LoadedOnlyWhenReferenced.  If so, that means that the content will be loaded on first access
                // post-initialize.  This method will allow users to load this content on the async thread prior
                // to Initialize finishing without having to worry about which content belongs in which state.

                List<StateSave> list = sharedVariableStates;

                codeBlock = GeneratePreloadStateContentForStateType(codeBlock, element, list, "VariableState");

                foreach (StateSaveCategory category in element.StateCategoryList.Where((category) => category.SharesVariablesWithOtherCategories == false))
                {
                    codeBlock = GeneratePreloadStateContentForStateType(codeBlock, element, category.States, category.Name);
                }
            }
        }

        private static ICodeBlock GeneratePreloadStateContentForStateType(ICodeBlock codeBlock, IElement element, List<StateSave> list, string variableType)
        {
            if (list.Count != 0)
            {

                codeBlock = codeBlock.Function("public static void", "PreloadStateContent", variableType + " state, string contentManagerName");
                codeBlock.Line("ContentManagerName = contentManagerName;");

                codeBlock = codeBlock.Switch("state");

                // Loop through states here and access properties that need the values
                foreach (StateSave state in list)
                {
                    codeBlock = codeBlock.Case(variableType + "." + state.Name);
                    foreach (InstructionSave instruction in state.InstructionSaves)
                    {
                        if (instruction.Value != null && instruction.Value is string)
                        {
                            // We insert a block so that object throwaway is not redefined in the switch scope.
                            // We do this instead of making an object throwaway above the switch so that we don't
                            // get warnings if is nothing to load
                            codeBlock.Block().Line("object throwaway = " + GetRightSideAssignmentValueAsString(element, instruction) + ";");
                        }
                    }
                    codeBlock = codeBlock.End();

                }

                codeBlock = codeBlock.End();

                codeBlock = codeBlock.End();
            }
            return codeBlock;
        }

        public override ICodeBlock GenerateLoadStaticContent(ICodeBlock codeBlock, IElement element)
        {
            return codeBlock;
        }


        private static ICodeBlock GenerateCurrentStateProperty(IElement element, ICodeBlock codeBlock, string enumType, List<StateSave> states)
        {
            var createField = false;
            if (enumType == "VariableState" && !DoesBaseHaveUncategorizedStates(element))  //Uncategorized and not base
            {
                createField = true;
            }
            else if (enumType != "VariableState")    //Check if this state category exists in a parent entity
            {
                if (element.BaseElement != null)
                {
                    var categories = GetAllStateCategoryNames(ObjectFinder.Self.GetIElement(element.BaseElement), true);

                    if (!categories.Any(category => category == enumType))
                    {
                        createField = true;
                    }
                }else
                {
                    createField = true;
                }
            }

            string variableNameModifier = enumType;
            if (enumType == "VariableState")
            {
                variableNameModifier = "";
            }

            string qualifiedEnumType = element.Name.Replace("\\", ".").Replace("/", ".")  +  "." + enumType;

            if (states.Count != 0)
            {
                string variableToLookFor = "Current" + variableNameModifier + "State";
                CustomVariable customVariable = element.GetCustomVariable(variableToLookFor);
                bool hasEvent = customVariable != null && customVariable.CreatesEvent;


                #region Header and Getter stuff - simple stuff with no logic

                if (createField)
                {
                    codeBlock
                        .Line(string.Format("protected int mCurrent{0}State = 0;", variableNameModifier));
                }

                string publicWithOptionalNew = "public";
                if (ShouldUseNewKeyword(element, enumType))
                {
                    publicWithOptionalNew += " new";
                }
                var setBlock = codeBlock
                    .Property(publicWithOptionalNew + " " + qualifiedEnumType, "Current" + variableNameModifier + "State")
                        .Get()
                            .If(string.Format("System.Enum.IsDefined(typeof({0}), mCurrent{1}State)", enumType, variableNameModifier))
                                .Line(string.Format("return ({0})mCurrent{1}State;", enumType, variableNameModifier))
                            .End()
                            .Else()
                                .Line(string.Format("return {0}.Unknown;", enumType))
                            .End()
                        .End()
                        .Set();

                #endregion

                #region Set the state value and call an event if necessary

                bool stillNeedsToAssignValue = true;

                if (element is EntitySave)
                {
                    EntitySave asEntitySave = element as EntitySave;

                    //if (!string.IsNullOrEmpty(asEntitySave.CurrentStateChange))
                    //{
                    //    setBlock
                    //        .If("value != mCurrent" + variableNameModifier + "State")
                    //            .Line("mCurrent" + variableNameModifier + "State = value;")
                    //            .Line(asEntitySave.CurrentStateChange + "(this, null);");

                    //    stillNeedsToAssignValue = false;
                    //}
                }

                if (hasEvent)
                {
                    EventCodeGenerator.GenerateEventRaisingCode(setBlock, BeforeOrAfter.Before, variableToLookFor, element);
                }

                if (stillNeedsToAssignValue)
                {
                    setBlock.Line("mCurrent" + variableNameModifier + "State = (int)value;");
                }

                #endregion

                var switchBlock = setBlock.Switch("Current" + variableNameModifier + "State");

                switchBlock.Case(enumType + ".Uninitialized");
                switchBlock.Case(enumType + ".Unknown");

                foreach (StateSave stateSave in states)
                {
                    GenerateCurrentStateCodeForIndividualState(element, switchBlock, stateSave, enumType);
                }

                if ((enumType == "VariableState" && DoesBaseHaveUncategorizedStates(element)) || 
                    (!string.IsNullOrEmpty(element.BaseElement) && GetAllStateCategoryNames(ObjectFinder.Self.GetIElement(element.BaseElement), true).Any(category => category == enumType)))
                {
                    switchBlock.Default()
                        .Line("base.Current" + variableNameModifier + "State = base.Current" + variableNameModifier + "State;");
                }

                if (hasEvent)
                {
                    EventCodeGenerator.GenerateEventRaisingCode(setBlock, BeforeOrAfter.After, variableToLookFor, element);
                }
            }
            return codeBlock;
        }

        private static bool DoesBaseHaveUncategorizedStates(IElement element)
        {
            if (string.IsNullOrEmpty(element.BaseElement) || element.InheritsFromFrbType())
            {
                return false;
            }
            else
            {
                IElement baseElement = ObjectFinder.Self.GetIElement(element.BaseElement);

                if (baseElement != null)
                {

                    if (baseElement.States.Count != 0)
                    {
                        return true;
                    }
                    else
                    {
                        foreach (StateSaveCategory category in baseElement.StateCategoryList)
                        {
                            if (category.SharesVariablesWithOtherCategories)
                            {
                                return true;
                            }
                        }
                    }

                }
                return false;

            }

        }

        private static ICodeBlock GenerateCurrentStateCodeForIndividualState(IElement element, ICodeBlock codeBlock, StateSave stateSave, string enumType)
        {
            var curBlock = codeBlock.Case(enumType + "." + stateSave.Name);
            bool doesStateAssignAbsoluteValues = GetDoesStateAssignAbsoluteValues(stateSave, element);

            foreach (InstructionSave instruction in stateSave.InstructionSaves)
            {
                if (instruction.Value != null)
                {
                    // Get the valueAsString, which is the right-side of the equals sign
                    string rightSideOfEquals = GetRightSideAssignmentValueAsString(element, instruction);
                    
                    if (!string.IsNullOrEmpty(rightSideOfEquals))
                    {
                        CustomVariable customVariable = element.GetCustomVariableRecursively(instruction.Member);
                        NamedObjectSave referencedNos = element.GetNamedObjectRecursively(customVariable.SourceObject);

                        if (referencedNos != null)
                        {
                            NamedObjectSaveCodeGenerator.AddIfConditionalSymbolIfNecesssary(curBlock, referencedNos);
                        }

                        string leftSideOfEquals = GetLeftSideOfEquals(element, customVariable, instruction, false);
                        string leftSideOfEqualsWithRelative = GetLeftSideOfEquals(element, customVariable, instruction, true);




                        if (leftSideOfEquals != leftSideOfEqualsWithRelative)
                        {
                            string objectWithParent = null;

                            if (string.IsNullOrEmpty(customVariable.SourceObject))
                            {
                                objectWithParent = "this";
                            }
                            else
                            {
                                objectWithParent = customVariable.SourceObject;
                            }

                            curBlock
                                .If(objectWithParent + ".Parent == null")
                                    .Line(leftSideOfEquals + " = " + rightSideOfEquals + ";")
                                .End()

                                .Else()
                                    .Line(leftSideOfEqualsWithRelative + " = " + rightSideOfEquals + ";");
                        }
                        else
                        {
                            curBlock.Line(leftSideOfEquals + " = " + rightSideOfEquals + ";");
                        }

                        if (referencedNos != null)
                        {
                            NamedObjectSaveCodeGenerator.AddEndIfIfNecessary(curBlock, referencedNos);
                        }

                    }
                }
            }

            return codeBlock;
        }

        private static string GetLeftSideOfEquals(IElement element, CustomVariable customVariable, InstructionSave instruction, bool switchToRelative)
        {
            string leftSideOfEquals = instruction.Member;

            if (switchToRelative && customVariable != null)
            {
                string possibleLeftSide = RelativeValueForInstruction(instruction, customVariable, element);
                if (!string.IsNullOrEmpty(possibleLeftSide))
                {
                    
                    leftSideOfEquals = possibleLeftSide;
                    if (!string.IsNullOrEmpty(customVariable.SourceObject))
                    {
                        leftSideOfEquals = customVariable.SourceObject + "." + leftSideOfEquals;
                    }
                }
            }

            return leftSideOfEquals;
        }

        private static bool GetDoesStateAssignAbsoluteValues(StateSave stateSave, IElement element)
        {
            bool returnValue = false;
            foreach (InstructionSave instruction in stateSave.InstructionSaves)
            {
                CustomVariable customVariable = element.GetCustomVariableRecursively(instruction.Member);

                if (customVariable != null)
                {
                    returnValue |= !string.IsNullOrEmpty(RelativeValueForInstruction(instruction, customVariable, element));
                }
            }

            return returnValue;
        }

        private static string RelativeValueForInstruction(InstructionSave instruction, CustomVariable customVariable, IElement element)
        {
            if (!string.IsNullOrEmpty(customVariable.SourceObject))
            {
                string relativeMember = InstructionManager.GetRelativeForAbsolute(customVariable.SourceObjectProperty);

                return relativeMember;
            }
            else
            {

                string relativeMember = null;

                if (element is EntitySave)
                {
                    relativeMember = InstructionManager.GetRelativeForAbsolute(instruction.Member);
                }
                return relativeMember;
            }

        }

        public static string GetRightSideAssignmentValueAsString(IElement element, InstructionSave instruction)
        {
            CustomVariable customVariable = element.GetCustomVariableRecursively(instruction.Member);

            IElement referencedElement = null;

            #region Determine if the assignment is a file

            bool isFile = false;

            if (customVariable != null)
            {
                referencedElement =
                    BaseElementTreeNode.GetElementIfCustomVariableIsVariableState(customVariable, element);

                isFile = customVariable.GetIsFile();

            }

            #endregion

            string valueAsString = "";

            if (referencedElement == null)
            {

                valueAsString = CodeParser.ParseObjectValue(instruction.Value);

                if (isFile)
                {
                    valueAsString = valueAsString.Replace("\"", "");

                    if (valueAsString == "<NONE>")
                    {
                        valueAsString = "null";
                    }
                }
                else if (CustomVariableCodeGenerator.ShouldAssignToCsv(customVariable, valueAsString))
                {
                    valueAsString = CustomVariableCodeGenerator.GetAssignmentToCsvItem(customVariable, element, valueAsString);
                }
                else if (customVariable != null && customVariable.Type == "Color")
                {
                    valueAsString = "Color." + valueAsString.Replace("\"", "");
                }
                if (customVariable != null && !string.IsNullOrEmpty(customVariable.SourceObject) && !isFile)
                {
                    NamedObjectSave namedObject = element.GetNamedObjectRecursively(customVariable.SourceObject);

                    bool isVariableState = customVariable.GetIsVariableState();

                    IElement objectElement = null;

                    if (namedObject != null)
                    {
                        ObjectFinder.Self.GetIElement(namedObject.SourceClassType);
                    }

                    if (objectElement != null)
                    {
                        if (isVariableState)
                        {
                            string typeName = "VariableState";

                            StateSaveCategory category = objectElement.GetStateCategoryRecursively(customVariable.Type);

                            if(category != null && category.SharesVariablesWithOtherCategories == false)
                            {
                                typeName = category.Name;
                            }
                            valueAsString = objectElement.Name.Replace("/", ".").Replace("\\", ".") + "." + typeName + "." + valueAsString.Replace("\"", "");
                        }
                    }

                    valueAsString = CodeWriter.MakeLocalizedIfNecessary(
                        namedObject,
                        instruction.Member,
                        instruction.Value,
                        valueAsString,
                        customVariable);
                }
            }
            else
            {
                string enumValue = (string)instruction.Value;

                if (!string.IsNullOrEmpty(enumValue))
                {
                    string variableType = "VariableState";

                    if (customVariable != null && customVariable.Type.ToLower() != "string")
                    {
                        variableType = customVariable.Type;
                    }
                    valueAsString = FullyQualifyStateValue(referencedElement, enumValue, variableType);
                }

            }
            return valueAsString;
        }

        public static string FullyQualifyStateValue(IElement referencedElement, string enumValue, string variableType)
        {

            string valueAsString =
                ProjectManager.ProjectNamespace + "." + referencedElement.Name.Replace("\\", ".") + "." + variableType + "." + enumValue;
            return valueAsString;
        }

        public static string FullyQualifiedDefaultStateValue(IElement referencedElement, string variableType)
        {
            string stateName = null;
            if (variableType == "VariableState")
            {
                if (referencedElement.States.Count != 0)
                {
                    stateName = referencedElement.States[0].Name;
                }
                else
                {
                    foreach (StateSaveCategory category in referencedElement.StateCategoryList.Where((category) => category.SharesVariablesWithOtherCategories == true && category.States.Count != 0))
                    {
                        stateName = category.States[0].Name;
                        break;
                    }
                }
            }
            else
            {
                StateSaveCategory category = referencedElement.GetStateCategoryRecursively(variableType);
                if (category != null && category.States.Count != 0)
                {
                    stateName = category.States[0].Name;
                }
            }

            if (stateName != null)
            {
                return ProjectManager.ProjectNamespace + "." + referencedElement.Name.Replace("\\", ".") + "." + variableType + "." + stateName;
            }
            else
            {
                return null;
            }
        }

        public static void WriteSetStateOnNamedObject(NamedObjectSave namedObject, ICodeBlock codeBlock)
        {
            if (!string.IsNullOrEmpty(namedObject.CurrentState))
            {
                IElement referencedElement = namedObject.GetReferencedElement();
                if (referencedElement != null && referencedElement.GetUncategorizedStatesRecursively().Count != 0)
                {
                    string qualifiedName = NamedObjectSaveCodeGenerator.GetQualifiedTypeName(namedObject);

                    string lineToAdd = namedObject.FieldName + ".CurrentState = " + qualifiedName + ".VariableState." + namedObject.CurrentState + ";";
                    codeBlock.Line(lineToAdd);
                }

            }


        }
    }
}
