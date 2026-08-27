using System;
using System.IO;
using Xunit;

namespace GlueUnitTests.GameCommunicationPlugin;

/// <summary>
/// A user reported a boss entity (a subclass with its own shared-static content field, the
/// common case for an Entity-declared file) crashing with ObjectDisposedException on a texture
/// that a live-edit PNG save should have reloaded. <c>HandleDto(ForceReloadFileDto)</c> reflects
/// into each container Type it's told about (<c>dto.ElementsContainingFile</c>) to find and
/// reassign that Type's own copy of the shared-static field, via
/// <c>elementType.GetField(dto.StrippedFileName)</c>.
///
/// <c>Type.GetField(string)</c> with no BindingFlags only searches PUBLIC members. But
/// <c>ReferencedFileSaveCodeGenerator</c> generates an Entity-declared shared-static field as
/// `protected static` unless the user explicitly checks "Has Public Property" in Glue -
/// <c>ReferencedFileSave.HasPublicProperty</c> defaults to false. So for the common, default case,
/// GetField silently returned null, the reflection call
/// (`reloadMethod != null &amp;&amp; fileObjectReference != null`) was skipped entirely, and that
/// entity type's static field kept pointing at the disposed texture until the game restarted - any
/// Sprite/AnimationChain later built from that stale field crashes exactly as reported.
///
/// See the sibling <c>ForceReloadFileDtoThreadSafetyTests</c> for why this asserts against the
/// checked-in source text rather than compiling/running it directly - CommandReceiver.cs only
/// compiles inside a real running game process.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class ForceReloadFileDtoFieldLookupTests
{
    [Fact]
    public void HandleDto_ForceReloadFileDto_FindsNonPublicAndInheritedSharedStaticFields()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "FRBDK", "Glue", "GameCommunicationPlugin", "GlueControl", "Embedded", "CommandReceiver.cs"));

        var methodBody = ExtractMethodBody(source, "HandleDto(Dtos.ForceReloadFileDto dto)");

        // Before the fix, this was the bare `elementType.GetField(dto.StrippedFileName)` - which only
        // finds public members, missing the protected-by-default shared-static field Glue actually
        // generates for an Entity-declared file.
        Assert.Contains("GetField(dto.StrippedFileName,", methodBody);
        Assert.Contains("BindingFlags.NonPublic", methodBody);
        Assert.Contains("BindingFlags.FlattenHierarchy", methodBody);
    }

    // Returns the full declaration (modifiers, return type, name, parameters) through the matching closing
    // brace - mirrors ForceReloadFileDtoThreadSafetyTests's helper.
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
