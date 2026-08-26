using System.IO;
using System.Linq;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GameCommunicationPlugin;

/// <summary>
/// Everything under GlueControl/Embedded is &lt;Compile Remove&gt;d from GameCommunicationPlugin.csproj -
/// it is copied into the user's game project and compiled there, never inside Glue. Game projects
/// define MONOGAME;DESKTOP_GL;MONOGAME_381 (see any Samples/*/**.csproj); WINDOWS is defined by
/// exactly one project in this repo (Tests/TestProjectDesktopNet6) and by no project a user ever
/// gets. So a `#if WINDOWS` block in embedded code is dead in every real project - the `#else` arm
/// is what ships - and it fails silently: it compiles, it reads as platform-guarded, and the stubs
/// return default values that look like real answers.
/// </summary>
public class EmbeddedCodePlatformDefineTests
{
    [Fact]
    public void EmbeddedCode_DoesNotBranchOnTheWindowsDefine()
    {
        var embeddedRoot = Path.Combine(RepoPaths.FrbRoot, "FRBDK", "Glue", "GameCommunicationPlugin",
            "GlueControl", "Embedded");
        Directory.Exists(embeddedRoot).ShouldBeTrue($"Expected embedded live-edit source at {embeddedRoot}.");

        var offenders = Directory.EnumerateFiles(embeddedRoot, "*.cs", SearchOption.AllDirectories)
            .SelectMany(file => File.ReadAllLines(file)
                .Select((line, index) => (file, lineNumber: index + 1, text: line.Trim()))
                .Where(x => x.text.StartsWith("#if ") || x.text.StartsWith("#elif "))
                .Where(x => System.Text.RegularExpressions.Regex.IsMatch(x.text, @"\bWINDOWS\b")))
            .Select(x => $"{Path.GetRelativePath(embeddedRoot, x.file)}:{x.lineNumber}: {x.text}")
            .ToList();

        offenders.ShouldBeEmpty(
            "Embedded live-edit code is compiled inside the user's game project, which never defines " +
            "WINDOWS - so the #else arm is what actually ships and the guarded code is dead. Use a " +
            "runtime OS check, or move the Windows-only work into Glue itself, which really is a " +
            "Windows-only assembly. Offenders:\n" + string.Join("\n", offenders));
    }
}
