﻿using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Text;
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

            FillWithTmxFilesWithMultipleTilesetsPerLayer(errors);

            return errors.ToArray();
        }

        private void FillWithTmxFilesWithMultipleTilesetsPerLayer(List<ErrorViewModel> errors)
        {

            foreach(var rfs in ObjectFinder.Self.GetAllReferencedFiles())
            {
                if(rfs.GetAssetTypeInfo() == AssetTypeInfoAdder.Self.TmxAssetTypeInfo)
                {
                    var fileName = GlueCommands.Self.GetAbsoluteFilePath(rfs);

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
                                    Details = $"The layer {layer.Name} in {rfs.Name} references multiple tilesets. This can cause rendering errors",
                                    FilePath = GlueCommands.Self.GetAbsoluteFileName(rfs),
                                    LayerName = layer.Name

                                });

                            }
                        }
                    }

                }
            }
        }
    }
}
