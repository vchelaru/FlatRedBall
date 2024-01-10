using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TileGraphicsPlugin.Managers
{
    public class CodeItemAdderManager : FlatRedBall.Glue.Managers.Singleton<CodeItemAdderManager>
    {
        CodeBuildItemAdder mTileGraphicsAdder;
        CodeBuildItemAdder mTileCollisionAdder;
        CodeBuildItemAdder mTileEntityAdder;

        public void AddFilesToCodeBuildItemAdder()
        {
            mTileGraphicsAdder = new CodeBuildItemAdder();
            mTileGraphicsAdder.OutputFolderInProject = "TileGraphics";
            mTileGraphicsAdder.AddFileBehavior = AddFileBehavior.AlwaysCopy;
            mTileGraphicsAdder.Add("TiledPluginCore/EmbeddedCodeFiles/AnimationChainContainer.cs");
            mTileGraphicsAdder.Add("TiledPluginCore/EmbeddedCodeFiles/LayeredTileMap.cs");
            mTileGraphicsAdder.Add("TiledPluginCore/EmbeddedCodeFiles/LayeredTileMapAnimation.cs");
            mTileGraphicsAdder.Add("TiledPluginCore/EmbeddedCodeFiles/MapDrawableBatch.cs");
            mTileGraphicsAdder.Add("TiledPluginCore/EmbeddedCodeFiles/Tileset.cs");
            mTileGraphicsAdder.Add("TiledPluginCore/EmbeddedCodeFiles/ReducedTileMapInfo.cs");
            mTileGraphicsAdder.Add("TiledPluginCore/EmbeddedCodeFiles/TileAnimationFrame.cs");
            mTileGraphicsAdder.Add("TiledPluginCore/EmbeddedCodeFiles/TileNodeNetworkCreator.cs");



            mTileGraphicsAdder.Add("TiledPluginCore/EmbeddedCodeFiles/AbstractMapLayer.cs");
            mTileGraphicsAdder.Add("TiledPluginCore/EmbeddedCodeFiles/ExternalTileset.cs");
            mTileGraphicsAdder.Add("TiledPluginCore/EmbeddedCodeFiles/MapLayer.cs");
            mTileGraphicsAdder.Add("TiledPluginCore/EmbeddedCodeFiles/MapTileset.cs");
            mTileGraphicsAdder.Add("TiledPluginCore/EmbeddedCodeFiles/MapTilesetTile.cs");
            mTileGraphicsAdder.Add("TiledPluginCore/EmbeddedCodeFiles/NamedValue.cs");
            mTileGraphicsAdder.Add("TiledPluginCore/EmbeddedCodeFiles/TiledMapSave.Conversion.cs");
            mTileGraphicsAdder.Add("TiledPluginCore/EmbeddedCodeFiles/TiledMapSave.Serialization.cs");
            mTileGraphicsAdder.Add("TiledPluginCore/EmbeddedCodeFiles/TileAnimation.cs");
            mTileGraphicsAdder.Add("TiledPluginCore/EmbeddedCodeFiles/TiledMapToShapeCollectionConverter.cs");
            mTileGraphicsAdder.Add("TiledPluginCore/EmbeddedCodeFiles/TilesetExtensionMethods.cs");
            // Sept 5, 2022 - why is this being added 2x?
            //mTileGraphicsAdder.Add("TiledPluginCore/EmbeddedCodeFiles/TilesetExtensionMethods.cs");
            mTileGraphicsAdder.Add("TiledPluginCore/EmbeddedCodeFiles/ReducedTileMapInfo.TiledMapSave.cs");

            


            mTileCollisionAdder = new CodeBuildItemAdder();
            mTileCollisionAdder.OutputFolderInProject = "TileCollisions";
            mTileCollisionAdder.AddFileBehavior = AddFileBehavior.AlwaysCopy;
            mTileCollisionAdder.Add("TiledPluginCore/EmbeddedCodeFiles/TileShapeCollection.cs");
            mTileCollisionAdder.Add("TiledPluginCore/EmbeddedCodeFiles/CollidableListVsTileShapeCollectionRelationship.cs");
            mTileCollisionAdder.Add("TiledPluginCore/EmbeddedCodeFiles/CollidableVsTileShapeCollectionRelationship.cs");
            mTileCollisionAdder.Add("TiledPluginCore/EmbeddedCodeFiles/CollisionManagerTileShapeCollectionExtensions.cs");

            mTileEntityAdder = new CodeBuildItemAdder();
            mTileEntityAdder.OutputFolderInProject = "TileEntities";
            mTileEntityAdder.AddFileBehavior = AddFileBehavior.AlwaysCopy;
            mTileEntityAdder.Add("TiledPluginCore/EmbeddedCodeFiles/TileEntityInstantiator.cs");
        }


        public void UpdateCodePresenceInProject()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            mTileGraphicsAdder.PerformAddAndSaveTask(assembly);
            mTileCollisionAdder.PerformAddAndSaveTask(assembly);
            mTileEntityAdder.PerformAddAndSaveTask(assembly);
        }

        internal void RefreshAppendGenerated()
        {
            var addAsGenerated = GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.AllTiledFilesGenerated;
            mTileGraphicsAdder.AddAsGenerated = addAsGenerated;
            mTileCollisionAdder.AddAsGenerated = addAsGenerated;
            mTileEntityAdder.AddAsGenerated = addAsGenerated;


        }
    }



}
