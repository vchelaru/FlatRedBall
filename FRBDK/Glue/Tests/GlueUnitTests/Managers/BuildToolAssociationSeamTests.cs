using System;
using System.IO;
using EditorObjects.SaveClasses;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Managers;

// Regression from #2315, which moved GetIsFileOutOfDate into GlueCommon behind the
// BuildToolAssociationCore seam. The seam was assigned only inside BuildToolAssociationManager.Self's
// lazy getter, and nothing on the project-load path reads that getter before
// ProjectLoader.BuildAllOutOfDateFiles asks every built file (non-empty SourceFile) whether it is out of
// date - so a fresh Glue launch threw NullReferenceException on the first built file of any project.
// The GlueCommon tests can't see this: they pre-assign a fake seam in their setup.
//
// These tests drive the Glue-side GetIsBuiltFileOutOfDate (the exact call BuildIfOutOfDate makes) after
// only the startup wiring, never touching BuildToolAssociationManager.Self themselves, so an unwired seam
// fails here instead of in the tool.
public class BuildToolAssociationSeamTests : IDisposable
{
    readonly VisualStudioProject _originalMainProject;
    readonly GlueSettingsSave _originalSettings;
    readonly IBuildToolAssociationCore _originalSeam;
    readonly string _projectDirectory;

    public BuildToolAssociationSeamTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalSettings = GlueState.Self.GlueSettingsSave;
        _originalSeam = BuildToolAssociationCore.Self;

        GlueState.Self.CurrentMainProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _projectDirectory);
        GlueState.Self.GlueSettingsSave = new GlueSettingsSave();

        // Simulate a fresh Glue process: nothing has read BuildToolAssociationManager.Self yet, so the
        // seam holds only what startup wired. WireGlueCommonSeams is the same set of calls
        // MainGlueWindow makes before loading a project.
        BuildToolAssociationCore.Self = null;
        GlueTestBootstrap.WireGlueCommonSeams();
    }

    public void Dispose()
    {
        BuildToolAssociationCore.Self = _originalSeam;
        GlueState.Self.GlueSettingsSave = _originalSettings;
        GlueState.Self.CurrentMainProject = _originalMainProject;
        Directory.Delete(_projectDirectory, recursive: true);
    }

    static string WriteFile(string directory, string relativeName, DateTime lastWriteTime)
    {
        var fullName = Path.Combine(directory, relativeName);
        Directory.CreateDirectory(Path.GetDirectoryName(fullName)!);
        File.WriteAllText(fullName, "");
        File.SetLastWriteTime(fullName, lastWriteTime);
        return fullName;
    }

    // A built file whose destination is newer than its source is the case that reaches the seam: the
    // timestamp check alone can't answer, so GetIsFileOutOfDate asks the seam for the build tool.
    static ReferencedFileSave CreateUpToDateBuiltFile()
    {
        var contentDirectory = GlueState.Self.ContentDirectory;
        WriteFile(contentDirectory, "GlobalContent/Data.src", new DateTime(2020, 1, 1));
        WriteFile(contentDirectory, "GlobalContent/Data.bin", new DateTime(2020, 1, 2));

        return new ReferencedFileSave
        {
            Name = "GlobalContent/Data.bin",
            SourceFile = "GlobalContent/Data.src",
        };
    }

    [Fact]
    public void GetIsBuiltFileOutOfDate_UpToDateBuiltFile_AfterStartupWiringOnly_ReturnsFalse()
    {
        var rfs = CreateUpToDateBuiltFile();

        rfs.GetIsBuiltFileOutOfDate().ShouldBeFalse();
    }

    // Proves the seam startup wires is the real manager (reading the project's build tool associations),
    // not merely something non-null.
    [Fact]
    public void GetIsBuiltFileOutOfDate_BuildToolNewerThanBuiltFile_AfterStartupWiringOnly_ReturnsTrue()
    {
        var rfs = CreateUpToDateBuiltFile();
        WriteFile(GlueState.Self.CurrentMainProjectDirectory, "Tools/Builder.exe", new DateTime(2020, 1, 3));
        GlueState.Self.GlueSettingsSave.BuildToolAssociations.Add(new BuildToolAssociation
        {
            BuildTool = "Tools/Builder.exe",
            SourceFileType = "src",
            DestinationFileType = "bin",
        });

        rfs.GetIsBuiltFileOutOfDate().ShouldBeTrue();
    }
}
