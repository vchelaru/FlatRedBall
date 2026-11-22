using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using GlueUnitTests.TestSupport;
using OfficialPlugins.MonoGameContent;
using Shouldly;

namespace GlueUnitTests.ContentPipelinePlugin;

// Loading a project builds every stale content file. It used to start MGCB once per file, about a second
// each, so a fresh clone of a game with 200 sounds for two platforms took minutes. Files in the same folder
// now share one MGCB run; a file that run could not build still gets its own build, which reports its error.
public class BuildLogicBatchBuildTests : IDisposable
{
    readonly string directory = Path.Combine(Path.GetTempPath(), nameof(BuildLogicBatchBuildTests) + "_" + Guid.NewGuid().ToString("N"));
    readonly VisualStudioProject originalMainProject;
    readonly GlueProjectSave originalGlueProject;
    readonly string originalRelativeDirectory;
    readonly Func<VisualStudioProject, string> originalGetBuildExe;
    readonly Action<ContentItem, VisualStudioProject, bool> originalBuildContentItem;
    readonly Action<IReadOnlyList<ContentItem>, VisualStudioProject, Action<string>> originalBuildContentItems;
    readonly MonoGameDesktopGlNetCoreProject project;
    readonly string projectDirectory;

    readonly List<string[]> batchBuilds = new();
    readonly List<string> singleBuilds = new();
    readonly HashSet<string> filesTheBatchFails = new();
    Action<IReadOnlyList<ContentItem>> duringBatch = _ => { };

    public BuildLogicBatchBuildTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        originalMainProject = GlueState.Self.CurrentMainProject;
        originalGlueProject = ObjectFinder.Self.GlueProject;
        originalRelativeDirectory = FileManager.RelativeDirectory;
        originalGetBuildExe = BuildLogic.Self.GetBuildExe;
        originalBuildContentItem = BuildLogic.Self.BuildContentItem;
        originalBuildContentItems = BuildLogic.Self.BuildContentItems;

        Directory.CreateDirectory(directory);
        var classLibrary = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out projectDirectory, parentDirectory: directory);
        project = new MonoGameDesktopGlNetCoreProject(classLibrary.Project);
        // As ProjectCreator sets up a project that holds its own content.
        project.ContentProject = project;
        GlueState.Self.CurrentMainProject = project;
        FileManager.RelativeDirectory = projectDirectory + "\\";
        ObjectFinder.Self.GlueProject = new GlueProjectSave();

        var fakeBuilder = Path.Combine(directory, "mgcb.exe");
        File.WriteAllText(fakeBuilder, "");
        BuildLogic.Self.GetBuildExe = _ => fakeBuilder;
        BuildLogic.Self.BuildContentItems = (contentItems, _, _) =>
        {
            // Batches run on several threads at once.
            lock (batchBuilds)
            {
                batchBuilds.Add(contentItems.Select(item => item.BuildFileName.NoPath).ToArray());
            }
            duringBatch(contentItems);
            foreach (var item in contentItems.Where(item => !filesTheBatchFails.Contains(item.BuildFileName.NoPath)))
            {
                WriteBuiltFile(item);
            }
        };
        BuildLogic.Self.BuildContentItem = (contentItem, _, _) =>
        {
            singleBuilds.Add(contentItem.BuildFileName.NoPath);
            WriteBuiltFile(contentItem);
        };
    }

    public void Dispose()
    {
        BuildLogic.Self.GetBuildExe = originalGetBuildExe;
        BuildLogic.Self.BuildContentItem = originalBuildContentItem;
        BuildLogic.Self.BuildContentItems = originalBuildContentItems;
        GlueState.Self.CurrentMainProject = originalMainProject;
        ObjectFinder.Self.GlueProject = originalGlueProject;
        FileManager.RelativeDirectory = originalRelativeDirectory;
        try { Directory.Delete(directory, recursive: true); } catch (Exception) { }
    }

    static void WriteBuiltFile(ContentItem contentItem)
    {
        Directory.CreateDirectory(contentItem.OutputDirectory);
        File.WriteAllText(Path.Combine(contentItem.OutputDirectory, contentItem.BuildFileName.NoPathNoExtension + ".xnb"), "built");
    }

    void AddSound(string relativeToContent)
    {
        var path = Path.Combine(projectDirectory, "Content", relativeToContent);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "source");
        ObjectFinder.Self.GlueProject.GlobalFiles.Add(new ReferencedFileSave { Name = relativeToContent.Replace('\\', '/'), UseContentPipeline = true });
    }

    [Fact]
    public async Task RefreshBuiltFilesFor_ShouldRunTheBatchesOfDifferentFolders_AtTheSameTime()
    {
        AddSound(@"Sfx\A.wav");
        AddSound(@"Music\B.wav");
        // Each batch waits for the other: built one after another, the first one would wait out the timeout.
        using var bothRunning = new System.Threading.Barrier(2);
        var sawTheOtherBatch = new List<bool>();
        duringBatch = _ =>
        {
            var arrived = bothRunning.SignalAndWait(TimeSpan.FromSeconds(10));
            lock (sawTheOtherBatch) { sawTheOtherBatch.Add(arrived); }
        };

        await BuildLogic.Self.RefreshBuiltFilesFor(project);
        await TaskManager.Self.WaitForAllTasksFinished();

        sawTheOtherBatch.ShouldBe(new[] { true, true });
    }

    [Fact]
    public async Task RefreshBuiltFilesFor_ShouldNotRunBatchesThatShareAnIntermediateDirectory_AtTheSameTime()
    {
        // A KNI texture builds with /compress and a sound without, so a folder holding both is two batches
        // that share the folder's intermediate directory, where MGCB keeps its cache.
        var kniProject = new KniWebProject(project.Project);
        kniProject.ContentProject = kniProject;
        GlueState.Self.CurrentMainProject = kniProject;
        AddSound(@"Mixed\A.wav");
        var png = Path.Combine(projectDirectory, "Content", "Mixed", "B.png");
        File.WriteAllText(png, "source");
        ObjectFinder.Self.GlueProject.GlobalFiles.Add(new ReferencedFileSave { Name = "Mixed/B.png", UseContentPipeline = true });
        var running = 0;
        var mostRunning = 0;
        duringBatch = _ =>
        {
            var now = System.Threading.Interlocked.Increment(ref running);
            lock (batchBuilds) { mostRunning = Math.Max(mostRunning, now); }
            System.Threading.Thread.Sleep(300);
            System.Threading.Interlocked.Decrement(ref running);
        };

        await BuildLogic.Self.RefreshBuiltFilesFor(kniProject);
        await TaskManager.Self.WaitForAllTasksFinished();

        batchBuilds.Count.ShouldBe(2);
        mostRunning.ShouldBe(1);
    }

    [Fact]
    public async Task RefreshBuiltFilesFor_ShouldBuildEachFolderWithOneRun()
    {
        AddSound(@"Sfx\A.wav");
        AddSound(@"Sfx\B.wav");
        AddSound(@"Sfx\C.wav");
        AddSound(@"Music\D.wav");

        await BuildLogic.Self.RefreshBuiltFilesFor(project);
        await TaskManager.Self.WaitForAllTasksFinished();

        batchBuilds.Count.ShouldBe(2);
        batchBuilds.ShouldContain(batch => batch.SequenceEqual(new[] { "A.wav", "B.wav", "C.wav" }));
        batchBuilds.ShouldContain(batch => batch.SequenceEqual(new[] { "D.wav" }));
        singleBuilds.ShouldBeEmpty();
    }

    [Fact]
    public async Task RefreshBuiltFilesFor_ShouldBuildAFileAlone_WhenItsBatchCouldNotBuildIt()
    {
        AddSound(@"Sfx\A.wav");
        AddSound(@"Sfx\B.wav");
        filesTheBatchFails.Add("B.wav");

        await BuildLogic.Self.RefreshBuiltFilesFor(project);
        await TaskManager.Self.WaitForAllTasksFinished();

        singleBuilds.ShouldBe(new[] { "B.wav" });
    }

    [Fact]
    public async Task RefreshBuiltFilesFor_ShouldNotBuild_WhenEveryFileIsUpToDate()
    {
        AddSound(@"Sfx\A.wav");
        await BuildLogic.Self.RefreshBuiltFilesFor(project);
        await TaskManager.Self.WaitForAllTasksFinished();
        batchBuilds.Clear();

        await BuildLogic.Self.RefreshBuiltFilesFor(project);
        await TaskManager.Self.WaitForAllTasksFinished();

        batchBuilds.ShouldBeEmpty();
        singleBuilds.ShouldBeEmpty();
    }

    [Fact]
    public async Task RefreshBuiltFilesFor_ShouldRecordBatchBuiltFiles_SoTheWatcherSkipsThem()
    {
        AddSound(@"Sfx\A.wav");
        AddSound(@"Sfx\B.wav");

        await BuildLogic.Self.RefreshBuiltFilesFor(project);
        await TaskManager.Self.WaitForAllTasksFinished();

        foreach (var name in new[] { "A", "B" })
        {
            FileWatchManager.IsUnchangedSelfWrite(new FilePath(Path.Combine(directory, "BuiltXnbs", "DesktopGL", "Content", "Sfx", name + ".xnb")))
                .ShouldBeTrue();
        }
    }

    [Fact]
    public void GenerateCommandLine_ShouldBeUnchanged_ForOneItem()
    {
        var item = ContentItem.CreateWavBuild();
        item.Platform = "DesktopGL";
        item.OutputDirectory = @"C:\Out\";
        item.IntermediateDirectory = @"C:\Obj\";
        item.BuildFileName = new FilePath(@"C:\Content\A.wav");

        item.GenerateCommandLine(project).ShouldBe(
            "/outputDir:\"C:\\Out\\\" /intermediateDir:\"C:\\Obj\\\" /platform:DesktopGL /importer:WavImporter /processor:SoundEffectProcessor " +
            "/processorParam:Quality=Best /build:\"C:/Content/A.wav\" /incremental");
    }

    [Fact]
    public void GenerateCommandLine_ShouldPutEachItemsImporterAndProcessorBeforeItsBuild()
    {
        var wav = ContentItem.CreateWavBuild();
        var png = ContentItem.CreateTextureBuild(project);
        foreach (var item in new[] { wav, png })
        {
            item.Platform = "DesktopGL";
            item.OutputDirectory = @"C:\Out\";
            item.IntermediateDirectory = @"C:\Obj\";
        }
        wav.BuildFileName = new FilePath(@"C:\Content\A.wav");
        png.BuildFileName = new FilePath(@"C:\Content\B.png");

        ContentItem.GenerateCommandLine(new[] { wav, png }).ShouldBe(
            "/outputDir:\"C:\\Out\\\" /intermediateDir:\"C:\\Obj\\\" /platform:DesktopGL " +
            "/importer:WavImporter /processor:SoundEffectProcessor /processorParam:Quality=Best /build:\"C:/Content/A.wav\" " +
            "/importer:TextureImporter /processor:TextureProcessor /processorParam:ColorKeyEnabled=False /processorParam:GenerateMipmaps=False " +
            "/processorParam:PremultiplyAlpha=True /processorParam:TextureFormat=Color /build:\"C:/Content/B.png\" /incremental");
    }
}
