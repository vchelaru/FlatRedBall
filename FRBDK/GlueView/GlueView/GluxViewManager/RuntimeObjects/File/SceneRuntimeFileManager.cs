using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Graphics;
using FlatRedBall.IO;
using FlatRedBall.Localization;

namespace FlatRedBall.Glue.RuntimeObjects.File
{
    public class SceneRuntimeFileManager : RuntimeFileManager
    {

        public override void Activity(ICollection<LoadedFile> allFileObjects)
        {
            foreach (var fileObject in allFileObjects)
            {
                if (fileObject.RuntimeObject is Scene)
                {
                    ((Scene)fileObject.RuntimeObject).ManageAll();
                }
            }
        }

        public override void RemoveFromManagers(ICollection<LoadedFile> allFileObjects)
        {
            foreach (var fileObject in allFileObjects)
            {
                if (fileObject.RuntimeObject is Scene)
                {
                    ((Scene)fileObject.RuntimeObject).RemoveFromManagers();
                }
            }
        }

        public override bool TryDestroy(LoadedFile runtimeFileObject, ICollection<LoadedFile> allFileObjects)
        {
            var runtimeObject = runtimeFileObject.RuntimeObject;
            return DestroyRuntimeObject(runtimeObject);
        }

        public override bool DestroyRuntimeObject(object runtimeObject)
        {
            if (runtimeObject is Scene)
            {
                ((Scene)runtimeObject).RemoveFromManagers();
                return true;
            }
            else
            {
                return false;
            }
        }

        public override object TryGetObjectFromFile(ICollection<LoadedFile> allFileObjects, ReferencedFileSave rfs, string objectType, string objectName)
        {
            return null;
        }

        protected override void Load(FilePath filePath, out object runtimeObjects, out object dataModel)
        {
            dataModel = null;
            Scene newScene = null;
            try
            {
                if(filePath.Extension == "scnx")
                {
                    newScene = FlatRedBallServices.Load<Scene>(filePath.FullPath,
                        GluxManager.ContentManagerName);

                    foreach (Text text in newScene.Texts)
                    {
                        text.AdjustPositionForPixelPerfectDrawing = true;
                        if (ObjectFinder.Self.GlueProject.UsesTranslation)
                        {
                            text.DisplayText = LocalizationManager.Translate(text.DisplayText);
                        }
                    }

                    runtimeObjects = newScene;
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error loading Scene file " + ElementRuntime.ContentDirectory + filePath.FullPath + e.ToString());
            }
            runtimeObjects = newScene;
        }

        public override bool AddToManagers(LoadedFile loadedFile)
        {
            var scene = loadedFile.RuntimeObject as Scene;
            if(scene != null)
            {
                scene?.AddToManagers();
                return true;
            }
            return false;
        }

        public override bool TryHandleRefreshFile(FilePath fileName, List<LoadedFile> allFileObjects)
        {
            // do nothing for now...ever? .scnx is going out of style
            return false;
        }

        public override object CreateEmptyObjectMatchingArgumentType(object originalObject)
        {
            if(originalObject is Scene)
            {
                return new Scene();
            }
            else
            {
                return null;
            }
        }
    }
}
