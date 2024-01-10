using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.ICollidablePlugins;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Math;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TileGraphicsPlugin.ViewModels;

namespace TileGraphicsPlugin.CodeGeneration
{
    class TileShapeCollectionCodeGenerator : ElementComponentCodeGenerator
    {
        public TileShapeCollectionCodeGenerator()
        {
            InitializeCategory = "Collision Objects";
        }
        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, IElement element)
        {
            return codeBlock;
        }

        public override ICodeBlock GenerateInitializeLate(ICodeBlock codeBlock, IElement element)
        {
            // Vic says - I think we have to generate late so we can reference other objects that may be assigned in the initialize function,
            // but we need to make sure this happens before 
            NamedObjectSave[] tileShapeCollections = GetAllTileShapeCollectionNamedObjectsInElement(element);

            foreach (var tileShapeCollection in tileShapeCollections)
            {
                if(tileShapeCollection.DefinedByBase == false)
                {
                    codeBlock.Line($"FillCollisionFor{tileShapeCollection.InstanceName}();");
                }
            }

            return codeBlock;
        }

        // This is handled in CollidableCodeGenerator.GeneratePostInitialize
        //public override ICodeBlock GeneratePostInitialize(ICodeBlock codeBlock, IElement element)
        //{
        //    if(element.IsICollidableRecursive())
        //    {
        //        foreach(var nos in element.NamedObjects)
        //        {
        //            // If it's in derived, we don't want to add it here, since it should be added by base. Otherwise
        //            // we'd get a double-add
        //            if(nos.DefinedByBase == false && 
        //                nos.GetAssetTypeInfo() == AssetTypeInfoAdder.Self.TileShapeCollectionAssetTypeInfo )
        //            {
        //                codeBlock.If($"{nos.InstanceName} != null")
        //                    .Line($"mGeneratedCollision.AxisAlignedRectangles.AddRange({nos.InstanceName}.Rectangles);")
        //                    .Line($"mGeneratedCollision.Polygons.AddRange({nos.InstanceName}.Polygons);");
        //            }
        //        }
        //    }

        //    return codeBlock;
        //}

        static NamedObjectSave[] GetAllTileShapeCollectionNamedObjectsInElement(IElement element)
        {
            return element
                .AllNamedObjects
                .Where(item => item.GetAssetTypeInfo() == AssetTypeInfoAdder.Self.TileShapeCollectionAssetTypeInfo)
                .ToArray();
        }

        public override ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, IElement element)
        {
            NamedObjectSave[] tileShapeCollections = GetAllTileShapeCollectionNamedObjectsInElement(element);



            foreach (var shapeCollection in tileShapeCollections)
            {
                T Get<T>(string name)
                {
                    return shapeCollection.Properties.GetValue<T>(name);
                }

                var creationOptions = Get<CollisionCreationOptions>(
                    nameof(TileShapeCollectionPropertiesViewModel.CollisionCreationOptions));

                var variable = shapeCollection.GetCustomVariable("Visible");

                bool filledInHere = creationOptions == CollisionCreationOptions.BorderOutline ||
                    creationOptions == CollisionCreationOptions.FillCompletely ||
                    creationOptions == CollisionCreationOptions.Empty;

                if (variable != null && variable.Value is bool && ((bool)variable.Value) == true && 
                    filledInHere)
                {
                    //codeBlock.Line($"{shapeCollection.FieldName}.Visible = true;");
                    codeBlock.Line($"{shapeCollection.FieldName}.AddToLayer(null);");
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

            var creationOptions = Get<CollisionCreationOptions>(
                nameof(TileShapeCollectionPropertiesViewModel.CollisionCreationOptions));

            bool comesFromLayer = creationOptions == CollisionCreationOptions.FromLayer;

            if(comesFromLayer)
            {
                var instanceName = namedObjectSave.FieldName;
                var mapName = Get<string>(nameof(TileShapeCollectionPropertiesViewModel.SourceTmxName));
                var layerName = Get<string>(nameof(TileShapeCollectionPropertiesViewModel.CollisionLayerName));
                var typeNameInLayer = Get<string>(nameof(TileShapeCollectionPropertiesViewModel.CollisionLayerTileType));

                var effectiveName = layerName;
                if (!string.IsNullOrEmpty(typeNameInLayer))
                {
                    effectiveName += "_" + typeNameInLayer;
                }

                // assign itself if there's nothing in the 
                return $"{instanceName} = {mapName}.Collisions.FirstOrDefault(item => item.Name == \"{effectiveName}\")" +
                    $" ?? new FlatRedBall.TileCollisions.TileShapeCollection(){{Name = \"{effectiveName}\" }};";

            }
            // If it comes from map collision, don't creat a new instance, we'll do a straight assignment
            else if (creationOptions == CollisionCreationOptions.FromMapCollision)
            {
                return string.Empty;
            }
            else 
            {
                return $"{namedObjectSave.FieldName} = new FlatRedBall.TileCollisions.TileShapeCollection(); {namedObjectSave.FieldName}.Name = \"{namedObjectSave.InstanceName}\";";

            }

        }

        private void GenerateInitializeCodeFor(NamedObjectSave namedObjectSave, ICodeBlock codeBlock)
        {

            T Get<T>(string name)
            {
                return namedObjectSave.Properties.GetValue<T>(name);
            }
            var creationOptions = Get<CollisionCreationOptions>(
                nameof(TileShapeCollectionPropertiesViewModel.CollisionCreationOptions));

            // Victor Chelaru Sept 6 2021
            // Currently only the outline can override the settings. This could expand over time but let's take it incrementally
            var canGenerateInitialize =
                namedObjectSave.DefinedByBase == false || creationOptions == CollisionCreationOptions.BorderOutline;

            if (canGenerateInitialize)
            {
                var sourceType = namedObjectSave.SourceType;

                if(sourceType == SourceType.File)
                {
                    
                    codeBlock.Line($"//The NamedObject {namedObjectSave} has a SourceType of {sourceType}, so it may not instantiate " +
                        $"properly. If you are experiencing a NullReferenceException, consider changing the" +
                        $"SourceType to {SourceType.FlatRedBallType}");
                }

                var isVisible = namedObjectSave.GetCustomVariable("Visible")?.Value is bool asBool && asBool == true;
                


                var ifBlock = codeBlock.If($"{namedObjectSave.InstanceName} != null");
                if (!isVisible)
                {
                    ifBlock.Line("// normally we wait to set variables until after the object is created, but in this case if the");
                    ifBlock.Line("// TileShapeCollection doesn't have its Visible set before creating the tiles, it can result in");
                    ifBlock.Line("// really bad performance issues, as shapes will be made visible, then invisible. Really bad perf!");
                    {
                        ifBlock.Line($"{namedObjectSave.InstanceName}.Visible = false;");
                    }

                }
                bool adjustRepositionDirectionsOnAddAndRemove =
                    (namedObjectSave.GetCustomVariable("AdjustRepositionDirectionsOnAddAndRemove")?.Value as bool?) ?? true;


                if(!adjustRepositionDirectionsOnAddAndRemove)
                {
                    ifBlock.Line("// normally we wait to set variables until after the object is created, in this case");
                    ifBlock.Line("// we want the variable set before the object is instantiated");
                    {
                        ifBlock.Line($"{namedObjectSave.InstanceName}.AdjustRepositionDirectionsOnAddAndRemove = false;");
                    }
                }

                var repositionStyle = namedObjectSave.GetCustomVariable("RepositionUpdateStyle")?.Value as string;
                if(!string.IsNullOrEmpty(repositionStyle))
                {
                    ifBlock.Line($"{namedObjectSave.InstanceName}.RepositionUpdateStyle = FlatRedBall.TileCollisions.RepositionUpdateStyle.{repositionStyle};");
                }

                switch(creationOptions)
                {
                    case CollisionCreationOptions.Empty:
                        // do nothing
                        break;
                    case CollisionCreationOptions.FillCompletely:
                        GenerateFillCompletely(namedObjectSave, ifBlock);
                        break;
                    case CollisionCreationOptions.BorderOutline:
                        GenerateBorderOutline(namedObjectSave, ifBlock);
                        break;
                    case CollisionCreationOptions.FromProperties:
                        GenerateFromProperties(namedObjectSave, ifBlock);
                        break;
                    case CollisionCreationOptions.FromType:
                        GenerateFromTileType(namedObjectSave, ifBlock);
                        break;
                    case CollisionCreationOptions.FromLayer:
                        // not handled:
                        GenerateFromLayerCollision(namedObjectSave, ifBlock);
                        break;
                    case CollisionCreationOptions.FromMapCollision:
                        GenerateFromMapCollision(namedObjectSave, codeBlock);

                        break;
                }
            }

            void GenerateFromMapCollision(NamedObjectSave namedObjectSave, ICodeBlock codeBlock)
            {
                var mapName = Get<string>(nameof(TileShapeCollectionPropertiesViewModel.SourceTmxName));
                var tmxCollisionName = Get<string>(nameof(TileShapeCollectionPropertiesViewModel.TmxCollisionName));

                var instanceName = namedObjectSave.FieldName;
                // If we do FirstOrDefault this doesn't fail, but codegen will fail later on. This gives a more informative error
                //codeBlock.Line($"{instanceName} = {mapName}.Collisions.FirstOrDefault(item => item.Name == \"{tmxCollisionName}\");");
                // Update Feb 27, 2022
                // If a TileShapeCollection uses custom collision shapes, then the type will create a ShapeCollection and put it in the
                // Collisions list. But if the current map doesn't use the tile, then the shape collection won't be created, and First will
                // crash. We should not crash here, but rather rely on FRB Editor to report errors.
                codeBlock.Line($"{instanceName} = {mapName}.Collisions.FirstOrDefault(item => item.Name == \"{tmxCollisionName}\")" +
                    $" ?? new FlatRedBall.TileCollisions.TileShapeCollection(){{Name = \"{tmxCollisionName}\" }};");
    


            }
        }

        private void GenerateFromLayerCollision(NamedObjectSave namedObjectSave, ICodeBlock codeBlock)
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

        private void GenerateBorderOutline(NamedObjectSave namedObjectSave, ICodeBlock codeBlock)
        {

            T Get<T>(string name)
            {
                return namedObjectSave.Properties.GetValue<T>(name);
            }
            string FloatString(float value) => value.ToString(CultureInfo.InvariantCulture) + "f";

            var tileSize = Get<float>(nameof(TileShapeCollectionPropertiesViewModel.CollisionTileSize));
            var tileSizeString = FloatString(tileSize);

            var leftFill = Get<float>(nameof(TileShapeCollectionPropertiesViewModel.CollisionFillLeft));
            var leftFillString = FloatString(leftFill);

            var topFill = Get<float>(nameof(TileShapeCollectionPropertiesViewModel.CollisionFillTop));
            var topFillString = FloatString(topFill);

            var borderOutlineType = (BorderOutlineType)Get<int>(
                nameof(TileShapeCollectionPropertiesViewModel.BorderOutlineType));

            var remainderX = leftFill % tileSize;
            var remainderY = topFill % tileSize;

            var instanceName = namedObjectSave.FieldName;

            codeBlock.Line($"// Call RemoveFromManagers in case this was populated in a base class");
            codeBlock.Line($"{instanceName}.RemoveFromManagers();");
            codeBlock.Line($"{instanceName}.GridSize = {tileSizeString};");
            //TileShapeCollectionInstance.GridSize = gridSize;

            codeBlock.Line($"{instanceName}.LeftSeedX = {remainderX.ToString(CultureInfo.InvariantCulture)};");
            codeBlock.Line($"{instanceName}.BottomSeedY = {remainderY.ToString(CultureInfo.InvariantCulture)};");

            codeBlock.Line($"{instanceName}.SortAxis = FlatRedBall.Math.Axis.X;");
            //TileShapeCollectionInstance.SortAxis = FlatRedBall.Math.Axis.X;

            var widthFill = Get<int>(nameof(TileShapeCollectionPropertiesViewModel.CollisionFillWidth));
            var heightFill = Get<int>(nameof(TileShapeCollectionPropertiesViewModel.CollisionFillHeight));

            if(borderOutlineType == BorderOutlineType.InnerSize)
            {
                var innerWidth = Get<float>(
                    nameof(TileShapeCollectionPropertiesViewModel.InnerSizeWidth));

                var innerHeight = Get<float>(
                    nameof(TileShapeCollectionPropertiesViewModel.InnerSizeHeight));

                var additionalWidth = 2 * tileSize;
                var additionalHeight = 2 * tileSize;

                widthFill = MathFunctions.RoundToInt( (innerWidth + additionalWidth)/tileSize);
                heightFill = MathFunctions.RoundToInt((innerHeight + additionalHeight) / tileSize);


            }
            var xFor = codeBlock.For($"int x = 0; x < {widthFill}; x++");
            //int(int x = 0; x < width; x++)
            //{
            var ifBlock = xFor.If($"x == 0 || x == {widthFill} - 1");
            var yFor = ifBlock.For($"int y = 0; y < {heightFill}; y++");
            //    for (int y = 0; y < height; y++)
            //    {

            yFor.Line(
                $"{instanceName}.AddCollisionAtWorld(" +
                $"{leftFillString} + x * {tileSize} + {tileSize} / 2.0f," +
                $"{topFillString} - y * {tileSize} - {tileSize} / 2.0f);");
            //        TileShapeCollectionInstance.AddCollisionAtWorld(
            //            left + x * gridSize + gridSize / 2.0f,
            //            top - y * gridSize - gridSize / 2.0f);
            //    }
            //}
            var elseBlock = ifBlock.End().Else();

            elseBlock.Line(
                $"{instanceName}.AddCollisionAtWorld(" +
                $"{leftFillString} + x * {tileSize} + {tileSize} / 2.0f," +
                $"{topFillString} - {tileSize} / 2.0f);");

            elseBlock.Line(
                $"{instanceName}.AddCollisionAtWorld(" +
                $"{leftFillString} + x * {tileSize} + {tileSize} / 2.0f," +
                $"{topFillString} - {heightFill - 1} * {tileSize} - {tileSize} / 2.0f);");

        }

        private void GenerateFillCompletely(NamedObjectSave namedObjectSave, ICodeBlock codeBlock)
        {
            T Get<T>(string name)
            {
                return namedObjectSave.Properties.GetValue<T>(name);
            }

            var tileSize = Get<float>(nameof(TileShapeCollectionPropertiesViewModel.CollisionTileSize));
            var tileSizeString = tileSize.ToString(CultureInfo.InvariantCulture) + "f";

            var leftFill = Get<float>(nameof(TileShapeCollectionPropertiesViewModel.CollisionFillLeft));
            var leftFillString = leftFill.ToString(CultureInfo.InvariantCulture) + "f";

            var topFill = Get<float>(nameof(TileShapeCollectionPropertiesViewModel.CollisionFillTop));
            var topFillString = topFill.ToString(CultureInfo.InvariantCulture) + "f";

            var widthFill = Get<int>(nameof(TileShapeCollectionPropertiesViewModel.CollisionFillWidth));
            var heightFill = Get<int>(nameof(TileShapeCollectionPropertiesViewModel.CollisionFillHeight));

            var remainderX = leftFill % tileSize;
            var remainderY = topFill % tileSize;

            var instanceName = namedObjectSave.FieldName;

            codeBlock.Line($"{instanceName}.GridSize = {tileSizeString};");
            //TileShapeCollectionInstance.GridSize = gridSize;

            codeBlock.Line($"{instanceName}.LeftSeedX = {remainderX.ToString(CultureInfo.InvariantCulture)};");
            codeBlock.Line($"{instanceName}.BottomSeedY = {remainderY.ToString(CultureInfo.InvariantCulture)};");

            codeBlock.Line($"{instanceName}.SortAxis = FlatRedBall.Math.Axis.X;");
            //TileShapeCollectionInstance.SortAxis = FlatRedBall.Math.Axis.X;

            var xFor = codeBlock.For($"int x = 0; x < {widthFill}; x++");
            //int(int x = 0; x < width; x++)
            //{
            var yFor = xFor.For($"int y = 0; y < {heightFill}; y++");
            //    for (int y = 0; y < height; y++)
            //    {

            yFor.Line(
                $"{instanceName}.AddCollisionAtWorld(" +
                $"{leftFillString} + x * {tileSize} + {tileSize} / 2.0f," +
                $"{topFillString} - y * {tileSize} - {tileSize} / 2.0f);");
            //        TileShapeCollectionInstance.AddCollisionAtWorld(
            //            left + x * gridSize + gridSize / 2.0f,
            //            top - y * gridSize - gridSize / 2.0f);
            //    }
            //}

        }

        private void GenerateFromProperties(NamedObjectSave namedObjectSave, ICodeBlock codeBlock)
        {
            T Get<T>(string name)
            {
                return namedObjectSave.Properties.GetValue<T>(name);
            }

            var mapName = Get<string>(nameof(TileShapeCollectionPropertiesViewModel.SourceTmxName));
            var propertyName = Get<string>(nameof(TileShapeCollectionPropertiesViewModel.CollisionPropertyName));

            if(!string.IsNullOrEmpty(mapName) && !string.IsNullOrEmpty(propertyName))
            {
                codeBlock.Line("FlatRedBall.TileCollisions.TileShapeCollectionLayeredTileMapExtensions" +
                    ".AddCollisionFromTilesWithProperty(" +
                    $"{namedObjectSave.InstanceName}, {mapName}, \"{propertyName}\");");
            }

            //FlatRedBall.TileCollisions.TileShapeCollectionLayeredTileMapExtensions
            //    .AddCollisionFromTilesWithProperty(
            //    TileShapeCollectionInstance,
            //    map,
            //    "propertyName");
        }

        private void GenerateFromTileType(NamedObjectSave namedObjectSave, ICodeBlock codeBlock)
        {
            T Get<T>(string name)
            {
                return namedObjectSave.Properties.GetValue<T>(name);
            }

            var mapName = Get<string>(nameof(TileShapeCollectionPropertiesViewModel.SourceTmxName));
            var typeName = Get<string>(nameof(TileShapeCollectionPropertiesViewModel.CollisionTileTypeName));
            var removeTiles = Get<bool>(nameof(TileShapeCollectionPropertiesViewModel.RemoveTilesAfterCreatingCollision));
            var isMerged = Get<bool>(nameof(TileShapeCollectionPropertiesViewModel.IsCollisionMerged));
            if (!string.IsNullOrEmpty(mapName) && !string.IsNullOrEmpty(typeName))
            {
                string method = "AddCollisionFromTilesWithType";
                if(isMerged)
                {
                    method = "AddMergedCollisionFromTilesWithType";
                }
                codeBlock.Line("FlatRedBall.TileCollisions.TileShapeCollectionLayeredTileMapExtensions" +
                    $".{method}(" +
                    $"{namedObjectSave.InstanceName}, {mapName}, \"{typeName}\", {removeTiles.ToString().ToLowerInvariant()});");
            }

        }

        public override ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, IElement element)
        {
            NamedObjectSave[] tileShapeCollections = GetAllTileShapeCollectionNamedObjectsInElement(element);

            foreach (var tileShapeCollection in tileShapeCollections)
            {
                if (tileShapeCollection.DefinedByBase == false)
                {
                    var method = codeBlock.Function("protected virtual void", $"FillCollisionFor{tileShapeCollection.InstanceName}");
                    GenerateInitializeCodeFor(tileShapeCollection, method);
                }
                else
                {
                    // override it if it's an outline:
                    T Get<T>(string name)
                    {
                        return tileShapeCollection.Properties.GetValue<T>(name);
                    }
                    var creationOptions = Get<CollisionCreationOptions>(
                        nameof(TileShapeCollectionPropertiesViewModel.CollisionCreationOptions));

                    if(creationOptions == CollisionCreationOptions.BorderOutline)
                    {
                        var method = codeBlock.Function("protected override void", $"FillCollisionFor{tileShapeCollection.InstanceName}");
                        GenerateInitializeCodeFor(tileShapeCollection, method);
                    }
                }
            }

            return codeBlock;
        }
    }
}
