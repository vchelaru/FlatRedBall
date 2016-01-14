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

#if GLUE_VIEW
using PluginManager = GlueView.Plugin.PluginManager;
using GlueView.Plugin;
#else
using Glue;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.Rss;
using System.Threading;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
#endif
namespace FlatRedBall.Glue.Controls
{

    public partial class PluginsWindow : Form
    {
        #region Enum

        

        #endregion

        #region Fields

        AllFeed mAllFeed;

        DownloadState mPanelState;

        DownloadPluginProgressWindow mDownloadWindow;

        const string mAllFeedUrl = "http://www.gluevault.com/glueplugins";

        #endregion

        #region Properties
        PluginContainer SelectedPlugin
        {
            get
            {
                return ListBox.SelectedItem as PluginContainer;
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

            SetGlueViewPanelState(DownloadState.Downloading);

            Application.DoEvents();

            StartFeedLoading();
        }



        #endregion

        #region Methods


        private void RefreshCheckBoxes()
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

            toAdd.Sort(delegate(PluginContainer first, PluginContainer second)
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
#if GLUE_VIEW
                        MessageBox.Show("Re-enabling plugins is not supported in GlueView yet.");
#else
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
#endif
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
                string text = GetInfoForContainer(container);

                DetailsTextBox.Text = text;
                LoadOnStartupCheckbox.Visible = true;
                LoadOnStartupCheckbox.Checked = IsLoadedOnStartup(container);
            }
            else
            {
                DetailsTextBox.Text = null;
                LoadOnStartupCheckbox.Visible = false;
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

        private void StartFeedLoading()
        {
            AllFeed.StartDownloadingInformation(mAllFeedUrl, HandleFinishedDownloading);
        }

        private void HandleFinishedDownloading(AllFeed allFeed, DownloadState state)
        {
            mAllFeed = allFeed;
            // The widndow may have been closed
            if (this.Visible)
            {
                try
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        SetGlueViewPanelState(state); // runs on UI thread
                    });
                }
                catch(ObjectDisposedException)
                {
                    // do nothing
                }
            }
        }

        void SetGlueViewPanelState(DownloadState state)
        {
            mPanelState = state;
            
            UpdateGlueViewPanelToState();
        }

        private void UpdateGlueViewPanelToState()
        {
            switch (mPanelState)
            {
                case DownloadState.Downloading:
                    LastUpdatedTitleLabel.Text = "Downloading main feed...";
                    LastUpdatedTitleLabel.Visible = true;
                    LastUpdatedValueLabel.Visible = false;

                    RemoteActionButton.Visible = false;
                    RemoteActionButton2.Visible = false;

                    break;
                case DownloadState.Error:
                    LastUpdatedTitleLabel.Text = "Error connecting to GlueVault.com";
                    LastUpdatedTitleLabel.Visible = true;
                    LastUpdatedValueLabel.Visible = false;

                    RemoteActionButton.Visible = true;
                    RemoteActionButton2.Visible = false;

                    RemoteActionButton.Text = "Try again";

                    break;

                case DownloadState.InformationDownloaded:
                    ShowSelectedPluginInformation();


                    break;
                case DownloadState.NoConnection:
                    LastUpdatedTitleLabel.Text = "No Internet Connection Detected";

                    LastUpdatedTitleLabel.Visible = true;
                    LastUpdatedValueLabel.Visible = false;

                    RemoteActionButton.Visible = true;
                    RemoteActionButton2.Visible = false;

                    RemoteActionButton.Text = "Try again";
                    break;
            }
        }

        private void ShowSelectedPluginInformation()
        {
            object selectedItem = ListBox.SelectedItem;

            if (selectedItem == null)
            {
                LastUpdatedTitleLabel.Text = "Select an item to view update information";
                LastUpdatedTitleLabel.Visible = true;

                LastUpdatedValueLabel.Visible = false;

                RemoteActionButton.Visible = false;
            }
            else if(mAllFeed != null)
            {
                RssItem item = GetItemFor(mAllFeed, SelectedPlugin);

                SelectedRssItem = item;

                if (item != null)
                {
                    
                    string glueLink = item.GlueLink;
                    SelectedPlugin.RemoteLocation = glueLink;

                    if (SelectedPlugin.DownloadState == DownloadState.InformationDownloaded)
                    {
                        LastUpdatedTitleLabel.Text = "Last Update: ";

                        LastUpdatedValueLabel.Visible = true;
                        LastUpdatedValueLabel.Text =  SelectedPlugin.LastUpdate.ToString();

                        RemoteActionButton.Visible = true;
                        RemoteActionButton.Text = "Go to GlueVault";

                        RemoteActionButton2.Visible = true;
                        RemoteActionButton2.Text = "Install Latest Plugin";
                    }
                    else if(SelectedPlugin.DownloadState == DownloadState.NotStarted)
                    {
                        LastUpdatedTitleLabel.Text = "Downloading information for " + item.Title;
                        SelectedPlugin.TryStartDownload((arg)=>
                            {
                                // The window may have been closed already
                                if (this.Visible)
                                {
                                    try
                                    {
                                        this.Invoke((MethodInvoker)delegate
                                        {
                                            ShowSelectedPluginInformation();
                                        });
                                    }
                                    catch
                                    {
                                        // no big deal, it's disposed most likely because the user closed the window.
                                    }
                                }
                            });
                    }
                    else if (SelectedPlugin.DownloadState == DownloadState.Error)
                    {
                        LastUpdatedTitleLabel.Text = "Error getting information about plugin";


                        RemoteActionButton.Visible = true;
                        RemoteActionButton2.Visible = false;

                        RemoteActionButton.Text = "Try again";
                    }


                }
                else
                {
                    LastUpdatedTitleLabel.Text = "Plugin not found on GlueVault.com:";
                    LastUpdatedValueLabel.Visible = false;
                    RemoteActionButton.Visible = false;
                    RemoteActionButton2.Visible = false;

                }
                //LastUpdatedTitleLabel.Text = "Last Updated:";
                //LastUpdatedValueLabel.Text = "12 34 5678";

                //LastUpdatedTitleLabel.Visible = true;
                //LastUpdatedValueLabel.Visible = true;

                //RemoteActionButton.Visible = true;
                //RemoteActionButton.Text = "Refresh";
            }

        }

        private RssItem GetItemFor(AllFeed feed, PluginContainer SelectedPlugin)
        {
            string folder = 
                FileManager.RemovePath(
                 FileManager.GetDirectory(SelectedPlugin.AssemblyLocation));
            folder = folder.Replace("/", "");
            if (!folder.ToLowerInvariant().EndsWith("plugin"))
            {
                folder += "plugin";
            }
            folder = folder.ToLower();

            RssItem itemToReturn = null;
                
            string whatToLookFor = folder + ".plug";

            // We're going to narrow things down a bit here:
            whatToLookFor = ">" + whatToLookFor + @"</a>";
            
            foreach (var item in feed.Items)
            {
                var description = item.Description.ToLower();

                if (description.Contains(whatToLookFor))
                {
                    itemToReturn = item;
                    break;
                }
            }

            return itemToReturn;
        }

        private void RemoteActionButton_Click(object sender, EventArgs e)
        {
            if (SelectedPlugin.DownloadState == DownloadState.InformationDownloaded)
            {
                int m = 3;
                if (SelectedPlugin != null)
                {
                    System.Diagnostics.Process.Start(SelectedPlugin.RemoteLocation);
                }

            }
            else if (SelectedPlugin.DownloadState == DownloadState.Error)
            {
                SelectedPlugin.ResetDownloadState();

                ShowSelectedPluginInformation();
            }
        }
        
        private void RightSideSplitContainer_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }

        private void LoadOnStartupCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            var plugin = SelectedPlugin;

            string pluginFolder = FileManager.GetDirectory(SelectedPlugin.AssemblyLocation);

            bool shouldBeLoaded = true;

#if GLUE

            var pluginSettings = GlueState.Self.CurrentPluginSettings;

            shouldBeLoaded = LoadOnStartupCheckbox.Checked;

            bool shouldSave = false;
            if (shouldBeLoaded && !IsLoadedOnStartup(plugin))
            {
                pluginSettings.PluginsToIgnore.Remove(pluginFolder);

                if(plugin.Plugin is NotLoadedPlugin)
                {
                    (plugin.Plugin as NotLoadedPlugin).LoadedState = LoadedState.LoadedNextTime;
                }

                shouldSave = true;
            }
            else if (!shouldBeLoaded && IsLoadedOnStartup(plugin))
            {
                pluginSettings.PluginsToIgnore.Add(pluginFolder);

                if (plugin.Plugin is NotLoadedPlugin)
                {
                    (plugin.Plugin as NotLoadedPlugin).LoadedState = LoadedState.NotLoaded;
                }

                shouldSave = true;
            }
            if (shouldSave)
            {
                GlueState.Self.CurrentPluginSettings.Save(FileManager.GetDirectory(ProjectManager.GlueProjectFileName));
            }
#endif
        }

        private bool IsLoadedOnStartup(PluginContainer container)
        {
            #if GLUE


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
            #else
            return true;
            #endif
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

                    // I wrote this for Glue, didn't realize it was also being used by GlueView
#if GLUE
                    try
                    {
                        EditorObjects.IoC.Container.Get<PluginUpdater>().StartDownload(SelectedRssItem.DirectLink, AfterDownload);
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show("Failed to download plugin\n\n" + exc);
                    }
#endif
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
