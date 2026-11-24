using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using GumPlugin.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms.Design;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace GumPlugin.CodeGeneration;

public partial class StateCodeGenerator : Singleton<StateCodeGenerator>
{
    #region Fields

    List<string> mVariableNamesToSkipForStates = new List<string>();

    Dictionary<string, List<string>> typeSpecificVariableNamesToSkipForStates = new();

    public static Dictionary<string, string> VariableNamesToReplaceForStates = new Dictionary<string, string>();
    private TextCodeGenerator _textCodeGenerator;
    private ContainerCodeGenerator _containerCodeGenerator;
    private NineSliceCodeGenerator _nineSliceCodeGenerator;

    #endregion


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

    public void Initialize(TextCodeGenerator textCodeGenerator, ContainerCodeGenerator containerCodeGenerator, NineSliceCodeGenerator nineSliceCodeGenerator)
    {
        _textCodeGenerator = textCodeGenerator;
        _containerCodeGenerator = containerCodeGenerator;
        _nineSliceCodeGenerator = nineSliceCodeGenerator;
    }

    public StateCodeGenerator()
    {
    }

    // Which standard-element variables a state may assign at all is FrbBackedGumVariables' job (see
    // GetIfShouldGenerateVariableInStateSetter). What's left here are version gates on top of it.
    public void RefreshVariablesToSkipForStates()
    {
        mVariableNamesToSkipForStates.Clear();
        typeSpecificVariableNamesToSkipForStates.Clear();

        _containerCodeGenerator.AddTypeSpecificVariableNamesToSkipForStates(typeSpecificVariableNamesToSkipForStates);
        _nineSliceCodeGenerator.AddTypeSpecificVariableNamesToSkipForStates(typeSpecificVariableNamesToSkipForStates);
    }

    public void RefreshVariableNamesToSkipBasedOnGlueVersion()
    {
        var version = (int)GlueState.Self.CurrentGlueProject.FileVersion;

        if (version >= (int)GluxVersions.GumSupportsStackSpacing)
        {
            Include("StackSpacing");
        }
        else
        {
            Skip("StackSpacing");
        }

        if (version >= (int)GluxVersions.GumUsesSystemTypes)
        {
            Include("CustomFrameTextureCoordinateWidth");
            Include("AutoGridHorizontalCells");
            Include("AutoGridVerticalCells");
            Include("LineHeightMultiplier");
        }
        else
        {
            Skip("CustomFrameTextureCoordinateWidth");
            Skip("AutoGridHorizontalCells");
            Skip("AutoGridVerticalCells");
            Skip("LineHeightMultiplier");
        }

        if (version >= (int)GluxVersions.GumHasIgnoredByParentSize)
        {
            Include("IgnoredByParentSize");
        }
        else
        {
            Skip("IgnoredByParentSize");
        }

        if (version >= (int)GluxVersions.NineSliceHasTilingMiddleSections)
        {
            Include("IsTilingMiddleSections");
        }
        else
        {
            Skip("IsTilingMiddleSections");
        }

        // NOT a global skip: unlike StackSpacing/IsTilingMiddleSections above, "Animate" is not unique to
        // NineSlice - Sprite has had its own, much older "Animate" variable since long before
        // GumNineSliceHasAnimate existed (SpriteCodeGenerator/Sprite.gutx). Gating the name globally here
        // silently suppressed Sprite's Animate state assignment too, for any Sprite instance nested inside
        // a Component, below this version. The NineSlice-only version gate now lives in
        // NineSliceCodeGenerator.AddTypeSpecificVariableNamesToSkipForStates instead.

        if (version >= (int)GluxVersions.GumTextHasIsBold)
        {
            // We can include this one, it's handled by
            Include("IsBold");
        }
        else
        {
            Skip("IsBold");
        }

        _textCodeGenerator.AddVariableNamesToSkipForStates(mVariableNamesToSkipForStates);
        _containerCodeGenerator.AddVariableNamesToSkipForStates(mVariableNamesToSkipForStates);

        return;

        void Include(string variable)
        {
            if (mVariableNamesToSkipForStates.Contains(variable))
            {
                mVariableNamesToSkipForStates.Remove(variable);
            }
        }
        void Skip(string variable)
        {
            if (!mVariableNamesToSkipForStates.Contains(variable))
            {
                mVariableNamesToSkipForStates.Add(variable);
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
                    foreach (var state in elementSave.States)
                    {
                        innerIf.Line($"if (state.Name == \"{state.Name}\") this.mCurrentVariableState = VariableState.{state.MemberNameInCode()};");
                    }
                }
                foreach (var category in elementSave.Categories)
                {
                    var elseIf = ifStatement.ElseIf($"category.Name == \"{category.Name}\"");
                    {
                        foreach (var state in category.States)
                        {
                            elseIf.Line($"if(state.Name == \"{state.Name}\") this.mCurrent{category.Name}State = {category.EnumNameInCode()}.{state.MemberNameInCode()};");
                        }
                    }
                }
            }

            currentBlock.Line("base.ApplyState(state);");
        }
    }

    #region Generate Enums

    public bool SupportsEnumsInInterfaces => GlueState.Self.CurrentMainProject?.DotNetVersion?.Major >= 5;

    bool IsUncagetgorizedStateNew(IStateContainer stateContainer)
    {
        var isStandard = stateContainer is StandardElementSave;

        var toReturn = !isStandard;

        if (!isStandard && stateContainer is ScreenSave screenSave && string.IsNullOrEmpty(screenSave.BaseType))
        {
            toReturn = false;
        }
        return toReturn;
    }

    public void GenerateStateEnums(IStateContainer stateContainer, ICodeBlock currentBlock, string enumNamePrefix = null)
    {
        bool hasUncategorized = (stateContainer is BehaviorSave) == false;

        var canGenerate = true;

        if (stateContainer is BehaviorSave)
        {
            // If it's a behavior, we need to make sure we are on a version of C# that supports enums in interfaces
            canGenerate = SupportsEnumsInInterfaces;
        }


        if (canGenerate)
        {
            currentBlock.Line("#region State Enums");

            if (hasUncategorized)
            {
                string categoryName = "VariableState";
                var states = stateContainer.UncategorizedStates;

                var newAndSpace = IsUncagetgorizedStateNew(stateContainer) ? "new " : string.Empty;

                GenerateEnumsForCategory(currentBlock, categoryName, states, newAndSpace);
            }
            // loop through categories:
            foreach (var category in stateContainer.Categories)
            {
                string categoryName = enumNamePrefix + category.EnumNameInCode();
                var states = category.States;
                GenerateEnumsForCategory(currentBlock, categoryName, states, string.Empty);

            }

            currentBlock.Line("#endregion");
        }

    }

    public void GenerateEnumsForCategory(ICodeBlock codeBlock, string categoryName, IEnumerable<StateSave> states, string newAndSpace)
    {
        var enumBlock = codeBlock.Enum("public " + newAndSpace, categoryName);

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

    #endregion

    #region Fields (aka StateType mCurrentStateTypeState)
    private void GenerateCurrentStateFields(ElementSave elementSave, ICodeBlock currentBlock)
    {
        currentBlock.Line("#region State Fields");
        string propertyName = "CurrentVariableState";
        string propertyType = "VariableState";
        currentBlock.Line(propertyType + " m" + propertyName + ";");


        foreach (var category in elementSave.Categories)
        {
            propertyName = "Current" + category.Name + "State";
            propertyType = category.EnumNameInCode();

            // Make these nullable because categorized states may not be set at all
            currentBlock.Line($"{propertyType}? m{propertyName};");
        }
        currentBlock.Line("#endregion");
    }
    #endregion

    #region Properties (which contain all the individual setters

    private void GeneratePropertyForCurrentState(ICodeBlock currentBlock, string propertyType, string propertyName,
        List<Gum.DataTypes.Variables.StateSave> states, ElementSave container, bool isNullable, string newAndSpace)
    {
        string propertyPrefix;

        if (isNullable)
        {
            propertyPrefix = $"public {newAndSpace}{propertyType}?";
        }
        else
        {
            propertyPrefix = $"public {newAndSpace}{propertyType}";
        }
        var property = currentBlock.Property(propertyPrefix, propertyName);

        property.Get().Line("return m" + propertyName + ";");

        var setter = property.Set();
        {
            setter = GenerateSetterForCurrentState(propertyType, propertyName, states, container, isNullable, setter);
        }
    }

    private ICodeBlock GenerateSetterForCurrentState(string propertyType, string propertyName, List<StateSave> states, ElementSave container, bool isNullable, ICodeBlock setter)
    {
        if (isNullable)
        {
            setter = setter.If("value != null");
        }
        setter.Line($"var oldValue = m{propertyName};");
        setter.Line("m" + propertyName + " = value;");

        if (states.Count > 0)
        {
            var switchBlock = setter.Switch("m" + propertyName);

            var shouldIncludeSwitchBlock = false;
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
                            shouldGenerate = GetIfShouldGenerateVariableInStateSetter(variable, container);
                        }
                        catch (Exception e)
                        {
                            GlueCommands.Self.PrintError(e.ToString());
                        }
                        // where block doesn't debug well for some reason, so I unrolled it...
                        if (shouldGenerate)
                        {
                            shouldIncludeSwitchBlock = true;
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

            // even though this has variables, it could have all variables that are not generated;
            if (!shouldIncludeSwitchBlock)
            {
                switchBlock.Parent.BodyCodeLines.Remove(switchBlock);
            }
        }

        setter.If($"oldValue != m{propertyName}")
              .Line($"NotifyPropertyChanged();");

        return setter;
    }

    private void GenerateCurrentStateProperties(ElementSave elementSave, ICodeBlock currentBlock)
    {
        currentBlock.Line("#region State Properties");

        string propertyName = "CurrentVariableState";
        string propertyType = "VariableState";
        var states = elementSave.States;

        var isStandard = elementSave is StandardElementSave;

        //var newAndSpace = isStandard ? string.Empty : "new ";
        var newAndSpace = IsUncagetgorizedStateNew(elementSave) ? "new " : string.Empty;


        GeneratePropertyForCurrentState(currentBlock, propertyType, propertyName, states, elementSave, isNullable: false, newAndSpace: newAndSpace);

        if (elementSave.Categories.Count > 0)
        {
            var baseElement = ObjectFinder.Self.GetElementSave(elementSave.BaseType);

            foreach (var category in elementSave.Categories)
            {
                propertyName = "Current" + category.Name + "State";
                propertyType = category.EnumNameInCode();
                states = category.States;

                newAndSpace = baseElement?.Categories.Any(item => item.Name == category.Name) == true ? "new " : string.Empty;

                GeneratePropertyForCurrentState(currentBlock, propertyType, propertyName, states, elementSave, isNullable: true, newAndSpace: newAndSpace);
            }
        }

        currentBlock.Line("#endregion");
    }

    #endregion

    /// <summary>
    /// The name of the element a state variable actually sets a value on: the container itself for an
    /// unqualified variable, or the instance's base type for a "SomeInstance.SomeVariable" variable.
    /// Returns null when the instance can't be resolved, which the callers treat as "not a match".
    /// </summary>
    private static string GetVariableTargetElementName(Gum.DataTypes.Variables.VariableSave variable, ElementSave container)
    {
        if (string.IsNullOrEmpty(variable.SourceObject))
        {
            return container.Name;
        }

        return container.GetInstance(variable.SourceObject)?.BaseType;
    }

    /// <summary>
    /// Gum only sets IsCustomVariable on a custom variable's definition (in the owning element's Default state).
    /// A value for that variable in any other state, or in a derived element's Default state, is a plain
    /// VariableSave. So the flag on the variable itself can't tell a real custom variable apart from a stale
    /// leftover; the definition on the container or one of its base elements can.
    /// </summary>
    private static bool IsDefinedAsCustomVariableOnContainerOrBase(Gum.DataTypes.Variables.VariableSave variable, ElementSave container)
    {
        if (DefinesCustomVariable(container, variable.Name))
        {
            return true;
        }

        foreach (var baseElement in ObjectFinder.Self.GetBaseElements(container))
        {
            if (DefinesCustomVariable(baseElement, variable.Name))
            {
                return true;
            }
        }

        return false;

        static bool DefinesCustomVariable(ElementSave element, string variableName) =>
            element.DefaultState?.Variables.Any(item => item.IsCustomVariable && item.Name == variableName) == true;
    }

    /// <summary>
    /// False when the variable sets a standard-element variable FRB's runtime does not back
    /// (FrbBackedGumVariables). Custom and exposed variables belong to a component, not to a standard
    /// element, so they always pass. "State" and category variables are excluded by the caller.
    /// </summary>
    private static bool IsBackedIfStandardElementVariable(Gum.DataTypes.Variables.VariableSave variable, ElementSave container, string variableRootName)
    {
        var targetElement = string.IsNullOrEmpty(variable.SourceObject)
            ? container
            : ObjectFinder.Self.GetElementSave(container.GetInstance(variable.SourceObject)?.BaseType);

        if (targetElement == null || IsCustomOrExposedOnElementOrBase(targetElement, variableRootName))
        {
            return true;
        }

        var rootStandardElement = targetElement as StandardElementSave
            ?? ObjectFinder.Self.GetRootStandardElementSave(targetElement);

        return rootStandardElement == null
            || FrbBackedGumVariables.IsAssignableInState(rootStandardElement.Name, variableRootName);
    }

    private static bool IsCustomOrExposedOnElementOrBase(ElementSave element, string variableRootName)
    {
        return Defines(element) || ObjectFinder.Self.GetBaseElements(element).Any(Defines);

        bool Defines(ElementSave candidate) =>
            candidate is not StandardElementSave &&
            candidate.DefaultState?.Variables.Any(item =>
                item.ExposedAsName == variableRootName ||
                (item.IsCustomVariable && item.Name == variableRootName)) == true;
    }

    private bool GetIfShouldGenerateVariableInStateSetter(Gum.DataTypes.Variables.VariableSave variable, ElementSave container)
    {
        bool toReturn = true;

        string variableRootName = variable.GetRootName();



        if (variable.Value == null || !variable.SetsValue)
        {
            toReturn = false;
        }

        var isVariableState = variable.IsState(container, out var stateCategoryContainer, out var stateCategory);

        // A category owned by the container itself can't be set from inside its own states. A category inherited
        // from a base element can: a component deriving from Text picks Text's "ColorCategory" state in its Default state.
        var isInheritedCategoryState =
            isVariableState &&
            string.IsNullOrEmpty(variable.SourceObject) &&
            stateCategory != null &&
            stateCategoryContainer != null &&
            stateCategoryContainer != container &&
            // Gum keeps a value after its state is deleted; assigning it would not compile.
            stateCategory.States.Any(item => item.Name == variable.Value?.ToString());

        // states can't set states on this
        if (isVariableState && string.IsNullOrEmpty(variable.SourceObject) && !isInheritedCategoryState)
        {
            toReturn = false;
        }

        if (toReturn && mVariableNamesToSkipForStates.Contains(variableRootName))
        {
            toReturn = false;
        }

        // Issue #1967 / #1987 - mirrors StandardsCodeGenerator.GetIfShouldGenerateProperty's live check;
        // see the comment on rectangleVariablesToSkip above for why this isn't a static skip entry.
        // Keyed on the element the variable actually targets, not on the container: a Rectangle
        // *instance* inside a component ("HighlightRectangle.StrokeBlue") is the common case, and
        // gating on container.Name alone only ever covered RectangleRuntime's own generated file.
        if (toReturn &&
            GumPlugin.CodeGeneration.StandardsCodeGenerator.RectangleFillStrokeVariableNames.Contains(variableRootName) &&
            GetVariableTargetElementName(variable, container) == "Rectangle")
        {
            toReturn = GumPlugin.CodeGeneration.StandardsCodeGenerator.IsRectangleFillStrokeSupported;
        }



        if (toReturn && typeSpecificVariableNamesToSkipForStates.ContainsKey(container.Name))
        {
            var typeSpecificVariables = typeSpecificVariableNamesToSkipForStates[container.Name];

            if (typeSpecificVariables.Contains(variableRootName))
            {
                toReturn = false;
            }
        }

        // check this and any of its base types:
        var baseTypes = ObjectFinder.Self.GetBaseElements(container);

        foreach(var baseType in baseTypes)
        {
            if(toReturn && typeSpecificVariableNamesToSkipForStates.ContainsKey(baseType.Name))
            {
                var typeSpecificVariables = typeSpecificVariableNamesToSkipForStates[baseType.Name];
                if (typeSpecificVariables.Contains(variableRootName))
                {
                    toReturn = false;
                }
            }
        }

        bool hasSourceObject = !string.IsNullOrEmpty(variable.SourceObject);

        if (toReturn && hasSourceObject)
        {
            InstanceSave instanceSave = container.GetInstance(variable.SourceObject);

            if (instanceSave == null)
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


                if (toReturn && typeSpecificVariableNamesToSkipForStates.ContainsKey(baseElement.Name))
                {
                    var typeSpecificVariables = typeSpecificVariableNamesToSkipForStates[baseElement.Name];

                    if (typeSpecificVariables.Contains(variableRootName))
                    {
                        toReturn = false;
                    }
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
                    if (defaultState.ParentContainer == null)
                    {
                        baseElement.Initialize(null);
                    }

                    RecursiveVariableFinder rvf = new RecursiveVariableFinder(defaultState);

                    var foundVariable = rvf.GetVariable(variableRootName);

                    if (foundVariable == null)
                    {
                        // This doesn't exist anywhere in the inheritance chain, so we don't want to generate it:
                        toReturn = false;
                    }

                    if (isVariableState)
                    {
                        // see if the base type has this category. If so, see if the state name exists. If not, we should skip generation:
                        var category = baseElement.Categories.FirstOrDefault(item => item.Name == variable.Type);
                        var variableValueAsString = variable.Value?.ToString();

                        if (!string.IsNullOrEmpty(variableValueAsString) && category != null && !category.States.Any(item => item.Name == variableValueAsString))
                        {
                            // this references an invalid state. This can happen if a state is deleted from an entity
                            toReturn = false;
                        }
                    }
                }
            }
        }

        if (toReturn && !hasSourceObject)
        {
            if (isInheritedCategoryState || variable.IsCustomVariable || IsDefinedAsCustomVariableOnContainerOrBase(variable, container))
            {
                // all good, we can generate this:
                toReturn = true;
            }
            else
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
                    if(StandardElementsManager.Self.DefaultStates.ContainsKey(rootStandardElementSave.Name) == false)
                    {
                        var message = $"Error generating variable {variable} - could not find default state for {rootStandardElementSave.Name} " +
                            $"in the StandardElementsManager. Found the following\n:";

                        foreach(var kvp in StandardElementsManager.Self.DefaultStates)
                        {
                            message += kvp.Key + ", ";
                        }
                        throw new KeyNotFoundException(message);
                    }
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

                    var variableRootNameWithNoSpaces = variableRootName.Replace(" ", "");

                    bool wasMatchFound = variablesToCheck.Any(item => item.Name == variableRootNameWithNoSpaces || item.Name == variableRootName);
                    toReturn = wasMatchFound;
                }
            }
        }

        if (toReturn && !isVariableState && variableRootName != "State")
        {
            toReturn = IsBackedIfStandardElementVariable(variable, container, variableRootName);
        }

        // special case (for now)
        if (container.Name == "Svg")
        {
            if (variable.Name == "SourceFile")
            {
                toReturn = false;
            }
        }
        if (container.Name == "LottieAnimation")
        {
            if (variable.Name == "SourceFile")
            {
                toReturn = false;
            }
        }
        return toReturn;
    }

    private void GenerateGetCurrentValuesOnState(ElementSave elementSave, ICodeBlock currentBlock)
    {
        currentBlock.Line("#region Get Current Values on State");

        string categoryName = "VariableState";
        var states = elementSave.States;
        GenerateGetCurrentValuesOnStateForCategory(currentBlock, elementSave, categoryName, states, addValues: false);
        GenerateGetCurrentValuesOnStateForCategory(currentBlock, elementSave, categoryName, states, addValues: true);


        foreach (var category in elementSave.Categories)
        {
            var categoryType = category.EnumNameInCode();
            states = category.States;
            GenerateGetCurrentValuesOnStateForCategory(currentBlock, elementSave, categoryType, states, addValues: false);
            GenerateGetCurrentValuesOnStateForCategory(currentBlock, elementSave, categoryType, states, addValues: true);
        }

        currentBlock.Line("#endregion");
    }

    private void GenerateGetCurrentValuesOnStateForCategory(ICodeBlock currentBlock, ElementSave container, string categoryType, List<Gum.DataTypes.Variables.StateSave> states, bool addValues = false)
    {
        string methodName = "GetCurrentValuesOnState";

        if (addValues)
        {
            methodName = "AddToCurrentValuesWithState";
        }

        currentBlock = currentBlock.Function("private Gum.DataTypes.Variables.StateSave", methodName, categoryType + " state");

        currentBlock.Line("Gum.DataTypes.Variables.StateSave newState = new Gum.DataTypes.Variables.StateSave();");

        if (states.Count > 0)
        {
            var switchBlock = currentBlock.Switch("state");
            {
                foreach (var state in states)
                {
                    var caseBlock = switchBlock.Case(categoryType + "." + state.MemberNameInCode());
                    {
                        var instanceNames = container.Instances.Select(item => item.Name).ToList();
                        var orderedVariables = state.Variables
                            .Where(item => GetIfShouldGenerateVariableInStateSetter(item, container))
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


                                if (addValues && IsVariableNumeric(variable))
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

}
