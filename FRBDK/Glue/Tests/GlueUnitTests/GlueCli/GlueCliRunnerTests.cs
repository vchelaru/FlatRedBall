using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using GlueCli;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Cli;

/// <summary>
/// GlueCli's commands, run in-process through <see cref="GlueCliRunner.Run"/> against a temp copy of
/// Samples/Beefball. The test host registers plugins through <see cref="GlueTestBootstrap"/> rather than
/// GlueCli's MEF scan of the Plugins folder, since that scan would load every plugin into the test process
/// for the rest of the run; the real exe is what exercises the scan.
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

    static (int exitCode, string output) Run(params string[] args)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exitCode = GlueCliRunner.Run(args, stdout, stderr, new TestHost());
        return (exitCode, stdout + Environment.NewLine + "stderr:" + Environment.NewLine + stderr);
    }

    static CliResult Parse(string output)
    {
        var json = output.Substring(0, output.IndexOf(Environment.NewLine + "stderr:", StringComparison.Ordinal));
        return JsonSerializer.Deserialize<CliResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    sealed class TestHost : IGlueCliHost
    {
        public IReadOnlyList<string> Prepare(string gameProjectDirectory, Action<string> reportMessage)
        {
            GlueTestBootstrap.EnsureGameProjectPluginsRegistered();
            GlueTestBootstrap.EnsureErrorReportingPluginRegisteredWithPluginManager();
            GlueTestBootstrap.EnsureMsBuildEnvironmentVariable();
            return Array.Empty<string>();
        }
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
    public void Run_ShouldExitTwo_WithoutStartingGlue_WhenTheArgumentsAreWrong(params string[] args)
    {
        var stderr = new StringWriter();

        var exitCode = GlueCliRunner.Run(args, new StringWriter(), stderr, new ThrowingHost());

        exitCode.ShouldBe(GlueCliRunner.ExitUsageOrLoadFailure);
        stderr.ToString().ShouldContain("Usage:");
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
        public IReadOnlyList<string> Prepare(string gameProjectDirectory, Action<string> reportMessage) =>
            throw new InvalidOperationException("Argument errors must be reported before Glue starts.");
    }
}
