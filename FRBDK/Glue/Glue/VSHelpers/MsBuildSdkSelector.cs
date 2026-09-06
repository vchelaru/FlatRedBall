using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FlatRedBall.Glue.VSHelpers;

/// <summary>
/// Decides which installed .NET SDK's MSBuild the editor evaluates .csproj files with - the value that
/// goes into MSBUILD_EXE_PATH. Split out of MainGlueWindow so the choice can be unit tested against real
/// "dotnet --list-sdks" output instead of only against whatever SDKs the developer happens to have.
/// </summary>
public static class MsBuildSdkSelector
{
    // "8.0.303 [C:\Program Files\dotnet\sdk]"
    static readonly Regex SdkLine = new("([0-9]+)[.]([0-9]+)[.]([0-9]+) \\[(.*)\\]");

    /// <summary>
    /// The version of the Microsoft.Build engine Glue itself loads. MSBUILD_EXE_PATH does not change which
    /// engine runs - it only changes which SDK's targets and resolvers that engine reads - so this is the
    /// version an SDK's targets have to be written against.
    /// </summary>
    public static Version EngineVersion => GetMsBuildVersionOrNull(
        typeof(Microsoft.Build.Evaluation.Project).Assembly.Location) ?? new Version(0, 0);

    /// <summary>
    /// The MSBuild version an SDK ships, read off its MSBuild.dll, or null if there is no such file.
    /// </summary>
    public static Version? GetMsBuildVersionOrNull(string msBuildDllPath)
    {
        if (string.IsNullOrEmpty(msBuildDllPath) || !File.Exists(msBuildDllPath))
        {
            return null;
        }

        var fileVersion = FileVersionInfo.GetVersionInfo(msBuildDllPath);
        return new Version(fileVersion.FileMajorPart, fileVersion.FileMinorPart);
    }

    /// <summary>
    /// Usable MSBuild.dll paths parsed out of "dotnet --list-sdks" output, newest first.
    ///
    /// An SDK is usable when the MSBuild it ships is no newer than <paramref name="engineVersion"/>. An SDK
    /// newer than the engine is not: its Microsoft.Common.CurrentVersion.targets calls property functions
    /// the engine does not have (the .NET 8.0.4xx SDK uses [MSBuild]::SubstringByAsciiChars, added well
    /// after MSBuild 17.3), and every project evaluation dies on "Method not found". A newer *major* SDK
    /// fails earlier still - its resolvers are compiled against a newer runtime than the editor process, so
    /// they throw "Could not load file or assembly 'System.Runtime, Version=10.0.0.0'" before resolving
    /// anything. Older SDKs stay in the list as fallbacks; their targets evaluate modern SDK-style projects
    /// fine.
    ///
    /// Comparison is on major.minor because that is the band MSBuild adds features in - the .NET 6.0.4xx
    /// SDK ships MSBuild 17.3.4, which the 17.3.1 engine reads without trouble.
    /// </summary>
    /// <param name="getSdkMsBuildVersion">
    /// Reads an SDK's MSBuild version from a candidate path, returning null when the file is missing.
    /// Injected so the rule can be tested without installing SDKs.
    /// </param>
    public static IReadOnlyList<string> GetUsableMsBuildPathsNewestFirst(string dotnetListSdksOutput,
        Version engineVersion, Func<string, Version?> getSdkMsBuildVersion)
    {
        return SdkLine.Matches(dotnetListSdksOutput)
            .OfType<Match>()
            .OrderByDescending(m => int.Parse(m.Groups[1].Value))
            .ThenByDescending(m => int.Parse(m.Groups[2].Value))
            .ThenByDescending(m => int.Parse(m.Groups[3].Value))
            .Select(m => Path.Combine(m.Groups[4].Value,
                m.Groups[1].Value + "." + m.Groups[2].Value + "." + m.Groups[3].Value, "MSBuild.dll"))
            .Where(path =>
            {
                var sdkMsBuildVersion = getSdkMsBuildVersion(path);
                return sdkMsBuildVersion != null && sdkMsBuildVersion <= engineVersion;
            })
            .ToArray();
    }
}
