using System;
using System.IO;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.ExportedImplementations;

// A screen using global content cascades to its entities, but a new entity defaults to
// UseGlobalContent = false, which trips the duplicate-content-manager exception when it is
// later loaded from a screen that does not use global content. The project-wide
// DefaultNewEntitiesToUseGlobalContent setting makes new entities start as global.
[Collection(nameof(TaskManagerSequentialCollection))]
public class AddEntityGlobalContentDefaultTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _originalRelativeDirectory;
    private readonly bool _originalSynchronousMode;
    private readonly string _tempProjectDirectory;

    public AddEntityGlobalContentDefaultTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        _originalRelativeDirectory = FlatRedBall.IO.FileManager.RelativeDirectory;
        _originalSynchronousMode = TaskManager.SynchronousMode;

        var vsProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);

        GlueState.Self.CurrentMainProject = vsProject;
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
        FlatRedBall.IO.FileManager.RelativeDirectory = _tempProjectDirectory + "\\";

        TaskManager.SynchronousMode = true;
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        FlatRedBall.IO.FileManager.RelativeDirectory = _originalRelativeDirectory;
        TaskManager.SynchronousMode = _originalSynchronousMode;

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
    public void AddEntity_SettingOn_NewEntityUsesGlobalContent()
    {
        ObjectFinder.Self.GlueProject.GlobalContentSettingsSave.DefaultNewEntitiesToUseGlobalContent = true;

        var entity = GlueCommands.Self.GluxCommands.EntityCommands.AddEntity("Player", is2D: true);

        entity.UseGlobalContent.ShouldBeTrue();
    }

    [Fact]
    public void AddEntity_SettingOff_NewEntityDoesNotUseGlobalContent()
    {
        var entity = GlueCommands.Self.GluxCommands.EntityCommands.AddEntity("Player", is2D: true);

        entity.UseGlobalContent.ShouldBeFalse();
    }
}
