using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;

namespace TileGraphicsPlugin.Managers
{
    public class BuildToolSaver : Singleton<BuildToolSaver>
    {
        /// <summary>
        /// Saves all build tools (like TmxToScnx.exe) to the proper location if either they don't exist or if
        /// the plugin's date is newer than the tools on disk.
        /// </summary>
        /// <remarks>
        /// If running from source then this will always save the tools, which will result in a rebuild of all tmx-sourcing files.
        /// </remarks>
        public void SaveBuildToolsToDisk()
        {

            Assembly assembly = Assembly.GetExecutingAssembly();

            string[] allNames = assembly.GetManifestResourceNames();


            var projectFolder = GlueState.Self.GlueProjectFileName.GetDirectoryContainingThis();
            string destinationFolder = projectFolder.FullPath + "Libraries/Tmx/";

            System.IO.Directory.CreateDirectory(destinationFolder);
            const string prefix = "TileGraphicsPlugin.ToolsAndLibrariesToCopyToProject.";

            const int maxNumberOfTries = 5;



            var thisTime =
                System.IO.File.GetLastWriteTime(
                Assembly.GetAssembly(typeof(MainTiledPluginClass)).Location);

            foreach (string name in allNames.ToArray().Where(item => item.StartsWith(prefix)))
            {
                string nameWithoutPath = name.Substring(prefix.Length);
                string destination = destinationFolder + nameWithoutPath;

                // We should try doing this in a loop.  If it fails, we should just report the failure and continue on
                int numberOfTries = 0;

                bool succeeded = false;

                string errorMessage = null;

                while (succeeded == false && numberOfTries < maxNumberOfTries)
                {
                    try
                    {
                        // only save it if it's out of date:
                        bool isOutOfDate = File.Exists(destination) == false ||
                            File.GetLastWriteTime(destination) < thisTime;

                        if (isOutOfDate)
                        {
                            FlatRedBall.IO.FileManager.SaveEmbeddedResource(assembly, name, destination);
                        }
                        succeeded = true;
                    }
                    catch (Exception exception)
                    {
                        // do nothing
                        errorMessage = exception.Message;
                    }
                    numberOfTries++;
                }

                if (!succeeded)
                {
                    MessageBox.Show("Error trying to save the file at \n" + destination + "\n\n" + errorMessage);
                }
            }
        }

    }
}
