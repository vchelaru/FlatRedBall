using $PROJECT_NAMESPACE$.DataTypes;
using FlatRedBall.TileGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using $PROJECT_NAMESPACE$.Performance;
using FlatRedBall.Graphics;
using System.Reflection;


namespace FlatRedBall.TileEntities
{
    public static class TileEntityInstantiator
    {
        public static void CreateEntitiesFrom(LayeredTileMap layeredTileMap, Dictionary<string, TileMapInfo> tileMapInfos)
        {
            IEnumerable<TileMapInfo> tileMapInfoEnumerable = tileMapInfos.Values;

            CreateEntitiesFrom(layeredTileMap, tileMapInfoEnumerable);
        }

        public static void CreateEntitiesFrom(LayeredTileMap layeredTileMap, IEnumerable<TileMapInfo> tileMapInfos)
        {
            // prob need to clear out the tileShapeCollection
            var entitiesToRemove = new List<string>();

            foreach (var layer in layeredTileMap.MapLayers)
            {

                CreateEntitiesFrom(entitiesToRemove, layer, tileMapInfos);
            }
            foreach (var entityToRemove in entitiesToRemove)
            {
                string remove = entityToRemove;
                layeredTileMap.RemoveTiles(t => t.EntityToCreate == remove, tileMapInfos);
            }
        }

        private static void CreateEntitiesFrom(List<string> entitiesToRemove, MapDrawableBatch layer, IEnumerable<TileMapInfo> tileMapInfos)
        {
            float dimension = float.NaN;
            float dimensionHalf = 0;

            var dictionary = layer.NamedTileOrderedIndexes;

            foreach (var info in tileMapInfos)
            {
                if (!string.IsNullOrEmpty(info.EntityToCreate) && dictionary.ContainsKey(info.Name))
                {
#if WINDOWS_8
                    var assembly = typeof(TileEntityInstantiator).GetTypeInfo().Assembly;
                    var types = assembly.DefinedTypes;

                    var filteredTypes =
                        types.Where(t => t.ImplementedInterfaces.Contains(typeof(IEntityFactory))
                                    && t.DeclaredConstructors.Any(c=>c.GetParameters().Count() == 0));
#else
                    var assembly = Assembly.GetExecutingAssembly();
                    var types = assembly.GetTypes();
                    var filteredTypes =
                        types.Where(t => t.GetInterfaces().Contains(typeof(IEntityFactory))
                                    && t.GetConstructor(Type.EmptyTypes) != null);
#endif

                    var factories = filteredTypes
                        .Select(
                            t =>
                            {
#if WINDOWS_8
                                var propertyInfo = t.DeclaredProperties.First(item => item.Name == "Self");
#else
                                var propertyInfo = t.GetProperty("Self");
#endif
                                var value = propertyInfo.GetValue(null, null);
                                return value as IEntityFactory;
                            }).ToList();

                    foreach (var factory in factories)
                    {
                        var type = factory.GetType();
                        var methodInfo = type.GetMethod("CreateNew", new[] { typeof(Layer) });
                        var returntypeString = methodInfo.ReturnType.Name;

                        if (info.EntityToCreate.EndsWith("\\" + returntypeString))
                        {
                            entitiesToRemove.Add(info.EntityToCreate);
                            var indexList = dictionary[info.Name];

                            foreach (var tileIndex in indexList)
                            {
                                float left;
                                float bottom;
                                layer.GetBottomLeftWorldCoordinateForOrderedTile(tileIndex, out left, out bottom);

                                if (float.IsNaN(dimension))
                                {
                                    int vertexIndex = tileIndex * 4;
                                    dimension = layer.Vertices[vertexIndex + 1].Position.X - layer.Vertices[vertexIndex].Position.X;
                                    dimensionHalf = dimension / 2.0f;
                                }

                                var entity = factory.CreateNew() as PositionedObject;


                                if (entity != null)
                                {
                                    entity.X = left + dimensionHalf;
                                    entity.Y = bottom + dimensionHalf;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
