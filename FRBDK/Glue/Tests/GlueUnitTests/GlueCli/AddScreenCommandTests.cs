using System.IO;
using System.Linq;
using System.Text.Json;
using GlueCli;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;
using static GlueUnitTests.Cli.GlueCliTestRun;

namespace GlueUnitTests.Cli;

/// <summary>
/// <c>GlueCli add-screen</c> against a temp copy of Samples/Beefball, whose only screen is GameScreen, the
/// startup screen.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class AddScreenCommandTests
{
    [StaFact]
    public void AddScreen_ShouldSaveTheScreenAndGenerateItsCode()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("add-screen", project.Root, "--name", "TitleScreen", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "name").ShouldBe("Screens\\TitleScreen");
        EditProperty(result, "baseScreen").ShouldBeNull();
        EditProperty(result, "isStartupScreen").ShouldBe("False");
        result.FilesWritten.ShouldContain("Screens/TitleScreen.glsj");

        File.ReadAllText(Gluj(project)).ShouldContain("\"Screens\\\\TitleScreen\"", Case.Sensitive);
        File.ReadAllText(Path.Combine(project.Root, "Beefball", "Screens", "TitleScreen.Generated.cs"))
            .ShouldContain("public partial class TitleScreen : FlatRedBall.Screens.Screen", Case.Sensitive);
        File.ReadAllText(Path.Combine(project.Root, "Beefball", "Beefball.csproj"))
            .ShouldContain("Screens\\TitleScreen.Generated.cs", Case.Sensitive);
    }

    [StaFact]
    public void AddScreen_ShouldInheritTheBase_WhenBaseIsPassed()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("add-screen", project.Root, "--name", "Level2", "--base", "GameScreen", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        EditProperty(Parse(output), "baseScreen").ShouldBe("Screens\\GameScreen");

        var screen = ReadJson(project, "Screens", "Level2.glsj");
        screen.GetProperty("BaseScreen").GetString().ShouldBe("Screens\\GameScreen");
        File.ReadAllText(Path.Combine(project.Root, "Beefball", "Screens", "Level2.Generated.cs"))
            .ShouldMatch(@"partial class Level2\s*:\s*[\w.]*GameScreen\b");
    }

    [StaFact]
    public void AddScreen_ShouldExitOneAndSaveNothing_WhenTheNameIsTaken()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var glujBefore = File.ReadAllText(Gluj(project));

        var (exitCode, output) = Run("add-screen", project.Root, "--name", "GameScreen", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        var result = Parse(output);
        result.EditFailures.ShouldHaveSingleItem().ShouldContain("There is already a Screen named GameScreen", Case.Sensitive);
        // Loading regenerates code and adds it to the .csproj; the failed edit must save no project data.
        result.FilesWritten.ShouldNotContain(file => file.EndsWith(".gluj") || file.EndsWith(".glej") || file.EndsWith(".glsj"));
        File.ReadAllText(Gluj(project)).ShouldBe(glujBefore);
    }

    static string Gluj(TempDir project) => Path.Combine(project.Root, "Beefball", "Beefball.gluj");

    static JsonElement ReadJson(TempDir project, params string[] pathUnderGameFolder) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(new[] { project.Root, "Beefball" }.Concat(pathUnderGameFolder).ToArray()))).RootElement;
}
