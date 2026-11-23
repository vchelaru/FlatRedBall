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
/// <c>GlueCli rename</c> against a temp copy of Samples/Beefball. PlayerBall has a CooldownCircle that two of its
/// variables tunnel into; GameScreen has a PuckVsGoal collision relationship its PuckVsGoalCollided event uses, a
/// ScoreHudList holding ScoreHud1, and is the startup screen.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class RenameCommandTests
{
    [StaFact]
    public void Rename_ShouldRenameAnObject_AndTheVariablesThatTunnelIntoIt()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("rename", project.Root, "--element", "PlayerBall", "--object", "CooldownCircle",
            "--to", "DashCircle", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "kind").ShouldBe("object");
        EditProperty(result, "oldName").ShouldBe("CooldownCircle");
        EditProperty(result, "newName").ShouldBe("DashCircle");
        EditList(result, "updatedReferences").ShouldBe(new[]
        {
            "variable Entities\\PlayerBall.CooldownCircleRadius",
            "variable Entities\\PlayerBall.CooldownCircleColor",
        });

        var entity = ReadJson(project, "Entities", "PlayerBall.glej");
        AllObjects(entity.GetProperty("NamedObjects")).Select(nos => nos.GetProperty("InstanceName").GetString())
            .ShouldBe(new[] { "CircleInstance", "DashCircle" });
        entity.GetProperty("CustomVariables").EnumerateArray()
            .Where(variable => variable.TryGetProperty("SourceObject", out _))
            .Select(variable => variable.GetProperty("Name").GetString() + "->" + variable.GetProperty("SourceObject").GetString())
            .ShouldBe(new[] { "CooldownCircleRadius->DashCircle", "CircleInstanceColor->CircleInstance", "CooldownCircleColor->DashCircle" });

        var generated = File.ReadAllText(Path.Combine(project.Root, "Beefball", "Entities", "PlayerBall.Generated.cs"));
        generated.ShouldContain("DashCircle.Radius", Case.Sensitive);
        generated.ShouldNotContain("CooldownCircle.", Case.Sensitive);
    }

    [StaFact]
    public void Rename_ShouldRepointTheEvent_WhenRenamingItsSourceObject()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("rename", project.Root, "--element", "GameScreen", "--object", "PuckVsGoal",
            "--to", "PuckGoalCollision", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        EditList(Parse(output), "updatedReferences").ShouldBe(new[] { "event Screens\\GameScreen.PuckVsGoalCollided" });

        var screen = ReadJson(project, "Screens", "GameScreen.glsj");
        screen.GetProperty("Events").EnumerateArray().Single().GetProperty("SourceObject").GetString().ShouldBe("PuckGoalCollision");

        var generated = File.ReadAllText(Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.Generated.cs"));
        generated.ShouldContain("PuckGoalCollision.CollisionOccurred +=", Case.Sensitive);
        generated.ShouldNotContain("PuckVsGoal.", Case.Sensitive);
    }

    [StaFact]
    public void Rename_ShouldRenameAnEntity_ItsFilesAndTheObjectsThatUseIt_AndTheProjectShouldBuild()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        Regen(project.Root);
        var beefball = Path.Combine(project.Root, "Beefball");

        var (exitCode, output) = Run("rename", project.Root, "--element", "Entities\\ScoreHud", "--to", "Scoreboard", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "kind").ShouldBe("entity");
        EditProperty(result, "newName").ShouldBe("Entities\\Scoreboard");
        var renamedFiles = EditObjects(result, "renamedFiles").Select(file => file.GetProperty("from").GetString() + " -> " + file.GetProperty("to").GetString()).ToList();
        renamedFiles.ShouldContain("Entities/ScoreHud.glej -> Entities/Scoreboard.glej");
        renamedFiles.ShouldContain("Entities/ScoreHud.cs -> Entities/Scoreboard.cs");
        EditList(result, "updatedReferences").ShouldContain("list type of Screens\\GameScreen.ScoreHudList");
        EditList(result, "changedElements").ShouldContain("Screens\\GameScreen");

        File.Exists(Path.Combine(beefball, "Entities", "ScoreHud.glej")).ShouldBeFalse();
        File.Exists(Path.Combine(beefball, "Entities", "ScoreHud.cs")).ShouldBeFalse();
        File.ReadAllText(Path.Combine(beefball, "Entities", "Scoreboard.cs")).ShouldContain("partial class Scoreboard", Case.Sensitive);
        ReadJson(project, "Entities", "Scoreboard.glej").GetProperty("Name").GetString().ShouldBe("Entities\\Scoreboard");

        var gluj = JsonDocument.Parse(File.ReadAllText(Path.Combine(beefball, "Beefball.gluj"))).RootElement;
        gluj.GetProperty("EntityReferences").EnumerateArray().Select(item => item.GetProperty("Name").GetString())
            .ShouldBe(new[] { "Entities\\Goal", "Entities\\PlayerBall", "Entities\\Puck", "Entities\\Scoreboard" });

        var screen = ReadJson(project, "Screens", "GameScreen.glsj");
        FindObject(screen, "ScoreHudList").GetProperty("SourceClassGenericType").GetString().ShouldBe("Entities\\Scoreboard");
        FindObject(screen, "ScoreHud1").GetProperty("SourceClassType").GetString().ShouldBe("Entities\\Scoreboard");

        var csproj = File.ReadAllText(Path.Combine(beefball, "Beefball.csproj"));
        csproj.ShouldContain(@"Entities\Scoreboard.cs", Case.Sensitive);
        csproj.ShouldNotContain(@"Entities\ScoreHud.cs", Case.Sensitive);

        var (buildExitCode, buildOutput) = NestedDotnetCli.Run($"build \"{Path.Combine(beefball, "Beefball.csproj")}\" -c Debug");
        buildExitCode.ShouldBe(0, $"dotnet build failed after the rename:\n{buildOutput}");
    }

    [StaFact]
    public void Rename_ShouldRenameAScreen_AndTheStartupScreen()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var beefball = Path.Combine(project.Root, "Beefball");

        var (exitCode, output) = Run("rename", project.Root, "--element", "GameScreen", "--to", "MatchScreen", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "kind").ShouldBe("screen");
        EditList(result, "updatedReferences").ShouldContain("startup screen");

        File.Exists(Path.Combine(beefball, "Screens", "GameScreen.glsj")).ShouldBeFalse();
        ReadJson(project, "Screens", "MatchScreen.glsj").GetProperty("Name").GetString().ShouldBe("Screens\\MatchScreen");
        File.ReadAllText(Path.Combine(beefball, "Screens", "MatchScreen.cs")).ShouldContain("partial class MatchScreen", Case.Sensitive);
        File.ReadAllText(Path.Combine(beefball, "Screens", "MatchScreen.Event.cs")).ShouldContain("partial class MatchScreen", Case.Sensitive);

        var gluj = JsonDocument.Parse(File.ReadAllText(Path.Combine(beefball, "Beefball.gluj"))).RootElement;
        gluj.GetProperty("StartUpScreen").GetString().ShouldBe("Screens\\MatchScreen");
        gluj.GetProperty("ScreenReferences").EnumerateArray().Single().GetProperty("Name").GetString().ShouldBe("Screens\\MatchScreen");
    }

    [StaFact]
    public void Rename_ShouldRenameAFile_AndTheFileOnDisk()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var beefball = Path.Combine(project.Root, "Beefball");
        AddBackgroundFileToGameScreen(beefball);

        var (exitCode, output) = Run("rename", project.Root, "--element", "GameScreen", "--file", "Background.png",
            "--to", "Field", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "kind").ShouldBe("file");
        EditProperty(result, "oldName").ShouldBe("Screens/GameScreen/Background.png");
        EditProperty(result, "newName").ShouldBe("Screens/GameScreen/Field.png");

        File.Exists(Path.Combine(beefball, "Content", "Screens", "GameScreen", "Background.png")).ShouldBeFalse();
        File.Exists(Path.Combine(beefball, "Content", "Screens", "GameScreen", "Field.png")).ShouldBeTrue();
        ReadJson(project, "Screens", "GameScreen.glsj").GetProperty("ReferencedFiles").EnumerateArray().Single()
            .GetProperty("Name").GetString().ShouldBe("Screens/GameScreen/Field.png");
        var generated = File.ReadAllText(Path.Combine(beefball, "Screens", "GameScreen.Generated.cs"));
        generated.ShouldContain("Texture2D Field", Case.Sensitive);
        generated.ShouldNotContain("Background", Case.Sensitive);
    }

    [StaFact]
    public void Rename_ShouldExitOneAndSaveNothing_WhenAnotherEntityHasTheName()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var snapshot = SnapshotElementFiles(project);

        var (exitCode, output) = Run("rename", project.Root, "--element", "ScoreHud", "--to", "Puck", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        var result = Parse(output);
        result.EditFailures.ShouldHaveSingleItem().ShouldBe("Cannot rename Entities\\ScoreHud to Entities\\Puck: There is already an entity named Puck");
        result.Edit.ShouldBeNull();
        ChangedSince(project, snapshot).ShouldBeEmpty();
    }

    [StaFact]
    public void Rename_ShouldExitOneAndSaveNothing_WhenTheObjectNameIsInvalid()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var snapshot = SnapshotElementFiles(project);

        var (exitCode, output) = Run("rename", project.Root, "--element", "PlayerBall", "--object", "CooldownCircle",
            "--to", "2Circle", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        Parse(output).EditFailures.ShouldHaveSingleItem().ShouldStartWith("Cannot rename CooldownCircle in Entities\\PlayerBall to 2Circle: ");
        ChangedSince(project, snapshot).ShouldBeEmpty();
    }

    [StaFact]
    public void Rename_ShouldExitOne_WhenTheObjectNameIsTaken()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var snapshot = SnapshotElementFiles(project);

        var (exitCode, output) = Run("rename", project.Root, "--element", "PlayerBall", "--object", "CooldownCircle",
            "--to", "CircleInstance");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        output.ShouldContain("Cannot rename CooldownCircle in Entities\\PlayerBall to CircleInstance", Case.Sensitive, output);
        ChangedSince(project, snapshot).ShouldBeEmpty();
    }

    // Wall2 is inside the Walls ShapeCollection, which the screen's name check used to skip.
    [StaFact]
    public void Rename_ShouldExitOne_WhenAnObjectInAListHasTheName()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("rename", project.Root, "--element", "GameScreen", "--object", "Wall1", "--to", "Wall2");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        output.ShouldContain("Cannot rename Wall1 in Screens\\GameScreen to Wall2", Case.Sensitive, output);
    }

    [StaFact]
    public void Rename_ShouldSuggestCloseMatches_WhenTheObjectIsUnknown()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("rename", project.Root, "--element", "PlayerBall", "--object", "CooldownCircl", "--to", "X");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        output.ShouldContain("Entities\\PlayerBall has no object 'CooldownCircl'. Did you mean: CooldownCircle?", Case.Sensitive, output);
    }

    // The editor asks whether to overwrite; declining used to leave ScoreHud.glej in the recycle bin.
    [StaFact]
    public void Rename_ShouldExitOneAndKeepTheElementFile_WhenItWouldOverwriteCodeWithoutOverwrite()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var beefball = Path.Combine(project.Root, "Beefball");
        var existing = Path.Combine(beefball, "Entities", "Scoreboard.cs");
        File.WriteAllText(existing, "// not an entity yet");
        var snapshot = SnapshotElementFiles(project);

        var (exitCode, output) = Run("rename", project.Root, "--element", "ScoreHud", "--to", "Scoreboard", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        var result = Parse(output);
        var failure = result.EditFailures.ShouldHaveSingleItem();
        failure.ShouldContain("would overwrite existing files:", Case.Sensitive);
        failure.ShouldContain("Entities/Scoreboard.cs", Case.Sensitive);
        failure.ShouldContain("Pass --overwrite", Case.Sensitive);
        result.DialogMessages.ShouldBeEmpty();
        ChangedSince(project, snapshot).ShouldBeEmpty();
        File.ReadAllText(existing).ShouldBe("// not an entity yet");
    }

    [StaFact]
    public void Rename_ShouldReplaceTheExistingCode_WithOverwrite()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var beefball = Path.Combine(project.Root, "Beefball");
        File.WriteAllText(Path.Combine(beefball, "Entities", "Scoreboard.cs"), "// not an entity yet");

        var (exitCode, output) = Run("rename", project.Root, "--element", "ScoreHud", "--to", "Scoreboard", "--overwrite");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        File.ReadAllText(Path.Combine(beefball, "Entities", "Scoreboard.cs")).ShouldContain("partial class Scoreboard", Case.Sensitive);
        File.Exists(Path.Combine(beefball, "Entities", "Scoreboard.glej")).ShouldBeTrue();
    }

    /// <summary>GameScreen references Content/Screens/GameScreen/Background.png, as adding the file in the editor leaves it.</summary>
    static void AddBackgroundFileToGameScreen(string beefball)
    {
        var folder = Path.Combine(beefball, "Content", "Screens", "GameScreen");
        Directory.CreateDirectory(folder);
        File.Copy(Path.Combine(RepoPaths.FrbRoot, "Samples", "BeefballWeb", "BeefballWeb", "wwwroot", "kni.png"),
            Path.Combine(folder, "Background.png"));

        var screenFile = Path.Combine(beefball, "Screens", "GameScreen.glsj");
        var text = File.ReadAllText(screenFile);
        const string anchor = "\"Source\": \"GLUE\",";
        text.ShouldContain(anchor);
        File.WriteAllText(screenFile, text.Replace(anchor, anchor +
            "\n  \"ReferencedFiles\": [ { \"Name\": \"Screens/GameScreen/Background.png\", " +
            "\"RuntimeType\": \"Microsoft.Xna.Framework.Graphics.Texture2D\", \"ProjectsToExcludeFrom\": [] } ],"));
    }

    static Dictionary<string, string> SnapshotElementFiles(TempDir project)
    {
        var beefball = Path.Combine(project.Root, "Beefball");
        return Directory.EnumerateFiles(beefball, "*", SearchOption.AllDirectories)
            // Not the .gluj or .csproj: loading the project rewrites those whether or not an edit is saved.
            .Where(file => Path.GetExtension(file) is ".glsj" or ".glej" ||
                (file.EndsWith(".cs") && !file.Contains(".Generated.")))
            .Where(file => !file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) &&
                !file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .ToDictionary(file => Path.GetRelativePath(beefball, file), File.ReadAllText);
    }

    /// <summary>The files in <paramref name="snapshot"/> that are now changed or gone, and any new ones.</summary>
    static List<string> ChangedSince(TempDir project, Dictionary<string, string> snapshot)
    {
        var now = SnapshotElementFiles(project);
        return snapshot.Keys.Union(now.Keys)
            .Where(file => !snapshot.TryGetValue(file, out var before) || !now.TryGetValue(file, out var after) || before != after)
            .ToList();
    }

    static IReadOnlyList<string?> EditList(CliResult result, string camelCaseName) =>
        EditObjects(result, camelCaseName).Select(item => item.GetString()).ToList();

    static IEnumerable<JsonElement> EditObjects(CliResult result, string camelCaseName) =>
        ((JsonElement)result.Edit!).GetProperty(camelCaseName).EnumerateArray().ToList();

    static JsonElement ReadJson(TempDir project, string folder, string file) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(project.Root, "Beefball", folder, file))).RootElement;

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
