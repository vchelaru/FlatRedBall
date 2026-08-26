using System.Collections.Generic;
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
    /// Candidate MSBuild.dll paths parsed out of "dotnet --list-sdks" output, newest first, skipping any
    /// SDK whose major version is above <paramref name="maximumMajorVersion"/>.
    ///
    /// The cap exists because the SDK resolvers MSBuild loads out of the chosen SDK's folder are compiled
    /// against that SDK's runtime: pointing a .NET 8 process at the .NET 10 SDK fails with "Could not load
    /// file or assembly 'System.Runtime, Version=10.0.0.0'" before resolving anything. So the caller passes
    /// the running runtime's major version. Any older SDK still resolves modern SDK-style projects, which
    /// is why everything below the cap stays in the list as a fallback.
    /// </summary>
    public static IReadOnlyList<string> GetMsBuildPathsNewestFirst(string dotnetListSdksOutput, int maximumMajorVersion)
    {
        return SdkLine.Matches(dotnetListSdksOutput)
            .OfType<Match>()
            .Where(m => int.Parse(m.Groups[1].Value) <= maximumMajorVersion)
            .OrderByDescending(m => int.Parse(m.Groups[1].Value))
            .ThenByDescending(m => int.Parse(m.Groups[2].Value))
            .ThenByDescending(m => int.Parse(m.Groups[3].Value))
            .Select(m => System.IO.Path.Combine(m.Groups[4].Value,
                m.Groups[1].Value + "." + m.Groups[2].Value + "." + m.Groups[3].Value, "MSBuild.dll"))
            .ToArray();
    }
}
