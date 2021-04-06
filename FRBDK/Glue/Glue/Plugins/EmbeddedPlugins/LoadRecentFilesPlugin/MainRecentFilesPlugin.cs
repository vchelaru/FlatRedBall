using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.LoadRecentFilesPlugin
{
    [Export(typeof(PluginBase))]
    public class MainRecentFilesPlugin : EmbeddedPlugin
    {
        ToolStripMenuItem recentFilesMenuItem;

        public override void StartUp()
        {

            recentFilesMenuItem = this.AddMenuItemTo("Load Recent", null, "File", preferredIndex:2);

            RefreshMenuItems();

            this.ReactToLoadedGlux += HandleGluxLoaded;
        }

        private void RefreshMenuItems()
        {
            recentFilesMenuItem.DropDownItems.Clear();
            var recentFiles = ProjectManager.GlueSettingsSave.RecentFiles;
            foreach (var file in recentFiles)
            {
                recentFilesMenuItem.DropDownItems.Add(file, null, HandleRecentFileClicked);
            }
        }

        private void HandleGluxLoaded()
        {
            var currentFile = GlueState.Self.CurrentCodeProjectFileName;

            var standardized = FileManager.Standardize(currentFile).ToLowerInvariant();


            if(ProjectManager.GlueSettingsSave == null)
            {
                ProjectManager.GlueSettingsSave = new SaveClasses.GlueSettingsSave();
            }

            if(ProjectManager.GlueSettingsSave.RecentFiles == null)
            {
                ProjectManager.GlueSettingsSave.RecentFiles = new List<string>();
            }

            ProjectManager.GlueSettingsSave.RecentFiles.RemoveAll(item =>
                string.IsNullOrEmpty(item) ||
                FileManager.Standardize(item).ToLowerInvariant() == standardized);
            // newest first
            ProjectManager.GlueSettingsSave.RecentFiles.Insert(0, standardized);

            const int maxItemCount = 20;

            if (ProjectManager.GlueSettingsSave.RecentFiles.Count > maxItemCount)
            {
                ProjectManager.GlueSettingsSave.RecentFiles.RemoveAt(ProjectManager.GlueSettingsSave.RecentFiles.Count - 1);
            }

            RefreshMenuItems();

            GlueCommands.Self.GluxCommands.SaveSettings();
        }

        private async void HandleRecentFileClicked(object sender, EventArgs e)
        {
            var toolStripMenuItem = sender as ToolStripMenuItem;

            var fileToLoad = toolStripMenuItem.Text;

            await GlueCommands.Self.LoadProjectAsync(fileToLoad);
        }
    }
}
