using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Gum.DataTypes;
using GumPluginCore.CodeGeneration;
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
            bool isGlueScreen, hasGumScreen, hasForms;
            bool needsGumIdb = NeedsGumIdb(element, out isGlueScreen, out hasGumScreen, out hasForms);

            if (needsGumIdb)
            {
                // Create a generic Gum IDB to support in-code creation of Gum objects:
                codeBlock.Line("FlatRedBall.Gum.GumIdb gumIdb;");
            }

            if (isGlueScreen && hasGumScreen)
            {
                var rfs = GetGumScreenRfs(element);

                var elementName = element.GetStrippedName();

                 if(hasForms)
                {
                    var formsObjectType = FormsClassCodeGenerator.Self.GetFullRuntimeNamespaceFor(elementName, "Screens") +
                        "." + rfs.GetInstanceName() + "Forms";

                    codeBlock.Line($"{formsObjectType} Forms;");
                }

                codeBlock.Line($"{rfs.RuntimeType} GumScreen;");

            }

            return codeBlock;
        }

        private bool NeedsGumIdb(IElement element, out bool isGlueScreen, out bool hasGumScreen, out bool hasForms)
        {
            isGlueScreen = element is FlatRedBall.Glue.SaveClasses.ScreenSave;
            hasGumScreen = GetIfContainsAnyGumScreenFiles(element);
            // technically all FRB projects now have forms, so let's just default that to true
            hasForms = true;
            // if it's derived, then the base will take care of it.
            var isDerivedScreen = !string.IsNullOrEmpty(element.BaseElement);

            return isGlueScreen && !hasGumScreen && !isDerivedScreen && GetIfHasGumProject();
        }

        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, IElement element)
        {
            
            var gumScreenRfs =
                element.ReferencedFiles.FirstOrDefault(item => item.Name.EndsWith(".gusx"));

            
            bool needsGumIdb = NeedsGumIdb(element, out bool isGlueScreen, out bool hasGumScreen, out bool hasForms);


            if (needsGumIdb)
            {
                // Create a generic Gum IDB to support in-code creation of Gum objects:
                codeBlock.Line("gumIdb = new FlatRedBall.Gum.GumIdb();");
            }

            if (isGlueScreen && hasGumScreen)
            {
                var elementName = element.GetStrippedName();

                //var screensOrComponents = element.Name.ToLowerInvariant().EndsWith(".gusx") ? "Screens" : "Components";


                var rfs = GetGumScreenRfs(element);

                if (hasForms)
                {
                    var formsObjectType = FormsClassCodeGenerator.Self.GetFullRuntimeNamespaceFor(elementName, "Screens") +
                        "." + rfs.GetInstanceName() + "Forms";
                    var formsInstantiationLine =
                        $"Forms = new {formsObjectType}({rfs.GetInstanceName()});";
                    codeBlock.Line(formsInstantiationLine);
                }

                // also instantiate the Gum object which has a common alias\
                codeBlock.Line($"GumScreen = {rfs.GetInstanceName()};");

            }

            return codeBlock;
        }

        public override ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, IElement element)
        {
            var needsGumIdb = NeedsGumIdb(element, out bool _, out bool _, out bool _);

            if (needsGumIdb)
            {
                // Create a generic Gum IDB to support in-code creation of Gum objects:
                codeBlock.Line("FlatRedBall.SpriteManager.AddDrawableBatch(gumIdb);");
            }

            return codeBlock;
        }

        public override ICodeBlock GenerateDestroy(ICodeBlock codeBlock, IElement element)
        {
            var needsGumIdb = NeedsGumIdb(element, out bool _, out bool _, out bool _);

            if (needsGumIdb)
            {
                codeBlock.Line("FlatRedBall.SpriteManager.RemoveDrawableBatch(gumIdb);");
            }

            return codeBlock;
        }

        public override ICodeBlock GenerateLoadStaticContent(ICodeBlock codeBlock, IElement element)
        {
            bool hasGumProject = GetIfHasGumProject();

            // We used to only do this code if the screen had a component, but we will do it if the entire project
            // has a gum file so that users don't have to manually do this 

            if (hasGumProject)
            {
                var gumProject =
                    GlueState.Self.CurrentGlueProject.GlobalFiles.FirstOrDefault(item => item.Name.EndsWith(".gumx"));

                codeBlock.Line("// Set the content manager for Gum");
                codeBlock.Line("var contentManagerWrapper = new FlatRedBall.Gum.ContentManagerWrapper();");
                codeBlock.Line("contentManagerWrapper.ContentManagerName = contentManagerName;");
                codeBlock.Line("RenderingLibrary.Content.LoaderManager.Self.ContentLoader = contentManagerWrapper;");

                codeBlock.Line("// Access the GumProject just in case it's async loaded");
                codeBlock.Line($"var throwaway = GlobalContent.{gumProject.GetInstanceName()};");
            }

            return codeBlock;
        }

        public override ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, IElement element)
        {
            bool isGlueScreen = element is FlatRedBall.Glue.SaveClasses.ScreenSave;

            var gumScreenRfs = GetGumScreenRfs(element);

            if(isGlueScreen && gumScreenRfs != null)
            {
                var method = codeBlock.Function("private void", "RefreshLayoutInternal", "object sender, EventArgs e");

                var ati = gumScreenRfs.GetAssetTypeInfo();

                // This could be null if the Gum screen has been deleted from the Gum project.
                // dont' throw an exception if so...
                //if(ati == null)
                //{
                //    throw new Exception($"Could not find asset type info for {element.Name}");
                //}
                if(ati?.RuntimeTypeName == "GumIdb")
                {
                    method.Line($"{gumScreenRfs.GetInstanceName()}.Element.UpdateLayout();");
                }
                else
                {
                    method.Line($"{gumScreenRfs.GetInstanceName()}.UpdateLayout();");
                }

            }

            return codeBlock;
        }

        private static bool GetIfHasGumProject()
        {
            return GlueState.Self.CurrentGlueProject.GlobalFiles.FirstOrDefault(item => item.Name.EndsWith(".gumx")) != null;
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

        private ReferencedFileSave GetGumScreenRfs(IElement element)
        {
            foreach (var file in element.ReferencedFiles)
            {
                string extension = FileManager.GetExtension(file.Name);

                if (file.LoadedAtRuntime && extension == GumProjectSave.ScreenExtension)
                {
                    return file;
                }
            }

            return null;
        }

        private bool GetIfContainsAnyGumScreenFiles(IElement element)
        {
            return GetGumScreenRfs(element) != null;
        }

        public override void GeneratePauseThisScreen(ICodeBlock codeBlock, IElement element)
        {
            if (element is FlatRedBall.Glue.SaveClasses.ScreenSave)
            {
                string line = "StateInterpolationPlugin.TweenerManager.Self.Pause();";
                if (!codeBlock.HasLine(line))
                {
                    codeBlock.Line(line);
                }
            }
        }

        public override void GenerateUnpauseThisScreen(ICodeBlock codeBlock, IElement element)
        {
            if (element is FlatRedBall.Glue.SaveClasses.ScreenSave)
            {
                string line = "StateInterpolationPlugin.TweenerManager.Self.Unpause();";
                if (!codeBlock.HasLine(line))
                {
                    codeBlock.Line(line);
                }
            }
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
