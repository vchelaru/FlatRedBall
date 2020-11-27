using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TiledPluginCore.CodeGeneration;
using TiledPluginCore.ViewModels;
using TileGraphicsPlugin.CodeGeneration;
using TileGraphicsPlugin.ViewModels;

namespace TileGraphicsPlugin
{
    public class AssetTypeInfoAdder : Singleton<AssetTypeInfoAdder>
    {
        AssetTypeInfo tmxAssetTypeInfo;
        AssetTypeInfo tileShapeCollectionAssetTypeInfo;
        AssetTypeInfo tileNodeNetworkAssetTypeInfo;

        public AssetTypeInfo TmxAssetTypeInfo
        {
            get
            {
                if(tmxAssetTypeInfo == null)
                {
                    tmxAssetTypeInfo = CreateAtiForRawTmx();
                }

                return tmxAssetTypeInfo;
            }
        }
        public AssetTypeInfo TileShapeCollectionAssetTypeInfo
        {
            get
            {
                if(tileShapeCollectionAssetTypeInfo == null)
                {
                    tileShapeCollectionAssetTypeInfo = CreateAtiForTileShapeCollection();
                }
                return tileShapeCollectionAssetTypeInfo;
            }
        }

        public AssetTypeInfo TileNodeNetworkAssetTypeInfo
        {
            get
            {
                if(tileNodeNetworkAssetTypeInfo == null)
                {
                    tileNodeNetworkAssetTypeInfo = CreateAtiForTileNodeNetwork();
                }
                return tileNodeNetworkAssetTypeInfo;
            }
        }

        public void UpdateAtiCsvPresence()
        {
            string projectFolder = FileManager.GetDirectory(GlueState.Self.GlueProjectFileName);
            string settingsFolder = projectFolder + "GlueSettings/";
            if (!Directory.Exists(settingsFolder))
            {
                Directory.CreateDirectory(settingsFolder);
            }

            List<AssetTypeInfo> list;
            list = new List<AssetTypeInfo>();

            var layeredTileMapScnx = CreateAtiForLayeredTilemapScnx();
            var layeredTilemapTilb = CreateAtiForLayeredTilemapTilb();

            AddIfNotPresent(TmxAssetTypeInfo);
            AddIfNotPresent(layeredTileMapScnx);
            AddIfNotPresent(layeredTilemapTilb);
            AddIfNotPresent(TileShapeCollectionAssetTypeInfo);
            AddIfNotPresent(TileNodeNetworkAssetTypeInfo);
        }

        public void AddIfNotPresent(AssetTypeInfo ati)
        {
            if (AvailableAssetTypes.Self.AllAssetTypes.Any(item => item.FriendlyName == ati.FriendlyName) == false)
            {
                AvailableAssetTypes.Self.AddAssetType(ati);
            }
        }

        private AssetTypeInfo CreateAtiForLayeredTilemapScnx()
        {
            AssetTypeInfo toReturn = new AssetTypeInfo();

            toReturn.FriendlyName = "LayeredTileMap (.scnx)";
            toReturn.QualifiedRuntimeTypeName = new PlatformSpecificType();
            toReturn.QualifiedRuntimeTypeName.QualifiedType = "FlatRedBall.TileGraphics.LayeredTileMap";
            toReturn.QualifiedSaveTypeName = "FlatRedBall.Content.SpriteEditorScene";
            toReturn.Extension = "scnx";
            toReturn.AddToManagersMethod = new List<string>();
            toReturn.AddToManagersMethod.Add("this.AddToManagers()");
            toReturn.LayeredAddToManagersMethod.Add("this.AddToManagers(mLayer)");
            toReturn.CustomLoadMethod = "{THIS} = FlatRedBall.TileGraphics.LayeredTileMap.FromScene(\"{FILE_NAME}\", {CONTENT_MANAGER_NAME});";
            toReturn.DestroyMethod = "this.Destroy()";
            toReturn.ShouldBeDisposed = false;
            toReturn.ShouldAttach = true;
            // I believe this means it will call Clone() 
            toReturn.CanBeCloned = true;
            toReturn.MustBeAddedToContentPipeline = false;
            toReturn.HasCursorIsOn = false;
            toReturn.HasVisibleProperty = false;
            toReturn.CanIgnorePausing = false;
            toReturn.CanBeObject = true;
            // We don't want this to show up as a file type - it's the same thing as a .scnx, and should only be used
            // from a TMX.
            toReturn.HideFromNewFileWindow = true;


            return toReturn;
        }

        private AssetTypeInfo CreateAtiForLayeredTilemapTilb()
        {
            AssetTypeInfo toReturn = CreateAtiForLayeredTilemapScnx();
            toReturn.FriendlyName = "LayeredTileMap (.tilb)";
            toReturn.QualifiedSaveTypeName = "";
            toReturn.Extension = "tilb";
            toReturn.CustomLoadMethod = "{THIS} = FlatRedBall.TileGraphics.LayeredTileMap.FromReducedTileMapInfo(\"{FILE_NAME}\", {CONTENT_MANAGER_NAME});";

            return toReturn;
        }

        private AssetTypeInfo CreateAtiForRawTmx()
        {
            AssetTypeInfo toReturn = CreateAtiForLayeredTilemapScnx();

            toReturn.VariableDefinitions.Add(new VariableDefinition
            {
                Name = "X",
                Type = "float",
                DefaultValue = "0",
                Category = "Position"

            });

            toReturn.VariableDefinitions.Add(new VariableDefinition
            {
                Name = "Y",
                Type = "float",
                DefaultValue = "0",
                Category = "Position"

            });

            toReturn.VariableDefinitions.Add(new VariableDefinition
            {
                Name = "Z",
                Type = "float",
                DefaultValue = "0",
                Category = "Position"

            });

            toReturn.VariableDefinitions.Add(new VariableDefinition
            {
                Name = "CreateEntitiesFromTiles",
                Type = "bool",
                DefaultValue = "false",
                Category = "Entities and Collision",
                UsesCustomCodeGeneration = true
            });

            toReturn.FriendlyName = "LayeredTileMap (.tmx)";
            toReturn.QualifiedSaveTypeName = "";
            toReturn.Extension = "tmx";
            toReturn.CustomLoadMethod = "{THIS} = FlatRedBall.TileGraphics.LayeredTileMap.FromTiledMapSave(\"{FILE_NAME}\", {CONTENT_MANAGER_NAME});";
            toReturn.HideFromNewFileWindow = false;
            toReturn.ActivityMethod = "this?.AnimateSelf();";

            return toReturn;
        }

        private AssetTypeInfo CreateAtiForTileShapeCollection()
        {

            AssetTypeInfo toReturn = new AssetTypeInfo();
            toReturn.FriendlyName = "TileShapeCollection";
            toReturn.QualifiedRuntimeTypeName = new PlatformSpecificType();
            toReturn.QualifiedRuntimeTypeName.QualifiedType = "FlatRedBall.TileCollisions.TileShapeCollection";
            toReturn.QualifiedSaveTypeName = null;
            toReturn.Extension = null;
            toReturn.AddToManagersMethod = new List<string>();
            toReturn.CustomLoadMethod = null;
            toReturn.DestroyMethod = "this.Visible = false";
            toReturn.ShouldBeDisposed = false;
            toReturn.ShouldAttach = false;
            toReturn.MustBeAddedToContentPipeline = false;
            toReturn.CanBeCloned = false;
            toReturn.HasCursorIsOn = false;
            toReturn.CanIgnorePausing = false;
            toReturn.CanBeObject = true;
            toReturn.HasVisibleProperty = true;
            toReturn.FindByNameSyntax = $"Collisions.First(item => item.Name == \"OBJECTNAME\");";

            toReturn.GetObjectFromFileFunc = GetTileShapeCollectionObjectFromFileFunc;
            toReturn.VariableDefinitions.Add(new VariableDefinition() { Name = "Visible", DefaultValue = "false", Type = "bool" });
            toReturn.ConstructorFunc = GenerateTileShapeCollectionConstructionFunc;
            return toReturn;
        }

        private string GenerateTileShapeCollectionConstructionFunc(IElement parentElement, NamedObjectSave namedObject, ReferencedFileSave referencedFile)
        {
            return TileShapeCollectionCodeGenerator.GenerateConstructorFor(namedObject);
            
        }

        private string GetTileShapeCollectionObjectFromFileFunc(IElement element, NamedObjectSave namedObjectSave, 
            ReferencedFileSave referencedFileSave, string overridingContainerName)
        {
            // CollisionLayer1 = TmxWithTileShapeCollectionLayers.Collisions.First(item => item.Name == "CollisionLayer1");
            T Get<T>(string name)
            {
                return namedObjectSave.Properties.GetValue<T>(name);
            }

            var creationOptions = Get<CollisionCreationOptions>(
                nameof(TileShapeCollectionPropertiesViewModel.CollisionCreationOptions));


            if(creationOptions == CollisionCreationOptions.FromLayer)
            {
                //var tileType = namedObjectSave.Properties.GetValue<string>(
                //    nameof(ViewModels.TileShapeCollectionPropertiesViewModel.CollisionTileType));

                var toReturn = $"{namedObjectSave.FieldName} = new FlatRedBall.TileCollisions.TileShapeCollection();\n";


                toReturn +=
                    "FlatRedBall.TileCollisions.TileShapeCollectionLayeredTileMapExtensions.AddCollisionFrom(\n" +
                    $"{namedObjectSave.FieldName},\n" +
                    $"{referencedFileSave.GetInstanceName()}.MapLayers.FindByName(\"{namedObjectSave.FieldName}\"),\n" +
                    $"{referencedFileSave.GetInstanceName()},\n" +
                    $"list => list.Any(" +
                    //$"item => item.Name == \"Type\" " +
                    //$"  && item.Value as string == \"{tileType}\")" +
                    $"));\n";

                //if (namedObjectSave.Properties.GetValue<bool>(nameof(ViewModels.TileShapeCollectionPropertiesViewModel.IsCollisionVisible)))
                //{
                //    toReturn += $"{namedObjectSave.FieldName}.Visible = true;\n";
                //}

                return toReturn;
            }
            //else
            //{
            //    return $"{namedObjectSave.FieldName} = {referencedFileSave.GetInstanceName()}.Collisions.First(item => item.Name == \"{sourceName}\");";
            //}
            return "";
        }

        private AssetTypeInfo CreateAtiForTileNodeNetwork()
        {
            AssetTypeInfo toReturn = new AssetTypeInfo();
            toReturn.FriendlyName = "TileNodeNetwork";
            toReturn.QualifiedRuntimeTypeName = new PlatformSpecificType();
            toReturn.QualifiedRuntimeTypeName.QualifiedType = "FlatRedBall.AI.Pathfinding.TileNodeNetwork";
            toReturn.QualifiedSaveTypeName = null;
            toReturn.Extension = null;
            toReturn.AddToManagersMethod = new List<string>();
            toReturn.CustomLoadMethod = null;
            toReturn.DestroyMethod = "this.Visible = false";
            toReturn.ShouldBeDisposed = false;
            toReturn.ShouldAttach = false;
            toReturn.MustBeAddedToContentPipeline = false;
            toReturn.CanBeCloned = false;
            toReturn.HasCursorIsOn = false;
            toReturn.CanIgnorePausing = false;
            toReturn.CanBeObject = true;
            toReturn.HasVisibleProperty = true;
            //toReturn.FindByNameSyntax = $"Collisions.First(item => item.Name == \"OBJECTNAME\");";

            toReturn.GetObjectFromFileFunc = GetTileNodeNetworkObjectFromFileFunc;
            toReturn.VariableDefinitions.Add(new VariableDefinition() { Name = "Visible", DefaultValue = "false", Type = "bool" });
            toReturn.ConstructorFunc = GenerateTileNodeNetworkConstructionFunc;
            return toReturn;
        }

        private string GenerateTileNodeNetworkConstructionFunc(IElement arg1, NamedObjectSave namedObject, ReferencedFileSave arg3)
        {
            return TileNodeNetworkCodeGenerator.GenerateConstructorFor(namedObject);
        }

        private string GetTileNodeNetworkObjectFromFileFunc(IElement element, NamedObjectSave namedObjectSave, ReferencedFileSave referencedFileSave, string overridingContainerName)
        {
            T Get<T>(string name)
            {
                return namedObjectSave.Properties.GetValue<T>(name);
            }

            var creationOptions = Get<TileNodeNetworkCreationOptions>(
                nameof(TileNodeNetworkPropertiesViewModel.NetworkCreationOptions));

            if (creationOptions == TileNodeNetworkCreationOptions.FromLayer)
            {
                throw new NotImplementedException();
                //var tileType = namedObjectSave.Properties.GetValue<string>(
                //    nameof(ViewModels.TileShapeCollectionPropertiesViewModel.CollisionTileType));

                var toReturn = $"{namedObjectSave.FieldName} = new FlatRedBall.TileCollisions.TileShapeCollection();\n";


                toReturn +=
                    "FlatRedBall.TileCollisions.TileShapeCollectionLayeredTileMapExtensions.AddCollisionFrom(\n" +
                    $"{namedObjectSave.FieldName},\n" +
                    $"{referencedFileSave.GetInstanceName()}.MapLayers.FindByName(\"{namedObjectSave.FieldName}\"),\n" +
                    $"{referencedFileSave.GetInstanceName()},\n" +
                    $"list => list.Any(" +
                    //$"item => item.Name == \"Type\" " +
                    //$"  && item.Value as string == \"{tileType}\")" +
                    $"));\n";

                //if (namedObjectSave.Properties.GetValue<bool>(nameof(ViewModels.TileShapeCollectionPropertiesViewModel.IsCollisionVisible)))
                //{
                //    toReturn += $"{namedObjectSave.FieldName}.Visible = true;\n";
                //}

                return toReturn;
            }
            //else
            //{
            //    return $"{namedObjectSave.FieldName} = {referencedFileSave.GetInstanceName()}.Collisions.First(item => item.Name == \"{sourceName}\");";
            //}
            return "";
        }
    }
}
