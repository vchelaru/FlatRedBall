using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.IO;

namespace OfficialPlugins.ProjectCopier
{
    [Export(typeof(PluginBase))]
    public class ProjectCopierMainPlugin : PluginBase
    {
        TabControl mTabControl;
        CopyToProjectControl mControl;
        PluginTab mTab;


        CopyManager copyManager;

        public override string FriendlyName
        {
            get { return "Project Copier"; }
        }

        public override Version Version
        {
            get { return new Version(1,1,0); }
        }

        public override void StartUp()
        {
            copyManager = new CopyManager(ShowError);
            //this.AddMenuItemTo("Copy Projects To...", ShowFileWindowForCopy, "Project");
            this.InitializeBottomTabHandler += AddControlToTab;

            this.ReactToLoadedGlux += HandleGluxLoad;
        }

        private void HandleGluxLoad()
        {
            // try to load:
            copyManager.TryLoadOrCreateSettings();

            mControl.DestinationDirectory = copyManager.Settings.DestinationFolder;
            if (!string.IsNullOrEmpty(copyManager.Settings.RelativeSourceFolder))
            {
                mControl.SourceDirectory = copyManager.Settings.EffectiveSourceFolder;
            }
            else
            {
                mControl.SourceDirectory = null;
            }
        }

        void AddControlToTab(TabControl tabControl)
        {
            mControl = new CopyToProjectControl(copyManager);
            mControl.Click += mControl_Click;

            mTab = new PluginTab();
            mTabControl = tabControl;

            mTab.Text = "  Copy Project";
            mTab.Controls.Add(mControl);
            mControl.Dock = DockStyle.Fill;
            mTabControl.Controls.Add(mTab);

        }

        void mControl_Click(object sender, EventArgs e)
        {
            // Eventually we'll want to show the tab
        }

        public static bool ShouldExclude(string fileUnmodified)
        {
            return FileManager.RemovePath(fileUnmodified) == ".svn" ||
                fileUnmodified.Contains("\\bin\\") ||
                fileUnmodified.Contains("\\obj\\") ||
                fileUnmodified.Contains("\\.svn") ||
                fileUnmodified.Contains("/.svn") ||
                // Ignore files from the Mac when doing a "copy back":
                fileUnmodified.EndsWith(".DS_Store")
                ;
        }

        public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason shutDownReason)
        {
            RemoveAllMenuItems();

            if (mTab != null)
            {
                mTabControl.Controls.Remove(mTab);
            }
            mTabControl = null;
            mTab = null;
            mControl = null;

            return true;
        }

        private void ShowError(string error)
        {
            MessageBox.Show(error);
        }
    }
}
