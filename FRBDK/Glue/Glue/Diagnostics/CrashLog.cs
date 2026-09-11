using System;
using System.IO;

namespace FlatRedBall.Glue.Diagnostics;

/// <summary>
/// Writes an unhandled exception to a file before Glue goes down. The output tab and the plugin-error
/// dialog die with the process, so without this a crash report reads "Glue crashes" with nothing to go on.
/// Files land next to the other diagnostics logs under %LOCALAPPDATA%\FlatRedBall\Glue\Diagnostics.
/// </summary>
public static class CrashLog
{
    public static string Directory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FlatRedBall", "Glue", "Diagnostics");

    /// <summary>
    /// Returns the written file's path, or null if writing failed - this runs inside the crash handler, so
    /// it must never throw.
    /// </summary>
    public static string Write(object exceptionObject)
    {
        try
        {
            System.IO.Directory.CreateDirectory(Directory);
            var filePath = Path.Combine(Directory, $"crash-{DateTime.Now:yyyyMMdd-HHmmss-fff}.log");
            File.WriteAllText(filePath,
                $"{DateTime.Now:O}{Environment.NewLine}{exceptionObject}{Environment.NewLine}");
            return filePath;
        }
        catch
        {
            return null;
        }
    }
}
