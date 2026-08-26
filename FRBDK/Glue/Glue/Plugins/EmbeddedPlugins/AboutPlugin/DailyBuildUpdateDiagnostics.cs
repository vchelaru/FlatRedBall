using System;
using System.IO;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.AboutPlugin;

internal static class DailyBuildUpdateDiagnostics
{
    const long MaximumLogLengthInBytes = 1_048_576;

    internal static string GetLogPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FlatRedBall",
            "GlueDailyBuildUpdate.log");
    }

    internal static void Append(string logPath, string message)
    {
        var directory = Path.GetDirectoryName(logPath);
        if (string.IsNullOrEmpty(directory))
        {
            throw new ArgumentException("The log path must include a directory.", nameof(logPath));
        }

        Directory.CreateDirectory(directory);

        if (File.Exists(logPath) && new FileInfo(logPath).Length > MaximumLogLengthInBytes)
        {
            File.WriteAllText(logPath, string.Empty);
        }

        File.AppendAllText(logPath, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
    }

    internal static void TryAppend(string logPath, string message)
    {
        try
        {
            Append(logPath, message);
        }
        catch
        {
            // Diagnostics must never stop the updater from showing its actual failure.
        }
    }
}
