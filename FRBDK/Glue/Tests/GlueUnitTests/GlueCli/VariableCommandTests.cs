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
/// <c>GlueCli add-variable</c>, <c>set-variable-property</c> and <c>remove-variable</c> against a temp copy of
/// Samples/Beefball. PlayerBall defines MovementSpeed, Drag (exposes PositionedObject.Drag) and
/// CooldownCircleRadius (tunnels CooldownCircle.Radius), and holds a CircleInstance whose Radius nothing tunnels.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class VariableCommandTests
{
    [StaFact]
    public void AddVariable_ShouldAddAFloatWithItsDefault_AndGenerateIt()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("add-variable", project.Root, "--element", "PlayerBall", "--name", "JumpSpeed",
            "--type", "float", "--default", "250.5", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "kind").ShouldBe("new");
        EditProperty(result, "type").ShouldBe("float");
        EditProperty(result, "setByDerived").ShouldBe("True");

        var variable = FindVariable(ReadEntity(project), "JumpSpeed");
        variable.GetProperty("DefaultValue").GetDouble().ShouldBe(250.5);
        variable.GetProperty("SetByDerived").GetBoolean().ShouldBeTrue();
        StoredProperty(variable, "Type").GetString().ShouldBe("float");

        var generated = ReadGenerated(project);
        generated.ShouldContain("private float mJumpSpeed = 250.5f;", Case.Sensitive);
        generated.ShouldContain("public virtual float JumpSpeed", Case.Sensitive);
    }

    [StaFact]
    public void AddVariable_ShouldTunnelIntoAnObjectsVariable()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("add-variable", project.Root, "--element", "PlayerBall", "--tunnel", "CircleInstance.Radius",
            "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "name").ShouldBe("CircleInstanceRadius");
        EditProperty(result, "kind").ShouldBe("tunneled");

        var variable = FindVariable(ReadEntity(project), "CircleInstanceRadius");
        variable.GetProperty("SourceObject").GetString().ShouldBe("CircleInstance");
        variable.GetProperty("SourceObjectProperty").GetString().ShouldBe("Radius");

        var generated = ReadGenerated(project);
        generated.ShouldContain("public virtual float CircleInstanceRadius", Case.Sensitive);
        generated.ShouldContain("CircleInstance.Radius = value;", Case.Sensitive);
    }

    [StaFact]
    public void AddVariable_ShouldExitOneAndSaveNothing_WhenTheNameIsTaken()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var before = File.ReadAllText(EntityFile(project));

        var (exitCode, output) = Run("add-variable", project.Root, "--element", "PlayerBall", "--name", "MovementSpeed",
            "--type", "float", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        Parse(output).EditFailures.ShouldHaveSingleItem().ShouldContain("already has a variable named MovementSpeed", Case.Sensitive);
        File.ReadAllText(EntityFile(project)).ShouldBe(before);
    }

    [StaFact]
    public void AddVariable_ShouldExitOneWithCloseMatches_WhenTheTypeIsUnknown()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var before = File.ReadAllText(EntityFile(project));

        var (exitCode, output) = Run("add-variable", project.Root, "--element", "PlayerBall", "--name", "JumpSpeed",
            "--type", "flot", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        Parse(output).EditFailures.ShouldHaveSingleItem().ShouldBe("'flot' is not a type the Add Variable window offers. Did you mean: float?");
        File.ReadAllText(EntityFile(project)).ShouldBe(before);
    }

    [StaFact]
    public void SetVariableProperty_ShouldTurnOffSetByDerived_AndRegenerate()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("set-variable-property", project.Root, "--element", "PlayerBall", "--variable", "MovementSpeed",
            "--property", "SetByDerived", "--value", "false", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "oldValue").ShouldBe("True");
        EditProperty(result, "newValue").ShouldBe("False");

        // The element files omit default values, so false leaves no key.
        FindVariable(ReadEntity(project), "MovementSpeed").TryGetProperty("SetByDerived", out _).ShouldBeFalse();
        var generated = ReadGenerated(project);
        generated.ShouldContain("public float MovementSpeed", Case.Sensitive);
        generated.ShouldNotContain("public virtual float MovementSpeed", Case.Sensitive);
    }

    [StaFact]
    public void SetVariableProperty_ShouldRefuseType_OnAVariableThatExposesABuiltInOne()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("set-variable-property", project.Root, "--element", "PlayerBall", "--variable", "Drag",
            "--property", "Type", "--value", "int");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        output.ShouldContain("Type of Entities\\PlayerBall.Drag is read-only in the editor. Only a new variable's type changes; Drag exposes a built-in variable.",
            Case.Sensitive, output);
    }

    [StaFact]
    public void RemoveVariable_ShouldRemoveIt_AndItsGeneratedProperty()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("remove-variable", project.Root, "--element", "PlayerBall", "--variable", "CooldownCircleRadius",
            "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        EditProperty(Parse(output), "variable").ShouldBe("CooldownCircleRadius");
        ReadEntity(project).GetProperty("CustomVariables").EnumerateArray()
            .Any(item => item.GetProperty("Name").GetString() == "CooldownCircleRadius").ShouldBeFalse();
        ReadGenerated(project).ShouldNotContain("CooldownCircleRadius", Case.Sensitive);
    }

    [StaFact]
    public void RemoveVariable_ShouldExitOneWithCloseMatches_WhenTheVariableIsUnknown()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var before = File.ReadAllText(EntityFile(project));

        var (exitCode, output) = Run("remove-variable", project.Root, "--element", "PlayerBall", "--variable", "MovementSped");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        output.ShouldContain("Entities\\PlayerBall has no variable 'MovementSped'. Did you mean: MovementSpeed?", Case.Sensitive, output);
        File.ReadAllText(EntityFile(project)).ShouldBe(before);
    }

    static string EntityFile(TempDir project) => Path.Combine(project.Root, "Beefball", "Entities", "PlayerBall.glej");

    static JsonElement ReadEntity(TempDir project) => JsonDocument.Parse(File.ReadAllText(EntityFile(project))).RootElement;

    static string ReadGenerated(TempDir project) =>
        File.ReadAllText(Path.Combine(project.Root, "Beefball", "Entities", "PlayerBall.Generated.cs"));

    static JsonElement FindVariable(JsonElement entity, string name) =>
        entity.GetProperty("CustomVariables").EnumerateArray().Single(item => item.GetProperty("Name").GetString() == name);

    /// <summary>A value kept in the variable's Properties list (Type, Category).</summary>
    static JsonElement StoredProperty(JsonElement owner, string name) =>
        owner.GetProperty("Properties").EnumerateArray().Single(item => item.GetProperty("Name").GetString() == name).GetProperty("Value");
}
