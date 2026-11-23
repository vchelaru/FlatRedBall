using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GlueCli;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;
using static GlueUnitTests.Cli.GlueCliTestRun;

namespace GlueUnitTests.Cli;

/// <summary>
/// <c>GlueCli describe</c> against a temp copy of Samples/Beefball: one GameScreen with lists of PlayerBall, Puck,
/// Goal and ScoreHud and a Walls ShapeCollection of AxisAlignedRectangles, and a PlayerBall entity that defines
/// MovementSpeed and tunnels CooldownCircleRadius.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class DescribeCommandTests
{
    [StaFact]
    public void Describe_ShouldListScreensAndEntities_WithoutElement()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("describe", project.Root, "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        var edit = (JsonElement)result.Edit!;
        edit.GetProperty("kind").GetString().ShouldBe("project");
        edit.GetProperty("startupScreen").GetString().ShouldBe("Screens\\GameScreen");
        edit.GetProperty("fileVersion").GetInt32().ShouldBeGreaterThan(0);
        Names(edit.GetProperty("screens")).ShouldBe(new[] { "Screens\\GameScreen" });
        Names(edit.GetProperty("entities")).ShouldBe(
            new[] { "Entities\\Goal", "Entities\\PlayerBall", "Entities\\Puck", "Entities\\ScoreHud" }, ignoreOrder: true);
        result.FilesWritten.ShouldNotContain(file => file.EndsWith(".glsj") || file.EndsWith(".glej") || file.EndsWith(".gluj"));

        var (_, text) = Run("describe", project.Root);
        text.ShouldContain("Screens\\GameScreen  (startup)", Case.Sensitive);
        text.ShouldContain("Entities\\PlayerBall", Case.Sensitive);
    }

    [StaFact]
    public void Describe_ShouldShowListChildrenAndObjectVariables_ForAScreen()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("describe", project.Root, "--element", "GameScreen", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var edit = (JsonElement)Parse(output).Edit!;
        edit.GetProperty("name").GetString().ShouldBe("Screens\\GameScreen");
        edit.GetProperty("elementType").GetString().ShouldBe("screen");

        var objects = edit.GetProperty("objects");
        var playerBallList = Find(objects, "PlayerBallList");
        playerBallList.GetProperty("genericType").GetString().ShouldBe("Entities\\PlayerBall");
        var children = playerBallList.GetProperty("children");
        Names(children).ShouldBe(new[] { "PlayerBall1", "PlayerBall2" });
        var playerBall1 = Find(children, "PlayerBall1");
        playerBall1.GetProperty("list").GetString().ShouldBe("PlayerBallList");
        playerBall1.GetProperty("sourceType").GetString().ShouldBe("Entity");
        playerBall1.GetProperty("type").GetString().ShouldBe("Entities\\PlayerBall");

        var wall1 = Find(Find(objects, "Walls").GetProperty("children"), "Wall1");
        wall1.GetProperty("type").GetString().ShouldBe("FlatRedBall.Math.Geometry.AxisAlignedRectangle");
        var width = wall1.GetProperty("variables").EnumerateArray().Single(item => item.GetProperty("name").GetString() == "Width");
        width.GetProperty("value").GetDouble().ShouldBe(800);

        var (_, text) = Run("describe", project.Root, "--element", "GameScreen");
        text.ShouldContain("  PlayerBallList  PositionedObjectList<Entities\\PlayerBall>", Case.Sensitive);
        text.ShouldContain("    Wall1  AxisAlignedRectangle  Height=30 Width=800 Y=300", Case.Sensitive);
    }

    [StaFact]
    public void Describe_ShouldShowCustomVariables_ForAnEntity()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("describe", project.Root, "--element", "Entities/PlayerBall", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var edit = (JsonElement)Parse(output).Edit!;
        edit.GetProperty("elementType").GetString().ShouldBe("entity");
        var variables = edit.GetProperty("variables");
        var speed = Find(variables, "MovementSpeed");
        speed.GetProperty("defaultValue").GetDouble().ShouldBe(300);
        speed.GetProperty("sourceObject").ValueKind.ShouldBe(JsonValueKind.Null);
        var radius = Find(variables, "CooldownCircleRadius");
        radius.GetProperty("sourceObject").GetString().ShouldBe("CooldownCircle");
        radius.GetProperty("sourceObjectProperty").GetString().ShouldBe("Radius");
    }

    [StaFact]
    public void Describe_ShouldExitOneWithCloseMatches_WhenTheElementIsUnknown()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("describe", project.Root, "--element", "PlayerBal");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        output.ShouldContain("No screen or entity named 'PlayerBal'.", Case.Sensitive, output);
        output.ShouldContain("Did you mean: Entities\\PlayerBall", Case.Sensitive, output);
    }

    [StaFact]
    public void Describe_ShouldShowAnObject_AddedByAnEarlierRun()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var (addExitCode, addOutput) = Run("add-object", project.Root, "--element", "GameScreen", "--type", "Sprite",
            "--name", "Background");
        addExitCode.ShouldBe(GlueCliRunner.ExitSuccess, addOutput);

        var (exitCode, output) = Run("describe", project.Root, "--element", "GameScreen", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var background = Find(((JsonElement)Parse(output).Edit!).GetProperty("objects"), "Background");
        background.GetProperty("type").GetString().ShouldBe("FlatRedBall.Sprite");
        background.GetProperty("list").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    static List<string?> Names(JsonElement array) =>
        array.EnumerateArray().Select(item => item.GetProperty("name").GetString()).ToList();

    static JsonElement Find(JsonElement array, string name) =>
        array.EnumerateArray().Single(item => item.GetProperty("name").GetString() == name);
}
