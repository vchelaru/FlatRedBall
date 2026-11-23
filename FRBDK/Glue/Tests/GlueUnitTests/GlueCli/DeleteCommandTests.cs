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
/// <c>GlueCli delete</c> against a temp copy of Samples/Beefball: one screen (GameScreen) holding lists of
/// the four entities, their instances, walls and collision relationships, and no files.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class DeleteCommandTests
{
    // A valid 1x1 png.
    static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    [StaFact]
    public void Delete_ShouldRemoveAnObjectFromTheScreenAndItsCode()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("delete", project.Root, "--element", "GameScreen", "--object", "Wall6", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "kind").ShouldBe("object");
        EditProperty(result, "name").ShouldBe("Wall6");
        EditProperty(result, "element").ShouldBe("Screens\\GameScreen");
        AllInstanceNames(ReadJson(project, "Screens", "GameScreen.glsj")).ShouldNotContain("Wall6");
        AllInstanceNames(ReadJson(project, "Screens", "GameScreen.glsj")).ShouldContain("Wall5");
        var generated = File.ReadAllText(Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.Generated.cs"));
        generated.ShouldNotContain("Wall6", Case.Sensitive);
        generated.ShouldContain("Wall5", Case.Sensitive);
    }

    [StaFact]
    public void Delete_ShouldRemoveAnEntityItsInstancesAndFactory_KeepingItsFilesOnDiskByDefault()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        // The factory's generated file only exists once code has been generated.
        Regen(project.Root);

        var (exitCode, output) = Run("delete", project.Root, "--element", "Puck", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "kind").ShouldBe("entity");
        EditProperty(result, "name").ShouldBe("Entities\\Puck");
        EditProperty(result, "fileAction").ShouldBe("remove-from-project");
        var edit = (JsonElement)result.Edit!;
        edit.GetProperty("alsoRemoved").EnumerateArray().Select(item => item.GetString()).ShouldContain(item => item!.Contains("Puck1"));

        var glsj = ReadJson(project, "Screens", "GameScreen.glsj");
        AllInstanceNames(glsj).ShouldNotContain("Puck1");
        File.ReadAllText(Path.Combine(project.Root, "Beefball", "Beefball.gluj")).ShouldNotContain("Entities\\\\Puck", Case.Sensitive);
        var generated = File.ReadAllText(Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.Generated.cs"));
        generated.ShouldNotContain("Entities.Puck", Case.Sensitive);

        var csproj = File.ReadAllText(Csproj(project));
        csproj.ShouldNotContain(@"Entities\Puck.cs", Case.Sensitive);
        csproj.ShouldNotContain("PuckFactory", Case.Sensitive);
        File.Exists(Path.Combine(project.Root, "Beefball", "Entities", "Puck.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(project.Root, "Beefball", "Entities", "Puck.glej")).ShouldBeTrue();

        var files = LeftOverFiles(result);
        files.ShouldContain(file => file.path == "Entities/Puck.cs" && file.onDisk && !file.inProject);
        files.ShouldContain(file => file.path == "Entities/Puck.Generated.cs" && file.onDisk && !file.inProject);
        // Glue's entity removal deletes the factory itself, whatever the dialog's file choice (as in the editor).
        files.ShouldContain(file => file.path == "Factories/PuckFactory.Generated.cs" && !file.onDisk && !file.inProject);
    }

    [StaFact]
    public void Delete_ShouldMoveAnEntitysFilesOffDisk_WhenFilesIsDelete()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("delete", project.Root, "--element", "Entities\\Puck", "--files", "delete", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        File.Exists(Path.Combine(project.Root, "Beefball", "Entities", "Puck.cs")).ShouldBeFalse();
        File.Exists(Path.Combine(project.Root, "Beefball", "Entities", "Puck.glej")).ShouldBeFalse();
        File.ReadAllText(Csproj(project)).ShouldNotContain(@"Entities\Puck.cs", Case.Sensitive);
        LeftOverFiles(Parse(output)).ShouldContain(file => file.path == "Entities/Puck.cs" && !file.onDisk && !file.inProject);
    }

    [StaFact]
    public void Delete_ShouldResetTheBaseOfADerivedEntity_UnlessKeepInheritanceIsPassed()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        AddDerivedEntity(project, "BigPuck", "Entities\\Puck");
        using var keptProject = GoldProject.CopyOutOfRepo("Samples/Beefball");
        AddDerivedEntity(keptProject, "BigPuck", "Entities\\Puck");

        var (exitCode, output) = Run("delete", project.Root, "--element", "Puck", "--json");
        var (_, keptOutput) = Run("delete", keptProject.Root, "--element", "Puck", "--keep-inheritance", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        Options(Parse(output)).ShouldBe(new[] { ("Reset the inheritance for Entities\\BigPuck", true) });
        BaseEntity(project, "BigPuck").ShouldBeNullOrEmpty();

        Options(Parse(keptOutput)).ShouldBe(new[] { ("Reset the inheritance for Entities\\BigPuck", false) });
        BaseEntity(keptProject, "BigPuck").ShouldBe("Entities\\Puck");
    }

    [StaFact]
    public void Delete_ShouldRemoveAScreen()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("delete", project.Root, "--element", "GameScreen", "--json");

        var result = Parse(output);
        result.EditFailures.ShouldBeEmpty(output);
        EditProperty(result, "kind").ShouldBe("screen");
        var gluj = File.ReadAllText(Path.Combine(project.Root, "Beefball", "Beefball.gluj"));
        gluj.ShouldNotContain("Screens\\\\GameScreen", Case.Sensitive);
        var csproj = File.ReadAllText(Csproj(project));
        csproj.ShouldNotContain(@"Screens\GameScreen.cs", Case.Sensitive);
        csproj.ShouldNotContain(@"Screens\GameScreen.Generated.cs", Case.Sensitive);
        File.Exists(Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.cs")).ShouldBeTrue();
        // Beefball has no other screen, so whatever Glue reports about a missing startup screen is accepted,
        // but nothing else.
        result.Errors.ShouldAllBe(error => error.Details.Contains("creen"), output);
        exitCode.ShouldBe(result.Errors.Count == 0 ? GlueCliRunner.ExitSuccess : GlueCliRunner.ExitErrors, output);
    }

    [StaFact]
    public void Delete_ShouldRemoveAFileAndTheObjectUsingIt()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        AddBackgroundWithObject(project);

        var (exitCode, output) = Run("delete", project.Root, "--element", "GameScreen", "--file", "Background", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "kind").ShouldBe("file");
        EditProperty(result, "name").ShouldBe("Screens/GameScreen/Background.png");
        Options(result).ShouldBe(new[] { ("Remove the object Texture2D BackgroundSprite in Screens\\GameScreen (Screen), which references this file", true) },
            customMessage: output);
        var glsj = ReadJson(project, "Screens", "GameScreen.glsj");
        (glsj.TryGetProperty("ReferencedFiles", out var files) ? files.GetArrayLength() : 0).ShouldBe(0);
        AllInstanceNames(glsj).ShouldNotContain("BackgroundSprite");
        File.ReadAllText(Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.Generated.cs")).ShouldNotContain("Background", Case.Sensitive);
        File.ReadAllText(Csproj(project)).ShouldNotContain(@"Background.png", Case.Sensitive);
        File.Exists(BackgroundPath(project)).ShouldBeTrue();
        LeftOverFiles(result).ShouldContain(file => file.path == "Content/Screens/GameScreen/Background.png" && file.onDisk && !file.inProject);
    }

    [StaFact]
    public void Delete_ShouldKeepTheObjectUsingAFile_WhenKeepObjectsIsPassed()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        AddBackgroundWithObject(project);

        var (_, output) = Run("delete", project.Root, "--element", "GameScreen", "--file", "Screens/GameScreen/Background.png",
            "--keep-objects", "--json");

        var result = Parse(output);
        result.EditFailures.ShouldBeEmpty(output);
        Options(result).ShouldHaveSingleItem().Item2.ShouldBeFalse();
        AllInstanceNames(ReadJson(project, "Screens", "GameScreen.glsj")).ShouldContain("BackgroundSprite");
    }

    [StaFact]
    public void Delete_ShouldRemoveAGlobalContentFile()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var csv = Path.Combine(ContentFolder(project), "GlobalContent", "EnemyInfo.csv");
        Directory.CreateDirectory(Path.GetDirectoryName(csv)!);
        File.WriteAllText(csv, "Name (string, required),Speed (float)\r\nSlow,1\r\n");
        Run("add-file", project.Root, "--global", "--file", csv).exitCode.ShouldBe(GlueCliRunner.ExitSuccess);

        var (exitCode, output) = Run("delete", project.Root, "--global", "--file", "GlobalContent/EnemyInfo.csv", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "element").ShouldBeNull();
        File.ReadAllText(Path.Combine(project.Root, "Beefball", "Beefball.gluj")).ShouldNotContain("EnemyInfo.csv", Case.Sensitive);
        File.ReadAllText(Path.Combine(project.Root, "Beefball", "GlobalContent.Generated.cs")).ShouldNotContain("EnemyInfo", Case.Sensitive);
        var csproj = File.ReadAllText(Csproj(project));
        csproj.ShouldNotContain("EnemyInfo", Case.Sensitive);
        LeftOverFiles(result).ShouldContain(file => file.path == "DataTypes/EnemyInfo.Generated.cs" && !file.inProject, output);
    }

    [StaFact]
    public void Delete_ShouldExitOneWithCloseMatches_WhenTheTargetDoesNotExist()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var glsjBefore = File.ReadAllText(Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.glsj"));

        var (elementExit, elementOutput) = Run("delete", project.Root, "--element", "Pukc", "--json");
        var (objectExit, objectOutput) = Run("delete", project.Root, "--element", "GameScreen", "--object", "Wal6", "--json");
        var (fileExit, fileOutput) = Run("delete", project.Root, "--global", "--file", "Missing.png", "--json");

        elementExit.ShouldBe(GlueCliRunner.ExitErrors, elementOutput);
        Parse(elementOutput).EditFailures.ShouldHaveSingleItem().ShouldContain("Entities\\Puck", Case.Sensitive);
        objectExit.ShouldBe(GlueCliRunner.ExitErrors, objectOutput);
        Parse(objectOutput).EditFailures.ShouldHaveSingleItem().ShouldContain("Wall6", Case.Sensitive);
        fileExit.ShouldBe(GlueCliRunner.ExitErrors, fileOutput);
        Parse(fileOutput).EditFailures.ShouldHaveSingleItem().ShouldContain("Global Content has no file 'Missing.png'", Case.Sensitive);
        File.ReadAllText(Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.glsj")).ShouldBe(glsjBefore);
    }

    static void AddBackgroundWithObject(TempDir project)
    {
        var png = BackgroundPath(project);
        Directory.CreateDirectory(Path.GetDirectoryName(png)!);
        File.WriteAllBytes(png, Png);
        Run("add-file", project.Root, "--element", "GameScreen", "--file", png).exitCode.ShouldBe(GlueCliRunner.ExitSuccess);
        var (exitCode, output) = Run("add-object", project.Root, "--element", "GameScreen", "--source-file", "Background.png",
            "--source-name", "Entire File (Texture2D)", "--name", "BackgroundSprite");
        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
    }

    /// <summary>Hand-writes an entity file deriving from <paramref name="baseEntity"/> and lists it in the .gluj.</summary>
    static void AddDerivedEntity(TempDir project, string name, string baseEntity)
    {
        var folder = Path.Combine(project.Root, "Beefball");
        File.WriteAllText(Path.Combine(folder, "Entities", name + ".glej"),
            JsonSerializer.Serialize(new { Name = "Entities\\" + name, BaseEntity = baseEntity }));
        File.WriteAllText(Path.Combine(folder, "Entities", name + ".cs"),
            $"namespace Beefball.Entities\r\n{{\r\n    public partial class {name}\r\n    {{\r\n    }}\r\n}}\r\n");
        var glujPath = Path.Combine(folder, "Beefball.gluj");
        var gluj = File.ReadAllText(glujPath);
        File.WriteAllText(glujPath, gluj.Replace("\"EntityReferences\": [", $"\"EntityReferences\": [\r\n    {{\r\n      \"Name\": \"Entities\\\\{name}\"\r\n    }},"));
    }

    static string? BaseEntity(TempDir project, string name)
    {
        var glej = ReadJson(project, "Entities", name + ".glej");
        return glej.TryGetProperty("BaseEntity", out var value) ? value.GetString() : null;
    }

    static (string, bool)[] Options(CliResult result) =>
        ((JsonElement)result.Edit!).GetProperty("options").EnumerateArray()
            .Select(option => (option.GetProperty("text").GetString()!, option.GetProperty("checked").GetBoolean()))
            .ToArray();

    static (string path, bool onDisk, bool inProject)[] LeftOverFiles(CliResult result) =>
        ((JsonElement)result.Edit!).GetProperty("files").EnumerateArray()
            .Select(file => (file.GetProperty("path").GetString()!, file.GetProperty("onDisk").GetBoolean(), file.GetProperty("inProject").GetBoolean()))
            .ToArray();

    static string[] AllInstanceNames(JsonElement element)
    {
        var names = new System.Collections.Generic.List<string>();
        void Walk(JsonElement objects)
        {
            foreach (var nos in objects.EnumerateArray())
            {
                names.Add(nos.GetProperty("InstanceName").GetString()!);
                if (nos.TryGetProperty("ContainedObjects", out var contained))
                {
                    Walk(contained);
                }
            }
        }
        if (element.TryGetProperty("NamedObjects", out var top))
        {
            Walk(top);
        }
        return names.ToArray();
    }

    static string BackgroundPath(TempDir project) => Path.Combine(ContentFolder(project), "Screens", "GameScreen", "Background.png");

    static string ContentFolder(TempDir project) => Path.Combine(project.Root, "Beefball", "Content");

    static string Csproj(TempDir project) => Path.Combine(project.Root, "Beefball", "Beefball.csproj");

    static JsonElement ReadJson(TempDir project, params string[] pathInProject) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(new[] { project.Root, "Beefball" }.Concat(pathInProject).ToArray()))).RootElement;
}
