using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Gum.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GumPlugin.CodeGeneration
{
    class GumPluginCodeGenerator : ElementComponentCodeGenerator
    {
        public override FlatRedBall.Glue.Plugins.Interfaces.CodeLocation CodeLocation
        {
            get
            {
                return FlatRedBall.Glue.Plugins.Interfaces.CodeLocation.BeforeStandardGenerated;
            }
        }


        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element)
        {
            bool isGlueScreen = element is FlatRedBall.Glue.SaveClasses.ScreenSave;
            bool hasGumScreen = GetIfContainsAnyGumScreenFiles(element);

            if(isGlueScreen && !hasGumScreen)
            {
                // Create a generic Gum IDB to support in-code creation of Gum objects:
                codeBlock.Line("FlatRedBall.Gum.GumIdb gumIdb;");
            }

            return codeBlock;
        }

        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, IElement element)
        {
            bool isGlueScreen = element is FlatRedBall.Glue.SaveClasses.ScreenSave;
            bool hasGumScreen = GetIfContainsAnyGumScreenFiles(element);

            if (isGlueScreen && !hasGumScreen)
            {
                // Create a generic Gum IDB to support in-code creation of Gum objects:
                codeBlock.Line("gumIdb = new FlatRedBall.Gum.GumIdb();");
                codeBlock.Line("FlatRedBall.SpriteManager.AddDrawableBatch(gumIdb);");
            }

            return codeBlock;

        }

        public override ICodeBlock GenerateDestroy(ICodeBlock codeBlock, IElement element)
        {
            bool isGlueScreen = element is FlatRedBall.Glue.SaveClasses.ScreenSave;
            bool hasGumScreen = GetIfContainsAnyGumScreenFiles(element);

            if (isGlueScreen && !hasGumScreen)
            {
                codeBlock.Line("FlatRedBall.SpriteManager.RemoveDrawableBatch(gumIdb);");
            }

            return codeBlock;
        }

        public override ICodeBlock GenerateLoadStaticContent(ICodeBlock codeBlock, IElement element)
        {
            // We used to only do this code if the screen had a component, but we will do it if the entire project
            // has a gum file so that users don't have to manually do this 
            var gumProject = GlueState.Self.CurrentGlueProject.GlobalFiles.FirstOrDefault(item => item.Name.EndsWith(".gumx"));

            if (gumProject != null)
            {
                codeBlock.Line("// Set the content manager for Gum");
                codeBlock.Line("var contentManagerWrapper = new FlatRedBall.Gum.ContentManagerWrapper();");
                codeBlock.Line("contentManagerWrapper.ContentManagerName = contentManagerName;");
                codeBlock.Line("RenderingLibrary.Content.LoaderManager.Self.ContentLoader = contentManagerWrapper;");

                codeBlock.Line("// Access the GumProject just in case it's async loaded");
                codeBlock.Line($"var throwaway = GlobalContent.{gumProject.GetInstanceName()};");
            }

            return codeBlock;
        }

        private bool GetIfContainsAnyGumFilesIn(IElement element)
        {
            foreach (var file in element.ReferencedFiles)
            {
                string extension = FileManager.GetExtension( file.Name);

                if (file.LoadedAtRuntime &&
                    (extension == GumProjectSave.ComponentExtension ||
                    extension == GumProjectSave.ScreenExtension ||
                    extension == GumProjectSave.StandardExtension))
                {
                    return true;
                }
            }

            return false;
        }

        private bool GetIfContainsAnyGumScreenFiles(IElement element)
        {
            foreach (var file in element.ReferencedFiles)
            {
                string extension = FileManager.GetExtension( file.Name);

                if (file.LoadedAtRuntime && extension == GumProjectSave.ScreenExtension)
                {
                    return true;
                }
            }

            return false;
        }


        public static bool IsGue(FlatRedBall.Glue.SaveClasses.NamedObjectSave item)
        {
            return item.SourceType == FlatRedBall.Glue.SaveClasses.SourceType.File &&
                !string.IsNullOrEmpty(item.SourceFile) &&
                !string.IsNullOrEmpty(item.SourceName) &&
                (FileManager.GetExtension(item.SourceFile) == "gusx" || FileManager.GetExtension(item.SourceFile) == "gucx");

        }
    }
}
