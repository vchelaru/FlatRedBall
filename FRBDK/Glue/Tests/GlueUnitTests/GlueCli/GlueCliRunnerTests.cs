using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using GlueCli;
using GlueUnitTests.TestSupport;
using static GlueUnitTests.Cli.GlueCliTestRun;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Cli;

/// <summary>
/// GlueCli's check and regen, run in-process (<see cref="GlueCliTestRun"/>) against a temp copy of
/// Samples/Beefball.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class GlueCliRunnerTests
{
    [StaFact]
    public void Check_ShouldExitZero_WhenTheProjectIsClean()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = Run("check", project.Root);

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        output.ShouldContain("No errors.");
    }

    [StaFact]
    public void Check_ShouldNotRefreshTheErrorList()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var originalAction = RefreshCommands.RefreshErrorsAction;
        var refreshCount = 0;
        // The editor's load fills the Errors tab through this; check computes the errors it reports itself.
        RefreshCommands.RefreshErrorsAction = () => refreshCount++;
        try
        {
            var (exitCode, output) = Run("check", project.Root);

            exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        }
        finally
        {
            RefreshCommands.RefreshErrorsAction = originalAction;
        }

        refreshCount.ShouldBe(0);
    }

    [StaFact]
    public void Check_ShouldExitOneAndNameTheFile_WhenTheGlujReferencesAMissingFile()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var gluj = Path.Combine(project.Root, "Beefball", "Beefball.gluj");
        var text = File.ReadAllText(gluj);
        text.ShouldContain("\"GlobalFiles\": []");
        File.WriteAllText(gluj, text.Replace("\"GlobalFiles\": []",
            "\"GlobalFiles\": [ { \"Name\": \"GlobalContent/DoesNotExist.png\" } ]"));

        var (exitCode, output) = Run("check", gluj, "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        var result = Parse(output);
        result.ExitCode.ShouldBe(GlueCliRunner.ExitErrors);
        result.Errors.ShouldContain(error => error.Details.Contains("DoesNotExist.png"), output);
    }

    [StaFact]
    public void Check_ShouldLeaveEveryFileByteIdentical_WhenGeneratedCodeIsStale()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        Regen(project.Root);
        // Opening the project in the editor would regenerate this file; check must not.
        var generated = Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.Generated.cs");
        File.AppendAllText(generated, "// stale" + Environment.NewLine);
        var before = ReadAllFiles(project.Root);

        var (exitCode, output) = Run("check", project.Root, "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        Parse(output).FilesWritten.ShouldBeEmpty(output);
        var after = ReadAllFiles(project.Root);
        after.Keys.ShouldBe(before.Keys, ignoreOrder: true);
        foreach (var (file, bytes) in before)
        {
            after[file].ShouldBe(bytes, file);
        }
    }

    // Plugins write their project-level code (Gum runtimes, Tiled classes, camera setup) in regen, not on load.
    [StaTheory]
    [InlineData("Beefball")]
    [InlineData("FormsSampleProject")]
    public void Check_ShouldWriteNothing_WhenTheProjectWasNeverGenerated(string sample)
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/" + sample);
        GoldProject.DeleteGeneratedCode(project.Root);
        var before = ReadAllFiles(project.Root);

        var (exitCode, output) = Run("check", project.Root, "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        Parse(output).FilesWritten.ShouldBeEmpty(output);
        var after = ReadAllFiles(project.Root);
        after.Keys.ShouldBe(before.Keys, ignoreOrder: true);
        foreach (var (file, bytes) in before)
        {
            after[file].ShouldBe(bytes, file);
        }
    }

    // Without atlases, Glue adds the images a Gum project uses to the .csproj. That is a .csproj fix-up, so the
    // model load leaves it to regen (and the editor's open).
    [StaFact]
    public void Check_ShouldLeaveTheCsprojAlone_WhenAGumImageIsMissingFromIt()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/FormsSampleProject");
        var csproj = RemoveGumSpriteSheetFromCsproj(project.Root);
        var before = File.ReadAllBytes(csproj);

        var (_, output) = Run("check", project.Root, "--json");

        Parse(output).FilesWritten.ShouldNotContain(file => file.EndsWith(".csproj"), output);
        File.ReadAllBytes(csproj).ShouldBe(before);
    }

    [StaFact]
    public void Regen_ShouldAddAGumImageMissingFromTheCsproj()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/FormsSampleProject");
        var csproj = RemoveGumSpriteSheetFromCsproj(project.Root);

        Regen(project.Root);

        File.ReadAllText(csproj).ShouldContain(GumSpriteSheetItem);
    }

    const string GumSpriteSheetItem = @"Include=""Content\GumProject\UISpriteSheet.png""";

    static string RemoveGumSpriteSheetFromCsproj(string projectRoot)
    {
        var csproj = Path.Combine(projectRoot, "FormsSampleProject", "FormsSampleProject.csproj");
        var text = File.ReadAllText(csproj);
        var start = text.IndexOf("<None " + GumSpriteSheetItem, StringComparison.Ordinal);
        start.ShouldBeGreaterThan(0);
        var end = text.IndexOf("</None>", start, StringComparison.Ordinal) + "</None>".Length;
        File.WriteAllText(csproj, text.Remove(start, end - start));
        File.ReadAllText(csproj).ShouldNotContain(GumSpriteSheetItem);
        return csproj;
    }

    static Dictionary<string, byte[]> ReadAllFiles(string root)
    {
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file);
            if (!relative.Split(Path.DirectorySeparatorChar).Any(segment => segment is "bin" or "obj" or ".vs"))
            {
                files[relative] = File.ReadAllBytes(file);
            }
        }
        return files;
    }

    [StaFact]
    public void Regen_ShouldWriteGeneratedCode()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        // Glue does not rewrite a generated file whose content is unchanged, so leftovers would make
        // "regen wrote it" and "it was already there" look the same.
        GoldProject.DeleteGeneratedCode(project.Root);

        var (exitCode, output) = Run("regen", project.Root, "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        result.FilesWritten.ShouldContain("Screens/GameScreen.Generated.cs");
        result.FilesWritten.ShouldContain("Entities/PlayerBall.Generated.cs");
        File.Exists(Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.Generated.cs")).ShouldBeTrue();
    }

    [StaTheory]
    [InlineData("Entities/Coin.glej", "add-entity", "--name", "Coin", "--no-factory", "--no-list")]
    [InlineData("Entities/PlayerBall.glej", "set-variable", "--element", "PlayerBall", "--variable", "MovementSpeed", "--value", "350")]
    public void Edit_ShouldSaveOnlyTheElementsItChanged(string changedElementFile, string command, params string[] options)
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        // Every element file is saved in a form Glue no longer writes (here compact instead of indented, in
        // the report a default value Glue now leaves out), so rewriting an untouched element changes its text.
        var gameFolder = Path.Combine(project.Root, "Beefball");
        foreach (var elementFile in Directory.EnumerateFiles(gameFolder, "*.gl?j", SearchOption.AllDirectories)
            .Where(file => !file.EndsWith(".gluj")))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(elementFile));
            File.WriteAllText(elementFile, JsonSerializer.Serialize(document.RootElement));
        }

        var (exitCode, output) = Run(new[] { command, project.Root }.Concat(options).Append("--json").ToArray());

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        Parse(output).FilesWritten.Where(file => file.EndsWith(".glej") || file.EndsWith(".glsj"))
            .ShouldBe(new[] { changedElementFile }, output);
    }
}

/// <summary>
/// Argument handling that fails before Glue starts, so these need no project load.
/// </summary>
public class GlueCliRunnerArgumentTests
{
    [Theory]
    [InlineData("frobnicate", "x")]
    [InlineData("check")]
    [InlineData("check", "a", "b")]
    [InlineData("check", "x", "--overwrite")]
    [InlineData("add-object", "x", "--type", "Sprite")]
    [InlineData("add-object", "x", "--element", "GameScreen")]
    [InlineData("add-object", "x", "--element")]
    [InlineData("add-object", "x", "--element", "GameScreen", "--type", "Sprite", "--list", "L", "--no-list")]
    [InlineData("add-object", "x", "--element", "GameScreen", "--type", "Sprite", "--source-file", "a.png", "--source-name", "n")]
    [InlineData("add-object", "x", "--element", "GameScreen", "--type", "Sprite", "--as-list=yes")]
    public void Run_ShouldExitTwo_WithoutStartingGlue_WhenTheArgumentsAreWrong(params string[] args)
    {
        var stderr = new StringWriter();

        var exitCode = GlueCliRunner.Run(args, new StringWriter(), stderr, new ThrowingHost());

        exitCode.ShouldBe(GlueCliRunner.ExitUsageOrLoadFailure);
        stderr.ToString().ShouldContain("Usage:");
    }

    [Fact]
    public void Help_ShouldListEveryCommand()
    {
        var stdout = new StringWriter();

        GlueCliRunner.Run(new[] { "--help" }, stdout, new StringWriter(), new ThrowingHost()).ShouldBe(GlueCliRunner.ExitSuccess);

        foreach (var command in GlueCliRunner.Commands)
        {
            stdout.ToString().ShouldContain("  " + command.Name + " ");
        }
    }

    [Theory]
    [InlineData("add-object", "--help")]
    [InlineData("help", "add-object")]
    public void CommandHelp_ShouldShowOptionsAndExamples(params string[] args)
    {
        var stdout = new StringWriter();

        GlueCliRunner.Run(args, stdout, new StringWriter(), new ThrowingHost()).ShouldBe(GlueCliRunner.ExitSuccess);

        var help = stdout.ToString();
        help.ShouldContain("--element <element>");
        help.ShouldContain("Examples:");
        help.ShouldContain("GlueCli add-object MyGame --element GameScreen --type Sprite");
    }

    [Fact]
    public void Run_ShouldExitTwo_WhenThePathDoesNotExist()
    {
        using var temp = new TempDir("GlueCliArgs_");
        var stdout = new StringWriter();

        var exitCode = GlueCliRunner.Run(new[] { "check", Path.Combine(temp.Root, "Missing.gluj"), "--json" },
            stdout, new StringWriter(), new ThrowingHost());

        exitCode.ShouldBe(GlueCliRunner.ExitUsageOrLoadFailure);
        stdout.ToString().ShouldContain("does not exist");
    }

    [Fact]
    public void ResolveCsproj_ShouldRefuse_WhenAFolderHasTwoGlujFiles()
    {
        using var temp = new TempDir("GlueCliArgs_");
        Directory.CreateDirectory(Path.Combine(temp.Root, "A"));
        Directory.CreateDirectory(Path.Combine(temp.Root, "B"));
        File.WriteAllText(Path.Combine(temp.Root, "A", "A.gluj"), "{}");
        File.WriteAllText(Path.Combine(temp.Root, "B", "B.gluj"), "{}");

        GlueCliRunner.ResolveCsproj(temp.Root, out var error).ShouldBeNull();

        error.ShouldNotBeNull();
        error.ShouldContain("2 .gluj files");
    }

    [Fact]
    public void ResolveCsproj_ShouldPickTheSameNamedCsproj_ForAGluj()
    {
        using var temp = new TempDir("GlueCliArgs_");
        var gluj = Path.Combine(temp.Root, "Game.gluj");
        File.WriteAllText(gluj, "{}");
        File.WriteAllText(Path.Combine(temp.Root, "Game.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(temp.Root, "Other.csproj"), "<Project />");

        GlueCliRunner.ResolveCsproj(temp.Root, out _).ShouldBe(Path.Combine(temp.Root, "Game.csproj"));
    }

    sealed class ThrowingHost : IGlueCliHost
    {
        public IReadOnlyList<string> Prepare(string csproj, Action<string> reportMessage) =>
            throw new InvalidOperationException("Argument errors must be reported before Glue starts.");
    }
}
