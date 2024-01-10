using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.IO;
using GumPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GumPlugin.Managers
{
    public class EmbeddedResourceManager : Singleton<EmbeddedResourceManager>
    {

        Assembly mContextAssembly;
        string mContextDirectoryToSaveTo;


        CodeBuildItemAdder mStateInterpolationItemAdder;

        public void SaveEmptyProject(FilePath directoryToSaveProjectTo)
        {
            mContextAssembly = Assembly.GetExecutingAssembly();
            mContextDirectoryToSaveTo = directoryToSaveProjectTo.FullPath;

            SaveGumFile("GumProject.gumx");
            SaveGumFile("Standards/Circle.gutx");
            SaveGumFile("Standards/ColoredRectangle.gutx");
            SaveGumFile("Standards/Component.gutx");
            SaveGumFile("Standards/Container.gutx");
            SaveGumFile("Standards/NineSlice.gutx");
            SaveGumFile("Standards/Polygon.gutx");
            SaveGumFile("Standards/Rectangle.gutx");
            SaveGumFile("Standards/Sprite.gutx");
            SaveGumFile("Standards/Text.gutx");

            SaveGumFile("FontCache/Font18Arial.fnt");
            SaveGumFile("FontCache/Font18Arial_0.png");

            SaveGumFile("Standards/StandardGraphics/Red_BottomCenter.png");
            SaveGumFile("Standards/StandardGraphics/Red_BottomLeft.png");
            SaveGumFile("Standards/StandardGraphics/Red_BottomRight.png");
            SaveGumFile("Standards/StandardGraphics/Red_Center.png");
            SaveGumFile("Standards/StandardGraphics/Red_Left.png");
            SaveGumFile("Standards/StandardGraphics/Red_Right.png");
            SaveGumFile("Standards/StandardGraphics/Red_TopCenter.png");
            SaveGumFile("Standards/StandardGraphics/Red_TopLeft.png");
            SaveGumFile("Standards/StandardGraphics/Red_TopRight.png");

            
        }

        void SaveGumFile(string fileName)
        {
            string resourceName = "GumPluginCore/Embedded/EmptyProject/" + fileName;

            resourceName = resourceName.Replace("/", ".");

            FileManager.SaveEmbeddedResource(mContextAssembly, resourceName,
                 mContextDirectoryToSaveTo + fileName);

        }

        public void UpdateCodeInProjectPresence(FileAdditionBehavior behavior)
        {
            bool hasGumProject = AppState.Self.GumProjectSave != null;

            if (hasGumProject)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();

                UpdateCoreGumFilePresence(assembly, behavior);

                UpdateAdvancedStateInterpolationFiles(assembly, behavior);
            }
        }

        private void UpdateAdvancedStateInterpolationFiles(Assembly assembly, FileAdditionBehavior behavior)
        {

            mStateInterpolationItemAdder = new CodeBuildItemAdder();

            var prefix = "GumPluginCore/Embedded/StateInterpolation/";

            mStateInterpolationItemAdder.Add(prefix + "Back.cs");
            mStateInterpolationItemAdder.Add(prefix + "Bounce.cs");
            mStateInterpolationItemAdder.Add(prefix + "Circular.cs");
            mStateInterpolationItemAdder.Add(prefix + "Cubic.cs");
            mStateInterpolationItemAdder.Add(prefix + "Elastic.cs");
            mStateInterpolationItemAdder.Add(prefix + "Exponential.cs");
            mStateInterpolationItemAdder.Add(prefix + "Instant.cs");
            mStateInterpolationItemAdder.Add(prefix + "Linear.cs");
            mStateInterpolationItemAdder.Add(prefix + "Quadratic.cs");
            mStateInterpolationItemAdder.Add(prefix + "Quartic.cs");
            mStateInterpolationItemAdder.Add(prefix + "Quintic.cs");
            mStateInterpolationItemAdder.Add(prefix + "ShakeTweener.cs");
            mStateInterpolationItemAdder.Add(prefix + "Sinusoidal.cs");
            mStateInterpolationItemAdder.Add(prefix + "Tweener.cs");
            mStateInterpolationItemAdder.Add(prefix + "TweenerManager.cs");


            mStateInterpolationItemAdder.OutputFolderInProject = "StateInterpolation";
            if(behavior == FileAdditionBehavior.EmbedCodeFiles)
            {
                mStateInterpolationItemAdder.AddFileBehavior = AddFileBehavior.IfOutOfDate;

                mStateInterpolationItemAdder.PerformAddAndSaveTask(assembly);

            }
            else if(behavior == FileAdditionBehavior.IncludeNoFiles)
            {
                TaskManager.Self.Add(() =>
                {
                    mStateInterpolationItemAdder.PerformRemoveAndSave(assembly);

                }, "Removing standard Gum files");
            }
        }

        private void UpdateCoreGumFilePresence(Assembly assemblyContainingResources, FileAdditionBehavior behavior)
        {
            var codeItemAdder = GetGumCoreCodeItemAdder(assemblyContainingResources);

            if(behavior == FileAdditionBehavior.EmbedCodeFiles)
            {
                // April 14, 2017
                // Used to only copy
                // if out of date, but
                // this plugin is updated
                // so frequently, and if we
                // don't force copy, then starter
                // projects will always be out of date
                // because their modified date is newer
                // than the plugin.
                //mCodeAdder.AddFileBehavior = AddFileBehavior.IfOutOfDate;
                codeItemAdder.AddFileBehavior = AddFileBehavior.AlwaysCopy;

                codeItemAdder.PerformAddAndSaveTask(assemblyContainingResources);
            }
            else // remove both:
            {
                TaskManager.Self.Add(() =>
                {
                    codeItemAdder.PerformRemoveAndSave(assemblyContainingResources);

                }, "Removing standard Gum files");
            }
        }

        private CodeBuildItemAdder GetGumCoreCodeItemAdder(Assembly assemblyContainingResources)
        {
            var codeItemAdder = new CodeBuildItemAdder();
            codeItemAdder.OutputFolderInProject = "GumCore";

            var embeddedFolder = "GumPluginCore/Embedded/";

            codeItemAdder.Add(embeddedFolder + "ContentManagerWrapper.cs");

            codeItemAdder.Add(embeddedFolder + "GumIdb.cs");
            codeItemAdder.Add(embeddedFolder + "PlatformCompatability.cs");
            codeItemAdder.Add(embeddedFolder + "GumToFrbShapeRelationship.cs");
            codeItemAdder.Add(embeddedFolder + "GumCollidableExtensions.cs");
            
            codeItemAdder.Add(embeddedFolder + "PositionedObjectGueWrapper.cs");

            codeItemAdder.Add(embeddedFolder + "GraphicalUiElement.IWindow.cs");
            codeItemAdder.Add(embeddedFolder + "SystemManagers.FlatRedBall.cs");
            codeItemAdder.Add(embeddedFolder + "GumAnimation.cs");


            // Sometimes we can add entire folders because the extensions
            // are simple:
            codeItemAdder.AddFolder("GumPluginCore.Embedded.LibraryFiles.GumDataTypes", assemblyContainingResources);


            // But in situations where files have names like
            // FileName.Subname.cs, we have to be explicit and use slashes:
            codeItemAdder.Add(embeddedFolder + "LibraryFiles/GumRuntime/Blend.cs");
            codeItemAdder.Add(embeddedFolder + "LibraryFiles/GumRuntime/ElementSaveExtensionMethods.cs");
            codeItemAdder.Add(embeddedFolder + "LibraryFiles/GumRuntime/ElementSaveExtensions.GumRuntime.cs");
            codeItemAdder.Add(embeddedFolder + "LibraryFiles/GumRuntime/ElementWithState.cs");
            codeItemAdder.Add(embeddedFolder + "LibraryFiles/GumRuntime/GraphicalUiElement.cs");
            codeItemAdder.Add(embeddedFolder + "LibraryFiles/GumRuntime/InstanceSaveExtensionMethods.cs");
            codeItemAdder.Add(embeddedFolder + "LibraryFiles/GumRuntime/InstanceSaveExtensionMethods.GumRuntime.cs");
            codeItemAdder.Add(embeddedFolder + "LibraryFiles/GumRuntime/ObjectFinder.cs");
            codeItemAdder.Add(embeddedFolder + "LibraryFiles/GumRuntime/RecursiveVariableFinder.cs");
            codeItemAdder.Add(embeddedFolder + "LibraryFiles/GumRuntime/StandardElementsManager.cs");
            codeItemAdder.Add(embeddedFolder + "LibraryFiles/GumRuntime/StateSaveExtensionMethods.cs");
            codeItemAdder.Add(embeddedFolder + "LibraryFiles/GumRuntime/VariableSaveExtensionMethods.cs");


            codeItemAdder.AddFolder("GumPluginCore.Embedded.LibraryFiles.RenderingLibrary", assemblyContainingResources);

            codeItemAdder.AddFolder("GumPluginCore.Embedded.LibraryFiles.ToolsUtilities", assemblyContainingResources);

            return codeItemAdder;
        }
    }
}
