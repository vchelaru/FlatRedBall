using System;
using TMXGlueLib;
using FlatRedBall.IO;
using System.IO;

namespace TmxToScnx
{
    public class TmxFileCopier
    {
        public static bool CopyTmxTilesetImagesToDestination(string sourceTmx, string destinationScnx, TiledMapSave tms)
        {
            bool success = true;
            //////////Early Out///////////////////
            if (tms.Tilesets == null)
            {
                // Not sure if we should consider this a success or failure
                return success;
            }
            ////////End Early Out////////////////

            var oldDir = FileManager.RelativeDirectory;

            FileManager.RelativeDirectory = FileManager.GetDirectory(sourceTmx);
            string tmxPath = FileManager.RelativeDirectory;
            string destinationPath = FileManager.GetDirectory(destinationScnx);

            foreach (Tileset tileset in tms.Tilesets)
            {
                foreach (TilesetImage image in tileset.Images)
                {
                    bool didCopySucceed = TryCopyTilesetImage(tmxPath, destinationPath, tileset, image);

                    if (!didCopySucceed)
                    {
                        success = false;
                    }
                }
            }

            return success;
        }

        private static bool TryCopyTilesetImage(string tmxPath, string destinationPath, Tileset tileset, TilesetImage image)
        {
            string sourcepath = GetImageSourcePath(tmxPath, tileset, image);

            string whyCantCopy = null;

            if(!System.IO.File.Exists(sourcepath))
            {
                whyCantCopy = "Could not find the file\n" + sourcepath + "\nwhich is referenced by the tileset " + tileset.Name + " in the tmx\n" + tmxPath;
            }

            if (!string.IsNullOrEmpty(whyCantCopy))
            {
                System.Console.Error.WriteLine(whyCantCopy);
            }
            else
            {
                string destinationFullPath = destinationPath + image.sourceFileName;

                bool shouldCopy = 
                    !sourcepath.Equals(destinationFullPath, StringComparison.InvariantCultureIgnoreCase) &&
                    !FileManager.GetDirectory(destinationFullPath).Equals(FileManager.GetDirectory(sourcepath));

                if(shouldCopy)
                {
                    if (File.Exists(destinationFullPath))
                    {
                        // Only copy if source is newer than destination
                        var sourceDate = File.GetLastWriteTimeUtc(sourcepath);
                        var destinationDate = File.GetLastWriteTimeUtc(destinationFullPath);
                        shouldCopy = sourceDate > destinationDate;
                    }
                }

                if (shouldCopy)
                {
                    System.Console.WriteLine("Copying \"{0}\" to \"{1}\".", sourcepath, destinationFullPath);

                    string fileWithoutDotDotSlash = FileManager.RemoveDotDotSlash(sourcepath);

                    const int maxFailures = 5;
                    int currentFailures = 0;

                    while (true)
                    {
                        try
                        {
                            // try it 3 times, in case someone else is copying it at the same time

                            File.Copy(fileWithoutDotDotSlash, destinationFullPath, true);

                            break;
                        }
                        catch (Exception e)
                        {
                            if (currentFailures < maxFailures)
                            {
                                System.Threading.Thread.Sleep(300);
                                // try again:
                                currentFailures++;
                            }
                            else
                            {
                                System.Console.Error.WriteLine("Could not copy \"{0}\" to \"{1}\" \n{2}.", sourcepath, destinationFullPath, e.ToString());
                                break;
                            }
                        }
                    }
                }
            }

            bool succeeded = string.IsNullOrEmpty(whyCantCopy);

            return succeeded;
        }

        private static string GetImageSourcePath(string tmxPath, Tileset tileset, TilesetImage image)
        {
            // The image may be absolute, so only prepend the tmx path if the image is relative
            string sourcepath;

            if (FileManager.IsRelative(image.Source))
            {
                sourcepath = tmxPath + image.Source;
            }
            else
            {
                sourcepath = image.Source;
            }

            sourcepath = FileManager.RemoveDotDotSlash(sourcepath);

            if (tileset.Source != null)
            {
                if (tileset.SourceDirectory != "." && !tileset.SourceDirectory.Contains(":"))
                {
                    sourcepath = tmxPath + tileset.SourceDirectory.Replace("\\", "/") + "/" + image.sourceFileName;
                    sourcepath = FileManager.GetDirectory(sourcepath) + image.sourceFileName;
                }
                else if (tileset.SourceDirectory.Contains(":"))
                {
                    sourcepath = tileset.SourceDirectory + "/" + image.sourceFileName;
                }
                else
                {
                    sourcepath = tmxPath + image.sourceFileName;
                }
            }
            return sourcepath;
        }

        public static void FixupImageSources(TiledMapSave tms)
        {
            System.Console.WriteLine("Fixing up tileset relative paths");
            if (tms.Tilesets != null)
            {
                foreach (Tileset tileset in tms.Tilesets)
                {
                    foreach (TilesetImage image in tileset.Images)
                    {
                        image.Source = image.sourceFileName;
                    }
                }
            }
        }
    }
}
