using FlatRedBall;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.RuntimeObjects.File;
using FlatRedBall.Glue.SaveClasses;
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
    public class GumRuntimeFileManager : RuntimeFileManager
    {
        GumIdb gumIdb;

        public void SubscribeToFrbWindowResize()
        {
            FlatRedBallServices.GraphicsOptions.SizeOrOrientationChanged += HandleWindowResize;
        }

        private void HandleWindowResize(object sender, EventArgs e)
        {
            // early out:

            if (GlueViewState.Self?.CurrentGlueProject == null ||
                RenderingLibrary.SystemManagers.Default == null)
            {
                return;
            }

            // end early out

            var graphicsOptions = FlatRedBallServices.GraphicsOptions;

            var newHeight = graphicsOptions.ResolutionHeight;
            var newWidth = graphicsOptions.ResolutionWidth;

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

        public override void Activity(ICollection<LoadedFile> allFileObjects)
        {

        }


        protected override void Load(FilePath referencedFilePath, out object runtimeObjects, out object dataModel)
        {
            var extension = referencedFilePath.Extension;
            if (extension == "gusx")
            {
                var gumReferencedFile = GlueViewState.Self
                    .GetAllReferencedFiles()
                    .FirstOrDefault(item => item.Name.ToLowerInvariant().EndsWith(".gumx"));
                var absoluteGumxFileName = GlueViewCommands.Self.FileCommands.GetAbsoluteFileName(gumReferencedFile);

                GumIdb.StaticInitialize(absoluteGumxFileName.FullPath);

                var contentManagerWrapper = new FlatRedBall.Gum.ContentManagerWrapper();
                contentManagerWrapper.ContentManagerName = "test";
                RenderingLibrary.Content.LoaderManager.Self.ContentLoader = contentManagerWrapper;

                gumIdb = new GumIdb();

                string elementFileName = referencedFilePath.Standardized;
                gumIdb.LoadFromFile(elementFileName);
                gumIdb.AssignReferences();
                gumIdb.Element.UpdateLayout();

                // Handled in StaticInit
                //SpriteManager.AddDrawableBatch(gumIdb);

                runtimeObjects = gumIdb;
                dataModel = gumIdb.Element;
            }
            else
            {
                runtimeObjects = null;
                dataModel = null;
            }
        }

        public override bool AddToManagers(LoadedFile loadedFile)
        {
            var runtimeObject = loadedFile.RuntimeObject;

            var gumIdb = runtimeObject as GumIdb;
            if(gumIdb != null)
            {
                //gumIdb.InstanceInitialize();
                gumIdb.Element.AddToManagers();
                return true;
            }
            return false;
        }

        public override void RemoveFromManagers(ICollection<LoadedFile> allFileObjects)
        {
            foreach(var fileObject in allFileObjects)
            {
                if(fileObject.RuntimeObject is GumIdb)
                {
                    var gumIdb = fileObject.RuntimeObject as GumIdb;
                    FlatRedBall.SpriteManager.RemoveDrawableBatch(gumIdb);

                    gumIdb.Element.RemoveFromManagers();
                }
            }

            gumIdb = null;
        }



        public override bool TryDestroy(LoadedFile runtimeFileObject, ICollection<LoadedFile> allFileObjects)
        {
            if(allFileObjects.Contains(runtimeFileObject))
            {
                var runtimeObject = runtimeFileObject.RuntimeObject;

                return DestroyRuntimeObject(runtimeObject);
            }
            return false;
        }

        public override bool DestroyRuntimeObject(object runtimeObject)
        {
            if (runtimeObject is GumIdb)
            {
                if (runtimeObject == gumIdb)
                {
                    gumIdb = null;
                }
                ((GumIdb)runtimeObject).Destroy();
                return false;
            }

            return true;
        }

        public override object TryGetObjectFromFile(ICollection<LoadedFile> allFileObjects, ReferencedFileSave rfs, string objectType, string objectName)
        {
            return null;
        }

        public override bool TryHandleRefreshFile(FilePath filePath, List<LoadedFile> allFileObjects)
        {
            var shouldRefresh = false;

            var extension = filePath.Extension;

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

                        shouldRefresh = referencedFiles.Any(item => item == filePath);
                    }
                }


                if (shouldRefresh)
                {
                    gumIdb.Destroy();
                    allFileObjects.RemoveAll(item =>item.RuntimeObject == gumIdb);

                    string fullFileName = GetGumIdbFullFileName();
                    var existingLoadedFile = allFileObjects.FirstOrDefault(item => item.FilePath == fullFileName);

                    object gumIdbAsObject;
                    object dataModel;
                    Load(fullFileName, out gumIdbAsObject, out dataModel);
                        
                    gumIdb = gumIdbAsObject as GumIdb;

                    gumIdb?.Element.AddToManagers();

                    if (existingLoadedFile != null)
                    {
                        existingLoadedFile.RuntimeObject = gumIdb;
                    }
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

        public override object CreateEmptyObjectMatchingArgumentType(object originalObject)
        {
            if(originalObject is GumIdb)
            {
                // I don't know if we support clones of these....well I suppose we might, so I should throw an exception:
                // Eventually we'll want to support this but as of this release I'm focused on
                // the new structure for file loading, and mot making these changes.
             //   throw new NotImplementedException();
            }
            return null;
        }
    }
}
