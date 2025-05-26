using System.IO;
using System.IO.Compression;
using ToolsUtilities;

namespace FRBDKUpdater.Actions
{
    public class CleanAndZipAction
    {
        public static void CleanAndZip(string userAppPath, string directoryToClear, string zipFile, string extractionPath)
        {
            if (!string.IsNullOrEmpty(directoryToClear))
            {
                var directoryInfo = new DirectoryInfo(directoryToClear);
                // Let's clear it:
                FileManager.DeleteDirectory(directoryToClear);
                System.IO.Directory.CreateDirectory(directoryToClear);
            }

            //Extract downloaded zip if it's a .zip file.  
            if (FileManager.GetExtension(zipFile) == "zip" && !string.IsNullOrEmpty(extractionPath))
            {
                Logger.Log("Unzipping file " + zipFile + " to " + extractionPath);

                ZipFile.ExtractToDirectory(zipFile, extractionPath, overwriteFiles: true);

                Logger.Log("Unzip complete");
            }
        }
    }
}
