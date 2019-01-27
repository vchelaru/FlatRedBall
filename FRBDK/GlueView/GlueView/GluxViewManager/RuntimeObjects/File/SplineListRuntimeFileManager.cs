using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Math.Splines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.RuntimeObjects.File
{
    public class SplineListRuntimeFileManager : RuntimeFileManager
    {
        public override void Activity(ICollection<LoadedFile> allFileObjects)
        {
        }

        public override bool AddToManagers(LoadedFile loadedFile)
        {
            return false;
        }

        public override object CreateEmptyObjectMatchingArgumentType(object originalObject)
        {
            if (originalObject is SplineList)
            {
                return new SplineList();
            }
            return null;
        }

        public override bool DestroyRuntimeObject(object runtimeObject)
        {
            return false;
        }

        public override void RemoveFromManagers(ICollection<LoadedFile> allFileObjects)
        {
        }

        public override bool TryDestroy(LoadedFile runtimeFileObject, ICollection<LoadedFile> allFileObjects)
        {
            return false;
        }

        public override object TryGetObjectFromFile(ICollection<LoadedFile> allFileObjects, ReferencedFileSave rfs, string objectType, string objectName)
        {
            return null;
        }

        public override bool TryHandleRefreshFile(FilePath fileName, List<LoadedFile> allFileObjects)
        {
            return false;
        }

        protected override void Load(FilePath filePath, out object runtimeObjects, out object dataModel)
        {
            runtimeObjects = null;
            dataModel = null;
        }
    }
}
