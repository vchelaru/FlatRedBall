using System;
using System.IO;
using FlatRedBall.Glue.SaveClasses;
using Shouldly;
using Xunit;

namespace GlueUnitTests.SaveClasses;

// Fixing the capitalization of every referenced file at load time looked up each folder of each file's path on
// disk again, so a project's open time grew with (files x folder depth) even though most files share their folders.
public class ExactPathResolverTests : IDisposable
{
    readonly string _root = Path.Combine(Path.GetTempPath(), "glue-exactpath-" + Guid.NewGuid());

    public ExactPathResolverTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Content", "Sub", "Deeper"));
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    string MakeFile(string relative)
    {
        var path = Path.Combine(_root, relative);
        File.WriteAllText(path, "x");
        return path;
    }

    [Fact]
    public void TryGetExactPath_WrongCapitalization_ReturnsTheSpellingOnDisk()
    {
        var onDisk = MakeFile(Path.Combine("Content", "Sub", "Deeper", "Hero.png"));
        var wrongCase = Path.Combine(_root, "content", "SUB", "deeper", "HERO.PNG");

        new ExactPathResolver().TryGetExactPath(wrongCase, out var exact).ShouldBeTrue();

        exact.ShouldBe(onDisk);
    }

    [Fact]
    public void TryGetExactPath_MissingFile_ReturnsFalse()
    {
        new ExactPathResolver().TryGetExactPath(Path.Combine(_root, "Content", "nothing.png"), out var exact).ShouldBeFalse();

        exact.ShouldBeNull();
    }

    [Fact]
    public void TryGetExactPath_SameFolderTwice_AsksEachFolderOnlyOnce()
    {
        // Two files in the same folder: the second shares every folder with the first, so it adds one lookup (its file name).
        var first = MakeFile(Path.Combine("Content", "Sub", "Deeper", "A.png"));
        var second = MakeFile(Path.Combine("Content", "Sub", "Deeper", "B.png"));
        var resolver = new ExactPathResolver();

        resolver.TryGetExactPath(first, out _).ShouldBeTrue();
        var afterFirst = resolver.DirectoryLookupCount;
        resolver.TryGetExactPath(second, out var exactSecond).ShouldBeTrue();

        exactSecond.ShouldBe(second);
        (resolver.DirectoryLookupCount - afterFirst).ShouldBe(1);
    }

    [Fact]
    public void TryGetExactPath_ManyFilesInOneFolder_LookupsGrowWithFilesNotFilesTimesDepth()
    {
        var resolver = new ExactPathResolver();
        const int fileCount = 50;
        string first = null;
        for (int i = 0; i < fileCount; i++)
        {
            var file = MakeFile(Path.Combine("Content", "Sub", "Deeper", $"File{i}.png"));
            first ??= file;
            resolver.TryGetExactPath(file, out _).ShouldBeTrue();
        }

        var depth = first.Split(Path.DirectorySeparatorChar).Length;
        // one lookup per file plus every folder once
        resolver.DirectoryLookupCount.ShouldBeLessThanOrEqualTo(fileCount + depth);
    }

    [Fact]
    public void TryGetExactPath_SharedResolver_GivesTheSameAnswerAsAFreshResolverForEveryPath()
    {
        MakeFile(Path.Combine("Content", "Sub", "Deeper", "Hero.png"));
        MakeFile(Path.Combine("Content", "Sub", "Tile.png"));
        MakeFile(Path.Combine("Content", "Back.png"));
        var paths = new[]
        {
            Path.Combine(_root, "content", "sub", "deeper", "hero.png"),
            Path.Combine(_root, "CONTENT", "SUB", "tile.PNG"),
            Path.Combine(_root, "Content", "sub", "deeper", "HERO.png"),
            Path.Combine(_root, "content", "back.png"),
            Path.Combine(_root, "Content", "Sub", "Deeper"),
        };
        var shared = new ExactPathResolver();

        foreach (var path in paths)
        {
            new ExactPathResolver().TryGetExactPath(path, out var expected).ShouldBeTrue(path);

            shared.TryGetExactPath(path, out var actual).ShouldBeTrue(path);

            actual.ShouldBe(expected, path);
        }
    }

    [Fact]
    public void TryGetExactPath_FileRenamedBetweenCalls_ReturnsTheNewSpellingOfTheFile()
    {
        // Folders are remembered between calls; the file itself is always asked.
        var path = MakeFile(Path.Combine("Content", "Sub", "hero.png"));
        var resolver = new ExactPathResolver();
        resolver.TryGetExactPath(path, out var before).ShouldBeTrue();
        before.ShouldBe(path);

        File.Move(path, Path.Combine(_root, "Content", "Sub", "Hero.png"));
        resolver.TryGetExactPath(Path.Combine(_root, "content", "sub", "HERO.png"), out var after).ShouldBeTrue();

        after.ShouldBe(Path.Combine(_root, "Content", "Sub", "Hero.png"));
    }
}
