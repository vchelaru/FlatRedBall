using System.IO;
using FlatRedBall.Glue.VSHelpers.Projects;
using GlueUnitTests.TestSupport;
using Xunit;

namespace GlueUnitTests.Projects;

public class Frb2Game1InitializeUpdaterTests
{
    [Fact]
    public void TryUpdateGame1ToLoadGlueProject_WithTheDefaultCall_ReplacesItAndReturnsTrue()
    {
        using var temp = new TempDir("Frb2Game1Updater_");
        var game1Path = Path.Combine(temp.Root, "Game1.cs");
        File.WriteAllText(game1Path,
            "namespace MyGame;\npublic class Game1\n{\n    void Initialize()\n    {\n        " +
            Frb2ProjectFixture.DefaultInitializeCall + "\n    }\n}\n");

        var result = Frb2Game1InitializeUpdater.TryUpdateGame1ToLoadGlueProject(
            game1Path, "Content/FrbEditor/MyGame.gluj");

        Assert.True(result);
        var contents = File.ReadAllText(game1Path);
        Assert.DoesNotContain(Frb2ProjectFixture.DefaultInitializeCall, contents);
        Assert.Contains(
            "FlatRedBallService.Default.Initialize(this, \"Content/FrbEditor/MyGame.gluj\");", contents);
    }

    [Fact]
    public void TryUpdateGame1ToLoadGlueProject_WithoutTheDefaultCall_LeavesTheFileAloneAndReturnsFalse()
    {
        using var temp = new TempDir("Frb2Game1UpdaterNoMatch_");
        var game1Path = Path.Combine(temp.Root, "Game1.cs");
        const string customContents = "namespace MyGame;\npublic class Game1\n{\n    // already customized\n}\n";
        File.WriteAllText(game1Path, customContents);

        var result = Frb2Game1InitializeUpdater.TryUpdateGame1ToLoadGlueProject(
            game1Path, "Content/FrbEditor/MyGame.gluj");

        Assert.False(result);
        Assert.Equal(customContents, File.ReadAllText(game1Path));
    }

    [Fact]
    public void TryUpdateGame1ToLoadGlueProject_WhenTheFileDoesNotExist_ReturnsFalse()
    {
        var result = Frb2Game1InitializeUpdater.TryUpdateGame1ToLoadGlueProject(
            @"C:\does\not\exist\Game1.cs", "Content/FrbEditor/MyGame.gluj");

        Assert.False(result);
    }
}
