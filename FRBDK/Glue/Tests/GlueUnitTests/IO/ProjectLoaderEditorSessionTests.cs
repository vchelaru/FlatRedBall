using System;
using System.IO;
using System.Threading.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.IO;

/// <summary>
/// The editor's project open (<c>ProjectLoadScope.EditorSession</c>) still runs the work GlueCli's model-only
/// load skips. GlueCliRunnerTests covers the other side: check leaves the same stale file alone.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class ProjectLoaderEditorSessionTests
{
    [StaFact]
    public async Task LoadProject_ShouldRegenerateStaleCode_AndCreateMissingCode()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var projectDirectory = Path.Combine(project.Root, "Beefball");
        var csproj = Path.Combine(projectDirectory, "Beefball.csproj");
        await GoldProject.LoadInGlueAsync(csproj);

        var stale = Path.Combine(projectDirectory, "Screens", "GameScreen.Generated.cs");
        File.AppendAllText(stale, "// stale" + Environment.NewLine);
        var missing = Path.Combine(projectDirectory, "Performance", "PoolList.Generated.cs");
        File.Delete(missing);

        await GoldProject.LoadInGlueAsync(csproj);

        File.ReadAllText(stale).ShouldNotContain("// stale");
        File.Exists(missing).ShouldBeTrue();
    }
}
