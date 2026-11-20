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

// A component deriving from a standard element (Text) whose Default state sets a category state that the
// standard element owns (Text's ColorCategory) emitted nothing for it, so the runtime kept the defaults.
// The same variable on an instance inside a container was emitted correctly.
[Collection(nameof(TaskManagerSequentialCollection))]
public class InheritedCategoryStateCodegenTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly bool _originalSynchronousMode;
    private readonly GumProjectSave _originalGumProjectSave;
    private readonly string _tempProjectDirectory;

    public InheritedCategoryStateCodegenTests()
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

        // See CustomVariableStateCodegenTests: Gum's ObjectFinder cache is process-wide and goes stale.
        Gum.Managers.ObjectFinder.Self.ForceResetCache();
        Gum.Managers.ObjectFinder.Self.DisableCache();
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

    private static VariableSave Var(string name, string type, object value)
        => new VariableSave { Name = name, Type = type, Value = value, SetsValue = true };

    /// <summary>Text owns a "ColorCategory" (White, Gold), the way a styled Gum project's Text.gutx does.</summary>
    private void BuildTextWithColorCategory()
    {
        var gumProject = Gum.Managers.ObjectFinder.Self.GumProjectSave;

        var text = new StandardElementSave { Name = "Text" };
        text.Initialize(Gum.Managers.StandardElementsManager.Self.GetDefaultStateFor("Text"));

        var category = new StateSaveCategory { Name = "ColorCategory" };
        category.States.Add(new StateSave { Name = "White" });
        category.States.Add(new StateSave { Name = "Gold" });
        text.Categories.Add(category);
        gumProject.StandardElements.Add(text);

        var componentStandard = new StandardElementSave { Name = "Component" };
        componentStandard.Initialize(Gum.Managers.StandardElementsManager.Self.GetDefaultStateFor("Component"));
        gumProject.StandardElements.Add(componentStandard);
    }

    private static ComponentSave BuildTextComponent(string colorCategoryStateValue)
    {
        var comp = new ComponentSave { Name = "Announcement", BaseType = "Text" };
        var defaultState = new StateSave { Name = "Default" };
        defaultState.Variables.Add(Var("ColorCategoryState", "ColorCategory", colorCategoryStateValue));
        comp.States.Add(defaultState);
        comp.Initialize(defaultState);
        Gum.Managers.ObjectFinder.Self.GumProjectSave.Components.Add(comp);
        return comp;
    }

    [Fact]
    public void ComponentDerivingFromText_DefaultState_AppliesInheritedCategoryState()
    {
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();
        BuildTextWithColorCategory();
        var comp = BuildTextComponent("Gold");

        var source = GueDerivingClassCodeGenerator.Self.GenerateCodeFor(comp);

        source.ShouldContain("CurrentColorCategoryState = ");
        source.ShouldContain("ColorCategory.Gold");
    }

    [Fact]
    public void ComponentOwnCategory_SettingItselfFromItsOwnState_IsStillNotGenerated()
    {
        // The rule the inherited-category fix must not loosen: a state of the component's own category
        // can't assign that same category (it would recurse into its own setter).
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();
        BuildTextWithColorCategory();

        var comp = new ComponentSave { Name = "Announcement", BaseType = "Text" };
        var defaultState = new StateSave { Name = "Default" };
        defaultState.Variables.Add(new VariableSave { Name = "MyCategoryState", Type = "MyCategory", SetsValue = false });
        comp.States.Add(defaultState);

        var category = new StateSaveCategory { Name = "MyCategory" };
        var first = new StateSave { Name = "First" };
        first.Variables.Add(Var("MyCategoryState", "MyCategory", "Second"));
        category.States.Add(first);
        category.States.Add(new StateSave { Name = "Second" });
        comp.Categories.Add(category);

        comp.Initialize(defaultState);
        Gum.Managers.ObjectFinder.Self.GumProjectSave.Components.Add(comp);

        var source = GueDerivingClassCodeGenerator.Self.GenerateCodeFor(comp);

        // The public property assigns "CurrentMyCategoryState = ..."; the field helpers assign "mCurrentMyCategoryState = ...",
        // so require that no character precedes "Current" other than a non-identifier one.
        System.Text.RegularExpressions.Regex
            .IsMatch(source, @"(?<![A-Za-z_])CurrentMyCategoryState = [^;]*Second;")
            .ShouldBeFalse(source);
    }

    [Fact]
    public void ComponentDerivingFromText_InheritedCategoryStateThatNoLongerExists_IsNotGenerated()
    {
        // Gum keeps a value around after its state is deleted; assigning it would not compile.
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();
        BuildTextWithColorCategory();
        var comp = BuildTextComponent("DeletedState");

        var source = GueDerivingClassCodeGenerator.Self.GenerateCodeFor(comp);

        source.ShouldNotContain("DeletedState");
    }
}
