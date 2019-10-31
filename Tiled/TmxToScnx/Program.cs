using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FlatRedBall.Content.Scene;
using FlatRedBall.IO;
using TMXGlueLib;
using TMXGlueLib.DataTypes;

namespace TmxToScnx
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: tmxtoscnx.exe <input.tmx> <output.scnx or output.tilb> [scale=##.#] [layervisibilitybehavior=Ignore|Skip|Match] [offset=xf,yf,zf] copyimages=true|false");
                return;
            }

            try
            {

                TmxToScnxCommandLineArgs parsedArgs = new TmxToScnxCommandLineArgs();
                parsedArgs.ParseOptionalCommandLineArgs(args);

                if (parsedArgs.IsVerbose)
                {
                    PrintVersionInformation();
                }

                TiledMapSave.LayerVisibleBehaviorValue = parsedArgs.LayerVisibleBehavior;
                TiledMapSave.Offset = parsedArgs.Offset;
                TiledMapSave tms = TiledMapSave.FromFile(parsedArgs.SourceFile);
                bool succeeded = true;

                if (tms.Tilesets.Any(ts => ts.Images == null))
                {
                    Console.Error.WriteLine("This map uses a tileset format that is unsupported. Tilesets should only have one image associated with them. If you created a tileset and added many small images, please pack your tileset together.");
                    succeeded = false;
                }
                else
                {
                    tms.CorrectImageSizes(FileManager.GetDirectory(parsedArgs.SourceFile));
                }


                bool hasMultiTextureLayers = GetIfHasMultiTextureLayers(tms);

                if(hasMultiTextureLayers)
                {
                    const string error = "This map uses multiple textures on one layer which is not supported.";
                    Console.Error.WriteLine(error);
                }

                succeeded = succeeded && !hasMultiTextureLayers;

                if (succeeded)
                {
                    if (parsedArgs.IsVerbose)
                    {
                        Console.WriteLine("{0} converted successfully.", parsedArgs.SourceFile);
                    }

                    if (parsedArgs.CopyImages)
                    {
                        bool didCopySucceed = 
                            TmxFileCopier.CopyTmxTilesetImagesToDestination(parsedArgs.SourceFile, parsedArgs.DestinationFile, tms);

                        if(!didCopySucceed)
                        {
                            // ReSharper disable once RedundantAssignment
                            // Leaving this in case someone puts a test for "succeeded" later
                            succeeded = false;
                        }
                    }

                    // Fix up the image sources to be relative to the newly copied ones.
                    // I don't know why we're doing this, but it wipes out the old relative 
                    // directory structure.  We should not do this...
                    //TmxFileCopier.FixupImageSources(tms);

                    PerformSave(parsedArgs, tms);

                    if (parsedArgs.IsVerbose)
                    {
                        Console.WriteLine("Done.");
                    }
                }
            }
            catch (Exception ex)
            {

                if (ex.InnerException != null && ex.InnerException is System.IO.FileNotFoundException)
                {
                    var exception = ex.InnerException;
                    Console.Error.WriteLine("Error: [" + exception.Message + "] Stack trace: [" + exception.StackTrace + "]");
                }
                else
                {
                    System.Console.WriteLine("Error: [" + ex.Message + "] Stack trace: [" + ex.StackTrace + "]");
                }
            }
        }

        private static void PrintVersionInformation()
        {
            Assembly assembly = Assembly.GetEntryAssembly();
            AssemblyName assemblyName = assembly.GetName();
            Version version = assemblyName.Version;

            Console.WriteLine("TMX to SCNX converter version " + version.ToString());
        }

        private static bool GetIfHasMultiTextureLayers(TiledMapSave tms)
        {
            foreach(var layer in tms.Layers)
            {
                Tileset tileset = null;

                
                foreach(var dataItem in layer.data)
                {
                    foreach(var tile in dataItem.tiles)
                    {
                        var tilesetForThis = tms.GetTilesetForGid(tile);

                        if(tileset == null)
                        {
                            tileset = tilesetForThis;
                        }
                        else if (tilesetForThis != null && tileset != tilesetForThis)
                        {
                            // early out for perf.
                            return true;
                        }
                    }
                }

            }

            return false;

        }

        private static void PerformSave(TmxToScnxCommandLineArgs parsedArgs, TiledMapSave tms)
        {
            string extension = FileManager.GetExtension(parsedArgs.DestinationFile);

            if (extension == "scnx")
            {

                SaveScnx(parsedArgs, tms);
            }
            else if (extension == "tilb")
            {
                SaveTilb(parsedArgs, tms);

            }
            else
            {
                Console.WriteLine("The following extension is not understood: " + extension);
            }
        }

        private static void SaveTilb(TmxToScnxCommandLineArgs parsedArgs, TiledMapSave tms)
        {
            if (parsedArgs.IsVerbose)
            {
                Console.WriteLine("Saving \"{0}\".", parsedArgs.DestinationFile);
            }
            // all files should have been copied over, and since we're using the .scnx files,
            // we are going to use the destination instead of the source.

            FileReferenceType referenceType = FileReferenceType.NoDirectory;

            if (parsedArgs.CopyImages == false)
            {
                referenceType = FileReferenceType.Absolute;
            }

            ReducedTileMapInfo rtmi =
                ReducedTileMapInfo.FromTiledMapSave(tms, parsedArgs.Scale, parsedArgs.Offset.Item3, FileManager.GetDirectory(parsedArgs.DestinationFile), referenceType);

            var directoryToMakeRelativeTo = FileManager.GetDirectory(parsedArgs.SourceFile);

            // If CopyImages is false, then we're going to keep the images where they are and reference them from there.
            if(parsedArgs.CopyImages == false)
            {
                directoryToMakeRelativeTo = FileManager.GetDirectory(parsedArgs.DestinationFile);
            }

            if (parsedArgs.CopyImages == false)
            {
                foreach (var item in rtmi.Layers)
                {
                    item.Texture = FileManager.MakeRelative(item.Texture, directoryToMakeRelativeTo);
                }
            }

            if (File.Exists(parsedArgs.DestinationFile))
            {
                File.Delete(parsedArgs.DestinationFile);
            }

            using (Stream outputStream = File.OpenWrite(parsedArgs.DestinationFile))
            using (BinaryWriter binaryWriter = new BinaryWriter(outputStream))
            {
                rtmi.WriteTo(binaryWriter);
                if (parsedArgs.IsVerbose)
                {
                    Console.WriteLine("Saved \"{0}\".", parsedArgs.DestinationFile);
                }
            }
        }

        private static void SaveScnx(TmxToScnxCommandLineArgs parsedArgs, TiledMapSave tms)
        {
            Console.WriteLine("Saving \"{0}\".", parsedArgs.DestinationFile);

            SceneSave spriteEditorScene = tms.ToSceneSave(parsedArgs.Scale);

            spriteEditorScene.FileName = parsedArgs.DestinationFile;
            spriteEditorScene.ScenePath = FileManager.GetDirectory(parsedArgs.DestinationFile);
            spriteEditorScene.AssetsRelativeToSceneFile = true;
            var result = spriteEditorScene.GetMissingFiles();
            foreach (var missing in result)
            {
                Console.Error.WriteLine("Missing file: " + spriteEditorScene.ScenePath + missing);
            }
            spriteEditorScene.Save(parsedArgs.DestinationFile.Trim());
        }
    }
}
