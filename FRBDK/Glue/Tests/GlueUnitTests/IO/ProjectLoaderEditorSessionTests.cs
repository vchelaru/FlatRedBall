using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
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

    [StaFact]
    public async Task LoadProject_ShouldRefreshTheErrorList()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var originalAction = RefreshCommands.RefreshErrorsAction;
        var refreshCount = 0;
        RefreshCommands.RefreshErrorsAction = () => refreshCount++;
        try
        {
            await GoldProject.LoadInGlueAsync(Path.Combine(project.Root, "Beefball", "Beefball.csproj"));
            await TaskManager.Self.WaitForAllTasksFinished();
        }
        finally
        {
            RefreshCommands.RefreshErrorsAction = originalAction;
        }

        refreshCount.ShouldBeGreaterThan(0);
    }

    [StaFact]
    public Task LoadProject_ShouldWriteTheRecordedFiles_WhenBeefballWasNeverGenerated() =>
        AssertEditorLoadWritesRecordedFiles("Beefball", "Beefball");

    [StaFact]
    public Task LoadProject_ShouldWriteTheRecordedFiles_WhenFormsSampleProjectWasNeverGenerated() =>
        AssertEditorLoadWritesRecordedFiles("FormsSampleProject", "FormsSampleProject");

    /// <summary>
    /// The files an editor open writes in a fresh clone of the sample, compared with the list in
    /// <c>EditorSessionLoadFiles/&lt;sample&gt;.txt</c>. The lists were recorded before plugin code generation
    /// moved out of <c>ReactToLoadedGlux</c>, so a plugin whose codegen stops running on open, or a new write
    /// on open, shows up as a difference. When a generator deliberately adds or drops a file, rerun with the
    /// environment variable <c>GLUE_RECORD_EDITOR_LOAD_FILES=1</c> and review the diff of the list.
    /// </summary>
    static async Task AssertEditorLoadWritesRecordedFiles(string sample, string projectFolder)
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();
        using var project = GoldProject.CopyOutOfRepo("Samples/" + sample);
        GoldProject.DeleteGeneratedCode(project.Root);
        var projectDirectory = Path.Combine(project.Root, projectFolder);
        var before = ReadAllFiles(projectDirectory);

        await GoldProject.LoadInGlueAsync(Path.Combine(projectDirectory, projectFolder + ".csproj"));
        await TaskManager.Self.WaitForAllTasksFinished();

        GlueTestBootstrap.RecordedDialogMessages.ShouldBeEmpty();
        var written = ReadAllFiles(projectDirectory)
            .Where(file => !before.TryGetValue(file.Key, out var bytes) || !bytes.AsSpan().SequenceEqual(file.Value))
            .Select(file => file.Key)
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var recordedList = Path.Combine(RepoPaths.FrbRoot, "FRBDK", "Glue", "Tests", "GlueUnitTests", "IO",
            "EditorSessionLoadFiles", sample + ".txt");
        if (Environment.GetEnvironmentVariable("GLUE_RECORD_EDITOR_LOAD_FILES") == "1")
        {
            File.WriteAllLines(recordedList, written);
        }
        var recorded = File.ReadAllLines(recordedList).Where(line => line.Length > 0).ToList();

        var missing = recorded.Except(written, StringComparer.OrdinalIgnoreCase).ToList();
        var extra = written.Except(recorded, StringComparer.OrdinalIgnoreCase).ToList();
        (missing.Count + extra.Count).ShouldBe(0,
            "Not written any more:\n" + string.Join("\n", missing) + "\nNewly written:\n" + string.Join("\n", extra));
    }

    static Dictionary<string, byte[]> ReadAllFiles(string root)
    {
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file);
            if (!relative.Split(Path.DirectorySeparatorChar).Any(segment => segment is "bin" or "obj" or ".vs"))
            {
                files[relative.Replace('\\', '/')] = File.ReadAllBytes(file);
            }
        }
        return files;
    }
}
