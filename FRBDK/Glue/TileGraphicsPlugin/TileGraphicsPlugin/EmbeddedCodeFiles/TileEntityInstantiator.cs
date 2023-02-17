$GLUE_VERSIONS$

using $PROJECT_NAMESPACE$.DataTypes;
using FlatRedBall.TileGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using $PROJECT_NAMESPACE$.Performance;
using FlatRedBall.Graphics;
using System.Reflection;
using TMXGlueLib.DataTypes;
using System.Collections;
using FlatRedBall.Math.Geometry;

namespace FlatRedBall.TileEntities
{
    public class InstantiationRestrictions
    {
        public AxisAlignedRectangle Bounds = null;
        public List<string> InclusiveList = null;

    }


    public static class TileEntityInstantiator
    {
        public class Settings
        {
            public bool RemoveTileObjectsAfterEntityCreation = true;
        }

        static Type[] typesInThisAssembly;
        static Type[] TypesInThisAssembly
        {
            get
            {
                if (typesInThisAssembly == null)
                {
#if WINDOWS_8 || UWP
                var assembly = typeof(TileEntityInstantiator).GetTypeInfo().Assembly;
                typesInThisAssembly = assembly.DefinedTypes.Select(item=>item.AsType()).ToArray();
#else
                    var assembly = Assembly.GetExecutingAssembly();
                    typesInThisAssembly = assembly.GetTypes();
#endif
                }

                return typesInThisAssembly;
            }
        }
        public static Settings CurrentSettings { get; set; } = new Settings();

        public static Func<string, PositionedObject> CreationFunction;

        /// <summary>
        /// A dictionary that stores all available values for a given type.
        /// </summary>
        /// <remarks>
        /// The structure of this class is a dictionary, where each entry in the dictionary is a list of dictionaries. This mgiht
        /// seem confusing so let's look at an example.
        /// This was originally created to cache values from CSVs. Let's say there's a type called PlatformerValues.
        /// The class PlatformerValues would be the key in the dictionary.
        /// Of course, platformer values can be defined in multiple entities (if multiple entities are platformers).
        /// Each CSV in each entity becomes 1 dictionary, but since there can be multiple CSVs, there is a list. Therefore, the struture
        /// might look like this:
        /// * PlatformerValues
        ///   * List from Entity 1
        ///     * OnGround
        ///     * InAir
        ///   * List from Entity 2
        ///     * OnGround
        ///     * In Air
        ///   // and so on...
        /// </remarks>
        static Dictionary<string, List<IDictionary>> allDictionaries = new Dictionary<string, List<IDictionary>>();

        /// <summary>
        /// Creates entities from a single layer for any tile with the EntityToCreate property.
        /// </summary>
        /// <param name="mapLayer">The layer to create entities from.</param>
        /// <param name="layeredTileMap">The map which contains the mapLayer instance.</param>
        public static void CreateEntitiesFrom(MapDrawableBatch mapLayer, LayeredTileMap layeredTileMap)
        {
            var entitiesToRemove = new List<string>();

            CreateEntitiesFrom(entitiesToRemove, mapLayer, layeredTileMap.TileProperties, layeredTileMap.WidthPerTile ?? 16);

            if (CurrentSettings.RemoveTileObjectsAfterEntityCreation)
            {
                foreach (var entityToRemove in entitiesToRemove)
                {
                    string remove = entityToRemove;
                    mapLayer.RemoveTiles(t => t.Any(item => (item.Name == "EntityToCreate" || item.Name == "Type") && item.Value as string == remove), layeredTileMap.TileProperties);
                }
            }

        }

        public static void CreateEntitiesFrom(LayeredTileMap layeredTileMap, InstantiationRestrictions restrictions = null)
        {
            if (layeredTileMap != null)
            {
                var entitiesToRemove = new List<string>();

                foreach (var layer in layeredTileMap.MapLayers)
                {
                    CreateEntitiesFrom(entitiesToRemove, layer, layeredTileMap.TileProperties, layeredTileMap.WidthPerTile ?? 16, restrictions);
                }
                if (CurrentSettings.RemoveTileObjectsAfterEntityCreation)
                {
                    foreach (var entityToRemove in entitiesToRemove)
                    {
                        string remove = entityToRemove;
                        layeredTileMap.RemoveTiles(t => t.Any(item => (item.Name == "EntityToCreate" || item.Name == "Type") && item.Value as string == remove), layeredTileMap.TileProperties);
                    }
                }

                foreach (var shapeCollection in layeredTileMap.ShapeCollections)
                {
                    CreateEntitiesFromCircles(layeredTileMap, shapeCollection, restrictions);

                    CreateEntitiesFromRectangles(layeredTileMap, shapeCollection, restrictions);

                    CreateEntitiesFromPolygons(layeredTileMap, shapeCollection, restrictions);
                }
            }
        }

        private static void CreateEntitiesFromCircles(LayeredTileMap layeredTileMap, ShapeCollection shapeCollection, InstantiationRestrictions restrictions)
        {
            var circles = shapeCollection.Circles;
            for (int i = circles.Count - 1; i > -1; i--)
            {
                var circle = circles[i];
                if (!string.IsNullOrEmpty(circle.Name) && layeredTileMap.ShapeProperties.ContainsKey(circle.Name))
                {
                    var properties = layeredTileMap.ShapeProperties[circle.Name];
                    var entityAddingProperty = properties.FirstOrDefault(item => item.Name == "EntityToCreate" || item.Name == "Type");

                    var entityType = entityAddingProperty.Value as string;

                    var shouldCreate = !string.IsNullOrEmpty(entityType);
                    if (restrictions?.InclusiveList != null)
                    {
                        shouldCreate = restrictions.InclusiveList.Contains(entityType);
                    }
                    if (shouldCreate)
                    {
                        PositionedObject entity = CreateEntity(entityType);
                        if (entity != null)
                        {
                            entity.Name = circle.Name;
                            ApplyPropertiesTo(entity, properties, circle.Position);

                            if (CurrentSettings.RemoveTileObjectsAfterEntityCreation)
                            {
                                shapeCollection.Circles.Remove(circle);
                            }

                            if (entity is Math.Geometry.ICollidable)
                            {
                                var entityCollision = (entity as Math.Geometry.ICollidable).Collision;
                                entityCollision.Circles.Add(circle);
                                circle.AttachTo(entity, false);
                            }
                        }
                    }
                }
            }
        }

        private static void CreateEntitiesFromRectangles(LayeredTileMap layeredTileMap, ShapeCollection shapeCollection, InstantiationRestrictions restrictions)
        {
            var rectangles = shapeCollection.AxisAlignedRectangles;
            for (int i = rectangles.Count - 1; i > -1; i--)
            {
                var rectangle = rectangles[i];
                if (!string.IsNullOrEmpty(rectangle.Name) && layeredTileMap.ShapeProperties.ContainsKey(rectangle.Name))
                {
                    var properties = layeredTileMap.ShapeProperties[rectangle.Name];
                    var entityAddingProperty = properties.FirstOrDefault(item => item.Name == "EntityToCreate" || item.Name == "Type");

                    var entityType = entityAddingProperty.Value as string;
                    var shouldCreate = !string.IsNullOrEmpty(entityType);
                    if (restrictions?.InclusiveList != null)
                    {
                        shouldCreate = restrictions.InclusiveList.Contains(entityType);
                    }
                    if (shouldCreate)
                    {
                        PositionedObject entity = CreateEntity(entityType);
                        if (entity != null)
                        {
                            entity.Name = rectangle.Name;
                            ApplyPropertiesTo(entity, properties, rectangle.Position);

                            if (CurrentSettings.RemoveTileObjectsAfterEntityCreation)
                            {
                                shapeCollection.AxisAlignedRectangles.Remove(rectangle);
                            }

                            if (entity is Math.Geometry.ICollidable)
                            {
                                var entityCollision = (entity as Math.Geometry.ICollidable).Collision;
                                entityCollision.AxisAlignedRectangles.Add(rectangle);
                                rectangle.AttachTo(entity, false);
                            }
                        }
                    }
                }
            }
        }

        private static void CreateEntitiesFromPolygons(LayeredTileMap layeredTileMap, Math.Geometry.ShapeCollection shapeCollection, InstantiationRestrictions restrictions)
        {
            var polygons = shapeCollection.Polygons;
            for (int i = polygons.Count - 1; i > -1; i--)
            {
                var polygon = polygons[i];
                if (!string.IsNullOrEmpty(polygon.Name) && layeredTileMap.ShapeProperties.ContainsKey(polygon.Name))
                {
                    var properties = layeredTileMap.ShapeProperties[polygon.Name];
                    var entityAddingProperty = properties.FirstOrDefault(item => item.Name == "EntityToCreate" || item.Name == "Type");

                    var entityType = entityAddingProperty.Value as string;
                    var shouldCreate = !string.IsNullOrEmpty(entityType);
                    if (restrictions?.InclusiveList != null)
                    {
                        shouldCreate = restrictions.InclusiveList.Contains(entityType);
                    }
                    if (shouldCreate)
                    {
                        PositionedObject entity = CreateEntity(entityType);
                        if (entity != null)
                        {
                            entity.Name = polygon.Name;
                            ApplyPropertiesTo(entity, properties, polygon.Position);

                            if (CurrentSettings.RemoveTileObjectsAfterEntityCreation)
                            {
                                shapeCollection.Polygons.Remove(polygon);
                            }

                            if (entity is Math.Geometry.ICollidable)
                            {
                                var entityCollision = (entity as Math.Geometry.ICollidable).Collision;
                                entityCollision.Polygons.Add(polygon);
                                polygon.AttachTo(entity, false);
                            }
                        }
                    }
                }
            }
        }

        private static PositionedObject CreateEntity(string entityType)
        {
            PositionedObject entity = null;
            IEntityFactory factory = GetFactory(entityType);
            if (factory != null)
            {
                entity = factory.CreateNew((FlatRedBall.Graphics.Layer)null) as PositionedObject;
            }
            else if (CreationFunction != null)
            {
                entity = CreationFunction(entityType);
            }

            return entity;
        }

        private static void CreateEntitiesFrom(List<string> entitiesToRemove, MapDrawableBatch layer, Dictionary<string, List<NamedValue>> propertiesDictionary,
            float tileSize,
            InstantiationRestrictions restrictions = null)
        {
            var flatRedBallLayer = SpriteManager.Layers.FirstOrDefault(item => item.Batches.Contains(layer));

            var dictionary = layer.NamedTileOrderedIndexes;

            // layer needs its position updated:
            layer.ForceUpdateDependencies();

            foreach (var propertyList in propertiesDictionary.Values)
            {
                var property =
                    propertyList.FirstOrDefault(item2 => item2.Name == "EntityToCreate" || item2.Name == "Type");

                if (!string.IsNullOrEmpty(property.Name))
                {
                    var tileName = propertyList.FirstOrDefault(item => item.Name.ToLowerInvariant() == "name").Value as string;

                    var entityType = property.Value as string;

                    var shouldCreateEntityType =
                        !string.IsNullOrEmpty(entityType) && dictionary.ContainsKey(tileName);

                    if (shouldCreateEntityType && restrictions?.InclusiveList != null)
                    {
                        shouldCreateEntityType = restrictions.InclusiveList.Contains(entityType);
                    }

                    if (shouldCreateEntityType)
                    {
                        IEntityFactory factory = GetFactory(entityType);

                        if (factory == null && CreationFunction == null)
                        {
                            // do nothing?
                        }
                        else
                        {
                            var createdEntityOfThisType = false;

                            var indexList = dictionary[tileName];

                            foreach (var tileIndex in indexList)
                            {
                                var shouldCreate = true;
                                var bounds = restrictions?.Bounds;
                                if (bounds != null)
                                {
                                    layer.GetBottomLeftWorldCoordinateForOrderedTile(tileIndex, out float x, out float y);
                                    x += tileSize / 2.0f;
                                    y += tileSize / 2.0f;
                                    shouldCreate = bounds.IsPointInside(x, y);
                                }

                                if (shouldCreate)
                                {
                                    PositionedObject entity = null;
                                    if (factory != null)
                                    {
                                        entity = factory.CreateNew(flatRedBallLayer) as PositionedObject;
                                    }
                                    else if (CreationFunction != null)
                                    {
                                        entity = CreationFunction(entityType);
                                        // todo - need to support moving to layer
                                    }

                                    if (entity != null)
                                    {
                                        ApplyPropertiesTo(entity, layer, tileIndex, propertyList);
                                        createdEntityOfThisType = true;

#if ITiledTileMetadataInFrb
                                        if(entity is FlatRedBall.Entities.ITiledTileMetadata asEntity) 
                                        {
                                            float tx, ty;                                            
                                            layer.GetTextureCoordiantesForOrderedTile(tileIndex, out tx, out ty);
                                            var ttm = new FlatRedBall.Entities.TiledTileMetadata() 
                                            {
                                                LeftTextureCoordinate = tx,
                                                TopTextureCoordinate = ty,
                                                RightTextureCoordinate = tx + (tileSize / layer.Texture.Width),
                                                BottomTextureCoordinate = ty + (tileSize / layer.Texture.Height)
                                            };
                                            
                                            ttm.RotationZ = layer.GetRotationZForOrderedTile(tileIndex);

                                            asEntity.SetTileMetadata(ttm);
                                        }
#endif
                                    }
                                }
                            }
                            if (createdEntityOfThisType)
                            {
                                entitiesToRemove.Add(entityType);
                            }
                        }
                    }
                }
            }
        }

        private static void ApplyPropertiesTo(PositionedObject entity, MapDrawableBatch layer, int tileIndex, List<NamedValue> propertiesToAssign)
        {
            int vertexIndex = tileIndex * 4;
            var dimension =
                (layer.Vertices[vertexIndex + 1].Position - layer.Vertices[vertexIndex].Position).Length();

            float dimensionHalf = dimension / 2.0f;


            float left;
            float bottom;
            layer.GetBottomLeftWorldCoordinateForOrderedTile(tileIndex, out left, out bottom);
            Microsoft.Xna.Framework.Vector3 position = new Microsoft.Xna.Framework.Vector3(left, bottom, 0);

            var bottomRight = layer.Vertices[tileIndex * 4 + 1].Position;

            float xDifference = bottomRight.X - left;
            float yDifference = bottomRight.Y - bottom;

            if (yDifference != 0 || xDifference < 0)
            {
                float angle = (float)System.Math.Atan2(yDifference, xDifference);

                entity.RotationZ = angle;

            }

            position += entity.RotationMatrix.Right * dimensionHalf;
            position += entity.RotationMatrix.Up * dimensionHalf;

            position += layer.Position;

            ApplyPropertiesTo(entity, propertiesToAssign, position);
        }

        private static void ApplyPropertiesTo(PositionedObject entity, List<NamedValue> propertiesToAssign, Microsoft.Xna.Framework.Vector3 position)
        {
            if (entity != null)
            {
                entity.Position = position;
            }

            var entityType = entity.GetType();
            var lateBinder = Instructions.Reflection.LateBinder.GetInstance(entityType);

            foreach (var property in propertiesToAssign)
            {
                // If name is EntityToCreate, skip it:
                string propertyName = property.Name;

                bool shouldSet = propertyName != "EntityToCreate" &&
                            propertyName != "Type";

                if (shouldSet)
                {
                    if (propertyName == "name")
                    {
                        propertyName = "Name";
                    }

                    var valueToSet = property.Value;

                    var propertyType = property.Type;

                    if (string.IsNullOrEmpty(propertyType))
                    {
                        propertyType = TryGetPropertyType(entityType, propertyName);
                    }

                    valueToSet = ConvertValueAccordingToType(valueToSet, propertyName, propertyType, entityType);
                    try
                    {
                        switch (propertyName)
                        {
                            case "X":
                                if (valueToSet is float)
                                {
                                    entity.X += (float)valueToSet;
                                }
                                else if (valueToSet is int)
                                {
                                    entity.X += (int)valueToSet;
                                }
                                break;
                            case "Y":
                                if (valueToSet is float)
                                {
                                    entity.Y += (float)valueToSet;
                                }
                                else if (valueToSet is int)
                                {
                                    entity.Y += (int)valueToSet;
                                }
                                break;
                            default:
                                lateBinder.SetValue(entity, propertyName, valueToSet);
                                break;
                        }


                    }
                    catch (InvalidCastException e)
                    {
                        string assignedType = valueToSet.GetType().ToString() ?? "unknown type";
                        assignedType = GetFriendlyNameForType(assignedType);

                        string expectedType = "unknown type";
                        object outValue;
                        if (lateBinder.TryGetValue(entity, propertyName, out outValue) && outValue != null)
                        {
                            expectedType = outValue.GetType().ToString();
                            expectedType = GetFriendlyNameForType(expectedType);
                        }

                        // This means that the property exists but is of a different type. 
                        string message = $"Attempted to assign the property {propertyName} " +
                            $"to a value of type {assignedType} but expected {expectedType}. " +
                            $"Check the property type in your TMX and make sure it matches the type on the entity.";
                        throw new Exception(message, e);
                    }
                    catch (Exception)
                    {
                        // Since this code indiscriminately tries to set properties, it may set properties which don't
                        // actually exist. Therefore, we tolerate failures.
                    }
                }
            }
        }

        private static string TryGetPropertyType(Type entityType, string propertyName)
        {
            // todo - cache for perf
            var property = entityType.GetProperty(propertyName);

            if (property != null)
            {
                return property?.PropertyType.FullName;
            }
            else
            {
                var field = entityType.GetField(propertyName);
                return field?.FieldType.FullName;
            }
        }

        public static void RegisterDictionary<T>(Dictionary<string, T> data)
        {
#if DEBUG
            if(data == null)
            {
                throw new ArgumentNullException("The argument data is null - do you need to call LoadStaticContent on the type containing this dictionary?");
            }
#endif

            var type = typeof(T).FullName;

            if (allDictionaries.ContainsKey(type) == false)
            {
                allDictionaries.Add(type, new List<IDictionary>());
            }

            if (allDictionaries[type].Contains(data) == false)
            {
                allDictionaries[type].Add(data);
            }
        }

        private static string GetFriendlyNameForType(string type)
        {
            switch (type)
            {
                case "System.String": return "string";
                case "System.Single": return "float";
                case "System.Decimal": return "decimal";

            }
            return type;
        }

        private static object ConvertValueAccordingToType(object valueToSet, string valueName, string valueType, Type entityType)
        {
            if (valueType == "bool")
            {
                bool boolValue = false;

                if (bool.TryParse((string)valueToSet, out boolValue))
                {
                    valueToSet = boolValue;
                }
            }
            else if (valueType == "float")
            {
                float floatValue;

                if (float.TryParse((string)valueToSet, System.Globalization.NumberStyles.Float, System.Globalization.NumberFormatInfo.InvariantInfo, out floatValue))
                {
                    valueToSet = floatValue;
                }
            }
            else if (valueType == "decimal")
            {
                decimal decimalValue;

                if (decimal.TryParse((string)valueToSet, System.Globalization.NumberStyles.Float, System.Globalization.NumberFormatInfo.InvariantInfo, out decimalValue))
                {
                    valueToSet = decimalValue;
                }
            }
            else if (valueType == "int")
            {
                int intValue;

                if (int.TryParse((string)valueToSet, out intValue))
                {
                    valueToSet = intValue;
                }
            }
            else if (valueName == "CurrentState")
            {
                // Since it's part of the class, it uses the "+" separator
                var enumTypeName = entityType.FullName + "+VariableState";
                var enumType = TypesInThisAssembly.FirstOrDefault(item => item.FullName == enumTypeName);

                valueToSet = Enum.Parse(enumType, (string)valueToSet);
            }
            else if (valueType != null && allDictionaries.ContainsKey(valueType))
            {
                var list = allDictionaries[valueType];

                foreach (var dictionary in list)
                {
                    if (dictionary.Contains(valueToSet))
                    {
                        valueToSet = dictionary[valueToSet];
                        break;
                    }
                }
            }
            // todo - could add support for more file types here like textures, etc...
            else if (valueType == "FlatRedBall.Graphics.Animation.AnimationChainList")
            {
                var method = entityType.GetMethod("GetFile");

                valueToSet = method.Invoke(null, new object[] { valueToSet });
            }
            // If this has a + in it, then that might mean it's a state. We should try to get the type, and if we find it, stuff
            // it in allDictionaries to make future calls faster
            else if (valueType != null && valueType.Contains("+"))
            {
                var stateType = TypesInThisAssembly.FirstOrDefault(item => item.FullName == valueType);

                if (stateType != null)
                {
                    Dictionary<string, object> allValues = new Dictionary<string, object>();

                    var fields = stateType.GetFields(BindingFlags.Static | BindingFlags.Public);

                    foreach (var field in fields)
                    {
                        allValues[field.Name] = field.GetValue(null);
                    }

                    // The list has all the dictioanries that contain values. But for states there is only one set of values, so 
                    // we create a list
                    List<IDictionary> list = new List<IDictionary>();
                    list.Add(allValues);

                    allDictionaries[valueType] = list;

                    if (allValues.ContainsKey((string)valueToSet))
                    {
                        valueToSet = allValues[(string)valueToSet];
                    }
                }

            }
            else if (valueType?.Contains(".") == true)
            {
                var type = typeof(TileEntityInstantiator).Assembly.GetType(valueType);

                if (type != null && type.IsEnum)
                {
                    valueToSet = Enum.Parse(type, (string)valueToSet);
                }
            }
            return valueToSet;
        }


        private static void AssignCustomPropertyTo(PositionedObject entity, NamedValue property)
        {
            throw new NotImplementedException();
        }

        public static IEntityFactory GetFactory(string entityType) => FactoryManager.Get(entityType);
    }

}
