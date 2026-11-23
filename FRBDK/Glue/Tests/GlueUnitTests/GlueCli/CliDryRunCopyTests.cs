using System;
using System.IO;
using System.Linq;
using GlueCli;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Cli;

/// <summary>
/// The temp copy behind <c>--dry-run</c>, on a hand-made folder: no Glue involved.
/// </summary>
public class CliDryRunCopyTests : IDisposable
{
    readonly TempDir temp = new("CliDryRunCopyTests_");
    readonly string game;
    readonly string csproj;

    public CliDryRunCopyTests()
    {
        game = Path.Combine(temp.Root, "Repo", "Game");
        Directory.CreateDirectory(Path.Combine(game, "Screens"));
        Directory.CreateDirectory(Path.Combine(game, "Content"));
        Directory.CreateDirectory(Path.Combine(game, "bin", "Debug"));
        csproj = Path.Combine(game, "Game.csproj");
        File.WriteAllText(csproj,
            "<Project>\r\n  <ItemGroup>\r\n    <ProjectReference Include=\"..\\Engine\\Engine.csproj\" />\r\n" +
            "    <ProjectReference Include=\"Inner\\Inner.csproj\" />\r\n  </ItemGroup>\r\n</Project>\r\n");
        File.WriteAllText(Path.Combine(temp.Root, "Repo", "Game.sln"), "solution");
        File.WriteAllText(Path.Combine(game, "Game.gluj"), "{\n  \"a\": 1\n}\n");
        File.WriteAllText(Path.Combine(game, "Screens", "GameScreen.glsj"), "{\n  \"x\": 1,\n  \"y\": 2\n}\n");
        File.WriteAllText(Path.Combine(game, "Screens", "Old.glsj"), "{}\n");
        File.WriteAllBytes(Path.Combine(game, "Content", "art.png"), new byte[] { 1, 2, 3 });
        File.WriteAllText(Path.Combine(game, "bin", "Debug", "Game.dll"), "build output");
    }

    public void Dispose() => temp.Dispose();

    CliDryRunCopy MakeCopy() => new CliDryRunCopier(temp.Root).Copy(csproj);

    [Fact]
    public void Copy_ShouldSkipBuildOutput_AndPointOutsideReferencesAtTheOriginal()
    {
        using var copy = MakeCopy();

        File.Exists(Path.Combine(copy.Root, "Screens", "GameScreen.glsj")).ShouldBeTrue();
        File.ReadAllText(Path.Combine(copy.Root, "..", "Game.sln")).ShouldBe("solution");
        Directory.Exists(Path.Combine(copy.Root, "bin")).ShouldBeFalse();
        var copiedCsproj = File.ReadAllText(copy.Csproj);
        copiedCsproj.ShouldContain($"Include=\"{Path.Combine(temp.Root, "Repo", "Engine", "Engine.csproj")}\"");
        copiedCsproj.ShouldContain("Include=\"Inner\\Inner.csproj\"", Case.Sensitive);
        copy.ComputeChanges().ShouldBeEmpty("the reference rewrite is part of the baseline");
    }

    [Fact]
    public void ComputeChanges_ShouldListModifiedAddedAndDeletedFiles_WithJsonDiffs()
    {
        using var copy = MakeCopy();
        File.WriteAllText(Path.Combine(copy.Root, "Screens", "GameScreen.glsj"), "{\n  \"x\": 1,\n  \"y\": 3\n}\n");
        File.WriteAllText(Path.Combine(copy.Root, "Screens", "GameScreen.Generated.cs"), "class GameScreen {}");
        File.Delete(Path.Combine(copy.Root, "Screens", "Old.glsj"));
        File.WriteAllText(Path.Combine(copy.Root, "..", "Game.sln"), "outside the project folder");

        var changes = copy.ComputeChanges();

        changes.Select(change => $"{change.Kind} {change.Path}").ShouldBe(new[]
        {
            "modified ../Game.sln",
            "added Screens/GameScreen.Generated.cs",
            "modified Screens/GameScreen.glsj",
            "deleted Screens/Old.glsj",
        });
        changes[2].Diff.ShouldBe(string.Join("\n",
            "--- a/Screens/GameScreen.glsj",
            "+++ b/Screens/GameScreen.glsj",
            "@@ -1,4 +1,4 @@",
            " {",
            "   \"x\": 1,",
            "-  \"y\": 2",
            "+  \"y\": 3",
            " }"));
        changes[1].Diff.ShouldBeNull();
    }

    [Fact]
    public void LinkedFile_ShouldRefuseWrites_AndLeaveTheOriginalAlone()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }
        var original = Path.Combine(game, "Content", "art.png");

        using (var copy = MakeCopy())
        {
            copy.LinkedFileCount.ShouldBe(1);
            var link = Path.Combine(copy.Root, "Content", "art.png");

            Should.Throw<IOException>(() => File.WriteAllBytes(link, new byte[] { 9 }));

            // Replacing the link is how a dry run changes it, and only the copy sees that.
            File.Delete(link);
            File.WriteAllBytes(link, new byte[] { 9, 9 });
            copy.ComputeChanges().Single().ShouldBe(new CliFileChange("Content/art.png", "modified", null));
        }

        File.ReadAllBytes(original).ShouldBe(new byte[] { 1, 2, 3 });
    }

    [Fact]
    public void Dispose_ShouldDeleteTheCopy_AndKeepTheOriginal()
    {
        string root;
        using (var copy = MakeCopy())
        {
            root = copy.Root;
        }

        Directory.Exists(root).ShouldBeFalse();
        File.ReadAllBytes(Path.Combine(game, "Content", "art.png")).ShouldBe(new byte[] { 1, 2, 3 });
        File.Exists(Path.Combine(game, "Screens", "Old.glsj")).ShouldBeTrue();
    }

    [Fact]
    public void MapPaths_ShouldNameTheOriginalFolder()
    {
        using var copy = MakeCopy();

        copy.MapPaths($"Could not read {copy.Root}\\Screens\\X.glsj").ShouldBe($"Could not read {game}\\Screens\\X.glsj");
        copy.MapPaths(copy.Root.Replace('\\', '/') + "/a").ShouldBe(game.Replace('\\', '/') + "/a");
    }

    [Fact]
    public void UnifiedDiff_ShouldSplitDistantChangesIntoHunks()
    {
        var before = string.Join("\n", Enumerable.Range(1, 20).Select(i => $"line{i}"));
        var after = before.Replace("line2\n", "line2b\n").Replace("line18\n", "");

        var diff = new UnifiedDiff().Create("f.json", before, after);

        diff.Split('\n').Where(line => line.StartsWith("@@")).ShouldBe(new[] { "@@ -1,5 +1,5 @@", "@@ -15,6 +15,5 @@" });
    }
}
