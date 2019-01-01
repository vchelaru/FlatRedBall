using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TileGraphicsPlugin
{
    public class AssetTypeInfoAdder : Singleton<AssetTypeInfoAdder>
    {
        AssetTypeInfo tmxAssetTypeInfo;

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
            var tileShapeCollectionAti = CreateAtiForTileShapeCollection();

            AddIfNotPresent(TmxAssetTypeInfo);
            AddIfNotPresent(layeredTileMapScnx);
            AddIfNotPresent(layeredTilemapTilb);
            AddIfNotPresent(tileShapeCollectionAti);
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

            toReturn.FriendlyName = "LayeredTileMap (.tmx)";
            toReturn.QualifiedSaveTypeName = "";
            toReturn.Extension = "tmx";
            toReturn.CustomLoadMethod = "{THIS} = FlatRedBall.TileGraphics.LayeredTileMap.FromTiledMapSave(\"{FILE_NAME}\", {CONTENT_MANAGER_NAME});";

            return toReturn;
        }

        public AssetTypeInfo CreateAtiForTileShapeCollection()
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
            toReturn.VariableDefinitions.Add(new VariableDefinition() { Name = "Visible", DefaultValue = "false", Type = "bool" });

            return toReturn;
        }

    }
}
