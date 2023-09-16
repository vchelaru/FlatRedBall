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
            this.AddMenuItemTo(Localization.Texts.ProjectShowCopyEntireTab, Localization.MenuIds.ProjectShowCopyEntireTabId, AddControlToTab, Localization.MenuIds.ProjectId);

            CreateControl();
            this.ReactToLoadedGlux += HandleGluxLoad;
        }

        private void CreateControl()
        {
            mControl = new CopyToProjectControl(copyManager);
            mControl.Click += mControl_Click;
            mControl.Dock = DockStyle.Fill;
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

        void AddControlToTab()
        {
            if(mTab == null)
            {
                mTab = this.CreateTab(mControl, "Copy Project", TabLocation.Bottom);
            }
            mTab.Show();
            mTab.Focus();
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

            mTab?.Hide();
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
