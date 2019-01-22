using FlatRedBall;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.RuntimeObjects.File;
using FlatRedBall.TileGraphics;
using GlueView.Facades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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


        protected override object Load(FlatRedBall.Glue.IO.FilePath fileName)
        {
            bool shouldCreate = fileName.Extension == "tmx";

            if (shouldCreate && fileName.Exists())
            {
                //var layeredTileMap = new LayeredTileMap();
                // todo load it
                string contentManagerName = nameof(TiledRuntimeFileManager);
                var layeredTileMap =
                    LayeredTileMap.FromTiledMapSave(fileName.FullPath, contentManagerName);

                layeredTileMap.Name = fileName.FullPath;

                return layeredTileMap;
            }
            else
            {
                return null;
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

        public override object TryGetCombinedObjectByName(string name)
        {
            throw new NotImplementedException();
        }

        public override bool TryHandleRefreshFile(FilePath fileName, List<LoadedFile> allFileObjects)
        {
            var extension = fileName.Extension;
            var toReturn = false;
            if(extension == "tmx")
            {
                foreach(var item in allFileObjects.Where(item =>item.FilePath == fileName))
                {
                    ((LayeredTileMap)item.RuntimeObject).Destroy();
                    var newMap = (LayeredTileMap)Load(fileName);
                    newMap?.AddToManagers();
                    item.RuntimeObject = Load(fileName);

                    toReturn = true;
                }
            }

            return toReturn;
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
