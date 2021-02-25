using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;
using Windows.UI.Xaml.Controls;

namespace Npc.Managers
{
    public class UnzipManager
    {

        public static async Task<bool> UnzipFile(string zipToUnpack, string unpackDirectory)
        {
            bool succeeded = true;

            succeeded = await Unzip(zipToUnpack, unpackDirectory, succeeded);

            if (succeeded)
            {
                bool isGithubZip = GetIfIsGithubZip(unpackDirectory);

                if(isGithubZip)
                {
                    CopyContentsUpOneDirectory(unpackDirectory);
                }
            }


            return succeeded;
        }

        private static void CopyContentsUpOneDirectory(string unpackDirectory)
        {
            var directories = Directory.GetDirectories(unpackDirectory);

            var directoryToMove = directories.First();

            FileManager.CopyFilesRecursively(directoryToMove, unpackDirectory);

            FileManager.DeleteDirectory(directoryToMove);
        }

        private static bool GetIfIsGithubZip(string unpackDirectory)
        {
            var files = Directory.GetFiles(unpackDirectory);
            var directories = Directory.GetDirectories(unpackDirectory);

            return files.Count() == 0 && directories.Count() == 1;
        }


        static async Task ShowMessageBox(string message)
        {
            var msgbox = new ContentDialog
            {
                Title = "",
                Content = message,
                CloseButtonText = "OK"
            };
            await msgbox.ShowAsync();
        }

        private static async Task<bool> Unzip(string zipToUnpack, string unpackDirectory, bool succeeded)
        {
            try
            {

                using (ZipFile zip1 = ZipFile.Read(zipToUnpack))
                {
                    // here, we extract every entry, but we could extract conditionally
                    // based on entry name, size, date, checkbox status, etc.  
                    foreach (ZipEntry zipEntry in zip1)
                    {
                        zipEntry.Extract(unpackDirectory, ExtractExistingFileAction.OverwriteSilently);
                    }
                }
            }
            catch (Exception e)
            {
                succeeded = false;
                await ShowMessageBox("The local .zip file could not be unzipped.  Deleting this file.  Attempt to create the project again to re-download it." +
                    $"\nAdditional info:\n\n{e.ToString()}");
                File.Delete(zipToUnpack);
            }

            return succeeded;
        }
    }
}
