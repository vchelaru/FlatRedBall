using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Npc.Managers;

/// <summary>
/// Runs the dotnet CLI's template engine. Separated behind an interface so project creation can be
/// tested without the SDK, the network, or a real template package installed.
/// </summary>
public interface IDotnetNewService
{
    /// <summary>
    /// Installs/updates <paramref name="templatePackageId"/> to whatever the latest published version
    /// is, then runs the template into <paramref name="outputDirectory"/>.
    /// </summary>
    Task<GeneralResponse> CreateProjectAsync(
        string templatePackageId, string templateShortName, string projectName, string outputDirectory);
}

public class DotnetNewService : IDotnetNewService
{
    static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);

    public async Task<GeneralResponse> CreateProjectAsync(
        string templatePackageId, string templateShortName, string projectName, string outputDirectory)
    {
        // Always install rather than probing "is it already there" first: `dotnet new install` on an
        // already-installed package checks nuget and updates to the latest version, but skipping it
        // once any version was ever installed meant Glue kept silently reusing whatever stale template
        // (and stale engine version it referenced) happened to be cached locally.
        var install = await RunAsync($"new install {templatePackageId}");

        if (install.ExitCode != 0)
        {
            return GeneralResponse.UnsuccessfulWith(
                $"Could not install the {templatePackageId} template package. FlatRedBall 2 projects " +
                $"are created by the dotnet CLI, so the .NET SDK has to be installed and on the PATH, " +
                $"and this needs internet access.\n\n{install.Output}");
        }

        var create = await RunAsync(
            $"new {templateShortName} -n \"{projectName}\" -o \"{outputDirectory}\"");

        return create.ExitCode == 0
            ? GeneralResponse.SuccessfulResponse
            : GeneralResponse.UnsuccessfulWith(
                $"`dotnet new {templateShortName}` failed:\n\n{create.Output}");
    }

    /// <summary>
    /// Both streams are drained concurrently and the whole process tree is killed on timeout. The
    /// obvious version - redirect, StandardOutput.ReadToEnd(), WaitForExit() - deadlocks: a dotnet
    /// command can leave build worker nodes holding the inherited stdout handle open long after it
    /// exits, so ReadToEnd never sees EOF and blocks for the nodes' idle timeout instead. Node reuse is
    /// disabled here for the same reason. See GlueUnitTests' NestedDotnetCli and GitHub issue #1969.
    /// </summary>
    static async Task<(int ExitCode, string Output)> RunAsync(string arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet", arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.EnvironmentVariables["MSBUILDDISABLENODEREUSE"] = "1";
        startInfo.EnvironmentVariables["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";

        var output = new StringBuilder();

        try
        {
            using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

            void Collect(string line)
            {
                if (line != null)
                {
                    lock (output) { output.AppendLine(line); }
                }
            }

            process.OutputDataReceived += (_, e) => Collect(e.Data);
            process.ErrorDataReceived += (_, e) => Collect(e.Data);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var cancellation = new System.Threading.CancellationTokenSource(Timeout);

            try
            {
                await process.WaitForExitAsync(cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return (-1, $"`dotnet {arguments}` did not finish within {Timeout.TotalMinutes} minutes.");
            }

            lock (output) { return (process.ExitCode, output.ToString()); }
        }
        catch (Exception exception)
        {
            // Most commonly the SDK simply is not installed, so "dotnet" does not resolve.
            return (-1, $"Could not run `dotnet {arguments}`: {exception.Message}");
        }
    }
}
