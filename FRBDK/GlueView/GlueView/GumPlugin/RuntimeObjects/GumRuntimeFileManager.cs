using FlatRedBall;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.RuntimeObjects.File;
using FlatRedBall.Gum;
using GlueView.Facades;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumPlugin.RuntimeObjects
{
    public class GumRuntimeFileManager : IRuntimeFileManager
    {
        GumIdb gumIdb;

        public void SubscribeToFrbWindowResize()
        {
            FlatRedBallServices.GraphicsOptions.SizeOrOrientationChanged += HandleWindowResize;
        }

        private void HandleWindowResize(object sender, EventArgs e)
        {
            // early out:

            if (GlueViewState.Self?.CurrentGlueProject == null)
            {
                return;
            }

            // end early out

            var newHeight = FlatRedBallServices.GraphicsOptions.ResolutionHeight;
            var newWidth = FlatRedBallServices.GraphicsOptions.ResolutionWidth;

            GraphicalUiElement.CanvasHeight = newHeight;
            GraphicalUiElement.CanvasWidth = newWidth;

            var gumCamera =
                RenderingLibrary.SystemManagers.Default.Renderer.Camera;

            var displaySettings = GlueViewState.Self.CurrentGlueProject?.DisplaySettings;

            var canvasAspectRatio = (float)newWidth / newHeight;

            // If we have a fixed aspect ratio, offset the camera


            if (displaySettings != null)
            {
                if(displaySettings.FixedAspectRatio == false || displaySettings.Is2D == false)
                {
                    gumCamera.Zoom =
                        newHeight / displaySettings.ResolutionHeight;
}
                else
                {

                    var displaySettingsAspectRatio = (float)
                        (displaySettings.AspectRatioWidth / displaySettings.AspectRatioHeight);

                    var shouldOffset = displaySettings.GenerateDisplayCode && displaySettings.FixedAspectRatio &&
                        displaySettingsAspectRatio != canvasAspectRatio;

                    if(canvasAspectRatio > displaySettingsAspectRatio)
                    {
                        var desiredWidthFromSettings = newHeight *
                            displaySettingsAspectRatio;

                        var extraWidth = newWidth - desiredWidthFromSettings;

                        var offset = extraWidth / 2.0f;

                        gumCamera.Zoom =
                            newHeight / (float)displaySettings.ResolutionHeight; // todo - replace this with the camera settings from Glue

                        gumCamera.Position.X = -offset / gumCamera.Zoom;
                        gumCamera.Position.Y = 0;
                    }
                    else
                    {
                        var desiredHeightFromSettings = newWidth /
                            displaySettingsAspectRatio;

                        var extraHeight = newHeight - desiredHeightFromSettings;

                        var offset = extraHeight / 2.0f;

                        gumCamera.Zoom =
                            newWidth / (float)displaySettings.ResolutionWidth; // todo - replace this with the camera settings from Glue

                        gumCamera.Position.X = 0;
                        gumCamera.Position.Y = -offset / gumCamera.Zoom;

                    }


                }



            }
            gumIdb?.Element?.UpdateLayout();
        }

        public void Activity(ICollection<object> allFileObjects)
        {

        }

        public void Destroy(ICollection<object> allFileObjects)
        {
            foreach(var fileObject in allFileObjects)
            {
                if(fileObject is GumIdb)
                {
                    ((GumIdb)fileObject).Destroy();
                }
            }

            gumIdb = null;
        }

        public object TryCreateFile(FlatRedBall.Glue.SaveClasses.ReferencedFileSave file, FlatRedBall.Glue.SaveClasses.IElement container)
        {
            var filePath = GlueViewCommands.Self.FileCommands.GetAbsoluteFileName(file);
            return TryCreateFileInternal(filePath);
        }

        private object TryCreateFileInternal(FilePath fileName)
        {
            var extension = fileName.Extension;
            if (extension == "gusx")
            {
                var rfs = GlueViewState.Self.GetAllReferencedFiles().FirstOrDefault(item => item.Name.ToLowerInvariant().EndsWith(".gumx"));
                if (rfs != null)
                {
                    var absoluteFileName = GlueViewCommands.Self.FileCommands.GetAbsoluteFileName(rfs);
                    GumIdb.StaticInitialize(absoluteFileName.Standardized);

                    var contentManagerWrapper = new FlatRedBall.Gum.ContentManagerWrapper();
                    contentManagerWrapper.ContentManagerName = "test";
                    RenderingLibrary.Content.LoaderManager.Self.ContentLoader = contentManagerWrapper;

                    gumIdb = new GumIdb();

                    string elementFileName = fileName.Standardized;
                    gumIdb.LoadFromFile(elementFileName);
                    gumIdb.AssignReferences();
                    gumIdb.InstanceInitialize();
                    gumIdb.Element.UpdateLayout();

                    // Handled in StaticInit
                    //SpriteManager.AddDrawableBatch(gumIdb);

                    return gumIdb;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public bool TryDestroy(object runtimeFileObject, ICollection<object> allFileObjects)
        {
            if(allFileObjects.Contains(runtimeFileObject))
            {
                if(runtimeFileObject is GumIdb)
                {
                    if(runtimeFileObject == gumIdb)
                    {
                        gumIdb = null;
                    }
                    ((GumIdb)runtimeFileObject).Destroy();
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

            var shouldRefresh = false;

            var extension = FlatRedBall.IO.FileManager.GetExtension(fileName);

            var isGumFile = extension == Gum.DataTypes.GumProjectSave.ScreenExtension ||
                extension == Gum.DataTypes.GumProjectSave.ComponentExtension ||
                extension == Gum.DataTypes.GumProjectSave.StandardExtension;

            if(isGumFile)
            {

                // For now we only refresh if it's a gum file. Eventually
                // we may want to refresh if any referenced file is changed.
                if (gumIdb != null)
                {
                    string fullFileName = GetGumIdbFullFileName();

                    shouldRefresh = fullFileName == filePath;

                    if(!shouldRefresh)
                    {
                        // See if the file is referenced by the GumIDB
                        var referencedFiles = 
                            Gum.Managers.ObjectFinder.Self
                            .GetFilesReferencedBy(gumIdb.Element.ElementSave)
                            .Select( item =>new FilePath(item));

                        shouldRefresh = referencedFiles.Any(item => item == fileName);
                    }
                }


                if (shouldRefresh)
                {
                    gumIdb.Destroy();
                    allFileObjects.Remove(gumIdb);

                    string fullFileName = GetGumIdbFullFileName();


                    allFileObjects.Add(TryCreateFileInternal(fullFileName));
                }

            }

            return shouldRefresh;
        }

        private string GetGumIdbFullFileName()
        {
            var rfs = GlueViewState.Self.GetAllReferencedFiles().FirstOrDefault(item => item.Name.ToLowerInvariant().EndsWith(".gumx"));
            var fullFile = GlueViewCommands.Self.FileCommands.GetAbsoluteFileName(rfs);
            var gumFolder = fullFile.GetDirectoryContainingThis();

            string subFolder = null;
            string extension = null;
            var element = gumIdb.Element.ElementSave;

            if (element is Gum.DataTypes.ScreenSave)
            {
                subFolder = Gum.DataTypes.ElementReference.ScreenSubfolder;
                extension = Gum.DataTypes.GumProjectSave.ScreenExtension;
            }
            else if (element is Gum.DataTypes.ComponentSave)
            {
                subFolder = Gum.DataTypes.ElementReference.ComponentSubfolder;
                extension = Gum.DataTypes.GumProjectSave.ComponentExtension;
            }

            var fullFileName = gumFolder + subFolder + "\\" + element.Name + "." + extension;
            return fullFileName;
        }
    }
}
