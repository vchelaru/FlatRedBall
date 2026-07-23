using System;
using System.IO;
using System.Threading.Tasks;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using OfficialPlugins.Wizard.Managers;
using OfficialPlugins.Wizard.Models;
using Shouldly;

namespace GlueUnitTests.Wizard;

// Follow-up to GitHub issue #1894 / WizardProjectLogicTests: that first pass left
// WizardProjectLogic.Apply's "bare AddGameScreen" fallback step untested because
// GlueState.CurrentMainProject needs a real, MSBuild-backed VisualStudioProject and there was no
// fakeable interface (see REFACTORING.md's "Known Areas Needing Improvement" and the comment above
// WizardProjectLogic.Apply). Rather than build an IVisualStudioProject seam - a separate, much larger
// refactor whose need turned out to be avoidable - this drives a real VisualStudioProject backed by a
// minimal, non-SDK-style .csproj (TestVisualStudioProjectFactory), which doesn't require the
// SDK/MSBuildLocator resolution that only Glue.exe's own startup registers.
//
// Getting from there to a passing AddScreen call also required routing a previously-missed
// MainGlueWindow.Self.Invoke call (GlueCommands.DoOnUiThread) through the TaskManager.UiThreadMarshaller
// seam PR #1895 added - see REFACTORING.md - plus a handful of one-time bootstrap calls
// (GlueTestBootstrap) that mirror what Glue.exe's real startup does.
//
// WizardProjectLogic.Apply() itself is still not covered end-to-end: even with only AddGameScreen set,
// Apply() unconditionally also runs "Generate all code" (GenerateAllCode, walks every element in the
// project), a "Flush Files" step with a hard-coded 2.5s+ real delay, and "Saving Project" - a
// meaningfully larger surface than AddScreen alone, and not needed to satisfy issue #1894's stated
// fallback ("at minimum the AddGameScreen step"). HandleAddGameScreen is called directly here instead.
[Collection(nameof(TaskManagerSequentialCollection))]
public class WizardProjectLogicAddGameScreenTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _originalRelativeDirectory;
    private readonly bool _originalSynchronousMode;
    private readonly IUiThreadMarshaller _originalMarshaller;
    private readonly string _tempProjectDirectory;

    public WizardProjectLogicAddGameScreenTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        _originalRelativeDirectory = FlatRedBall.IO.FileManager.RelativeDirectory;
        _originalSynchronousMode = TaskManager.SynchronousMode;
        _originalMarshaller = TaskManager.UiThreadMarshaller;

        var vsProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);

        GlueState.Self.CurrentMainProject = vsProject;
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
        FlatRedBall.IO.FileManager.RelativeDirectory = _tempProjectDirectory + "\\";

        TaskManager.SynchronousMode = true;
        TaskManager.UiThreadMarshaller = new InlineUiThreadMarshaller();
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        FlatRedBall.IO.FileManager.RelativeDirectory = _originalRelativeDirectory;
        TaskManager.SynchronousMode = _originalSynchronousMode;
        TaskManager.UiThreadMarshaller = _originalMarshaller;

        try
        {
            Directory.Delete(_tempProjectDirectory, recursive: true);
        }
        catch
        {
            // best-effort cleanup; a stray temp dir isn't worth failing the test over
        }
    }

    [Fact]
    public async Task HandleAddGameScreen_ShouldAddGameScreenToProject_WithNoOptionalAddOns()
    {
        var vm = new WizardViewModel { AddGameScreen = true };

        var (gameScreen, solidCollisionNos, cloudCollisionNos) = await WizardProjectLogic.HandleAddGameScreen(vm);

        gameScreen.ShouldNotBeNull();
        gameScreen.Name.ShouldBe(@"Screens\GameScreen");
        gameScreen.Tags.ShouldContain("GLUE");
        solidCollisionNos.ShouldBeNull();
        cloudCollisionNos.ShouldBeNull();

        ObjectFinder.Self.GlueProject.Screens.ShouldContain(gameScreen);
        // First screen added becomes the startup screen - see ElementCommands.AddScreen.
        ObjectFinder.Self.GlueProject.StartUpScreen.ShouldBe(gameScreen.Name);

        var mainProject = GlueState.Self.CurrentMainProject;
        mainProject.CodeProject.IsFilePartOfProject(@"Screens\GameScreen.cs").ShouldBeTrue();
        mainProject.CodeProject.IsFilePartOfProject(@"Screens\GameScreen.Generated.cs").ShouldBeTrue();
    }

    // AddSolidCollision/AddCloudCollision were deliberately NOT added as a follow-up test here: they
    // route through NamedObjectSaveCodeGenerator.GetDestroyForNamedObject, which reads
    // AvailableAssetTypes.CommonAtis (a catalog populated by scanning every plugin's registered
    // AssetTypeInfos at real app startup - PluginManager loading, not something GlueTestBootstrap
    // reasonably replicates) and NREs when that catalog is empty. That's a materially bigger, separate
    // blocker from the VisualStudioProject one this test class exists to unblock - out of scope here.

    private class InlineUiThreadMarshaller : IUiThreadMarshaller
    {
        public void Invoke(Action action) => action();
        public T Invoke<T>(Func<T> func) => func();
        public Task Invoke(Func<Task> func) => func();
        public Task<T> Invoke<T>(Func<Task<T>> func) => func();
        public void BeginInvoke(Action action) => action();
    }
}
