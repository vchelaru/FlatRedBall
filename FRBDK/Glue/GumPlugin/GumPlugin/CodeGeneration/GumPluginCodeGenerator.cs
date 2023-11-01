using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Gum.DataTypes;
using GumPluginCore.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Navigation;

namespace GumPlugin.CodeGeneration;

class GumPluginCodeGenerator : ElementComponentCodeGenerator
{
    public override FlatRedBall.Glue.Plugins.Interfaces.CodeLocation CodeLocation
    {
        get
        {
            return FlatRedBall.Glue.Plugins.Interfaces.CodeLocation.BeforeStandardGenerated;
        }
    }

    public static string GumScreenObjectNameFor(IElement element) => element.Name.EndsWith("\\GumScreen")
        // Can't be named the same as its parent
        ? "GumScreen_"
        : "GumScreen";

    bool ShouldGenerateGumScreenOwner(IElement element) =>
        element is FlatRedBall.Glue.SaveClasses.ScreenSave &&
        GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.HasIGumScreenOwner &&
        GetIfContainsAnyGumScreenFiles(element);

    public override void AddInheritedTypesToList(List<string> listToAddTo, IElement element)
    {
        /////////////////////////Early Out/////////////////////////////
        if(ShouldGenerateGumScreenOwner(element) == false)
        {
            return;
        }
        
        //////////////////////End Early Out/////////////////////////////

        listToAddTo.Add("FlatRedBall.Gum.IGumScreenOwner");

    }

    public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element)
    {
        // June 6, 2023
        // GUMX files in Global Content load and initialize a static global GumIdb automatically.
        // By leaving this in, we have 2 Gum IDBs as reported here:
        // https://github.com/vchelaru/FlatRedBall/issues/1100
        // So why do we do this? I don't think we should...
        //bool needsGumIdb = NeedsGumIdb(element, out isGlueScreen, out hasGumScreen, out hasForms);
        //bool needsGumIdb = false;

        //if (needsGumIdb)
        //{
        //    // Create a generic Gum IDB to support in-code creation of Gum objects:
        //    codeBlock.Line("FlatRedBall.Gum.GumIdb gumIdb;");
        //}

        // It's possible to have a Gum screen in derived but not base, but that's rare so I'm not going to handle that case until someone complains
        if(ShouldGenerateGumScreenOwner(element) && !HasBaseWithGumScreen(element as GlueElement))
        {
            codeBlock.Line("global::Gum.Wireframe.GraphicalUiElement FlatRedBall.Gum.IGumScreenOwner.GumScreen { get; }");

            codeBlock.Line("void FlatRedBall.Gum.IGumScreenOwner.RefreshLayout() => RefreshLayoutInternal(null, null);");
        }

        return codeBlock;
    }

    public static bool NeedsGumIdb(IElement element, out bool isGlueScreen, out bool hasGumScreen, out bool hasForms)
    {
        isGlueScreen = element is FlatRedBall.Glue.SaveClasses.ScreenSave;
        hasGumScreen = GetIfContainsAnyGumScreenFiles(element);
        // technically all FRB projects now have forms, so let's just default that to true
        hasForms = element.ReferencedFiles.Any(item =>
        {
            return item.Name.EndsWith(".gusx") &&
                item.RuntimeType != "FlatRedBall.Gum.GumIdb";
            //return item.RuntimeType?.EndsWith(".GraphicalUiElement") == true;
        });
        // if it's derived, then the base will take care of it.
        var isDerivedScreen = !string.IsNullOrEmpty(element.BaseElement);

        //return isGlueScreen && !hasGumScreen && !isDerivedScreen && GetIfHasGumProject();
        // no more gum IDB per screen:
        return false;
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
            // also instantiate the Gum object which has a common alias\
            var gumScreenName = GumScreenObjectNameFor(element);
            var shouldGenerateGum = element.AllNamedObjects.Any(item => item.InstanceName == gumScreenName) == false;
            if(shouldGenerateGum)
            {
                codeBlock.Line($"{gumScreenName} = {rfs.GetInstanceName()};");
            }

        }

        return codeBlock;
    }

    public override ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, IElement element)
    {
        // We no longer do this as of June 7, 2023. Always using the global one
        //var needsGumIdb = NeedsGumIdb(element, out bool _, out bool _, out bool _);

        //if (needsGumIdb)
        //{
        //    // Create a generic Gum IDB to support in-code creation of Gum objects:
        //    codeBlock.Line("FlatRedBall.SpriteManager.AddDrawableBatch(gumIdb);");
        //}

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

    bool HasBaseWithGumScreen(GlueElement element)
    {
        var allBaseElements = ObjectFinder.Self.GetAllBaseElementsRecursively(element);

        foreach(var baseElement in allBaseElements)
        {
            if(GetGumScreenRfs(baseElement) != null)
            {
                return true;
            }
        }
        return false;
    }

    public override ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, IElement element)
    {
        bool isGlueScreen = element is FlatRedBall.Glue.SaveClasses.ScreenSave;

        var gumScreenRfs = GetGumScreenRfs(element);

        if(isGlueScreen && gumScreenRfs != null)
        {
            var hasBase = HasBaseWithGumScreen(element as GlueElement);
            string prefix = "protected virtual void";
            if(hasBase)
            {
                prefix = "protected override void";
            }

            var method = codeBlock.Function(prefix, "RefreshLayoutInternal", "object sender, EventArgs e");

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

            if(hasBase)
            {
                method.Line("base.RefreshLayoutInternal(sender, e);");
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

    public static ReferencedFileSave GetGumScreenRfs(IElement element)
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

    private static bool GetIfContainsAnyGumScreenFiles(IElement element)
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
