using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.SaveClasses;
using Shouldly;

namespace GlueUnitTests.IO;

// Pins UpdateReactor.BuildProjectDiffPlan - the classification logic ReloadGlux uses to decide
// whether an external edit to the .glux/.glsj/.glej can be applied as a single Screen/Entity/
// GlobalFile swap + per-element codegen, or whether it must fall back to a full project reload.
public class UpdateReactorTests
{
    static GlueProjectSave MakeProject(int screenCount = 0, int entityCount = 0, int globalFileCount = 0)
    {
        var project = new GlueProjectSave();
        for (int i = 0; i < screenCount; i++)
        {
            project.Screens.Add(new ScreenSave { Name = $"Screen{i}" });
        }
        for (int i = 0; i < entityCount; i++)
        {
            project.Entities.Add(new EntitySave { Name = $"Entity{i}" });
        }
        for (int i = 0; i < globalFileCount; i++)
        {
            project.GlobalFiles.Add(new ReferencedFileSave { Name = $"File{i}.png" });
        }
        return project;
    }

    [Fact]
    public void ShouldReturnNoDifferences_WhenThereAreNoDifferences()
    {
        var oldProject = MakeProject(screenCount: 1);
        var newProject = MakeProject(screenCount: 1);

        var plan = UpdateReactor.BuildProjectDiffPlan(Array.Empty<string>(), oldProject, newProject);

        plan.Outcome.ShouldBe(ProjectDiffOutcome.NoDifferences);
        plan.ElementsToReplace.ShouldBeEmpty();
        plan.GlobalFilesToReplace.ShouldBeEmpty();
    }

    [Fact]
    public void ShouldReplaceOnlyTheChangedScreen_WhenOneScreenPropertyDiffers()
    {
        var oldProject = MakeProject(screenCount: 2);
        var newProject = MakeProject(screenCount: 2);

        var plan = UpdateReactor.BuildProjectDiffPlan(
            new[] { "Screens[1].SomeProperty" }, oldProject, newProject);

        plan.Outcome.ShouldBe(ProjectDiffOutcome.Partial);
        plan.ElementsToReplace.Count.ShouldBe(1);
        plan.ElementsToReplace[0].Index.ShouldBe(1);
        plan.ElementsToReplace[0].OldElement.ShouldBeSameAs(oldProject.Screens[1]);
        plan.ElementsToReplace[0].ReplacementElement.ShouldBeSameAs(newProject.Screens[1]);
        plan.GlobalFilesToReplace.ShouldBeEmpty();
    }

    [Fact]
    public void ShouldReplaceEachChangedScreenOnce_WhenMultiplePropertiesOnTheSameScreenDiffer()
    {
        var oldProject = MakeProject(screenCount: 1);
        var newProject = MakeProject(screenCount: 1);

        var plan = UpdateReactor.BuildProjectDiffPlan(
            new[] { "Screens[0].PropertyA", "Screens[0].PropertyB" }, oldProject, newProject);

        plan.Outcome.ShouldBe(ProjectDiffOutcome.Partial);
        // Multiple diffs against the same screen collapse to a single replacement, not one per diff.
        plan.ElementsToReplace.Count.ShouldBe(1);
    }

    [Fact]
    public void ShouldReplaceOnlyTheChangedEntity_WhenOneEntityPropertyDiffers()
    {
        var oldProject = MakeProject(entityCount: 2);
        var newProject = MakeProject(entityCount: 2);

        var plan = UpdateReactor.BuildProjectDiffPlan(
            new[] { "Entities[0].SomeProperty" }, oldProject, newProject);

        plan.Outcome.ShouldBe(ProjectDiffOutcome.Partial);
        plan.ElementsToReplace.Count.ShouldBe(1);
        plan.ElementsToReplace[0].OldElement.ShouldBeSameAs(oldProject.Entities[0]);
        plan.ElementsToReplace[0].ReplacementElement.ShouldBeSameAs(newProject.Entities[0]);
    }

    [Fact]
    public void ShouldReplaceOnlyTheChangedGlobalFile_WhenOneGlobalFilePropertyDiffers()
    {
        var oldProject = MakeProject(globalFileCount: 2);
        var newProject = MakeProject(globalFileCount: 2);

        var plan = UpdateReactor.BuildProjectDiffPlan(
            new[] { "GlobalFiles[1].SomeProperty" }, oldProject, newProject);

        plan.Outcome.ShouldBe(ProjectDiffOutcome.Partial);
        plan.ElementsToReplace.ShouldBeEmpty();
        plan.GlobalFilesToReplace.Count.ShouldBe(1);
        plan.GlobalFilesToReplace[0].Index.ShouldBe(1);
        plan.GlobalFilesToReplace[0].OldFile.ShouldBeSameAs(oldProject.GlobalFiles[1]);
    }

    [Fact]
    public void ShouldRequireFullReload_WhenAnUnrecognizedProjectLevelPropertyDiffers()
    {
        var oldProject = MakeProject(screenCount: 1);
        var newProject = MakeProject(screenCount: 1);

        var plan = UpdateReactor.BuildProjectDiffPlan(
            new[] { "CustomGameClass" }, oldProject, newProject);

        plan.Outcome.ShouldBe(ProjectDiffOutcome.FullReloadRequired);
    }

    [Fact]
    public void ShouldRequireFullReload_WhenAScreenIsAddedOrRemoved()
    {
        // Simulates the diff CompareNetObjects reports when the Screens list Count changes - it
        // has no [index] suffix, so it can never collapse to a single element replacement.
        var oldProject = MakeProject(screenCount: 1);
        var newProject = MakeProject(screenCount: 2);

        var plan = UpdateReactor.BuildProjectDiffPlan(
            new[] { "Screens.Count" }, oldProject, newProject);

        plan.Outcome.ShouldBe(ProjectDiffOutcome.FullReloadRequired);
    }

    [Fact]
    public void ShouldStopAtTheFirstUnresolvableDifference_ButKeepEarlierReplacements()
    {
        var oldProject = MakeProject(screenCount: 2);
        var newProject = MakeProject(screenCount: 2);

        // Pins ReloadGlux's existing quirk: a difference list is processed in order, and the
        // replacements resolved before an unresolvable diff stay in the plan even though the
        // overall outcome is FullReloadRequired (ReloadGlux applies them anyway, then reloads).
        var plan = UpdateReactor.BuildProjectDiffPlan(
            new[] { "Screens[0].SomeProperty", "CustomGameClass", "Screens[1].SomeProperty" },
            oldProject, newProject);

        plan.Outcome.ShouldBe(ProjectDiffOutcome.FullReloadRequired);
        plan.ElementsToReplace.Count.ShouldBe(1);
        plan.ElementsToReplace[0].Index.ShouldBe(0);
    }

    [Fact]
    public void ShouldApplyStartUpScreenChange_WithoutRequiringFullReload()
    {
        var oldProject = MakeProject(screenCount: 2);
        var newProject = MakeProject(screenCount: 2);
        oldProject.StartUpScreen = "Screen0";
        newProject.StartUpScreen = "Screen1";

        var plan = UpdateReactor.BuildProjectDiffPlan(
            new[] { "StartUpScreen" }, oldProject, newProject);

        plan.Outcome.ShouldBe(ProjectDiffOutcome.Partial);
        plan.TopLevelPropertiesChanged.ShouldBe(new[] { "StartUpScreen" });
        plan.ElementsToReplace.ShouldBeEmpty();
        plan.GlobalFilesToReplace.ShouldBeEmpty();
    }

    [Fact]
    public void ShouldApplyStartUpScreenChangeAlongsideAScreenSwap_WhenBothDiffer()
    {
        var oldProject = MakeProject(screenCount: 2);
        var newProject = MakeProject(screenCount: 2);

        var plan = UpdateReactor.BuildProjectDiffPlan(
            new[] { "Screens[0].SomeProperty", "StartUpScreen" }, oldProject, newProject);

        plan.Outcome.ShouldBe(ProjectDiffOutcome.Partial);
        plan.ElementsToReplace.Count.ShouldBe(1);
        plan.TopLevelPropertiesChanged.ShouldBe(new[] { "StartUpScreen" });
    }
}
