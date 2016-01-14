using System.IO;
using FlatRedBall.IO;
using Ionic.Zip;

namespace FRBDKUpdater.Actions
{
    public class CleanAndZipAction
    {
        public static void CleanAndZip(string userAppPath, string directoryToClear, string saveFile, string extractionPath)
        {
            if (!string.IsNullOrEmpty(directoryToClear))
            {
                var directoryInfo = new DirectoryInfo(directoryToClear);
                // Let's clear it:
                directoryInfo.Empty();
            }

            //Extract downloaded zip if it's a .zip file.  
            if (FileManager.GetExtension(saveFile) == "zip" && !string.IsNullOrEmpty(extractionPath))
            {
                Logger.Log("Unzipping file " + saveFile + " to " + extractionPath);
                using (var zip = new ZipFile(saveFile))
                {
                    zip.ExtractAll(extractionPath, true);
                }
                Logger.Log("Unzip complete");
            }
        }
    }
}
