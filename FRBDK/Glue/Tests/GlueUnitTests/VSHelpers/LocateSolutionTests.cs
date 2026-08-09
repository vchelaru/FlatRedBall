using System;
using System.IO;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.IO;
using Xunit;

namespace GlueUnitTests.VSHelpers;

/// <summary>
/// <see cref="ProjectSyncer.LocateSolution"/> throws when it finds no solution, and that exception
/// comes out of <c>GlueState.CurrentSlnFileName</c> while a project is loading - so a solution format
/// it does not recognize is a project that will not open, not a cosmetic gap.
/// </summary>
public class LocateSolutionTests : IDisposable
{
    readonly string _directory;

    public LocateSolutionTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "GlueUnitTests_Sln_" + Guid.NewGuid());
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); } catch (IOException) { }
    }

    string WriteProject(string name = "TestGame")
    {
        var csproj = Path.Combine(_directory, name + ".csproj");
        File.WriteAllText(csproj, "<Project />");
        return csproj;
    }

    [Fact]
    public void FindsAClassicSln()
    {
        var csproj = WriteProject();
        var sln = Path.Combine(_directory, "TestGame.sln");
        File.WriteAllText(sln, @"Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""TestGame"", ""TestGame.csproj"", ""{1}""");

        // FilePath rather than string: LocateSolution returns forward slashes on Windows.
        Assert.Equal(new FilePath(sln), new FilePath(ProjectSyncer.LocateSolution(csproj)));
    }

    [Fact]
    public void FindsAnSlnx()
    {
        // Visual Studio's XML solution format. The FRB2 sample ships only this.
        var csproj = WriteProject();
        var slnx = Path.Combine(_directory, "TestGame.slnx");
        File.WriteAllText(slnx, "<Solution>\n  <Project Path=\"TestGame.csproj\" />\n</Solution>");

        Assert.Equal(new FilePath(slnx), new FilePath(ProjectSyncer.LocateSolution(csproj)));
    }

    [Fact]
    public void PrefersTheClassicSln_WhenBothExist()
    {
        // A solution mid-migration has both. Everything else in the toolchain still reads the .sln.
        var csproj = WriteProject();
        var sln = Path.Combine(_directory, "TestGame.sln");
        File.WriteAllText(sln, @"""TestGame.csproj""");
        File.WriteAllText(Path.Combine(_directory, "TestGame.slnx"),
            "<Solution>\n  <Project Path=\"TestGame.csproj\" />\n</Solution>");

        // FilePath rather than string: LocateSolution returns forward slashes on Windows.
        Assert.Equal(new FilePath(sln), new FilePath(ProjectSyncer.LocateSolution(csproj)));
    }

    [Fact]
    public void ThrowsWhenThereIsNoSolutionAtAll()
    {
        var csproj = WriteProject();

        Assert.Throws<FileNotFoundException>(() => ProjectSyncer.LocateSolution(csproj));
    }
}
