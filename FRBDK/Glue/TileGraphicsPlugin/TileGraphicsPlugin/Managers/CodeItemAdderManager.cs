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
            mTileGraphicsAdder.AddFileBehavior = AddFileBehavior.IfOutOfDate;
            mTileGraphicsAdder.Add("TileGraphicsPlugin.AnimationChainContainer.cs");
            // Are we still supporting this?
            //mItemAdder.Add("TileGraphicsPlugin.CulledMapDrawableBatch.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin.LayeredTileMap.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin.LayeredTileMapAnimation.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin.MapDrawableBatch.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin.Tileset.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin.ReducedTileMapInfo.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin.TileAnimationFrame.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin.TileAnimationFrame.cs");

            

            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/AbstractMapLayer.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/ExternalTileset.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/MapLayer.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/MapTileset.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/MapTilesetTile.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/NamedValue.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/TiledMapSave.Conversion.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/TiledMapSave.Serialization.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/TileAnimation.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/TiledMapToShapeCollectionConverter.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/TilesetExtensionMethods.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/TilesetExtensionMethods.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/ReducedTileMapInfo.TiledMapSave.cs");

            


            mTileCollisionAdder = new CodeBuildItemAdder();
            mTileCollisionAdder.OutputFolderInProject = "TileCollisions";
            mTileCollisionAdder.AddFileBehavior = AddFileBehavior.IfOutOfDate;
            mTileCollisionAdder.Add("TileGraphicsPlugin.TileShapeCollection.cs");

            mTileEntityAdder = new CodeBuildItemAdder();
            mTileEntityAdder.OutputFolderInProject = "TileEntities";
            mTileEntityAdder.AddFileBehavior = AddFileBehavior.IfOutOfDate;
            mTileEntityAdder.Add("TileGraphicsPlugin.TileEntityInstantiator.cs");
        }


        public void UpdateCodeInProjectPresence()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            mTileGraphicsAdder.PerformAddAndSave(assembly);
            mTileCollisionAdder.PerformAddAndSave(assembly);
            mTileEntityAdder.PerformAddAndSave(assembly);
        }

    }



}
