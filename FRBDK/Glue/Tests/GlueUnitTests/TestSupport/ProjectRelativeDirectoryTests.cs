using System.IO;
using System.Threading;
using FlatRedBall.IO;
using Shouldly;
using Xunit;

namespace GlueUnitTests.TestSupport;

public class ProjectRelativeDirectoryTests
{
    [Fact]
    public void Constructor_ShouldApplyToOtherThreads_AndDisposeShouldRestore()
    {
        var originalDefault = FileManager.DefaultRelativeDirectory;
        var originalRelative = FileManager.RelativeDirectory;
        var projectDirectory = Path.Combine(Path.GetTempPath(), "ProjectRelativeDirectoryTests");
        var expected = new FilePath(projectDirectory + "\\");

        string seenOnOtherThread = null;
        using (new ProjectRelativeDirectory(projectDirectory))
        {
            var thread = new Thread(() => seenOnOtherThread = FileManager.RelativeDirectory);
            thread.Start();
            thread.Join();

            new FilePath(FileManager.RelativeDirectory).ShouldBe(expected);
        }

        new FilePath(seenOnOtherThread).ShouldBe(expected);
        FileManager.DefaultRelativeDirectory.ShouldBe(originalDefault);
        FileManager.RelativeDirectory.ShouldBe(originalRelative);
    }
}
