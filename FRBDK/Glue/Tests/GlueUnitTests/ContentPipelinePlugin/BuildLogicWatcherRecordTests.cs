using System;
using System.IO;
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

// A content build while a project loads records what it wrote, so Glue's file watcher does not react to it.
// A build caused by an edited source file must not: live edit copies the rebuilt .xnb to the running game
// and restarts its screen from that reaction. The MGCB run is replaced by a fake that writes the .xnb.
public class BuildLogicWatcherRecordTests : IDisposable
{
    readonly string directory = Path.Combine(Path.GetTempPath(), nameof(BuildLogicWatcherRecordTests) + "_" + Guid.NewGuid().ToString("N"));
    readonly VisualStudioProject originalMainProject;
    readonly GlueProjectSave originalGlueProject;
    readonly string originalRelativeDirectory;
    readonly Func<VisualStudioProject, string> originalGetBuildExe;
    readonly Action<ContentItem, VisualStudioProject, bool> originalBuildContentItem;
    readonly MonoGameDesktopGlNetCoreProject project;
    readonly FilePath sound;
    readonly FilePath builtSound;

    public BuildLogicWatcherRecordTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        originalMainProject = GlueState.Self.CurrentMainProject;
        originalGlueProject = ObjectFinder.Self.GlueProject;
        originalRelativeDirectory = FileManager.RelativeDirectory;
        originalGetBuildExe = BuildLogic.Self.GetBuildExe;
        originalBuildContentItem = BuildLogic.Self.BuildContentItem;

        Directory.CreateDirectory(directory);
        var classLibrary = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out var projectDirectory, parentDirectory: directory);
        project = new MonoGameDesktopGlNetCoreProject(classLibrary.Project);
        // As ProjectCreator sets up a project that holds its own content.
        project.ContentProject = project;
        GlueState.Self.CurrentMainProject = project;
        FileManager.RelativeDirectory = projectDirectory + "\\";

        sound = new FilePath(Path.Combine(projectDirectory, "Content", "GlobalContent", "Sound.wav"));
        Directory.CreateDirectory(sound.GetDirectoryContainingThis().FullPath);
        File.WriteAllText(sound.FullPath, "source");
        builtSound = new FilePath(Path.Combine(directory, "BuiltXnbs", "DesktopGL", "Content", "GlobalContent", "Sound.xnb"));

        ObjectFinder.Self.GlueProject = new GlueProjectSave();
        ObjectFinder.Self.GlueProject.GlobalFiles.Add(new ReferencedFileSave { Name = "GlobalContent/Sound.wav", UseContentPipeline = true });

        var fakeBuilder = Path.Combine(directory, "mgcb.exe");
        File.WriteAllText(fakeBuilder, "");
        BuildLogic.Self.GetBuildExe = _ => fakeBuilder;
        BuildLogic.Self.BuildContentItem = (contentItem, _, _) =>
        {
            Directory.CreateDirectory(contentItem.OutputDirectory);
            File.WriteAllText(Path.Combine(contentItem.OutputDirectory, contentItem.BuildFileName.NoPathNoExtension + ".xnb"), "built");
        };
    }

    public void Dispose()
    {
        BuildLogic.Self.GetBuildExe = originalGetBuildExe;
        BuildLogic.Self.BuildContentItem = originalBuildContentItem;
        GlueState.Self.CurrentMainProject = originalMainProject;
        ObjectFinder.Self.GlueProject = originalGlueProject;
        FileManager.RelativeDirectory = originalRelativeDirectory;
        try { Directory.Delete(directory, recursive: true); } catch (Exception) { }
    }

    [Fact]
    public async Task RefreshBuiltFilesFor_ShouldRecordTheBuiltFile_SoTheWatcherSkipsIt()
    {
        await BuildLogic.Self.RefreshBuiltFilesFor(project);
        await TaskManager.Self.WaitForAllTasksFinished();

        File.Exists(builtSound.FullPath).ShouldBeTrue();
        FileWatchManager.IsUnchangedSelfWrite(builtSound).ShouldBeTrue();
    }

    [Fact]
    public async Task TryAddXnbReferencesAndBuild_ShouldNotRecordTheBuiltFile_WhenAnEditedSourceCausedTheBuild()
    {
        // The path an edited source file takes (MainContentPipelinePlugin.HandleFileChanged ->
        // TryHandleReferencedFile), which leaves the watcher option at its default.
        await BuildLogic.Self.TryAddXnbReferencesAndBuild(sound, project, saveProjectAfterAdd: false);
        await TaskManager.Self.WaitForAllTasksFinished();

        File.Exists(builtSound.FullPath).ShouldBeTrue();
        FileWatchManager.IsUnchangedSelfWrite(builtSound).ShouldBeFalse();
    }
}
