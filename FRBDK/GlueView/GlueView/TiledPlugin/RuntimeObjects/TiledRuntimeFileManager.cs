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
    public class TiledRuntimeFileManager : IRuntimeFileManager
    {
        public void Activity(ICollection<object> allFileObjects)
        {

        }

        public void Destroy(ICollection<object> allFileObjects)
        {
            foreach(var fileObject in allFileObjects)
            {
                if(fileObject is LayeredTileMap)
                {
                    ((LayeredTileMap)fileObject).Destroy();
                }
            }
        }

        public object TryCreateFile(FlatRedBall.Glue.SaveClasses.ReferencedFileSave file, FlatRedBall.Glue.SaveClasses.IElement container)
        {
            var filePath = GlueViewCommands.Self.FileCommands.GetAbsoluteFileName(file);
            return TryCreateFileInternal(filePath);
        }

        private object TryCreateFileInternal(FlatRedBall.Glue.IO.FilePath fileName)
        {
            var extension = fileName.Extension;

            if(extension == "tmx")
            {
                FlatRedBall.Glue.SaveClasses.ReferencedFileSave rfs = GetRfsFromFile(fileName);

                if (rfs != null)
                {
                    var absoluteFileName = GlueViewCommands.Self.FileCommands.GetAbsoluteFileName(rfs);

                    //var layeredTileMap = new LayeredTileMap();
                    // todo load it
                    string contentManagerName = nameof(TiledRuntimeFileManager);
                    var layeredTileMap = 
                        LayeredTileMap.FromTiledMapSave("content/screens/tiledscreen/tmxfile.tmx", contentManagerName);

                    layeredTileMap.Name = fileName.FullPath;

                    //todo - need to look at the layer it might be on
                    layeredTileMap.AddToManagers();
                    return layeredTileMap;
                    // create the tmx now


                }
            }

            return null;

        }

        private static FlatRedBall.Glue.SaveClasses.ReferencedFileSave GetRfsFromFile(FilePath fileName)
        {
            return GlueViewState.Self.GetAllReferencedFiles()
                .FirstOrDefault(item =>
                    GlueViewCommands.Self.FileCommands.GetAbsoluteFileName(item) == fileName);
        }

        public bool TryDestroy(object runtimeFileObject, ICollection<object> allFileObjects)
        {
            if (allFileObjects.Contains(runtimeFileObject))
            {
                if (runtimeFileObject is LayeredTileMap)
                {
                    ((LayeredTileMap)runtimeFileObject).Destroy();
                    return true;
                }
            }
            return false;
        }

        public object TryGetCombinedObjectByName(string name)
        {
            throw new NotImplementedException();
        }

        public bool TryHandleRefreshFile(string fileName, List<object> allFileObjects)
        {
            var filePath = new FilePath(fileName);


            var extension = FlatRedBall.IO.FileManager.GetExtension(fileName);

            var isTmx = extension == "tmx";

            if(isTmx)
            {
                FlatRedBall.Glue.SaveClasses.ReferencedFileSave rfs = GetRfsFromFile(fileName);

                if(rfs != null)
                {
                    var runtime = allFileObjects.FirstOrDefault(item => item is LayeredTileMap &&
                        ((LayeredTileMap)item).Name == filePath);

                    if(runtime != null)
                    {
                        ((LayeredTileMap)runtime).Destroy();
                        allFileObjects.Remove(runtime);
                        allFileObjects.Add(TryCreateFileInternal(filePath));

                    }
                    return true;
                }
            }
            return false;
        }
    }
}
