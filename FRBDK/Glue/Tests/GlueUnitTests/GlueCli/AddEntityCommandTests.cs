using System;
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
/// <c>GlueCli add-entity</c> against a temp copy of Samples/Beefball, whose one screen is
/// GameScreen and whose entities are Goal, PlayerBall, Puck and ScoreHud.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class AddEntityCommandTests : IDisposable
{
    public AddEntityCommandTests()
    {
        // Process-wide singletons: a view model left IsPlatformer = true by an earlier test turns the next
        // "IsPlatformer = true" into a no-op.
        FlatRedBall.PlatformerPlugin.Controllers.MainController.Self.ResetViewModel();
        TopDownPlugin.Controllers.MainController.Self.ResetViewModel();
    }

    public void Dispose()
    {
        FlatRedBall.PlatformerPlugin.Controllers.MainController.Self.ResetViewModel();
        TopDownPlugin.Controllers.MainController.Self.ResetViewModel();
    }

    [StaFact]
    public void AddEntity_ShouldSaveTheEntityAndGenerateItsCode()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var gameScreenObjectsBefore = ScreenObjectNames(project);

        var (exitCode, output) = Run("add-entity", project.Root, "--name", "Coin", "--no-factory", "--no-list", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "name").ShouldBe("Entities\\Coin");
        EditProperty(result, "createdByOtherEntities").ShouldBe("False");
        EditProperty(result, "implementsICollidable").ShouldBe("False");
        result.FilesWritten.ShouldContain("Entities/Coin.glej");
        result.FilesWritten.ShouldContain("Entities/Coin.cs");
        result.FilesWritten.ShouldContain("Entities/Coin.Generated.cs");

        File.ReadAllText(Gluj(project)).ShouldContain("\"Entities\\\\Coin\"", Case.Sensitive);
        var entity = ReadJson(project, "Entities", "Coin.glej");
        entity.TryGetProperty("CreatedByOtherEntities", out _).ShouldBeFalse();
        File.ReadAllText(Path.Combine(project.Root, "Beefball", "Entities", "Coin.Generated.cs"))
            .ShouldContain("public partial class Coin", Case.Sensitive);
        var csproj = File.ReadAllText(Path.Combine(project.Root, "Beefball", "Beefball.csproj"));
        csproj.ShouldContain("Entities\\Coin.cs", Case.Sensitive);
        csproj.ShouldContain("Entities\\Coin.Generated.cs", Case.Sensitive);
        csproj.ShouldNotContain("CoinFactory");
        ScreenObjectNames(project).ShouldBe(gameScreenObjectsBefore);
    }

    [StaFact]
    public void AddEntity_ShouldAddTheFactoryAndTheGameScreenList_WhenCollidable()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("add-entity", project.Root, "--name", "Enemy", "--circle", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "implementsICollidable").ShouldBe("True");
        EditProperty(result, "createdByOtherEntities").ShouldBe("True");
        EditProperty(result, "objects").ShouldContain("CircleInstance");
        EditProperty(result, "addedToScreens").ShouldContain("Screens\\\\GameScreen.EnemyList");

        var entity = ReadJson(project, "Entities", "Enemy.glej");
        PropertyValue(entity, "ImplementsICollidable").ShouldBe("True");
        entity.GetProperty("CreatedByOtherEntities").GetBoolean().ShouldBeTrue();
        File.ReadAllText(Path.Combine(project.Root, "Beefball", "Entities", "Enemy.Generated.cs"))
            .ShouldContain("FlatRedBall.Math.Geometry.ICollidable", Case.Sensitive);
        File.ReadAllText(Path.Combine(project.Root, "Beefball", "Factories", "EnemyFactory.Generated.cs"))
            .ShouldContain("class EnemyFactory", Case.Sensitive);

        var enemyList = ReadJson(project, "Screens", "GameScreen.glsj").GetProperty("NamedObjects").EnumerateArray()
            .Single(nos => nos.GetProperty("InstanceName").GetString() == "EnemyList");
        enemyList.GetProperty("SourceClassGenericType").GetString().ShouldBe("Entities\\Enemy");
        File.ReadAllText(Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.Generated.cs"))
            .ShouldContain("EnemyList", Case.Sensitive);
    }

    [StaFact]
    public void AddEntity_ShouldPutTheEntityInTheFolderAndInheritTheBase()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("add-entity", project.Root, "--name", "BigPuck", "--folder", "Pucks", "--base", "Puck", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "name").ShouldBe("Entities\\Pucks\\BigPuck");
        EditProperty(result, "baseEntity").ShouldBe("Entities\\Puck");

        ReadJson(project, "Entities", "Pucks", "BigPuck.glej").GetProperty("BaseEntity").GetString().ShouldBe("Entities\\Puck");
        var generated = File.ReadAllText(Path.Combine(project.Root, "Beefball", "Entities", "Pucks", "BigPuck.Generated.cs"));
        generated.ShouldContain("namespace Beefball.Entities.Pucks", Case.Sensitive);
        generated.ShouldContain("Beefball.Entities.Puck", Case.Sensitive);
    }

    [StaFact]
    public void AddEntity_ShouldMakeThePlatformer_WhenPlatformerIsPassed()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("add-entity", project.Root, "--name", "Player", "--rectangle", "--platformer", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        EditProperty(Parse(output), "movement").ShouldBe("Platformer");
        var entity = ReadJson(project, "Entities", "Player.glej");
        PropertyValue(entity, "ImplementsICollidable").ShouldBe("True");
        PropertyValue(entity, "IsPlatformer").ShouldBe("True");
        entity.GetProperty("CustomVariables").EnumerateArray().Select(variable => variable.GetProperty("Name").GetString())
            .ShouldContain("GroundMovement");
        File.ReadAllText(Path.Combine(project.Root, "Beefball", "Entities", "Player.Generated.cs"))
            .ShouldContain("IPlatformer", Case.Sensitive);
    }

    [StaFact]
    public void AddEntity_ShouldMakeItTopDown_WhenTopDownIsPassed()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("add-entity", project.Root, "--name", "Hero", "--top-down", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        EditProperty(Parse(output), "movement").ShouldBe("TopDown");
        var entity = ReadJson(project, "Entities", "Hero.glej");
        PropertyValue(entity, "IsTopDown").ShouldBe("True");
        PropertyValue(entity, "ImplementsICollidable").ShouldBe("True");
    }

    [StaFact]
    public void AddEntity_ShouldExitOneAndSaveNothing_WhenTheNameIsTaken()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var glujBefore = File.ReadAllText(Gluj(project));

        var (exitCode, output) = Run("add-entity", project.Root, "--name", "Puck", "--folder", "Other", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        var result = Parse(output);
        result.EditFailures.ShouldHaveSingleItem().ShouldContain("There is already an entity named Puck", Case.Sensitive);
        // Loading regenerates code and adds it to the .csproj; the failed edit must save no project data.
        result.FilesWritten.ShouldNotContain(file => file.EndsWith(".gluj") || file.EndsWith(".glej") || file.EndsWith(".glsj"));
        File.ReadAllText(Gluj(project)).ShouldBe(glujBefore);
        File.ReadAllText(Path.Combine(project.Root, "Beefball", "Beefball.csproj")).ShouldNotContain("Other\\Puck");
    }

    [StaFact]
    public void AddEntity_ShouldExitOneAndSuggestCloseMatches_WhenTheBaseIsUnknown()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("add-entity", project.Root, "--name", "BigPuck", "--base", "Puk");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        output.ShouldContain("No entity named 'Puk'. Did you mean: Entities\\Puck", Case.Sensitive, output);
    }

    static string Gluj(TempDir project) => Path.Combine(project.Root, "Beefball", "Beefball.gluj");

    static string[] ScreenObjectNames(TempDir project) =>
        ReadJson(project, "Screens", "GameScreen.glsj").GetProperty("NamedObjects").EnumerateArray()
            .Select(nos => nos.GetProperty("InstanceName").GetString()!).ToArray();

    /// <summary>An element's entry in its Properties list (ImplementsICollidable, IsPlatformer), as text.</summary>
    static string? PropertyValue(JsonElement element, string name) =>
        element.GetProperty("Properties").EnumerateArray()
            .FirstOrDefault(property => property.GetProperty("Name").GetString() == name) is { ValueKind: JsonValueKind.Object } found
            ? found.GetProperty("Value").ToString() : null;

    static JsonElement ReadJson(TempDir project, params string[] pathUnderGameFolder) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(new[] { project.Root, "Beefball" }.Concat(pathUnderGameFolder).ToArray()))).RootElement;
}
