using System.IO;
using System.Linq;
using GlueUnitTests.TestSupport;
using OfficialPlugins.FrbSourcePlugin.Managers;
using PluginTestbed.GlobalContentManagerPlugins;
using Shouldly;

namespace GlueUnitTests.Projects;

// Regression test for GitHub issue #1735: adding a synced project (Glue > Add Synced Project, or a
// project discovered via file-watch sync) only showed up in the "Link Game to FRB Source" dropdown
// after restarting Glue. RefreshLinkToSourceItems only ran off ReactToLoadedGlux/ReactToUnloadedGlux,
// so a synced project added mid-session never triggered a rebuild of the dropdown.
public class FrbSourcePluginSyncedProjectDropdownTests
{
    [StaFact]
    public void LinkToSourceDropdown_ShouldIncludeSyncedProject_AddedAfterGluxAlreadyLoaded()
    {
        GlueTestBootstrap.EnsureHeadlessProjectLoadReady();

        var plugin = new FrbSourcePlugin();
        FlatRedBall.Glue.Plugins.PluginManager.RegisterPluginForTesting(plugin);

        var mainProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out var mainDirectory, "MainProject");
        var syncedProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out var syncedDirectory, "SyncedProject");

        var glueState = FlatRedBall.Glue.Plugins.ExportedImplementations.GlueState.Self;
        var originalMainProject = glueState.CurrentMainProject;
        var originalSyncedProjects = glueState.SyncedProjects.ToList();

        try
        {
            glueState.CurrentMainProject = mainProject;
            glueState.SyncedProjects.Clear();

            // Simulates the real ReactToLoadedGlux dispatch (PluginManager.ReactToLoadedGlux) that
            // happens when a project is loaded/reloaded.
            plugin.ReactToLoadedGlux();

            plugin.LinkToSourceDropDownProjectNames.ShouldBe(new[] { mainProject.Name });

            // Mirrors what ProjectCommands.AddSyncedProject (and ProjectLoader's file-watch sync path)
            // do in production: mutate GlueState.SyncedProjects, then notify plugins via
            // PluginManager.ReactToSyncedProjectLoad - without reloading the whole glux.
            glueState.SyncedProjects.Add(syncedProject);
            FlatRedBall.Glue.Plugins.PluginManager.ReactToSyncedProjectLoad(syncedProject);

            plugin.LinkToSourceDropDownProjectNames.ShouldBe(new[] { mainProject.Name, syncedProject.Name });
        }
        finally
        {
            glueState.CurrentMainProject = originalMainProject;
            glueState.SyncedProjects.Clear();
            glueState.SyncedProjects.AddRange(originalSyncedProjects);

            Directory.Delete(mainDirectory, recursive: true);
            Directory.Delete(syncedDirectory, recursive: true);
        }
    }
}
