using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using GlueCli;
using GlueUnitTests.TestSupport;

namespace GlueUnitTests.Cli;

/// <summary>
/// Runs GlueCli in-process for a test and parses its --json output. Every command's test class uses this,
/// against a <see cref="GoldProject.CopyOutOfRepo"/> copy of a sample. The host registers plugins through
/// <see cref="GlueTestBootstrap"/> rather than GlueCli's MEF scan of the Plugins folder, since that scan
/// would load every plugin into the test process for the rest of the run; the real exe is what exercises
/// the scan.
/// </summary>
public static class GlueCliTestRun
{
    /// <returns>The exit code, and stdout followed by stderr for assertion messages.</returns>
    public static (int exitCode, string output) Run(params string[] args)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exitCode = GlueCliRunner.Run(args, stdout, stderr, new TestHost());
        return (exitCode, stdout + Environment.NewLine + StderrMarker + Environment.NewLine + stderr);
    }

    /// <summary>The --json result from <see cref="Run"/>'s output. <c>Edit</c> comes back as a JsonElement.</summary>
    public static CliResult Parse(string output)
    {
        var json = output.Substring(0, output.IndexOf(Environment.NewLine + StderrMarker, StringComparison.Ordinal));
        return JsonSerializer.Deserialize<CliResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    /// <summary>A property of the command-specific <see cref="CliResult.Edit"/> object, or null.</summary>
    public static string? EditProperty(CliResult result, string camelCaseName)
    {
        if (result.Edit is not JsonElement edit || !edit.TryGetProperty(camelCaseName, out var value) ||
            value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }
        return value.ToString();
    }

    const string StderrMarker = "stderr:";

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
