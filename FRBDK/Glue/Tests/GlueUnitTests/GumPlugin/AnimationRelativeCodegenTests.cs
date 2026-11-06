using System;
using System.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.StateAnimation.SaveClasses;
using StateSave = Gum.DataTypes.Variables.StateSave;
using StateSaveCategory = Gum.DataTypes.Variables.StateSaveCategory;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using GumPlugin.CodeGeneration;
using Shouldly;

namespace GlueUnitTests.GumPluginTests;

// Spike for #2405: what does the generated "Relative" variant of a Gum animation look like?
[Collection(nameof(TaskManagerSequentialCollection))]
public class AnimationRelativeCodegenTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly bool _originalSynchronousMode;
    private readonly GumProjectSave _originalGumProjectSave;
    private readonly string _tempProjectDirectory;

    public AnimationRelativeCodegenTests()
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
            FullFileName = Path.Combine(_tempProjectDirectory, "GumProject", "GumProject.gumx"),
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
        try { Directory.Delete(_tempProjectDirectory, recursive: true); } catch { }
    }

    private ComponentSave BuildAnimatedComponent()
    {
        var gumProject = Gum.Managers.ObjectFinder.Self.GumProjectSave;

        var container = new StandardElementSave { Name = "Container" };
        container.Initialize(Gum.Managers.StandardElementsManager.Self.GetDefaultStateFor("Container"));
        gumProject.StandardElements.Add(container);

        var componentStandard = new StandardElementSave { Name = "Component" };
        componentStandard.Initialize(Gum.Managers.StandardElementsManager.Self.GetDefaultStateFor("Component"));
        gumProject.StandardElements.Add(componentStandard);

        var comp = new ComponentSave { Name = "Panel", BaseType = "Container" };
        var defaultState = new StateSave { Name = "Default" };
        comp.States.Add(defaultState);

        var category = new StateSaveCategory { Name = "Pos" };
        var start = new StateSave { Name = "Start" };
        start.Variables.Add(new VariableSave { Name = "Y", Type = "float", Value = -150f, SetsValue = true });
        var end = new StateSave { Name = "End" };
        end.Variables.Add(new VariableSave { Name = "Y", Type = "float", Value = 400f, SetsValue = true });
        category.States.Add(start);
        category.States.Add(end);
        comp.Categories.Add(category);
        comp.Initialize(defaultState);
        gumProject.Components.Add(comp);

        var animations = new ElementAnimationsSave { ElementName = "Panel" };
        var animation = new AnimationSave { Name = "SlideDown" };
        animation.States.Add(new AnimatedStateSave { StateName = "Pos/Start", Time = 0 });
        animation.States.Add(new AnimatedStateSave { StateName = "Pos/End", Time = 1.5f });
        animations.Animations.Add(animation);

        var animationFile = GumPlugin.Managers.AnimationLogic.GetAnimationFile(comp).FullPath;
        Directory.CreateDirectory(Path.GetDirectoryName(animationFile));
        animations.Save(animationFile);
        return comp;
    }

    [Fact]
    public void RelativeAnimation_IsGenerated_AndInterpolatesDeltaFromLiveValues()
    {
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();
        var comp = BuildAnimatedComponent();

        var source = GueDerivingClassCodeGenerator.Self.GenerateCodeFor(comp);

        // The Relative variant starts from the element's live values and adds the (End - Start) delta, so an
        // element positioned by the game (e.g. in a corner) plays the animation from there. It must not snap to
        // the Start state first, which is what the absolute variant does.
        var start = source.IndexOf("SlideDownAnimationRelativeInstructions (object target)", StringComparison.Ordinal);
        start.ShouldBeGreaterThanOrEqualTo(0, source);
        var relative = source.Substring(start, source.IndexOf("slideDownAnimation;", start, StringComparison.Ordinal) - start);

        relative.ShouldContain("SubtractFromThis(relativeEnd, relativeStart)");
        relative.ShouldContain("GetCurrentValuesOnState(Pos.End)");
        relative.ShouldContain("AddIntoThis(second, difference)");
        relative.ShouldNotContain("CurrentPosState =");

        source.ShouldContain("public FlatRedBall.Gum.Animation.GumAnimation SlideDownAnimationRelative");
    }
}
