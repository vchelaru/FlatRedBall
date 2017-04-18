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

            var leftTextureCoordinate = System.Math.Min( spriteSave.LeftTextureCoordinate, spriteSave.RightTextureCoordinate);
            var topTextureCoordinate = System.Math.Min(spriteSave.TopTextureCoordinate, spriteSave.BottomTextureCoordinate);

            if (spriteSave.LeftTextureCoordinate > spriteSave.RightTextureCoordinate)
            {
                toReturn.FlipFlags = (byte)(toReturn.FlipFlags | ReducedQuadInfo.FlippedHorizontallyFlag);
            }
            
            if(spriteSave.TopTextureCoordinate > spriteSave.BottomTextureCoordinate)
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


            var ses = tiledMapSave.ToSceneSave(scale, referenceType);

            // This is not a stable sort!
            //ses.SpriteList.Sort((first, second) => first.Z.CompareTo(second.Z));
            ses.SpriteList = ses.SpriteList.OrderBy(item => item.Z).ToList();

            ReducedLayerInfo reducedLayerInfo = null;

            // If we rely on the image, it's both slow (have to open the images), and
            // doesn't work at runtime in games:
            //Dictionary<string, Point> loadedTextures = new Dictionary<string, Point>();
            //SetCellWidthAndHeight(tiledMapSave, directory, toReturn, ses, loadedTextures);

            toReturn.CellHeightInPixels = (ushort)tiledMapSave.tileheight;
            toReturn.CellWidthInPixels = (ushort)tiledMapSave.tilewidth;

            // We used to set the quad width/height based on the sprite size, 
            // but if there is an object layer that doesn't match the tile size,
            // then the quad width/height is reported incorrectly. Changing this to
            // use the tileheight and tilewidth values.
            //if (ses.SpriteList.Count != 0)
            //{
            //    SpriteSave spriteSave = ses.SpriteList[0];

            //    toReturn.QuadWidth = spriteSave.ScaleX * 2;
            //    toReturn.QuadHeight = spriteSave.ScaleY * 2;
            //}
            toReturn.QuadHeight = tiledMapSave.tileheight;
            toReturn.QuadWidth = tiledMapSave.tilewidth;

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
            return toReturn;



        }


    }
}
