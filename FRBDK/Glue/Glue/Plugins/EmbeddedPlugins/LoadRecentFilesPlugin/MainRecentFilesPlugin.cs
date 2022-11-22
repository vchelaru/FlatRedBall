using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.LoadRecentFilesPlugin.ViewModels;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.LoadRecentFilesPlugin.Views;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
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
        GlueSettingsSave GlueSettings => GlueState.Self.GlueSettingsSave;

        public override void StartUp()
        {

            recentFilesMenuItem = this.AddMenuItemTo("Load Recent", HandleLoadRecentClicked, "File", preferredIndex:2);

            //RefreshMenuItems();

            this.ReactToLoadedGlux += HandleGluxLoaded;
        }

        private async void HandleLoadRecentClicked(object sender, EventArgs e)
        {
            var viewModel = new LoadRecentViewModel();
            var recentFiles = GlueState.Self.GlueSettingsSave?.RecentFiles;
            viewModel.AllItems.Clear();
            if (recentFiles != null)
            {
                viewModel.AllItems.AddRange(recentFiles);
            }

            viewModel.RefreshFilteredItems();

            var window = new LoadRecentWindow();

            window.DataContext = viewModel;

            var result = window.ShowDialog();

            if(result == true)
            {
                var fileToLoad = viewModel.SelectedItem.FullPath;

                await GlueCommands.Self.LoadProjectAsync(fileToLoad);
                
            }
        }

        private void HandleGluxLoaded()
        {
            var currentFile = GlueState.Self.CurrentCodeProjectFileName;

            if(ProjectManager.GlueSettingsSave == null)
            {
                ProjectManager.GlueSettingsSave = new SaveClasses.GlueSettingsSave();
            }

            if(GlueSettings.RecentFiles == null)
            {
                GlueSettings.RecentFiles = new List<string>();
            }

            GlueSettings.RecentFiles.RemoveAll(item =>
                string.IsNullOrEmpty(item) ||
                item == currentFile);
            // newest first
            GlueSettings.RecentFiles.Insert(0, currentFile.FullPath);

            // Vic bounces around projects enough that sometimes he needs more...
            // Increase from 30 up now that we have a dedicated window
            const int maxItemCount = 60;

            if (GlueSettings.RecentFiles.Count > maxItemCount)
            {
                GlueSettings.RecentFiles.RemoveAt(GlueSettings.RecentFiles.Count - 1);
            }


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
