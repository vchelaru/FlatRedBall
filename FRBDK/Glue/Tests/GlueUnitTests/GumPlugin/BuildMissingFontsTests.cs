using System;
using System.Threading.Tasks;
using FlatRedBall.IO;
using GlueUnitTests.TestSupport;
using GumPlugin;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GumPluginTests;

// gumcli runs outside the TaskManager queue so it no longer holds every other task while it runs.
// Adding the fonts to the project still has to wait for it.
public class BuildMissingFontsTests
{
    static readonly FilePath GumProject = new FilePath("C:/Project/Content/GumProject/GumProject.gumx");

    public BuildMissingFontsTests() => GlueTestBootstrap.EnsureInitialized();

    [Fact]
    public async Task BuildMissingFontsAsync_ShouldAddFontsToProject_OnlyAfterGumCliFinishes()
    {
        var gumCli = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var added = false;
        FilePath? passedProject = null;

        var build = MainGumPlugin.BuildMissingFontsAsync(GumProject, project => { passedProject = project; return gumCli.Task; },
            () => { added = true; return Task.CompletedTask; });

        await Task.Delay(50);
        added.ShouldBeFalse();

        gumCli.SetResult(true);
        await build;

        added.ShouldBeTrue();
        passedProject.ShouldBe(GumProject);
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
