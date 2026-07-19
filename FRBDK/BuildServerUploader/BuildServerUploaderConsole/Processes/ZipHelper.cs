using System.IO;
using System.IO.Compression;

namespace BuildServerUploaderConsole.Processes
{
    public class ZipHelper
    {
        public static void CreateZip(IResults results, string destinationDirectory, string sourceDirectory, string zipFileNameNoExtension)
        {
            string fullZipFileName = sourceDirectory + zipFileNameNoExtension + ".zip";

            if (File.Exists(fullZipFileName))
            {
                File.Delete(fullZipFileName);
            }

            Directory.CreateDirectory(destinationDirectory);
            string destinationZipFileName = destinationDirectory + zipFileNameNoExtension + ".zip";

            if (File.Exists(destinationZipFileName))
            {
                File.Delete(destinationZipFileName);
            }

            results.WriteMessage($" Zipping {sourceDirectory}...");

            // Written directly to destinationDirectory (never sourceDirectory) so the archive
            // being created can't be picked up as one of its own entries mid-write.
            ZipFile.CreateFromDirectory(sourceDirectory, destinationZipFileName, CompressionLevel.Optimal, includeBaseDirectory: false);

            results.WriteMessage($" Finished saving zip file to {destinationZipFileName}");

            File.Copy(destinationZipFileName, fullZipFileName, true);

            results.WriteMessage("Zipped directory " + sourceDirectory + " into " + zipFileNameNoExtension);
        }
    }
}
