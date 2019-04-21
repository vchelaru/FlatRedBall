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

        public void SaveEmptyProject(string directoryToSaveProjectTo)
        {
            mContextAssembly = Assembly.GetExecutingAssembly();
            mContextDirectoryToSaveTo = directoryToSaveProjectTo;

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
            string resourceName = "GumPlugin/Embedded/EmptyProject/" + fileName;

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
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Back.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Bounce.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Circular.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Cubic.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Elastic.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Exponential.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Instant.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Linear.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Quadratic.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Quartic.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Quintic.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/ShakeTweener.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Sinusoidal.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Tweener.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/TweenerManager.cs");


            mStateInterpolationItemAdder.OutputFolderInProject = "StateInterpolation";
            if(behavior == FileAdditionBehavior.EmbedCodeFiles)
            {
                mStateInterpolationItemAdder.AddFileBehavior = AddFileBehavior.IfOutOfDate;

                TaskManager.Self.AddSync(() =>
                {
                    FlatRedBall.Glue.Plugins.PluginManager.ReceiveOutput("Adding interpolation files from Gum plugin");
                    mStateInterpolationItemAdder.IsVerbose = true;
                    mStateInterpolationItemAdder.PerformAddAndSave(assembly);
                }
                , "Adding interpolation files for Gum");

            }
            else if(behavior == FileAdditionBehavior.IncludeNoFiles)
            {
                TaskManager.Self.AddSync(() =>
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
                TaskManager.Self.AddSync(() =>
                {
                    // Just in case the project was unloaded:
                    if(GlueState.Self.CurrentGlueProject != null)
                    {
                        codeItemAdder.PerformAddAndSave(assemblyContainingResources);
                    }

                }, "Adding standard Gum files");
            }
            else // remove both:
            {
                TaskManager.Self.AddSync(() =>
                {
                    codeItemAdder.PerformRemoveAndSave(assemblyContainingResources);

                }, "Removing standard Gum files");
            }
        }

        private CodeBuildItemAdder GetGumCoreCodeItemAdder(Assembly assemblyContainingResources)
        {
            var codeItemAdder = new CodeBuildItemAdder();
            codeItemAdder.OutputFolderInProject = "GumCore";

            codeItemAdder.Add("GumPlugin/Embedded/ContentManagerWrapper.cs");

            codeItemAdder.Add("GumPlugin/Embedded/GumIdb.cs");
            codeItemAdder.Add("GumPlugin/Embedded/PlatformCompatability.cs");
            codeItemAdder.Add("GumPlugin/Embedded/PositionedObjectGueWrapper.cs");

            codeItemAdder.Add("GumPlugin/Embedded/GraphicalUiElement.IWindow.cs");
            codeItemAdder.Add("GumPlugin/Embedded/SystemManagers.FlatRedBall.cs");
            codeItemAdder.Add("GumPlugin/Embedded/GumAnimation.cs");


            // Sometimes we can add entire folders because the extensions
            // are simple:
            codeItemAdder.AddFolder("GumPlugin.Embedded.LibraryFiles.GumDataTypes", assemblyContainingResources);


            // But in situations where files have names like
            // FileName.Subname.cs, we have to be explicit and use slashes:
            codeItemAdder.Add("GumPlugin/Embedded/LibraryFiles/GumRuntime/Blend.cs");
            codeItemAdder.Add("GumPlugin/Embedded/LibraryFiles/GumRuntime/ElementSaveExtensionMethods.cs");
            codeItemAdder.Add("GumPlugin/Embedded/LibraryFiles/GumRuntime/ElementSaveExtensions.GumRuntime.cs");
            codeItemAdder.Add("GumPlugin/Embedded/LibraryFiles/GumRuntime/ElementWithState.cs");
            codeItemAdder.Add("GumPlugin/Embedded/LibraryFiles/GumRuntime/GraphicalUiElement.cs");
            codeItemAdder.Add("GumPlugin/Embedded/LibraryFiles/GumRuntime/InstanceSaveExtensionMethods.cs");
            codeItemAdder.Add("GumPlugin/Embedded/LibraryFiles/GumRuntime/InstanceSaveExtensionMethods.GumRuntime.cs");
            codeItemAdder.Add("GumPlugin/Embedded/LibraryFiles/GumRuntime/ObjectFinder.cs");
            codeItemAdder.Add("GumPlugin/Embedded/LibraryFiles/GumRuntime/RecursiveVariableFinder.cs");
            codeItemAdder.Add("GumPlugin/Embedded/LibraryFiles/GumRuntime/StandardElementsManager.cs");
            codeItemAdder.Add("GumPlugin/Embedded/LibraryFiles/GumRuntime/StateSaveExtensionMethods.cs");
            codeItemAdder.Add("GumPlugin/Embedded/LibraryFiles/GumRuntime/VariableSaveExtensionMethods.cs");


            codeItemAdder.AddFolder("GumPlugin.Embedded.LibraryFiles.RenderingLibrary", assemblyContainingResources);

            codeItemAdder.AddFolder("GumPlugin.Embedded.LibraryFiles.ToolsUtilities", assemblyContainingResources);

            return codeItemAdder;
        }
    }
}
