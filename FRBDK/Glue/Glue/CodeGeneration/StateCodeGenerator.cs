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

        #region Generating Fields / Inner Classes



        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, SaveClasses.IElement element)
        {


            var currentBlock = codeBlock;

            if (element.HasStates)
            {
                List<StateSave> statesForThisCategory = GetSharedVariableStates(element);


                currentBlock = CreateClassForStateCategory(currentBlock, statesForThisCategory, null, element);
                GenerateCurrentStateProperty(element, codeBlock, null, statesForThisCategory);

                //Build State Categories
                var stateCategories = GetAllStateCategoryNames(element, false);

                foreach (var kvp in stateCategories)
                {
                    var states = GetAllStatesForCategory(element, kvp.Key);

                    var category = kvp.Value;
                    CreateClassForStateCategory(currentBlock, states, category, element);
                    GenerateCurrentStateProperty(element, codeBlock, category, states);
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

        private static Dictionary<string, StateSaveCategory> GetAllStateCategoryNames(IElement element, bool includeInheritance)
        {
            var names = new Dictionary<string, StateSaveCategory>();
            if (element != null)
            {
                if (!string.IsNullOrEmpty(element.BaseElement) && includeInheritance)
                {
                    var uncontained = GetAllStateCategoryNames(ObjectFinder.Self.GetIElement(element.BaseElement), includeInheritance).Where(name => !names.ContainsKey(name.Key));

                    foreach (var kvp in uncontained)
                    {
                        names.Add(kvp.Key, kvp.Value);
                    }
                }

                foreach (var category in element.StateCategoryList.Where(category => !category.SharesVariablesWithOtherCategories && !names.ContainsKey(category.Name)))
                {
                    names.Add(category.Name, category);
                }
            }
            return names;
        }

        private static ICodeBlock CreateClassForStateCategory(ICodeBlock currentBlock, List<StateSave> statesForThisCategory, StateSaveCategory category, IElement element)
        {
            string categoryClassName = category?.Name ?? "VariableState";

            if (statesForThisCategory.Count != 0)
            {
                string prefix = "public";

                string postfix = null;
                if (IsStateDefinedInBase(element, categoryClassName))
                {
                    postfix = $" : {element.BaseElement.Replace("\\", ".")}.{categoryClassName}";
                }


                currentBlock = currentBlock.Class(prefix, categoryClassName, postfix);

                currentBlock.Line($"public string Name;");

                var includedVariables = element.CustomVariables.Where(item => category?.ExcludedVariables.Contains(item.Name) == false)
                    .ToArray();

                foreach (var variable in includedVariables)
                {
                    string type = variable.Type;

                    if (variable.GetIsFile())
                    {
                        type = "string";
                    }
                    else
                    {
                        type = CustomVariableCodeGenerator.GetMemberTypeFor(variable, element);
                    }
                    currentBlock.Line($"public {type} {variable.Name};");
                }



                for (int i = 0; i < statesForThisCategory.Count; i++)
                {
                    var state = statesForThisCategory[i];
                    currentBlock.Line($"public static {categoryClassName} {state.Name} = new {categoryClassName}()");

                    var variableBlock = currentBlock.Block();

                    variableBlock.Line($"Name = \"{state.Name}\",");

                    foreach(var variable in includedVariables)
                    {
                        var instruction = state.InstructionSaves.FirstOrDefault(item => item.Member == variable.Name);

                        var valueToSet = state.InstructionSaves.FirstOrDefault(item => item.Member == variable.Name)?.Value
                            ?? variable.DefaultValue;
                        if(valueToSet != null)
                        {
                            var rightSide = GetRightSideAssignmentValueAsString(element, variable.Name, valueToSet);
                            var matchingVariable = element.GetCustomVariableRecursively(variable.Name);
                            if(matchingVariable?.GetIsFile() == true)
                            {
                                // If it's a file we are only going to reference the file name here as to not preload the file
                                rightSide = $"\"{rightSide}\"";
                            }

                            variableBlock.Line($"{variable.Name} = {rightSide},");
                        }
                    }
                    variableBlock.End().Line(";");


                }

                currentBlock.Line($"public static Dictionary<string, {categoryClassName}> AllStates = new Dictionary<string, {categoryClassName}>");
                var dictionaryBlock = currentBlock.Block();
                dictionaryBlock.PostCodeLines.Add(new CodeLine(";"));

                for (int i = 0; i < statesForThisCategory.Count; i++)
                {
                    var state = statesForThisCategory[i];
                    dictionaryBlock.Line("{\"" + state.Name + "\", " + state.Name + "},");
                }

                currentBlock = currentBlock.End();
            }
            return currentBlock;
        }

        private static ICodeBlock GenerateCurrentStateProperty(IElement element, ICodeBlock codeBlock, StateSaveCategory category, List<StateSave> states)
        {
            string enumType = category?.Name ?? "VariableState";
            // early out
            if (states.Count == 0)
            {
                return codeBlock;
            }

            string variableNameModifier = enumType;
            if (enumType == "VariableState")
            {
                variableNameModifier = "";
            }

            string qualifiedEnumType = element.Name.Replace("\\", ".").Replace("/", ".") + "." + enumType;

            string variableToLookFor = "Current" + variableNameModifier + "State";
            CustomVariable customVariable = element.GetCustomVariable(variableToLookFor);
            bool hasEvent = customVariable != null && customVariable.CreatesEvent;


            #region Header and Getter stuff - simple stuff with no logic


            codeBlock
                .Line($"private {enumType} mCurrent{variableNameModifier}State = null;");

            string publicWithOptionalNew = "public";
            if (IsStateDefinedInBase(element, enumType))
            {
                publicWithOptionalNew += " new";
            }

            // This is inclusive (-1), but includes 0 and 1 values (+2), which means the net is +1
            int maxIntValueInclusive = states.Count + 1;

            var setBlock = codeBlock
                .Property(publicWithOptionalNew + " " + qualifiedEnumType, "Current" + variableNameModifier + "State")
                    .Get()
                        .Line(string.Format("return mCurrent{1}State;", enumType, variableNameModifier))
                    .End()
                    .Set();

            #endregion

            #region Set the state value and call an event if necessary

            bool stillNeedsToAssignValue = true;

            if (hasEvent)
            {
                EventCodeGenerator.GenerateEventRaisingCode(setBlock, BeforeOrAfter.Before, variableToLookFor, element);
            }

            if (stillNeedsToAssignValue)
            {
                setBlock.Line("mCurrent" + variableNameModifier + "State = value;");
            }

            #endregion

            bool isElse = false;
            foreach (StateSave stateSave in states)
            {
                GenerateVariableAssignmentForState(element, setBlock, stateSave, category, isElse);
                isElse = true;
            }

            if ((enumType == "VariableState" && DoesBaseHaveUncategorizedStates(element)) ||
                (!string.IsNullOrEmpty(element.BaseElement) && GetAllStateCategoryNames(ObjectFinder.Self.GetIElement(element.BaseElement), true).Any(category => category.Key == enumType)))
            {
                setBlock.Else()
                    .Line("base.Current" + variableNameModifier + "State = base.Current" + variableNameModifier + "State;");
            }

            if (hasEvent)
            {
                EventCodeGenerator.GenerateEventRaisingCode(setBlock, BeforeOrAfter.After, variableToLookFor, element);
            }

            return codeBlock;
        }

        private static bool IsStateDefinedInBase(IElement element, string enumName)
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

        private static ICodeBlock GenerateVariableAssignmentForState(IElement element, ICodeBlock codeBlock, StateSave stateSave, StateSaveCategory category, bool isElse)
        {
            string enumType = category?.Name ?? "VariableState";

            string variableNameModifier = enumType;
            if (enumType == "VariableState")
            {
                variableNameModifier = "";
            }

            ICodeBlock curBlock;

            if(isElse)
            {
                curBlock = codeBlock.ElseIf($"Current{variableNameModifier}State == {enumType}.{stateSave.Name}");
            }
            else
            {
                curBlock = codeBlock.If($"Current{variableNameModifier}State == {enumType}.{stateSave.Name}");

            }
            bool doesStateAssignAbsoluteValues = GetDoesStateAssignAbsoluteValues(stateSave, element);

            foreach(var variable in element.CustomVariables)
            {
                var shouldInclude = false;
                if(category != null)
                {
                    if(category.ExcludedVariables.Contains(variable.Name))
                    {
                        shouldInclude = false;
                    }
                    else if(variable.Type == category.Name)
                    {
                        shouldInclude = false;
                    }
                    else
                    {
                        shouldInclude = true;
                    }
                }
                else if(category == null)
                {
                    shouldInclude = stateSave.InstructionSaves.Any(item => item.Member == variable.Name);
                }

                if(shouldInclude)
                {

                    string member = variable.Name;
                    var value = stateSave.InstructionSaves.FirstOrDefault(item => item.Member == variable.Name)?.Value ?? variable.DefaultValue;

                    if (value != null)
                    {
                        // Get the valueAsString, which is the right-side of the equals sign
                        string rightSideOfEquals = GetRightSideAssignmentValueAsString(element, member, value);

                        if (!string.IsNullOrEmpty(rightSideOfEquals))
                        {
                            CustomVariable customVariable = element.GetCustomVariableRecursively(member);
                            NamedObjectSave referencedNos = element.GetNamedObjectRecursively(customVariable.SourceObject);

                            if (referencedNos != null)
                            {
                                NamedObjectSaveCodeGenerator.AddIfConditionalSymbolIfNecesssary(curBlock, referencedNos);
                            }

                            string leftSideOfEquals = GetLeftSideOfEquals(element, customVariable, member, false);
                            string leftSideOfEqualsWithRelative = GetLeftSideOfEquals(element, customVariable, member, true);




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
            }


            return codeBlock;
        }

        #endregion


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
                        .Line("static VariableState mLoadingState = null;")
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

                //codeBlock = codeBlock.Switch("state");
                bool isElse = false;
                // Loop through states here and access properties that need the values
                foreach (StateSave state in list)
                {
                    if(isElse)
                    {
                        codeBlock = codeBlock.ElseIf($"state == {variableType}.{state.Name}");

                    }
                    else
                    {
                        codeBlock = codeBlock.If($"state == {variableType}.{state.Name}");

                    }

                    isElse = true;
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
            }
            return codeBlock;
        }

        public override ICodeBlock GenerateLoadStaticContent(ICodeBlock codeBlock, IElement element)
        {
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


        private static string GetLeftSideOfEquals(IElement element, CustomVariable customVariable, string member, bool switchToRelative)
        {
            string leftSideOfEquals = member;

            if (switchToRelative && customVariable != null)
            {
                string possibleLeftSide = RelativeValueForInstruction(member, customVariable, element);
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
                    returnValue |= !string.IsNullOrEmpty(RelativeValueForInstruction(instruction.Member, customVariable, element));
                }
            }

            return returnValue;
        }

        private static string RelativeValueForInstruction(string memberName, CustomVariable customVariable, IElement element)
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
                    relativeMember = InstructionManager.GetRelativeForAbsolute(memberName);
                }
                return relativeMember;
            }

        }

        public static string GetRightSideAssignmentValueAsString(IElement element, InstructionSave instruction)
        {
            var value = instruction.Value;
            var memberName = instruction.Member;
            return GetRightSideAssignmentValueAsString(element, memberName, value);
        }

        private static string GetRightSideAssignmentValueAsString(IElement element, string memberName, object value)
        {
            CustomVariable customVariable = element.GetCustomVariableRecursively(memberName);

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

                valueAsString = CodeParser.ParseObjectValue(value);

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

                            if (category != null && category.SharesVariablesWithOtherCategories == false)
                            {
                                typeName = category.Name;
                            }
                            valueAsString = objectElement.Name.Replace("/", ".").Replace("\\", ".") + "." + typeName + "." + valueAsString.Replace("\"", "");
                        }
                    }

                    valueAsString = CodeWriter.MakeLocalizedIfNecessary(
                        namedObject,
                        memberName,
                        value,
                        valueAsString,
                        customVariable);
                }
            }
            else
            {
                string enumValue = (string)value;

                if (!string.IsNullOrEmpty(enumValue) && enumValue != "<NONE>")
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
            var isAlreadyQualifed = variableType.Contains('.');
            if(isAlreadyQualifed)
            {
                return $"{variableType}.{enumValue}";
            }
            else
            {
                string valueAsString =
                    ProjectManager.ProjectNamespace + "." + referencedElement.Name.Replace("\\", ".") + "." + variableType + "." + enumValue;
                return valueAsString;
            }
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
