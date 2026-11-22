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

    // Glue.exe starts reading the .gluj of the project it will open during startup (#2422), seconds before
    // LoadProject takes it, so the load has to drop that read when anything it read has changed since.
    FlatRedBall.Glue.IO.ProjectLoader StartEarlyReadsOfGame(out string csproj)
    {
        SaveProject("Level1", "Boss", Path.Combine(_tempProjectDirectory, "Game.gluj"));
        csproj = Path.Combine(_tempProjectDirectory, "Game.csproj");
        File.WriteAllText(csproj, "<Project />");

        var loader = FlatRedBall.Glue.IO.ProjectLoader.Self;
        _originalEarlyEvaluation ??= loader.EarlyEvaluation;
        // Only the .gluj read is under test; MSBuild isn't needed for it.
        loader.EarlyEvaluation = new FlatRedBall.Glue.VSHelpers.Projects.EarlyProjectEvaluation(_ => null!);
        loader.StartEarlyReads(csproj);
        return loader;
    }

    FlatRedBall.Glue.VSHelpers.Projects.EarlyProjectEvaluation? _originalEarlyEvaluation;

    void RestoreEarlyEvaluation()
    {
        if (_originalEarlyEvaluation != null)
        {
            FlatRedBall.Glue.IO.ProjectLoader.Self.DiscardEarlyReads();
            FlatRedBall.Glue.IO.ProjectLoader.Self.EarlyEvaluation = _originalEarlyEvaluation;
        }
    }

    [Fact]
    public void StartupRead_ShouldBeTakenByTheLoad_WhenNothingChanged()
    {
        try
        {
            var loader = StartEarlyReadsOfGame(out var csproj);

            var prefetched = FlatRedBall.Glue.IO.ProjectLoader.TakePrefetchIgnoringErrors(loader.TakeEarlyGlueProjectRead(csproj));

            prefetched.ShouldNotBeNull();
            GlueProjectSaveExtensions.Load(new FilePath(Path.Combine(_tempProjectDirectory, "Game.gluj")), prefetched)
                .Entities.ShouldHaveSingleItem().Name.ShouldBe("Entities\\Boss");
            // Taken once only.
            loader.TakeEarlyGlueProjectRead(csproj).ShouldBeNull();
        }
        finally
        {
            RestoreEarlyEvaluation();
        }
    }

    [Fact]
    public void StartupRead_ShouldBeDropped_WhenAnElementFileChangedSince()
    {
        try
        {
            var loader = StartEarlyReadsOfGame(out var csproj);
            var read = loader.TakeEarlyGlueProjectRead(csproj);
            read.ShouldNotBeNull();
            read.Wait();

            var entityFile = Directory.GetFiles(_tempProjectDirectory, "Boss.glej", SearchOption.AllDirectories).ShouldHaveSingleItem();
            File.SetLastWriteTimeUtc(entityFile, File.GetLastWriteTimeUtc(entityFile).AddSeconds(5));

            FlatRedBall.Glue.IO.ProjectLoader.TakePrefetchIgnoringErrors(read).ShouldBeNull();
        }
        finally
        {
            RestoreEarlyEvaluation();
        }
    }

    [Fact]
    public void StartupRead_ShouldNotBeTaken_ForAnotherProject()
    {
        try
        {
            var loader = StartEarlyReadsOfGame(out _);
            var other = Path.Combine(_tempProjectDirectory, "Other.csproj");
            File.WriteAllText(other, "<Project />");

            loader.TakeEarlyGlueProjectRead(other).ShouldBeNull();
        }
        finally
        {
            RestoreEarlyEvaluation();
        }
    }

    [Fact]
    public void Prefetch_ShouldReturnNull_WhenThereIsNoGluj()
    {
        GlueProjectSaveExtensions.Prefetch(new FilePath(Path.Combine(_tempProjectDirectory, "Missing.gluj"))).ShouldBeNull();
    }
}
