using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TmxEditor;
using TMXGlueLib;
using TMXGlueLib.DataTypes;

namespace TileGraphicsPlugin.Managers
{
    public struct TiledMapRfsPair
    {
        public TiledMapSave TiledMapSave;
        public ReferencedFileSave ReferencedFileSave;
    }

    public class FileReferenceManager : FlatRedBall.Glue.Managers.Singleton<FileReferenceManager>
    {
        string GetTsxFileFor(ReferencedFileSave rfs)
        {
            if(rfs != null)
            {
                string candidate = rfs.Name;

                if(FileManager.GetExtension(candidate) != "tmx")
                {
                    candidate = rfs.SourceFile;
                }

                if (FileManager.GetExtension(candidate) == "tmx") 
                {
                    return candidate;
                }
            }

            return null;
        }


        public GeneralResponse HandleGetFilesReferencedBy(FilePath filePath, List<FilePath> listToFill)
        {
            GeneralResponse generalResponse = GeneralResponse.SuccessfulResponse;

            var extension = filePath.Extension;
            if (extension == "tilb")
            {
                // .tilb files are no longer used much, so I don't care about maintaining this by adding a general response
                GetTilbFileReferences(filePath, listToFill);
            }
            else if(extension == "tsx")
            {
                generalResponse = GetTsxFileReferences(filePath, listToFill);
            }
            else if(extension == "tmx")
            {
                generalResponse = GetTmxFileReferences(filePath, listToFill);
            }

            return generalResponse;
        }

        private GeneralResponse GetTsxFileReferences(FilePath tsxFile, List<FilePath> referencedFiles)
        {
            ExternalTileSet external = null;

            GeneralResponse response = GeneralResponse.SuccessfulResponse;

            try
            {
                external = FileManager.XmlDeserialize<ExternalTileSet>(tsxFile.Standardized);
            }
            catch(Exception e)
            {
                // This is now properly handled by the error window, so let's not clutter the output window
                //PluginManager.ReceiveError(e.ToString());
                response = new GeneralResponse
                {
                    Message = $"Failed to parse {tsxFile}:\n{e.ToString()}",
                    Succeeded = false
                };
            }

            var directory = FileManager.GetDirectory(tsxFile.Standardized);

            if (external?.Images != null && external.Images.Length != 0)
            {
                var image = external.Images[0];

                string fileName = directory + image.Source;

                referencedFiles.Add(fileName);
            }

            return response;
        }

        private GeneralResponse GetTmxFileReferences(FilePath fileName, List<FilePath> listToFill)
        {
            TiledMapSave tms = null;
            GeneralResponse response = GeneralResponse.SuccessfulResponse;

            var oldLoadValuesFromSource = Tileset.ShouldLoadValuesFromSource;
            Tileset.ShouldLoadValuesFromSource = false;

            try
            {
                tms = TiledMapSave.FromFile(fileName.Standardized);
            }
            catch (Exception e)
            {
                // This is now properly handled by the error window, so let's not clutter the output window
                //PluginManager.ReceiveError($"Error loading file {fileName}\n{e.ToString()}");
                response = new GeneralResponse
                {
                    Message = $"Failed to load {fileName}:\n{e.ToString()}",
                    Succeeded = false
                };
            }

            Tileset.ShouldLoadValuesFromSource = oldLoadValuesFromSource;


            if (tms != null)
            {
                var referencedFiles = tms.GetReferencedFiles();

                string directory = FileManager.GetDirectory(fileName.Standardized);
                for (int i = 0; i < referencedFiles.Count; i++)
                {
                    referencedFiles[i] = directory + referencedFiles[i];
                }

                listToFill.AddRange(referencedFiles.Select(item =>new FilePath(item)));
            }

            return response;
        }

        private static void GetTilbFileReferences(FilePath fileName, List<FilePath> listToFill)
        {
            try
            {
                ReducedTileMapInfo rtmi = ReducedTileMapInfo.FromFile(fileName.Standardized);

                var referencedFiles = rtmi.GetReferencedFiles();

                string directory = FileManager.GetDirectory(fileName.Standardized);
                for (int i = 0; i < referencedFiles.Count; i++)
                {
                    referencedFiles[i] = directory + referencedFiles[i];
                }

                listToFill.AddRange(referencedFiles.Select(item =>new FilePath(item)));
            }
            catch (EndOfStreamException e)
            {
                PluginManager.ReceiveError("Error trying to read TMX: " + fileName + "\nEnd of file reached unexpectedly");
            }
            catch (Exception e)
            {
                PluginManager.ReceiveError("Error trying to read TMX:\n\n" + fileName + "\n\n" + e.ToString());
            }
        }

        public bool IsTmx(ReferencedFileSave rfs)
        {
            string tsxFile = GetTsxFileFor(rfs);

            return tsxFile != null;
        }

        public void UpdateAvailableTsxFiles()
        {
            /////////// Early Out //////////////

            if (GlueState.Self.CurrentElement == null)
            {
                return;
            }

            //////////End Early Out ////////////

            AppState.Self.ProvidedContext.AvailableTsxFiles.Clear();

            List<string> foundFiles = new List<string>();

            var tmxFiles =
                GlueState.Self.CurrentElement.ReferencedFiles
                .Where(item => IsTmx(item))
                .ToList();

            foreach (var rfs in tmxFiles)
            {
                string fullFile = FlatRedBall.Glue.ProjectManager.MakeAbsolute(GetTsxFileFor(rfs), true);


                if (System.IO.File.Exists(fullFile))
                {
                    TiledMapSave tms = null;
                    string tmxDirectory = FileManager.GetDirectory(fullFile);

                    bool succeeded = true;

                    try
                    {
                        tms = TiledMapSave.FromFile(fullFile);
                    }
                    catch (Exception e)
                    {
                        var exception = e;

                        if(e.InnerException != null && e.InnerException is FileNotFoundException)
                        {
                            exception = e.InnerException;
                        }

                        PluginManager.ReceiveError("Error trying to load " + GetTsxFileFor(rfs) + ":\n" + exception.ToString());
                        succeeded = false;

                    }

                    if (succeeded)
                    {
                        foreach (var tileset in tms.Tilesets.Where(item => !string.IsNullOrEmpty(item.Source)))
                        {
                            string absoluteSource = tmxDirectory + tileset.Source;

                            absoluteSource = FileManager.RemoveDotDotSlash(absoluteSource);

                            foundFiles.Add(absoluteSource);
                        }
                    }
                }
            }



            AppState.Self.ProvidedContext.AvailableTsxFiles.AddRange(foundFiles.Distinct(StringComparer.InvariantCultureIgnoreCase));


        }

        public IEnumerable<TiledMapRfsPair> GetAllTiledMapSaves()
        {
            foreach (var rfs in GlueState.Self.CurrentGlueProject.GetAllReferencedFiles().Where(item => IsTmx(item)))
            {
                TiledMapSave tms = null;
                string fullFile = FlatRedBall.Glue.ProjectManager.MakeAbsolute(GetTsxFileFor(rfs), true);
                bool succeeded = true;

                string tmxDirectory = FileManager.GetDirectory(fullFile);

                try
                {
                    tms = TiledMapSave.FromFile(fullFile);
                }
                catch (Exception e)
                {
                // This is now properly handled by the error window, so let's not clutter the output window
                    //PluginManager.ReceiveError("Error trying to load " + GetTsxFileFor( rfs)+ ":\n" + e.ToString());
                    succeeded = false;
                }
                if(succeeded)
                {
                    yield return new TiledMapRfsPair { TiledMapSave = tms, ReferencedFileSave = rfs };
                }
            }
        }

        public IEnumerable<ReferencedFileSave> GetReferencedFileSavesReferencingTsx(string tsxFileName)
        {
            foreach (var pair in GetAllTiledMapSaves())
            {
                bool foundAny = false;
                foreach (var tileset in pair.TiledMapSave.Tilesets.Where(item => !string.IsNullOrEmpty(item.Source)))
                {
                    string tmxDirectory = FileManager.GetDirectory(pair.TiledMapSave.FileName);
                    string absoluteSource = tmxDirectory + tileset.Source;

                    absoluteSource = FileManager.RemoveDotDotSlash(absoluteSource);

                    if(absoluteSource == tsxFileName)
                    {
                        foundAny = true;
                        break;
                    }
                }
                if(foundAny)
                {
                    yield return pair.ReferencedFileSave;
                }
            }
        }

        public IEnumerable<ReferencedFileSave> GetReferencedFileSavesReferencingPng(string pngFileName)
        {
            foreach (var pair in GetAllTiledMapSaves())
            {
                bool foundAny = false;
                foreach (var tileset in pair.TiledMapSave.Tilesets)
                {
                    string relativeDirectoryForImage = FileManager.GetDirectory(pair.TiledMapSave.FileName);
                    if (!string.IsNullOrEmpty(tileset.Source))
                    {
                        string absoluteSource = relativeDirectoryForImage + tileset.Source;

                        absoluteSource = FileManager.RemoveDotDotSlash(absoluteSource);

                        relativeDirectoryForImage = FileManager.GetDirectory(absoluteSource);
                    }

                    foreach(var image in tileset.Images)
                    {
                        string absoluteImage = relativeDirectoryForImage + image.Source;
                        absoluteImage = FileManager.Standardize(FileManager.RemoveDotDotSlash(absoluteImage)).ToLowerInvariant();

                        if(pngFileName.ToLowerInvariant() == absoluteImage)
                        {
                            foundAny = true;
                            break;
                        }

                    }
                    if(foundAny)
                    {
                        break;
                    }
                }
                if (foundAny)
                {
                    yield return pair.ReferencedFileSave;
                }
            }
        }

    }
}
