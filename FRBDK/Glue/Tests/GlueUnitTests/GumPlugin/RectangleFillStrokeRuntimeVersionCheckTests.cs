using System;
using System.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers.Projects;
using Gum.DataTypes;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using GumPlugin.ErrorReporting;
using Microsoft.Build.Evaluation;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GumPluginTests;

// Issue #1967 bug #2 - nothing stopped a v3 gumx project from generating FilledStrokedRectangle
// codegen against a referenced GumCore.*.dll that predates that type. This pins the detection logic
// end-to-end against a real project file + a real (non-Gum) DLL reference, without needing a slow
// BuildSmoke engine build - GumRuntimeSyntaxVersionReaderTests.ReadVersion_RealBuiltGumCoreDll_Returns4
// covers the "reads a real Gum-stamped DLL correctly" half; this covers "wires that reader into a
// real project's references correctly".
[Collection(nameof(TaskManagerSequentialCollection))]
public class RectangleFillStrokeRuntimeVersionCheckTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GumProjectSave _originalGumProjectSave;
    private readonly string _tempProjectDirectory;

    public RectangleFillStrokeRuntimeVersionCheckTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGumProjectSave = Gum.Managers.ObjectFinder.Self.GumProjectSave;

        GlueState.Self.CurrentMainProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        Gum.Managers.ObjectFinder.Self.GumProjectSave = _originalGumProjectSave;

        try
        {
            Directory.Delete(_tempProjectDirectory, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    private void SetGumProjectVersion(int version)
    {
        Gum.Managers.ObjectFinder.Self.GumProjectSave = new GumProjectSave { Version = version };
    }

    // Points the test project's GumCore.DesktopGlNet6 reference at a real, valid, but non-Gum DLL
    // (mscorlib/System.Private.CoreLib) - proves the resolution reaches a real file and reads it via
    // GumRuntimeSyntaxVersionReader.ReadVersion, which correctly reports "no attribute" for it.
    private void AddStaleDllReference()
    {
        var sourceDll = typeof(object).Assembly.Location;
        var destDll = Path.Combine(_tempProjectDirectory, "StaleGumCore.dll");
        File.Copy(sourceDll, destDll, overwrite: true);

        var csprojPath = GlueState.Self.CurrentMainProject.FullFileName.FullPath;
        var content = File.ReadAllText(csprojPath);
        content = content.Replace("</Project>", $"""
              <ItemGroup>
                <Reference Include="GumCore.DesktopGlNet6">
                  <HintPath>StaleGumCore.dll</HintPath>
                </Reference>
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(csprojPath, content);
    }

    [Fact]
    public void GetIfCurrentlyFixed_NoGumProject_IsFixed()
    {
        Gum.Managers.ObjectFinder.Self.GumProjectSave = null;

        RectangleFillStrokeRuntimeVersionCheck.GetIfCurrentlyFixed().ShouldBeTrue();
    }

    [Fact]
    public void GetIfCurrentlyFixed_V2GumProject_IsFixed_RegardlessOfReferences()
    {
        SetGumProjectVersion((int)GumProjectSave.GumxVersions.AttributeVersion);
        AddStaleDllReference();

        RectangleFillStrokeRuntimeVersionCheck.GetIfCurrentlyFixed().ShouldBeTrue();
    }

    [Fact]
    public void GetIfCurrentlyFixed_V3GumProject_StaleDllReference_IsNotFixed()
    {
        SetGumProjectVersion((int)GumProjectSave.GumxVersions.ShapeVariableExpansion);
        AddStaleDllReference();

        RectangleFillStrokeRuntimeVersionCheck.GetIfCurrentlyFixed().ShouldBeFalse();
        RectangleFillStrokeRuntimeVersionCheck.DetectedRuntimeSyntaxVersion().ShouldBeNull();
    }

    [Fact]
    public void GetIfCurrentlyFixed_V3GumProject_Frb2Project_IsFixed_RegardlessOfReferences()
    {
        // GitHub issue #2172: FRB2 has no committed GumCore.*.dll to go stale, so this check does not
        // apply to it at all - without the Frb2Project early-out, DetectedRuntimeSyntaxVersion's
        // FRB1-only name lists never resolve an FRB2 engine reference, so this fired on every FRB2
        // project the moment its gumx hit version 3+ (the default for any project saved by current
        // Gum tooling).
        SetGumProjectVersion((int)GumProjectSave.GumxVersions.ShapeVariableExpansion);
        AddStaleDllReference();

        var csprojPath = GlueState.Self.CurrentMainProject.FullFileName.FullPath;
        GlueState.Self.CurrentMainProject = new Frb2Project(new Project(csprojPath, null, null, new ProjectCollection()));

        RectangleFillStrokeRuntimeVersionCheck.GetIfCurrentlyFixed().ShouldBeTrue();
    }

    [Fact]
    public void GetIfCurrentlyFixed_V3GumProject_NoResolvableReferenceAtAll_IsNotFixed()
    {
        // No Reference/PackageReference added at all - DetectedRuntimeSyntaxVersion can't resolve
        // anything, which must be treated the same as "too old", not silently passed.
        SetGumProjectVersion((int)GumProjectSave.GumxVersions.ShapeVariableExpansion);

        RectangleFillStrokeRuntimeVersionCheck.GetIfCurrentlyFixed().ShouldBeFalse();
    }

    [Fact]
    public void RectangleFillStrokeRuntimeVersionError_MessageMentionsDetectedVersionAndRemedy()
    {
        var error = new RectangleFillStrokeRuntimeVersionError(detectedVersion: null);

        error.Details.ShouldContain("GumSyntaxVersion");
        error.Details.ShouldContain("relink");
    }
}
