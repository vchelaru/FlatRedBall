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

        // Real side effect #5: the new project does NOT seed the deprecated ColoredRectangle standard
        // element. Gum deprecated ColoredRectangle in favor of the plain Rectangle standard element
        // (upstream Gum #2965 phase 2, #2968, #2771) - StandardElementsManager.SeedableStandardTypes no
        // longer includes it there, but Glue's embedded new-project template was never updated to match
        // (GitHub issue #1931). Assert both that the loaded project doesn't reference it and that its
        // backing .gutx file was never written to disk, so this covers both the .gumx template and the
        // Embedded/EmptyProject/Standards file list in EmbeddedResourceManager.SaveEmptyProject.
        Gum.Managers.ObjectFinder.Self.GumProjectSave.StandardElementReferences
            .Any(reference => reference.Name == "ColoredRectangle")
            .ShouldBeFalse();
        Directory.GetFiles(_tempProjectDirectory, "ColoredRectangle.gutx", SearchOption.AllDirectories)
            .ShouldBeEmpty();

        // Deliberately NOT calling CreateGumProjectInternal a second time here to check the "already
        // exists" no-op branch: GumProjectManager.GetIsGumProjectAlreadyInGlueProject()==true routes to a
        // real, unfaked System.Windows.Forms.MessageBox.Show("A Gum project already exists") with no
        // GlueTestBootstrap seam (unlike MainGlueWindow/PluginManager, this dialog isn't behind any
        // interface) - calling it again would pop a real blocking dialog in a headless test host instead of
        // asserting anything. Single-call coverage above is sufficient to prove CreateGumProjectNoForms's
        // real behavior; the guard branch is simple enough (one bool check) not to need its own test at the
        // cost of a live MessageBox.
    }

    [Fact]
    public async Task CreateGumProjectInternal_SeedsGumxAtShapeVariableExpansionVersion_WithRectangleFillStrokeVariables()
    {
        // Issue #1967 - Glue's embedded new-project template was stamped at gumx Version 2
        // (AttributeVersion), one below the ShapeVariableExpansion (3) version that unlocks Gum's
        // Rectangle fill/stroke variable family. That meant every Glue-generated .gumx opened in the
        // standalone Gum Editor showed zero color variables for a Rectangle - Gum's canonical v3 schema
        // no longer defines legacy Red/Green/Blue/Alpha on Rectangle, and the version gate hid the
        // replacement Fill*/Stroke* family. This pins the fix: new projects seed at v3+, and the
        // Rectangle standard element's own .gutx carries the new variables so the Gum Editor's Variable
        // panel (which merges saved values with the canonical schema) has real data to show, not just an
        // unlocked but empty category.
        var creationLogic = new NewGumProjectCreationLogic(new GumxPropertiesManager());

        await creationLogic.CreateGumProjectInternal(shouldAlsoAddForms: false, askToOverwrite: false);

        Gum.Managers.ObjectFinder.Self.GumProjectSave.ShouldNotBeNull();
        Gum.Managers.ObjectFinder.Self.GumProjectSave!.Version
            .ShouldBeGreaterThanOrEqualTo((int)Gum.DataTypes.GumProjectSave.GumxVersions.ShapeVariableExpansion);

        var rectangleGutxPath = Directory.GetFiles(_tempProjectDirectory, "Rectangle.gutx", SearchOption.AllDirectories)
            .ShouldHaveSingleItem();
        var rectangleGutxContent = File.ReadAllText(rectangleGutxPath);

        foreach (var variableName in GumPlugin.CodeGeneration.StandardsCodeGenerator.RectangleFillStrokeVariableNames)
        {
            rectangleGutxContent.ShouldContain($"Name=\"{variableName}\"");
        }
    }

    [Fact]
    public async Task CreateGumProjectInternal_ShouldAddFormsComponentsAndGenerateFonts_WhenFormsRequested()
    {
        // Mirrors WizardProjectLogic.HandleAddGum's "with forms" branch (CreateGumProjectWithForms):
        // askToOverwrite is always false there too. shouldAlsoAddForms:true additionally routes through
        // FormsControlAdder.SaveElements/SaveBehaviors and MainGumPlugin.HandleBuildMissingFonts - the
        // real external GumProjectFontGenerator.exe spawn PR #1902 left uncovered. That tool is a real,
        // already-checked-in, headless console app (its own "About" doc: no XNA/window dependency, built
        // for exactly this "run it from another process" use case) - GlueUnitTests.csproj now mirrors
        // Glue.exe's own "Plugins/<PluginName>/Tools/..." deployment layout under this test project's
        // output directory (see the <None Include> item added alongside the GumPlugin ProjectReference),
        // so the real spawn - not a fake - runs here.
        GlueTestBootstrap.EnsureGumPluginStandardElementsInitialized();

        // Real projects get FileVersion = LatestVersion (ProjectLoader.cs sets this for every new/loaded
        // project), which routes FormsControlAdder.EmbeddedProjectRoot to "FormsGumProject" (sourced from
        // Gum's Templates/FormsTemplate, not FRB's own legacy EmbeddedObjectGumProject). Without this, the
        // constructor's default `new GlueProjectSave()` leaves FileVersion at 0, silently exercising the
        // legacy fallback path instead of the one every real new project actually takes - which is exactly
        // how issue #1933 (new Forms-enabled projects missing Button/CheckBox/ComboBox behaviors) slipped
        // through this test.
        ObjectFinder.Self.GlueProject.FileVersion = GlueProjectSave.LatestVersion;
        var creationLogic = new NewGumProjectCreationLogic(new GumxPropertiesManager());

        await creationLogic.CreateGumProjectInternal(shouldAlsoAddForms: true, askToOverwrite: false);

        // Real side effect #1: the .gumx was written and marked to include Forms in its generated code,
        // same as the "no forms" test's side effect #2, plus the two Forms-specific flags this branch sets.
        var gumRfs = GumProjectManager.Self.GetRfsForGumProject();
        gumRfs.ShouldNotBeNull();
        gumRfs.Properties.GetValue<bool>(nameof(global::GumPlugin.ViewModels.GumViewModel.IncludeFormsInComponents)).ShouldBeTrue();
        gumRfs.Properties.GetValue<bool>(nameof(global::GumPlugin.ViewModels.GumViewModel.IncludeComponentToFormsAssociation)).ShouldBeTrue();

        // Real side effect #2: FormsControlAdder.SaveElements actually wrote the default FRB Forms
        // .gucx components to disk under the Gum project's Components folder - not just flags flipped on
        // the RFS. The current FormsTemplate offers several Button variants (ButtonStandard, ButtonIcon,
        // ButtonTab, ...) rather than a single plain "Button.gucx" - assert the one FormsControlInfo
        // actually wires up as the default Button control.
        var componentsDirectory = Path.Combine(_tempProjectDirectory, "GumProject", "Components");
        var buttonGucx = Directory.GetFiles(componentsDirectory, "ButtonStandard.gucx", SearchOption.AllDirectories);
        buttonGucx.ShouldNotBeEmpty();

        // Real side effect #3: those components were also registered into the loaded GumProjectSave itself
        // (GumPluginCommands.Self.AddComponent), not just written to disk unreferenced.
        Gum.Managers.ObjectFinder.Self.GumProjectSave.ShouldNotBeNull();
        Gum.Managers.ObjectFinder.Self.GumProjectSave!.ComponentReferences
            .Any(reference => reference.Name.EndsWith("ButtonStandard"))
            .ShouldBeTrue();

        // Real side effect #4 (regression pin for #1933): FormsControlAdder.SaveBehaviors wrote every core
        // Forms behavior to disk under the Gum project's Behaviors folder - not just "some .behx file".
        // Upstream Gum commit dc8a204a3 (#4076) moved these 25 shared behaviors from
        // Templates/FormsTemplate/Behaviors into a new Templates/FormsBehaviors folder; GumPlugin.csproj's
        // embedded-resource globs weren't updated to also include the new location, so every new
        // Forms-enabled Gum project silently lost Button/CheckBox/ComboBox/etc. This asserts the specific
        // behaviors a real user found missing, not just "at least one .behx exists" (which the legacy
        // Standard-only behaviors satisfy on their own and would mask this exact regression).
        var behaviorsDirectory = Path.Combine(_tempProjectDirectory, "GumProject", "Behaviors");
        Directory.Exists(behaviorsDirectory).ShouldBeTrue();
        var coreBehaviorNames = new[]
        {
            "ButtonBehavior", "CheckBoxBehavior", "ComboBoxBehavior", "ListBoxBehavior",
            "RadioButtonBehavior", "ScrollViewerBehavior", "SliderBehavior", "TextBoxBehavior",
        };
        foreach (var behaviorName in coreBehaviorNames)
        {
            Directory.GetFiles(behaviorsDirectory, $"{behaviorName}.behx", SearchOption.AllDirectories)
                .ShouldNotBeEmpty($"{behaviorName}.behx should have been seeded into a new Forms-enabled project");
        }

        // Real side effect #5: MainGumPlugin.HandleBuildMissingFonts really spawned
        // GumProjectFontGenerator.exe against the newly-created project, and it really generated the fonts
        // the default Forms controls need (e.g. the Text standard element's default Arial fonts) - proven
        // by real .fnt bitmap-font files landing in the project's FontCache folder. The embedded project
        // template deliberately ships without any FontCache files (GumPlugin.csproj excludes
        // "EmbeddedObjectGumProject\FontCache\**" from its embedded resources), so any .fnt file found here
        // was generated by the real external tool, not copied from a template.
        var fontCacheDirectory = Path.Combine(_tempProjectDirectory, "GumProject", "FontCache");
        Directory.Exists(fontCacheDirectory).ShouldBeTrue();
        Directory.GetFiles(fontCacheDirectory, "*.fnt", SearchOption.AllDirectories).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task CreateGumProjectInternal_ShouldRegenerateCameraSetup_SoResetGumResolutionValuesExists()
    {
        // Issue #1972: Glue generates a call to CameraSetup.ResetGumResolutionValues() in
        // CameraLogic.Generated.cs (guarded by "#if HasGum", which MainGumPlugin.HasGum() computes fresh
        // on every codegen pass off Gum.Managers.ObjectFinder.Self.GumProjectSave - see
        // GlueControlCodeGenerator.cs). But CameraSetupCodeGenerator only emits the
        // ResetGumResolutionValues() method itself into CameraSetup.Generated.cs when
        // GetIfHasGumProject() is true, and that file is only regenerated by
        // CameraSetupCodeGenerator.UpdateOrAddCameraSetup() - wired to glux-load and display-setting-changed
        // events (CameraMainPlugin.cs), never to "a Gum project was just added". Repro: create empty
        // project -> add Gum project -> build. Result: CS0117, 'CameraSetup' has no definition for
        // 'ResetGumResolutionValues'.
        ObjectFinder.Self.GlueProject.FileVersion = GlueProjectSave.LatestVersion;
        ObjectFinder.Self.GlueProject.DisplaySettings = new FlatRedBall.Glue.SaveClasses.DisplaySettings();
        ObjectFinder.Self.GlueProject.DisplaySettings.SetDefaults();

        var cameraSetupGeneratedPath = Path.Combine(_tempProjectDirectory, "Setup", "CameraSetup.Generated.cs");

        // Simulates the real "project load" trigger (CameraMainPlugin.HandleLoadedGlux), which runs before
        // any Gum project exists for a brand-new FRB project.
        FlatRedBall.Glue.CodeGeneration.CameraSetupCodeGenerator.UpdateOrAddCameraSetup();

        File.Exists(cameraSetupGeneratedPath).ShouldBeTrue();
        File.ReadAllText(cameraSetupGeneratedPath).ShouldNotContain("ResetGumResolutionValues");

        var creationLogic = new NewGumProjectCreationLogic(new GumxPropertiesManager());
        await creationLogic.CreateGumProjectInternal(shouldAlsoAddForms: false, askToOverwrite: false);

        // CameraSetup.Generated.cs must be regenerated once a Gum project exists, so the method
        // CameraLogic.Generated.cs's "#if HasGum" branch calls actually exists.
        File.ReadAllText(cameraSetupGeneratedPath).ShouldContain("ResetGumResolutionValues");
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
