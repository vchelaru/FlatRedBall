using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Tasks;
using FlatRedBall.IO;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.IO;

// Every .xnb the content pipeline built used to come back through Glue's own file watcher, and each reaction
// regenerated global content code. The build now records what it wrote, and the watcher skips a recorded file
// only while it still has the recorded last-write time and length, with no time window either way.
public class FileWatchManagerSelfWriteTests : IDisposable
{
    // Long path: the watcher reports long paths, and the temp directory can be an 8.3 short path, which would put
    // every reported file outside the project.
    readonly string directory = Path.Combine(LongPath(Path.GetTempPath()), nameof(FileWatchManagerSelfWriteTests) + "_" + Guid.NewGuid().ToString("N"));
    readonly FilePath builtFile;

    public FileWatchManagerSelfWriteTests()
    {
        Directory.CreateDirectory(directory);
        builtFile = new FilePath(Path.Combine(directory, "Sound.xnb"));
        File.WriteAllText(builtFile.FullPath, "built");
    }

    public void Dispose()
    {
        try { Directory.Delete(directory, recursive: true); } catch (Exception) { }
    }

    [Fact]
    public void IsUnchangedSelfWrite_ShouldBeTrue_WhenTheRecordedFileIsUnchanged()
    {
        FileWatchManager.IgnoreChangesWhileUnchanged(builtFile);

        FileWatchManager.IsUnchangedSelfWrite(builtFile).ShouldBeTrue();
        FileWatchManager.IsUnchangedSelfWrite(builtFile).ShouldBeTrue();
    }

    [Fact]
    public void IsUnchangedSelfWrite_ShouldBeFalse_WhenTheFileWasNotRecorded()
    {
        FileWatchManager.IsUnchangedSelfWrite(builtFile).ShouldBeFalse();
    }

    [Fact]
    public void IsUnchangedSelfWrite_ShouldBeFalse_WhenTheFileIsRewrittenWithTheSameLength()
    {
        FileWatchManager.IgnoreChangesWhileUnchanged(builtFile);

        File.WriteAllText(builtFile.FullPath, "BUILT");
        File.SetLastWriteTimeUtc(builtFile.FullPath, DateTime.UtcNow.AddMinutes(1));

        FileWatchManager.IsUnchangedSelfWrite(builtFile).ShouldBeFalse();
    }

    [Fact]
    public void IsUnchangedSelfWrite_ShouldBeFalse_WhenTheFileChangesLength()
    {
        var lastWrite = File.GetLastWriteTimeUtc(builtFile.FullPath);
        FileWatchManager.IgnoreChangesWhileUnchanged(builtFile);

        File.WriteAllText(builtFile.FullPath, "changed outside Glue");
        File.SetLastWriteTimeUtc(builtFile.FullPath, lastWrite);

        FileWatchManager.IsUnchangedSelfWrite(builtFile).ShouldBeFalse();
    }

    [Fact]
    public void IsUnchangedSelfWrite_ShouldStayFalse_WhenAChangedFileIsRestored()
    {
        var lastWrite = File.GetLastWriteTimeUtc(builtFile.FullPath);
        FileWatchManager.IgnoreChangesWhileUnchanged(builtFile);
        File.SetLastWriteTimeUtc(builtFile.FullPath, lastWrite.AddMinutes(1));
        FileWatchManager.IsUnchangedSelfWrite(builtFile).ShouldBeFalse();

        File.SetLastWriteTimeUtc(builtFile.FullPath, lastWrite);

        FileWatchManager.IsUnchangedSelfWrite(builtFile).ShouldBeFalse();
    }

    [Fact]
    public async Task Flush_ShouldNotReactToARecordedFile_ButShouldReactToAnUnrecordedOne()
    {
        GlueTestBootstrap.EnsureInitialized();
        var originalProject = GlueState.Self.CurrentMainProject;
        var originalRelativeDirectory = FileManager.DefaultRelativeDirectory;
        string projectDirectory = null;
        var reactions = new List<string>();
        void OnTask(TaskEvent taskEvent, GlueTaskBase task)
        {
            if (task.DisplayInfo?.StartsWith(FileWatchManager.ReactingToChangedFileTaskPrefix) == true)
            {
                lock (reactions) { reactions.Add(task.DisplayInfo); }
            }
        }
        try
        {
            // The watcher watches the folder above the project, so that folder is our own: other writes to it
            // would keep the watcher's debounce from ever letting a flush through.
            GlueState.Self.CurrentMainProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out projectDirectory,
                parentDirectory: directory);
            FileWatchManager.UpdateToProjectDirectory();
            // As in a loaded project: the watcher's skip check resolves the names it is given against this, on
            // the watcher's own thread, which uses the default.
            FileManager.DefaultRelativeDirectory = projectDirectory + "\\";
            var recorded = new FilePath(Path.Combine(projectDirectory, "Built.txt"));
            var unrecorded = new FilePath(Path.Combine(projectDirectory, "Changed.txt"));
            TaskManager.Self.TaskAddedOrRemoved += OnTask;

            File.WriteAllText(recorded.FullPath, "built");
            FileWatchManager.IgnoreChangesWhileUnchanged(recorded);
            File.WriteAllText(unrecorded.FullPath, "changed outside Glue");
            // Past the watcher's 500 ms debounce.
            await Task.Delay(1500);
            await FileWatchManager.Flush();
            await TaskManager.Self.WaitForAllTasksFinished();

            reactions.ShouldNotContain(item => item.EndsWith("Built.txt"));
            reactions.ShouldContain(item => item.EndsWith("Changed.txt"));
        }
        finally
        {
            TaskManager.Self.TaskAddedOrRemoved -= OnTask;
            GlueState.Self.CurrentMainProject = originalProject;
            FileManager.DefaultRelativeDirectory = originalRelativeDirectory;
            FileWatchManager.UpdateToProjectDirectory();
        }
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    static extern uint GetLongPathName(string shortPath, System.Text.StringBuilder longPath, uint bufferLength);

    static string LongPath(string path)
    {
        var builder = new System.Text.StringBuilder(1024);
        return GetLongPathName(path, builder, (uint)builder.Capacity) > 0 ? builder.ToString() : path;
    }

    [Fact]
    public void IsUnchangedSelfWrite_ShouldBeFalse_WhenTheFileIsDeleted()
    {
        FileWatchManager.IgnoreChangesWhileUnchanged(builtFile);

        File.Delete(builtFile.FullPath);

        FileWatchManager.IsUnchangedSelfWrite(builtFile).ShouldBeFalse();
    }
}
