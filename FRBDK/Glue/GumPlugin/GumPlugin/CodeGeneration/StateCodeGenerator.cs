using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;

namespace GumPlugin.CodeGeneration
{
    public partial class StateCodeGenerator : Singleton<StateCodeGenerator>
    {
        #region Fields

        List<string> mVariableNamesToSkipForStates = new List<string>();

        public static Dictionary<string, string> VariableNamesToReplaceForStates = new Dictionary<string, string>();
        
        #endregion

        #region Methods

        #region Constructor/Init

        static StateCodeGenerator()
        {
            VariableNamesToReplaceForStates.Add("Texture Address", "TextureAddress");
            VariableNamesToReplaceForStates.Add("Texture Height Scale", "TextureHeightScale");
            VariableNamesToReplaceForStates.Add("Texture Width Scale", "TextureWidthScale");
            VariableNamesToReplaceForStates.Add("Texture Height", "TextureHeight");
            VariableNamesToReplaceForStates.Add("Texture Width", "TextureWidth");
            VariableNamesToReplaceForStates.Add("Texture Left", "TextureLeft");
            VariableNamesToReplaceForStates.Add("Texture Top", "TextureTop");
            VariableNamesToReplaceForStates.Add("Font Scale", "FontScale");
            VariableNamesToReplaceForStates.Add("Clips Children", "ClipsChildren");
            VariableNamesToReplaceForStates.Add("Children Layout", "ChildrenLayout");
            VariableNamesToReplaceForStates.Add("Custom Texture Coordinates", "CustomTextureCoordinates");


            VariableNamesToReplaceForStates.Add("X Origin", "XOrigin");
            VariableNamesToReplaceForStates.Add("X Units", "XUnits");
            VariableNamesToReplaceForStates.Add("Y Origin", "YOrigin");
            VariableNamesToReplaceForStates.Add("Y Units", "YUnits");
            VariableNamesToReplaceForStates.Add("Wraps Children", "WrapsChildren");
            VariableNamesToReplaceForStates.Add("Source File", "SourceFile");

            VariableNamesToReplaceForStates.Add("Width Units", "WidthUnits");
            VariableNamesToReplaceForStates.Add("Height Units", "HeightUnits");

        }

        public StateCodeGenerator()
        {
            AddVariablesToSkipForStates();
        }

        private void AddVariablesToSkipForStates()
        {
            //mVariableNamesToSkipForStates.Add("CustomFontFile");
            //mVariableNamesToSkipForStates.Add("UseCustomFont");
            mVariableNamesToSkipForStates.Add("Guide");
            //mVariableNamesToSkipForStates.Add("Parent");

            // Why did we skip width units and height units?
            //mVariableNamesToSkipForStates.Add("Height Units");
            //mVariableNamesToSkipForStates.Add("Width Units");
            mVariableNamesToSkipForStates.Add("Custom Texture Coordinates"); // This is now handled by TextureCoordinateType
            //mVariableNamesToSkipForStates.Add("Children Layout");

            //mVariableNamesToSkipForStates.Add("Font");
            //mVariableNamesToSkipForStates.Add("FontSize");
            //mVariableNamesToSkipForStates.Add("OutlineThickness");

            // August 29 - adding support for these:
            //mVariableNamesToSkipForStates.Add("HasEvents");
            //mVariableNamesToSkipForStates.Add("ExposeChildrenEvents");

            //mVariableNamesToSkipForStates.Add("SourceFile");
            mVariableNamesToSkipForStates.Add("Contained Type");
            mVariableNamesToSkipForStates.Add("IsXamarinFormsControl");
            mVariableNamesToSkipForStates.Add("IsOverrideInCodeGen");
            //mVariableNamesToSkipForStates.Add("IsBold");



            // Eventually we'll support this but first Gum needs to support 
            // setting categorized states on instances
            // September 17 2014
            // no longer needed:
            //mVariableNamesToSkipForStates.Add("State");



        }

        public void RefreshVariableNamesToSkipBasedOnGlueVersion()
        {
            var version = (int)GlueState.Self.CurrentGlueProject.FileVersion;

            if (version >= (int)GluxVersions.GumSupportsStackSpacing)
            {
                if (mVariableNamesToSkipForStates.Contains("StackSpacing"))
                {
                    mVariableNamesToSkipForStates.Remove("StackSpacing");
                }
            }
            else
            {
                if (!mVariableNamesToSkipForStates.Contains("StackSpacing"))
                {
                    mVariableNamesToSkipForStates.Add("StackSpacing");
                }
            }
        }

        #endregion

        public void GenerateEverythingFor(ElementSave elementSave, ICodeBlock currentBlock)
        {
            GenerateStateEnums(elementSave, currentBlock);

            GenerateCurrentStateFields(elementSave, currentBlock);
            
            GenerateCurrentStateProperties(elementSave, currentBlock);

            GenerateStateInterpolateBetween(elementSave, currentBlock);

            GenerateStateInterpolateTo(elementSave, currentBlock);

            GenerateAnimationEnumerables(elementSave, currentBlock);

            GenerateStopAnimations(elementSave, currentBlock);

            GenerateGetAnimations(elementSave, currentBlock);

            GenerateGetCurrentValuesOnState(elementSave, currentBlock);

            GenerateApplyStateOverride(elementSave, currentBlock);
        }

        private void GenerateApplyStateOverride(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock = currentBlock.Function("public override void", "ApplyState", "Gum.DataTypes.Variables.StateSave state");
            {
                currentBlock.Line("bool matches = this.ElementSave.AllStates.Contains(state);");

                var ifStatement = currentBlock.If("matches");
                {
                    ifStatement.Line("var category = this.ElementSave.Categories.FirstOrDefault(item => item.States.Contains(state));");

                    var innerIf = ifStatement.If("category == null");
                    {
                        foreach(var state in elementSave.States)
                        {
                            innerIf.Line($"if (state.Name == \"{state.Name}\") this.mCurrentVariableState = VariableState.{state.MemberNameInCode()};");
                        }
                    }
                    foreach(var category in elementSave.Categories)
                    {
                        var elseIf = ifStatement.ElseIf($"category.Name == \"{category.Name}\"");
                        {
                            foreach(var state in category.States)
                            {
                                elseIf.Line($"if(state.Name == \"{state.Name}\") this.mCurrent{category.Name}State = {category.Name}.{state.MemberNameInCode()};");
                            }
                        }
                    }
                }

                currentBlock.Line("base.ApplyState(state);");
            }
        }

        public void GenerateStateEnums(IStateContainer stateContainer, ICodeBlock currentBlock, string enumNamePrefix = null)
        {
            bool hasUncategorized = (stateContainer is BehaviorSave) == false;

            currentBlock.Line("#region State Enums");

            if (hasUncategorized)
            {
                string categoryName = "VariableState";
                var states = stateContainer.UncategorizedStates;
                GenerateEnumsForCategory(currentBlock, categoryName, states);
            }
            // loop through categories:
            foreach (var category in stateContainer.Categories)
            {
                string categoryName = enumNamePrefix + category.Name;
                var states = category.States;
                GenerateEnumsForCategory(currentBlock, categoryName, states);

            }

            currentBlock.Line("#endregion");
        }

        public void GenerateEnumsForCategory(ICodeBlock codeBlock, string categoryName, IEnumerable<StateSave> states)
        {
            var enumBlock = codeBlock.Enum("public", categoryName);

            foreach (var item in states)
            {
                if (item == states.Last())
                {
                    enumBlock.Line(item.MemberNameInCode());
                }
                else
                {
                    enumBlock.Line(item.MemberNameInCode() + ",");
                }
            }
        }


        private bool GetIfShouldGenerateStateVariable(Gum.DataTypes.Variables.VariableSave variable, ElementSave container)
        {
            bool toReturn = true;

            string variableName = variable.GetRootName();



            if (variable.Value == null || !variable.SetsValue)
            {
                toReturn = false;
            }
            // states can't set states on this
            if(variable.IsState(container) && string.IsNullOrEmpty(variable.SourceObject ) )
            {
                toReturn = false;
            }

            if (toReturn && mVariableNamesToSkipForStates.Contains(variableName))
            {
                toReturn = false;
            }

            bool hasSourceObject = !string.IsNullOrEmpty(variable.SourceObject);

            if (toReturn && hasSourceObject)
            {
                InstanceSave instanceSave = container.GetInstance(variable.SourceObject);

                if(instanceSave == null)
                {
                    toReturn = false;
                }
                else 
                { 
                    var baseElement = Gum.Managers.ObjectFinder.Self.GetElementSave(instanceSave.BaseType);

                    if (baseElement == null)
                    {
                        toReturn = false;
                    }

                    if (toReturn)
                    {

                        // Gum (just like Glue) keeps variables that aren't needed around.  This allows users to rename things and not lose
                        // important information accidentally.  But because of that we have to make sure that the variable we're working with is
                        // valid for the type of object we're dealing with.  
                        var defaultState = baseElement.DefaultState;

                        // October 26, 2018
                        // Bernardo reported
                        // a crash caused by the
                        // RecursiveVariableFinder
                        // being given a state without
                        // a ParentContainer. This is a
                        // sign that the element hasn't
                        // been initialized yet. Elements
                        // should be initialized, but if they're
                        // not, we could just catch it here and initialize
                        // it on the spot. Not sure if I like this solution
                        // or not. It allows code to behave a little unpredictably,
                        // but at the same time, we could simply solve the problem by
                        // initializing here, so I'm going to do that:
                        if(defaultState.ParentContainer == null)
                        {
                            baseElement.Initialize(null);
                        }
                        
                        RecursiveVariableFinder rvf = new RecursiveVariableFinder(defaultState);

                        var foundVariable = rvf.GetVariable(variable.GetRootName());

                        if (foundVariable == null)
                        {
                            // This doesn't exist anywhere in the inheritance chain, so we don't want to generate it:
                            toReturn = false;
                        }
                    }
                }
            }

            if(toReturn && !hasSourceObject)
            {
                // If a variable is part of a component, it better be defined in the base type or else we won't generate it.
                // For example, consider a component that used to inherit from Text. It will have variables for fonts. If that
                // component switches to inheriting from Sprite, those variables will still exist in the XML for that component,
                // but we shouldn't generate any state variables for those variables. So we'll go to the base type and see if those
                // variables exist
                bool isComponent = container is ComponentSave;

                var rootStandardElementSave = Gum.Managers.ObjectFinder.Self.GetRootStandardElementSave(container);

                // If the Container is a Screen, then rootComponent will be null, so we don't need to do anything
                if (rootStandardElementSave == null)
                {
                    toReturn = false;
                }
                else
                {
                    IEnumerable<VariableSave> variablesToCheck;

                    // This code used to get the default state from the rootStandardElementSave, 
                    // but the standard element save can have variables missing from the Gum XML,
                    // but it should still support them based on the definition in the StandardElementsManager,
                    // especially if new variables have been added in the future. Therefore, use the StandardElementsManager
                    // rather than the DefaultState:
                    //var rootStandardElementVariables = rootStandardElementSave.DefaultState.Variables;
                    var rootStandardElementVariables = StandardElementsManager.Self
                        .DefaultStates[rootStandardElementSave.Name].Variables;

                    if (isComponent)
                    {
                        var component = Gum.Managers.ObjectFinder.Self.GetStandardElement("Component");

                        variablesToCheck = rootStandardElementVariables.Concat(component.DefaultState.Variables).ToList();
                    }
                    else
                    {
                        variablesToCheck = rootStandardElementVariables.ToList();
                    }


                    bool wasMatchFound = variablesToCheck.Any(item => item.Name == variable.GetRootName());
                    toReturn = wasMatchFound;
                }
            }



            return toReturn;
        }

        private void GenerateCurrentStateFields(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock.Line("#region State Fields");
            string propertyName = "CurrentVariableState";
            string propertyType = "VariableState";
            currentBlock.Line(propertyType + " m" + propertyName + ";");


            foreach (var category in elementSave.Categories)
            {
                propertyName = "Current" + category.Name + "State";
                propertyType = category.Name;

                // Make these nullable because categorized states may not be set at all
                currentBlock.Line($"{propertyType}? m{propertyName};");
            }
            currentBlock.Line("#endregion");
        }

        private void GenerateCurrentStateProperties(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock.Line("#region State Properties");

            string propertyName = "CurrentVariableState";
            string propertyType = "VariableState";
            var states = elementSave.States;



            GeneratePropertyForCurrentState(currentBlock, propertyType, propertyName, states, elementSave, isNullable:false);

            foreach (var category in elementSave.Categories)
            {


                propertyName = "Current" + category.Name + "State";
                propertyType = category.Name;
                states = category.States;



                GeneratePropertyForCurrentState(currentBlock, propertyType, propertyName, states, elementSave, isNullable:true);
            }

            //GenerateBehaviorStateProperties(currentBlock, elementSave);

            currentBlock.Line("#endregion");

        }

        private void GenerateBehaviorStateProperties(ICodeBlock currentBlock, ElementSave elementSave)
        {
            var asComponentSave = elementSave as ComponentSave;

            if(asComponentSave != null)
            {
                foreach(var elementBehavior in asComponentSave.Behaviors)
                {
                    var behavior = Managers.AppState.Self.GumProjectSave.Behaviors
                        .FirstOrDefault(item => item.Name == elementBehavior.BehaviorName);

                    if(behavior == null)
                    {
                        // user set a behavior, then deleted the behavior. We don't want to generate code for it
                        currentBlock.Line("// No properties generated for behavior because it's not part of the Gum project: " + elementBehavior.BehaviorName);
                    }
                    else
                    {

                        string interfaceType = $"I{behavior.Name}";

                        foreach(var behaviorCategory in behavior.Categories)
                        {
                            string propertyType = $"{behavior.Name}{behaviorCategory.Name}";

                            var propertyBlock = currentBlock.Property($"{propertyType}", $"{interfaceType}.Current{propertyType}State");
                            var setBlock = propertyBlock.Set();
                            var switchBlock = setBlock.Switch("value");
                            foreach(var behaviorState in behaviorCategory.States)
                            {
                                var caseBlock = switchBlock.Case($"{behavior.Name}{behaviorCategory.Name}.{behaviorState.Name}");

                                caseBlock.Line($"this.Current{behaviorCategory.Name}State = {behaviorCategory.Name}.{behaviorState.Name};");

                            }
                        }
                    }
                }
            }
        }

        private void GeneratePropertyForCurrentState(ICodeBlock currentBlock, string propertyType, string propertyName, 
            List<Gum.DataTypes.Variables.StateSave> states, ElementSave container, bool isNullable)
        {
            string propertyPrefix;
            if(isNullable)
            {
                propertyPrefix = $"public {propertyType}?";
            }
            else
            {
                propertyPrefix = $"public {propertyType}";
            }
            var property = currentBlock.Property(propertyPrefix, propertyName);

            property.Get().Line("return m" + propertyName + ";");

            var setter = property.Set();
            {
                if(isNullable)
                {
                    setter = setter.If("value != null");
                }
                setter.Line("m" + propertyName + " = value;");

                var switchBlock = setter.Switch("m" + propertyName);

                foreach (var state in states)
                {
                    var caseBlock = switchBlock.Case(propertyType + "." + state.MemberNameInCode());
                    {
                        // Parent variables need to be assigned in the order of the objects in the component so that they're attached in the right order.
                        // If they're attached in the wrong order, then stacking won't work properly:
                        var instanceNames = container.Instances.Select(item => item.Name).ToList();

                        var orderedVariables = state.Variables
                            .OrderByDescending(variable => variable.GetRootName() == "Parent")
                            .ThenByDescending(variable => variable.IsState(container))
                            .ThenBy(variable => instanceNames.IndexOf(variable.SourceObject))
                            .ToList();

                        foreach (var variable in orderedVariables)
                        {
                            var shouldGenerate = false;
                            try
                            {
                                shouldGenerate = GetIfShouldGenerateStateVariable(variable, container);
                            }
                            catch(Exception e)
                            {
                                GlueCommands.Self.PrintError(e.ToString());
                            }
                            // where block doesn't debug well for some reason, so I unrolled it...
                            if (shouldGenerate)
                            {
                                // Note that this could return values like "1,2" instead of "1.2" depending
                                // on the current language, so the AdjustVariableValueIfNecessary needs to account for that.
                                string variableValue = variable.Value.ToString();
                                bool isEntireAssignment;

                                GueDerivingClassCodeGenerator.Self.AdjustVariableValueIfNecessary(variable, container, ref variableValue, out isEntireAssignment);
                                if (isEntireAssignment)
                                {
                                    caseBlock.Line(variableValue);
                                }
                                else
                                {
                                    string memberNameInCode = variable.MemberNameInCode(container, VariableNamesToReplaceForStates);
                                    caseBlock.Line(memberNameInCode + " = " + variableValue + ";");
                                }
                            }
                        }
                    }
                }
            }
        }

        private void GenerateGetCurrentValuesOnState(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock.Line("#region Get Current Values on State");

            string categoryName = "VariableState";
            var states = elementSave.States;
            GenerateGetCurrentValuesOnStateForCategory(currentBlock, elementSave, categoryName, states, addValues:false);
            GenerateGetCurrentValuesOnStateForCategory(currentBlock, elementSave, categoryName, states, addValues:true);


            foreach (var category in elementSave.Categories)
            {
                categoryName = category.Name;
                states = category.States;
                GenerateGetCurrentValuesOnStateForCategory(currentBlock, elementSave, categoryName, states, addValues:false);
                GenerateGetCurrentValuesOnStateForCategory(currentBlock, elementSave, categoryName, states, addValues:true);
            }

            currentBlock.Line("#endregion");
        }

        private void GenerateGetCurrentValuesOnStateForCategory(ICodeBlock currentBlock, ElementSave container, string categoryName, List<Gum.DataTypes.Variables.StateSave> states, bool addValues = false)
        {
            string methodName = "GetCurrentValuesOnState";

            if(addValues)
            {
                methodName = "AddToCurrentValuesWithState";
            }

            currentBlock = currentBlock.Function("private Gum.DataTypes.Variables.StateSave", methodName, categoryName + " state");

            currentBlock.Line("Gum.DataTypes.Variables.StateSave newState = new Gum.DataTypes.Variables.StateSave();");

            var switchBlock = currentBlock.Switch("state");
            {
                foreach (var state in states)
                {
                    var caseBlock = switchBlock.Case(categoryName + "." + state.MemberNameInCode());
                    {
                        var instanceNames = container.Instances.Select(item => item.Name).ToList();
                        var orderedVariables = state.Variables
                            .Where(item => GetIfShouldGenerateStateVariable(item, container))
                            .OrderBy(variable => instanceNames.IndexOf(variable.SourceObject));

                        foreach (var variable in orderedVariables)
                        {
                            string memberNameInCode = variable.MemberNameInCode(container, VariableNamesToReplaceForStates);

                            caseBlock.Line("newState.Variables.Add(new Gum.DataTypes.Variables.VariableSave()");
                            var instantiatorBlock = caseBlock.Block();
                            {
                                instantiatorBlock.Line("SetsValue = true,");

                                // Don't use memberNameInCode - states from the XML files will not, and we want this
                                // to behave the same so merging (used in interpolation) works properly
                                //instantiatorBlock.Line("Name = \"" + memberNameInCode + "\",");
                                instantiatorBlock.Line("Name = \"" + variable.Name + "\",");

                                instantiatorBlock.Line($"Type = \"{variable.Type}\",");

                                string valueString = "Value = " + memberNameInCode + "";


                                if(addValues && IsVariableNumeric(variable))
                                {
                                    
                                    string variableValue = variable.Value.ToString();
                                    bool isEntireAssignment;
                                    GueDerivingClassCodeGenerator.Self.AdjustVariableValueIfNecessary(variable, container, ref variableValue, out isEntireAssignment);

                                    if (isEntireAssignment)
                                    {
                                        valueString = variableValue;
                                    }
                                    else
                                    {
                                        valueString += " + " + variableValue;
                                    }
                                }
                                instantiatorBlock.Line(valueString);

                            }
                            caseBlock.Line(");");
                        }
                    }
                }
            }

            currentBlock.Line("return newState;");
        }

        private bool IsVariableNumeric(VariableSave variable)
        {
            string type = variable.Type;

            return type == "float" ||
                type == "int" ||
                type == "double" ||
                type == "byte" ||
                type == "decimal" ||
                type == "long";
        }

        #endregion



    }
}
