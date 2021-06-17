using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;
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
                    var tms = TiledMapSave.FromFile(fileName.FullPath);


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
