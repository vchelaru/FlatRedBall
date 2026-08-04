using System;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Gum.DataTypes;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using GumPlugin.CodeGeneration;
using Shouldly;

namespace GlueUnitTests.GumPluginTests;

// Bug from #1958: Gum can supply a standard element's schema (e.g. "Line", via
// StandardElementsManager.GetLineState) that FRB has no backing runtime type for - "Line" is absent
// from StandardsCodeGenerator.mStandardElementToQualifiedTypes and LineRuntime does not exist anywhere
// in the repo. Before this fix, GueDerivingClassCodeGenerator.ShouldGenerateRuntimeFor returned true
// for every non-"Component" standard element regardless of whether FRB had a mapped backing type, so:
//  - GenerateStandardElementSaveCodeFor still tried to generate a Line runtime class, which threw
//    InvalidOperationException from CreateContainedObjectMembers (unmapped key).
//  - GueRuntimeTypeAssociationGenerator (a separate, unguarded-by-the-same-map path) still emitted
//    RegisterGueInstantiationType("Line", typeof(LineRuntime)) regardless, referencing a class that was
//    never generated - CS0234 in the user's project.
// Unlike GumStandardElementCodegenSweepTests's fixtures, "Line" isn't in StandardElementsManager's
// built-in mDefaults - it's a Skia-plugin-contributed standard (like Arc/Canvas/Svg/LottieAnimation),
// normally reachable only through CustomGetDefaultState once the SkiaGum plugin wires it up, which
// this test host doesn't load. StandardElementsManager.GetLineState() builds the same canonical schema
// directly, bypassing that plugin dispatch.
[Collection(nameof(TaskManagerSequentialCollection))]
public class UnmappedStandardElementCodegenTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly bool _originalSynchronousMode;
    private readonly Gum.DataTypes.GumProjectSave _originalGumProjectSave;
    private readonly string _tempProjectDirectory;

    public UnmappedStandardElementCodegenTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject;
        _originalSynchronousMode = TaskManager.SynchronousMode;
        _originalGumProjectSave = Gum.Managers.ObjectFinder.Self.GumProjectSave;

        var vsProject = TestSupport.TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
        GlueState.Self.CurrentMainProject = vsProject;

        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = new GlueProjectSave
        {
            FileVersion = GlueProjectSave.LatestVersion
        };
        TaskManager.SynchronousMode = true;
        Gum.Managers.ObjectFinder.Self.GumProjectSave = new Gum.DataTypes.GumProjectSave
        {
            FullFileName = System.IO.Path.Combine(_tempProjectDirectory, "GumProject", "GumProject.gumx"),
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

    [Fact]
    public void ShouldGenerateRuntimeFor_Line_ReturnsFalse()
    {
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        // Sanity check on the fixture: if Line ever gets a mapped backing type, this stops pinning
        // anything and should be revisited rather than silently passing for the wrong reason.
        StandardsCodeGenerator.Self.StandardElementToQualifiedTypes.ContainsKey("Line").ShouldBeFalse();

        var standardElementSave = new StandardElementSave { Name = "Line" };
        standardElementSave.Initialize(Gum.Managers.StandardElementsManager.GetLineState());

        GueDerivingClassCodeGenerator.Self.ShouldGenerateRuntimeFor(standardElementSave).ShouldBeFalse();
    }

    [Fact]
    public void GenerateStandardElementSaveCodeFor_Line_GeneratesNothingAndDoesNotThrow()
    {
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();

        var standardElementSave = new StandardElementSave { Name = "Line" };
        standardElementSave.Initialize(Gum.Managers.StandardElementsManager.GetLineState());

        var codeBlock = new CodeBlockBase();
        var generated = StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(standardElementSave, codeBlock);

        generated.ShouldBeFalse();
    }
}
