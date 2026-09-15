using System;
using System.IO;
using System.Linq;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin.GlueControl.CodeGeneration;
using GlueUnitTests.TestSupport;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GlueControl;

// Every embedded live edit file is regenerated on project load with the #defines for the project's
// FileVersion at its top, then compiled against whatever engine dll the project references. A call to an
// engine member that is not behind a `#if <GluxVersions gate>` reaches every project on an older engine as
// CS0117 on load. The BuildSmoke gold tests catch that by building a real project against a released
// engine; this is the fast half - it runs the real generator for one file and one FileVersion, then lets
// Roslyn evaluate the #if regions, so "is this member reference active at version N" is answered without a
// game project build.
public class EmbeddedVersionGateTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _tempProjectDirectory;

    public EmbeddedVersionGateTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;

        GlueState.Self.CurrentMainProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        try { Directory.Delete(_tempProjectDirectory, recursive: true); } catch { }
    }

    const string CameraLogic = "GameCommunicationPlugin.GlueControl.Embedded.Editing.CameraLogic.cs";
    const string ZoomLayerMember = "GetOrCreateEntityAttachmentZoomLayer";

    [Theory]
    [InlineData(72, false)]
    [InlineData(73, true)]
    public void CameraLogic_ZoomLayerCall_IsActiveOnlyFromVersion73(int fileVersion, bool expectActive)
    {
        var generated = Generate(CameraLogic, fileVersion);

        // Pins the fixture: the reference has to be in the file for "inactive" to mean "gated" rather than
        // "removed".
        generated.ShouldContain(ZoomLayerMember);

        IsActiveCode(generated, ZoomLayerMember, "HasGum").ShouldBe(expectActive);
    }

    // A project that references engine source always has the newest member, whatever its FileVersion says.
    [Fact]
    public void CameraLogic_ZoomLayerCall_IsActiveWhenReferencingFrbSource_RegardlessOfVersion()
    {
        var generated = Generate(CameraLogic, fileVersion: 61);

        IsActiveCode(generated, ZoomLayerMember, "HasGum", "REFERENCES_FRB_SOURCE").ShouldBeTrue();
    }

    static string Generate(string embeddedResource, int fileVersion)
    {
        ObjectFinder.Self.GlueProject = new GlueProjectSave { FileVersion = fileVersion };
        return GlueControlCodeGenerator.GetEmbeddedStringContents(embeddedResource);
    }

    // The generated file carries its own `#define` lines, which Roslyn honors when parsing; only symbols
    // that come from outside the file (HasGum is asked of the Gum plugin, REFERENCES_FRB_SOURCE of the
    // csproj) are passed in. A token inside a false #if region is disabled-text trivia, not a token.
    static bool IsActiveCode(string source, string identifier, params string[] externalSymbols)
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(preprocessorSymbols: externalSymbols));
        return tree.GetRoot().DescendantTokens().Any(token => token.Text == identifier);
    }
}
