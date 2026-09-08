using System;
using FlatRedBall.IO;
using Shouldly;

namespace GlueUnitTests.IO;

/// <summary>
/// FilePath's own accessors split cleanly on case: FullPath and StandardizedCaseSensitive preserve it,
/// Standardized deliberately lower-cases. RelativeTo did neither - it delegated to
/// FileManager.MakeRelative, which lower-cases whenever the process-wide FileManager.PreserveCase flag
/// happens to be false. That made the case of a relative path a function of unrelated global state
/// rather than of the path itself, which is how lower-cased XNB &lt;Link&gt; values got written into
/// .csproj files (GitHub issue #1757).
/// </summary>
public class FilePathRelativeToCaseTests : IDisposable
{
    readonly bool wasPreservingCase = FileManager.PreserveCase;

    public void Dispose() => FileManager.PreserveCase = wasPreservingCase;

    [Fact]
    public void RelativeTo_ShouldPreserveCase_WhenPreserveCaseIsFalse()
    {
        FileManager.PreserveCase = false;

        var file = new FilePath(@"C:\Project\Content\Gum\FontCache\Font16Titillium_Web_Italic_0.png");

        var relative = file.RelativeTo(new FilePath(@"C:\Project\Content"));

        relative.ShouldBe("Gum/FontCache/Font16Titillium_Web_Italic_0.png");
    }

    [Fact]
    public void RelativeTo_ShouldPreserveCase_WhenPreserveCaseIsTrue()
    {
        FileManager.PreserveCase = true;

        var file = new FilePath(@"C:\Project\Content\Gum\FontCache\Font16Titillium_Web_Italic_0.png");

        var relative = file.RelativeTo(new FilePath(@"C:\Project\Content"));

        relative.ShouldBe("Gum/FontCache/Font16Titillium_Web_Italic_0.png");
    }

    [Fact]
    public void RelativeTo_ShouldStillMatchContainingFolder_WhenTheFolderIsCasedDifferentlyThanTheFile()
    {
        FileManager.PreserveCase = true;

        var file = new FilePath(@"C:\Project\Content\Gum\FontCache\Font16Titillium_Web_Italic_0.png");

        var relative = file.RelativeTo(new FilePath(@"C:\project\content"));

        relative.ShouldBe("Gum/FontCache/Font16Titillium_Web_Italic_0.png");
    }
}
