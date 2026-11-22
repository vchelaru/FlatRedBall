using System;
using System.Threading.Tasks;
using FlatRedBall.Glue.IO;
using FlatRedBall.IO;
using GlueUnitTests.TestSupport;
using GumPlugin;
using GumPlugin.Managers;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GumPluginTests;

// gumcli runs outside the TaskManager queue so it no longer holds every other task while it runs.
// Adding the fonts to the project still has to wait for it, and so do queued readers of font files.
public class BuildMissingFontsTests
{
    static readonly FilePath GumProject = new FilePath("C:/Project/Content/GumProject/GumProject.gumx");
    static readonly GumCliResult Succeeded = new GumCliResult(0, "", "", "gumcli fonts");

    public BuildMissingFontsTests() => GlueTestBootstrap.EnsureInitialized();

    [Fact]
    public async Task BuildMissingFontsAsync_ShouldAddFontsToProject_OnlyAfterGumCliFinishes()
    {
        var gumCli = new TaskCompletionSource<GumCliResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var added = false;
        FilePath? passedProject = null;

        var build = MainGumPlugin.BuildMissingFontsAsync(GumProject, project => { passedProject = project; return gumCli.Task; },
            () => { added = true; return Task.CompletedTask; });

        await Task.Delay(50);
        added.ShouldBeFalse();

        gumCli.SetResult(Succeeded);
        await build;

        added.ShouldBeTrue();
        passedProject.ShouldBe(GumProject);
    }

    [Fact]
    public async Task FontFileReaders_ShouldWaitForGumCli_WhileItIsWriting()
    {
        var gumCli = new TaskCompletionSource<GumCliResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var build = MainGumPlugin.BuildMissingFontsAsync(GumProject, _ => gumCli.Task, () => Task.CompletedTask);

            var offTaskThreadReader = Task.Run(() => ExternalFileWrites.WaitIf(isOnTaskThread: false));
            (await Task.WhenAny(offTaskThreadReader, Task.Delay(5000))).ShouldBe(offTaskThreadReader);

            var reader = Task.Run(() => ExternalFileWrites.WaitIf(isOnTaskThread: true));
            await Task.Delay(50);
            reader.IsCompleted.ShouldBeFalse();

            gumCli.SetResult(Succeeded);
            (await Task.WhenAny(reader, Task.Delay(5000))).ShouldBe(reader);
            await build;
        }
        finally
        {
            gumCli.TrySetResult(Succeeded);
        }
    }

    [Fact]
    public async Task BuildMissingFontsAsync_ShouldNotAddToProject_WhenGumCliThrows()
    {
        var added = false;

        await MainGumPlugin.BuildMissingFontsAsync(GumProject,
            _ => throw new InvalidOperationException("gumcli missing"),
            () => { added = true; return Task.CompletedTask; });

        added.ShouldBeFalse();
    }
}
