using FlatRedBall.Glue.Plugins.EmbeddedPlugins.LoadRecentFilesPlugin.ViewModels;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.LoadRecentFilesPlugin.Views;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Forms;
using L = Localization;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.LoadRecentFilesPlugin
{
    [Export(typeof(PluginBase))]
    public class MainRecentFilesPlugin : EmbeddedPlugin
    {
        ToolStripMenuItem recentFilesMenuItem;
        GlueSettingsSave GlueSettings => GlueState.Self.GlueSettingsSave;

        public override void StartUp()
        {

            recentFilesMenuItem = this.AddMenuItemTo(L.Texts.LoadRecent, L.MenuIds.LoadRecentId, null, L.MenuIds.FileId, preferredIndex:2);

            RefreshMenuItems();

            this.ReactToLoadedGlux += HandleGluxLoaded;
        }

        private void RefreshMenuItems()
        {
            var recentFiles = GlueState.Self.GlueSettingsSave?.RecentFileList;

            recentFilesMenuItem.DropDownItems.Clear();

            foreach(var item in recentFiles.Where(item => item.IsFavorite))
            {
                AddToRecentFilesMenuItem(item);
            }

            var nonFavorites = recentFiles.Where(item => !item.IsFavorite).ToArray();

            var hasNonFavorites = nonFavorites.Length > 0;

            if(hasNonFavorites)
            {
                recentFilesMenuItem.DropDownItems.Add("-");

                foreach(var item in nonFavorites.Take(5))
                {
                    AddToRecentFilesMenuItem(item);
                }
            }

            recentFilesMenuItem.DropDownItems.Add(L.Texts.More, null, HandleMoreClicked);

            void AddToRecentFilesMenuItem(RecentFileSave item)
            {
                var name = FileManager.RemovePath(FileManager.RemoveExtension(item.FileName));

                var directory = FileManager.GetDirectory(item.FileName);

                if(System.IO.Directory.Exists(directory))
                {
                    var icoFile = System.IO.Directory.GetFiles(directory, "*.ico").FirstOrDefault();

                    System.Drawing.Icon icon = null;
                    if(!string.IsNullOrEmpty(icoFile ))
                    {
                        // load this into an icon to use in a dropdown item
                        icon = new System.Drawing.Icon(icoFile);
                    }
                    recentFilesMenuItem.DropDownItems.Add(name, icon?.ToBitmap(), (_, _) => GlueCommands.Self.LoadProjectAsync(item.FileName));
                }

            }
        }

        private async void HandleMoreClicked(object sender, EventArgs e)
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
                    vm.RemoveClicked += () => HandleRemovedRecentFile(vm, viewModel);
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

            foreach (var item in viewModel.AllItems)
            {
                item.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(item.IsFavorite))
                    {
                        var isFavorite = item.IsFavorite;

                        var existing = GlueSettings.RecentFileList.FirstOrDefault(candidate => candidate.FileName == item.FullPath);

                        if (existing != null)
                        {
                            existing.IsFavorite = isFavorite;
                        }

                        GlueCommands.Self.GluxCommands.SaveSettings();

                        RefreshMenuItems();

                    }
                };
            }
        }

        private void HandleRemovedRecentFile(RecentItemViewModel vm, LoadRecentViewModel mainViewModel)
        {
            if(GlueState.Self.GlueSettingsSave == null)
            {
                return;
            }

            var fullPath = vm.FullPath;
            var countRemoved = GlueState.Self.GlueSettingsSave.RecentFileList.RemoveAll(item => item.FileName == fullPath);

            mainViewModel.AllItems.Remove(vm);
            mainViewModel.RefreshFilteredItems();

            if (countRemoved > 0)
            {
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
