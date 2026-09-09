using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using CompilerLibrary.ViewModels;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GameCommunicationPlugin.GlueControl.CommandSending;
using GameCommunicationPlugin.GlueControl.Dtos;
using ToolsUtilities;

namespace GlueUnitTests.TestSupport;

/// <summary>
/// Launches a gold project's actual built game process and drives it through Glue's real
/// <see cref="CommandSender"/>/<see cref="GameJsonCommunicationPlugin.Common.GameConnectionManager"/> - the
/// exact socket protocol production Glue uses to talk to a running game, not a test-only stand-in.
///
/// This exists because live edit's runtime behavior (GlueControl's embedded Screen/DTO handling in
/// <c>Embedded/CommandReceiver.cs</c>) only exists inside a compiled, running game process - it is
/// `&lt;Compile Remove&gt;`d from GameCommunicationPlugin.csproj (see #1986) specifically because it needs
/// a real game to run against. A GoldProjectCompileTests-style test proves the closure compiles; this
/// proves it actually does the right thing when driven the way Glue really drives it.
///
/// Requires a project whose Game1 already constructs GlueControlManager/GameConnectionManager - i.e. one
/// that went through the real Glue "enable live edit" flow at least once. See Samples/EditorTest1's
/// .gitignore exception and comment for why its Generated.cs is checked in rather than regenerated per run
/// (MainCompilerPlugin, which owns that wiring, isn't safe to run in the test host).
/// </summary>
internal sealed class LiveGameProcess : IDisposable
{
    /// <summary>
    /// Set to "1" on the launched process's environment - read by <c>EditorTest1.Game1.Initialize</c> right
    /// after <c>FlatRedBallServices.InitializeFlatRedBall</c> creates the real (still hidden) SDL window, to
    /// position it off the combined virtual desktop before it's ever shown. See that call site's comment for
    /// why this is the only point where setting <c>Window.Position</c> actually sticks.
    /// </summary>
    internal const string OffscreenWindowEnvironmentVariable = "FRB_LIVE_GAME_TEST_OFFSCREEN";

    /// <summary>
    /// Set to a directory of OpenGL runtime DLLs (Mesa's llvmpipe build) to have them copied next to the
    /// game before it launches. A GPU-less CI runner resolves opengl32.dll to Windows' generic software
    /// implementation, which is OpenGL 1.1 and has no framebuffer objects, so MonoGame's GraphicsDevice
    /// throws NoSuitableGraphicsDeviceException and the game dies before it can connect back. Windows
    /// resolves opengl32.dll from the exe's own directory ahead of System32, so Mesa's copy sitting there
    /// overrides the system one. Left unset on a developer machine, where the real driver already works.
    /// </summary>
    internal const string GraphicsRuntimeDirectoryEnvironmentVariable = "FRB_LIVE_GAME_TEST_GL_RUNTIME";

    readonly TempDir project;
    readonly System.Diagnostics.Process process;
    readonly GameJsonCommunicationPlugin.Common.GameConnectionManager connectionManager;
    readonly ConcurrentQueue<string> capturedStandardOutput;
    readonly ConcurrentQueue<string> capturedStandardError;
    readonly ConcurrentQueue<string> connectionDiagnosticLog;

    /// <summary>
    /// Response timeout for every command a LiveGame test sends after the process has connected -
    /// see <see cref="Send"/>/<see cref="Send{T}"/>. Deliberately well above
    /// GameConnectionManager's interactive-editor default of 10 seconds (unchanged there - a real
    /// user wants a dead game reported quickly): a test's first command routinely drives real work
    /// (e.g. loading a Screen's content) on a freshly-launched, cold-JIT'd game, on CI rendering
    /// through Mesa's llvmpipe software GL rather than a GPU - see issue #2244, where that combination
    /// took long enough to blow through the 10-second default and fail with "No response received"
    /// though the same test passed on a re-run of the same commit.
    /// </summary>
    const double CommandResponseTimeoutInSeconds = 30;

    public string ProjectRoot => project.Root;

    LiveGameProcess(TempDir project, System.Diagnostics.Process process,
        GameJsonCommunicationPlugin.Common.GameConnectionManager connectionManager,
        ConcurrentQueue<string> capturedStandardOutput,
        ConcurrentQueue<string> capturedStandardError,
        ConcurrentQueue<string> connectionDiagnosticLog)
    {
        this.project = project;
        this.process = process;
        this.connectionManager = connectionManager;
        this.capturedStandardOutput = capturedStandardOutput;
        this.capturedStandardError = capturedStandardError;
        this.connectionDiagnosticLog = connectionDiagnosticLog;
    }

    /// <summary>
    /// Copies <paramref name="repoRelativeProjectDirectory"/> out of the repo (so a run never dirties the
    /// working tree or a checked-in port number), refreshes its live-edit closure from the CURRENT branch
    /// source (see <paramref name="refreshLiveEditCodeFromSource"/>), builds it, launches its built exe, and
    /// waits for it to connect back to a freshly-started Glue-side listener on a port picked for this run.
    /// Throws if the build fails, the exe exits before connecting, or it never connects within
    /// <paramref name="connectTimeout"/>.
    ///
    /// Must be called on an STA thread - <see cref="GoldProject.LoadInGlueAsync"/> and
    /// <see cref="GoldProject.EmbedLiveEditCode"/> both require one (plugin StartUp methods construct real
    /// WPF toolbars). Call <see cref="GlueTestBootstrap.EnsureGameProjectPluginsRegistered"/> first.
    /// </summary>
    /// <param name="refreshLiveEditCodeFromSource">
    /// Default true: loads the project into Glue and regenerates its <c>GlueControl/**</c> closure from
    /// this branch's <c>Embedded/*.cs</c> source (exactly what GoldProject.EmbedLiveEditCode does for the
    /// other gold projects), so the harness always exercises current code rather than whatever was checked
    /// in. Game1.Generated.cs is preserved exactly as checked in either way - see the class doc comment for
    /// why (its GlueControlManager wiring can't be regenerated in the test host) - only its port is patched.
    /// </param>
    /// <param name="afterLoadBeforeEmbed">
    /// Optional hook that runs after the project is loaded into Glue (so <c>ObjectFinder.Self</c>/
    /// <c>GlueCommands.Self</c> are usable, real project-editing APIs like
    /// <c>GluxCommands.AddNamedObjectToAsync</c> can add fixture objects) but before the live-edit closure
    /// is embedded and the project built - so a test-only object added here reaches the built exe. Only
    /// called when <paramref name="refreshLiveEditCodeFromSource"/> is true.
    /// </param>
    public static async Task<LiveGameProcess> StartAsync(
        string repoRelativeProjectDirectory,
        string csprojRelativeToProjectRoot,
        string exeRelativeToProjectRoot,
        bool refreshLiveEditCodeFromSource = true,
        TimeSpan? connectTimeout = null,
        Func<Task> afterLoadBeforeEmbed = null)
    {
        var project = GoldProject.CopyOutOfRepo(repoRelativeProjectDirectory);
        try
        {
            var csprojPath = Path.Combine(project.Root, csprojRelativeToProjectRoot);
            var projectDirectory = Path.Combine(project.Root, Path.GetDirectoryName(csprojRelativeToProjectRoot) ?? "");
            var game1GeneratedPath = Path.Combine(projectDirectory, "Game1.Generated.cs");

            var port = GetFreeTcpPort();
            var game1GeneratedWithPort = PatchGlueControlPort(File.ReadAllText(game1GeneratedPath), game1GeneratedPath, port);
            File.WriteAllText(game1GeneratedPath, game1GeneratedWithPort);

            if (refreshLiveEditCodeFromSource)
            {
                // Deliberately does NOT call GoldProject.DeleteGeneratedCode first: that would also delete
                // Game1.Generated.cs, and nothing in this headless host can regenerate its live-edit wiring
                // (MainCompilerPlugin owns that - see the class doc comment). Everything else still gets a
                // real regenerate-if-changed pass from LoadInGlueAsync, same as any other gold project.
                await GoldProject.LoadInGlueAsync(csprojPath);

                if (afterLoadBeforeEmbed != null)
                {
                    await afterLoadBeforeEmbed();
                }

                GoldProject.EmbedLiveEditCode();

                // LoadInGlueAsync's normal codegen pass may have rewritten Game1.Generated.cs without the
                // live-edit wiring (it doesn't know about MainCompilerPlugin's Game1GlueControlGenerator
                // either) - put back the one real, working copy regardless of what it produced.
                File.WriteAllText(game1GeneratedPath, game1GeneratedWithPort);
            }

            var (exitCode, buildOutput) = NestedDotnetCli.Run($"build \"{csprojPath}\" -c Debug");
            if (exitCode != 0)
            {
                throw new InvalidOperationException($"Failed to build {csprojPath}:{Environment.NewLine}{buildOutput}");
            }

            // Glue-side of the protocol: listens, and completes the two-socket handshake the game's
            // GlueCommunication.GameConnectionManager initiates on startup (see GameConnectionManager.cs -
            // Glue-side accepts twice, byte 1 for glue->game, byte 2 for game->glue).
            var connectionDiagnosticLog = new ConcurrentQueue<string>();
            var connectionManager = new GameJsonCommunicationPlugin.Common.GameConnectionManager((_, __) => { })
            {
                Port = port,
                TimeoutInSeconds = CommandResponseTimeoutInSeconds,
                // Records every connect/reset transition with a wall-clock timestamp - if a Send times out
                // because the socket was silently torn down and re-listened mid-request (rather than the
                // game genuinely being slow to reply), this is the only place that would show it; the
                // default LogAction only reaches Debug.WriteLine and a WPF-only plugin host, neither of
                // which this headless test process has.
                LogAction = message => connectionDiagnosticLog.Enqueue($"{DateTime.UtcNow:HH:mm:ss.fff} {message}")
            };
            GameJsonCommunicationPlugin.Common.GameConnectionManager.Self = connectionManager;

            var exePath = Path.Combine(project.Root, exeRelativeToProjectRoot);
            CopySuppliedGraphicsRuntimeNextToGame(Path.GetDirectoryName(exePath)!);

            var capturedStandardOutput = new ConcurrentQueue<string>();
            var capturedStandardError = new ConcurrentQueue<string>();
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo(exePath)
                {
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(exePath),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                }
            };
            process.StartInfo.Environment[OffscreenWindowEnvironmentVariable] = "1";
            // Async, event-based capture rather than ReadToEnd(): this process is long-lived (killed by
            // Dispose, not naturally exiting), so a blocking read would never return - see NestedDotnetCli's
            // doc comment for the same deadlock shape with dotnet build's child MSBuild nodes. This is what
            // CommandReceiver.Receive's catch-all writes an unhandled DTO-handling exception to (see
            // Embedded/CommandReceiver.cs) - the only way to observe a screen-load exception from outside
            // the game process, since ScreenManager.LoadScreen sets CurrentScreen before Initialize runs and
            // the SelectObjectDto response carries no failure signal either way.
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    capturedStandardOutput.Enqueue(e.Data);
                }
            };
            // stderr is where an unhandled exception in the game lands, and that is the only account of why
            // a startup crash happened - the exit code on its own is just 0xE0434352, "managed exception".
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    capturedStandardError.Enqueue(e.Data);
                }
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var deadline = DateTime.UtcNow + (connectTimeout ?? TimeSpan.FromSeconds(20));
            while (!connectionManager.IsConnected && DateTime.UtcNow < deadline)
            {
                if (process.HasExited)
                {
                    // WaitForExit() with no timeout also waits for the async output handlers to drain, so the
                    // crash output is all in the queues by the time it is read. The overload taking a
                    // timeout does not, and returns with the interesting lines still unread.
                    process.WaitForExit();
                    connectionManager.Dispose();
                    throw new InvalidOperationException(
                        $"{exePath} exited before connecting (exit code {process.ExitCode})." +
                        DescribeCapturedOutput(capturedStandardOutput, capturedStandardError));
                }
                await Task.Delay(100);
            }

            if (!connectionManager.IsConnected)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                connectionManager.Dispose();
                throw new InvalidOperationException(
                    $"{exePath} did not connect back to Glue on port {port} within {(connectTimeout ?? TimeSpan.FromSeconds(20)).TotalSeconds:0}s." +
                    DescribeCapturedOutput(capturedStandardOutput, capturedStandardError));
            }

            // Wired up so the real CommandSender can be used exactly as Glue uses it - see
            // CommandSender.SendCommand, which dereferences CompilerViewModel when printing is enabled.
            // IsPrintEditorToGameCheckboxChecked defaults false, so this only needs to exist.
            CommandSender.Self.CompilerViewModel = CompilerViewModel.Self;

            return new LiveGameProcess(project, process, connectionManager, capturedStandardOutput, capturedStandardError, connectionDiagnosticLog);
        }
        catch
        {
            project.Dispose();
            throw;
        }
    }

    /// <summary>
    /// The running game's current screen as a fully-qualified type name (e.g.
    /// "EditorTest1.Screens.GameScreen"), or "" if no screen is currently loaded - see
    /// GlueControlManager.ProcessMessage's "GetCurrentScreen" case, which is exactly what this sends.
    /// </summary>
    public async Task<string> GetCurrentScreenName()
    {
        var screenName = await CommandSender.Self.GetScreenName();
        return screenName ?? "";
    }

    /// <summary>
    /// A snapshot of the game process's captured stdout so far, one entry per line. An unhandled exception
    /// from a DTO handler (e.g. a screen failing to Initialize) lands here via
    /// CommandReceiver.Receive's catch-all Console.WriteLine - see the capture wiring in StartAsync for why
    /// this is the only way to observe that from outside the process.
    /// </summary>
    public IReadOnlyList<string> GetCapturedStandardOutputLines() => capturedStandardOutput.ToArray();

    /// <summary>
    /// Copies the GL runtime named by <see cref="GraphicsRuntimeDirectoryEnvironmentVariable"/>, if any,
    /// into the directory the game is about to launch from. Each test builds its game into its own temp
    /// directory, so CI cannot stage these DLLs itself the way it can for an in-process test host.
    /// </summary>
    static void CopySuppliedGraphicsRuntimeNextToGame(string exeDirectory)
    {
        var runtimeDirectory = Environment.GetEnvironmentVariable(GraphicsRuntimeDirectoryEnvironmentVariable);
        if (string.IsNullOrEmpty(runtimeDirectory))
        {
            return;
        }

        // Failing loudly rather than launching anyway: a game left on the system's OpenGL 1.1 dies with a
        // NoSuitableGraphicsDeviceException that says nothing about the DLLs having gone missing.
        if (!Directory.Exists(runtimeDirectory))
        {
            throw new DirectoryNotFoundException(
                $"{GraphicsRuntimeDirectoryEnvironmentVariable} is set to \"{runtimeDirectory}\", which does not exist.");
        }

        foreach (var dll in Directory.GetFiles(runtimeDirectory, "*.dll"))
        {
            File.Copy(dll, Path.Combine(exeDirectory, Path.GetFileName(dll)), overwrite: true);
        }
    }

    /// <summary>
    /// Formats whatever the game printed before it died, for a <see cref="StartAsync"/> failure message.
    /// Without it the only evidence is an exit code, and a game that throws on startup reports the same
    /// 0xE0434352 ("managed exception") whatever the cause.
    /// </summary>
    static string DescribeCapturedOutput(
        ConcurrentQueue<string> standardOutput, ConcurrentQueue<string> standardError)
    {
        var result = new System.Text.StringBuilder();

        foreach (var (name, lines) in new[]
                 {
                     ("stderr", standardError.ToArray()),
                     ("stdout", standardOutput.ToArray()),
                 })
        {
            result.Append(Environment.NewLine).Append(Environment.NewLine).Append(name).Append(':')
                .Append(Environment.NewLine)
                .Append(lines.Length == 0 ? "<empty>" : string.Join(Environment.NewLine, lines));
        }

        return result.ToString();
    }

    /// <summary>
    /// Sends any DTO over the real CommandSender, for tests that care about what the running game answers
    /// rather than about a particular editor gesture.
    /// </summary>
    public async Task<GeneralResponse<string>> Send(object dto)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await CommandSender.Self.Send(dto);
        AppendDiagnosticsOnFailure(response, stopwatch.Elapsed);
        return response;
    }

    /// <summary>
    /// Same as <see cref="Send"/>, but deserializes the game's raw JSON reply into <typeparamref name="T"/>
    /// - see <see cref="CommandSender.Send{T}"/> - for tests that need the handler's typed return value
    /// rather than just success/failure.
    /// </summary>
    public async Task<GeneralResponse<T>> Send<T>(object dto)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await CommandSender.Self.Send<T>(dto);
        AppendDiagnosticsOnFailure(response, stopwatch.Elapsed);
        return response;
    }

    /// <summary>
    /// A failed Send (e.g. the timeout described by <see cref="CommandResponseTimeoutInSeconds"/>
    /// elapsing) otherwise gives no way to tell "the game hung" from "the game crashed" from outside
    /// the process - appending what it printed is the only account of which, same rationale as
    /// <see cref="DescribeCapturedOutput"/>'s use in <see cref="StartAsync"/>. The elapsed time and
    /// IsConnected snapshot separate "the request legitimately took the whole timeout" from "the
    /// socket was torn down mid-request and this failed fast" - see #2244, where the failure message
    /// alone ("No response received") could not tell those apart.
    /// </summary>
    void AppendDiagnosticsOnFailure<T>(GeneralResponse<T> response, TimeSpan elapsed)
    {
        if (response?.Succeeded == false)
        {
            response.Message +=
                $"{Environment.NewLine}{Environment.NewLine}Send took {elapsed.TotalSeconds:0.00}s " +
                $"(configured timeout {CommandResponseTimeoutInSeconds:0}s), IsConnected={connectionManager.IsConnected}." +
                $"{Environment.NewLine}{Environment.NewLine}connection log:{Environment.NewLine}" +
                (connectionDiagnosticLog.IsEmpty ? "<empty>" : string.Join(Environment.NewLine, connectionDiagnosticLog)) +
                DescribeCapturedOutput(capturedStandardOutput, capturedStandardError);
        }
    }

    /// <summary>
    /// Sends the same SelectObjectDto Glue sends when the user clicks an entity in the tree, over the real
    /// CommandSender/wire protocol - see RefreshManager.PushGlueSelectionToGame for the production version
    /// of this DTO shape.
    ///
    /// Deliberately does NOT go through GlueState.Self.CurrentEntitySave / PushGlueSelectionToGame
    /// themselves: setting CurrentEntitySave routes through GlueState.Find.TreeNodeByTag, which needs a
    /// real, populated WPF tree view. There isn't one in this headless host, so the assignment silently
    /// no-ops (CurrentElement stays null) and nothing gets sent - a limitation of driving Glue's UI-bound
    /// selection state outside a UI, not a product bug. Building the DTO directly sends the exact same
    /// thing a real click would, without depending on tree-view state this host can't provide.
    /// </summary>
    public async Task<GeneralResponse<string>> SelectEntity(string entityNameGlue)
    {
        var entity = ObjectFinder.Self.GetEntitySave(entityNameGlue);
        if (entity == null)
        {
            throw new InvalidOperationException($"No entity named \"{entityNameGlue}\" found in the loaded project.");
        }

        var dto = new SelectObjectDto
        {
            ElementNameGlue = entityNameGlue,
            EntitySave = entity,
        };
        return await CommandSender.Self.Send(dto);
    }

    /// <summary>
    /// Sends the same SelectObjectDto Glue sends when the user clicks a Screen in the tree - see
    /// SelectEntity's doc comment for why this builds the DTO by hand instead of going through
    /// GlueState.Self.CurrentScreenSave / PushGlueSelectionToGame.
    ///
    /// Deliberately does NOT set BackupElementNameGlue - a real click on an abstract Screen with a
    /// concrete derived Screen would carry the derived Screen's name there (see
    /// RefreshManager.PushGlueSelectionToGame), but this is for driving the "no concrete derived Screen
    /// exists" case (#2006) where production code now leaves it null too.
    /// </summary>
    public async Task<GeneralResponse<string>> SelectScreen(string screenNameGlue)
    {
        var screen = ObjectFinder.Self.GetScreenSave(screenNameGlue);
        if (screen == null)
        {
            throw new InvalidOperationException($"No screen named \"{screenNameGlue}\" found in the loaded project.");
        }

        var dto = new SelectObjectDto
        {
            ElementNameGlue = screenNameGlue,
            ScreenSave = screen,
        };
        return await CommandSender.Self.Send(dto);
    }

    static string PatchGlueControlPort(string game1GeneratedContents, string game1GeneratedPath, int port)
    {
        // Exactly two literal call sites bake in the checked-in port (8846) - see CompilerSettings.json's
        // PortNumber and Game1GlueControlGenerator. Asserting the count catches this drifting silently if
        // the checked-in fixture is ever regenerated with a different port baked in.
        var occurrences = CountOccurrences(game1GeneratedContents, "(8846)");
        if (occurrences != 2)
        {
            throw new InvalidOperationException(
                $"Expected exactly 2 occurrences of \"(8846)\" in {game1GeneratedPath} (GameConnectionManager and GlueControlManager construction), found {occurrences}. The port-patch below no longer matches the file - update it.");
        }

        return game1GeneratedContents.Replace("(8846)", $"({port})");
    }

    static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }

    static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public void Dispose()
    {
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
        try { process.Dispose(); } catch { }
        try { connectionManager.Dispose(); } catch { }
        if (GameJsonCommunicationPlugin.Common.GameConnectionManager.Self == connectionManager)
        {
            GameJsonCommunicationPlugin.Common.GameConnectionManager.Self = null;
        }
        project.Dispose();
    }
}
