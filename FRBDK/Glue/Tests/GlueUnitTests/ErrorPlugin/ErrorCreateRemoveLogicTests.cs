using System;
using System.IO;
using System.Linq;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using GlueUnitTests.TestSupport;
using OfficialPlugins.ErrorPlugin.Logic;
using OfficialPlugins.ErrorPlugin.ViewModels;
using Shouldly;
using Xunit;

namespace GlueUnitTests.ErrorPlugin;

// The missing-file and out-of-project scans skip a file whose reference tree an earlier file in the same
// scan already checked clean. A tree with a problem is still walked from every file that reaches it, so
// each one gets its own error, as before.
public class ErrorCreateRemoveLogicTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _tempProjectDirectory;
    private readonly string _outsideDirectory = Path.Combine(Path.GetTempPath(), "ErrorCreateRemoveLogicTests_" + Guid.NewGuid().ToString("N"));

    public ErrorCreateRemoveLogicTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;

        GlueState.Self.CurrentMainProject =
            TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
        Directory.CreateDirectory(_outsideDirectory);
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        try { Directory.Delete(_tempProjectDirectory, recursive: true); } catch { }
        try { Directory.Delete(_outsideDirectory, recursive: true); } catch { }
    }

    // Two screens reference the same bitmap font, whose page is texturePath (relative to the font).
    void SetUpTwoScreensSharingAFont(string texturePath)
    {
        var fontPath = Path.Combine(_tempProjectDirectory, "Font.fnt");
        File.WriteAllText(fontPath,
            "info face=\"Arial\" size=16\n" +
            "common lineHeight=16 base=13 scaleW=256 scaleH=256 pages=1\n" +
            $"page id=0 file=\"{texturePath}\"\n");

        var project = new GlueProjectSave();
        foreach (var name in new[] { "Screens\\First", "Screens\\Second" })
        {
            var screen = new ScreenSave { Name = name };
            screen.ReferencedFiles.Add(new ReferencedFileSave { Name = "Font.fnt", FilePath = new FilePath(fontPath) });
            project.Screens.Add(screen);
        }
        ObjectFinder.Self.GlueProject = project;
    }

    [Fact]
    public void GetAllErrors_ShouldReportAMissingTexture_ForEveryFileThatReachesIt()
    {
        SetUpTwoScreensSharingAFont("Missing.png");

        var errors = new ErrorCreateRemoveLogic().GetAllErrors();

        errors.OfType<IndirectMissingFileErrorViewModel>().Count().ShouldBe(2);
    }

    [Fact]
    public void GetAllErrors_ShouldReportAnOutOfProjectTexture_ForEveryFileThatReachesIt()
    {
        var outsideTexture = Path.Combine(_outsideDirectory, "Outside.png");
        File.WriteAllBytes(outsideTexture, new byte[] { 0 });
        SetUpTwoScreensSharingAFont(Path.GetRelativePath(_tempProjectDirectory, outsideTexture));

        var errors = new ErrorCreateRemoveLogic().GetAllErrors();

        errors.OfType<OutOfProjectErrorViewModel>().Count().ShouldBe(2);
        errors.OfType<IndirectMissingFileErrorViewModel>().ShouldBeEmpty();
    }

    [Fact]
    public void GetAllErrors_ShouldReportNothing_WhenTheSharedTreeIsClean()
    {
        File.WriteAllBytes(Path.Combine(_tempProjectDirectory, "Present.png"), new byte[] { 0 });
        SetUpTwoScreensSharingAFont("Present.png");

        var errors = new ErrorCreateRemoveLogic().GetAllErrors();

        errors.OfType<IndirectMissingFileErrorViewModel>().ShouldBeEmpty();
        errors.OfType<OutOfProjectErrorViewModel>().ShouldBeEmpty();
    }
}
