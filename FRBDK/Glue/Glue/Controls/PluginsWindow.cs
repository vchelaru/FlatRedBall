using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins;

using System.Reflection;
using FlatRedBall.IO;
using FlatRedBall.Glue.Plugins.Rss;
using System.ComponentModel;
using GlueSaveClasses;
using System.Diagnostics;

using FlatRedBall.Glue.Plugins.EmbeddedPlugins.ManagePlugins.ViewModels;
using FlatRedBall.Glue.Managers;
using Glue;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using System.Threading;
using System.Windows.Navigation;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.Controls
{

    public partial class PluginsWindow : UserControl
    {
        #region Fields

        AllFeed mAllFeed;

        public AllFeed AllFeed
        {
            get
            {
                return mAllFeed;
            }
            set
            {
                mAllFeed = value;
                UpdateViewToFeed();
            }
        }

        DownloadState mPanelState;
        public DownloadState DownloadState
        {
            get
            {
                return mPanelState;
            }
            set
            {
                mPanelState = value;
                UpdateViewToFeed();
            }
        }




        DownloadPluginProgressWindow mDownloadWindow;

        #endregion

        #region Properties
        PluginContainer SelectedPlugin
        {
            get
            {
                return ListBox.SelectedItem as PluginContainer;
            }
        }

        PluginViewModel lastPluginViewModel;
        PluginViewModel SelectedPluginViewModel
        {
            get
            {
                PluginViewModel toReturn = null;

                if(lastPluginViewModel != null)
                {
                    lastPluginViewModel.PropertyChanged -= HandlePluginPropertyChanged;
                }

                if (SelectedPlugin != null)
                {
                    toReturn = new PluginViewModel();

                    toReturn.LoadOnStartup = IsLoadedOnStartup(SelectedPlugin);
                    toReturn.RequiredByProject = IsRequiredByProject(SelectedPlugin);
                    toReturn.LastUpdatedText = GetInfoForContainer(SelectedPlugin);
                    toReturn.GithubRepoAddress = GetGithubAddress(SelectedPlugin);
                   
                    toReturn.PropertyChanged += HandlePluginPropertyChanged;
                    

                    
                    toReturn.BackingData = SelectedPlugin;
                }

                return toReturn;
            }
        }

        RssItem SelectedRssItem
        {
            get;
            set;
        }

        #endregion

        #region Constructor

        public PluginsWindow()
        {
            InitializeComponent();

            RefreshCheckBoxes();

            DownloadState = DownloadState.Downloading;
        }



#endregion

        #region Methods


        public void RefreshCheckBoxes()
        {

            this.ListBox.Items.Clear();

            List<PluginContainer> toAdd = new List<PluginContainer>();

            foreach (PluginContainer pluginContainer in PluginManager.AllPluginContainers)
            {
                if (pluginContainer.Plugin is EmbeddedPlugin == false)
                {
                    toAdd.Add(pluginContainer);
                }
            }

            toAdd.Sort(
                delegate(PluginContainer first, PluginContainer second)
                {
                    return first.Name.CompareTo(second.Name);
                }
            );



            foreach (PluginContainer pluginContainer in toAdd)
            {
                int index = ListBox.Items.Add(pluginContainer);

                ListBox.SetItemChecked(index, pluginContainer.IsEnabled);
            }
        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            PluginContainer container = ListBox.Items[e.Index] as PluginContainer;
            bool isShuttingDown = e.CurrentValue == CheckState.Checked &&
                e.NewValue == CheckState.Unchecked;

            if (isShuttingDown)
            {
                if (PluginManager.ShutDownPlugin(container.Plugin, Plugins.Interfaces.PluginShutDownReason.UserDisabled))
                {
                    e.NewValue = CheckState.Unchecked;
                }

            }
            else
            {
                bool shouldBeEnabled = e.NewValue == CheckState.Checked;

                if (shouldBeEnabled && !container.IsEnabled)
                {
                    DialogResult result = System.Windows.Forms.DialogResult.Yes;

                    if (!string.IsNullOrEmpty(container.FailureDetails))
                    {
                        result = MessageBox.Show("The plugin " + container.Name + " has crashed so " +
                            " it was disabled.  Are you sure you want to re-enable it?",
                            "Re-enable crashed plugin?",
                            MessageBoxButtons.YesNo);
                    }

                    if (result == System.Windows.Forms.DialogResult.Yes)
                    {
                        container.IsEnabled = true;
                        try
                        {
                            container.Plugin.StartUp();
                            PluginManager.ReenablePlugin(container.Plugin);
                        }
                        catch (Exception exception)
                        {
                            container.Fail(exception, "Failed in StartUp");
                            RefreshCheckBoxes();
                        }
                    }
                    else
                    {
                        e.NewValue = CheckState.Unchecked;
                    }
                }
            }
        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            
            PluginContainer container = SelectedPlugin;

            if (container != null)
            {
                PluginView.Visible = true;
                (PluginView.Child as System.Windows.Controls.UserControl).DataContext =
                    SelectedPluginViewModel;
            }
            else
            {
                if (SelectedPluginViewModel != null)
                {
                    this.SelectedPluginViewModel.LastUpdatedText = null;
                }
                PluginView.Visible = false;
            }

            ShowSelectedPluginInformation();
        }

        private static string GetInfoForContainer(PluginContainer container)
        {
            if (container == null)
            {
                return null;
            }
            else if(container.Plugin is NotLoadedPlugin)
            {
                string text = "This plugin is not loaded.\n";

                var asNotLoadedPlugin = container.Plugin as NotLoadedPlugin;

                if(asNotLoadedPlugin.LoadedState == LoadedState.NotLoaded)
                {
                    text += "Plugin is disabled.";
                }
                else
                {
                    text += "Plugin will be loaded next time Glue is started.";
                }

                return text;
            }
            else
            {
                string text = "Version: " + container.Plugin.Version + "\n";

                string fileName = container.AssemblyLocation;
                if (System.IO.File.Exists(fileName))
                {
                    text += "Created: " + System.IO.File.GetCreationTime(fileName).ToShortDateString() + "\n";

                    text += "Location: " + fileName + "\n";
                }

                if (container != null && container.FailureException != null)
                {
                    text += container.FailureException.ToString();

                }
                
                return text;
            }
        }

        private static string GetGithubAddress(PluginContainer container)
        {
            var plugin = container.Plugin;
            if (string.IsNullOrWhiteSpace(plugin.GithubRepoName) || string.IsNullOrWhiteSpace(plugin.GithubRepoOwner))
            {
                return null;
            }

            return $"https://github.com/{plugin.GithubRepoOwner}/{plugin.GithubRepoName}";
        }

        private void UpdateViewToFeed()
        { 
            // The window may have been closed
            if (this.Visible)
            {
                try
                {
                    UpdateGlueViewPanelToState(); // runs on UI thread
                }
                catch(ObjectDisposedException)
                {
                    // do nothing
                }
            }
        }

        private void UpdateGlueViewPanelToState()
        {
            switch (mPanelState)
            {
                case DownloadState.Downloading:
                    LastUpdatedValueLabel.Visible = false;

                    RemoteActionButton2.Visible = false;

                    break;
                case DownloadState.Error:
                    LastUpdatedValueLabel.Visible = false;

                    RemoteActionButton2.Visible = false;

                    break;

                case DownloadState.InformationDownloaded:
                    ShowSelectedPluginInformation();


                    break;
                case DownloadState.NoConnection:
                    LastUpdatedValueLabel.Visible = false;

                    RemoteActionButton2.Visible = false;

                    break;
            }
        }

        private void ShowSelectedPluginInformation()
        {
            object selectedItem = ListBox.SelectedItem;

            if (selectedItem == null)
            {
                LastUpdatedValueLabel.Visible = false;

            }
        }

        private void RemoteActionButton_Click(object sender, EventArgs e)
        {
            if (SelectedPlugin?.DownloadState == DownloadState.InformationDownloaded)
            {
                if (SelectedPlugin != null)
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.UseShellExecute = true;
                    psi.FileName = SelectedPlugin.RemoteLocation;

                    System.Diagnostics.Process.Start(psi);
                }

            }
            else if (SelectedPlugin?.DownloadState == DownloadState.Error)
            {
                SelectedPlugin.ResetDownloadState();

                ShowSelectedPluginInformation();
            }
        }
        
        private void RightSideSplitContainer_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }

        private void HandlePluginPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var propertyName = e.PropertyName;

            if(propertyName == nameof(SelectedPluginViewModel.LoadOnStartup))
            {
                var vm = sender as PluginViewModel;
                RespondToLoadOnStartupChange(vm.BackingData, vm.LoadOnStartup);
            }
            else if(propertyName == nameof(SelectedPluginViewModel.RequiredByProject))
            {
                var vm = sender as PluginViewModel;

                RespondToRequiredByProject(vm.BackingData, vm.RequiredByProject);
            }
        }

        private void RespondToLoadOnStartupChange(PluginContainer pluginContainer, bool shouldBeLoaded)
        {
            string pluginFolder = FileManager.GetDirectory(SelectedPlugin.AssemblyLocation);

            var pluginSettings = GlueState.Self.CurrentPluginSettings;
            bool shouldSave = false;
            if (shouldBeLoaded && !IsLoadedOnStartup(pluginContainer))
            {
                pluginSettings.PluginsToIgnore.Remove(pluginFolder);

                if (pluginContainer.Plugin is NotLoadedPlugin)
                {
                    (pluginContainer.Plugin as NotLoadedPlugin).LoadedState = LoadedState.LoadedNextTime;
                }

                shouldSave = true;
            }
            else if (!shouldBeLoaded && IsLoadedOnStartup(pluginContainer))
            {
                pluginSettings.PluginsToIgnore.Add(pluginFolder);

                if (pluginContainer.Plugin is NotLoadedPlugin)
                {
                    (pluginContainer.Plugin as NotLoadedPlugin).LoadedState = LoadedState.NotLoaded;
                }

                shouldSave = true;
            }
            if (shouldSave)
            {
                GlueState.Self.CurrentPluginSettings.Save(FileManager.GetDirectory(GlueState.Self.GlueProjectFileName.FullPath));
            }
        }


        private void RespondToRequiredByProject(PluginContainer pluginContainer, bool requiredByProject)
        {
            var didChange = GlueCommands.Self.GluxCommands.SetPluginRequirement(
                (PluginBase)pluginContainer.Plugin, requiredByProject);

            if(didChange)
            {
                GlueCommands.Self.GluxCommands.SaveGlux();
            }
        }


        private bool IsLoadedOnStartup(PluginContainer container)
        {
            var plugin = SelectedPlugin.Plugin;

            if (plugin is NotLoadedPlugin)
            {
                var loadedState = (plugin as NotLoadedPlugin).LoadedState;

                if(loadedState == LoadedState.NotLoaded)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                string pluginFolder = FileManager.GetDirectory(SelectedPlugin.AssemblyLocation);

                return GlueState.Self.CurrentPluginSettings.PluginsToIgnore.Contains(pluginFolder) == false;
            }
        }

        private bool IsRequiredByProject(PluginContainer selectedPlugin)
        {
            string nameToSearchFor = selectedPlugin.Name;

            return GlueState.Self.CurrentGlueProject?.PluginData.RequiredPlugins.Any(item => item.Name == nameToSearchFor) == true;
        }





#endregion


        private void RemoteActionButton2_Click(object sender, EventArgs e)
        {
            if (mDownloadWindow == null)
            {
                mDownloadWindow = new DownloadPluginProgressWindow();

                mDownloadWindow.Location = System.Windows.Forms.Cursor.Position;
                
                mDownloadWindow.Show(this);

            }
            try
            {
                if (SelectedRssItem != null && !string.IsNullOrEmpty(SelectedRssItem.DirectLink))
                {

                    try
                    {
                        EditorObjects.IoC.Container.Get<PluginUpdater>().StartDownload(SelectedRssItem.DirectLink, AfterDownload);
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show("Failed to download plugin\n\n" + exc);
                    }
                }
            }
            catch(Exception exception)
            {
                MessageBox.Show("Error downloading:\n\n" + exception.Message);
                if (mDownloadWindow != null)
                {
                    mDownloadWindow.Hide();
                    mDownloadWindow.Dispose();
                    mDownloadWindow = null;
                }
            }
        }

        void AfterDownload()
        {
            if (mDownloadWindow != null)
            {
                mDownloadWindow.Hide();
                mDownloadWindow.Dispose();
                mDownloadWindow = null;
            }

        }

    }
}
