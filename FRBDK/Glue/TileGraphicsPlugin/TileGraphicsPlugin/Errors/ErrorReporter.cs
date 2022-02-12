using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TiledPluginCore.Errors;
using TileGraphicsPlugin;
using TMXGlueLib;

namespace TiledPluginCore.Managers
{
    class CachedTiledMapSave
    {
        public FilePath FilePath;
        public DateTime LastTimeChanged;
        public TiledMapSave TiledMapSave;
    }

    class ErrorReporter : IErrorReporter
    {
        Dictionary<FilePath, CachedTiledMapSave> CachedTiledMapSaves = new Dictionary<FilePath, CachedTiledMapSave>();

        public ErrorViewModel[] GetAllErrors()
        {
            List<ErrorViewModel> errors = new List<ErrorViewModel>();

            List<FilePath> tmxFiles = new List<FilePath>();
            foreach (var rfs in ObjectFinder.Self.GetAllReferencedFiles())
            {
                if (rfs.GetAssetTypeInfo() == AssetTypeInfoAdder.Self.TmxAssetTypeInfo)
                {
                    var fileName = GlueCommands.Self.GetAbsoluteFilePath(rfs);
                    tmxFiles.Add(fileName);
                }
            }


            RefreshCache(tmxFiles);

            FillWithTmxFilesWithMultipleTilesetsPerLayer(tmxFiles, errors);

            return errors.ToArray();
        }

        private void RefreshCache(List<FilePath> tmxFiles)
        {
            ConcurrentDictionary<FilePath, CachedTiledMapSave> dictionary = new System.Collections.Concurrent.ConcurrentDictionary<FilePath, CachedTiledMapSave>();

            foreach(var item in CachedTiledMapSaves)
            {
                dictionary[item.Key] = item.Value;
            }
            Parallel.ForEach(tmxFiles, fileName =>
            {
                if (!CachedTiledMapSaves.ContainsKey(fileName) && fileName.Exists())
                {
                    var tms = TiledMapSave.FromFile(fileName.FullPath);

                    var cachedTiledMapSave = new CachedTiledMapSave
                    {
                        LastTimeChanged = DateTime.Now,
                        FilePath = fileName,
                        TiledMapSave = tms
                    };

                    dictionary[fileName] = cachedTiledMapSave;

                }
            });

            CachedTiledMapSaves.Clear();
            foreach (var kvp in dictionary)
            {
                CachedTiledMapSaves[kvp.Key] = kvp.Value;
            }
        }

        private void FillWithTmxFilesWithMultipleTilesetsPerLayer(List<FilePath> tmxFiles, List<ErrorViewModel> errors)
        {

            foreach(var fileName in tmxFiles)
            {
                TiledMapSave tms = null;
                if(CachedTiledMapSaves.ContainsKey(fileName))
                {
                    var cached = CachedTiledMapSaves[fileName];

                    var date = cached.LastTimeChanged;

                    if(fileName.Exists() && System.IO.File.GetLastWriteTime(fileName.FullPath) <= cached.LastTimeChanged)
                    {
                        tms = cached.TiledMapSave;
                    }
                }

                // unlikely, but possible that the file changed since it was cached so we re-check here.
                if(tms == null)
                {
                    if (fileName.Exists())
                    {
                        tms = TiledMapSave.FromFile(fileName.FullPath);

                        CachedTiledMapSaves[fileName] = new CachedTiledMapSave
                        {
                            LastTimeChanged = DateTime.Now,
                            FilePath = fileName,
                            TiledMapSave = tms
                        };
                    }

                }

                if(tms != null)
                {
                    foreach(var layer in tms.Layers)
                    {
                        if(MultipleTilesetPerLayerErrorViewModel.HasMultipleTilesets(tms, layer))
                        {
                            errors.Add(new MultipleTilesetPerLayerErrorViewModel()
                            {
                                Details = $"The layer {layer.Name} in {fileName.FullPath} references multiple tilesets. This can cause rendering errors",
                                FilePath = fileName,
                                LayerName = layer.Name

                            });

                        }
                    }
                }

            }
        }
    }
}
