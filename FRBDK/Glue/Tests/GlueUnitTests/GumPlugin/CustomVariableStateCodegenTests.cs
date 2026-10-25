using System;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using StateSave = Gum.DataTypes.Variables.StateSave;
using StateSaveCategory = Gum.DataTypes.Variables.StateSaveCategory;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using GumPlugin.CodeGeneration;
using Shouldly;

namespace GlueUnitTests.GumPluginTests;

// Issue #1740 - a custom variable (Gum's "Add Variable") is flagged IsCustomVariable only on its definition
// in the owning element's Default state. A value for it inside any other state (a categorized state, an
// uncategorized non-default state) or inside a derived component's Default state is a plain VariableSave
// with IsCustomVariable == false, so StateCodeGenerator treated it as a stale/unknown variable and dropped
// it. Instances picking such a state (A.CurrentTypeState = Type._1) then never got the state's value.
[Collection(nameof(TaskManagerSequentialCollection))]
public class CustomVariableStateCodegenTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly bool _originalSynchronousMode;
    private readonly GumProjectSave _originalGumProjectSave;
    private readonly string _tempProjectDirectory;

    public CustomVariableStateCodegenTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject;
        _originalSynchronousMode = TaskManager.SynchronousMode;
        _originalGumProjectSave = Gum.Managers.ObjectFinder.Self.GumProjectSave;

        var vsProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
        GlueState.Self.CurrentMainProject = vsProject;

        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = new GlueProjectSave();
        TaskManager.SynchronousMode = true;

        Gum.Managers.ObjectFinder.Self.GumProjectSave = new GumProjectSave
        {
            FullFileName = System.IO.Path.Combine(_tempProjectDirectory, "GumProject", "GumProject.gumx"),
            Version = (int)GumProjectSave.GumxVersions.ShapeVariableExpansion
        };
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = _originalGlueProject;
        TaskManager.SynchronousMode = _originalSynchronousMode;
        Gum.Managers.ObjectFinder.Self.GumProjectSave = _originalGumProjectSave;

        try
        {
            System.IO.Directory.Delete(_tempProjectDirectory, recursive: true);
        }
        catch
        {
            // best-effort cleanup; a stray temp dir isn't worth failing the test over
        }
    }

    private static VariableSave Var(string name, string type, object value, bool isCustom = false)
        => new VariableSave { Name = name, Type = type, Value = value, SetsValue = true, IsCustomVariable = isCustom };

    /// <summary>
    /// Built the way Gum saves it: Comp defines custom string TestString ("Default"), has a category "Type" with
    /// states "1" ("State1") and "2" ("State2") and an uncategorized state "Alt" ("AltValue"). The values inside
    /// states are plain variables (IsCustomVariable false), as written by StateSave.SetValue.
    /// </summary>
    private ComponentSave BuildCompFixture()
    {
        var gumProject = Gum.Managers.ObjectFinder.Self.GumProjectSave;

        var container = new StandardElementSave { Name = "Container" };
        container.Initialize(Gum.Managers.StandardElementsManager.Self.GetDefaultStateFor("Container"));
        gumProject.StandardElements.Add(container);

        var componentStandard = new StandardElementSave { Name = "Component" };
        componentStandard.Initialize(Gum.Managers.StandardElementsManager.Self.GetDefaultStateFor("Component"));
        gumProject.StandardElements.Add(componentStandard);

        var comp = new ComponentSave { Name = "Comp", BaseType = "Container" };
        var compDefault = new StateSave { Name = "Default" };
        compDefault.Variables.Add(Var("TestString", "string", "Default", isCustom: true));
        compDefault.Variables.Add(new VariableSave { Name = "TypeState", Type = "Type", SetsValue = false });
        comp.States.Add(compDefault);

        var alt = new StateSave { Name = "Alt" };
        alt.Variables.Add(Var("TestString", "string", "AltValue"));
        // A leftover from a deleted/renamed variable: Gum keeps these around, and they must stay out of the code.
        alt.Variables.Add(Var("StaleVariable", "string", "Stale"));
        comp.States.Add(alt);

        var category = new StateSaveCategory { Name = "Type" };
        var state1 = new StateSave { Name = "1" };
        state1.Variables.Add(Var("TestString", "string", "State1"));
        var state2 = new StateSave { Name = "2" };
        state2.Variables.Add(Var("TestString", "string", "State2"));
        category.States.Add(state1);
        category.States.Add(state2);
        comp.Categories.Add(category);

        comp.Initialize(compDefault);
        gumProject.Components.Add(comp);
        return comp;
    }

    /// <summary>
    /// The shape from the issue: Comp instances A and B each have an explicit TestString override and a different
    /// Type state. Instance C picks a state but has no override.
    /// </summary>
    private ComponentSave BuildTestContainerFixture()
    {
        var gumProject = Gum.Managers.ObjectFinder.Self.GumProjectSave;

        var testContainer = new ComponentSave { Name = "TestContainer", BaseType = "Container" };
        foreach (var name in new[] { "A", "B", "C" })
        {
            testContainer.Instances.Add(new InstanceSave { Name = name, BaseType = "Comp" });
        }
        var defaultState = new StateSave { Name = "Default" };
        defaultState.Variables.Add(Var("A.TestString", "string", "OverrideA"));
        defaultState.Variables.Add(Var("A.TypeState", "Type", "1"));
        defaultState.Variables.Add(Var("B.TestString", "string", "OverrideB"));
        defaultState.Variables.Add(Var("B.TypeState", "Type", "2"));
        defaultState.Variables.Add(Var("C.TypeState", "Type", "1"));
        testContainer.States.Add(defaultState);
        testContainer.Initialize(defaultState);
        gumProject.Components.Add(testContainer);
        return testContainer;
    }

    /// <summary>A component inheriting Comp whose Default state restates the inherited custom variable.</summary>
    private ComponentSave BuildDerivedFixture()
    {
        var derived = new ComponentSave { Name = "DerivedComp", BaseType = "Comp" };
        var defaultState = new StateSave { Name = "Default" };
        defaultState.Variables.Add(Var("TestString", "string", "DerivedDefault"));
        derived.States.Add(defaultState);
        derived.Initialize(defaultState);
        Gum.Managers.ObjectFinder.Self.GumProjectSave.Components.Add(derived);
        return derived;
    }

    /// <summary>The text of one state property's setter, e.g. "public Type? CurrentTypeState".</summary>
    private static string GetStatePropertyText(string generatedSource, string propertyDeclaration)
    {
        var start = generatedSource.IndexOf(propertyDeclaration, StringComparison.Ordinal);
        start.ShouldBeGreaterThanOrEqualTo(0, $"{propertyDeclaration} was not generated:\n{generatedSource}");
        var end = generatedSource.IndexOf("NotifyPropertyChanged", start, StringComparison.Ordinal);
        end.ShouldBeGreaterThan(start);
        return generatedSource.Substring(start, end - start);
    }

    [Fact]
    public void CategorizedState_AssignsCustomVariableValue()
    {
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();
        var comp = BuildCompFixture();

        var source = GueDerivingClassCodeGenerator.Self.GenerateCodeFor(comp);

        var setter = GetStatePropertyText(source, "public Type? CurrentTypeState");
        var state1Index = setter.IndexOf("case  Type._1:", StringComparison.Ordinal);
        var state2Index = setter.IndexOf("case  Type._2:", StringComparison.Ordinal);
        state1Index.ShouldBeGreaterThanOrEqualTo(0, setter);
        state2Index.ShouldBeGreaterThan(state1Index, setter);

        setter.Substring(state1Index, state2Index - state1Index).ShouldContain("TestString = \"State1\"");
        setter.Substring(state2Index).ShouldContain("TestString = \"State2\"");
    }

    [Fact]
    public void UncategorizedNonDefaultState_AssignsCustomVariableValue()
    {
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();
        var comp = BuildCompFixture();

        var source = GueDerivingClassCodeGenerator.Self.GenerateCodeFor(comp);

        var setter = GetStatePropertyText(source, "public new VariableState CurrentVariableState");
        var altIndex = setter.IndexOf("case  VariableState.Alt:", StringComparison.Ordinal);
        altIndex.ShouldBeGreaterThanOrEqualTo(0, setter);
        setter.Substring(altIndex).ShouldContain("TestString = \"AltValue\"");
    }

    [Fact]
    public void StateVariableWithNoDefinitionAnywhere_IsStillNotGenerated()
    {
        // Gum keeps variables whose definition was deleted. Only a variable defined as a custom variable in the
        // element or one of its base elements may be treated as one.
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();
        var comp = BuildCompFixture();

        var source = GueDerivingClassCodeGenerator.Self.GenerateCodeFor(comp);

        source.ShouldNotContain("StaleVariable");
    }

    [Fact]
    public void DerivedComponentDefaultState_AssignsInheritedCustomVariableValue()
    {
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();
        BuildCompFixture();
        var derived = BuildDerivedFixture();

        var source = GueDerivingClassCodeGenerator.Self.GenerateCodeFor(derived);

        source.ShouldContain("TestString = \"DerivedDefault\"");
    }

    [Fact]
    public void InstancesWithOverrideAndState_AssignStateBeforeOverride()
    {
        // The issue's scenario. Gum resolves an explicit value set by the container on an instance ahead of the
        // value from the instance's own category state (InstanceSaveExtensionMethods.GetVariableFromThisOrBase), so
        // A must end up "OverrideA" and B "OverrideB": the state is applied first and the override last. C has
        // no override, so its value comes from state 1 through CompRuntime.CurrentTypeState.
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();
        BuildCompFixture();
        var testContainer = BuildTestContainerFixture();

        var source = GueDerivingClassCodeGenerator.Self.GenerateCodeFor(testContainer);

        var setter = GetStatePropertyText(source, "public new VariableState CurrentVariableState");

        IndexOf("A.CurrentTypeState = ").ShouldBeLessThan(IndexOf("A.TestString = \"OverrideA\""));
        IndexOf("B.CurrentTypeState = ").ShouldBeLessThan(IndexOf("B.TestString = \"OverrideB\""));
        setter.ShouldContain("C.CurrentTypeState = ");
        setter.ShouldNotContain("C.TestString");

        int IndexOf(string text)
        {
            var index = setter.IndexOf(text, StringComparison.Ordinal);
            index.ShouldBeGreaterThanOrEqualTo(0, $"{text} was not generated:\n{setter}");
            return index;
        }
    }
}
