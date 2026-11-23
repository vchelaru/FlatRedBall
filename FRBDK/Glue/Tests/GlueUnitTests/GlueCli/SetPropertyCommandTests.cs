using System.Collections.Generic;
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
/// <c>GlueCli set-property</c> against a temp copy of Samples/Beefball. GameScreen holds Puck1 (in PuckList) and no
/// Layer; PlayerBall has a CircleInstance and no base entity; ScoreHud is not ICollidable.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class SetPropertyCommandTests
{
    [StaFact]
    public void SetProperty_ShouldPutAnObjectOnALayer()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        Run("add-object", project.Root, "--element", "GameScreen", "--type", "Layer", "--name", "GameLayer").exitCode
            .ShouldBe(GlueCliRunner.ExitSuccess);

        var (exitCode, output) = Run("set-property", project.Root, "--element", "GameScreen", "--object", "Puck1",
            "--property", "LayerOn", "--value", "gamelayer", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "property").ShouldBe("LayerOn");
        EditProperty(result, "newValue").ShouldBe("GameLayer");
        FindObject(ReadJson(project, "Screens", "GameScreen.glsj"), "Puck1").GetProperty("LayerOn").GetString().ShouldBe("GameLayer");

        var generated = File.ReadAllText(Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.Generated.cs"));
        generated.ShouldContain("Puck1.AddToManagers(GameLayer)", Case.Sensitive);
    }

    [StaFact]
    public void SetProperty_ShouldFailAndSaveNothing_WhenTheIsContainerQuestionIsUnanswered()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var entityFile = Path.Combine(project.Root, "Beefball", "Entities", "PlayerBall.glej");
        var entityBefore = File.ReadAllText(entityFile);

        // CircleInstance is a Circle but PlayerBall has no base, so Glue asks what to do about the container's type.
        var (exitCode, output) = Run("set-property", project.Root, "--element", "PlayerBall", "--object", "CircleInstance",
            "--property", "IsContainer", "--value", "true", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        var failure = Parse(output).EditFailures.ShouldHaveSingleItem();
        failure.ShouldContain("but the container is of type <NONE>", Case.Sensitive);
        failure.ShouldContain("Pass --container-base set or ignore", Case.Sensitive);
        File.ReadAllText(entityFile).ShouldBe(entityBefore);
    }

    [StaFact]
    public void SetProperty_ShouldMarkAnEntityObjectAsContainer_WhenTheQuestionIsAnswered()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("set-property", project.Root, "--element", "PlayerBall", "--object", "CircleInstance",
            "--property", "IsContainer", "--value", "true", "--container-base", "ignore", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var answer = EditObjects(Parse(output), "answers").ShouldHaveSingleItem();
        answer.GetProperty("question").GetString().ShouldContain("but the container is of type <NONE>", Case.Sensitive);
        answer.GetProperty("answered").GetString().ShouldBe("ignore (--container-base)");

        var entity = ReadJson(project, "Entities", "PlayerBall.glej");
        StoredProperty(FindObject(entity, "CircleInstance"), "IsContainer").GetBoolean().ShouldBeTrue();
        entity.TryGetProperty("BaseEntity", out _).ShouldBeFalse();

        var generated = File.ReadAllText(Path.Combine(project.Root, "Beefball", "Entities", "PlayerBall.Generated.cs"));
        generated.ShouldContain("mCircleInstance = this;", Case.Sensitive);
    }

    [StaFact]
    public void SetProperty_ShouldSetAnElementProperty_AndRegenerateItsCode()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("set-property", project.Root, "--element", "ScoreHud",
            "--property", "ImplementsICollidable", "--value", "True", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "object").ShouldBeNull();
        EditProperty(result, "oldValue").ShouldBe("False");
        EditProperty(result, "newValue").ShouldBe("True");

        StoredProperty(ReadJson(project, "Entities", "ScoreHud.glej"), "ImplementsICollidable").GetBoolean().ShouldBeTrue();
        var generated = File.ReadAllText(Path.Combine(project.Root, "Beefball", "Entities", "ScoreHud.Generated.cs"));
        generated.ShouldContain("FlatRedBall.Math.Geometry.ICollidable", Case.Sensitive);
    }

    [StaFact]
    public void SetProperty_ShouldPointToRename_ForInstanceName()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("set-property", project.Root, "--element", "GameScreen", "--object", "Puck1",
            "--property", "InstanceName", "--value", "Puck2");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        output.ShouldContain("set-property does not rename. Use: rename <project> --element Screens\\GameScreen --object Puck1 --to <new name>",
            Case.Sensitive, output);
    }

    [StaFact]
    public void SetProperty_ShouldExitOneWithCloseMatches_WhenThePropertyIsUnknown()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("set-property", project.Root, "--element", "GameScreen", "--object", "Puck1",
            "--property", "LayrOn", "--value", "GameLayer");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        output.ShouldContain("Puck1 in Screens\\GameScreen has no property 'LayrOn'.", Case.Sensitive, output);
        output.ShouldContain("Did you mean: LayerOn", Case.Sensitive, output);
    }

    [StaFact]
    public void SetProperty_ShouldPointToSetVariable_ForAVariable()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("set-property", project.Root, "--element", "GameScreen", "--object", "Wall1",
            "--property", "Width", "--value", "5");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        output.ShouldContain("'Width' is a variable of Wall1 in Screens\\GameScreen, not a property.", Case.Sensitive, output);
    }

    static JsonElement ReadJson(TempDir project, string folder, string file) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(project.Root, "Beefball", folder, file))).RootElement;

    /// <summary>A value kept in the save class's Properties list (IsContainer, ImplementsICollidable).</summary>
    static JsonElement StoredProperty(JsonElement owner, string name) =>
        owner.GetProperty("Properties").EnumerateArray().Single(item => item.GetProperty("Name").GetString() == name).GetProperty("Value");

    static IEnumerable<JsonElement> EditObjects(CliResult result, string camelCaseName) =>
        ((JsonElement)result.Edit!).GetProperty(camelCaseName).EnumerateArray().ToList();

    static JsonElement FindObject(JsonElement element, string instanceName) =>
        AllObjects(element.GetProperty("NamedObjects")).Single(nos => nos.GetProperty("InstanceName").GetString() == instanceName);

    static IEnumerable<JsonElement> AllObjects(JsonElement namedObjects)
    {
        foreach (var nos in namedObjects.EnumerateArray())
        {
            yield return nos;
            if (nos.TryGetProperty("ContainedObjects", out var contained))
            {
                foreach (var child in AllObjects(contained))
                {
                    yield return child;
                }
            }
        }
    }
}
