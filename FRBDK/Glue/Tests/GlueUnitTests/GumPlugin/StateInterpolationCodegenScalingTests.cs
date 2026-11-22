using System;
using System.Diagnostics;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using StateSave = Gum.DataTypes.Variables.StateSave;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using GumPlugin.CodeGeneration;
using Shouldly;

namespace GlueUnitTests.GumPluginTests;

// Generating the InterpolateBetween methods looked each variable up by scanning every variable generated so far,
// recomputing every member name on each scan, so a component's generation time grew with the square of its
// variable count (about 2 s of a 33 s Kid Defense load, 178 elements).
[Collection(nameof(TaskManagerSequentialCollection))]
public class StateInterpolationCodegenScalingTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly bool _originalSynchronousMode;
    private readonly GumProjectSave _originalGumProjectSave;
    private readonly string _tempProjectDirectory;

    public StateInterpolationCodegenScalingTests()
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

    /// <summary>A component with instanceCount Container instances whose X is set in a Default state and in a second state.</summary>
    private ComponentSave BuildComponent(int instanceCount, float xValue)
    {
        var gumProject = Gum.Managers.ObjectFinder.Self.GumProjectSave;

        foreach (var standardName in new[] { "Container", "Component" })
        {
            var standard = new StandardElementSave { Name = standardName };
            standard.Initialize(Gum.Managers.StandardElementsManager.Self.GetDefaultStateFor(standardName));
            gumProject.StandardElements.Add(standard);
        }

        var component = new ComponentSave { Name = "Big", BaseType = "Component" };
        var defaultState = new StateSave { Name = "Default" };
        var otherState = new StateSave { Name = "Other" };

        for (int i = 0; i < instanceCount; i++)
        {
            component.Instances.Add(new InstanceSave { Name = "Inst" + i, BaseType = "Container" });
            defaultState.Variables.Add(new VariableSave { Name = $"Inst{i}.X", Type = "float", Value = xValue, SetsValue = true });
            otherState.Variables.Add(new VariableSave { Name = $"Inst{i}.X", Type = "float", Value = xValue + 1, SetsValue = true });
        }

        component.States.Add(defaultState);
        component.States.Add(otherState);
        component.Initialize(defaultState);
        gumProject.Components.Add(component);
        return component;
    }

    [Fact]
    public void GenerateCodeFor_ComponentWithManyVariables_GeneratesInterpolationForEach()
    {
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();
        var component = BuildComponent(instanceCount: 3, xValue: 5);

        var source = GueDerivingClassCodeGenerator.Self.GenerateCodeFor(component);

        source.ShouldContain("bool setInst0XFirstValue = false;");
        source.ShouldContain("bool setInst2XSecondValue = false;");
        source.ShouldContain("Inst1.X = Inst1XFirstValue * (1 - interpolationValue) + Inst1XSecondValue * interpolationValue;");
    }

    [Fact]
    public void GenerateCodeFor_ChangedVariableValue_ChangesTheGeneratedCode()
    {
        // A cheaper generator must not turn into a stale one: editing an element has to change its output.
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();
        var component = BuildComponent(instanceCount: 3, xValue: 5);
        var before = GueDerivingClassCodeGenerator.Self.GenerateCodeFor(component);

        component.States[0].Variables.Find(item => item.Name == "Inst1.X").Value = 77f;
        var after = GueDerivingClassCodeGenerator.Self.GenerateCodeFor(component);

        before.ShouldNotContain("Inst1XFirstValue = 77");
        after.ShouldContain("Inst1XFirstValue = 77");
    }

    [Fact]
    public void GenerateCodeFor_ComponentWithManyVariables_GeneratesInTimeThatIsNotQuadratic()
    {
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        // A ratio of two sizes measured back to back, not a wall-clock bound, so a busy machine slows both
        // alike. Ten times the variables measured 46-79 times as long with the lookup by name (the rest of
        // component generation is not linear either) and about 350 times with the old rescan.
        TimeGeneration(instanceCount: 150);
        var small = TimeGeneration(instanceCount: 150);
        var large = TimeGeneration(instanceCount: 1500);

        var ratio = large / small;
        ratio.ShouldBeLessThan(150, $"150 variables: {small:F1} ms, 1500 variables: {large:F1} ms");
    }

    // Best of three, so one stall on a busy machine doesn't count.
    private double TimeGeneration(int instanceCount)
    {
        var best = double.MaxValue;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            Gum.Managers.ObjectFinder.Self.GumProjectSave = new GumProjectSave
            {
                FullFileName = System.IO.Path.Combine(_tempProjectDirectory, "GumProject", "GumProject.gumx"),
                Version = (int)GumProjectSave.GumxVersions.ShapeVariableExpansion
            };
            var component = BuildComponent(instanceCount, xValue: 5);

            var stopwatch = Stopwatch.StartNew();
            var source = GueDerivingClassCodeGenerator.Self.GenerateCodeFor(component);
            stopwatch.Stop();

            source.ShouldContain($"bool setInst{instanceCount - 1}XFirstValue = false;");
            best = Math.Min(best, stopwatch.Elapsed.TotalMilliseconds);
        }
        return best;
    }
}
