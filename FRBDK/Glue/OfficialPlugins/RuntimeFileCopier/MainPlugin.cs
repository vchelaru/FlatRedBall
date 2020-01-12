using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OfficialPlugins.RuntimeFileCopier
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : EmbeddedPlugin
    {
        string[] copiedExtensions = new[]
        {
            "csv",
            "txt",
            "png",
            "tmx",
            "tsx",
            "bmp",
            "png",
            "achx",
            "emix",
            "json"
        };

        ToolStripMenuItem menuItem;

        public bool ShouldCopyFile(string fileName)
        {
            string extension = FileManager.GetExtension(fileName);

            return copiedExtensions.Contains(extension);

        }

        public override void StartUp()
        {
            this.ReactToLoadedGlux += HandleGluxLoaded;
            this.ReactToFileChangeHandler += HandleFileChanged;

            menuItem = base.AddMenuItemTo("Copy changed files to bin folder",
                HandleContentMenuItemClick, "Content");

            menuItem.CheckOnClick = true;
        }

        private void HandleFileChanged(string fileName)
        {
            if (GlueState.Self.GlueSettingsSave.AutoCopyFilesOnChange && ShouldCopyFile(fileName) && System.IO.File.Exists(fileName))
            {
                GlueCommands.Self.ProjectCommands.CopyToBuildFolder(fileName);
            }
        }

        private void HandleGluxLoaded()
        {
            menuItem.Checked = GlueState.Self.GlueSettingsSave.AutoCopyFilesOnChange;
        }

        private void HandleContentMenuItemClick(object sender, EventArgs e)
        {

            GlueState.Self.GlueSettingsSave.AutoCopyFilesOnChange = menuItem.Checked;
            GlueCommands.Self.GluxCommands.SaveSettings();
        }
    }
}
