using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Collections.Concurrent;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.SaveClasses;
using GlueCli;
using GlueCli.Commands;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;
using static GlueUnitTests.Cli.GlueCliTestRun;

namespace GlueUnitTests.Cli;

/// <summary>
/// <c>batch</c> against temp copies of Samples/Beefball: several commands on one load give what running them
/// one by one gives, a failure stops the batch unless --keep-going, and --dry-run covers the whole batch.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class BatchCommandTests
{
    const string ThreeEdits =
@"# Comments and blank lines are skipped.

add-object --element GameScreen --type Sprite --name S1
set-variable --element 'Screens\GameScreen' --object S1 --variable X --value ""120.5""
add-object --element Entities\PlayerBall --type Circle --name C1
";

    [StaFact]
    public void ThreeCommands_ShouldMatchRunningThemOneByOne()
    {
        using var batchCopy = GoldProject.CopyOutOfRepo("Samples/Beefball");
        using var singleCopy = GoldProject.CopyOutOfRepo("Samples/Beefball");

        var (exitCode, output) = RunWithStdin(ThreeEdits, "batch", batchCopy.Root, "--stdin", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var batch = ParseBatch(output);
        batch.Results.Select(result => result.Command).ShouldBe(new[] { "add-object", "set-variable", "add-object" });

        var singles = new[]
        {
            Run("add-object", singleCopy.Root, "--element", "GameScreen", "--type", "Sprite", "--name", "S1", "--json"),
            Run("set-variable", singleCopy.Root, "--element", "Screens\\GameScreen", "--object", "S1", "--variable", "X",
                "--value", "120.5", "--json"),
            Run("add-object", singleCopy.Root, "--element", "Entities\\PlayerBall", "--type", "Circle", "--name", "C1", "--json"),
        };
        var singleFilesWritten = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < singles.Length; i++)
        {
            singles[i].exitCode.ShouldBe(GlueCliRunner.ExitSuccess, singles[i].output);
            var single = Parse(singles[i].output);
            // Compact, since the batch nests each result one level deeper in its indented output.
            JsonSerializer.Serialize(batch.Results[i].Edit).ShouldBe(JsonSerializer.Serialize(single.Edit));
            batch.Results[i].FilesWritten.ShouldBeEmpty("a batch writes once, after the last command");
            singleFilesWritten.UnionWith(single.FilesWritten);
        }
        batch.FilesWritten.ShouldBe(singleFilesWritten.ToList(), ignoreOrder: true);
        batch.FilesWritten.ShouldContain("Screens/GameScreen.Generated.cs");
        // Element files, the .csproj and generated code.
        HashFolder(batchCopy.Root).ShouldBe(HashFolder(singleCopy.Root));
    }

    [StaFact]
    public void EditsToOneElement_ShouldSaveAndGenerateItOnce()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var generator = new CountingGenerator();
        CodeWriter.CodeGenerators.Add(generator);
        SaveCountingPlugin.EnsureRegistered();
        SaveCountingPlugin.Saved.Clear();
        try
        {
            var (exitCode, output) = RunWithStdin(
@"add-object --element GameScreen --type Sprite --name S1
set-variable --element GameScreen --object S1 --variable X --value 10
set-variable --element GameScreen --object S1 --variable Y --value 20
", "batch", project.Root, "--stdin", "--json");

            exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        }
        finally
        {
            CodeWriter.CodeGenerators.Remove(generator);
        }
        SaveCountingPlugin.Saved.Count(name => name == "Screens\\GameScreen").ShouldBe(1);
        generator.Generated.Count(name => name == "Screens\\GameScreen").ShouldBe(1);
    }

    /// <summary>Records each element file save (SaveElementAsync), registered once for the whole test run.</summary>
    sealed class SaveCountingPlugin : FlatRedBall.Glue.Plugins.PluginBase
    {
        static readonly object lockObject = new();
        static bool registered;
        public static readonly ConcurrentQueue<string> Saved = new();

        public static void EnsureRegistered()
        {
            lock (lockObject)
            {
                if (!registered)
                {
                    GlueTestBootstrap.EnsureInitialized();
                    registered = true;
                    FlatRedBall.Glue.Plugins.PluginManager.RegisterPlugin(new SaveCountingPlugin());
                }
            }
        }

        public override string FriendlyName => "Batch Save Counting Plugin (test)";
        public override Version Version => new(1, 0);

        public override void StartUp()
        {
            ReactToScreenJsonSave = (name, _) => Saved.Enqueue(name);
            ReactToEntityJsonSave = (name, _) => Saved.Enqueue(name);
        }

        public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason shutDownReason) => true;
    }

    sealed class CountingGenerator : ElementComponentCodeGenerator
    {
        public readonly ConcurrentQueue<string> Generated = new();

        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, IElement element)
        {
            Generated.Enqueue(element.Name);
            return codeBlock;
        }
    }

    [StaFact]
    public void FailingMiddleCommand_ShouldStopTheBatch_AndWriteNothing()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var before = HashFolder(project.Root);

        var (exitCode, output) = RunWithStdin(FailingMiddle, "batch", project.Root, "--stdin", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        var batch = ParseBatch(output);
        batch.CommandCount.ShouldBe(3);
        batch.Results.Count.ShouldBe(2);
        batch.Results[1].EditFailures.ShouldNotBeEmpty();
        batch.FilesWritten.ShouldBeEmpty();
        ShouldBeUnchanged(project.Root, before);
    }

    // A save or codegen between lines would leave line 1's or line 3's files on disk.
    [StaFact]
    public void FailingMiddleCommand_ShouldRunTheRest_AndWriteNothing_WithKeepGoing()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var before = HashFolder(project.Root);

        var (exitCode, output) = RunWithStdin(FailingMiddle, "batch", project.Root, "--stdin", "--keep-going");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        output.ShouldContain("Command(s) 2 of 3 failed, so nothing was written.");
        ShouldBeUnchanged(project.Root, before);
    }

    /// <summary>
    /// Every kind of edit, with the last line failing: no command may write anything on its own during a batch.
    /// </summary>
    [StaFact]
    public void FailingLastCommand_ShouldWriteNothing_AfterEveryKindOfEdit()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var png = Path.Combine(project.Root, "Beefball", "Content", "Screens", "GameScreen", "Background.png");
        Directory.CreateDirectory(Path.GetDirectoryName(png)!);
        File.WriteAllBytes(png, Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=="));
        var before = HashFolder(project.Root);

        var (exitCode, output) = RunWithStdin(
$@"add-entity --name Coin --circle
add-screen --name TitleScreen
add-object --element GameScreen --type Coin --name Coin1
set-variable --element GameScreen --object Wall1 --variable Width --value 512.5
add-variable --element PlayerBall --name JumpSpeed --type float --default 250.5
add-state --element PlayerBall --category DashCategory --name Exhausted
add-event --element GameScreen --custom --name MatchEnded
add-file --element GameScreen --file ""{png}""
add-collision-relationship --element GameScreen --first PlayerBallList --second GoalList
rename --element PlayerBall --object CooldownCircle --to DashCircle
rename --element ScoreHud --to Scoreboard
delete --element GameScreen --object Wall6
delete --element Puck
add-object --element GameScreen --type PlayerBal --name P1
", "batch", project.Root, "--stdin", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        var batch = ParseBatch(output);
        batch.Results.Count.ShouldBe(14);
        batch.Results.Take(13).ShouldAllBe(result => result.ExitCode == GlueCliRunner.ExitSuccess);
        ShouldBeUnchanged(project.Root, before);
    }

    [StaFact]
    public void DryRun_ShouldLeaveTheFolderByteIdentical_AndReportTheCombinedChanges()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var file = Path.Combine(project.Root, "edits.txt");
        File.WriteAllText(file, ThreeEdits);
        var before = HashFolder(project.Root);

        var (exitCode, output) = Run("batch", project.Root, "--file", file, "--dry-run", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        HashFolder(project.Root).ShouldBe(before, "a dry run must not write to the project");
        var batch = ParseBatch(output);
        batch.DryRun.ShouldBeTrue();
        batch.Results.Count.ShouldBe(3);
        var screen = batch.Changes!.Single(change => change.Path == "Screens/GameScreen.glsj");
        screen.Diff!.ShouldContain("\"S1\"", Case.Sensitive);
        screen.Diff.ShouldContain("120.5");
        batch.Changes!.ShouldContain(change => change.Path == "Entities/PlayerBall.glej");
        output.ShouldNotContain("GlueCliDryRun_", Case.Insensitive, "paths should name the real project, not the temp copy");
    }

    const string FailingMiddle =
@"add-object --element GameScreen --type Sprite --name S1
add-object --element GameScreen --type PlayerBal --name P1
add-object --element GameScreen --type Sprite --name S2
";

    static CliBatchResult ParseBatch(string output)
    {
        var json = output.Substring(0, output.IndexOf(Environment.NewLine + "stderr:", StringComparison.Ordinal));
        return JsonSerializer.Deserialize<CliBatchResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    static void ShouldBeUnchanged(string root, Dictionary<string, string> before)
    {
        var after = HashFolder(root);
        var changed = before.Keys.Union(after.Keys)
            .Where(file => !before.TryGetValue(file, out var hash) || !after.TryGetValue(file, out var newHash) || hash != newHash)
            .OrderBy(file => file)
            .ToList();
        changed.ShouldBeEmpty("a batch with a failed command must write nothing");
    }

    static Dictionary<string, string> HashFolder(string root) =>
        Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .ToDictionary(file => Path.GetRelativePath(root, file),
                file => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))));
}

/// <summary>Reading and checking a batch's lines, which happens before Glue starts.</summary>
public class BatchCommandParseTests
{
    [Theory]
    [InlineData("add-object --element GameScreen --type Sprite", new[] { "add-object", "--element", "GameScreen", "--type", "Sprite" })]
    [InlineData("  set-variable   --value \"a b\"  ", new[] { "set-variable", "--value", "a b" })]
    [InlineData("x --source-name 'Entire File (\"Map\")'", new[] { "x", "--source-name", "Entire File (\"Map\")" })]
    [InlineData("x --element Entities\\Enemy --name=\"A B\"", new[] { "x", "--element", "Entities\\Enemy", "--name=A B" })]
    [InlineData("x --value \"\"", new[] { "x", "--value", "" })]
    public void SplitArguments_ShouldHandleQuotes_AndKeepBackslashes(string line, string[] expected)
    {
        BatchCommand.SplitArguments(line, out var error).ShouldBe(expected);
        error.ShouldBeNull();
    }

    [Fact]
    public void ParseSteps_ShouldSkipBlankLinesAndComments()
    {
        var steps = BatchCommand.ParseSteps("# first\r\n\r\n  check\r\n# add-object\r\ndescribe --element GameScreen\r\n",
            "Game.csproj", out var error);

        error.ShouldBeNull();
        steps!.Select(step => step.Command.Name).ShouldBe(new[] { "check", "describe" });
    }

    [Theory]
    [InlineData("frobnicate", "Batch line 1: unknown command 'frobnicate'")]
    [InlineData("check\nadd-object --element \"GameScreen", "Batch line 2: the \" quote is not closed")]
    [InlineData("add-object --type Sprite", "Batch line 1: add-object needs --element")]
    [InlineData("add-object MyGame --element GameScreen --type Sprite", "takes no project in a batch")]
    [InlineData("check --json", "--json applies to the whole batch")]
    [InlineData("check --dry-run", "--dry-run applies to the whole batch")]
    [InlineData("batch --stdin", "a batch cannot run batch")]
    [InlineData("regen", "regen writes every generated file, so it cannot run in a batch")]
    [InlineData("check --frob", "Unknown option --frob")]
    [InlineData("# nothing\n\n", "The batch has no commands")]
    public void Run_ShouldExitTwo_WithoutStartingGlue_WhenALineIsBad(string text, string expectedMessage)
    {
        using var temp = new TempDir("GlueCliBatch_");
        File.WriteAllText(Path.Combine(temp.Root, "Game.gluj"), "{}");
        File.WriteAllText(Path.Combine(temp.Root, "Game.csproj"), "<Project />");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = GlueCliRunner.Run(new[] { "batch", temp.Root, "--stdin", "--json" }, stdout, stderr,
            new ThrowingHost(), stdin: new StringReader(text));

        exitCode.ShouldBe(GlueCliRunner.ExitUsageOrLoadFailure);
        stderr.ToString().ShouldContain(expectedMessage, Case.Sensitive);
        stdout.ToString().ShouldContain("\"command\": \"batch\"", Case.Sensitive);
    }

    [Theory]
    [InlineData("batch", "x")]
    [InlineData("batch", "x", "--stdin", "--file", "a.txt")]
    public void Run_ShouldExitTwo_WhenTheBatchHasNoSingleSource(params string[] args)
    {
        var stderr = new StringWriter();

        GlueCliRunner.Run(args, new StringWriter(), stderr, new ThrowingHost())
            .ShouldBe(GlueCliRunner.ExitUsageOrLoadFailure);

        stderr.ToString().ShouldContain("batch needs exactly one of --file or --stdin.");
    }

    sealed class ThrowingHost : IGlueCliHost
    {
        public IReadOnlyList<string> Prepare(string csproj, Action<string> reportMessage) =>
            throw new InvalidOperationException("Batch syntax errors must be reported before Glue starts.");
    }
}
