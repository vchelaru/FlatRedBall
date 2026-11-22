using System;
using System.IO;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using GlueUnitTests.TestSupport;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace GlueUnitTests.IO;

// Project load reads the .gluj and its element files on another thread while the .csproj is evaluated.
// Loading from that prefetch has to give the same project as reading the files in Load.
public class GlueProjectPrefetchTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _originalRelativeDirectory;
    private readonly string _tempProjectDirectory;

    public GlueProjectPrefetchTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        _originalRelativeDirectory = FileManager.RelativeDirectory;

        GlueState.Self.CurrentMainProject =
            TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
        FileManager.RelativeDirectory = _tempProjectDirectory + "\\";
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        FileManager.RelativeDirectory = _originalRelativeDirectory;

        try { Directory.Delete(_tempProjectDirectory, recursive: true); } catch { }
    }

    FilePath SaveProject(string screenName, string entityName, string glujPath)
    {
        var project = new GlueProjectSave { FileVersion = GlueProjectSave.LatestVersion };
        project.Screens.Add(new ScreenSave { Name = "Screens\\" + screenName });
        var entity = new EntitySave { Name = "Entities\\" + entityName };
        entity.CustomVariables.Add(new CustomVariable { Name = "Health", Type = "float" });
        project.Entities.Add(entity);
        ObjectFinder.Self.GlueProject = project;

        project.Save("GLUE", glujPath, out var saveException).ShouldBeTrue(saveException?.ToString());
        return new FilePath(glujPath);
    }

    [Fact]
    public void Load_ShouldGiveTheSameProject_WithOrWithoutPrefetch()
    {
        var gluj = SaveProject("Level1", "Boss", Path.Combine(_tempProjectDirectory, "Game.gluj"));

        var read = GlueProjectSaveExtensions.Load(gluj);
        var fromPrefetch = GlueProjectSaveExtensions.Load(gluj, GlueProjectSaveExtensions.Prefetch(gluj));

        fromPrefetch.Screens.Count.ShouldBe(1);
        fromPrefetch.Entities.Count.ShouldBe(1);
        JsonConvert.SerializeObject(fromPrefetch).ShouldBe(JsonConvert.SerializeObject(read));
    }

    [Fact]
    public void Load_ShouldReadTheFile_WhenThePrefetchIsOfAnotherFile()
    {
        var other = SaveProject("OtherScreen", "OtherEntity", Path.Combine(_tempProjectDirectory, "Other", "Other.gluj"));
        var prefetchOfOther = GlueProjectSaveExtensions.Prefetch(other);
        var gluj = SaveProject("Level1", "Boss", Path.Combine(_tempProjectDirectory, "Game.gluj"));

        var loaded = GlueProjectSaveExtensions.Load(gluj, prefetchOfOther);

        loaded.Screens.ShouldHaveSingleItem().Name.ShouldBe("Screens\\Level1");
        loaded.Entities.ShouldHaveSingleItem().Name.ShouldBe("Entities\\Boss");
    }

    [Fact]
    public void Prefetch_ShouldReturnNull_WhenThereIsNoGluj()
    {
        GlueProjectSaveExtensions.Prefetch(new FilePath(Path.Combine(_tempProjectDirectory, "Missing.gluj"))).ShouldBeNull();
    }
}
