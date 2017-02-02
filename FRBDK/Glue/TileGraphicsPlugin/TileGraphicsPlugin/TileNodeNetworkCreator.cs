using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.TileGraphics;
using FlatRedBall.Math;

namespace FlatRedBall.AI.Pathfinding
{
    public static class TileNodeNetworkCreator
    {
        public static TileNodeNetwork CreateFrom(LayeredTileMap layeredTileMap, DirectionalType directionalType,
            Func<List<TMXGlueLib.DataTypes.NamedValue>, bool> predicate)
        {
            var numberOfTilesWide =
                MathFunctions.RoundToInt(layeredTileMap.Width / layeredTileMap.WidthPerTile.Value);
            var numberOfTilesTall =
                MathFunctions.RoundToInt(layeredTileMap.Height / layeredTileMap.HeightPerTile.Value);

            var tileWidth = layeredTileMap.WidthPerTile.Value;

            var dimensionHalf = tileWidth / 2.0f;

            TileNodeNetwork nodeNetwork = new TileNodeNetwork(
                0 + dimensionHalf,
                -layeredTileMap.Height + tileWidth / 2.0f,
                tileWidth,
                numberOfTilesWide,
                numberOfTilesTall,
                directionalType);


            var properties = layeredTileMap.Properties;

            foreach (var kvp in properties)
            {
                string name = kvp.Key;
                var namedValues = kvp.Value;

                if (predicate(namedValues))
                {
                    foreach (var layer in layeredTileMap.MapLayers)
                    {
                        var dictionary = layer.NamedTileOrderedIndexes;

                        if (dictionary.ContainsKey(name))
                        {
                            var indexList = dictionary[name];

                            foreach (var index in indexList)
                            {
                                float left;
                                float bottom;
                                layer.GetBottomLeftWorldCoordinateForOrderedTile(index, out left, out bottom);

                                var centerX = left + dimensionHalf;
                                var centerY = bottom + dimensionHalf;

                                nodeNetwork.AddAndLinkTiledNodeWorld(centerX, centerY);
                            }
                        }
                    }
                }
            }

            nodeNetwork.Visible = true;

            return nodeNetwork;
        }
    }
}
