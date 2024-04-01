using System.Collections.Generic;
using System.IO;
using FlatRedBall.IO;
using Ionic.Zip;

namespace BuildServerUploaderConsole.Processes
{
    public class ZipHelper
    {
        public static void CreateZip(IResults results, string destinationDirectory, string sourceDirectory, string zipFileNameNoExtension)
        {
            var containedObjects = new List<string>();

            string fullZipFileName = sourceDirectory + zipFileNameNoExtension + ".zip";

            if (File.Exists(fullZipFileName))
            {
                File.Delete(fullZipFileName);
            }

            var directories = Directory.GetDirectories(sourceDirectory, "*", SearchOption.TopDirectoryOnly);

            containedObjects.AddRange(directories);

            for (int i = 0; i < containedObjects.Count; i++)
            {
                containedObjects[i] = containedObjects[i] + "\\";
            }


            containedObjects.AddRange(Directory.GetFiles(sourceDirectory, "*", SearchOption.TopDirectoryOnly));

            using (var zip = new ZipFile())
            {
                foreach (string containedObject in containedObjects)
                {

                    if (containedObject.EndsWith("\\"))
                    {
                        string relativeDirectory = FileManager.MakeRelative(containedObject, sourceDirectory);
                        zip.AddDirectory(containedObject, relativeDirectory);
                    }
                    else
                    {
                        zip.AddFile(containedObject, "");
                    }

                }
                results.WriteMessage($" Finished adding {containedObjects.Count} files to zip");

                zip.Save(fullZipFileName);

                results.WriteMessage($" Finished saving zip file to {fullZipFileName}");


                Directory.CreateDirectory(destinationDirectory);

                results.WriteMessage($" Starting to copy zip file {sourceDirectory} to {zipFileNameNoExtension}.zip");

                File.Copy(fullZipFileName, destinationDirectory + zipFileNameNoExtension + ".zip", true);


            }

            results.WriteMessage("Zipped directory " + sourceDirectory + " into " + zipFileNameNoExtension);
        }
    }
}
