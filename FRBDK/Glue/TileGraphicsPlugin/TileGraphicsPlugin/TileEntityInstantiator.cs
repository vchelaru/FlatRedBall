using $PROJECT_NAMESPACE$.DataTypes;
using FlatRedBall.TileGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using $PROJECT_NAMESPACE$.Performance;
using FlatRedBall.Graphics;
using System.Reflection;
using TMXGlueLib.DataTypes;


namespace FlatRedBall.TileEntities
{
    public static class TileEntityInstantiator
    {
        public static void CreateEntitiesFrom(LayeredTileMap layeredTileMap)
        {
            // prob need to clear out the tileShapeCollection
            var entitiesToRemove = new List<string>();

            foreach (var layer in layeredTileMap.MapLayers)
            {
                CreateEntitiesFrom(entitiesToRemove, layer, layeredTileMap.Properties);
            }
            foreach (var entityToRemove in entitiesToRemove)
            {
                string remove = entityToRemove;
                layeredTileMap.RemoveTiles(t => t.Any(item => item.Name == "EntityToCreate" && item.Value as string == remove), layeredTileMap.Properties);
            }
        }

        private static void CreateEntitiesFrom(List<string> entitiesToRemove, MapDrawableBatch layer, Dictionary<string, List<NamedValue>> propertiesDictionary)
        {
            var flatRedBallLayer = SpriteManager.Layers.FirstOrDefault(item => item.Batches.Contains(layer));

            var dictionary = layer.NamedTileOrderedIndexes;

            // layer needs its position updated:
            layer.ForceUpdateDependencies();

            foreach (var propertyList in propertiesDictionary.Values)
            {
                if (propertyList.Any(item2 => item2.Name == "EntityToCreate"))
                {
                    var tileName = propertyList.FirstOrDefault(item => item.Name.ToLowerInvariant() == "name").Value as string;

                    var entityType = propertyList.FirstOrDefault(item => item.Name == "EntityToCreate").Value as string;

                    if (!string.IsNullOrEmpty(entityType) && dictionary.ContainsKey(tileName))
                    {
                        IEntityFactory factory = GetFactory(entityType);

                        if (factory == null)
                        {
                            string message =
                                $"The factory for entity {entityType} could not be found. To create instances of this entity, " +
                                "set its 'CreatedByOtherEntities' property to true in Glue.";
                            throw new Exception(message);
                        }
                        else
                        {
                            entitiesToRemove.Add(entityType);
                            var indexList = dictionary[tileName];


                            foreach (var tileIndex in indexList)
                            {

                                var entity = factory.CreateNew(flatRedBallLayer) as PositionedObject;

                                ApplyPropertiesTo(entity, layer, tileIndex, propertyList);
                            }

                        }
                    }
                }
            }
        }

        private static void ApplyPropertiesTo(PositionedObject entity, MapDrawableBatch layer, int tileIndex, List<NamedValue> propertiesToAssign)
        {

            int vertexIndex = tileIndex * 4;
            var dimension = layer.Vertices[vertexIndex + 1].Position.X - layer.Vertices[vertexIndex].Position.X;

            float dimensionHalf = dimension / 2.0f;


            float left;
            float bottom;
            layer.GetBottomLeftWorldCoordinateForOrderedTile(tileIndex, out left, out bottom);

            if (entity != null)
            {
                entity.X = left + dimensionHalf;
                entity.Y = bottom + dimensionHalf;
                entity.Z = layer.Z;
            }

            var lateBinder = Instructions.Reflection.LateBinder.GetInstance(entity.GetType());

            foreach (var property in propertiesToAssign)
            {
                try
                {
                    var valueToSet = property.Value;

                    valueToSet = SetValueAccordingToType(valueToSet, property.Name, property.Type, entityType);

                    lateBinder.SetValue(entity, property.Name, valueToSet);
                }
                catch (Exception e)
                {
                    // Since this code indiscriminately tries to set properties, it may set properties which don't
                    // actually exist. Therefore, we tolerate failures.

                }
            }
        }

        private static object SetValueAccordingToType(object valueToSet, string valueName, string valueType, Type entityType)
        {
            if (valueName == "CurrentState")
            {
                // Since it's part of the class, it uses the "+" separator
                var enumTypeName = entityType.FullName + "+VariableState";
                var enumType = typesInThisAssembly.FirstOrDefault(item => item.FullName == enumTypeName);

                valueToSet = Enum.Parse(enumType, (string)valueToSet);
            }
            return valueToSet;
        }

        private static void AssignCustomPropertyTo(PositionedObject entity, NamedValue property)
        {
            throw new NotImplementedException();
        }


        static Type[] typesInThisAssembly;
        private static IEntityFactory GetFactory(string entityType)
        {
            if (typesInThisAssembly == null)
            {
#if WINDOWS_8 || UWP
                var assembly = typeof(TileEntityInstantiator).GetTypeInfo().Assembly;
                typesInThisAssembly = assembly.DefinedTypes.ToArray();
#else
                var assembly = Assembly.GetExecutingAssembly();
                typesInThisAssembly = assembly.GetTypes();
#endif
            }


#if WINDOWS_8 || UWP
            var filteredTypes =
                types.Where(t => t.ImplementedInterfaces.Contains(typeof(IEntityFactory))
                            && t.DeclaredConstructors.Any(c=>c.GetParameters().Count() == 0));
#else
            var filteredTypes =
                typesInThisAssembly.Where(t => t.GetInterfaces().Contains(typeof(IEntityFactory))
                            && t.GetConstructor(Type.EmptyTypes) != null);
#endif

            var factories = filteredTypes
                .Select(
                    t =>
                    {
#if WINDOWS_8 || UWP
                                var propertyInfo = t.DeclaredProperties.First(item => item.Name == "Self");
#else
                        var propertyInfo = t.GetProperty("Self");
#endif
                        var value = propertyInfo.GetValue(null, null);
                        return value as IEntityFactory;
                    }).ToList();


            var factory = factories.FirstOrDefault(item =>
            {
                var type = item.GetType();
                var methodInfo = type.GetMethod("CreateNew", new[] { typeof(Layer) });
                var returntypeString = methodInfo.ReturnType.Name;

                return entityType == returntypeString || entityType.EndsWith("\\" + returntypeString);
            });
            return factory;
        }
    }
}
