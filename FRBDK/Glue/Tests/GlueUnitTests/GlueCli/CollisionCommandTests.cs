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
/// <c>GlueCli add-collision-relationship</c> and <c>set-collision-property</c> against a temp copy of Samples/Beefball.
/// GameScreen has PlayerBallList, PuckList, GoalList and ScoreHudList (ScoreHud is not ICollidable), a Walls
/// ShapeCollection, and relationships including PlayerBallVsGoal and PuckVsGoal.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class CollisionCommandTests
{
    [StaFact]
    public void AddCollisionRelationship_ShouldAddABounceRelationship_AndTheProjectShouldBuild()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var beefball = Path.Combine(project.Root, "Beefball");
        Regen(project.Root);

        var (exitCode, output) = Run("add-collision-relationship", project.Root, "--element", "GameScreen",
            "--first", "PlayerBallList", "--second", "GoalList", "--type", "bounce", "--first-mass", "0", "--elasticity", "0.5", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        // PlayerBallVsGoal exists, so the auto name is numbered.
        EditProperty(result, "name").ShouldBe("PlayerBallVsGoal2");
        EditProperty(result, "first").ShouldBe("PlayerBallList");
        EditProperty(result, "second").ShouldBe("GoalList");
        EditProperty(result, "collisionType").ShouldBe("BounceCollision");
        EditProperty(result, "swapped").ShouldBe("False");

        var added = ReadGameScreen(project).GetProperty("NamedObjects").EnumerateArray()
            .Single(item => item.GetProperty("InstanceName").GetString() == "PlayerBallVsGoal2");
        added.GetProperty("SourceClassType").GetString().ShouldBe("FlatRedBall.Math.Collision.ListVsListRelationship<Entities.PlayerBall, Entities.Goal>");
        Property(added, "FirstCollisionName").GetString().ShouldBe("PlayerBallList");
        Property(added, "SecondCollisionName").GetString().ShouldBe("GoalList");
        Property(added, "CollisionType").GetInt32().ShouldBe(2);
        Property(added, "FirstCollisionMass").GetDouble().ShouldBe(0);
        Property(added, "CollisionElasticity").GetDouble().ShouldBe(0.5);

        var generated = File.ReadAllText(Path.Combine(beefball, "Screens", "GameScreen.Generated.cs"));
        generated.ShouldContain("PlayerBallVsGoal2 = FlatRedBall.Math.Collision.CollisionManager.Self.CreateRelationship(PlayerBallList, GoalList);", Case.Sensitive);
        // The second mass keeps its unstored default, which codegen writes as 1.0f.
        generated.ShouldContain("PlayerBallVsGoal2.SetBounceCollision(0f, 1.0f, 0.5f);", Case.Sensitive);

        var (buildExitCode, buildOutput) = NestedDotnetCli.Run($"build \"{Path.Combine(beefball, "Beefball.csproj")}\" -c Debug");
        buildExitCode.ShouldBe(0, $"dotnet build failed after add-collision-relationship:\n{buildOutput}");
    }

    [StaFact]
    public void AddCollisionRelationship_ShouldExitOneAndSuggestTheObject_WhenAnObjectIsUnknown()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var glsjPath = Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.glsj");
        var glsjBefore = File.ReadAllText(glsjPath);

        var (exitCode, output) = Run("add-collision-relationship", project.Root, "--element", "GameScreen",
            "--first", "PlayerBallList", "--second", "GoalLst", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        var failure = Parse(output).EditFailures.ShouldHaveSingleItem();
        failure.ShouldContain("has no top-level object 'GoalLst'", Case.Sensitive);
        failure.ShouldContain("GoalList", Case.Sensitive);
        File.ReadAllText(glsjPath).ShouldBe(glsjBefore);
    }

    [StaFact]
    public void AddCollisionRelationship_ShouldExitOne_WhenTheNameIsTaken()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var glsjPath = Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.glsj");
        var glsjBefore = File.ReadAllText(glsjPath);

        var (exitCode, output) = Run("add-collision-relationship", project.Root, "--element", "GameScreen",
            "--first", "PlayerBallList", "--second", "GoalList", "--name", "PuckVsGoal", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        Parse(output).EditFailures.ShouldHaveSingleItem().ShouldContain("already has an object named PuckVsGoal", Case.Sensitive);
        File.ReadAllText(glsjPath).ShouldBe(glsjBefore);
    }

    [StaFact]
    public void AddCollisionRelationship_ShouldExitOne_WhenAnObjectCannotCollide()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("add-collision-relationship", project.Root, "--element", "GameScreen",
            "--first", "ScoreHudList", "--second", "PuckList", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        var failure = Parse(output).EditFailures.ShouldHaveSingleItem();
        failure.ShouldContain("ScoreHudList in Screens\\GameScreen cannot collide", Case.Sensitive);
        Parse(output).FilesWritten.ShouldNotContain("Screens/GameScreen.glsj");
    }

    [StaFact]
    public void AddCollisionRelationship_WithAName_ShouldTurnOffAutoNaming()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("add-collision-relationship", project.Root, "--element", "GameScreen",
            "--first", "Walls", "--second", "GoalList", "--name", "GoalsHitWalls", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "name").ShouldBe("GoalsHitWalls");
        // A ShapeCollection cannot be first.
        EditProperty(result, "first").ShouldBe("GoalList");
        EditProperty(result, "swapped").ShouldBe("True");
        var added = ReadGameScreen(project).GetProperty("NamedObjects").EnumerateArray()
            .Single(item => item.GetProperty("InstanceName").GetString() == "GoalsHitWalls");
        Property(added, "IsAutoNameEnabled").GetBoolean().ShouldBeFalse();
    }

    [StaFact]
    public void SetCollisionProperty_ShouldChangeTheSetting_AndRegenerate()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var beefball = Path.Combine(project.Root, "Beefball");

        var (exitCode, output) = Run("set-collision-property", project.Root, "--element", "GameScreen",
            "--relationship", "PuckVsGoal", "--property", "CollisionType", "--value", "move", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "oldValue").ShouldBe("NoPhysics");
        EditProperty(result, "newValue").ShouldBe("MoveCollision");
        var relationship = ReadGameScreen(project).GetProperty("NamedObjects").EnumerateArray()
            .Single(item => item.GetProperty("InstanceName").GetString() == "PuckVsGoal");
        Property(relationship, "CollisionType").GetInt32().ShouldBe(1);
        File.ReadAllText(Path.Combine(beefball, "Screens", "GameScreen.Generated.cs"))
            .ShouldContain("PuckVsGoal.SetMoveCollision(", Case.Sensitive);
    }

    [StaFact]
    public void SetCollisionProperty_ShouldExitOneAndListValues_WhenTheValueIsInvalid()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("set-collision-property", project.Root, "--element", "GameScreen",
            "--relationship", "PuckVsGoal", "--property", "FirstSubCollisionSelectedItem", "--value", "NoSuchShape", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        Parse(output).EditFailures.ShouldHaveSingleItem().ShouldContain("<Entire Object>", Case.Sensitive);
    }

    static JsonElement ReadGameScreen(TempDir project) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.glsj"))).RootElement;

    static JsonElement Property(JsonElement namedObject, string name) =>
        namedObject.GetProperty("Properties").EnumerateArray().Single(item => item.GetProperty("Name").GetString() == name).GetProperty("Value");
}
