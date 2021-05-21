using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Content;
using FlatRedBall.Content.Scene;

namespace TMXGlueLib.DataTypes
{
    public partial class ReducedQuadInfo
    {


        internal static ReducedQuadInfo FromSpriteSave(SpriteSave spriteSave, int textureWidth, int textureHeight)
        {
            ReducedQuadInfo toReturn = new ReducedQuadInfo();
            toReturn.LeftQuadCoordinate = spriteSave.X - spriteSave.ScaleX;
            toReturn.BottomQuadCoordinate = spriteSave.Y - spriteSave.ScaleY;


            bool isRotated = spriteSave.RotationZ != 0;
            if (isRotated)
            {
                toReturn.FlipFlags = (byte)(toReturn.FlipFlags | ReducedQuadInfo.FlippedDiagonallyFlag);
            }

            var leftTextureCoordinate = System.Math.Min(spriteSave.LeftTextureCoordinate, spriteSave.RightTextureCoordinate);
            var topTextureCoordinate = System.Math.Min(spriteSave.TopTextureCoordinate, spriteSave.BottomTextureCoordinate);

            if (spriteSave.LeftTextureCoordinate > spriteSave.RightTextureCoordinate)
            {
                toReturn.FlipFlags = (byte)(toReturn.FlipFlags | ReducedQuadInfo.FlippedHorizontallyFlag);
            }

            if (spriteSave.TopTextureCoordinate > spriteSave.BottomTextureCoordinate)
            {
                toReturn.FlipFlags = (byte)(toReturn.FlipFlags | ReducedQuadInfo.FlippedVerticallyFlag);
            }

            toReturn.LeftTexturePixel = (ushort)FlatRedBall.Math.MathFunctions.RoundToInt(leftTextureCoordinate * textureWidth);
            toReturn.TopTexturePixel = (ushort)FlatRedBall.Math.MathFunctions.RoundToInt(topTextureCoordinate * textureHeight);

            toReturn.Name = spriteSave.Name;

            return toReturn;
        }


    }

    public partial class ReducedTileMapInfo
    {
        public static bool FastCreateFromTmx = true;


        /// <summary>
        /// Converts a TiledMapSave to a ReducedTileMapInfo object
        /// </summary>
        /// <param name="tiledMapSave">The TiledMapSave to convert</param>
        /// <param name="scale">The amount to scale by - default of 1</param>
        /// <param name="zOffset">The zOffset</param>
        /// <param name="directory">The directory of the file associated with the tiledMapSave, used to find file references.</param>
        /// <param name="referenceType">How the files in the .tmx are referenced.</param>
        /// <returns></returns>
        public static ReducedTileMapInfo FromTiledMapSave(TiledMapSave tiledMapSave, float scale, float zOffset, string directory, FileReferenceType referenceType)
        {
            var toReturn = new ReducedTileMapInfo
            {
                NumberCellsTall = tiledMapSave.Height,
                NumberCellsWide = tiledMapSave.Width
            };
            toReturn.CellHeightInPixels = (ushort)tiledMapSave.tileheight;
            toReturn.CellWidthInPixels = (ushort)tiledMapSave.tilewidth;
            toReturn.QuadHeight = tiledMapSave.tileheight;
            toReturn.QuadWidth = tiledMapSave.tilewidth;


            if (FastCreateFromTmx)
            {
                CreateFromTiledMapSave(tiledMapSave, directory, referenceType, toReturn);
            }
            else
            {
                // slow:
                CreateFromSpriteEditorScene(tiledMapSave, scale, zOffset, referenceType, toReturn);
            }

            return toReturn;



        }

        private static void CreateFromTiledMapSave(TiledMapSave tiledMapSave, string tmxDirectory, FileReferenceType referenceType,
            ReducedTileMapInfo reducedTileMapInfo)
        {
            ReducedLayerInfo reducedLayerInfo = null;

            for (int i = 0; i < tiledMapSave.MapLayers.Count; i++)
            {
                string directory = tmxDirectory;

                var tiledLayer = tiledMapSave.MapLayers[i];

                string texture = null;

                uint tileIdOfTexture = 0;
                Tileset tileSet = null;
                uint? firstGid = null;

                if (tiledLayer is MapLayer)
                {
                    var mapLayer = tiledLayer as MapLayer;

                    if (mapLayer.data.Length != 0)
                    {
                        firstGid = mapLayer.data[0].tiles.FirstOrDefault(item => item != 0);
                    }
                }
                else if (tiledLayer is mapObjectgroup)
                {
                    var objectLayer = tiledLayer as mapObjectgroup;

                    //The first element on the list might have null as a gid (it could be a shape)
                    //so we should use ">" instead of "!=" to avoid ignoring the rest of the list
                    var firstObjectWithTexture = objectLayer.@object?.FirstOrDefault(item => item.gid > 0);

                    firstGid = firstObjectWithTexture?.gid;
                }
                else
                {
                    //Image layers and any other future layer types in Tiled are not supported at
                    //this time. Just move onto the next layer and ignore this one.
                    continue;
                }

                if (firstGid > 0)
                {
                    tileSet = tiledMapSave.GetTilesetForGid(firstGid.Value);
                    if (tileSet != null)
                    {

                        if (referenceType == FileReferenceType.NoDirectory)
                        {
                            texture = tileSet.Images[0].sourceFileName;
                        }
                        else if (referenceType == FileReferenceType.Absolute)
                        {
                            if (!string.IsNullOrEmpty(tileSet.SourceDirectory) && tileSet.SourceDirectory != ".")
                            {
                                directory += tileSet.SourceDirectory;

                                directory = FlatRedBall.IO.FileManager.RemoveDotDotSlash(directory);

                            }

                            texture = FlatRedBall.IO.FileManager.RemoveDotDotSlash(directory + tileSet.Images[0].Source);

                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                }

                int tileWidth = FlatRedBall.Math.MathFunctions.RoundToInt(tiledMapSave.tilewidth);
                int tileHeight = FlatRedBall.Math.MathFunctions.RoundToInt(tiledMapSave.tileheight);

                reducedLayerInfo = new ReducedLayerInfo
                {
                    Z = i,
                    Texture = texture,
                    Name = tiledLayer.Name,
                    TileWidth = tileWidth,
                    TileHeight = tileHeight,
                    ParallaxMultiplierX = tiledLayer.ParallaxX,
                    ParallaxMultiplierY = tiledLayer.ParallaxY,
                };

                reducedTileMapInfo.Layers.Add(reducedLayerInfo);

                var tilesetIndex = tiledMapSave.Tilesets.IndexOf(tileSet);
                reducedLayerInfo.TextureId = tilesetIndex;


                // create the quad here:
                if (tiledLayer is MapLayer)
                {
                    AddTileLayerTiles(tiledMapSave, reducedLayerInfo, i, tiledLayer, tileSet, tileWidth, tileHeight);
                }

                else if (tiledLayer is mapObjectgroup)
                {
                    AddObjectLayerTiles(reducedLayerInfo, tiledLayer, tileSet, firstGid, tileWidth, tileHeight);
                }
            }
        }

        static SpriteSave spriteSaveForConversion = new SpriteSave();
        private static void AddTileLayerTiles(TiledMapSave tiledMapSave, ReducedLayerInfo reducedLayerInfo, int i, AbstractMapLayer tiledLayer, Tileset tileSet, int tileWidth, int tileHeight)
        {
            var asMapLayer = tiledLayer as MapLayer;
            var count = asMapLayer.data[0].tiles.Count;
            for (int dataId = 0; dataId < count; dataId++)
            {
                var dataAtIndex = asMapLayer.data[0].tiles[dataId];

                if (dataAtIndex != 0)
                {

                    ReducedQuadInfo quad = new DataTypes.ReducedQuadInfo();

                    float tileCenterX;
                    float tileCenterY;
                    float tileZ;

                    tiledMapSave.CalculateWorldCoordinates(i, dataId, tileWidth, tileHeight, asMapLayer.width,
                        out tileCenterX, out tileCenterY, out tileZ);

                    quad.LeftQuadCoordinate = tileCenterX - tileWidth / 2.0f;
                    quad.BottomQuadCoordinate = tileCenterY - tileHeight / 2.0f;

                    var gid = dataAtIndex;

                    //quad.FlipFlags = (byte)((gid & 0xf0000000) >> 28);

                    var valueWithoutFlip = gid & 0x0fffffff;

                    spriteSaveForConversion.RotationZ = 0;
                    spriteSaveForConversion.FlipHorizontal = false;
                    TiledMapSave.SetSpriteTextureCoordinates(gid, spriteSaveForConversion, tileSet, tiledMapSave.orientation);


                    bool isRotated = spriteSaveForConversion.RotationZ != 0;
                    if (isRotated)
                    {
                        quad.FlipFlags = (byte)(quad.FlipFlags | ReducedQuadInfo.FlippedDiagonallyFlag);
                    }

                    var leftTextureCoordinate = System.Math.Min(spriteSaveForConversion.LeftTextureCoordinate, spriteSaveForConversion.RightTextureCoordinate);
                    var topTextureCoordinate = System.Math.Min(spriteSaveForConversion.TopTextureCoordinate, spriteSaveForConversion.BottomTextureCoordinate);

                    if (spriteSaveForConversion.LeftTextureCoordinate > spriteSaveForConversion.RightTextureCoordinate)
                    {
                        quad.FlipFlags = (byte)(quad.FlipFlags | ReducedQuadInfo.FlippedHorizontallyFlag);
                    }

                    if (spriteSaveForConversion.TopTextureCoordinate > spriteSaveForConversion.BottomTextureCoordinate)
                    {
                        quad.FlipFlags = (byte)(quad.FlipFlags | ReducedQuadInfo.FlippedVerticallyFlag);
                    }

                    quad.LeftTexturePixel = (ushort)FlatRedBall.Math.MathFunctions.RoundToInt(leftTextureCoordinate * tileSet.Images[0].width);
                    quad.TopTexturePixel = (ushort)FlatRedBall.Math.MathFunctions.RoundToInt(topTextureCoordinate * tileSet.Images[0].height);


                    if (tileSet.TileDictionary.ContainsKey(valueWithoutFlip - tileSet.Firstgid))
                    {
                        var dictionary = tileSet.TileDictionary[valueWithoutFlip - tileSet.Firstgid].PropertyDictionary;
                        if (dictionary.ContainsKey("name"))
                        {
                            quad.Name = tileSet.TileDictionary[valueWithoutFlip - tileSet.Firstgid].PropertyDictionary["name"];
                        }
                        else if (dictionary.ContainsKey("Name"))
                        {
                            quad.Name = tileSet.TileDictionary[valueWithoutFlip - tileSet.Firstgid].PropertyDictionary["Name"];
                        }
                    }

                    reducedLayerInfo?.Quads.Add(quad);
                }
            }
        }

        private static void AddObjectLayerTiles(ReducedLayerInfo reducedLayerInfo, AbstractMapLayer tiledLayer, Tileset tileSet, uint? gid, int tileWidth, int tileHeight)
        {
            var asMapLayer = tiledLayer as mapObjectgroup;

            // early out
            if(asMapLayer.@object == null)
            {
                return;
            }

            foreach (var objectInstance in asMapLayer.@object)
            {
                if (objectInstance.gid > 0)
                {
                    ReducedQuadInfo quad = new DataTypes.ReducedQuadInfo();

                    quad.LeftQuadCoordinate = (float)objectInstance.x;
                    quad.BottomQuadCoordinate = (float)-objectInstance.y;

                    quad.OverridingWidth = objectInstance.width;
                    quad.OverridingHeight = objectInstance.height;

                    quad.RotationDegrees = (float)objectInstance.Rotation;

                    quad.FlipFlags = (byte)(gid.Value & 0xf0000000 >> 7);

                    var valueWithoutFlip = gid.Value & 0x0fffffff;

                    int leftPixelCoord;
                    int topPixelCoord;
                    int rightPixelCoord;
                    int bottomPixelCoord;
                    TiledMapSave.GetPixelCoordinatesFromGid(gid.Value, tileSet,
                        out leftPixelCoord, out topPixelCoord, out rightPixelCoord, out bottomPixelCoord);

                    quad.LeftTexturePixel = (ushort)Math.Min(leftPixelCoord, rightPixelCoord);
                    quad.TopTexturePixel = (ushort)Math.Min(topPixelCoord, bottomPixelCoord);

                    quad.Name = objectInstance.Name;
                    if (string.IsNullOrEmpty(quad.Name))
                    {
                        var prop = quad.QuadSpecificProperties.FirstOrDefault(quadProp => quadProp.Name.ToLowerInvariant() == "name");
                        quad.Name = (string)prop.Value;
                    }

                    reducedLayerInfo?.Quads.Add(quad);

                }
            }
        }

        private static void CreateFromSpriteEditorScene(TiledMapSave tiledMapSave, float scale, float zOffset, FileReferenceType referenceType, ReducedTileMapInfo toReturn)
        {
            var ses = tiledMapSave.ToSceneSave(scale, referenceType);

            // This is not a stable sort!
            //ses.SpriteList.Sort((first, second) => first.Z.CompareTo(second.Z));
            ses.SpriteList = ses.SpriteList.OrderBy(item => item.Z).ToList();

            ReducedLayerInfo reducedLayerInfo = null;

            float z = float.NaN;


            int textureWidth = 0;
            int textureHeight = 0;

            AbstractMapLayer currentLayer = null;
            int indexInLayer = 0;


            foreach (var spriteSave in ses.SpriteList)
            {
                if (spriteSave.Z != z)
                {
                    indexInLayer = 0;
                    z = spriteSave.Z;


                    int layerIndex = FlatRedBall.Math.MathFunctions.RoundToInt(z - zOffset);
                    var abstractMapLayer = tiledMapSave.MapLayers[layerIndex];
                    currentLayer = abstractMapLayer;

                    reducedLayerInfo = new ReducedLayerInfo
                    {
                        Z = spriteSave.Z,
                        Texture = spriteSave.Texture,
                        Name = abstractMapLayer.Name,
                        TileWidth = FlatRedBall.Math.MathFunctions.RoundToInt(spriteSave.ScaleX * 2),
                        TileHeight = FlatRedBall.Math.MathFunctions.RoundToInt(spriteSave.ScaleY * 2)
                    };

                    var mapLayer = abstractMapLayer as MapLayer;
                    // This should have data:
                    if (mapLayer != null)
                    {
                        var idOfTexture = mapLayer.data[0].tiles.FirstOrDefault(item => item != 0);
                        Tileset tileSet = tiledMapSave.GetTilesetForGid(idOfTexture);
                        var tilesetIndex = tiledMapSave.Tilesets.IndexOf(tileSet);

                        textureWidth = tileSet.Images[0].width;
                        textureHeight = tileSet.Images[0].height;

                        reducedLayerInfo.TextureId = tilesetIndex;
                        toReturn.Layers.Add(reducedLayerInfo);
                    }


                    var objectGroup = tiledMapSave.MapLayers[layerIndex] as mapObjectgroup;

                    // This code only works based on the assumption that only one tileset will be used in any given object layer's image objects
                    var mapObjectgroupObject = objectGroup?.@object.FirstOrDefault(o => o.gid != null);

                    if (mapObjectgroupObject?.gid != null)
                    {
                        var idOfTexture = mapObjectgroupObject.gid.Value;
                        Tileset tileSet = tiledMapSave.GetTilesetForGid(idOfTexture);
                        var tilesetIndex = tiledMapSave.Tilesets.IndexOf(tileSet);

                        textureWidth = tileSet.Images[0].width;
                        textureHeight = tileSet.Images[0].height;
                        reducedLayerInfo.TextureId = tilesetIndex;
                        toReturn.Layers.Add(reducedLayerInfo);
                    }
                }

                ReducedQuadInfo quad = ReducedQuadInfo.FromSpriteSave(spriteSave, textureWidth, textureHeight);

                if (currentLayer is mapObjectgroup)
                {
                    var asMapObjectGroup = currentLayer as mapObjectgroup;
                    var objectInstance = asMapObjectGroup.@object[indexInLayer];

                    // skip over any non-sprite objects:
                    while (objectInstance.gid == null)
                    {
                        indexInLayer++;
                        if (indexInLayer >= asMapObjectGroup.@object.Length)
                        {
                            objectInstance = null;
                            break;
                        }
                        else
                        {
                            objectInstance = asMapObjectGroup.@object[indexInLayer];
                        }
                    }

                    if (objectInstance != null && objectInstance.properties.Count != 0)
                    {
                        var nameProperty = objectInstance.properties.FirstOrDefault(item => item.StrippedNameLower == "name");
                        if (nameProperty != null)
                        {
                            quad.Name = nameProperty.value;
                        }
                        else
                        {
                            quad.Name = spriteSave.Name;

                            bool needsName = string.IsNullOrEmpty(spriteSave.Name);
                            if (needsName)
                            {
                                quad.Name = $"_{currentLayer.Name}runtime{indexInLayer}";
                            }
                        }

                        List<NamedValue> list = new List<NamedValue>();

                        foreach (var property in objectInstance.properties)
                        {
                            list.Add(
                                new NamedValue
                                {
                                    Name = property.StrippedName,
                                    Value = property.value,
                                    Type = property.Type
                                }
                            );
                        }

                        quad.QuadSpecificProperties = list;
                    }
                }

                reducedLayerInfo?.Quads.Add(quad);

                indexInLayer++;
            }
        }
    }
}
