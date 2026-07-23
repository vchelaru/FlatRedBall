using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.IO;
using Shouldly;
using Xunit;

namespace GlueUnitTests.VSHelpers;

// Pins the fix for an intermittent popup when creating a new project:
// "Failed to add project ...csproj. Errors: Unhandled exception: The process cannot access the file
// '...sln' because it is being used by another process." AddSourceManager mutates the same .sln file
// back-to-back in a loop (once per FRB/Gum project reference), racing a just-exited dotnet.exe
// subprocess's file handle, an antivirus scan, or an IDE file watcher. VSSolution.AddExistingProject
// now retries transient IOException file locks instead of failing on the first hit.
public class VSSolutionRetryTests
{
    [Fact]
    public void AddExistingProject_ShouldRetryAndSucceed_WhenSolutionFileIsTransientlyLockedByAnotherProcess()
    {
        var tempSolutionPath = Path.Combine(Path.GetTempPath(), $"RetryTest_{Guid.NewGuid():N}.sln");
        File.WriteAllLines(tempSolutionPath, new[]
        {
            "Microsoft Visual Studio Solution File, Format Version 12.00",
            "Global",
            "EndGlobal"
        });

        try
        {
            using var lockAcquired = new ManualResetEventSlim(false);

            // Simulate another process (a just-exited dotnet.exe, an AV scan, a file watcher) holding
            // an exclusive lock on the .sln for a short time.
            var lockTask = Task.Run(() =>
            {
                using var lockStream = new FileStream(
                    tempSolutionPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                lockAcquired.Set();
                Thread.Sleep(300);
            });

            lockAcquired.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue("background thread should have locked the file");

            FilePath solution = tempSolutionPath;
            FilePath project = Path.Combine(Path.GetTempPath(), "SomeProject.csproj");

            var succeeded = VSSolution.AddExistingProject(
                solution,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "SomeProject",
                project,
                sharedProjects: null,
                projectConfigurations: null,
                solutionConfigurations: null,
                out var error);

            lockTask.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();

            succeeded.ShouldBeTrue(error);

            var resultContents = File.ReadAllText(tempSolutionPath);
            resultContents.ShouldContain("SomeProject");
        }
        finally
        {
            File.Delete(tempSolutionPath);
        }
    }
}
