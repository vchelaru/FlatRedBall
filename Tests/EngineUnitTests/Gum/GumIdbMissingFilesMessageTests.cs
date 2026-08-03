using System.Collections.Generic;
using FlatRedBall.Gum;
using Shouldly;
using Xunit;

namespace EngineUnitTests.Gum;

// A Gum project saved in a format newer than the running GumCore deserializes every element reference
// with a blank Name, and the loader then reports paths like "...\Screens\.gusx" - a real path built
// around an empty string. The old message ("Missing files starting with " + first entry) presented that
// as a missing file, which sends people looking for a file that was never absent, and hid both the count
// and the actual cause.
public class GumIdbMissingFilesMessageTests
{
    [Fact]
    public void EmptyElementName_ShouldReportAFormatMismatchRatherThanAMissingFile()
    {
        var missingFiles = new List<string>
        {
            @"C:\Game\bin\Debug\net9.0\Content\GumProject\Screens\.gusx",
        };

        var message = GumIdb.GetMissingFilesMessage(missingFiles, @"C:\Game\Content\GumProject\GumProject.gumx");

        // The whole point: don't call this a missing file, and say what to actually do about it.
        message.ShouldContain("empty names");
        message.ShouldContain("newer format");
        message.ShouldContain("Update the FlatRedBall / Gum libraries");
        // The project being loaded is named, so the reader knows which .gumx is at fault.
        message.ShouldContain("GumProject.gumx");
        // The offending path is still shown - it's the evidence, just no longer the headline.
        message.ShouldContain(@"Screens\.gusx");
    }

    [Fact]
    public void GenuinelyMissingFiles_ShouldListThemAllWithACount()
    {
        var missingFiles = new List<string>
        {
            @"C:\Game\Content\GumProject\Screens\MainMenu.gusx",
            @"C:\Game\Content\GumProject\Components\Button.gucx",
        };

        var message = GumIdb.GetMissingFilesMessage(missingFiles, @"C:\Game\Content\GumProject\GumProject.gumx");

        message.ShouldContain("2 file(s)");
        // Both are listed - the old message only ever showed the first, so a multi-file problem looked
        // like a single-file one and got fixed one round-trip at a time.
        message.ShouldContain("MainMenu.gusx");
        message.ShouldContain("Button.gucx");
        // Must NOT misreport a real missing file as a version problem.
        message.ShouldNotContain("newer format");
    }

    [Fact]
    public void NoMissingFiles_ShouldProduceNoMessage()
    {
        GumIdb.GetMissingFilesMessage(new List<string>(), "anything.gumx").ShouldBeNull();
        GumIdb.GetMissingFilesMessage(null, "anything.gumx").ShouldBeNull();
    }
}
