using System;
using System.IO;
using Xunit;

namespace GlueUnitTests.GameCommunicationPlugin;

/// <summary>
/// A user's live-edit PNG save crashed with a TargetInvocationException wrapping
/// "System.InvalidOperationException: This image format is not supported". Glue only tells the game to
/// reload after it believes the changed file finished copying to the build folder (see
/// RefreshManager.HandleFileChanged's CopyToBuildFolderTaskIdFor wait), but that belief can be wrong on a
/// slow disk / antivirus-scanned drive - the copy can complete while the source is still being written by
/// the editing tool (e.g. ProMotion NG), leaving a truncated file in the build folder that fails to decode.
///
/// `GlobalContent.Reload(file)` already had a 10-attempt retry loop specifically for this
/// InvalidOperationException, but the per-Entity reflection-based reload right above it (called through
/// `MethodInfo.Invoke`, so the same InvalidOperationException arrives wrapped in
/// TargetInvocationException) had none - one entity hitting the race aborted the whole
/// HandleDto(ForceReloadFileDto) call, silently skipping every later entity AND the GlobalContent.Reload
/// call, with only a bare, context-free exception dump reaching the logs.
///
/// See the sibling ForceReloadFileDtoThreadSafetyTests/ForceReloadFileDtoFieldLookupTests for why this
/// asserts against the checked-in source text rather than compiling/running it directly - CommandReceiver.cs
/// only compiles inside a real running game process.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class ForceReloadFileDtoTransientFailureRetryTests
{
    [Fact]
    public void HandleDto_ForceReloadFileDto_RetriesBothReloadPathsWithDiagnosticLogging()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "FRBDK", "Glue", "GameCommunicationPlugin", "GlueControl", "Embedded", "CommandReceiver.cs"));

        var methodBody = ExtractMethodBody(source, "HandleDto(Dtos.ForceReloadFileDto dto)");

        // Both reload paths - the per-Entity reflection call and GlobalContent.Reload - must go through the
        // same retry helper. Before the fix, only GlobalContent.Reload had any retry protection.
        Assert.Contains("RetryOnTransientReloadFailure(", methodBody);
        Assert.Contains("reloadMethod.Invoke(", methodBody);
        Assert.Contains("GlobalContent.Reload(file)", methodBody);

        // The helper itself: must unwrap TargetInvocationException (reflection.Invoke's wrapper) down to
        // the real InvalidOperationException, and must log with enough context (which file, which attempt,
        // the actual exception) to diagnose the next occurrence without guessing.
        var helperBody = ExtractMethodBody(source, "RetryOnTransientReloadFailure(Action action, string label)");
        Assert.Contains("TargetInvocationException", helperBody);
        Assert.Contains("InvalidOperationException", helperBody);
        Assert.Contains("changed on disk, reloading, but got exception", helperBody);
        Assert.Contains("Console.WriteLine", helperBody);
    }

    // Mirrors ForceReloadFileDtoThreadSafetyTests's helper.
    static string ExtractMethodBody(string source, string signatureFragment)
    {
        var signatureIndex = source.IndexOf(signatureFragment, StringComparison.Ordinal);
        Assert.True(signatureIndex >= 0, $"Could not find \"{signatureFragment}\" in CommandReceiver.cs");

        var lineStart = source.LastIndexOf('\n', signatureIndex) + 1;

        var openBraceIndex = source.IndexOf('{', signatureIndex);
        Assert.True(openBraceIndex > 0);

        var depth = 0;
        for (var i = openBraceIndex; i < source.Length; i++)
        {
            if (source[i] == '{') depth++;
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source.Substring(lineStart, i - lineStart + 1);
                }
            }
        }

        throw new InvalidOperationException($"Could not find the end of the method body for \"{signatureFragment}\"");
    }

    static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "Samples")) &&
                Directory.Exists(Path.Combine(directory.FullName, "Engines")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException(
            "Could not locate the repo root (a directory containing both Samples/ and Engines/) above " +
            AppContext.BaseDirectory);
    }
}
