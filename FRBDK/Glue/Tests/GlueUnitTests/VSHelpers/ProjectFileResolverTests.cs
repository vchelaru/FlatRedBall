using System;
using System.IO;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.IO;
using Xunit;

namespace GlueUnitTests.VSHelpers;

/// <summary>
/// The Load Project dialog offers .gluj and solution files, but Glue can only load a .csproj, so
/// <see cref="ProjectFileResolver"/> stands between the two. Getting it wrong means the pick fails
/// with "Could not find the project" even though the project is perfectly loadable by its .csproj.
/// </summary>
public class ProjectFileResolverTests : IDisposable
{
    readonly string _directory;

    public ProjectFileResolverTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "GlueUnitTests_Resolve_" + Guid.NewGuid());
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); } catch (IOException) { }
    }

    string Write(string relativePath, string contents = "<Project />")
    {
        var full = Path.Combine(_directory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full));
        File.WriteAllText(full, contents);
        return full;
    }

    // FilePath rather than string: the resolver composes paths with forward slashes on Windows.
    static void AssertResolves(string expected, string selected) =>
        Assert.Equal(new FilePath(expected), new FilePath(ProjectFileResolver.ResolveCsproj(selected)));

    [Fact]
    public void Csproj_IsReturnedUnchanged()
    {
        var csproj = Write("TestGame.csproj");

        AssertResolves(csproj, csproj);
    }

    [Fact]
    public void Gluj_BesideTheCsproj_ResolvesToIt()
    {
        // FRB1 keeps the .gluj and the .csproj side by side.
        var csproj = Write("TestGame.csproj");
        var gluj = Write("TestGame.gluj", "{}");

        AssertResolves(csproj, gluj);
    }

    [Fact]
    public void Glux_BesideTheCsproj_ResolvesToIt()
    {
        var csproj = Write("TestGame.csproj");
        var glux = Write("TestGame.glux", "<GlueProjectSave />");

        AssertResolves(csproj, glux);
    }

    [Fact]
    public void Gluj_UnderContentFrbEditor_ResolvesToTheCsprojAboveIt()
    {
        // FRB2 puts everything Glue authors under Content/FrbEditor, so the .csproj is two up.
        var csproj = Write("TestGame.csproj");
        var gluj = Write("Content/FrbEditor/TestGame.gluj", "{}");

        AssertResolves(csproj, gluj);
    }

    [Fact]
    public void Gluj_UnderContentFrbEditor_ResolvesToADifferentlyNamedCsproj()
    {
        // Nothing forces the .gluj's name to match the .csproj's - a renamed project leaves the two
        // disagreeing, and the .csproj is still the one to open.
        var csproj = Write("RenamedGame.csproj");
        var gluj = Write("Content/FrbEditor/TestGame.gluj", "{}");

        AssertResolves(csproj, gluj);
    }

    [Fact]
    public void Gluj_UnderContentFrbEditor_WithTwoCsprojsAbove_ResolvesToTheOneNamedAfterIt()
    {
        var csproj = Write("TestGame.csproj");
        Write("TestGame.Desktop.csproj");
        var gluj = Write("Content/FrbEditor/TestGame.gluj", "{}");

        AssertResolves(csproj, gluj);
    }

    [Fact]
    public void Gluj_InSomeOtherSubdirectory_DoesNotWanderUpToAnUnrelatedProject()
    {
        // Searching upward for any .csproj reaches out of the project entirely - a .gluj in a temp
        // folder found an unrelated project sitting in %TEMP%.
        Write("TestGame.csproj");
        var gluj = Write("SomeFolder/TestGame.gluj", "{}");

        AssertResolves(Path.Combine(_directory, "SomeFolder", "TestGame.csproj"), gluj);
    }

    [Fact]
    public void Gluj_WithNoCsprojAnywhere_ResolvesToTheSiblingCsproj()
    {
        // Nothing to find, so hand back the path the old swap produced: it does not exist, and the
        // load reports "Could not find the project <that path>", which is the truth.
        var gluj = Write("TestGame.gluj", "{}");

        AssertResolves(Path.Combine(_directory, "TestGame.csproj"), gluj);
    }

    [Fact]
    public void Sln_ResolvesToTheProjectMatchingTheSolutionName()
    {
        var csproj = Write("TestGame.csproj");
        var sln = Write("TestGame.sln",
            "Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"TestGame\", \"TestGame.csproj\", \"{1}\"");

        AssertResolves(csproj, sln);
    }

    [Fact]
    public void Sln_WithOneProjectNotMatchingTheSolutionName_ResolvesToThatProject()
    {
        // Renaming a solution without renaming the project used to be a NullReferenceException.
        var csproj = Write("TestGame.csproj");
        var sln = Write("Renamed.sln",
            "Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"TestGame\", \"TestGame.csproj\", \"{1}\"");

        AssertResolves(csproj, sln);
    }

    [Fact]
    public void Slnx_ResolvesToTheProjectMatchingTheSolutionName()
    {
        // Visual Studio's XML solution format, which the FRB2 sample ships. The engine projects come
        // first in it, so the name match - not "the first project" - is what picks the game.
        var csproj = Write("TestGame.csproj");
        Write("engine/FlatRedBall2.csproj");
        var slnx = Write("TestGame.slnx",
            "<Solution>\n  <Project Path=\"engine/FlatRedBall2.csproj\" />\n  <Project Path=\"TestGame.csproj\" />\n</Solution>");

        AssertResolves(csproj, slnx);
    }

    [Fact]
    public void Slnx_WithOneProjectNotMatchingTheSolutionName_ResolvesToThatProject()
    {
        var csproj = Write("TestGame.csproj");
        var slnx = Write("Renamed.slnx",
            "<Solution>\n  <Project Path=\"TestGame.csproj\" />\n</Solution>");

        AssertResolves(csproj, slnx);
    }

    [Fact]
    public void Slnx_ResolvesAProjectInASubdirectory()
    {
        var csproj = Write("src/TestGame/TestGame.csproj");
        var slnx = Write("TestGame.slnx",
            "<Solution>\n  <Project Path=\"src/TestGame/TestGame.csproj\" />\n</Solution>");

        AssertResolves(csproj, slnx);
    }
}
