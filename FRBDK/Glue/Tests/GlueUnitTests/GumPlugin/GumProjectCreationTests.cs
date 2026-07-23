using System;
using System.IO;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using GumPlugin.Managers;
using Shouldly;

// Namespace is "GumPluginTests", not "GumPlugin": the real Gum plugin project's root namespace is the bare
// (non-nested) "GumPlugin", so nesting a same-named namespace under GlueUnitTests would shadow it for every
// file under GlueUnitTests.* (C# resolves unqualified names against enclosing namespaces before usings),
// silently breaking unrelated "GumPlugin.X" references elsewhere in this test project.
namespace GlueUnitTests.GumPluginTests;

// Follow-up to GitHub issue #1894 (reopened): WizardProjectLogic.HandleAddGum's two call sites
// (~line 279/283) go through PluginManager.CallPluginMethodAsync("Gum Plugin",
// "CreateGumProjectWithForms"/"CreateGumProjectNoForms", false), which silently no-ops in a test host
// (no plugin registered with PluginManager) - the same false-coverage risk PR #1901 fixed for the
// Collision Plugin's FixNamedObjectCollisionType.
//
// MainGumPlugin.CreateGumProjectWithForms/CreateGumProjectNoForms are already thin forwarders to the
// public NewGumProjectCreationLogic.CreateGumProjectInternal(shouldAlsoAddForms, askToOverwrite) - same
// "thin forwarder to directly-callable logic" shape as MainCollisionPlugin.FixNamedObjectCollisionType.
// NewGumProjectCreationLogic itself only depends on a GumxPropertiesManager (a plain class with a
// parameterless constructor, no UI dependency) - it does not need a live, StartUp'd MainGumPlugin
// instance at all. This test constructs NewGumProjectCreationLogic directly and calls
// CreateGumProjectInternal, bypassing both MainGumPlugin and PluginManager.CallPluginMethodAsync
// entirely - real production logic, not a fake.
[Collection(nameof(TaskManagerSequentialCollection))]
public class GumProjectCreationTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _originalRelativeDirectory;
    private readonly bool _originalSynchronousMode;
    private readonly IUiThreadMarshaller _originalMarshaller;
    private readonly Gum.DataTypes.GumProjectSave? _originalGumProjectSave;
    private readonly string _tempProjectDirectory;

    public GumProjectCreationTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        _originalRelativeDirectory = FlatRedBall.IO.FileManager.RelativeDirectory;
        _originalSynchronousMode = TaskManager.SynchronousMode;
        _originalMarshaller = TaskManager.UiThreadMarshaller;
        _originalGumProjectSave = Gum.Managers.ObjectFinder.Self.GumProjectSave;

        var vsProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);

        GlueState.Self.CurrentMainProject = vsProject;
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
        FlatRedBall.IO.FileManager.RelativeDirectory = _tempProjectDirectory + "\\";

        TaskManager.SynchronousMode = true;
        TaskManager.UiThreadMarshaller = new InlineUiThreadMarshaller();
        Gum.Managers.ObjectFinder.Self.GumProjectSave = null;
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        FlatRedBall.IO.FileManager.RelativeDirectory = _originalRelativeDirectory;
        TaskManager.SynchronousMode = _originalSynchronousMode;
        TaskManager.UiThreadMarshaller = _originalMarshaller;
        Gum.Managers.ObjectFinder.Self.GumProjectSave = _originalGumProjectSave;

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
    public async Task CreateGumProjectInternal_ShouldWriteGumxToDiskAndAddItToTheGlueProject_WhenNoFormsRequested()
    {
        // Mirrors WizardProjectLogic.HandleAddGum's "no forms" branch: askToOverwrite is always false at
        // that call site (there is nothing to overwrite for a brand-new project), and shouldAlsoAddForms
        // is false for CreateGumProjectNoForms.
        var creationLogic = new NewGumProjectCreationLogic(new GumxPropertiesManager());

        await creationLogic.CreateGumProjectInternal(shouldAlsoAddForms: false, askToOverwrite: false);

        // Real side effect #1: the .gumx (and its standard-element/font-cache siblings) were actually
        // written to disk by EmbeddedResourceManager.SaveEmptyProject - not just an in-memory object.
        // (Lands directly under the project's content folder + "GumProject/", not "Content/GumProject/" -
        // GlueState.ContentDirectory is empty for this minimal test .csproj, same as any real project with
        // no explicit content-folder override.)
        var expectedGumxPath = Path.Combine(_tempProjectDirectory, "GumProject", "GumProject.gumx");
        File.Exists(expectedGumxPath).ShouldBeTrue();

        // Real side effect #2: the .gumx was added to the Glue project as a real ReferencedFileSave -
        // exactly what GumProjectManager.GetIsGumProjectAlreadyInGlueProject checks for.
        var gumRfs = GumProjectManager.Self.GetRfsForGumProject();
        gumRfs.ShouldNotBeNull();
        gumRfs.Name.ShouldEndWith("GumProject.gumx");
        gumRfs.Properties.GetValue<bool>(nameof(global::GumPlugin.ViewModels.GumViewModel.AutoCreateGumScreens)).ShouldBeTrue();

        // Real side effect #3: the newly-saved .gumx was actually loaded back off disk into Gum's own
        // ObjectFinder (FileReferenceTracker.LoadGumxIfNecessaryFromDirectory), proving the file GumPlugin
        // wrote is a well-formed, loadable Gum project - not just bytes copied from an embedded resource.
        Gum.Managers.ObjectFinder.Self.GumProjectSave.ShouldNotBeNull();
        Gum.Managers.ObjectFinder.Self.GumProjectSave.FullFileName.ShouldBe(FlatRedBall.IO.FileManager.Standardize(expectedGumxPath));

        // Real side effect #4: CodeGeneratorManager.GenerateDerivedGueRuntimesAsync(forceReload: true) ran
        // for real at the end of CreateGumProjectInternal and generated actual Gum runtime code to disk -
        // proves this isn't just a file-copy step but drives Glue's real Gum code generation pipeline.
        Directory.GetFiles(_tempProjectDirectory, "GumIdb.Generated.cs", SearchOption.AllDirectories)
            .ShouldNotBeEmpty();

        // Deliberately NOT calling CreateGumProjectInternal a second time here to check the "already
        // exists" no-op branch: GumProjectManager.GetIsGumProjectAlreadyInGlueProject()==true routes to a
        // real, unfaked System.Windows.Forms.MessageBox.Show("A Gum project already exists") with no
        // GlueTestBootstrap seam (unlike MainGlueWindow/PluginManager, this dialog isn't behind any
        // interface) - calling it again would pop a real blocking dialog in a headless test host instead of
        // asserting anything. Single-call coverage above is sufficient to prove CreateGumProjectNoForms's
        // real behavior; the guard branch is simple enough (one bool check) not to need its own test at the
        // cost of a live MessageBox.
    }

    // CreateGumProjectInternal(shouldAlsoAddForms: true, ...) - the CreateGumProjectWithForms call site -
    // is NOT covered here. Driving it hits a genuine, out-of-scope blocker: it unconditionally calls
    // MainGumPlugin.HandleBuildMissingFonts, which spawns an external process at the hardcoded relative
    // path "Plugins/GumPlugin/Tools/GumProjectFontGenerator/GumProjectFontGenerator.exe" - only ever
    // deployed there by GumPlugin.csproj's PostBuild xcopy target into Glue.exe's own bin folder, not into
    // this test project's output directory. This isn't a PluginManager/MainGlueWindow coupling (the seam
    // shape every other blocker in this effort has been) - it's a real external-tool/file-layout dependency
    // with nothing to fake without either shipping a copy of the font generator into the test output or
    // extracting a seam around Process.Start itself. See REFACTORING.md for the full writeup; left
    // undone rather than forced.

    private class InlineUiThreadMarshaller : IUiThreadMarshaller
    {
        public void Invoke(Action action) => action();
        public T Invoke<T>(Func<T> func) => func();
        public Task Invoke(Func<Task> func) => func();
        public Task<T> Invoke<T>(Func<Task<T>> func) => func();
        public void BeginInvoke(Action action) => action();
    }
}
