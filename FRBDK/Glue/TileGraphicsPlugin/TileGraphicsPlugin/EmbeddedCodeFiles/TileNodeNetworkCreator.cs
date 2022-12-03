using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.TileGraphics;
using FlatRedBall.Math;
using TMXGlueLib.DataTypes;
using Microsoft.Xna.Framework;

namespace FlatRedBall.AI.Pathfinding
{
    public static class TileNodeNetworkCreator
    {
        public static TileNodeNetwork CreateFrom(LayeredTileMap layeredTileMap, DirectionalType directionalType,
            Func<List<TMXGlueLib.DataTypes.NamedValue>, bool> predicate)
        {
            TileNodeNetwork nodeNetwork = CreateTileNodeNetwork(layeredTileMap, directionalType);

            FillFromPredicate(nodeNetwork, layeredTileMap, predicate);

            return nodeNetwork;
        }

        public static void FillAllExceptFromPredicate(this TileNodeNetwork nodeNetwork, LayeredTileMap layeredTileMap, Func<List<NamedValue>, bool> predicate, int removalTileRadius = 0)
        {
            var dimensionHalf = layeredTileMap.WidthPerTile.Value / 2.0f;

            var properties = layeredTileMap.TileProperties;

            nodeNetwork.FillCompletely();

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

                                Vector3 positionToRemove = new Vector3();

                                positionToRemove.X = left + dimensionHalf;
                                positionToRemove.Y = bottom + dimensionHalf;

                                nodeNetwork.RemoveAndUnlinkNode(ref positionToRemove);

                                for (int radius = 1; radius <= removalTileRadius; radius++)
                                {
                                    RemoveTilesAtRadius(radius, positionToRemove);
                                }
                            }
                        }
                    }
                }


                void RemoveTilesAtRadius(int radius, Vector3 positionToRemove)
                {
                    var currentPosition = positionToRemove + new Vector3(-radius * layeredTileMap.WidthPerTile.Value, radius * layeredTileMap.WidthPerTile.Value, 0);

                    for (int i = 0; i < radius * 2; i++)
                    {
                        currentPosition.X += layeredTileMap.WidthPerTile.Value;
                        var copy = currentPosition;
                        nodeNetwork.RemoveAndUnlinkNode(ref copy);
                    }

                    for (int i = 0; i < radius * 2; i++)
                    {
                        currentPosition.Y -= layeredTileMap.WidthPerTile.Value;
                        var copy = currentPosition;

                        nodeNetwork.RemoveAndUnlinkNode(ref copy);
                    }

                    for (int i = 0; i < radius * 2; i++)
                    {
                        currentPosition.X -= layeredTileMap.WidthPerTile.Value;
                        var copy = currentPosition;

                        nodeNetwork.RemoveAndUnlinkNode(ref copy);
                    }

                    for (int i = 0; i < radius * 2; i++)
                    {
                        currentPosition.Y += layeredTileMap.WidthPerTile.Value;
                        var copy = currentPosition;

                        nodeNetwork.RemoveAndUnlinkNode(ref copy);
                    }
                }
            }
        }

        public static void FillFromPredicate(this TileNodeNetwork nodeNetwork, LayeredTileMap layeredTileMap, Func<List<NamedValue>, bool> predicate)
        {
            var dimensionHalf = layeredTileMap.WidthPerTile.Value / 2.0f;

            var properties = layeredTileMap.TileProperties;

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
        }

        public static TileNodeNetwork CreateFromTilesWithProperties(LayeredTileMap layeredTileMap, DirectionalType directionalType,
            ICollection<string> types)
        {

            Func<List<TMXGlueLib.DataTypes.NamedValue>, bool> predicate = (list) =>
            {
                var toReturn = false;

                foreach (var namedValue in list)
                {
                    if (types.Contains(namedValue.Name))
                    {
                        toReturn = true;
                        break;
                    }
                }

                return toReturn;
            };
            return CreateFrom(layeredTileMap, directionalType, predicate);
        }

        public static TileNodeNetwork CreateFromNames(LayeredTileMap layeredTileMap, DirectionalType directionalType,
            ICollection<string> names)
        {
            Func<List<TMXGlueLib.DataTypes.NamedValue>, bool> predicate = (list) =>
            {
                var toReturn = false;

                foreach (var namedValue in list)
                {
                    if (namedValue.Name == "Name")
                    {
                        var valueAsString = namedValue.Value as string;

                        if (!string.IsNullOrEmpty(valueAsString) && names.Contains(valueAsString))
                        {
                            toReturn = true;
                            break;
                        }
                    }
                }

                return toReturn;
            };
            return CreateFrom(layeredTileMap, directionalType, predicate);
        }


        public static TileNodeNetwork CreateFromTypes(LayeredTileMap layeredTileMap, DirectionalType directionalType, ICollection<string> types)
        {
            bool CreateFromTypesPredicate(List<NamedValue> list)
            {
                var toReturn = false;

                foreach (var namedValue in list)
                {
                    if (namedValue.Name == "Type")
                    {
                        var valueAsString = namedValue.Value as string;

                        if (!string.IsNullOrEmpty(valueAsString) && types.Contains(valueAsString))
                        {
                            toReturn = true;
                            break;
                        }
                    }
                }

                return toReturn;
            }
            return CreateFrom(layeredTileMap, directionalType, CreateFromTypesPredicate);
        }

        public static TileNodeNetwork CreateFromTilesWithoutTypes(LayeredTileMap layeredTileMap, DirectionalType directionalType, params string[] types) =>
            CreateFromTilesWithoutTypes(layeredTileMap, directionalType, (ICollection<string>)types);

            public static TileNodeNetwork CreateFromTilesWithoutTypes(LayeredTileMap layeredTileMap, DirectionalType directionalType, int removalTileRadius, params string[] types) =>
                CreateFromTilesWithoutTypes(layeredTileMap, directionalType, (ICollection<string>)types, removalTileRadius);

            public static TileNodeNetwork CreateFromTilesWithoutTypes(LayeredTileMap layeredTileMap, DirectionalType directionalType, ICollection<string> types, int removalTileRadius = 0)
            {
            bool CreateFromTypesPredicate(List<NamedValue> list)
            {
                var toReturn = false;

                foreach (var namedValue in list)
                {
                    if (namedValue.Name == "Type")
                    {
                        var valueAsString = namedValue.Value as string;

                        if (!string.IsNullOrEmpty(valueAsString) && types.Contains(valueAsString))
                        {
                            toReturn = true;
                            break;
                        }
                    }
                }

                return toReturn;
            }

            //return CreateFrom(layeredTileMap, directionalType, CreateFromTypesPredicate);

            TileNodeNetwork nodeNetwork = CreateTileNodeNetwork(layeredTileMap, directionalType);

            FillAllExceptFromPredicate(nodeNetwork, layeredTileMap, CreateFromTypesPredicate, removalTileRadius);

            return nodeNetwork;
        }

        public static void FillFromTypes(this TileNodeNetwork tileNodeNetwork, LayeredTileMap layeredTileMap, DirectionalType directionalType, ICollection<string> types)
        {
            bool CreateFromTypesPredicate(List<NamedValue> list)
            {
                var toReturn = false;

                foreach (var namedValue in list)
                {
                    if (namedValue.Name == "Type")
                    {
                        var valueAsString = namedValue.Value as string;

                        if (!string.IsNullOrEmpty(valueAsString) && types.Contains(valueAsString))
                        {
                            toReturn = true;
                            break;
                        }
                    }
                }

                return toReturn;
            }
            tileNodeNetwork.FillFromPredicate(layeredTileMap, CreateFromTypesPredicate);
        }

        public static TileNodeNetwork CreateFromEmptyTiles(MapDrawableBatch mapDrawableBatch, LayeredTileMap layeredTileMap, DirectionalType directionalType)
        {
            TileNodeNetwork toReturn = CreateTileNodeNetwork(layeredTileMap, directionalType);

            toReturn.FillCompletely();

            var offset = new Microsoft.Xna.Framework.Vector3(layeredTileMap.WidthPerTile.Value / 2, layeredTileMap.HeightPerTile.Value / 2, 0);

            for (int i = 0; i < mapDrawableBatch.Vertices.Length; i += 4)
            {
                var position = mapDrawableBatch.Vertices[i].Position + offset;

                var nodeToRemove = toReturn.TiledNodeAtWorld(position.X, position.Y);

                if (nodeToRemove != null)
                {
                    toReturn.Remove(nodeToRemove);
                }
            }

            return toReturn;
        }

        private static TileNodeNetwork CreateTileNodeNetwork(LayeredTileMap layeredTileMap, DirectionalType directionalType)
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

            return nodeNetwork;
        }
    }
}
