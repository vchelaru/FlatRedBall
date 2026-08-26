using System;
using System.IO;
using Xunit;

namespace GlueUnitTests.GameCommunicationPlugin;

/// <summary>
/// <c>GameCommunicationPlugin/GlueControl/Embedded/CommandReceiver.cs</c> is `&lt;Compile Remove&gt;`d from
/// this project - it only compiles inside a real, running game process (see the glue-live-game-testing
/// skill), so its runtime behavior can't be asserted with a normal in-process unit test without a sample
/// project wired up with reloadable global content, which does not exist today.
///
/// What CAN be asserted directly against this checked-in source (the master template
/// <c>EmbeddedCodeManager</c> copies byte-for-byte into every game project) is a structural property that
/// is the difference between the bug and the fix: <c>HandleDto(ForceReloadFileDto)</c> must never yield the
/// calling thread. It is only ever invoked already marshaled onto the game's primary thread
/// (<c>GlueControlManager.ApplySetMessage</c>), and it disposes/reassigns live textures - if it or its
/// retry-on-failure loop ever awaited (as it used to, via `await Task.Delay(250)`), the continuation could
/// resume on a thread-pool thread (a typical MonoGame app installs no SynchronizationContext), racing the
/// main thread's Draw() and disposing a texture out from under a mid-render Sprite. That surfaced as a
/// confusing deep ObjectDisposedException in FlatRedBall.Graphics.RenderBreak, far from the actual bug.
///
/// This test fails against the pre-fix source (which declared the method `async void` and awaited
/// Task.Delay) and passes once the method is fully synchronous.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class ForceReloadFileDtoThreadSafetyTests
{
    [Fact]
    public void HandleDto_ForceReloadFileDto_NeverYieldsTheCallingThread()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "FRBDK", "Glue", "GameCommunicationPlugin", "GlueControl", "Embedded", "CommandReceiver.cs"));

        var methodBody = ExtractMethodBody(source, "HandleDto(Dtos.ForceReloadFileDto dto)");
        var codeOnly = StripLineComments(methodBody);

        Assert.DoesNotContain("async", codeOnly);
        Assert.DoesNotContain("await", codeOnly);

        // Guards the diagnostic backstop too: if some future caller ever does invoke this off-thread, it
        // should fail loudly here rather than deep inside the renderer.
        Assert.Contains("FlatRedBallServices.IsThreadPrimary()", codeOnly);
    }

    // The method body's own explanatory comments talk about "async"/"await" by name - strip `//` comments
    // before searching so the test checks actual code, not its own commentary.
    static string StripLineComments(string code)
    {
        var lines = code.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var commentIndex = lines[i].IndexOf("//", StringComparison.Ordinal);
            if (commentIndex >= 0)
            {
                lines[i] = lines[i].Substring(0, commentIndex);
            }
        }
        return string.Join('\n', lines);
    }

    // Returns the full declaration (modifiers, return type, name, parameters) through the matching closing
    // brace - starting from the signature's own line, not just its body, so a check like "does this contain
    // async" also catches the method being re-declared `async void` again.
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
