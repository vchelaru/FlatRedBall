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

namespace TiledPlugin.Errors
{
    class MultipleTilesetPerLayerErrorViewModel : FileErrorViewModel
    {
        string layerName;
        public int TileIndex { get; set; }
        public string LayerName
        {
            get => layerName;
            set
            {
                layerName = value;
                UpdateDetails();
            }
        }

        public override void UpdateDetails()
        {
            Details = $"Layer {layerName} in {FilePath} references multiple tilesets which is not allowed. See tile index {TileIndex}";
        }

        
        public static int? GetFirstTileWithDifferentTileset(TiledMapSave tms, MapLayer layer)
        {
            uint? tilesetForLayer = null;
            if (layer.data.Length > 0)
            {
                var data = layer.data[0];
                try
                {
                    var tiles = data.tiles;
                    for (int i = 0; i < tiles.Length; i++)
                    {
                        var tileset = tms.GetTilesetForGid(tiles[i]);
                        if (tileset != null)
                        {
                            if (tilesetForLayer == null)
                            {
                                tilesetForLayer = tileset.Firstgid;
                            }
                            else if (tilesetForLayer != null && tilesetForLayer != tileset.Firstgid)
                            {
                                return i;
                            }
                        }
                    }
                }
                catch
                {
                    // couldn't parse this, it's busted badly, but this is here to report if a file has multiple tilesets, not if the TMX is broken.
                    return null;
                }
            }

            return null;
        }

        public override bool GetIfIsFixed()
        {
            var hasError = GetIfHasError(FilePath, layerName, out int? tileIndex);

            if(hasError)
            {
                if(tileIndex != null)
                {
                    TileIndex = tileIndex.Value;
                }
            }
            else
            {
                TileIndex = -1;
            }
            return !hasError;
        }


        public static bool GetIfHasError(FilePath filePath, string layerName, out int? tileIndex)
        {
            tileIndex = null;

            var rfs = GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(filePath);
            if (rfs == null)
            {
                return false;
            }

            // File doesn't exist anymore
            if (filePath.Exists() == false)
            {
                return false;
            }


            // 3. Layer doesn't exist in the TMX
            var tms = TiledMapSave.FromFile(filePath.FullPath);
            var layer = tms.Layers.FirstOrDefault(item => item.Name == layerName);
            if (layer == null)
            {
                return false;
            }

            // 4. Layer exists, but doen't have anymore duplicates
            var id = GetFirstTileWithDifferentTileset(tms, layer);
            if(id != null)
            {
                tileIndex = id.Value;
                return true;
            }

            return false;

        }


    }
}
