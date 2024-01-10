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
using FlatRedBall.Glue.TypeConversions;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Glue.CodeGeneration
{
    public partial class StateCodeGenerator : ElementComponentCodeGenerator
    {

        #region Create Inner Class

        private static ICodeBlock CreateClassForStateCategory(ICodeBlock currentBlock, List<StateSave> statesForThisCategory, StateSaveCategory category, GlueElement element)
        {
            string categoryClassName = category?.Name ?? "VariableState";

            // Update August 18, 2021
            // We want to generate the
            // class even if it doesn't
            // have any states. This allows
            // GView to inject states dynamically.
            //if (statesForThisCategory.Count != 0)
            string prefix = "public";

            string postfix = null;
            if (IsStateDefinedInBase(element, categoryClassName))
            {
                postfix = $" : {element.BaseElement.Replace("\\", ".")}.{categoryClassName}";
            }


            currentBlock = currentBlock.Class(prefix, categoryClassName, postfix);

            currentBlock.Line($"public string Name;");

            var includedVariables = element.CustomVariables.Where(item =>
            {
                return ShouldIncludeVariable(category, element, item);
            })
                .ToArray();

            foreach (var variable in includedVariables)
            {
                string type = variable.Type;

                if (variable.GetIsFile())
                {
                    type = "string";
                }
                else if (!string.IsNullOrEmpty(variable.OverridingPropertyType))
                {
                    type = variable.OverridingPropertyType;
                }
                else
                {
                    type = CustomVariableCodeGenerator.GetMemberTypeFor(variable, element);
                }
                currentBlock.Line($"public {type} {variable.Name};");
            }

            CreatePredefinedStateInstances(currentBlock, statesForThisCategory, element, categoryClassName, includedVariables);

            currentBlock.Line($"public static Dictionary<string, {categoryClassName}> AllStates = new Dictionary<string, {categoryClassName}>");
            var dictionaryBlock = currentBlock.Block();
            dictionaryBlock.PostCodeLines.Add(new CodeLine(";"));

            for (int i = 0; i < statesForThisCategory.Count; i++)
            {
                var state = statesForThisCategory[i];
                dictionaryBlock.Line("{\"" + state.Name + "\", " + state.Name + "},");
            }

            currentBlock = currentBlock.End();

            return currentBlock;
        }

        private static bool ShouldIncludeVariable(StateSaveCategory category, GlueElement element, CustomVariable item)
        {
            if (category == null)
            {
                var isState = item.GetIsVariableState(element);

                // states setting other states causes lots of problems. Since uncategorized states can't exclude variables in the StateData world, we just won't
                // set other states
                return !isState;
            }
            else
            {
                return category?.ExcludedVariables.Contains(item.Name) == false;
            }
        }

        private static void CreatePredefinedStateInstances(ICodeBlock currentBlock, List<StateSave> statesForThisCategory, GlueElement element, string categoryClassName, CustomVariable[] includedVariables)
        {
            for (int i = 0; i < statesForThisCategory.Count; i++)
            {
                var state = statesForThisCategory[i];
                currentBlock.Line($"public static {categoryClassName} {state.Name} = new {categoryClassName}()");

                var variableBlock = currentBlock.Block();

                variableBlock.Line($"Name = \"{state.Name}\",");

                foreach (var variable in includedVariables)
                {
                    var instruction = state.InstructionSaves.FirstOrDefault(item => item.Member == variable.Name);

                    var instructionValue = instruction?.Value;

                    var valueToSet = instructionValue ?? variable.DefaultValue;

                    var shouldAssign = valueToSet != null;

                    if(shouldAssign && valueToSet is string && ((string)valueToSet) == string.Empty)
                    {
                        if(variable.Type == "bool" ||
                            variable.Type == "int" ||
                            variable.Type == "float" ||
                            variable.Type == "long" ||
                            variable.Type == "byte" ||
                            variable.Type == "double" ||
                            variable.Type == "decimal"
                            )
                        {
                            shouldAssign = false;
                        }
                    }

                    if(shouldAssign && instructionValue is string asString && variable.GetIsVariableState(element) == true && string.IsNullOrEmpty(asString))
                    {
                        shouldAssign = false;
                    }

                    if (shouldAssign)
                    {
                        var rightSide = GetRightSideAssignmentValueAsString(element, variable.Name, valueToSet);
                        var matchingVariable = element.GetCustomVariableRecursively(variable.Name);
                        if (matchingVariable?.GetIsFile() == true)
                        {
                            // If it's a file we are only going to reference the file name here as to not preload the file
                            rightSide = $"\"{rightSide}\"";
                        }

                        variableBlock.Line($"{variable.Name} = {rightSide},");
                    }
                }
                variableBlock.End().Line(";");
            }
        }

        #endregion

        #region Generating Fields / Properties

        // Generates the fields AND PROPERTIES even though it's only called GenerateFields
        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, SaveClasses.IElement element)
        {


            var currentBlock = codeBlock;

            if (element.HasStates || element.StateCategoryList.Count > 0)
            {
                List<StateSave> statesForThisCategory = GetSharedVariableStates(element);


                currentBlock = CreateClassForStateCategory(currentBlock, statesForThisCategory, null, element as GlueElement);
                GenerateCurrentStateProperty(element as GlueElement, codeBlock, null, statesForThisCategory);

                //Build State Categories
                var stateCategories = GetAllStateCategoryNames(element, false);

                foreach (var kvp in stateCategories)
                {
                    var states = GetAllStatesForCategory(element, kvp.Key);

                    var category = kvp.Value;
                    CreateClassForStateCategory(currentBlock, states, category, element as GlueElement);
                    GenerateCurrentStateProperty(element as GlueElement, codeBlock, category, states);
                }
            }

            return codeBlock;
        }

        private static ICodeBlock GenerateCurrentStateProperty(GlueElement element, ICodeBlock codeBlock, StateSaveCategory category, List<StateSave> states)
        {
            string enumType = category?.Name ?? "VariableState";
            // early out
            // Update August 20, 2021
            // Don't early out anymore, 
            // even though there are no states
            // now, they could be added dynamically
            // by the user or by GView
            //if (states.Count == 0)
            //{
            //    return codeBlock;
            //}

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

            // August 9, 2021
            // Before StateData,
            // states were enums which
            // resulted in variables assigned
            // in code generation. After StateData,
            // states became an object which held their
            // own values. This means that users can now
            // define their own states dynamically and change
            // a state at runtime (not sure why, but it's possible).
            // Since states now store their own values, we no longer need
            // codegen to explicitly assign variables. Sure, assigning hard
            // values is faster, but having conditionals is slower so...does
            // it really save us that much? Also, it's extra gencode bloat that
            // we could get rid of and replace with a single assignment block. That
            // else statement was added today in GenerateVariableAssignmentForDynamicState
            // which means we could probably get rid of the GenerateVariableAssignmentForState
            // calls. But I don't know if the two are 100% equivalent so I'll leave the codegen
            // in for now. However, eventually this can go away completely with some testing...perhaps
            // once the automated test project is revived.
            // Update August 9, 2021 - Well that didn't take long. If we leave the hardcoded generation in
            // then if the user changes a compiled-in state, the hardcoded value will get assigned even if the
            // user changes it. I guess we're going to have to convert over right now...
            //bool isElse = false;
            //foreach (StateSave stateSave in states)
            //{
            //    GenerateVariableAssignmentForState(element, setBlock, stateSave, category, isElse);
            //    isElse = true;
            //}

            GenerateVariableAssignmentForDynamicState(element, setBlock, category);


            if ((enumType == "VariableState" && DoesBaseHaveUncategorizedStates(element)) ||
                (!string.IsNullOrEmpty(element.BaseElement) && GetAllStateCategoryNames(ObjectFinder.Self.GetElement(element.BaseElement), true).Any(category => category.Key == enumType)))
            {
                // October 29, 2021
                // I dont' think we need
                // an else anymore, we can
                // just call into the base:
                //setBlock.Else()
                setBlock
                    .Line("base.Current" + variableNameModifier + "State = base.Current" + variableNameModifier + "State;");
            }

            if (hasEvent)
            {
                EventCodeGenerator.GenerateEventRaisingCode(setBlock, BeforeOrAfter.After, variableToLookFor, element);
            }

            return codeBlock;
        }

        private static void GenerateVariableAssignmentForDynamicState(GlueElement element, ICodeBlock setBlock, StateSaveCategory category)
        {
            setBlock = setBlock.If("value != null");
            foreach (var variable in element.CustomVariables)
            {
                var shouldInclude = false;
                if (category != null)
                {
                    if (category.ExcludedVariables.Contains(variable.Name))
                    {
                        shouldInclude = false;
                    }
                    // If we don't do this, the variable will recursively get set. 
                    // I can't think of a case where a user would want this since it
                    // will inevitably turn into a StackOverflowException.
                    else if (variable.Type == category.Name)
                    {
                        shouldInclude = false;
                    }
                    else
                    {
                        shouldInclude = true;
                    }
                }
                else
                {
                    // I guess if its the default state, we assign always?
                    shouldInclude = true;
                }

                if(shouldInclude)
                {
                    shouldInclude = ShouldIncludeVariable(category, element, variable);
                }

                if (shouldInclude)
                {

                    string member = variable.Name;

                    // Get the valueAsString, which is the right-side of the equals sign
                    string rightSideOfEquals = $"value.{variable.Name}";

                    CustomVariable customVariable = element.GetCustomVariableRecursively(member);
                    var isFile = customVariable.GetIsFile();
                    var assignOnlyIfNonNull = false;
                    if(isFile)
                    {
                        var type = customVariable.Type;
                        if(type == "AnimationChainList")
                        {
                            type = typeof(Graphics.Animation.AnimationChainList).FullName;
                            assignOnlyIfNonNull = true;
                        }
                        else if(type == nameof(Texture2D))
                        {
                            type = typeof(Texture2D).FullName;
                        }
                        rightSideOfEquals = $"GetFile(value.{variable.Name}) as {type}";
                    }
                    //else if(!string.IsNullOrWhiteSpace(customVariable.OverridingPropertyType))
                    //{
                    //    var value = TypeConverterHelper.Convert(customVariable, GetterOrSetter.Getter, "value." + variable.Name);
                    //    rightSideOfEquals = value;
                    //}

                    NamedObjectSave referencedNos = element.GetNamedObjectRecursively(customVariable.SourceObject);
                    if (referencedNos != null)
                    {
                        NamedObjectSaveCodeGenerator.AddIfConditionalSymbolIfNecesssary(setBlock, referencedNos);
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

                        setBlock
                            .If(objectWithParent + ".Parent == null")
                                .Line(leftSideOfEquals + " = " + rightSideOfEquals + ";")
                            .End()

                            .Else()
                                .Line(leftSideOfEqualsWithRelative + " = " + rightSideOfEquals + ";");
                    }
                    else
                    {
                        var effectiveBlock = setBlock;
                        if(assignOnlyIfNonNull)
                        {
                            effectiveBlock = setBlock.If($"({rightSideOfEquals}) != null");
                        }
                        effectiveBlock.Line(leftSideOfEquals + " = " + rightSideOfEquals + ";");
                    }

                    if (referencedNos != null)
                    {
                        NamedObjectSaveCodeGenerator.AddEndIfIfNecessary(setBlock, referencedNos);
                    }

                }
            }
        }



        public static List<StateSave> GetAllStatesForCategory(IElement element, string stateCategory)
        {
            var states = new List<StateSave>();

            if (!string.IsNullOrEmpty(element.BaseElement))
            {
                IElement baseElement = ObjectFinder.Self.GetElement(element.BaseElement);

                if (baseElement != null)
                {
                    states.AddRange(GetAllStatesForCategory(baseElement, stateCategory));
                }
            }

            var category =
                element.StateCategoryList.Find(
                    cat => cat.Name == stateCategory);

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
                    var uncontained = GetAllStateCategoryNames(ObjectFinder.Self.GetElement(element.BaseElement), includeInheritance).Where(name => !names.ContainsKey(name.Key));

                    foreach (var kvp in uncontained)
                    {
                        names.Add(kvp.Key, kvp.Value);
                    }
                }

                foreach (var category in element.StateCategoryList.Where(category => !names.ContainsKey(category.Name)))
                {
                    names.Add(category.Name, category);
                }
            }
            return names;
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
                IElement baseElement = ObjectFinder.Self.GetElement(element.BaseElement);

                if (baseElement != null)
                {
                    toReturn = baseElement.DefinesCategoryEnumRecursive(enumName);
                }
            }
            return toReturn;
        }

        private static ICodeBlock GenerateVariableAssignmentForState(GlueElement element, ICodeBlock codeBlock, StateSave stateSave, StateSaveCategory category, bool isElse)
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

            foreach(var variable in element.CustomVariables)
            {
                var shouldInclude = false;
                if(category != null)
                {
                    if(category.ExcludedVariables.Contains(variable.Name))
                    {
                        shouldInclude = false;
                    }
                    // If we don't do this, the variable will recursively get set. 
                    // I can't think of a case where a user would want this since it
                    // will inevitably turn into a StackOverflowException.
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
                IElement baseElement = ObjectFinder.Self.GetElement(currentElement.BaseElement);
                if (baseElement != null)
                {
                    statesForThisCategory.AddRange(GetSharedVariableStates(baseElement));
                }
            }

            
            for (int i = 0; i < element.States.Count; i++)
            {
                statesForThisCategory.Add(element.States[i]);
            }

            return statesForThisCategory;
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
                        IElement baseElement = ObjectFinder.Self.GetElement(element.BaseElement);
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




                GenerateInterpolationAdditionalMethods(codeBlock, element as GlueElement, sharedVariableStates);

                GeneratePreloadStateContent(codeBlock, element as GlueElement, sharedVariableStates);
            }

            return codeBlock;
        }

        private void GeneratePreloadStateContent(ICodeBlock codeBlock, GlueElement element, List<StateSave> sharedVariableStates)
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

                foreach (StateSaveCategory category in element.StateCategoryList)
                {
                    codeBlock = GeneratePreloadStateContentForStateType(codeBlock, element, category.States, category.Name);
                }
            }
        }

        private static ICodeBlock GeneratePreloadStateContentForStateType(ICodeBlock codeBlock, GlueElement element, List<StateSave> list, string variableType)
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
                IElement baseElement = ObjectFinder.Self.GetElement(element.BaseElement);

                if (baseElement != null)
                {

                    if (baseElement.States.Count != 0)
                    {
                        return true;
                    }

                }
                return false;

            }

        }

        private static string GetLeftSideOfEquals(GlueElement element, CustomVariable customVariable, string member, bool switchToRelative)
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

        private static bool GetDoesStateAssignAbsoluteValues(StateSave stateSave, GlueElement element)
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

        private static string RelativeValueForInstruction(string memberName, CustomVariable customVariable, GlueElement element)
        {
            var rootCustomVariable = ObjectFinder.Self.GetBaseCustomVariable(customVariable);

            if (!string.IsNullOrEmpty(rootCustomVariable.SourceObject))
            {
                var namedObject = element.GetNamedObjectRecursively(rootCustomVariable.SourceObject);

                var isGum = namedObject?.SourceType == SourceType.Gum;

                string relativeMember = null;

                if (!isGum)
                {
                    relativeMember = InstructionManager.GetRelativeForAbsolute(customVariable.SourceObjectProperty);
                }

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

        public static string GetRightSideAssignmentValueAsString(GlueElement element, InstructionSave instruction)
        {
            var value = instruction.Value;
            var memberName = instruction.Member;
            return GetRightSideAssignmentValueAsString(element, memberName, value);
        }

        // Note - this code is very similar to CustomVariableCodeGenerator's AppendAssignmentForCustomVariableInElement
        // Unify?
        private static string GetRightSideAssignmentValueAsString(GlueElement element, string memberName, object value)
        {
            CustomVariable customVariable = element.GetCustomVariableRecursively(memberName);
            customVariable = ObjectFinder.Self.GetBaseCustomVariable(customVariable);

            IElement referencedElement = null;

            #region Determine if the assignment is a file

            bool isFile = false;

            if (customVariable != null)
            {
                referencedElement =
                    CustomVariableCodeGenerator.GetElementIfCustomVariableIsVariableState(customVariable, element);

                isFile = customVariable.GetIsFile();

            }

            #endregion

            string valueAsString = "";

            if (referencedElement == null)
            {

                valueAsString = CodeParser.ConvertValueToCodeString(value);

                if (isFile)
                {
                    valueAsString = valueAsString.Replace("\"", "");

                    if (valueAsString == "<NONE>")
                    {
                        valueAsString = "null";
                    }
                }
                else if (CustomVariableCodeGenerator.ShouldAssignToCsv(customVariable, valueAsString, element))
                {
                    valueAsString = CustomVariableCodeGenerator.GetAssignmentToCsvItem(customVariable, element, valueAsString);
                }
                else if (customVariable is { Type: "Color" })
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
                        objectElement = ObjectFinder.Self.GetElement(namedObject.SourceClassType);
                    }

                    if (objectElement != null)
                    {
                        if (isVariableState)
                        {
                            string typeName = "VariableState";

                            StateSaveCategory category = objectElement.GetStateCategoryRecursively(customVariable.Type);

                            if(category != null)
                            {
                                typeName = category.Name;

                                valueAsString = objectElement.Name.Replace("/", ".").Replace("\\", ".") + "." + typeName + "." + valueAsString.Replace("\"", "");
                            }
                        }
                    }

                    valueAsString = CodeWriter.MakeLocalizedIfNecessary(
                        namedObject,
                        memberName,
                        value,
                        valueAsString,
                        customVariable);

                    if(namedObject?.SourceType == SourceType.Gum && customVariable.Type?.Contains(".") == true && customVariable.Type.EndsWith("?"))
                    {
                        // this is a state type, so remove the "?" and prefix it:
                        valueAsString = customVariable.Type.Substring(0, customVariable.Type.Length - 1) + "." + value;
                    }

                    // Conversion happens in the variable assignment on the instance, not in the state
                    //if(!string.IsNullOrEmpty(customVariable.OverridingPropertyType))
                    //{
                    //    valueAsString = TypeConverterHelper.Convert(customVariable, GetterOrSetter.Setter, valueAsString);
                    //}
                }
            }
            else
            {
                string enumValue = (string)value;

                if (!string.IsNullOrEmpty(enumValue) && enumValue != "<NONE>")
                {
                    string variableType = "VariableState";

                    if (customVariable != null && !String.Equals(customVariable.Type, "string", StringComparison.OrdinalIgnoreCase))
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
