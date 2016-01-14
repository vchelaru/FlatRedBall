using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasPlugin.Managers
{
    public class AtiManager : Singleton<AtiManager>
    {

        List<AssetTypeInfo> createdAtis = new List<AssetTypeInfo>();

        public AssetTypeInfo TextureAtlasAti
        {
            get;
            private set;
        }

        public void PerformStartupLogic()
        {

            //AddAtlasedTextureAti();

            CreateAndAddTpsAti();

            CreateAndAddTexturePackerSpriteSheetAti();

            ModifySpriteAti();
        }

        private void ModifySpriteAti()
        {
            var allAssetTypes = AvailableAssetTypes.Self.AllAssetTypes;

            var existingSprite = allAssetTypes.FirstOrDefault(item => item.QualifiedRuntimeTypeName.QualifiedType == "FlatRedBall.Sprite");

            //existingSprite.ExtraVariablesPattern += " TexturePackerLoader.AtlasedTexture AtlasedTexture";

            var variableDefinition = new VariableDefinition
            {
                Name = "AtlasedTexture",
                Type = "AtlasedTextureName",
                DefaultValue = null,
                Category = "Texture",
                UsesCustomCodeGeneration = true
            };

            existingSprite.VariableDefinitions.Insert(0, variableDefinition);
        }

        private void CreateAndAddTpsAti()
        {
            AssetTypeInfo gumProjectAti = new AssetTypeInfo();
            gumProjectAti.FriendlyName = "TexturePacker Sprite Sheet (.tps)";

            //gumProjectAti.QualifiedSaveTypeName = "Gum.Data.ProjectSave";
            gumProjectAti.Extension = "tps";
            //gumProjectAti.CustomLoadMethod = "FlatRedBall.Gum.GumIdb.StaticInitialize(\"{FILE_NAME}\"); " +
            //    "FlatRedBall.Gum.GumIdb.RegisterTypes();  " +
            //    "FlatRedBall.Gui.GuiManager.BringsClickedWindowsToFront = false;";
            gumProjectAti.SupportsMakeOneWay = false;
            gumProjectAti.ShouldAttach = false;
            gumProjectAti.MustBeAddedToContentPipeline = false;
            gumProjectAti.CanBeCloned = false;
            gumProjectAti.HasCursorIsOn = false;
            gumProjectAti.HasVisibleProperty = false;
            gumProjectAti.CanIgnorePausing = false;

            // don't let users add this:
            gumProjectAti.HideFromNewFileWindow = true;

            gumProjectAti.CanBeObject = false;

            AddIfNotPresent(gumProjectAti);

        }

        private void CreateAndAddTexturePackerSpriteSheetAti()
        {
            TextureAtlasAti = new AssetTypeInfo();
            TextureAtlasAti.FriendlyName = "Texture Atlas";
            // We don't want this as part of the VS project, since it's not loaded at runtime.
            TextureAtlasAti.Extension = "atlas";

            TextureAtlasAti.QualifiedRuntimeTypeName.QualifiedType = "FlatRedBall.Graphics.Texture.Atlas";
            TextureAtlasAti.CustomLoadMethod =
                "{THIS} = FlatRedBall.Graphics.Texture.AtlasLoader.Load(\"{FILE_NAME}\", {CONTENT_MANAGER_NAME});";

            TextureAtlasAti.CustomBuildToolName = "*.tps->TexturePacker.exe-> *.atlas";

            AddIfNotPresent(TextureAtlasAti);

            AvailableAssetTypes.Self.AddAssetType(TextureAtlasAti);
        }

        public void AddIfNotPresent(AssetTypeInfo ati)
        {
            if (AvailableAssetTypes.Self.AllAssetTypes.Any(item => item.FriendlyName == ati.FriendlyName) == false)
            {
                AvailableAssetTypes.Self.AddAssetType(ati);
                createdAtis.Add(ati);
            }
        }

        internal void RemoveAllAtis()
        {
            foreach(var ati in createdAtis)
            {
                AvailableAssetTypes.Self.RemoveAssetType(ati);
            }

            createdAtis.Clear();
        }
    }
}
