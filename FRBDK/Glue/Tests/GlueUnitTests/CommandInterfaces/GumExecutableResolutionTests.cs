using System;
using System.IO;
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
    public void WhenBothConfigurationsExist_TheNewerBuildWins()
    {
        var now = DateTime.UtcNow;
        Create("Tool/Gum.Avalonia/bin/Debug/net10.0/Gum.Avalonia.exe", now.AddHours(-2));
        var newerRelease = Create("Tool/Gum.Avalonia/bin/Release/net10.0/Gum.Avalonia.exe", now);

        SamePath(Resolve(), newerRelease).ShouldBeTrue();
    }

    [Fact]
    public void WhenDebugIsNewer_DebugWins()
    {
        var now = DateTime.UtcNow;
        var newerDebug = Create("Tool/Gum.Avalonia/bin/Debug/net10.0/Gum.Avalonia.exe", now);
        Create("Tool/Gum.Avalonia/bin/Release/net10.0/Gum.Avalonia.exe", now.AddHours(-2));

        SamePath(Resolve(), newerDebug).ShouldBeTrue();
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
