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
/// <c>GlueCli set-variable</c> against a temp copy of Samples/Beefball, whose GameScreen has AxisAlignedRectangle
/// walls in a ShapeCollection and PlayerBall instances in a list, and whose PlayerBall entity defines MovementSpeed.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class SetVariableCommandTests
{
    [StaFact]
    public void SetVariable_ShouldStoreAFloat_OnAFlatRedBallObject()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("set-variable", project.Root, "--element", "GameScreen", "--object", "Wall1",
            "--variable", "Width", "--value", "512.5", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "object").ShouldBe("Wall1");
        EditProperty(result, "variable").ShouldBe("Width");
        EditProperty(result, "type").ShouldBe("float");
        EditProperty(result, "oldValue").ShouldBe("800");
        EditProperty(result, "newValue").ShouldBe("512.5");

        var width = Instruction(FindObject(ReadJson(project, "Screens", "GameScreen.glsj"), "Wall1"), "Width");
        width.ValueKind.ShouldBe(JsonValueKind.Number);
        width.GetDouble().ShouldBe(512.5);

        var generated = File.ReadAllText(Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.Generated.cs"));
        generated.ShouldContain("Wall1.Width = 512.5f;", Case.Sensitive);
    }

    [StaFact]
    public void SetVariable_ShouldSetAnEntityVariable_OnAnEntityInstance()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("set-variable", project.Root, "--element", "GameScreen", "--object", "PlayerBall1",
            "--variable", "MovementSpeed", "--value", "450", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        EditProperty(Parse(output), "oldValue").ShouldBeNull();

        var speed = Instruction(FindObject(ReadJson(project, "Screens", "GameScreen.glsj"), "PlayerBall1"), "MovementSpeed");
        speed.ValueKind.ShouldBe(JsonValueKind.Number);
        speed.GetDouble().ShouldBe(450);

        var generated = File.ReadAllText(Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.Generated.cs"));
        generated.ShouldContain("PlayerBall1.MovementSpeed = 450f;", Case.Sensitive);
    }

    [StaFact]
    public void SetVariable_ShouldSetTheElementDefault_WithoutAnObject()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("set-variable", project.Root, "--element", "PlayerBall",
            "--variable", "MovementSpeed", "--value", "350", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "element").ShouldBe("Entities\\PlayerBall");
        EditProperty(result, "object").ShouldBeNull();
        EditProperty(result, "oldValue").ShouldBe("300");
        EditProperty(result, "newValue").ShouldBe("350");

        var variable = ReadJson(project, "Entities", "PlayerBall.glej").GetProperty("CustomVariables").EnumerateArray()
            .Single(item => item.GetProperty("Name").GetString() == "MovementSpeed");
        variable.GetProperty("DefaultValue").ValueKind.ShouldBe(JsonValueKind.Number);
        variable.GetProperty("DefaultValue").GetDouble().ShouldBe(350);

        var generated = File.ReadAllText(Path.Combine(project.Root, "Beefball", "Entities", "PlayerBall.Generated.cs"));
        generated.ShouldContain("MovementSpeed = 350f;", Case.Sensitive);
    }

    [StaFact]
    public void SetVariable_ShouldExitOneAndSaveNothing_WhenTheValueDoesNotParse()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var screenFile = Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.glsj");
        var screenBefore = File.ReadAllText(screenFile);

        var (exitCode, output) = Run("set-variable", project.Root, "--element", "GameScreen", "--object", "Wall1",
            "--variable", "Width", "--value", "wide", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        var result = Parse(output);
        result.EditFailures.ShouldHaveSingleItem().ShouldBe("'wide' is not a valid float for Wall1.Width.");
        result.Edit.ShouldBeNull();
        File.ReadAllText(screenFile).ShouldBe(screenBefore);
    }

    [StaFact]
    public void SetVariable_ShouldExitOneAndSuggestCloseMatches_WhenTheVariableIsUnknown()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("set-variable", project.Root, "--element", "GameScreen", "--object", "Wall1",
            "--variable", "Wdth", "--value", "5");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        output.ShouldContain("has no variable 'Wdth'.", Case.Sensitive, output);
        output.ShouldContain("Did you mean: Width", Case.Sensitive, output);
    }

    [StaFact]
    public void SetVariable_ShouldExitOne_WhenTheEntityDoesNotExposeTheVariable()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("set-variable", project.Root, "--element", "GameScreen", "--object", "PlayerBall1",
            "--variable", "Radius", "--value", "5");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        output.ShouldContain("Entities\\PlayerBall, which has no variable 'Radius'", Case.Sensitive, output);
    }

    [StaFact]
    public void SetVariable_ShouldReportTheMoveObjectsQuestion_WhenSettingAScreensDefaultLayer()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        // DefaultLayer is only live from this .gluj version on; Beefball predates it.
        var gluj = Path.Combine(project.Root, "Beefball", "Beefball.gluj");
        File.WriteAllText(gluj, File.ReadAllText(gluj).Replace("\"FileVersion\": 42", "\"FileVersion\": 56"));
        Run("add-object", project.Root, "--element", "GameScreen", "--type", "Layer", "--name", "GameLayer").exitCode
            .ShouldBe(GlueCliRunner.ExitSuccess);

        // Every unlayered object in GameScreen is on the old (null) layer, so the screen plugin asks whether to
        // move them. Headless that question goes through the dialog seam instead of a WPF window, unanswered.
        var (exitCode, output) = Run("set-variable", project.Root, "--element", "GameScreen",
            "--variable", "DefaultLayer", "--value", "GameLayer", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        var result = Parse(output);
        result.DialogMessages.ShouldHaveSingleItem().ShouldContain("Would you like to move the following objects to the new layer GameLayer?");
        var screen = ReadJson(project, "Screens", "GameScreen.glsj");
        screen.GetProperty("CustomVariables").EnumerateArray()
            .Single(item => item.GetProperty("Name").GetString() == "DefaultLayer")
            .GetProperty("DefaultValue").GetString().ShouldBe("GameLayer");
        FindObject(screen, "PlayerBall1").TryGetProperty("LayerOn", out _).ShouldBeFalse();
    }

    [StaFact]
    public void SetVariable_ShouldRefuseAnObjectProperty()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("set-variable", project.Root, "--element", "GameScreen", "--object", "Wall1",
            "--variable", "AttachToCamera", "--value", "true");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        output.ShouldContain("'AttachToCamera' is a property of the object Wall1, not a variable.", Case.Sensitive, output);
    }

    static JsonElement ReadJson(TempDir project, string folder, string file) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(project.Root, "Beefball", folder, file))).RootElement;

    static JsonElement FindObject(JsonElement element, string instanceName) =>
        AllObjects(element.GetProperty("NamedObjects")).Single(nos => nos.GetProperty("InstanceName").GetString() == instanceName);

    static System.Collections.Generic.IEnumerable<JsonElement> AllObjects(JsonElement namedObjects)
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

    static JsonElement Instruction(JsonElement nos, string member) =>
        nos.GetProperty("InstructionSaves").EnumerateArray().Single(item => item.GetProperty("Member").GetString() == member)
            .GetProperty("Value");
}
