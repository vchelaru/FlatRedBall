using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using GlueCli;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;
using static GlueUnitTests.Cli.GlueCliTestRun;

namespace GlueUnitTests.Cli;

/// <summary>
/// <c>--dry-run</c> on edit commands against a temp copy of Samples/Beefball: the edit runs in full, the
/// project folder is left byte-identical, and the run reports what it would have changed.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class DryRunTests
{
    [StaFact]
    public void AddObject_DryRun_ShouldReportTheChanges_AndLeaveEveryFileAsItWas()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var before = HashFolder(project.Root);

        var (exitCode, output) = Run("add-object", project.Root, "--element", "GameScreen", "--type", "Sprite",
            "--dry-run", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        HashFolder(project.Root).ShouldBe(before, "a dry run must not write to the project");

        var result = Parse(output);
        result.DryRun.ShouldBeTrue();
        result.FilesWritten.ShouldBeEmpty();
        EditProperty(result, "name").ShouldBe("SpriteInstance");

        var screen = result.Changes!.Single(change => change.Path == "Screens/GameScreen.glsj");
        screen.Kind.ShouldBe("modified");
        screen.Diff.ShouldNotBeNull();
        screen.Diff.ShouldContain("+++ b/Screens/GameScreen.glsj", Case.Sensitive);
        screen.Diff.Split('\n').ShouldContain(line => line.StartsWith("+") && line.Contains("\"SpriteInstance\""));

        // The sample is checked in without generated code, so the copy may not have it yet.
        var generated = result.Changes!.Single(change => change.Path == "Screens/GameScreen.Generated.cs");
        generated.Kind.ShouldBeOneOf("modified", "added");
        generated.Diff.ShouldBeNull();

        output.ShouldNotContain("GlueCliDryRun_", Case.Insensitive, "paths should name the real project, not the temp copy");
    }

    [StaFact]
    public void RenameEntity_DryRun_ShouldReportTheRenamedFiles_AndLeaveThemInPlace()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var before = HashFolder(project.Root);

        var (exitCode, output) = Run("rename", project.Root, "--element", "Entities\\ScoreHud", "--to", "Scoreboard",
            "--dry-run", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        HashFolder(project.Root).ShouldBe(before, "a dry run must not write to the project");
        File.Exists(Path.Combine(project.Root, "Beefball", "Entities", "ScoreHud.glej")).ShouldBeTrue();

        var changes = Parse(output).Changes!;
        changes.ShouldContain(change => change.Path == "Entities/ScoreHud.glej" && change.Kind == "deleted");
        changes.ShouldContain(change => change.Path == "Entities/Scoreboard.glej" && change.Kind == "added");
    }

    [StaFact]
    public void FailingEdit_DryRun_ShouldExitOne()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var before = HashFolder(project.Root);

        var (exitCode, output) = Run("add-object", project.Root, "--element", "GameScreen", "--type", "PlayerBal",
            "--dry-run", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        var result = Parse(output);
        result.DryRun.ShouldBeTrue();
        result.EditFailures.ShouldNotBeEmpty();
        HashFolder(project.Root).ShouldBe(before);
    }

    static Dictionary<string, string> HashFolder(string root) =>
        Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .ToDictionary(file => Path.GetRelativePath(root, file),
                file => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))));
}
