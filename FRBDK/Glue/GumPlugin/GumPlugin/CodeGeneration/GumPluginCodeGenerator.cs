using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
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


        public override ICodeBlock GenerateLoadStaticContent(ICodeBlock codeBlock, IElement element)
        {
            bool containsAnyGumFiles = GetIfContainsAnyGumFilesIn(element);

            if (containsAnyGumFiles)
            {
                codeBlock.Line("// Set the content manager for Gum");
                codeBlock.Line("var contentManagerWrapper = new FlatRedBall.Gum.ContentManagerWrapper();");
                codeBlock.Line("contentManagerWrapper.ContentManagerName = contentManagerName;");
                codeBlock.Line("RenderingLibrary.Content.LoaderManager.Self.ContentLoader = contentManagerWrapper;");

                codeBlock.Line("// Access the GumProject just in case it's async loaded");
                codeBlock.Line("var throwaway = GlobalContent.GumProject;");
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

        public static bool IsGue(FlatRedBall.Glue.SaveClasses.NamedObjectSave item)
        {
            return item.SourceType == FlatRedBall.Glue.SaveClasses.SourceType.File &&
                !string.IsNullOrEmpty(item.SourceFile) &&
                !string.IsNullOrEmpty(item.SourceName) &&
                (FileManager.GetExtension(item.SourceFile) == "gusx" || FileManager.GetExtension(item.SourceFile) == "gucx");

        }
    }
}
