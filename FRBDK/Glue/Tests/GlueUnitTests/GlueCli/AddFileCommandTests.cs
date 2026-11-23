using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using GlueCli;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;
using static GlueUnitTests.Cli.GlueCliTestRun;

namespace GlueUnitTests.Cli;

/// <summary>
/// <c>GlueCli add-file</c> against a temp copy of Samples/Beefball, which references no files, so each test
/// writes the files it adds into the copy.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class AddFileCommandTests
{
    // A valid 1x1 png.
    static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    const string Csv = "Name (string, required),Speed (float)\r\nSlow,1\r\nFast,5\r\n";

    [StaFact]
    public void AddFile_ShouldAddAPngToAScreenAndGenerateItsCode()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var png = WriteContentFile(project, "Screens/GameScreen/Background.png", Png);

        var (exitCode, output) = Run("add-file", project.Root, "--element", "GameScreen", "--file", png, "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "element").ShouldBe("Screens\\GameScreen");
        EditProperty(result, "name").ShouldBe("Screens/GameScreen/Background.png");
        EditProperty(result, "runtimeType").ShouldBe("Microsoft.Xna.Framework.Graphics.Texture2D");
        EditProperty(result, "copiedFrom").ShouldBeNull();
        result.FilesWritten.ShouldContain("Screens/GameScreen.glsj");

        ReferencedFileNames(ReadJson(project, "Screens", "GameScreen.glsj")).ShouldBe(new[] { "Screens/GameScreen/Background.png" });
        var generated = File.ReadAllText(Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.Generated.cs"));
        generated.ShouldContain("Microsoft.Xna.Framework.Graphics.Texture2D Background", Case.Sensitive);
        File.ReadAllText(Csproj(project)).ShouldContain(@"Content\Screens\GameScreen\Background.png", Case.Sensitive);
    }

    [StaFact]
    public void AddFile_ShouldAddACsvToGlobalContentAndGenerateItsDataClass()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var csv = WriteContentFile(project, "GlobalContent/EnemyInfo.csv", Csv);

        var (exitCode, output) = Run("add-file", project.Root, "--global", "--file", csv, "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "element").ShouldBeNull();
        EditProperty(result, "name").ShouldBe("GlobalContent/EnemyInfo.csv");
        EditProperty(result, "csvRowType").ShouldBe("Beefball.DataTypes.EnemyInfo");

        ReferencedFileNames(ReadJson(project, "Beefball.gluj"), "GlobalFiles").ShouldBe(new[] { "GlobalContent/EnemyInfo.csv" });
        var globalContent = File.ReadAllText(Path.Combine(project.Root, "Beefball", "GlobalContent.Generated.cs"));
        globalContent.ShouldContain("EnemyInfo", Case.Sensitive);
        var dataClass = File.ReadAllText(Path.Combine(project.Root, "Beefball", "DataTypes", "EnemyInfo.Generated.cs"));
        dataClass.ShouldContain("public float Speed;", Case.Sensitive);
    }

    [StaFact]
    public void AddFile_ShouldExitOneAndSaveNothing_WhenTheFileIsOutsideTheContentFolderWithoutCopy()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var outside = Path.Combine(project.Root, "Art", "Background.png");
        Directory.CreateDirectory(Path.GetDirectoryName(outside)!);
        File.WriteAllBytes(outside, Png);
        var screenBefore = File.ReadAllText(ScreenFile(project));

        var (exitCode, output) = Run("add-file", project.Root, "--element", "GameScreen", "--file", outside, "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        var result = Parse(output);
        result.EditFailures.ShouldHaveSingleItem().ShouldContain("--copy", Case.Sensitive);
        // The load itself generates code (samples ship without .Generated.cs), so only the element file is checked.
        result.FilesWritten.ShouldNotContain("Screens/GameScreen.glsj");
        File.ReadAllText(ScreenFile(project)).ShouldBe(screenBefore);
        File.Exists(Path.Combine(ContentFolder(project), "Screens", "GameScreen", "Background.png")).ShouldBeFalse();
    }

    [StaFact]
    public void AddFile_ShouldCopyTheFileIntoTheElementsContentFolder_WhenCopyIsPassed()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var outside = Path.Combine(project.Root, "Art", "Background.png");
        Directory.CreateDirectory(Path.GetDirectoryName(outside)!);
        File.WriteAllBytes(outside, Png);

        var (exitCode, output) = Run("add-file", project.Root, "--element", "GameScreen", "--file", outside, "--copy", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "name").ShouldBe("Screens/GameScreen/Background.png");
        EditProperty(result, "copiedFrom").ShouldBe(Path.GetFullPath(outside).Replace('\\', '/'), StringCompareShould.IgnoreCase);
        File.ReadAllBytes(Path.Combine(ContentFolder(project), "Screens", "GameScreen", "Background.png")).ShouldBe(Png);
        ReferencedFileNames(ReadJson(project, "Screens", "GameScreen.glsj")).ShouldBe(new[] { "Screens/GameScreen/Background.png" });
    }

    [StaFact]
    public void AddFile_ShouldExitOneAndSaveNothing_WhenTheFileIsAlreadyReferenced()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var png = WriteContentFile(project, "Screens/GameScreen/Background.png", Png);
        Run("add-file", project.Root, "--element", "GameScreen", "--file", png).exitCode.ShouldBe(GlueCliRunner.ExitSuccess);
        var screenBefore = File.ReadAllText(ScreenFile(project));
        var glujBefore = File.ReadAllText(Path.Combine(project.Root, "Beefball", "Beefball.gluj"));

        var (exitCode, output) = Run("add-file", project.Root, "--element", "GameScreen", "--file", png, "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        var result = Parse(output);
        result.EditFailures.ShouldHaveSingleItem().ShouldContain("already references Screens/GameScreen/Background.png", Case.Sensitive);
        result.Edit.ShouldBeNull();
        File.ReadAllText(ScreenFile(project)).ShouldBe(screenBefore);
        File.ReadAllText(Path.Combine(project.Root, "Beefball", "Beefball.gluj")).ShouldBe(glujBefore);
    }

    [StaFact]
    public void AddFile_ShouldExitOne_WhenAnotherFileInTheElementHasTheSameName()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var png = WriteContentFile(project, "Screens/GameScreen/Background.png", Png);
        var csv = WriteContentFile(project, "Screens/GameScreen/Background.csv", Csv);
        Run("add-file", project.Root, "--element", "GameScreen", "--file", png).exitCode.ShouldBe(GlueCliRunner.ExitSuccess);
        var screenBefore = File.ReadAllText(ScreenFile(project));

        var (exitCode, output) = Run("add-file", project.Root, "--element", "GameScreen", "--file", csv, "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        Parse(output).EditFailures.ShouldHaveSingleItem().ShouldContain("Background.csv cannot be added", Case.Sensitive);
        File.ReadAllText(ScreenFile(project)).ShouldBe(screenBefore);
    }

    [StaFact]
    public void AddFile_ShouldTreatAShortNamePathInTheContentFolderAsInIt()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var png = WriteContentFile(project, "Screens/GameScreen/Background.png", Png);
        var shortPath = new StringBuilder(1024);
        if (GetShortPathName(png, shortPath, shortPath.Capacity) == 0 ||
            string.Equals(shortPath.ToString(), png, StringComparison.OrdinalIgnoreCase))
        {
            // This volume has no 8.3 names, so there is no short form to pass.
            return;
        }

        var (exitCode, output) = Run("add-file", project.Root, "--element", "GameScreen", "--file", shortPath.ToString(), "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        EditProperty(Parse(output), "name").ShouldBe("Screens/GameScreen/Background.png");
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern int GetShortPathName(string longPath, StringBuilder shortPath, int bufferLength);

    static string WriteContentFile(TempDir project, string relativePath, object contents)
    {
        var path = Path.Combine(ContentFolder(project), relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (contents is byte[] bytes)
        {
            File.WriteAllBytes(path, bytes);
        }
        else
        {
            File.WriteAllText(path, (string)contents);
        }
        return path;
    }

    static string ContentFolder(TempDir project) => Path.Combine(project.Root, "Beefball", "Content");

    static string ScreenFile(TempDir project) => Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.glsj");

    static string Csproj(TempDir project) => Path.Combine(project.Root, "Beefball", "Beefball.csproj");

    static JsonElement ReadJson(TempDir project, params string[] pathInProject) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(new[] { project.Root, "Beefball" }.Concat(pathInProject).ToArray()))).RootElement;

    static string?[] ReferencedFileNames(JsonElement owner, string property = "ReferencedFiles") =>
        owner.TryGetProperty(property, out var files)
            ? files.EnumerateArray().Select(file => file.GetProperty("Name").GetString()).ToArray()
            : Array.Empty<string?>();
}
