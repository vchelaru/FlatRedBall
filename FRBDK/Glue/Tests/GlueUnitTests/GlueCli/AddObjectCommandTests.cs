using System.IO;
using System.Linq;
using System.Text.Json;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GlueCli;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;
using static GlueUnitTests.Cli.GlueCliTestRun;

namespace GlueUnitTests.Cli;

/// <summary>
/// <c>GlueCli add-object</c> against a temp copy of Samples/Beefball, whose GameScreen has a PlayerBallList,
/// a PuckList, a GoalList, a ScoreHudList and a Walls ShapeCollection.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class AddObjectCommandTests
{
    [StaFact]
    public void AddObject_ShouldSaveTheSpriteAndGenerateItsCode()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("add-object", project.Root, "--element", "GameScreen", "--type", "Sprite", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "element").ShouldBe("Screens\\GameScreen");
        EditProperty(result, "name").ShouldBe("SpriteInstance");
        EditProperty(result, "type").ShouldBe("FlatRedBall.Sprite");
        EditProperty(result, "list").ShouldBeNull();
        result.FilesWritten.ShouldContain("Screens/GameScreen.glsj");

        var screen = ReadScreen(project);
        var sprite = screen.GetProperty("NamedObjects").EnumerateArray()
            .Single(nos => nos.GetProperty("InstanceName").GetString() == "SpriteInstance");
        sprite.GetProperty("SourceClassType").GetString().ShouldBe("FlatRedBall.Sprite");

        var generated = File.ReadAllText(Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.Generated.cs"));
        generated.ShouldContain("FlatRedBall.Sprite SpriteInstance");
    }

    [StaFact]
    public void AddObject_ShouldPutAnEntityInstanceInTheListOfItsType()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("add-object", project.Root, "--element", "Screens/GameScreen", "--type", "PlayerBall",
            "--name", "PlayerBall3", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "list").ShouldBe("PlayerBallList");
        EditProperty(result, "type").ShouldBe("Entities\\PlayerBall");

        var playerBallList = ReadScreen(project).GetProperty("NamedObjects").EnumerateArray()
            .Single(nos => nos.GetProperty("InstanceName").GetString() == "PlayerBallList");
        playerBallList.GetProperty("ContainedObjects").EnumerateArray()
            .Select(nos => nos.GetProperty("InstanceName").GetString())
            .ShouldContain("PlayerBall3");
    }

    [StaFact]
    public void AddObject_ShouldLeaveNothingSelected_OnceTheProjectIsUnloaded()
    {
        var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        try
        {
            Run("add-object", project.Root, "--element", "GameScreen", "--type", "Sprite").exitCode
                .ShouldBe(GlueCliRunner.ExitSuccess);
            GlueState.Self.CurrentElement.ShouldNotBeNull();
        }
        finally
        {
            project.Dispose();
        }

        // add-object selects its target element. If that outlived the project, a later "generate the current
        // element" would write this GameScreen's code into the next project loaded in the process.
        GlueState.Self.CurrentElement.ShouldBeNull();
    }

    [StaFact]
    public void AddObject_ShouldGenerateCodeThatMatchesTheSavedList_WhenAddingAList()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("add-object", project.Root, "--element", "GameScreen", "--type", "Text", "--as-list", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "name").ShouldBe("TextList");
        EditProperty(result, "genericType").ShouldBe("FlatRedBall.Graphics.Text");

        // NewNamedObjectPlugin makes every new list ExposedInDerived, which generates it protected. The code
        // written by the add must already reflect that, not only the code a later regeneration writes.
        var textList = ReadScreen(project).GetProperty("NamedObjects").EnumerateArray()
            .Single(nos => nos.GetProperty("InstanceName").GetString() == "TextList");
        textList.GetProperty("ExposedInDerived").GetBoolean().ShouldBeTrue();
        var generated = File.ReadAllText(Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.Generated.cs"));
        generated.ShouldContain("protected global::FlatRedBall.Math.PositionedObjectList<global::FlatRedBall.Graphics.Text> TextList", Case.Sensitive);
    }

    [StaFact]
    public void AddObject_ShouldExitOneAndSaveNothing_WhenTheNameIsTaken()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var screenFile = Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.glsj");
        var gluj = Path.Combine(project.Root, "Beefball", "Beefball.gluj");
        var screenBefore = File.ReadAllBytes(screenFile);
        var glujBefore = File.ReadAllText(gluj);

        var (exitCode, output) = Run("add-object", project.Root, "--element", "GameScreen", "--type", "Circle",
            "--name", "PuckList", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        var result = Parse(output);
        result.EditFailures.ShouldHaveSingleItem().ShouldContain("PuckList", Case.Sensitive);
        result.Edit.ShouldBeNull();
        // Selecting GameScreen must not save it either: the property grid plugin used to add the screen's
        // DefaultLayer variable and save on selection, even headless (#2476).
        result.FilesWritten.ShouldNotContain("Screens/GameScreen.glsj");
        File.ReadAllBytes(screenFile).ShouldBe(screenBefore);
        File.ReadAllText(gluj).ShouldBe(glujBefore);
    }

    [StaFact]
    public void AddObject_ShouldLeaveTheEntityFileUntouched_WhenTheNameIsInvalid()
    {
        // Entities get their own selection reactions (input movement, performance), on top of the
        // property grid a screen gets (#2476).
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var entityFile = Path.Combine(project.Root, "Beefball", "Entities", "Puck.glej");
        var entityBefore = File.ReadAllBytes(entityFile);

        var (exitCode, output) = Run("add-object", project.Root, "--element", "Puck", "--type", "Circle",
            "--name", "Puck", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        Parse(output).FilesWritten.ShouldNotContain("Entities/Puck.glej");
        File.ReadAllBytes(entityFile).ShouldBe(entityBefore);
    }

    [StaFact]
    public void AddObject_ShouldExitOneAndSuggestCloseMatches_WhenTheTypeIsUnknown()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("add-object", project.Root, "--element", "GameScreen", "--type", "PlayerBal");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        output.ShouldContain("No entity or object type named 'PlayerBal'.", Case.Sensitive, output);
        output.ShouldContain("Did you mean: Entities\\PlayerBall", Case.Sensitive, output);
    }

    static JsonElement ReadScreen(TempDir project) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.glsj"))).RootElement;
}
