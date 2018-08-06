using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Graphics;
using FlatRedBall.IO;
using FlatRedBall.Localization;

namespace FlatRedBall.Glue.RuntimeObjects.File
{
    public class SceneRuntimeFileManager : IRuntimeFileManager
    {

        public void Activity(ICollection<object> allFileObjects)
        {
            foreach(var fileObject in allFileObjects)
            {
                if(fileObject is Scene)
                {
                    ((Scene)fileObject).ManageAll();
                }
            }
        }

        public void Destroy(ICollection<object> allFileObjects)
        {
            foreach (var fileObject in allFileObjects)
            {
                if (fileObject is Scene)
                {
                    ((Scene)fileObject).RemoveFromManagers();
                }
            }
        }

        public object TryCreateFile(ReferencedFileSave file, IElement container)
        {
            var extension = FileManager.GetExtension(file.Name);
            if(extension == "scnx")
            {
                var runtimeObject = LoadScnx(file, container);

                return runtimeObject;
            }
            else
            {
                return null;
            }
        }

        public bool TryDestroy(object runtimeFileObject, ICollection<object> allFileObjects)
        {
            if(runtimeFileObject is Scene && allFileObjects.Contains(runtimeFileObject))
            {
                ((Scene)runtimeFileObject).RemoveFromManagers();
                return true;
            }
            else
            {
                return false;
            }
        }

        public object TryGetCombinedObjectByName(string name)
        {
            throw new NotImplementedException();
        }

        static Scene LoadScnx(ReferencedFileSave r, IElement container)
        {
            Scene newScene = null;
            try
            {
                newScene = FlatRedBallServices.Load<Scene>(ElementRuntime.ContentDirectory + r.Name,
                    GluxManager.ContentManagerName);

                foreach (Text text in newScene.Texts)
                {
                    text.AdjustPositionForPixelPerfectDrawing = true;
                    if (ObjectFinder.Self.GlueProject.UsesTranslation)
                    {
                        text.DisplayText = LocalizationManager.Translate(text.DisplayText);
                    }
                }

                if (!r.IsSharedStatic || container is ScreenSave)
                {
                    newScene.AddToManagers();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error loading Scene file " + ElementRuntime.ContentDirectory + r.Name + e.ToString());
            }
            return newScene;
        }

        public bool TryHandleRefreshFile(string fileName, List<object> allFileObjects)
        {
            // do nothing for now...ever? .scnx is going out of style
            return false;
        }
    }
}
