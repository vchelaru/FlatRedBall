using System;
using System.IO;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.VSHelpers;

// The #defines written at the top of every embedded file come from the gluj FileVersion, and the engine
// dll a project references reports its own SyntaxVersion. A gate whose version is lower than the one the
// engine actually gained the member at compiles a call into a project whose engine lacks it: CS0117 on
// GlueControlManager.Generated.cs. ScreenManager.ScreenLoadExceptionOccurred landed after the version-70
// bump without one of its own, so 72 is the first version guaranteed to have it.
public class CodeBuildItemAdderVersionDefinesTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _tempProjectDirectory;

    public CodeBuildItemAdderVersionDefinesTests()
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

    [Theory]
    [InlineData(70, false)]
    [InlineData(71, false)]
    [InlineData(72, true)]
    public void GetGlueVersionsString_DefinesScreenLoadExceptionGate_OnlyFromVersion72(int fileVersion, bool expectDefined)
    {
        ObjectFinder.Self.GlueProject = new GlueProjectSave { FileVersion = fileVersion };

        var defines = CodeBuildItemAdder.GetGlueVersionsString();

        defines.Contains("#define ScreenManagerHasScreenLoadExceptionOccurred\n").ShouldBe(expectDefined);
    }
}
