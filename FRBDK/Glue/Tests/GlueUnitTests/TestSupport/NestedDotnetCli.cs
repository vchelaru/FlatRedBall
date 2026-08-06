using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace GlueUnitTests.TestSupport;

/// <summary>
/// Runs a nested <c>dotnet build</c>/<c>dotnet run</c> for the <c>Category=BuildSmoke</c> tests.
///
/// Every BuildSmoke test shells out to a real dotnet CLI invocation, and the obvious way to write that -
/// redirect both streams, <c>StandardOutput.ReadToEnd()</c>, then <c>WaitForExit()</c> - hangs for tens of
/// minutes rather than failing. Three separate reasons, all of which this helper exists to avoid:
///
///  - <b>MSBuild node reuse.</b> A plain <c>dotnet build</c> starts worker nodes with <c>/nodeReuse:true</c>
///    and they outlive it by design (15 minute idle timeout). Because <see cref="Process"/> creates the
///    redirect pipes as inheritable handles, those surviving nodes hold the write end of stdout/stderr open
///    after the build itself has exited - so <c>ReadToEnd()</c>, which returns on EOF and not on process
///    exit, blocks until the nodes idle out. This is why the symptom was intermittent: only the run that
///    *starts* the nodes inherits the handles, so the same test hangs once and then passes on an immediate
///    retry against the now-warm nodes. It is also the orphan-accumulation mechanism - a single successful
///    BuildSmoke pass left 20 stray <c>dotnet ... /nodemode:1</c> processes behind.
///  - <b>Sequential stream reads.</b> Draining stdout to EOF before reading stderr at all deadlocks the
///    moment the child writes more than a pipe buffer (~4 KB) of stderr: the child blocks writing, so it
///    never exits, so stdout never reaches EOF. Both streams have to be pumped concurrently.
///  - <b>No timeout.</b> An unbounded <c>WaitForExit()</c> turns any of the above into a wedged test run
///    that has to be killed from outside, leaving the whole child tree orphaned.
///
/// See GitHub issue #1969.
/// </summary>
internal static class NestedDotnetCli
{
    /// <summary>
    /// Generous relative to a warm build (the whole BuildSmoke suite runs in ~50s once the engine and the
    /// NuGet cache are warm) but well short of the 15 minute node idle timeout, so a regression of the
    /// hang above surfaces as a failing test with captured output rather than as a wedged run.
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    // Only reached after the process has already exited, so the readers are draining what is left in the
    // pipes rather than waiting on the child.
    private static readonly TimeSpan StreamFlushTimeout = TimeSpan.FromSeconds(15);

    public static (int exitCode, string output) Run(string arguments, TimeSpan? timeout = null)
    {
        var startInfo = new ProcessStartInfo("dotnet", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            // Closed immediately after start: a child that decides to prompt (a NuGet credential provider,
            // say) gets EOF and fails, instead of blocking forever on a console nobody is attached to.
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        // Environment variables become global MSBuild properties, so this covers `dotnet run` (which does
        // not accept `-nodeReuse:false` on its command line - unrecognized arguments there are forwarded to
        // the app being run) as well as `dotnet build`.
        foreach (var pair in NoPersistentBuildServers)
        {
            startInfo.Environment[pair.Key] = pair.Value;
        }

        // GlueTestBootstrap.EnsureHeadlessProjectLoadReady runs Glue's real SetMsBuildEnvironmentVariable,
        // which points MSBUILD_EXE_PATH at a pre-7 SDK so Microsoft.Build.Evaluation can resolve SDK-style
        // .csproj files in-process. A child dotnet CLI must not inherit that - it pins the child to that old
        // MSBuild and breaks the build. Production clears it around its own nested invocations for exactly
        // this reason (see CompilerPlugin's Compiler.cs and SyncedProjects' ProjectListEntry.xaml.cs).
        startInfo.Environment.Remove("MSBUILD_EXE_PATH");

        var output = new StringBuilder();
        using var stdoutClosed = new ManualResetEventSlim(false);
        using var stderrClosed = new ManualResetEventSlim(false);

        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, e) => Collect(e, output, stdoutClosed);
        process.ErrorDataReceived += (_, e) => Collect(e, output, stderrClosed);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.StandardInput.Close();

        var limit = timeout ?? DefaultTimeout;
        if (!process.WaitForExit((int)limit.TotalMilliseconds))
        {
            // entireProcessTree: the CLI is a launcher - killing it alone would leave the MSBuild worker
            // nodes and the compiler server it started still running, which is the accumulation this whole
            // helper exists to prevent.
            try { process.Kill(entireProcessTree: true); } catch { /* already gone */ }
            try { process.WaitForExit((int)StreamFlushTimeout.TotalMilliseconds); } catch { }

            return (TimedOutExitCode,
                $"`dotnet {arguments}` did not finish within {limit.TotalMinutes:0.#} minutes and was killed." +
                Environment.NewLine + Snapshot(output));
        }

        stdoutClosed.Wait(StreamFlushTimeout);
        stderrClosed.Wait(StreamFlushTimeout);

        return (process.ExitCode, Snapshot(output));
    }

    /// <summary>
    /// Distinct from any exit code the dotnet CLI itself produces, so a timeout is never mistaken for a
    /// build failure in a test's assertion message.
    /// </summary>
    public const int TimedOutExitCode = -9009;

    private static readonly Dictionary<string, string> NoPersistentBuildServers = new()
    {
        // The build's MSBuild worker nodes exit with it instead of lingering for 15 minutes holding the
        // inherited stdout/stderr pipe handles open.
        ["MSBUILDDISABLENODEREUSE"] = "1",
        // Same for the Roslyn compiler server (VBCSCompiler.exe), which otherwise survives ~10 minutes.
        ["UseSharedCompilation"] = "false",
        // ...and for the SDK's own persistent MSBuild server process.
        ["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0",
    };

    private static void Collect(DataReceivedEventArgs e, StringBuilder output, ManualResetEventSlim closed)
    {
        // A null Data is the stream-closed signal, not a blank line.
        if (e.Data == null)
        {
            closed.Set();
            return;
        }

        lock (output)
        {
            output.AppendLine(e.Data);
        }
    }

    private static string Snapshot(StringBuilder output)
    {
        lock (output)
        {
            return output.ToString();
        }
    }
}
