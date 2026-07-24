using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Npc;
using Npc.Managers;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Projects;

// Pins retry-on-transient-lock behavior for the new-project-creation (NPC) rename/GUID pipeline.
// RenameProject/GuidLogic.ReplaceGuids touch the same .sln/.csproj file several times back-to-back
// (a rename move, a content rewrite, a GUID rewrite) with no external process in between - just
// Glue's own successive reads/writes. Same bug shape as the VSSolution.cs .sln file-lock fix: a
// transient lock (AV scan, indexer, previous handle not yet released) on one of these touches used
// to surface as a raw IOException instead of being retried.
public class ProjectCreationHelperRetryTests
{
    [Fact]
    public void RenameProject_ShouldRetryAndSucceed_WhenSolutionFileIsTransientlyLockedByAnotherProcess()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"NpcRetryTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        // Named as the *target* project name already, so RenameFiles' File.Move never touches it -
        // only its text content ("GameTemplate") needs rewriting, which is what exercises
        // UpdateSolutionContents/EncodeSLNFiles.
        var slnPath = Path.Combine(tempDir, "MyGame.sln");
        File.WriteAllText(slnPath,
            "Microsoft Visual Studio Solution File, Format Version 12.00\r\n" +
            "Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"GameTemplate\", \"GameTemplate.csproj\", \"{11111111-1111-1111-1111-111111111111}\"\r\n" +
            "EndProject\r\n" +
            "Global\r\nEndGlobal");

        try
        {
            using var lockAcquired = new ManualResetEventSlim(false);

            // Simulate another process (AV scan, indexer, a handle from Glue's own prior touch not
            // yet released) holding an exclusive lock on the .sln for a short time.
            var lockTask = Task.Run(() =>
            {
                using var lockStream = new FileStream(
                    slnPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                lockAcquired.Set();
                Thread.Sleep(300);
            });

            lockAcquired.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue("background thread should have locked the file");

            Should.NotThrow(() =>
                ProjectCreationHelper.RenameProject("MyGame", "GameTemplate", newNamespace: null, tempDir));

            lockTask.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();

            var resultContents = File.ReadAllText(slnPath);
            resultContents.ShouldContain("MyGame");
            resultContents.ShouldNotContain("GameTemplate");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ReplaceGuids_ShouldRetryAndSucceed_WhenCsprojFileIsTransientlyLockedByAnotherProcess()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"NpcRetryTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        const string oldGuid = "ABCDEF00-AAAA-BBBB-CCCC-DDDDEEEEFFFF";
        var csprojPath = Path.Combine(tempDir, "MyGame.csproj");
        File.WriteAllText(csprojPath,
            $"<Project><PropertyGroup><ProjectGuid>{{{oldGuid}}}</ProjectGuid></PropertyGroup></Project>");
        // ReplaceGuids also rewrites the guid in the .sln (a legacy/non-SDK project always ships one).
        File.WriteAllText(Path.Combine(tempDir, "MyGame.sln"),
            $"Project(\"{{{oldGuid}}}\") = \"MyGame\"");

        try
        {
            using var lockAcquired = new ManualResetEventSlim(false);

            var lockTask = Task.Run(() =>
            {
                using var lockStream = new FileStream(
                    csprojPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                lockAcquired.Set();
                Thread.Sleep(300);
            });

            lockAcquired.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue("background thread should have locked the file");

            Should.NotThrow(() => GuidLogic.ReplaceGuids(tempDir));

            lockTask.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();

            File.ReadAllText(csprojPath).ShouldNotContain(oldGuid);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
