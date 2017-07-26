using System.Collections.Generic;
using System.IO;
using FlatRedBall.IO;
using Ionic.Zip;

namespace BuildServerUploaderConsole.Processes
{
    public class ZipHelper
    {
        public static void CreateZip(IResults results, string destinationDirectory, string directoryWithContentsToZip, string zipFileNameNoExtension)
        {
            var containedObjects = new List<string>();

            string fullZipFileName = directoryWithContentsToZip + "\\" + zipFileNameNoExtension + ".zip";

            if (File.Exists(fullZipFileName))
            {
                File.Delete(fullZipFileName);
            }

            var directories = Directory.GetDirectories(directoryWithContentsToZip, "*", SearchOption.TopDirectoryOnly);

            containedObjects.AddRange(directories);

            for (int i = 0; i < containedObjects.Count; i++)
            {
                containedObjects[i] = containedObjects[i] + "\\";
            }


            containedObjects.AddRange(Directory.GetFiles(directoryWithContentsToZip, "*", SearchOption.TopDirectoryOnly));

            using (var zip = new ZipFile())
            {
                foreach (string containedObject in containedObjects)
                {

                    if (containedObject.EndsWith("\\"))
                    {
                        string relativeDirectory = FileManager.MakeRelative(containedObject, directoryWithContentsToZip);
                        zip.AddDirectory(containedObject, relativeDirectory);
                    }
                    else
                    {
                        zip.AddFile(containedObject, "");
                    }

                }

                zip.Save(fullZipFileName);

                Directory.CreateDirectory(destinationDirectory);

                File.Copy(fullZipFileName, destinationDirectory + zipFileNameNoExtension + ".zip", true);
            }

            results.WriteMessage("Zipped directory " + directoryWithContentsToZip + " into " + zipFileNameNoExtension);
        }
    }
}
