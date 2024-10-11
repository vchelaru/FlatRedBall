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

            mTileGraphicsAdder.Add("TiledPlugin/EmbeddedCodeFiles/AnimationChainContainer.cs");
            mTileGraphicsAdder.Add("TiledPlugin/EmbeddedCodeFiles/LayeredTileMap.cs");
            mTileGraphicsAdder.Add("TiledPlugin/EmbeddedCodeFiles/LayeredTileMapAnimation.cs");
            mTileGraphicsAdder.Add("TiledPlugin/EmbeddedCodeFiles/MapDrawableBatch.cs");
            mTileGraphicsAdder.Add("TiledPlugin/EmbeddedCodeFiles/Tileset.cs");
            mTileGraphicsAdder.Add("TiledPlugin/EmbeddedCodeFiles/ReducedTileMapInfo.cs");
            mTileGraphicsAdder.Add("TiledPlugin/EmbeddedCodeFiles/TileAnimationFrame.cs");
            mTileGraphicsAdder.Add("TiledPlugin/EmbeddedCodeFiles/TileNodeNetworkCreator.cs");



            mTileGraphicsAdder.Add("TiledPlugin/EmbeddedCodeFiles/AbstractMapLayer.cs");
            mTileGraphicsAdder.Add("TiledPlugin/EmbeddedCodeFiles/ExternalTileset.cs");
            mTileGraphicsAdder.Add("TiledPlugin/EmbeddedCodeFiles/MapLayer.cs");
            mTileGraphicsAdder.Add("TiledPlugin/EmbeddedCodeFiles/MapTileset.cs");
            mTileGraphicsAdder.Add("TiledPlugin/EmbeddedCodeFiles/MapTilesetTile.cs");
            mTileGraphicsAdder.Add("TiledPlugin/EmbeddedCodeFiles/NamedValue.cs");
            mTileGraphicsAdder.Add("TiledPlugin/EmbeddedCodeFiles/TiledMapSave.Conversion.cs");
            mTileGraphicsAdder.Add("TiledPlugin/EmbeddedCodeFiles/TiledMapSave.Serialization.cs");
            mTileGraphicsAdder.Add("TiledPlugin/EmbeddedCodeFiles/TileAnimation.cs");
            mTileGraphicsAdder.Add("TiledPlugin/EmbeddedCodeFiles/TiledMapToShapeCollectionConverter.cs");
            mTileGraphicsAdder.Add("TiledPlugin/EmbeddedCodeFiles/TilesetExtensionMethods.cs");
            mTileGraphicsAdder.Add("TiledPlugin/EmbeddedCodeFiles/ReducedTileMapInfo.TiledMapSave.cs");

            


            mTileCollisionAdder = new CodeBuildItemAdder();
            mTileCollisionAdder.OutputFolderInProject = "TileCollisions";
            mTileCollisionAdder.AddFileBehavior = AddFileBehavior.AlwaysCopy;
            mTileCollisionAdder.Add("TiledPlugin/EmbeddedCodeFiles/TileShapeCollection.cs");
            mTileCollisionAdder.Add("TiledPlugin/EmbeddedCodeFiles/CollidableListVsTileShapeCollectionRelationship.cs");
            mTileCollisionAdder.Add("TiledPlugin/EmbeddedCodeFiles/CollidableVsTileShapeCollectionRelationship.cs");
            mTileCollisionAdder.Add("TiledPlugin/EmbeddedCodeFiles/CollisionManagerTileShapeCollectionExtensions.cs");

            mTileEntityAdder = new CodeBuildItemAdder();
            mTileEntityAdder.OutputFolderInProject = "TileEntities";
            mTileEntityAdder.AddFileBehavior = AddFileBehavior.AlwaysCopy;
            mTileEntityAdder.Add("TiledPlugin/EmbeddedCodeFiles/TileEntityInstantiator.cs");
        }


        public void UpdateCodePresenceInProject()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            mTileGraphicsAdder.PerformAddAndSaveTask(assembly, nameof(mTileGraphicsAdder));
            mTileCollisionAdder.PerformAddAndSaveTask(assembly, nameof(mTileCollisionAdder));
            mTileEntityAdder.PerformAddAndSaveTask(assembly, nameof(mTileEntityAdder));
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
