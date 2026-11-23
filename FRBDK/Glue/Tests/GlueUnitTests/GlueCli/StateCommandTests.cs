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
/// The state commands (add-state-category, add-state, set-state-variable, rename-state, delete-state) against a
/// temp copy of Samples/Beefball, whose PlayerBall entity has a DashCategory with Tired and Rested states that
/// set CooldownCircleRadius (0 and 16), and excludes every other variable.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class StateCommandTests
{
    [StaFact]
    public void AddStateCategory_ShouldIncludeTheNamedVariables_AndAddStateShouldAddToIt()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("add-state-category", project.Root, "--element", "PlayerBall", "--name", "Speed",
            "--include", "MovementSpeed,Drag", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        ((JsonElement)Parse(output).Edit!).GetProperty("includedVariables").EnumerateArray().Select(item => item.GetString())
            .ShouldBe(new[] { "MovementSpeed", "Drag" });
        var category = Category(project, "Speed");
        var excluded = category.GetProperty("ExcludedVariables").EnumerateArray().Select(item => item.GetString()).ToList();
        excluded.ShouldContain("X");
        excluded.ShouldNotContain("MovementSpeed");
        excluded.ShouldNotContain("Drag");
        var generated = Generated(project);
        generated.ShouldContain("public class Speed", Case.Sensitive);
        generated.ShouldContain("public Entities.PlayerBall.Speed CurrentSpeedState", Case.Sensitive);

        (exitCode, output) = Run("add-state", project.Root, "--element", "PlayerBall", "--category", "Speed", "--name", "Fast", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        EditProperty(Parse(output), "state").ShouldBe("Fast");
        Category(project, "Speed").GetProperty("States").EnumerateArray().Single().GetProperty("Name").GetString().ShouldBe("Fast");
        Generated(project).ShouldContain("public static Speed Fast = new Speed()", Case.Sensitive);
    }

    [StaFact]
    public void AddState_ShouldAddAStateToAnExistingCategory()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("add-state", project.Root, "--element", "PlayerBall", "--category", "DashCategory", "--name", "Exhausted");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        output.ShouldContain("Added state Exhausted to DashCategory in Entities\\PlayerBall.", Case.Sensitive);
        Category(project, "DashCategory").GetProperty("States").EnumerateArray().Select(item => item.GetProperty("Name").GetString())
            .ShouldBe(new[] { "Tired", "Rested", "Exhausted" });
        Generated(project).ShouldContain("{\"Exhausted\", Exhausted},", Case.Sensitive);
    }

    [StaFact]
    public void SetStateVariable_ShouldStoreTheConvertedValue_AndClearShouldRemoveIt()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("set-state-variable", project.Root, "--element", "PlayerBall", "--category", "DashCategory",
            "--state", "Tired", "--variable", "CooldownCircleRadius", "--value", "4.5", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "oldValue").ShouldBe("0");
        EditProperty(result, "newValue").ShouldBe("4.5");
        var value = Instruction(State(project, "DashCategory", "Tired"), "CooldownCircleRadius")!.Value;
        value.ValueKind.ShouldBe(JsonValueKind.Number);
        value.GetDouble().ShouldBe(4.5);
        Generated(project).ShouldContain("CooldownCircleRadius = 4.5f,", Case.Sensitive);

        (exitCode, output) = Run("set-state-variable", project.Root, "--element", "PlayerBall", "--category", "DashCategory",
            "--state", "Tired", "--variable", "CooldownCircleRadius", "--clear", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        result = Parse(output);
        EditProperty(result, "oldValue").ShouldBe("4.5");
        EditProperty(result, "newValue").ShouldBeNull();
        Instruction(State(project, "DashCategory", "Tired"), "CooldownCircleRadius").ShouldBeNull();
        // A state that does not set an included variable generates the variable's default (16).
        Generated(project).ShouldNotContain("CooldownCircleRadius = 4.5f,", Case.Sensitive);
    }

    [StaFact]
    public void SetStateVariable_ShouldIncludeAnExcludedVariable_OnlyWithInclude()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var entityFile = EntityFile(project);
        var entityBefore = File.ReadAllBytes(entityFile);

        var (exitCode, output) = Run("set-state-variable", project.Root, "--element", "PlayerBall", "--category", "DashCategory",
            "--state", "Tired", "--variable", "MovementSpeed", "--value", "100");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        output.ShouldContain("DashCategory in Entities\\PlayerBall excludes MovementSpeed", Case.Sensitive);
        output.ShouldContain("--include", Case.Sensitive);
        File.ReadAllBytes(entityFile).ShouldBe(entityBefore);

        (exitCode, output) = Run("set-state-variable", project.Root, "--element", "PlayerBall", "--category", "DashCategory",
            "--state", "Tired", "--variable", "MovementSpeed", "--value", "100", "--include", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        EditProperty(Parse(output), "includedVariable").ShouldBe("True");
        Category(project, "DashCategory").GetProperty("ExcludedVariables").EnumerateArray().Select(item => item.GetString())
            .ShouldNotContain("MovementSpeed");
        Generated(project).ShouldContain("MovementSpeed = 100f,", Case.Sensitive);
    }

    [StaFact]
    public void RenameState_ShouldRenameTheStateInTheGeneratedCode()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("rename-state", project.Root, "--element", "PlayerBall", "--category", "DashCategory",
            "--state", "Tired", "--to", "Exhausted");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        State(project, "DashCategory", "Exhausted").ValueKind.ShouldBe(JsonValueKind.Object);
        var generated = Generated(project);
        generated.ShouldContain("public static DashCategory Exhausted = new DashCategory()", Case.Sensitive);
        generated.ShouldNotContain("public static DashCategory Tired", Case.Sensitive);
    }

    [StaFact]
    public void DeleteState_ShouldRemoveAState_AndWholeCategoryShouldRemoveTheCategory()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("delete-state", project.Root, "--element", "PlayerBall", "--category", "DashCategory", "--state", "Rested");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        Category(project, "DashCategory").GetProperty("States").EnumerateArray().Select(item => item.GetProperty("Name").GetString())
            .ShouldBe(new[] { "Tired" });
        Generated(project).ShouldNotContain("public static DashCategory Rested", Case.Sensitive);

        (exitCode, output) = Run("delete-state", project.Root, "--element", "PlayerBall", "--category", "DashCategory", "--whole-category");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var entity = ReadEntity(project);
        (entity.TryGetProperty("StateCategoryList", out var categories) && categories.ValueKind == JsonValueKind.Array &&
            categories.GetArrayLength() > 0).ShouldBeFalse();
        Generated(project).ShouldNotContain("class DashCategory", Case.Sensitive);
    }

    [StaFact]
    public void StateCommands_ShouldExitOneAndSaveNothing_OnDuplicateUnknownOrUnparseableInput()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var entityFile = EntityFile(project);
        var entityBefore = File.ReadAllBytes(entityFile);

        void ShouldFail(string expected, params string[] args)
        {
            var (exitCode, output) = Run(new[] { args[0], project.Root }.Concat(args.Skip(1)).Concat(new[] { "--json" }).ToArray());
            exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
            var result = Parse(output);
            result.EditFailures.ShouldHaveSingleItem().ShouldContain(expected, Case.Sensitive);
            result.FilesWritten.ShouldNotContain("Entities/PlayerBall.glej");
            File.ReadAllBytes(entityFile).ShouldBe(entityBefore);
        }

        ShouldFail("already has a category with that name",
            "add-state-category", "--element", "PlayerBall", "--name", "DashCategory");
        ShouldFail("has no variable 'MovementSped'. Did you mean: MovementSpeed?",
            "add-state-category", "--element", "PlayerBall", "--name", "Speed", "--include", "MovementSped");
        ShouldFail("Did you mean: DashCategory?",
            "add-state", "--element", "PlayerBall", "--category", "DashCategry", "--name", "Exhausted");
        ShouldFail("Tired",
            "add-state", "--element", "PlayerBall", "--category", "DashCategory", "--name", "Tired");
        ShouldFail("has no state 'Tird'. Did you mean: Tired?",
            "set-state-variable", "--element", "PlayerBall", "--category", "DashCategory", "--state", "Tird",
            "--variable", "CooldownCircleRadius", "--value", "1");
        ShouldFail("'big' is not a valid float",
            "set-state-variable", "--element", "PlayerBall", "--category", "DashCategory", "--state", "Tired",
            "--variable", "CooldownCircleRadius", "--value", "big");
        ShouldFail("Rested",
            "rename-state", "--element", "PlayerBall", "--category", "DashCategory", "--state", "Tired", "--to", "Rested");
        ShouldFail("has no state 'Sleepy'.",
            "delete-state", "--element", "PlayerBall", "--category", "DashCategory", "--state", "Sleepy");
    }

    static string EntityFile(TempDir project) => Path.Combine(project.Root, "Beefball", "Entities", "PlayerBall.glej");

    static JsonElement ReadEntity(TempDir project) => JsonDocument.Parse(File.ReadAllText(EntityFile(project))).RootElement;

    static string Generated(TempDir project) =>
        File.ReadAllText(Path.Combine(project.Root, "Beefball", "Entities", "PlayerBall.Generated.cs"));

    static JsonElement Category(TempDir project, string name) =>
        ReadEntity(project).GetProperty("StateCategoryList").EnumerateArray().Single(item => item.GetProperty("Name").GetString() == name);

    static JsonElement State(TempDir project, string category, string name) =>
        Category(project, category).GetProperty("States").EnumerateArray().Single(item => item.GetProperty("Name").GetString() == name);

    static JsonElement? Instruction(JsonElement state, string member)
    {
        if (!state.TryGetProperty("InstructionSaves", out var instructions) || instructions.ValueKind != JsonValueKind.Array)
        {
            return null;
        }
        foreach (var instruction in instructions.EnumerateArray())
        {
            if (instruction.GetProperty("Member").GetString() == member)
            {
                return instruction.GetProperty("Value");
            }
        }
        return null;
    }
}
