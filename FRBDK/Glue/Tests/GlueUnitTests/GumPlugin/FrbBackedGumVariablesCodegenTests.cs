using System;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using GumPlugin.CodeGeneration;
using Shouldly;
using StateSave = Gum.DataTypes.Variables.StateSave;

namespace GlueUnitTests.GumPluginTests;

// Issue #2404: codegen emits only the standard-element variables FRB's runtime backs
// (FrbBackedGumVariables). A variable Gum adds to its schema later must be left out of both the generated
// property and every state assignment, or every game using the element fails with CS1061.
[Collection(nameof(TaskManagerSequentialCollection))]
public class FrbBackedGumVariablesCodegenTests : IDisposable
{
    const string NewGumVariable = "SomeVariableGumHasNotInventedYet";

    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly bool _originalSynchronousMode;
    private readonly GumProjectSave _originalGumProjectSave;
    private readonly string _tempProjectDirectory;

    public FrbBackedGumVariablesCodegenTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject;
        _originalSynchronousMode = TaskManager.SynchronousMode;
        _originalGumProjectSave = Gum.Managers.ObjectFinder.Self.GumProjectSave;

        var vsProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
        GlueState.Self.CurrentMainProject = vsProject;

        // Latest, so every version gate is open and only the allowlist decides.
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = new GlueProjectSave
        {
            FileVersion = GlueProjectSave.LatestVersion,
        };
        TaskManager.SynchronousMode = true;

        Gum.Managers.ObjectFinder.Self.GumProjectSave = new GumProjectSave
        {
            FullFileName = System.IO.Path.Combine(_tempProjectDirectory, "GumProject", "GumProject.gumx"),
            Version = (int)GumProjectSave.GumxVersions.ShapeVariableExpansion
        };

        // See CustomVariableStateCodegenTests: the element-name cache is process-wide.
        Gum.Managers.ObjectFinder.Self.ForceResetCache();
        Gum.Managers.ObjectFinder.Self.DisableCache();

        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        AddStandardElement("Container");
        AddStandardElement("Component");
        AddStandardElement("Text", Var(NewGumVariable, "float", 0f));
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
            // best-effort cleanup
        }
    }

    [Fact]
    public void NewGumVariable_IsNotGeneratedAsAProperty()
    {
        var text = Gum.Managers.ObjectFinder.Self.GetStandardElement("Text");

        var source = GueDerivingClassCodeGenerator.Self.GenerateCodeFor(text);

        source.ShouldContain("public string Text");
        source.ShouldNotContain(NewGumVariable);
    }

    [Fact]
    public void NewGumVariable_IsNotAssignedOnAnInstanceInAState()
    {
        var comp = AddComponent("Comp", "Container",
            new[] { new InstanceSave { Name = "TextInstance", BaseType = "Text" } },
            Var("TextInstance.FontSize", "int", 12),
            Var("TextInstance." + NewGumVariable, "float", 1f));

        var source = GueDerivingClassCodeGenerator.Self.GenerateCodeFor(comp);

        source.ShouldContain("TextInstance.FontSize = ");
        source.ShouldNotContain(NewGumVariable);
    }

    // The reported case: Gum added IgnoredByParentLayout to every standard element, and no shipped FRB
    // GumCore's GraphicalUiElement has it.
    [Fact]
    public void IgnoredByParentLayout_IsNotAssignedOnAnInstanceInAState()
    {
        var comp = AddComponent("Comp", "Container",
            new[] { new InstanceSave { Name = "ContainerInstance", BaseType = "Container" } },
            Var("ContainerInstance.IgnoredByParentSize", "bool", true),
            Var("ContainerInstance.IgnoredByParentLayout", "bool", true));

        var source = GueDerivingClassCodeGenerator.Self.GenerateCodeFor(comp);

        source.ShouldContain("ContainerInstance.IgnoredByParentSize = true");
        source.ShouldNotContain("IgnoredByParentLayout");
    }

    // An exposed variable belongs to the component, not to its root standard element, so its name being
    // absent from Container's allowlist must not drop it.
    [Fact]
    public void ExposedVariable_IsStillAssignedOnAnInstanceInAState()
    {
        var button = AddComponent("Button", "Container",
            new[] { new InstanceSave { Name = "TextInstance", BaseType = "Text" } },
            new VariableSave { Name = "TextInstance.Text", Type = "string", Value = "Default", SetsValue = true, ExposedAsName = "Label" });

        var screen = AddComponent("Menu", "Container",
            new[] { new InstanceSave { Name = "ButtonInstance", BaseType = "Button" } },
            Var("ButtonInstance.Label", "string", "Play"));

        button.ShouldNotBeNull();
        var source = GueDerivingClassCodeGenerator.Self.GenerateCodeFor(screen);

        source.ShouldContain("ButtonInstance.Label = \"Play\"");
    }

    private static VariableSave Var(string name, string type, object value) =>
        new VariableSave { Name = name, Type = type, Value = value, SetsValue = true };

    private static void AddStandardElement(string name, params VariableSave[] extraVariables)
    {
        // Cloned: StandardElementsManager's default states are process-wide.
        var defaultState = Gum.Managers.StandardElementsManager.Self.GetDefaultStateFor(name).Clone();
        defaultState.Variables.AddRange(extraVariables);

        var element = new StandardElementSave { Name = name };
        element.States.Add(defaultState);
        element.Initialize(defaultState);
        Gum.Managers.ObjectFinder.Self.GumProjectSave.StandardElements.Add(element);
    }

    private static ComponentSave AddComponent(string name, string baseType, InstanceSave[] instances, params VariableSave[] defaultVariables)
    {
        var component = new ComponentSave { Name = name, BaseType = baseType };
        foreach (var instance in instances)
        {
            instance.ParentContainer = component;
            component.Instances.Add(instance);
        }

        var defaultState = new StateSave { Name = "Default" };
        defaultState.Variables.AddRange(defaultVariables);
        component.States.Add(defaultState);
        component.Initialize(defaultState);
        Gum.Managers.ObjectFinder.Self.GumProjectSave.Components.Add(component);
        return component;
    }
}
