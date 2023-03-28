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

            recentFilesMenuItem = this.AddMenuItemTo("Load Recent", null, "File", preferredIndex:2);

            RefreshMenuItems();

            this.ReactToLoadedGlux += HandleGluxLoaded;
        }

        private void RefreshMenuItems()
        {
            var recentFiles = GlueState.Self.GlueSettingsSave?.RecentFileList;

            recentFilesMenuItem.DropDownItems.Clear();

            foreach(var item in recentFiles.Where(item => item.IsFavorite))
            {
                var name = FileManager.RemovePath(FileManager.RemoveExtension(item.FileName));
                recentFilesMenuItem.DropDownItems.Add(name, null, async (_, _) => GlueCommands.Self.LoadProjectAsync(item.FileName));
            }

            recentFilesMenuItem.DropDownItems.Add("More...", null, HandleLoadRecentClicked);
        }

        private async void HandleLoadRecentClicked(object sender, EventArgs e)
        {
            var viewModel = new LoadRecentViewModel();
            var recentFiles = GlueState.Self.GlueSettingsSave?.RecentFileList;
            viewModel.AllItems.Clear();
            if (recentFiles != null)
            {
                foreach(var recentFile in recentFiles)
                {
                    var vm = new RecentItemViewModel()
                    {
                        FullPath = recentFile.FileName,
                        IsFavorite = recentFile.IsFavorite
                    };
                    viewModel.AllItems.Add(vm);

                }
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

            if (recentFiles != null)
            {
                foreach(var item in viewModel.FilteredItems)
                {
                    var matching = recentFiles.FirstOrDefault(candidate => candidate.FileName == item.FullPath);

                    if(matching != null)
                    {
                        matching.IsFavorite = item.IsFavorite;
                    }
                }
                GlueCommands.Self.GluxCommands.SaveSettings();
            }

        }

        private void HandleGluxLoaded()
        {
            var currentFile = GlueState.Self.CurrentCodeProjectFileName;

            if(GlueState.Self.GlueSettingsSave == null)
            {
                // This should probably not be the responsibility of the plugin. It should be handled elsewhere before this hapens.
                GlueState.Self.GlueSettingsSave = new SaveClasses.GlueSettingsSave();
            }

            if(GlueSettings.RecentFileList == null)
            {
                GlueSettings.RecentFileList = new List<RecentFileSave>();
            }

            var existing = GlueSettings.RecentFileList.FirstOrDefault(item => item.FileName == currentFile);

            if(existing != null)
            {
                GlueSettings.RecentFileList.Remove(existing);
                existing.LastTimeAccessed = DateTime.Now;
                GlueSettings.RecentFileList.Insert(0, existing);
            }
            else
            {
                var file = new RecentFileSave
                {
                    FileName = currentFile.FullPath
                };
                file.LastTimeAccessed = DateTime.Now;
                GlueSettings.RecentFileList.Add(file);
            }

            GlueSettings.RecentFileList = GlueSettings.RecentFileList
                // Put favorites up front so we don't remove them from the recent files below
                .OrderBy(item => !item.IsFavorite)
                .ThenByDescending(item =>  item.LastTimeAccessed).ToList();

            // Vic bounces around projects enough that sometimes he needs more...
            // Increase from 30 up now that we have a dedicated window
            // Or why not 60?
            int maxItemCount = 60 + GlueSettings.RecentFileList.Count(item => item.IsFavorite);

            if (GlueSettings.RecentFileList.Count > maxItemCount)
            {
                GlueSettings.RecentFileList.RemoveAt(GlueSettings.RecentFileList.Count - 1);
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
