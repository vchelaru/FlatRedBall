using FlatRedBall;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.RuntimeObjects.File;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.TileCollisions;
using FlatRedBall.TileGraphics;
using GlueView.Facades;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMXGlueLib;

namespace TiledPlugin.RuntimeObjects
{
    public class TiledRuntimeFileManager : RuntimeFileManager
    {
        public override void Activity(ICollection<LoadedFile> allFileObjects)
        {

        }

        public override void RemoveFromManagers(ICollection<LoadedFile> allFileObjects)
        {
            foreach(var fileObject in allFileObjects)
            {
                if(fileObject.RuntimeObject is LayeredTileMap)
                {
                    ((LayeredTileMap)fileObject.RuntimeObject).Destroy();
                }
            }
        }

        protected override void Load(FlatRedBall.Glue.IO.FilePath fileName, out object runtimeObjects, out object dataModel)
        {
            bool shouldCreate = fileName.Extension == "tmx";

            if (shouldCreate && fileName.Exists())
            {
                //var layeredTileMap = new LayeredTileMap();
                // todo load it
                string contentManagerName = nameof(TiledRuntimeFileManager);

                var tiledMapSave = TiledMapSave.FromFile(fileName.FullPath);

                var layeredTileMap =
                    LayeredTileMap.FromTiledMapSave(fileName.FullPath, contentManagerName, tiledMapSave);

                layeredTileMap.Name = fileName.FullPath;

                runtimeObjects = layeredTileMap;
                dataModel = tiledMapSave;
            }
            else
            {

                runtimeObjects = null;
                dataModel = null;
            }
        }

        public override bool AddToManagers(LoadedFile loadedFile)
        {
            if(loadedFile.RuntimeObject is LayeredTileMap)
            {
                ((LayeredTileMap)loadedFile.RuntimeObject).AddToManagers();
                return true;
            }
            else
            {
                return false;
            }
        }

        private static FlatRedBall.Glue.SaveClasses.ReferencedFileSave GetRfsFromFile(FilePath fileName)
        {
            return GlueViewState.Self.GetAllReferencedFiles()
                .FirstOrDefault(item =>
                    GlueViewCommands.Self.FileCommands.GetAbsoluteFileName(item) == fileName);
        }

        public override bool TryDestroy(LoadedFile runtimeFileObject, ICollection<LoadedFile> allFileObjects)
        {
            if (allFileObjects.Contains(runtimeFileObject))
            {
                var runtimeObject = runtimeFileObject.RuntimeObject;

                return DestroyRuntimeObject(runtimeObject);
            }
            return false;
        }

        public override bool DestroyRuntimeObject(object runtimeObject)
        {
            if (runtimeObject is LayeredTileMap)
            {
                ((LayeredTileMap)runtimeObject).Destroy();
                return true;
            }
            return false;
        }

        public override object TryGetObjectFromFile(ICollection<LoadedFile> allFileObjects, ReferencedFileSave rfs, string objectType, string objectName)
        {
            var layeredTileMap = allFileObjects.FirstOrDefault(item => item.ReferencedFileSave == rfs)
                ?.RuntimeObject as LayeredTileMap;



            object toReturn = null;
            if(layeredTileMap != null)
            {
                switch(objectType)
                {
                    case "FlatRedBall.TileCollisions.TileShapeCollection":
                    case "TileShapeCollection":
                        var tileShapeCollection =
                            layeredTileMap.Collisions.FirstOrDefault(item => item.Name == objectName);
                        if(tileShapeCollection != null)
                        {
                            // temporary: as of January 24, 2019 Glue doesn't allow setting properties on objects
                            // that come from-file. Therefore we can't have a Visible property in Glue that can be toggled
                            // off/on to make this visible in GView. Some users may want shape collections to show in GView
                            // so we'll just have to make them show no matter what for now:
                            tileShapeCollection.Visible = true;

                        }
                        toReturn = tileShapeCollection;

                        break;
                }
            }

            return toReturn;
        }

        public override bool TryHandleRefreshFile(FilePath fileName, List<LoadedFile> loadedFiles, List<LoadedFile> addedFiles)
        {
            var extension = fileName.Extension;
            var wasHandled = false;
            if(extension == "tmx")
            {
                wasHandled = TryHandleTmxRefresh(fileName, loadedFiles, addedFiles);
            }
            else if(extension == "png")
            {
                // We never say "handled = true" for PNGs since others might want to handle it too
                TryHandlePngRefresh(fileName, loadedFiles);
            }
            else if(extension == "tsx")
            {
                wasHandled = TryHandleTsxRefresh(fileName, loadedFiles, addedFiles);
            }
            return wasHandled;
        }

        private bool TryHandleTsxRefresh(FilePath tsxFileName, List<LoadedFile> loadedFiles, List<LoadedFile> addedFiles)
        {
            foreach(var loadedFile in loadedFiles.Where(item =>item.DataModel is TiledMapSave))
            {
                var tiledMapSave = loadedFile.DataModel as TiledMapSave;

                var tmxDirectory = loadedFile.FilePath.GetDirectoryContainingThis();

                if(tiledMapSave != null)
                {
                    foreach(var tileset in tiledMapSave.Tilesets)
                    {
                        var path = tmxDirectory + tileset.Source;

                        if(path == tsxFileName)
                        {
                            if(loadedFile.RuntimeObject is LayeredTileMap)
                            {
                                RefreshTmx(loadedFile);
                            }
                            break;
                        }
                    }
                }
            }

            foreach (var addedFile in addedFiles.Where(item =>  item.DataModel is TiledMapSave && item.RuntimeObject is TileShapeCollection))
            {
                var tiledMapSave = addedFile.DataModel as TiledMapSave;

                var tmxDirectory = addedFile.FilePath.GetDirectoryContainingThis();

                if (tiledMapSave != null)
                {
                    foreach (var tileset in tiledMapSave.Tilesets)
                    {
                        var path = tmxDirectory + tileset.Source;

                        if (path == tsxFileName)
                        {
                            RefreshTileShapeCollection(addedFile, loadedFiles);
                            break;
                        }
                    }
                }
            }
            return true;
        }

        private static void TryHandlePngRefresh(FilePath fileName, List<LoadedFile> allFileObjects)
        {
            var mapLayersReferencingTexture = new List<MapDrawableBatch>();

            // see if this is referenced by any of the existing TMX's
            foreach (var tmxLoadedFile in allFileObjects.Where(item => item.FilePath.Extension == "tmx"))
            {
                var runtimeObject = tmxLoadedFile.RuntimeObject as LayeredTileMap;

                foreach (var mapLayer in runtimeObject.MapLayers)
                {
                    var referencesTexture = mapLayer.Texture.Name == fileName;

                    if (referencesTexture)
                    {
                        mapLayersReferencingTexture.Add(mapLayer);
                    }
                }
            }

            if (mapLayersReferencingTexture.Count > 0)
            {
                FlatRedBallServices.Unload(nameof(TiledRuntimeFileManager));

                var newTexture = FlatRedBallServices.Load<Texture2D>(fileName.FullPath, nameof(TiledRuntimeFileManager));
                // unload the content manager so that we can re-create the files:
                foreach (var layer in mapLayersReferencingTexture)
                {
                    layer.Texture = newTexture;
                }
                // even though we may have handled it, we don't want to return true because
                // other plugins may reload this file too
            }
        }

        private bool TryHandleTmxRefresh(FilePath fileName, List<LoadedFile> loadedFiles, List<LoadedFile> addedFiles)
        {
            bool wasHandled = false;
            foreach (var item in loadedFiles.Where(item => item.FilePath == fileName))
            {
                RefreshTmx(item);

                wasHandled = true;
            }


            foreach (var addedFile in addedFiles.Where(item => 
                item.FilePath == fileName && 
                item.DataModel is TiledMapSave &&
                item.RuntimeObject is TileShapeCollection))
            {
                var tiledMapSave = addedFile.DataModel as TiledMapSave;

                var tmxDirectory = addedFile.FilePath.GetDirectoryContainingThis();

                RefreshTileShapeCollection(addedFile, loadedFiles);
            }

            return wasHandled;
        }

        private void RefreshTmx(LoadedFile item)
        {
            ((LayeredTileMap)item.RuntimeObject).Destroy();
            LayeredTileMap newMap;
            TiledMapSave tiledMapSave;

            object newMapAsObject;
            object tiledMapSaveAsObject;

            Load(item.FilePath, out newMapAsObject, out tiledMapSaveAsObject);
            newMap = (LayeredTileMap)newMapAsObject;
            tiledMapSave = (TiledMapSave)tiledMapSaveAsObject;

            newMap?.AddToManagers();
            item.RuntimeObject = newMap;
            item.DataModel = tiledMapSaveAsObject;
        }

        private void RefreshTileShapeCollection(LoadedFile tileShapeCollectionLoadedFile, List<LoadedFile> loadedFiles)
        {
            var oldTileShapeCollection =
                ((TileShapeCollection)tileShapeCollectionLoadedFile.RuntimeObject);
            // find the loaded file that has the runtime LayeredTileMap and adjust the runtime:
            oldTileShapeCollection.RemoveFromManagers();

            var layeredTileMapLoadedFile = loadedFiles
                .FirstOrDefault(item => item.FilePath == item.FilePath && item.RuntimeObject is LayeredTileMap);

            if(layeredTileMapLoadedFile != null)
            {
                var layeredTileMap = layeredTileMapLoadedFile.RuntimeObject as LayeredTileMap;

                var newTileMap =
                    layeredTileMap.Collisions.FirstOrDefault(item => item.Name == oldTileShapeCollection.Name);

                tileShapeCollectionLoadedFile.RuntimeObject = newTileMap;
                newTileMap.Visible = true;
            }
        }

        public override object CreateEmptyObjectMatchingArgumentType(object originalObject)
        {
            if(originalObject is LayeredTileMap)
            {
                //throw new NotImplementedException();
            }
            return null;
        }
    }
}
