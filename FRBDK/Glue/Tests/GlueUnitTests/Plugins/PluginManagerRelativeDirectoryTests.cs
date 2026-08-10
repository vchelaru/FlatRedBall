using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.IO;
using GlueUnitTests.TestSupport;
using Xunit;
using FileManager = FlatRedBall.IO.FileManager;
using FilePath = FlatRedBall.IO.FilePath;
using GeneralResponse = ToolsUtilities.GeneralResponse;

namespace GlueUnitTests.Plugins;

/// <summary>
/// <c>PluginManager</c> brackets each plugin dispatch with a push/pop of
/// <c>FileManager.RelativeDirectory</c> onto one process-wide stack, so a plugin that changes the
/// relative directory and does not put it back is caught and corrected. The stack outlives any one
/// project load, so an unbalanced push or pop does not fail where it happens - it leaves a previous
/// project's directory on the stack for the next load to restore, and reports
/// "The relativeDirectory wasn't set properly" against a caller that did nothing wrong.
/// </summary>
public class PluginManagerRelativeDirectoryTests
{
    public PluginManagerRelativeDirectoryTests() => GlueTestBootstrap.EnsureInitialized();

    [Fact]
    public void HandleFileReadError_LeavesTheRelativeDirectoryAlone()
    {
        // It resumed a relative directory it never saved, so it consumed whichever entry the call it
        // was nested inside had pushed. That outer call then restored the entry below - a directory
        // belonging to whatever loaded before it.
        var original = FileManager.RelativeDirectory;
        try
        {
            var directory = Path.Combine(Path.GetTempPath(), "PluginManagerRelDirTest") +
                Path.DirectorySeparatorChar;
            FileManager.RelativeDirectory = directory;
            // Read back rather than comparing to what was assigned - the setter normalizes separators.
            var expected = FileManager.RelativeDirectory;
            ErrorRecordingPlugin.Errors.Clear();

            PluginManager.HandleFileReadError(
                new FilePath(directory + "missing.png"),
                GeneralResponse.UnsuccessfulWith("could not read"));

            Assert.Equal(expected, FileManager.RelativeDirectory);
            Assert.Empty(ErrorRecordingPlugin.Errors);
        }
        finally
        {
            FileManager.RelativeDirectory = original;
        }
    }

    [Fact]
    public void ConcurrentPluginCalls_DoNotConsumeEachOthersSavedRelativeDirectory()
    {
        // FileManager.RelativeDirectory is per-thread (it reads and writes a dictionary keyed on the
        // managed thread id), but the stack it was saved onto was shared by every thread. Glue calls
        // these through Parallel.For while updating file memberships, so one worker's resume popped
        // another's entry and then assigned that other worker's directory over its own - and the
        // orphaned entries outlived the load that made them.
        var original = FileManager.RelativeDirectory;
        try
        {
            var directories = Enumerable.Range(0, 8)
                .Select(i => Path.Combine(Path.GetTempPath(), "RelDirConcurrency" + i) +
                    Path.DirectorySeparatorChar)
                .ToArray();
            ErrorRecordingPlugin.Errors.Clear();

            Parallel.ForEach(directories, directory =>
            {
                FileManager.RelativeDirectory = directory;
                var expected = FileManager.RelativeDirectory;

                for (var i = 0; i < 50; i++)
                {
                    PluginManager.CanFileReferenceContent(directory + "content.png");
                    Assert.Equal(expected, FileManager.RelativeDirectory);
                }
            });

            Assert.Empty(ErrorRecordingPlugin.Errors);
        }
        finally
        {
            FileManager.RelativeDirectory = original;
        }
    }
}
