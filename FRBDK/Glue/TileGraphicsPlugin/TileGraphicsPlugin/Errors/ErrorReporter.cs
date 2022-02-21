using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Tiled;
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

    class ErrorReporter : IErrorReporter
    {

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

            var type = typeof(TiledCache);
            var methods = type.GetMethods();
            int m = 3;

            FillWithTmxFilesWithMultipleTilesetsPerLayer(tmxFiles, errors);

            return errors.ToArray();
        }


        private void FillWithTmxFilesWithMultipleTilesetsPerLayer(List<FilePath> tmxFiles, List<ErrorViewModel> errors)
        {

            foreach(var filePath in tmxFiles)
            {
                TiledMapSave tms = GlueState.Self.TiledCache.GetTiledMap(filePath);


                if (tms != null)
                {
                    foreach (var layer in tms.Layers)
                    {
                        if (MultipleTilesetPerLayerErrorViewModel.HasMultipleTilesets(tms, layer))
                        {
                            errors.Add(new MultipleTilesetPerLayerErrorViewModel()
                            {
                                Details = $"The layer {layer.Name} in {filePath.FullPath} references multiple tilesets. This can cause rendering errors",
                                FilePath = filePath,
                                LayerName = layer.Name

                            });

                        }
                    }
                }

            }
        }
    }
}
