using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.RuntimeObjects.File
{

    public class LoadedFile
    {
        public ReferencedFileSave ReferencedFileSave { get; set; }
        public FilePath FilePath { get; set; }
        public object RuntimeObject { get; set; }

        public override string ToString()
        {
            return ReferencedFileSave?.Name;
        }
    }

    public abstract class RuntimeFileManager
    {
        public abstract void Activity(ICollection<LoadedFile> allFileObjects);

        public LoadedFile TryCreateFile(ReferencedFileSave file, IElement container)
        {
            FilePath filePath =
                ElementRuntime.ContentDirectory + file.Name;


                var runtimeObject = Load(filePath);

                if(runtimeObject != null)
                {
                    var loadedFile = new LoadedFile();
                    loadedFile.FilePath = filePath;
                    loadedFile.ReferencedFileSave = file;
                    loadedFile.RuntimeObject = runtimeObject;

                    return loadedFile;
                }
                else
                {
                    return null;
                }

        }

        protected abstract object Load(FilePath filePath);

        public abstract bool AddToManagers(LoadedFile loadedFile);

        public abstract void RemoveFromManagers(ICollection<LoadedFile> allFileObjects);

        public abstract bool TryDestroy(LoadedFile runtimeFileObject, ICollection<LoadedFile> allFileObjects);

        public abstract bool DestroyRuntimeObject(object runtimeObject);

        public abstract object TryGetCombinedObjectByName(string name);

        public abstract bool TryHandleRefreshFile(FilePath fileName, List<LoadedFile> allFileObjects);
        public abstract object CreateEmptyObjectMatchingArgumentType(object originalObject);
    }
}
