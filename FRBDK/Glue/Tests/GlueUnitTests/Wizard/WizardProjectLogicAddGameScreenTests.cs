using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.Managers;
using Glue;
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
// HandleAddGameScreen_ShouldAddGameScreenToProject_WithNoOptionalAddOns drives just that one sub-step
// directly. Apply_ShouldAddGameScreenGenerateCodeAndSaveProject_EndToEnd (below) drives the real
// WizardProjectLogic.Apply() with the same minimal WizardViewModel, covering the rest of Apply's
// unconditional pipeline too: "Generate all code" (GenerateAllCode, walks every element in the project),
// a "Flush Files" step with a hard-coded 2.5s+ real delay, and "Saving Project"
// (SaveProjectAndElements) - see REFACTORING.md's "Cover WizardProjectLogic.Apply() end-to-end" entry
// for the one new blocker that took (MainGlueWindow.Self.HasErrorOccurred NREing) and how it was fixed.
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

    [Fact]
    public async Task Apply_ShouldAddGameScreenGenerateCodeAndSaveProject_EndToEnd()
    {
        var vm = new WizardViewModel { AddGameScreen = true };

        await WizardProjectLogic.Self.Apply(vm);

        var glueProject = ObjectFinder.Self.GlueProject;
        var gameScreen = glueProject.Screens.FirstOrDefault(item => item.Name == @"Screens\GameScreen");
        gameScreen.ShouldNotBeNull();
        glueProject.StartUpScreen.ShouldBe(gameScreen.Name);

        var mainProject = GlueState.Self.CurrentMainProject;
        mainProject.CodeProject.IsFilePartOfProject(@"Screens\GameScreen.cs").ShouldBeTrue();
        mainProject.CodeProject.IsFilePartOfProject(@"Screens\GameScreen.Generated.cs").ShouldBeTrue();

        // "Regenerating All Code" should have written the generated code file to disk.
        var generatedCodeFile = Path.Combine(_tempProjectDirectory, "Screens", "GameScreen.Generated.cs");
        File.Exists(generatedCodeFile).ShouldBeTrue();

        // "Saving Project" (SaveProjectAndElements) should have written the project file to disk.
        // A fresh GlueProjectSave() defaults to FileVersion 0, so GlueProjectFileName resolves to
        // .glux (not .gluj - that requires GluxVersions.GlueSavedToJson or later).
        File.Exists(GlueState.Self.GlueProjectFileName.FullPath).ShouldBeTrue();
    }

    // AddSolidCollision/AddCloudCollision were blocked by two separate NREs, both now fixed:
    // AvailableAssetTypes.CommonAtis (see GlueTestBootstrap.EnsureInitialized) and
    // MainGlueWindow.Self.PropertyGrid.Refresh() (AddNewNamedObjectToAsync always runs with updateUi:true -
    // see IMainGlueWindow/FakeMainGlueWindow and REFACTORING.md). Covered end-to-end below.
    [Fact]
    public async Task HandleAddGameScreen_ShouldAddSolidAndCloudCollision_WhenRequested()
    {
        var vm = new WizardViewModel { AddGameScreen = true, AddSolidCollision = true, AddCloudCollision = true };

        var (gameScreen, solidCollisionNos, cloudCollisionNos) = await WizardProjectLogic.HandleAddGameScreen(vm);

        gameScreen.ShouldNotBeNull();
        solidCollisionNos.ShouldNotBeNull();
        solidCollisionNos.InstanceName.ShouldBe("SolidCollision");
        solidCollisionNos.SourceClassType.ShouldBe("FlatRedBall.TileCollisions.TileShapeCollection");
        gameScreen.NamedObjects.ShouldContain(solidCollisionNos);

        cloudCollisionNos.ShouldNotBeNull();
        cloudCollisionNos.InstanceName.ShouldBe("CloudCollision");
        gameScreen.NamedObjects.ShouldContain(cloudCollisionNos);

        // AddNamedObjectToAsync's updateUi:true path (MainGlueWindow.Self.PropertyGrid.Refresh()) ran
        // against the fake window instead of NRE-ing - this is the assertion that matters for #1894.
        ((FakeMainGlueWindow)MainGlueWindow.Self).PropertyGrid.ShouldNotBeNull();

        // Confirms code generation for the screen actually completed for both new objects.
        var generatedCodeFile = Path.Combine(_tempProjectDirectory, "Screens", "GameScreen.Generated.cs");
        File.Exists(generatedCodeFile).ShouldBeTrue();
    }

    private class InlineUiThreadMarshaller : IUiThreadMarshaller
    {
        public void Invoke(Action action) => action();
        public T Invoke<T>(Func<T> func) => func();
        public Task Invoke(Func<Task> func) => func();
        public Task<T> Invoke<T>(Func<Task<T>> func) => func();
        public void BeginInvoke(Action action) => action();
    }
}
