using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Shouldly;

namespace GlueUnitTests.TestSupport;

// Guards the helper every Category=BuildSmoke test shells out through. See NestedDotnetCli's own doc
// comment and GitHub issue #1969 for what went wrong without it: a nested `dotnet build` left MSBuild
// worker nodes holding the inherited stdout pipe open, so a ReadToEnd() that should have returned in
// seconds blocked for the nodes' 15 minute idle timeout instead - which read as "the test suite hangs".
public class NestedDotnetCliTests
{
    [Fact]
    public void Run_ShouldCaptureOutputAndReturnTheChildsExitCode_WhenTheCommandFails()
    {
        // The dotnet CLI reports an unknown command on stderr, so an empty result here means only one
        // stream is being drained - the shape that deadlocks once a real build fills the other one.
        var (exitCode, output) = NestedDotnetCli.Run("--definitely-not-a-real-dotnet-command", TimeSpan.FromMinutes(1));

        exitCode.ShouldNotBe(0);
        exitCode.ShouldNotBe(NestedDotnetCli.TimedOutExitCode, "This should have failed fast, not timed out.");
        output.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Run_ShouldStopWaitingOnTheStartupGate_WhenTheProcessExitsWithoutOpeningIt()
    {
        // A command that fails before it reaches whatever the gate is watching for has to return at its own
        // pace. If the gate held the run for its whole budget instead, every such failure would cost the
        // budget - and the budget is sized for the worst cold start, not for the common case.
        var neverOpens = new NestedDotnetCli.StartupGate(() => false, TimeSpan.FromMinutes(10));

        var stopwatch = Stopwatch.StartNew();
        var (exitCode, _) = NestedDotnetCli.Run(
            "--definitely-not-a-real-dotnet-command", TimeSpan.FromMinutes(1), neverOpens);
        stopwatch.Stop();

        exitCode.ShouldNotBe(0);
        exitCode.ShouldNotBe(NestedDotnetCli.TimedOutExitCode, "This should have failed fast, not timed out.");
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromMinutes(1),
            "The gate outlived the process it was gating.");
    }

    [Fact]
    public void EveryNestedDotnetInvocation_ShouldGoThroughNestedDotnetCli()
    {
        // The bug was copy-pasted into seven files before anyone noticed, and running the tests cannot
        // catch it - the deadlocking version passes whenever the MSBuild nodes happen to already be warm.
        // So this is checked against the source instead.
        var offenders = Directory
            .EnumerateFiles(FindTestProjectRoot(), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(file => !Path.GetFileName(file).StartsWith("NestedDotnetCli", StringComparison.Ordinal))
            // LiveGameProcess.cs redirects the built game .exe's stdout - not a nested `dotnet` invocation,
            // so NestedDotnetCli.Run (which expects a `dotnet <args>` command it runs to completion or
            // times out) doesn't fit. It doesn't have the #1969 deadlock shape either: the process is
            // long-lived by design (killed by Dispose, not awaited to exit), and output is drained via the
            // async OutputDataReceived/BeginOutputReadLine event pair rather than a blocking ReadToEnd().
            .Where(file => Path.GetFileName(file) != "LiveGameProcess.cs")
            .Where(file => File.ReadAllText(file).Contains("RedirectStandardOutput", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToList();

        offenders.ShouldBeEmpty(
            "These test files start a redirected child process directly instead of calling " +
            $"{nameof(NestedDotnetCli)}.{nameof(NestedDotnetCli.Run)}, which is how the nested-build hang " +
            "in issue #1969 reached seven files at once:" + Environment.NewLine +
            string.Join(Environment.NewLine, offenders));
    }

    // Spawns a real nested MSBuild, which is what the BuildSmoke tag means; kept out of the fast suite for
    // the same reason as the rest, since it has to sit through its own timeout to prove anything.
    [Trait("Category", "BuildSmoke")]
    [Fact]
    public void Run_ShouldTimeOutAndKillTheWholeProcessTree_WhenTheCommandOutlivesItsTimeout()
    {
        // Killing the dotnet CLI process alone would leave MSBuild and its Exec grandchild running - the
        // orphan accumulation this helper exists to prevent - so the grandchild is what gets asserted on.
        using var project = new TempProjectThatRunsForever();

        // The startup is deliberately not part of the timeout. Cold, a runner can spend tens of seconds in
        // the CLI, MSBuild evaluation and PowerShell before the grandchild exists, and a timeout that fired
        // inside that window would kill an empty tree and then report the miss as if the kill had leaked one.
        // So the gate waits the startup out untimed, and the timeout only has to cover the kill (issue #1992).
        var startupBudget = TimeSpan.FromMinutes(3);
        var timeout = TimeSpan.FromSeconds(10);
        var untilGrandchildStarts = new NestedDotnetCli.StartupGate(
            () => project.GrandchildProcessId != null, startupBudget);

        var stopwatch = Stopwatch.StartNew();
        var (exitCode, output) = NestedDotnetCli.Run($"build \"{project.CsprojPath}\"", timeout, untilGrandchildStarts);
        stopwatch.Stop();

        var grandchild = project.GrandchildProcessId;
        grandchild.ShouldNotBeNull(
            $"The grandchild never started within {startupBudget.TotalMinutes:0.#} minutes, which means the " +
            "nested build is broken rather than merely slow. Output:" + Environment.NewLine + output);

        exitCode.ShouldBe(NestedDotnetCli.TimedOutExitCode,
            "Expected a timeout, got:" + Environment.NewLine + output);
        stopwatch.Elapsed.ShouldBeLessThan(startupBudget + timeout + TimeSpan.FromMinutes(1),
            "The timeout did not actually bound the wait.");
        IsAlive(grandchild.Value).ShouldBeFalse(
            "The grandchild outlived the kill, so the child tree is being orphaned rather than reaped.");
    }

    // The test above only proves the gate deferred the clock on a runner slow enough to need it, so it would
    // pass on a fast machine with the gate removed entirely. This one holds the gate shut for longer than the
    // whole timeout, which no undeferred clock can survive.
    [Trait("Category", "BuildSmoke")]
    [Fact]
    public void Run_ShouldNotStartTheTimeoutClock_UntilTheStartupGateOpens()
    {
        using var project = new TempProjectThatRunsForever();

        var gateHold = TimeSpan.FromSeconds(8);
        var timeout = TimeSpan.FromSeconds(3);
        var held = Stopwatch.StartNew();
        var opensLate = new NestedDotnetCli.StartupGate(
            () => held.Elapsed >= gateHold, gateHold + TimeSpan.FromMinutes(1));

        var stopwatch = Stopwatch.StartNew();
        var (exitCode, output) = NestedDotnetCli.Run($"build \"{project.CsprojPath}\"", timeout, opensLate);
        stopwatch.Stop();

        exitCode.ShouldBe(NestedDotnetCli.TimedOutExitCode,
            "Expected a timeout, got:" + Environment.NewLine + output);
        stopwatch.Elapsed.ShouldBeGreaterThan(gateHold,
            "The timeout fired while the gate was still shut, so its clock started at process start rather " +
            "than at the gate - which is what made this test flaky on a cold runner (issue #1992).");
    }

    private static bool IsAlive(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            // No process with that id - it is gone, which is the outcome being checked for.
            return false;
        }
    }

    private static string FindTestProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "GlueUnitTests.csproj")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate GlueUnitTests.csproj above " + AppContext.BaseDirectory);
    }

    private sealed class TempProjectThatRunsForever : IDisposable
    {
        private readonly string _root;
        private readonly string _pidFile;

        public string CsprojPath { get; }

        public TempProjectThatRunsForever()
        {
            _root = Path.Combine(Path.GetTempPath(), "FrbNestedCliTimeout_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);

            _pidFile = Path.Combine(_root, "grandchild.pid");
            CsprojPath = Path.Combine(_root, "RunsForever.csproj");

            // No Sdk attribute and an explicit Build target: nothing to restore and no compiler to run, so
            // the only thing this project can spend time on is the Exec. The grandchild records its own pid
            // before sleeping, which is how the test identifies it without guessing from a process name.
            File.WriteAllText(CsprojPath, $"""
                <Project>
                  <Target Name="Build">
                    <Exec Command="powershell -NoProfile -NonInteractive -Command &quot;Set-Content -LiteralPath '{_pidFile}' -Value $PID; Start-Sleep -Seconds 600&quot;" />
                  </Target>
                </Project>
                """);
        }

        /// <summary>
        /// The pid the Exec grandchild wrote before going to sleep, or null if it never got that far.
        /// </summary>
        public int? GrandchildProcessId
        {
            get
            {
                try
                {
                    return int.TryParse(File.ReadAllText(_pidFile).Trim(), out var pid) ? pid : null;
                }
                catch (IOException)
                {
                    // Missing, or caught mid-write by the polling gate below - both mean "not yet", not "never".
                    return null;
                }
                catch (UnauthorizedAccessException)
                {
                    return null;
                }
            }
        }

        public void Dispose()
        {
            // Belt and braces: if the kill under test failed, don't leave the sleeper running for 10 minutes.
            var pid = GrandchildProcessId;
            if (pid != null)
            {
                try
                {
                    using var process = Process.GetProcessById(pid.Value);
                    process.Kill(entireProcessTree: true);
                }
                catch { /* already gone, which is the expected case */ }
            }

            try { Directory.Delete(_root, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }
}
