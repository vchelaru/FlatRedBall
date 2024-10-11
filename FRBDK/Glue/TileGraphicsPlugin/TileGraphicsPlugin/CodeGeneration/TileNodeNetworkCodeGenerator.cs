using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TiledPlugin.ViewModels;
using TileGraphicsPlugin;
using TileGraphicsPlugin.CodeGeneration;

namespace TiledPlugin.CodeGeneration
{
    class TileNodeNetworkCodeGenerator : ElementComponentCodeGenerator
    {
        public override ICodeBlock GenerateInitializeLate(ICodeBlock codeBlock, IElement element)
        {
            NamedObjectSave[] tileNodeNetworksCollections = GetAllTileNodeNetworkNamedObjectsInElement(element);

            foreach (var tileShapeCollection in tileNodeNetworksCollections)
            {
                GenerateCodeFor(tileShapeCollection, codeBlock);
            }

            return codeBlock;
        }

        static NamedObjectSave[] GetAllTileNodeNetworkNamedObjectsInElement(IElement element)
        {
            return element
                .AllNamedObjects
                .Where(item => item.GetAssetTypeInfo() == AssetTypeInfoAdder.Self.TileNodeNetworkAssetTypeInfo)
                .ToArray();
        }

        public override ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, IElement element)
        {
            NamedObjectSave[] tileNodeNetworks = GetAllTileNodeNetworkNamedObjectsInElement(element);



            foreach (var nodeNetwork in tileNodeNetworks)
            {
                T Get<T>(string name)
                {
                    return nodeNetwork.Properties.GetValue<T>(name);
                }

                var creationOptions = Get<TileNodeNetworkCreationOptions>(
                    nameof(TileNodeNetworkPropertiesViewModel.NetworkCreationOptions));

                var variable = nodeNetwork.GetCustomVariable("Visible");

                bool filledInHere = 
                    creationOptions == TileNodeNetworkCreationOptions.FillCompletely ||
                    creationOptions == TileNodeNetworkCreationOptions.Empty;

                if (variable != null && variable.Value is bool && ((bool)variable.Value) == true &&
                    filledInHere)
                {
                    codeBlock.Line($"{nodeNetwork.FieldName}.Visible = true;");
                    //codeBlock.Line($"{nodeNetwork.FieldName}.AddToLayer(null);");
                    // todo - make it visible here!
                    //throw new NotImplementedException();
                }

            }

            return codeBlock;
        }

        public static string GenerateConstructorFor(NamedObjectSave namedObjectSave)
        {
            T Get<T>(string name)
            {
                return namedObjectSave.Properties.GetValue<T>(name);
            }
            string FloatString(float value) => value.ToString(CultureInfo.InvariantCulture) + "f";

            var creationOptions = Get<TileNodeNetworkCreationOptions>(
                nameof(TileNodeNetworkPropertiesViewModel.NetworkCreationOptions));

            bool comesFromLayer = creationOptions == TileNodeNetworkCreationOptions.FromLayer;

            var instanceName = namedObjectSave.FieldName;
            var mapName = Get<string>(nameof(TileNodeNetworkPropertiesViewModel.SourceTmxName));
            var fourOrEight = Get<FlatRedBall.AI.Pathfinding.DirectionalType>(nameof(TileNodeNetworkPropertiesViewModel.DirectionalType));

            string toReturn = null;

            switch(creationOptions)
            {
                case TileNodeNetworkCreationOptions.Empty:
                    // assume this for now:
                    {
                        var tileSize = 16;
                        var numberOfXTiles = 32;
                        var numberOfYTiles = 32;

                        var tileSizeString = FloatString(tileSize);

                        var leftFill = tileSize/2.0f;
                        var leftFillString = FloatString(leftFill);



                        var topFill = 0 - tileSize / 2.0f;
                        // heightFill - 1, because if it's just 1 cell high, we use the value as-is. Then each additional moves the bottom left by 1
                        var bottomFillString = FloatString(topFill - (numberOfYTiles - 1) * tileSize);
                        toReturn = $"{instanceName} = new FlatRedBall.AI.Pathfinding.TileNodeNetwork({leftFillString}, {bottomFillString}, {tileSizeString}, {numberOfXTiles}, {numberOfYTiles}, FlatRedBall.AI.Pathfinding.DirectionalType.{fourOrEight});";
                    }

                    break;
                case TileNodeNetworkCreationOptions.FillCompletely:
                    {
                        var tileSize = Get<float>(nameof(TileNodeNetworkPropertiesViewModel.NodeNetworkTileSize));
                        var tileSizeString = FloatString(tileSize);

                        var leftFill = Get<float>(nameof(TileNodeNetworkPropertiesViewModel.NodeNetworkFillLeft));
                        var leftFillString = FloatString(leftFill);


                        var widthFill = Get<int>(nameof(TileNodeNetworkPropertiesViewModel.NodeNetworkFillWidth));
                        var heightFill = Get<int>(nameof(TileNodeNetworkPropertiesViewModel.NodeNetworkFillHeight));

                        var topFill = Get<float>(nameof(TileNodeNetworkPropertiesViewModel.NodeNetworkFillTop));

                        // heightFill - 1, because if it's just 1 cell high, we use the value as-is. Then each additional moves the bottom left by 1
                        var bottomFillString = FloatString(topFill - (heightFill-1) * tileSize);

                        toReturn = $"{instanceName} = new FlatRedBall.AI.Pathfinding.TileNodeNetwork({leftFillString}, {bottomFillString}, {tileSizeString}, {widthFill}, {heightFill}, FlatRedBall.AI.Pathfinding.DirectionalType.{fourOrEight});";
                    }
                    break;
                case TileNodeNetworkCreationOptions.FromType:
                    var typeName = Get<string>(nameof(TileNodeNetworkPropertiesViewModel.NodeNetworkTileTypeName));

                    if (!string.IsNullOrEmpty(typeName))
                    {
                        toReturn = $"{instanceName} = FlatRedBall.AI.Pathfinding.TileNodeNetworkCreator.CreateFromTypes({mapName}, FlatRedBall.AI.Pathfinding.DirectionalType.{fourOrEight}, new string[]{{ \"{typeName}\" }});";
                    }
                    else
                    {
                        toReturn = $"{instanceName} = new new FlatRedBall.AI.Pathfinding.TileNodeNetwork();";
                    }
                    break;
                case TileNodeNetworkCreationOptions.FromProperties:
                    var propertyName = Get<string>(nameof(TileNodeNetworkPropertiesViewModel.NodeNetworkPropertyName));

                    if (!string.IsNullOrEmpty(propertyName))
                    {
                        toReturn = $"{instanceName} = FlatRedBall.AI.Pathfinding.TileNodeNetworkCreator.CreateFromTilesWithProperties({mapName}, FlatRedBall.AI.Pathfinding.DirectionalType.{fourOrEight}, new string[]{{ \"{propertyName}\" }});";
                    }
                    else
                    {
                        toReturn = $"{instanceName} = new new FlatRedBall.AI.Pathfinding.TileNodeNetwork();";
                    }
                    break;
                case TileNodeNetworkCreationOptions.FromLayer:
                    toReturn = GenerateFromLayerConstructor(namedObjectSave);
                    break;
                //break;
                default:
                    toReturn = $"throw new System.NotImplementedException(\"Unknown TileNodeNetwork Creation Type {creationOptions}\");";
                    break;
            }

            var eliminateCutCorners = Get<bool>(nameof(TileNodeNetworkPropertiesViewModel.EliminateCutCorners));
            if(eliminateCutCorners)
            {
                toReturn += $"{instanceName}.EliminateCutCorners();";
            }

            return toReturn;
        }

        private static string GenerateFromLayerConstructor(NamedObjectSave namedObjectSave)
        {
            T Get<T>(string name)
            {
                return namedObjectSave.Properties.GetValue<T>(name);
            }

            var layerName = Get<string>(nameof(TileNodeNetworkPropertiesViewModel.NodeNetworkLayerName));
            var typeNameInLayer = Get<string>(nameof(TileNodeNetworkPropertiesViewModel.NodeNetworkLayerTileType));
            var layerOption = Get<TileNodeNetworkFromLayerOptions>(nameof(TileNodeNetworkPropertiesViewModel.TileNodeNetworkFromLayerOptions));
            var instanceName = namedObjectSave.FieldName;
            var mapName = Get<string>(nameof(TileNodeNetworkPropertiesViewModel.SourceTmxName));
            var fourOrEight = Get<FlatRedBall.AI.Pathfinding.DirectionalType>(nameof(TileNodeNetworkPropertiesViewModel.DirectionalType));

            switch (layerOption)
            {
                case TileNodeNetworkFromLayerOptions.AllEmpty:
                    return $"{instanceName} = FlatRedBall.AI.Pathfinding.TileNodeNetworkCreator.CreateFromEmptyTiles({mapName}.MapLayers.FindByName(\"{layerName}\"), {mapName}, FlatRedBall.AI.Pathfinding.DirectionalType.{fourOrEight});";
                case TileNodeNetworkFromLayerOptions.FromType:
                    var effectiveName = layerName;
                    if (!string.IsNullOrEmpty(typeNameInLayer))
                    {
                        effectiveName += "_" + typeNameInLayer;
                    }

                    // assign itself if there's nothing in the 

                    //return $"{instanceName} = {mapName}.Collisions.FirstOrDefault(item => item.Name == \"{effectiveName}\")" +
                    //    $" ?? new FlatRedBall.TileCollisions.TileShapeCollection();";
                    return $"return new System.NotImplementedException();";
            }

            return $"return new System.NotImplementedException();";


        }

        private void GenerateCodeFor(NamedObjectSave namedObjectSave, ICodeBlock codeBlock)
        {
            if (namedObjectSave.DefinedByBase == false)
            {
                // don't generate if it's defined by base, the base class defines the properties
                // for now. Maybe eventually derived classes can override or change the behavior?
                // Not sure, that definitely does not seem like standard behavior, so maybe we leave
                // it to codegen.

                var sourceName = namedObjectSave.SourceNameWithoutParenthesis;

                T Get<T>(string name)
                {
                    return namedObjectSave.Properties.GetValue<T>(name);
                }

                var creationOptions = Get<TileNodeNetworkCreationOptions>(
                    nameof(TileNodeNetworkPropertiesViewModel.NetworkCreationOptions));

                var sourceType = namedObjectSave.SourceType;

                if (sourceType == SourceType.File)
                {

                    codeBlock.Line($"//The NamedObject {namedObjectSave} has a SourceType of {sourceType}, so it may not instantiate " +
                        $"properly. If you are experiencing a NullReferenceException, consider changing the" +
                        $"SourceType to {SourceType.FlatRedBallType}");
                }

                var isVisible = namedObjectSave.GetCustomVariable("Visible")?.Value is bool asBool && asBool == true;

                if (!isVisible)
                {
                    //codeBlock.Line("// normally we wait to set variables until after the object is created, but in this case if the");
                    //codeBlock.Line("// TileNodeNetwork doesn't have its Visible set before creating the tiles, it can result in");
                    //codeBlock.Line("// really bad performance issues, as shapes will be made visible, then invisible. Really bad perf!");
                    var ifBlock = codeBlock.If($"{namedObjectSave.InstanceName} != null");
                    {
                        ifBlock.Line($"{namedObjectSave.InstanceName}.Visible = false;");
                    }

                }

                switch (creationOptions)
                {
                    case TileNodeNetworkCreationOptions.Empty:
                        // do nothing
                        break;
                    case TileNodeNetworkCreationOptions.FillCompletely:
                        GenerateFillCompletely(namedObjectSave, codeBlock);
                        break;
                    //case TileNodeNetworkCreationOptions.BorderOutline:
                    //    GenerateBorderOutline(namedObjectSave, codeBlock);
                    //    break;
                    case TileNodeNetworkCreationOptions.FromLayer:
                        // not handled:
                        GenerateFromLayer(namedObjectSave, codeBlock);
                        break;

                    case TileNodeNetworkCreationOptions.FromProperties:
                        GenerateFromProperties(namedObjectSave, codeBlock);
                        break;
                    case TileNodeNetworkCreationOptions.FromType:
                        GenerateFromTileType(namedObjectSave, codeBlock);
                        break;
                }
            }

        }

        private void GenerateFromLayer(NamedObjectSave namedObjectSave, ICodeBlock codeBlock)
        {
            // Handled in the constructor call above
            //T Get<T>(string name)
            //{
            //    return namedObjectSave.Properties.GetValue<T>(name);
            //}

            //var instanceName = namedObjectSave.FieldName;
            //var mapName = Get<string>(nameof(TileShapeCollectionPropertiesViewModel.SourceTmxName));
            //var layerName = Get<string>(nameof(TileShapeCollectionPropertiesViewModel.CollisionLayerName));
            //var typeNameInLayer = Get<string>(nameof(TileShapeCollectionPropertiesViewModel.CollisionLayerTileType));

            //var effectiveName = layerName;
            //if(!string.IsNullOrEmpty(typeNameInLayer))
            //{
            //    effectiveName += "_" + typeNameInLayer;
            //}

            //// assign itself if there's nothing in the 
            //codeBlock.Line($"{instanceName} = {mapName}.Collisions.FirstOrDefault(item => item.Name == \"{effectiveName}\")" +
            //    $" ?? {instanceName};");

            // handled in the AssetTypeInfo's GetFromFileFunc because that already has access to the referenced file save

            //var tileType = namedObjectSave.Properties.GetValue<string>(
            //    nameof(ViewModels.TileShapeCollectionPropertiesViewModel.));

        }

        // no border outline

        private void GenerateFillCompletely(NamedObjectSave namedObjectSave, ICodeBlock codeBlock)
        {
            var instanceName = namedObjectSave.FieldName;

            codeBlock.Line($"{instanceName}.FillCompletely();");

            //T Get<T>(string name)
            //{
            //    return namedObjectSave.Properties.GetValue<T>(name);
            //}

            //var tileSize = Get<float>(nameof(TileShapeCollectionPropertiesViewModel.CollisionTileSize));
            //var tileSizeString = tileSize.ToString(CultureInfo.InvariantCulture) + "f";

            //var leftFill = Get<float>(nameof(TileShapeCollectionPropertiesViewModel.CollisionFillLeft));
            //var leftFillString = leftFill.ToString(CultureInfo.InvariantCulture) + "f";

            //var topFill = Get<float>(nameof(TileShapeCollectionPropertiesViewModel.CollisionFillTop));
            //var topFillString = topFill.ToString(CultureInfo.InvariantCulture) + "f";

            //var widthFill = Get<int>(nameof(TileShapeCollectionPropertiesViewModel.CollisionFillWidth));
            //var heightFill = Get<int>(nameof(TileShapeCollectionPropertiesViewModel.CollisionFillHeight));

            //var remainderX = leftFill % tileSize;
            //var remainderY = topFill % tileSize;

            //var instanceName = namedObjectSave.FieldName;

            //codeBlock.Line($"{instanceName}.GridSize = {tileSizeString};");
            ////TileShapeCollectionInstance.GridSize = gridSize;

            //codeBlock.Line($"{instanceName}.LeftSeedX = {remainderX.ToString(CultureInfo.InvariantCulture)};");
            //codeBlock.Line($"{instanceName}.BottomSeedY = {remainderY.ToString(CultureInfo.InvariantCulture)};");

            //codeBlock.Line($"{instanceName}.SortAxis = FlatRedBall.Math.Axis.X;");
            ////TileShapeCollectionInstance.SortAxis = FlatRedBall.Math.Axis.X;

            //var xFor = codeBlock.For($"int x = 0; x < {widthFill}; x++");
            ////int(int x = 0; x < width; x++)
            ////{
            //var yFor = xFor.For($"int y = 0; y < {heightFill}; y++");
            ////    for (int y = 0; y < height; y++)
            ////    {

            //yFor.Line(
            //    $"{instanceName}.AddCollisionAtWorld(" +
            //    $"{leftFillString} + x * {tileSize} + {tileSize} / 2.0f," +
            //    $"{topFillString} - y * {tileSize} - {tileSize} / 2.0f);");
            ////        TileShapeCollectionInstance.AddCollisionAtWorld(
            ////            left + x * gridSize + gridSize / 2.0f,
            ////            top - y * gridSize - gridSize / 2.0f);
            ////    }
            ////}

        }

        private void GenerateFromProperties(NamedObjectSave namedObjectSave, ICodeBlock codeBlock)
        {
            // handled in constructor



            //T Get<T>(string name)
            //{
            //    return namedObjectSave.Properties.GetValue<T>(name);
            //}

            //var mapName = Get<string>(nameof(TileShapeCollectionPropertiesViewModel.SourceTmxName));
            //var propertyName = Get<string>(nameof(TileShapeCollectionPropertiesViewModel.CollisionPropertyName));

            //if (!string.IsNullOrEmpty(mapName) && !string.IsNullOrEmpty(propertyName))
            //{
            //    codeBlock.Line("FlatRedBall.TileCollisions.TileShapeCollectionLayeredTileMapExtensions" +
            //        ".AddCollisionFromTilesWithProperty(" +
            //        $"{namedObjectSave.InstanceName}, {mapName}, \"{propertyName}\");");
            //}

            //FlatRedBall.TileCollisions.TileShapeCollectionLayeredTileMapExtensions
            //    .AddCollisionFromTilesWithProperty(
            //    TileShapeCollectionInstance,
            //    map,
            //    "propertyName");
        }

        private void GenerateFromTileType(NamedObjectSave namedObjectSave, ICodeBlock codeBlock)
        {
            // handled in constructor

            //T Get<T>(string name)
            //{
            //    return namedObjectSave.Properties.GetValue<T>(name);
            //}
            //var mapName = Get<string>(nameof(TileNodeNetworkPropertiesViewModel.SourceTmxName));
            //var typeName = Get<string>(nameof(TileNodeNetworkPropertiesViewModel.NodeNetworkTileTypeName));

            //if (!string.IsNullOrEmpty(mapName) && !string.IsNullOrEmpty(typeName))
            //{
            //    throw new NotImplementedException();
            //    string method = "AddCollisionFromTilesWithType";
            //    if (isMerged)
            //    {
            //        method = "AddMergedCollisionFromTilesWithType";
            //    }
            //    codeBlock.Line("FlatRedBall.TileCollisions.TileShapeCollectionLayeredTileMapExtensions" +
            //        $".{method}(" +
            //        $"{namedObjectSave.InstanceName}, {mapName}, \"{typeName}\");");
            //}

        }

    }
}
