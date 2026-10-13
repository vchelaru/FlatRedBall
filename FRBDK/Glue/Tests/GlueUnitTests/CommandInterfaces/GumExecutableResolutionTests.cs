using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.IO;
using Shouldly;
using Xunit;

namespace GlueUnitTests.CommandInterfaces;

/// <summary>
/// Opening a Gum file from Glue launches the Gum editor. The Avalonia head must win whenever it exists:
/// a checkout that only built Gum.Avalonia in Release used to fall through to the old Gum.exe, which
/// can't open a .gumj.
/// </summary>
public class GumExecutableResolutionTests : IDisposable
{
    readonly string _root = Path.Combine(Path.GetTempPath(), "glue-gumexe-" + Guid.NewGuid());
    readonly FilePath _glueExeDirectory;

    public GumExecutableResolutionTests()
    {
        // Mirrors a source checkout: <root>/FlatRedBall/FRBDK/Glue/Glue/bin/Debug/ with Gum as a sibling of FlatRedBall.
        var glueDirectory = Path.Combine(_root, "FlatRedBall", "FRBDK", "Glue", "Glue", "bin", "Debug");
        Directory.CreateDirectory(glueDirectory);
        _glueExeDirectory = new FilePath(glueDirectory + Path.DirectorySeparatorChar);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    string Create(string relativeToGumRepo, DateTime? writtenAt = null)
    {
        var path = Path.Combine(_root, "Gum", relativeToGumRepo.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "");
        if (writtenAt != null)
        {
            File.SetLastWriteTimeUtc(path, writtenAt.Value);
        }
        return path;
    }

    string Resolve() => FileCommands.ResolveGumExecutable(_glueExeDirectory).FullPath;

    static bool SamePath(string actual, string expected) =>
        new FilePath(actual) == new FilePath(expected);

    [Fact]
    public void AvaloniaBuiltOnlyInRelease_IsFound()
    {
        var release = Create("Tool/Gum.Avalonia/bin/Release/net10.0/Gum.Avalonia.exe");

        SamePath(Resolve(), release).ShouldBeTrue();
    }

    [Fact]
    public void AvaloniaBeatsTheOldGumExe()
    {
        var avalonia = Create("Tool/Gum.Avalonia/bin/Release/net10.0/Gum.Avalonia.exe");
        Create("Gum/bin/Debug/Gum.exe");

        SamePath(Resolve(), avalonia).ShouldBeTrue();
    }

    [Fact]
    public void DebugWinsOverRelease_EvenWhenReleaseIsNewer()
    {
        // A developer building Gum from source works in Debug; a Release build beside it is usually
        // a stale one (it crashed on startup in practice), so file times can't be trusted to pick.
        var now = DateTime.UtcNow;
        var debug = Create("Tool/Gum.Avalonia/bin/Debug/net10.0/Gum.Avalonia.exe", now.AddHours(-2));
        Create("Tool/Gum.Avalonia/bin/Release/net10.0/Gum.Avalonia.exe", now);

        SamePath(Resolve(), debug).ShouldBeTrue();
    }

    [Fact]
    public void OnlyTheOldGumExe_IsStillUsed()
    {
        var old = Create("Gum/bin/Debug/Gum.exe");

        SamePath(Resolve(), old).ShouldBeTrue();
    }

    [Fact]
    public void NothingBuilt_ReturnsAPathThatDoesNotExist()
    {
        FileCommands.ResolveGumExecutable(_glueExeDirectory).Exists().ShouldBeFalse();
    }
}

/// <summary>
/// Launching Gum used to be fire-and-forget: a build that died on startup just vanished and Glue
/// printed nothing. These run real processes (cmd.exe) rather than fakes.
/// </summary>
public class LaunchAndReportFailureTests
{
    static ProcessStartInfo Cmd(string commandLine)
    {
        var startInfo = new ProcessStartInfo("cmd.exe");
        startInfo.ArgumentList.Add("/c");
        startInfo.ArgumentList.Add(commandLine);
        return startInfo;
    }

    [Fact]
    public async Task ProcessThatExitsNonZero_ReportsTheExitCodeAndItsStderr()
    {
        var errors = new List<string>();

        await FileCommands.LaunchAndReportFailureAsync(Cmd("echo the editor blew up 1>&2 & exit 3"), errors.Add);

        errors.Count.ShouldBe(1);
        errors[0].ShouldContain("exited with code 3");
        errors[0].ShouldContain("the editor blew up");
    }

    [Fact]
    public async Task ProcessThatExitsCleanly_ReportsNothing()
    {
        var errors = new List<string>();

        await FileCommands.LaunchAndReportFailureAsync(Cmd("echo all good & exit 0"), errors.Add);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecutableThatCannotBeStarted_ReportsIt()
    {
        var errors = new List<string>();

        await FileCommands.LaunchAndReportFailureAsync(
            new ProcessStartInfo(Path.Combine(Path.GetTempPath(), "definitely-not-here-" + Guid.NewGuid() + ".exe")),
            errors.Add);

        errors.Count.ShouldBe(1);
        errors[0].ShouldContain("Could not start");
    }
}
