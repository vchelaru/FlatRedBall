using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// Publishes a real Glue game with PublishAot and runs the native exe, so a change that breaks Native AOT
/// (a trimmed content reader, an XmlSerializer load, reflection the trimmer removed) fails here instead of
/// in a user's game. Part of #2363.
///
/// Tagged "LiveGame" rather than "BuildSmoke" because it launches a real MonoGame DesktopGL window, which
/// pr-tests.yml only provides (a software OpenGL runtime) to that category. Locally:
/// `dotnet test ... --filter "FullyQualifiedName~NativeAotPublishTests"`. Needs the Native AOT toolchain
/// (Visual Studio's C++ workload) and the ILCompiler NuGet package.
///
/// Survives-startup is the assertion. It catches the crashes found while bringing AOT up (Shader.xnb, .gumx,
/// content load), not silent behavior differences such as the entity factories once returning null.
/// </summary>
[Trait("Category", "LiveGame")]
public class NativeAotPublishTests
{
    static readonly TimeSpan PublishTimeout = TimeSpan.FromMinutes(10);
    static readonly TimeSpan RunDuration = TimeSpan.FromSeconds(10);

    // The ILCompiler targets run findvcvarsall.bat and read its console output as the linker's location. With
    // Visual Studio 2026, vcvarsall looks for vswhere.exe on PATH and, when the Installer folder isn't there,
    // prints "'vswhere.exe' is not recognized" to stderr, which lands in that output and becomes part of the
    // link.exe path (exit code 3). A Developer Command Prompt has the folder on PATH; a test host doesn't.
    static IReadOnlyDictionary<string, string> VswhereOnPath()
    {
        var installer = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio", "Installer");
        return new Dictionary<string, string>
        {
            ["PATH"] = installer + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH"),
        };
    }

    // StaFact, not Fact: GoldProject.LoadInGlueAsync needs an STA thread - see GoldProjectCompileTests.
    [StaFact]
    public async Task DialogBoxDemo_PublishedWithNativeAot_StartsAndKeepsRunning()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var project = GoldProject.CopyOutOfRepo("Samples/Platformer/DialogBoxDemo");
        var csproj = Path.Combine(project.Root, "DialogBoxDemo", "DialogBoxDemo.csproj");
        var gumFolder = Path.Combine(project.Root, "DialogBoxDemo", "Content", "GumProject");

        GoldProject.LinkToEngineSource(csproj);
        await GoldProject.LoadWithGumJsonAsync(project.Root, csproj, gumFolder);

        var publishDirectory = Path.Combine(project.Root, "aot-publish");
        var (exitCode, output) = NestedDotnetCli.Run(
            $"publish \"{csproj}\" -c Release -r win-x64 -p:PublishAot=true -o \"{publishDirectory}\"", PublishTimeout,
            environment: VswhereOnPath());
        exitCode.ShouldBe(0, $"dotnet publish -p:PublishAot=true failed:\n{output}");

        var exe = Path.Combine(publishDirectory, "DialogBoxDemo.exe");
        File.Exists(exe).ShouldBeTrue("Native AOT publish did not produce DialogBoxDemo.exe");

        LiveGameProcess.CopySuppliedGraphicsRuntimeNextToGame(publishDirectory);

        var captured = new ConcurrentQueue<string>();
        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo(exe)
            {
                UseShellExecute = false,
                WorkingDirectory = publishDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };
        process.OutputDataReceived += (_, e) => { if (e.Data != null) captured.Enqueue(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) captured.Enqueue(e.Data); };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            var exited = process.WaitForExit((int)RunDuration.TotalMilliseconds);
            if (exited)
            {
                // The no-timeout overload drains the output handlers, so the crash text is in the queue.
                process.WaitForExit();
                Assert.Fail(
                    $"The Native AOT game exited with code {process.ExitCode} within {RunDuration.TotalSeconds:0}s:\n" +
                    string.Join(Environment.NewLine, captured));
            }
        }
        finally
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
        }
    }
}
