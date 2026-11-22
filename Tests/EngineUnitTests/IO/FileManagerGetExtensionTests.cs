using FlatRedBall.IO;
using Shouldly;
using Xunit;

namespace EngineUnitTests.IO;

public class FileManagerGetExtensionTests
{
    [Theory]
    [InlineData("Content/Screens/GameScreen/Hero.PNG", "png")]
    [InlineData(@"C:\Game\Content\Data.csv", "csv")]
    [InlineData("Content/My.Folder/Hero.achx", "achx")]
    [InlineData("Content/My.Folder/NoExtension", "")]
    [InlineData(@"Content\My.Folder\NoExtension", "")]
    [InlineData("Content/./Hero", "")]
    [InlineData("redball", "")]
    [InlineData(null, "")]
    public void GetExtension_ReturnsLowercaseExtensionOfTheFileNameOnly(string fileName, string expected)
    {
        FileManager.GetExtension(fileName).ShouldBe(expected);
    }
}
