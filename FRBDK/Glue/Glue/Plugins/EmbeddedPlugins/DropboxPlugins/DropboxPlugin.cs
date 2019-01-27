using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.DropboxPlugins
{
    [Export(typeof(PluginBase))]
    public class DropboxPlugin : EmbeddedPlugin
    {
        public override void StartUp()
        {
            this.ReactToLoadedGlux += HandleGluxLoad;
        }

        private void HandleGluxLoad()
        {
            string folder = GlueState.Self.CurrentGlueProjectDirectory;

            // This happened once, not sure why
            if(string.IsNullOrEmpty(folder))
            {
                int m = 3;
            }
            else
            {
                bool isInDropboxFolder = GetIfIsInDropBoxFolder(folder);

                if (isInDropboxFolder)
                {
                    GlueGui.ShowMessageBox("This project appears to be in a Dropbox folder.  " + 
                        "Working with projects while they are in Dropbox folders may cause problems.\n\n" + 
                        "Consider moving the project out of a Dropbox folder or pausing syncing while working.");
                }
            }
        }

        private bool GetIfIsInDropBoxFolder(string folder)
        {
            folder = folder.ToLower(CultureInfo.InvariantCulture);
            folder = folder.Replace("\\", "/");

            return folder.Contains("/dropbox/");
        }
    }
}
