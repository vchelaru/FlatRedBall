using System;
using System.Collections.Generic;
using FlatRedBall.Glue.IO;
using FlatRedBall.IO;
using Shouldly;

namespace GlueUnitTests.IO;

/// <summary>
/// Exporting an element de-duplicates its referenced files, which has to ignore case (the same file can
/// be reached as Content/Foo.png and content/foo.png). It used to do that by switching the process-wide
/// FileManager.PreserveCase off, lower-casing every entry, and switching it back - a global that any
/// other thread's path handling reads, restored without a try/finally, so an exception in between left
/// every path in the session lower-cased. That is the flag that produced the lower-cased XNB references
/// in GitHub issue #1757.
/// </summary>
public class ElementExporterDuplicatePathTests : IDisposable
{
    readonly bool wasPreservingCase = FileManager.PreserveCase;

    public void Dispose() => FileManager.PreserveCase = wasPreservingCase;

    [Fact]
    public void StandardizeAndRemoveDuplicatePaths_ShouldRemoveEntriesDifferingOnlyByCase()
    {
        var files = new List<string>
        {
            @"C:\Project\Content\Gum\FontCache\Font16Titillium_Web_Italic_0.png",
            @"C:\Project\content\gum\fontcache\font16titillium_web_italic_0.png",
        };

        ElementExporter.StandardizeAndRemoveDuplicatePaths(files);

        files.Count.ShouldBe(1);
    }

    [Fact]
    public void StandardizeAndRemoveDuplicatePaths_ShouldRemoveEntriesDifferingOnlyBySlashDirection()
    {
        var files = new List<string>
        {
            @"C:\Project\Content\Gum\FontCache\Font16Titillium_Web_Italic_0.png",
            "C:/Project/Content/Gum/FontCache/Font16Titillium_Web_Italic_0.png",
        };

        ElementExporter.StandardizeAndRemoveDuplicatePaths(files);

        files.Count.ShouldBe(1);
    }

    [Fact]
    public void StandardizeAndRemoveDuplicatePaths_ShouldPreserveTheCaseOfTheSurvivingEntry()
    {
        var files = new List<string>
        {
            @"C:\Project\Content\Gum\FontCache\Font16Titillium_Web_Italic_0.png",
        };

        ElementExporter.StandardizeAndRemoveDuplicatePaths(files);

        files[0].ShouldBe("C:/Project/Content/Gum/FontCache/Font16Titillium_Web_Italic_0.png");
    }

    [Fact]
    public void StandardizeAndRemoveDuplicatePaths_ShouldNotChangeThePreserveCaseFlag()
    {
        FileManager.PreserveCase = true;

        ElementExporter.StandardizeAndRemoveDuplicatePaths(new List<string> { @"C:\Project\Content\A.png" });

        FileManager.PreserveCase.ShouldBeTrue();
    }
}
