using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.IO;
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

        CodeBuildItemAdder mCodeAdder;

        CodeBuildItemAdder mStateInterpolationItemAdder;

        public void SaveEmptyProject(string directoryToSaveProjectTo)
        {
            mContextAssembly = Assembly.GetExecutingAssembly();
            mContextDirectoryToSaveTo = directoryToSaveProjectTo;

            SaveGumFile("GumProject.gumx");
            SaveGumFile("Standards/ColoredRectangle.gutx");
            SaveGumFile("Standards/Component.gutx");
            SaveGumFile("Standards/Container.gutx");
            SaveGumFile("Standards/NineSlice.gutx");
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

        public void UpdateCodeInProjectPresence()
        {
            bool hasGumProject = AppState.Self.GumProjectSave != null;

            if (hasGumProject)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();

                UpdateGumFiles(assembly);

                UpdateAdvancedStateInterpolationFiles(assembly);
            }
        }

        private void UpdateAdvancedStateInterpolationFiles(Assembly assembly)
        {

            mStateInterpolationItemAdder = new CodeBuildItemAdder();
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Back.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Bounce.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Circular.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Cubic.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Elastic.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Exponential.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Linear.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Quadratic.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Quartic.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Quintic.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/ShakeTweener.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Sinusoidal.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/Tweener.cs");
            mStateInterpolationItemAdder.Add("GumPlugin/Embedded/StateInterpolation/TweenerManager.cs");

            mStateInterpolationItemAdder.AddFileBehavior = AddFileBehavior.IfOutOfDate;

            mStateInterpolationItemAdder.OutputFolderInProject = "StateInterpolation";
            mStateInterpolationItemAdder.PerformAddAndSave(assembly);

        }

        private void UpdateGumFiles(Assembly assembly)
        {
            mCodeAdder = new CodeBuildItemAdder();
            mCodeAdder.OutputFolderInProject = "GumCore";
            mCodeAdder.AddFileBehavior = AddFileBehavior.IfOutOfDate;

            mCodeAdder.Add("GumPlugin/Embedded/ContentManagerWrapper.cs");

            mCodeAdder.Add("GumPlugin/Embedded/GumIdb.cs");
            mCodeAdder.Add("GumPlugin/Embedded/PlatformCompatability.cs");

            mCodeAdder.Add("GumPlugin/Embedded/GraphicalUiElement.IWindow.cs");
            mCodeAdder.Add("GumPlugin/Embedded/SystemManagers.FlatRedBall.cs");
            mCodeAdder.Add("GumPlugin/Embedded/GumAnimation.cs");


            // Sometimes we can add entire folders because the extensions
            // are simple:
            mCodeAdder.AddFolder("GumPlugin.Embedded.LibraryFiles.GumDataTypes", assembly);


            // But in situations where files have names like
            // FileName.Subname.cs, we have to be explicit and use slashes:
            mCodeAdder.Add("GumPlugin/Embedded/LibraryFiles/GumRuntime/Blend.cs");
            mCodeAdder.Add("GumPlugin/Embedded/LibraryFiles/GumRuntime/ElementSaveExtensionMethods.cs");
            mCodeAdder.Add("GumPlugin/Embedded/LibraryFiles/GumRuntime/ElementSaveExtensions.GumRuntime.cs");
            mCodeAdder.Add("GumPlugin/Embedded/LibraryFiles/GumRuntime/ElementWithState.cs");
            mCodeAdder.Add("GumPlugin/Embedded/LibraryFiles/GumRuntime/GraphicalUiElement.cs");
            mCodeAdder.Add("GumPlugin/Embedded/LibraryFiles/GumRuntime/InstanceSaveExtensionMethods.cs");
            mCodeAdder.Add("GumPlugin/Embedded/LibraryFiles/GumRuntime/InstanceSaveExtensionMethods.GumRuntime.cs");
            mCodeAdder.Add("GumPlugin/Embedded/LibraryFiles/GumRuntime/ObjectFinder.cs");
            mCodeAdder.Add("GumPlugin/Embedded/LibraryFiles/GumRuntime/RecursiveVariableFinder.cs");
            mCodeAdder.Add("GumPlugin/Embedded/LibraryFiles/GumRuntime/StandardElementsManager.cs");
            mCodeAdder.Add("GumPlugin/Embedded/LibraryFiles/GumRuntime/StateSaveExtensionMethods.cs");
            mCodeAdder.Add("GumPlugin/Embedded/LibraryFiles/GumRuntime/VariableSaveExtensionMethods.cs");


            mCodeAdder.AddFolder("GumPlugin.Embedded.LibraryFiles.RenderingLibrary", assembly);

            mCodeAdder.AddFolder("GumPlugin.Embedded.LibraryFiles.ToolsUtilities", assembly);

            mCodeAdder.PerformAddAndSave(assembly);
        }

    }
}
