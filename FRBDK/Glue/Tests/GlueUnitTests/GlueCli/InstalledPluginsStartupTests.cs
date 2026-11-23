using System;
using System.IO;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Cli;

/// <summary>
/// The built GlueCli in its own process, loading every installed plugin by MEF scan the way the editor does.
/// The in-process tests use their own host and register only some plugins; GlueControl and the tree view
/// cannot be registered in the test process (they replace process-wide statics such as GlueState.Find and
/// CommandSender and have no way to undo it), so this is what covers their headless StartUp and load handlers.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class InstalledPluginsStartupTests
{
    [Fact]
    public void Check_WithEveryInstalledPlugin_StartsAndLoadsWithNoPluginFailure()
    {
        var glueCli = Path.Combine(RepoPaths.FrbRoot, "FRBDK", "Glue", "Glue", "bin",
#if DEBUG
            "Debug",
#else
            "Release",
#endif
            "GlueCli.exe");
        File.Exists(glueCli).ShouldBeTrue($"Build GlueCli first: {glueCli}");
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        // The exe, not `dotnet GlueCli.dll`: plugins are found next to the running executable, which would
        // then be dotnet.exe, and the run would quietly load none of them.
        var (exitCode, output) = NestedDotnetCli.Run(
            $"check \"{Path.Combine(project.Root, "Beefball", "Beefball.csproj")}\" --json",
            TimeSpan.FromMinutes(3), executable: glueCli);

        exitCode.ShouldBe(0, output);
        output.ShouldNotContain("Failed in", Case.Sensitive);
        output.ShouldNotContain("failed to start", Case.Sensitive);
    }
}
