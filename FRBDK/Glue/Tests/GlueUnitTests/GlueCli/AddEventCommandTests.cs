using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using GlueCli;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;
using static GlueUnitTests.Cli.GlueCliTestRun;

namespace GlueUnitTests.Cli;

/// <summary>
/// <c>GlueCli add-event</c> and <c>remove-event</c> against a temp copy of Samples/Beefball. GameScreen has the
/// collision relationships PlayerBallVsPuck and PuckVsGoal, and one event, PuckVsGoalCollided, tunneling
/// PuckVsGoal.CollisionOccurred, with a hand-written handler in GameScreen.Event.cs. No entity has events.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class AddEventCommandTests
{
    [StaFact]
    public void AddEvent_ShouldTunnelACollisionRelationshipsEvent_AndAddOneHandler_AndTheProjectShouldBuild()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        Regen(project.Root);
        var beefball = Path.Combine(project.Root, "Beefball");

        var (exitCode, output) = Run("add-event", project.Root, "--element", "GameScreen", "--object", "PlayerBallVsPuck",
            "--object-event", "CollisionOccurred", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "name").ShouldBe("PlayerBallVsPuckCollisionOccurred");
        EditProperty(result, "kind").ShouldBe("tunneled");
        EditProperty(result, "generatedFile").ShouldBe("Screens/GameScreen.Generated.Event.cs");
        EditProperty(result, "handlerFile").ShouldBe("Screens/GameScreen.Event.cs");
        EditProperty(result, "handlerAdded").ShouldBe("True");
        result.FilesWritten.ShouldContain("Screens/GameScreen.glsj");
        result.FilesWritten.ShouldContain("Screens/GameScreen.Event.cs");

        var added = ReadJson(project, "Screens", "GameScreen.glsj").GetProperty("Events").EnumerateArray()
            .Single(item => item.GetProperty("EventName").GetString() == "PlayerBallVsPuckCollisionOccurred");
        added.GetProperty("SourceObject").GetString().ShouldBe("PlayerBallVsPuck");
        added.GetProperty("SourceObjectEvent").GetString().ShouldBe("CollisionOccurred");

        File.ReadAllText(Path.Combine(beefball, "Screens", "GameScreen.Generated.Event.cs"))
            .ShouldContain("void OnPlayerBallVsPuckCollisionOccurredTunnel (Entities.PlayerBall playerBall, Entities.Puck puck)", Case.Sensitive);
        File.ReadAllText(Path.Combine(beefball, "Screens", "GameScreen.Generated.cs"))
            .ShouldContain("PlayerBallVsPuck.CollisionOccurred += OnPlayerBallVsPuckCollisionOccurredTunnel;", Case.Sensitive);

        // GameScreen.Event.cs ends with a hand-written method. The stub goes on its own line, so the runner's
        // second code generation sees it rather than adding it again.
        var handlers = File.ReadAllText(Path.Combine(beefball, "Screens", "GameScreen.Event.cs"));
        Regex.Matches(handlers, @"void OnPlayerBallVsPuckCollisionOccurred \(Entities\.PlayerBall playerBall, Entities\.Puck puck\)").Count.ShouldBe(1, handlers);
        handlers.ShouldContain("void OnPuckVsGoalCollided(Entities.Puck puck, Entities.Goal goal)", Case.Sensitive);
        handlers.ShouldNotContain("}        void", Case.Sensitive);

        var (buildExitCode, buildOutput) = NestedDotnetCli.Run($"build \"{Path.Combine(beefball, "Beefball.csproj")}\" -c Debug");
        buildExitCode.ShouldBe(0, $"dotnet build failed after add-event:\n{buildOutput}");
    }

    [StaFact]
    public void AddEvent_ShouldExposeAVariablesChangeEvent_AndCreateTheEventFiles_AndTheProjectShouldBuild()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        Regen(project.Root);
        var beefball = Path.Combine(project.Root, "Beefball");
        // The Add Event window offers a variable's change events only once the variable creates events.
        var glejPath = Path.Combine(beefball, "Entities", "PlayerBall.glej");
        var glej = JsonNode.Parse(File.ReadAllText(glejPath))!;
        glej["CustomVariables"]!.AsArray().Single(variable => (string?)variable!["Name"] == "MovementSpeed")!["CreatesEvent"] = true;
        File.WriteAllText(glejPath, glej.ToJsonString());

        var (exitCode, output) = Run("add-event", project.Root, "--element", "Entities\\PlayerBall", "--variable", "MovementSpeed",
            "--when", "after", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "name").ShouldBe("AfterMovementSpeedSet");
        EditProperty(result, "kind").ShouldBe("variable");
        EditProperty(result, "handlerAdded").ShouldBe("True");
        result.FilesWritten.ShouldContain("Entities/PlayerBall.Event.cs");
        result.FilesWritten.ShouldContain("Entities/PlayerBall.Generated.Event.cs");

        var added = ReadJson(project, "Entities", "PlayerBall.glej").GetProperty("Events").EnumerateArray().Single();
        added.GetProperty("EventName").GetString().ShouldBe("AfterMovementSpeedSet");
        added.GetProperty("SourceVariable").GetString().ShouldBe("MovementSpeed");

        File.ReadAllText(Path.Combine(beefball, "Entities", "PlayerBall.Event.cs"))
            .ShouldContain("void OnAfterMovementSpeedSet (object sender, EventArgs e)", Case.Sensitive);
        File.ReadAllText(Path.Combine(beefball, "Entities", "PlayerBall.Generated.cs"))
            .ShouldContain("AfterMovementSpeedSet += OnAfterMovementSpeedSet;", Case.Sensitive);
        var csproj = File.ReadAllText(Path.Combine(beefball, "Beefball.csproj"));
        csproj.ShouldContain(@"Entities\PlayerBall.Event.cs", Case.Sensitive);
        csproj.ShouldContain(@"Entities\PlayerBall.Generated.Event.cs", Case.Sensitive);

        var (buildExitCode, buildOutput) = NestedDotnetCli.Run($"build \"{Path.Combine(beefball, "Beefball.csproj")}\" -c Debug");
        buildExitCode.ShouldBe(0, $"dotnet build failed after add-event:\n{buildOutput}");
    }

    [StaFact]
    public void AddEvent_ShouldExitOneAndSaveNothing_WhenTheNameIsTaken()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var glsjPath = Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.glsj");
        var glsjBefore = File.ReadAllText(glsjPath);

        var (exitCode, output) = Run("add-event", project.Root, "--element", "GameScreen", "--custom", "--name", "PuckVsGoalCollided", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        var result = Parse(output);
        result.EditFailures.ShouldHaveSingleItem().ShouldContain("already has an event named PuckVsGoalCollided", Case.Sensitive);
        // Loading regenerates code (GameScreen.Generated.Event.cs among it); the failed edit must save no project data.
        result.FilesWritten.ShouldNotContain(file => file.EndsWith(".gluj") || file.EndsWith(".glej") || file.EndsWith(".glsj"));
        result.FilesWritten.ShouldNotContain("Screens/GameScreen.Event.cs");
        File.ReadAllText(glsjPath).ShouldBe(glsjBefore);
    }

    [StaFact]
    public void AddEvent_ShouldExitOneAndSuggestTheEvent_WhenTheObjectEventIsUnknown()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("add-event", project.Root, "--element", "GameScreen", "--object", "PlayerBallVsPuck",
            "--object-event", "CollisionOccured", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        var failure = Parse(output).EditFailures.ShouldHaveSingleItem();
        failure.ShouldContain("PlayerBallVsPuck has no event 'CollisionOccured'", Case.Sensitive);
        failure.ShouldContain("CollisionOccurred", Case.Sensitive);
        Parse(output).FilesWritten.ShouldNotContain("Screens/GameScreen.glsj");
    }

    [StaFact]
    public void RemoveEvent_ShouldRemoveTheEvent_AndKeepItsHandler()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var beefball = Path.Combine(project.Root, "Beefball");

        var (exitCode, output) = Run("remove-event", project.Root, "--element", "GameScreen", "--name", "PuckVsGoalCollided", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        EditProperty(Parse(output), "handler").ShouldBe("OnPuckVsGoalCollided");
        ReadJson(project, "Screens", "GameScreen.glsj").TryGetProperty("Events", out _).ShouldBeFalse();
        File.ReadAllText(Path.Combine(beefball, "Screens", "GameScreen.Generated.cs")).ShouldNotContain("PuckVsGoalCollided", Case.Sensitive);
        File.ReadAllText(Path.Combine(beefball, "Beefball.csproj")).ShouldNotContain(@"Screens\GameScreen.Generated.Event.cs", Case.Sensitive);
        File.ReadAllText(Path.Combine(beefball, "Screens", "GameScreen.Event.cs")).ShouldContain("void OnPuckVsGoalCollided(", Case.Sensitive);
    }

    static JsonElement ReadJson(TempDir project, string folder, string file) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(project.Root, "Beefball", folder, file))).RootElement;
}
