using FlatRedBall.Glue.IO;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GumPluginTests;

// Gum's JSON project format swaps the trailing "x" of every XML extension for "j"
// (gumx->gumj, gusx->gusj, gucx->gucj, gutx->gutj, behx->behj, ganx->ganj). Glue compares
// extensions all over the place, so one helper owns the mapping.
public class GumFileExtensionsTests
{
    [Theory]
    [InlineData("gumj", "gumx")]
    [InlineData("gusj", "gusx")]
    [InlineData("gucj", "gucx")]
    [InlineData("gutj", "gutx")]
    [InlineData("behj", "behx")]
    [InlineData("ganj", "ganx")]
    public void Normalize_JsonExtension_ReturnsXmlCounterpart(string json, string xml)
    {
        GumFileExtensions.Normalize(json).ShouldBe(xml);
    }

    [Theory]
    [InlineData("gumx")]
    [InlineData("png")]
    [InlineData("json")]
    [InlineData("")]
    public void Normalize_NonJsonGumExtension_IsUnchanged(string extension)
    {
        GumFileExtensions.Normalize(extension).ShouldBe(extension);
    }

    [Fact]
    public void Normalize_Null_ReturnsNull()
    {
        GumFileExtensions.Normalize(null).ShouldBeNull();
    }

    [Fact]
    public void Normalize_IsCaseInsensitive()
    {
        GumFileExtensions.Normalize("GUSJ").ShouldBe("gusx");
    }

    [Theory]
    [InlineData("Screens/MainMenu.gusx", "gusx", true)]
    [InlineData("Screens/MainMenu.gusj", "gusx", true)]
    [InlineData("Screens/MainMenu.gucj", "gusx", false)]
    [InlineData("GumProject/GumProject.gumj", "gumx", true)]
    [InlineData("Screens/MainMenu", "gusx", false)]
    public void HasExtension_MatchesEitherFormat(string fileName, string xmlExtension, bool expected)
    {
        GumFileExtensions.HasExtension(fileName, xmlExtension).ShouldBe(expected);
    }

    [Theory]
    [InlineData("GumProject/GumProject.gumx", true)]
    [InlineData("GumProject/GumProject.gumj", true)]
    [InlineData("GumProject/Screens/A.gusj", false)]
    public void IsProject_MatchesBothProjectExtensions(string fileName, bool expected)
    {
        GumFileExtensions.IsProject(fileName).ShouldBe(expected);
    }

    [Theory]
    [InlineData("Screens/A.gusj", true)]
    [InlineData("Screens/A.GUSJ", true)]
    [InlineData("GumProject.gumj", true)]
    [InlineData("Screens/A.gusx", false)]
    [InlineData("Screens/A.json", false)]
    [InlineData("Screens/A", false)]
    [InlineData(null, false)]
    public void IsJsonFile_TrueOnlyForGumJsonExtensions(string fileName, bool expected)
    {
        GumFileExtensions.IsJsonFile(fileName).ShouldBe(expected);
    }

    [Theory]
    [InlineData("Screens/A.gusx", true, true)]
    [InlineData("Screens/A.gusj", false, true)]
    [InlineData("Screens/A.gusj", true, false)]
    [InlineData("Screens/A.gusx", false, false)]
    [InlineData("Screens/AAnimations.ganx", true, true)]
    [InlineData("Behaviors/B.behj", false, true)]
    [InlineData("gum_events.json", true, false)]
    [InlineData("hero.png", true, false)]
    public void IsGumFileOfOtherFormat_OnlyGumFilesInTheOtherFormat(string fileName, bool isJsonProject, bool expected)
    {
        GumFileExtensions.IsGumFileOfOtherFormat(fileName, isJsonProject).ShouldBe(expected);
    }

    [Theory]
    [InlineData("gusx", false, "gusx")]
    [InlineData("gusx", true, "gusj")]
    [InlineData("ganx", true, "ganj")]
    [InlineData("gusj", true, "gusj")]
    [InlineData("gusj", false, "gusx")]
    public void ForFormat_ReturnsExtensionForRequestedFormat(string extension, bool isJson, string expected)
    {
        GumFileExtensions.ForFormat(extension, isJson).ShouldBe(expected);
    }

    [Theory]
    [InlineData("C:/p/GumProject.gumj", true)]
    [InlineData("C:/p/GumProject.gumx", false)]
    [InlineData(null, false)]
    public void IsJsonProject_DependsOnProjectFileExtension(string projectFile, bool expected)
    {
        GumFileExtensions.IsJsonProject(projectFile).ShouldBe(expected);
    }
}
