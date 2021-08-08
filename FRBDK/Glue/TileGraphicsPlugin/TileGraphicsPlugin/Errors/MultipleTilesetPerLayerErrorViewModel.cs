using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMXGlueLib;

namespace TiledPluginCore.Errors
{
    class MultipleTilesetPerLayerErrorViewModel : ErrorViewModel
    {
        //ReferencedFileSave referencedFileSave;
        //public ReferencedFileSave ReferencedFileSave 
        //{
        //    get => referencedFileSave; 
        //    set
        //    {
        //        referencedFileSave = value;

        //    }
        //}
        FilePath filePath;
        public FilePath FilePath
        {
            get => filePath;
            set
            {
                filePath = value;
                UpdateUniqueId();
            }
        }

        string layerName;
        public string LayerName
        {
            get => layerName;
            set
            {
                layerName = value;
                UpdateUniqueId();
            }
        }

        private void UpdateUniqueId()
        {
            UniqueId = $"{nameof(MultipleTilesetPerLayerErrorViewModel)} {filePath.FullPath} {layerName}";
        }

        public override void HandleDoubleClick()
        {
            var rfs = GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(filePath.FullPath);
            GlueState.Self.CurrentReferencedFileSave = rfs;
        }

        public override bool ReactsToFileChange(FilePath filePath)
        {
            return filePath == FilePath;
        }

        public static bool HasMultipleTilesets(TiledMapSave tms, MapLayer layer)
        {
            uint? tilesetForLayer = null;
            if (layer.data.Length > 0)
            {
                var data = layer.data[0];
                for (int i = 0; i < data.tiles.Count; i++)
                {
                    var tileset = tms.GetTilesetForGid(data.tiles[i]);
                    if (tileset != null)
                    {
                        if (tilesetForLayer == null)
                        {
                            tilesetForLayer = tileset.Firstgid;
                        }
                        else if (tilesetForLayer != null && tilesetForLayer != tileset.Firstgid)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public override bool GetIfIsFixed()
        {
            // fixed if:
            // 1. File has been removed from the Glue project
            var rfs = GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(FilePath.FullPath);
            if(rfs == null)
            {
                return true;
            }

            // 2. TMX doesn't exist anymore
            if (FilePath.Exists() == false)
            {
                return true;
            }

            // 3. Layer doesn't exist in the TMX
            var tms = TiledMapSave.FromFile(FilePath.FullPath);
            var layer = tms.Layers.FirstOrDefault(item => item.Name == layerName);
            if (layer == null)
            {
                return true;
            }

            // 4. Layer exists, but doen't have anymore duplicates
            if(!HasMultipleTilesets(tms, layer))
            {
                return true;
            }

            return false;

        }
    }
}
