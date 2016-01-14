using EditorObjects.Parsing;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Graphics;
using FlatRedBall.IO;
using FlatRedBall.Utilities;
using Gum.DataTypes;
using Gum.Managers;
using Microsoft.Build.BuildEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Plugins;

namespace GumPlugin.Managers
{
    public class FileReferenceTracker : Singleton<FileReferenceTracker>
    {
        enum ProjectOrDisk
        {
            Project,
            Disk
        }

        public bool UseAtlases
        {
            get;
            set;

        }
        public static bool CanTrackDependenciesOn(string fileName)
        {
            string extension = FileManager.GetExtension(fileName);
            return extension == GumProjectSave.ComponentExtension ||
                extension == "gumx" ||
                extension == GumProjectSave.ScreenExtension ||
                extension == GumProjectSave.StandardExtension ||
                // We want to refresh code when this gets changed, so we have to return true for .ganx files:
                extension == "ganx"
                ;
        }

        public void HandleGetFilesNeededOnDiskBy(GumProjectSave gumProjectSave, TopLevelOrRecursive topLevelOrRecursive, List<string> listToFill)
        {

        }


        private void GetFilesReferencedBy(GumProjectSave gumProjectSave, TopLevelOrRecursive topLevelOrRecursive, List<string> listToFill, ProjectOrDisk projectOrDisk)
        {
            AppState.Self.GumProjectSave = gumProjectSave;

            foreach (var element in gumProjectSave.Screens)
            {
                AddFilesReferencedBy(topLevelOrRecursive, listToFill, element, projectOrDisk);
            }

            foreach (var element in gumProjectSave.Components)
            {
                AddFilesReferencedBy(topLevelOrRecursive, listToFill, element, projectOrDisk);
            }

            foreach (var element in gumProjectSave.StandardElements)
            {
                AddFilesReferencedBy(topLevelOrRecursive, listToFill, element, projectOrDisk);
            }

            StringFunctions.RemoveDuplicates(listToFill);
        }

        private void AddFilesReferencedBy(TopLevelOrRecursive topLevelOrRecursive, List<string> listToFill, ElementSave element, ProjectOrDisk projectOrDisk)
        {
            string fullFileName = FileManager.RelativeDirectory + element.Subfolder + "\\" + element.Name +
                "." + element.FileExtension;

            fullFileName = FileManager.RemoveDotDotSlash(fullFileName);

            listToFill.Add(fullFileName);

            if (topLevelOrRecursive == TopLevelOrRecursive.Recursive)
            {
                throw new NotImplementedException("This does not support recursive calls - should use the Glue system to track reference files");
            }
        }

        private void GetFilesReferencedBy(ElementSave element, TopLevelOrRecursive topLevelOrRecursive, List<string> listToFill, ProjectOrDisk projectOrDisk)
        {
            if(!string.IsNullOrEmpty(element.FileName) && projectOrDisk == ProjectOrDisk.Disk)
            {
                var fileWithoutExtension = FileManager.RemoveExtension(element.FileName);

                var potentialAnimationName = fileWithoutExtension + "Animations.ganx";

                if(System.IO.File.Exists(potentialAnimationName))
                {
                    listToFill.Add(potentialAnimationName);
                }
            }

            // Use AllStates so that we get stuff referenced in categorized states too
            foreach (var state in element.AllStates)
            {

                state.ParentContainer = element;

                TryGetFontReferences(topLevelOrRecursive, listToFill, state);



                GetRegularVariableFileReferences(listToFill, state, projectOrDisk);

                foreach (var variableList in state.VariableLists)
                {
                    if (variableList.IsFile)
                    {
                        throw new NotImplementedException();
                    }
                }
            }

            foreach (var instance in element.Instances)
            {
                var type = instance.BaseType;

                // find the element.
                var referencedElement = AppState.Self.GetElementSave(type);

                if (referencedElement != null)
                {
                    if (referencedElement is StandardElementSave)
                    {
                        listToFill.Add(FileManager.RelativeDirectory + "Standards/" + referencedElement.Name + ".gutx");
                    }
                    else if (referencedElement is ComponentSave)
                    {
                        listToFill.Add(FileManager.RelativeDirectory + "Components/" + referencedElement.Name + ".gucx");

                    }
                    else
                    {
                        throw new Exception();
                    }

                    if (topLevelOrRecursive == TopLevelOrRecursive.Recursive)
                    {
                        GetFilesReferencedBy(referencedElement, topLevelOrRecursive, listToFill, projectOrDisk);
                    }
                }
            }

            StringFunctions.RemoveDuplicates(listToFill);
        }

        private void GetRegularVariableFileReferences(List<string> listToFill, Gum.DataTypes.Variables.StateSave state, ProjectOrDisk projectOrDisk)
        {
            foreach (var variable in state.Variables.Where(item=>item.IsFile))
            {
                if (!string.IsNullOrEmpty(variable.Value as string))
                {
                    bool isGraphicFile = FileManager.IsGraphicFile(variable.Value as string);

                    bool shouldConsider = !UseAtlases || !isGraphicFile || projectOrDisk == ProjectOrDisk.Disk;

                    if (shouldConsider)
                    {

                        if (IsNineSliceSource(variable, state))
                        {
                            string variableValue = variable.Value as string;

                            var shouldUsePattern = RenderingLibrary.Graphics.NineSlice.GetIfShouldUsePattern(variableValue);

                            if (shouldUsePattern)
                            {
                                string extension = FileManager.GetExtension(variableValue);

                                string variableWithoutExtension = FileManager.RemoveExtension(variableValue);

                                string bareTexture = RenderingLibrary.Graphics.NineSlice.GetBareTextureForNineSliceTexture(
                                    variableValue);

                                foreach (var side in RenderingLibrary.Graphics.NineSlice.PossibleNineSliceEndings)
                                {
                                    listToFill.Add(FileManager.RelativeDirectory + bareTexture + side + "." + extension);
                                }
                            }
                            else
                            {
                                listToFill.Add(FileManager.RelativeDirectory + variableValue);
                            }
                        }
                        else
                        {

                            string absoluteFileName = FileManager.RelativeDirectory + variable.Value as string;
                            absoluteFileName = FileManager.RemoveDotDotSlash(absoluteFileName);

                            listToFill.Add(absoluteFileName);
                        }
                    }
                }
            }
        }

        private static bool IsNineSliceSource(Gum.DataTypes.Variables.VariableSave variable, Gum.DataTypes.Variables.StateSave state)
        {
            if (variable.GetRootName() == "SourceFile")
            {
                ElementSave rootElementSave = null;
                if (string.IsNullOrEmpty(variable.SourceObject))
                {
                    rootElementSave = state.ParentContainer;
                }
                else
                {
                    string instanceName = variable.SourceObject;

                    var instance = state.ParentContainer.GetInstance(variable.SourceObject);

                    rootElementSave = ObjectFinder.Self.GetElementSave(instance.BaseType);
                }

                return rootElementSave is StandardElementSave && rootElementSave.Name == "NineSlice";
            }

            return false;

        }

        private static void TryGetFontReferences(TopLevelOrRecursive topLevelOrRecursive, List<string> listToFill, Gum.DataTypes.Variables.StateSave state)
        {

            Gum.DataTypes.RecursiveVariableFinder rvf = new RecursiveVariableFinder(state);

            foreach (var fontNameVariable in state.Variables.Where(item => item.GetRootName() == "Font"))
            {
                string fontSizeVariableName = null;

                string prefix = null;

                if (fontNameVariable.Name.Contains('.'))
                {
                    prefix = FileManager.RemoveExtension(fontNameVariable.Name) + ".";
                }

                bool useCustomFont = rvf.GetValue<bool>(prefix + "UseCustomFont");
                if (!useCustomFont)
                {
                    fontSizeVariableName = prefix + "FontSize";

                    int fontSizeValue = rvf.GetValue<int>(fontSizeVariableName);
                    var fontSizeVariable = state.Variables.FirstOrDefault(item => item.Name == fontSizeVariableName);

                    string fontNameValue = fontNameVariable.Value as string;

                    TryAddFontFromSizeAndName(topLevelOrRecursive, listToFill, fontSizeValue, fontNameValue);
                }
            }

            foreach (var fontSizeVariable in state.Variables.Where(item => item.GetRootName() == "FontSize" && item.Value != null))
            {
                string fontNameVariableName = null;

                string prefix = null;

                if (fontSizeVariable.Name.Contains('.'))
                {
                    prefix = FileManager.RemoveExtension(fontSizeVariable.Name) + ".";
                }

                bool useCustomFont = rvf.GetValue<bool>(prefix + "UseCustomFont");
                if (!useCustomFont)
                {
                    fontNameVariableName = prefix + "Font";

                    string fontNameValue = rvf.GetValue<string>(fontNameVariableName);

                    var fontNameVariable = state.Variables.FirstOrDefault(item => item.Name == fontNameVariableName);


                    int fontSizeValue = (int)fontSizeVariable.Value;

                    TryAddFontFromSizeAndName(topLevelOrRecursive, listToFill, fontSizeValue, fontNameValue);
                }
            }
        }

        private static void TryAddFontFromSizeAndName(TopLevelOrRecursive topLevelOrRecursive, List<string> listToFill, int fontSizeValue, string fontNameValue)
        {
            if (!string.IsNullOrEmpty(fontNameValue))
            {
                int fontSize = fontSizeValue;

                string fontFileName = "Font" + fontSize.ToString() + fontNameValue + ".fnt";
                fontFileName = FileManager.RelativeDirectory + "FontCache\\" + fontFileName;

                listToFill.Add(fontFileName);

                if (topLevelOrRecursive == TopLevelOrRecursive.Recursive && System.IO.File.Exists(fontFileName))
                {
                    string fontDirectory = FileManager.GetDirectory(fontFileName);

                    string fontContents = FileManager.FromFileText(fontFileName);

                    string[] texturesToLoad = BitmapFont.GetSourceTextures(fontContents);

                    foreach (var referencedTexture in texturesToLoad)
                    {
                        string absoluteFileName = fontDirectory + referencedTexture;

                        listToFill.Add(absoluteFileName);
                    }

                }
            }
        }

        string GetGumxDirectory(string possibleGumx)
        {
            string argumentExtension = FileManager.GetExtension(possibleGumx);

            if (argumentExtension == "gumx")
            {
                return FileManager.GetDirectory(possibleGumx);
            }
            else if (AppState.Self.GumProjectSave != null)
            {
                return FileManager.GetDirectory(AppState.Self.GumProjectSave.FullFileName);
            }
            else if (argumentExtension == GumProjectSave.ScreenExtension || argumentExtension == GumProjectSave.StandardExtension ||
                argumentExtension == GumProjectSave.ComponentExtension)
            {
                // go up one directory, look for the gumx there
                string directory = FileManager.GetDirectory(FileManager.GetDirectory(possibleGumx));

                

                return directory;

            }
            else
            {
                return null;
            }
        }

        public void HandleGetFilesNeededOnDiskBy(string fileName, List<string> listToFill)
        {
            string oldRelativeDirectory = FileManager.RelativeDirectory;
            ProjectOrDisk projectOrDisk = ProjectOrDisk.Disk;
            // We aways only look at TopLevel, 
            // This can sometimes crash if it is run multiple times (like if the user clicks to 
            // refresh files quickly:
            try
            {
                GetReferencesInProjectOrDisk(fileName, TopLevelOrRecursive.TopLevel, listToFill, projectOrDisk);
            }
            catch(Exception e)
            {
                PluginManager.ReceiveError("Non-critical error: " + e.Message);
                FileManager.RelativeDirectory = oldRelativeDirectory;
            }
    }

    public void HandleGetFilesReferencedBy(string fileName, TopLevelOrRecursive topLevelOrRecursive,
            List<string> listToFill)
        {
            ProjectOrDisk projectOrDisk = ProjectOrDisk.Project;
            GetReferencesInProjectOrDisk(fileName, topLevelOrRecursive, listToFill, projectOrDisk);
        }

        private void GetReferencesInProjectOrDisk(string fileName, TopLevelOrRecursive topLevelOrRecursive, List<string> listToFill, ProjectOrDisk projectOrDisk)
        {
            if (CanTrackDependenciesOn(fileName))
            {
                string extension = FileManager.GetExtension(fileName);

                string oldRelative = FileManager.RelativeDirectory;

                string gumxFile = null;


                FileManager.RelativeDirectory = GetGumxDirectory(fileName);
                LoadGumxIfNecessaryFromDirectory(FileManager.RelativeDirectory);

                string absoluteFileName = fileName;
                if (FileManager.IsRelative(absoluteFileName))
                {
                    absoluteFileName = FileManager.RelativeDirectory + absoluteFileName;
                }

                string errors = null;
                if (System.IO.File.Exists(absoluteFileName))
                {
                    switch (extension)
                    {
                        case "gumx":
                            {
                                GumLoadResult result;

                                GumProjectSave gumProjectSave = GumProjectSave.Load(absoluteFileName, out result);
                                GetFilesReferencedBy(gumProjectSave, topLevelOrRecursive, listToFill, projectOrDisk);
                            }
                            break;
                        case "gucx":
                            {
                                ComponentSave gumComponentSave = null;
                                try
                                {
                                    gumComponentSave = FileManager.XmlDeserialize<ComponentSave>(absoluteFileName);
                                    gumComponentSave.FileName = absoluteFileName;
                                    GetFilesReferencedBy(gumComponentSave, topLevelOrRecursive, listToFill, projectOrDisk);
                                }
                                catch (Exception e)
                                {
                                    FlatRedBall.Glue.Plugins.PluginManager.ReceiveError(
                                        "Error tracking Gum references for " + absoluteFileName + "\n" + e.ToString());
                                }
                            }
                            break;
                        case "gusx":
                            {
                                ScreenSave gumScreenSave = null;
                                try
                                {
                                    gumScreenSave = FileManager.XmlDeserialize<ScreenSave>(absoluteFileName);
                                    gumScreenSave.FileName = absoluteFileName;

                                    GetFilesReferencedBy(gumScreenSave, topLevelOrRecursive, listToFill, projectOrDisk);
                                }
                                catch (Exception e)
                                {
                                    FlatRedBall.Glue.Plugins.PluginManager.ReceiveError(
                                        "Error tracking Gum references for " + absoluteFileName + "\n" + e.ToString());
                                }
                            }
                            break;
                        case "gutx":
                            {
                                StandardElementSave standardElementSave = null;
                                try
                                {
                                    standardElementSave = FileManager.XmlDeserialize<StandardElementSave>(absoluteFileName);
                                    standardElementSave.FileName = absoluteFileName;

                                    GetFilesReferencedBy(standardElementSave, topLevelOrRecursive, listToFill, projectOrDisk);
                                }
                                catch (Exception e)
                                {
                                    FlatRedBall.Glue.Plugins.PluginManager.ReceiveError(
                                        "Error tracking Gum references for " + absoluteFileName + "\n" + e.ToString());
                                }
                            }
                            break;
                    }
                }


                FileManager.RelativeDirectory = oldRelative;

            }
        }

        public void LoadGumxIfNecessaryFromDirectory(string gumxDirectory, bool force = false)
        {
            lock(this)
            {
                if (Gum.Managers.ObjectFinder.Self.GumProjectSave == null || force)
                {
                    string gumxFile = System.IO.Directory.GetFiles(gumxDirectory).FirstOrDefault(item => item.ToLowerInvariant().EndsWith(".gumx"));

                    // This can occur if the user has added a gumx file, then removed it, but the project still
                    // references gum files like gum screens or components.  This is an invalid setup, but we're
                    // going to handle it more gracefully than throwing exceptions.
                    if (!string.IsNullOrEmpty(gumxFile))
                    {

                        GumLoadResult result;

                        Gum.Managers.ObjectFinder.Self.GumProjectSave =
                            GumProjectSave.Load(gumxFile, out result);
                    }
                }

                // this used to be in the if-block above, but we want this
                // to always run because an already-loaded glux may have new
                // variables which are enums.
                InitializeElements();
            }
        }

        private static void InitializeElements()
        {
            var project = Gum.Managers.ObjectFinder.Self.GumProjectSave;

            if (project != null)
            {
                // The Gum tool does a lot more init than this, but we're going to only do a subset of initialization for performance
                // reasons:
                foreach (var item in Gum.Managers.ObjectFinder.Self.GumProjectSave.Screens)
                {
                    item.Initialize(item.DefaultState);
                }
                foreach (var item in Gum.Managers.ObjectFinder.Self.GumProjectSave.Components)
                {
                    item.Initialize(item.DefaultState);
                }
                foreach (var item in Gum.Managers.ObjectFinder.Self.GumProjectSave.StandardElements)
                {
                    item.Initialize(item.DefaultState);
                }
            }
        }

        internal void RemoveUnreferencedMissingFilesFromVsProject()
        {
            var gumProject = ObjectFinder.Self.GumProjectSave;

            if (gumProject != null && FileManager.FileExists(gumProject.FullFileName))
            {

                var codeProject = GlueState.Self.CurrentMainProject;
                var contentProject = GlueState.Self.CurrentMainContentProject;
                bool shouldSave = false;

                bool wasAnythingChanged = RemoveUnreferencedFilesFromProjects(gumProject, codeProject, contentProject);
                shouldSave |= wasAnythingChanged;

                foreach(var syncedProject in GlueState.Self.SyncedProjects)
                {
                    wasAnythingChanged = RemoveUnreferencedFilesFromProjects(gumProject, syncedProject, syncedProject.ContentProject);
                    shouldSave |= wasAnythingChanged;
                }


                if (shouldSave)
                {
                    GlueCommands.Self.ProjectCommands.SaveProjects();
                }
            }
        }

        private static bool RemoveUnreferencedFilesFromProjects(GumProjectSave gumProject, ProjectBase codeProject, ProjectBase contentProject)
        {
            List<BuildItem> toRemove = new List<BuildItem>();

            FillWithContentBuildItemsToRemove(gumProject, toRemove, contentProject);
            bool wasAnythingChanged = false;

            if (toRemove.Count != 0)
            {
                foreach (var buildItem in toRemove)
                {
                    contentProject.RemoveItem(buildItem);
                }

                wasAnythingChanged = true;
            }

            toRemove.Clear();
            FillWithCodeBuildItemsToRemove(gumProject, toRemove, codeProject);
            if (toRemove.Count != 0)
            {
                foreach (var buildItem in toRemove)
                {
                    codeProject.RemoveItem(buildItem);
                }

                wasAnythingChanged = true;
            }
            return wasAnythingChanged;
        }
        
        private static void FillWithCodeBuildItemsToRemove(GumProjectSave gumProject, List<BuildItem> toRemove, ProjectBase project)
        {

            IEnumerable<ElementSave> allElements = gumProject.Components.OfType<ElementSave>().Concat(
                gumProject.StandardElements.OfType<ElementSave>()).Concat(
                gumProject.Screens.OfType<ElementSave>());

            foreach (BuildItem buildItem in project)
            {
                if (buildItem.Include != null && buildItem.Include.ToLower().EndsWith("runtime.generated.cs") &&
                    FileManager.GetDirectory(buildItem.Include, RelativeType.Relative) == "GumRuntimes/")
                {
                    // is there an element with this name?


                    var elementName = FileManager.RemoveExtension( FileManager.RemoveExtension( 
                        FileManager.RemovePath(buildItem.Include)));

                    // "elementName" will end with "Runtime"
                    elementName = elementName.Substring(0, elementName.Length - "Runtime".Length);

                    var foundElement = allElements.FirstOrDefault(item => item.Name == elementName);

                    if (foundElement == null)
                    {
                        toRemove.Add(buildItem);
                    }
                }

            }
        }

        /// <summary>
        /// This checks if the project has any screens, components, or standard elements which
        /// are no longer referenced by the Gum project but are still part of the VS project.
        /// It can only check these because these are the only file types that *must* be referenced
        /// by a .gumx file to be referenced in a project. It will not check files like PNGs because
        /// PNGs can be used by other plugins or even in code.
        /// </summary>
        /// <param name="gumProject">The Gum project to check.</param>
        /// <param name="toRemove">A list of which files to remove. The method populates this.</param>
        /// <param name="contentProject">The content project to check inside of for orphan references.</param>
        private static void FillWithContentBuildItemsToRemove(GumProjectSave gumProject, List<BuildItem> toRemove, ProjectBase contentProject)
        {
            foreach (BuildItem buildItem in contentProject)
            {
                string extension = FileManager.GetExtension(buildItem.Include);


                var gumProjectFolder = FileManager.GetDirectory(gumProject.FullFileName);
                var contentFolder = FileManager.GetDirectory( contentProject.FullFileName );
                var gumFolderRelative = FileManager.MakeRelative(gumProjectFolder, contentFolder);

                // is it a standard element?
                if (extension == GumProjectSave.StandardExtension)
                {
                    var standardFolder = gumFolderRelative + ElementReference.StandardSubfolder + "/";

                    string elementName = FileManager.RemoveExtension(FileManager.MakeRelative(buildItem.Include, standardFolder))
                        .Replace("/", "\\");

                    bool exists = gumProject.StandardElements.Any(item => item.Name.ToLowerInvariant() == elementName.ToLowerInvariant());

                    if (!exists)
                    {
                        toRemove.Add(buildItem);
                    }
                }
                // or is it a component?
                else if (extension == GumProjectSave.ComponentExtension)
                {
                    var componentFolder = gumFolderRelative + ElementReference.ComponentSubfolder + "/";

                    string elementName = FileManager.RemoveExtension(FileManager.MakeRelative(buildItem.Include, componentFolder))
                        .Replace("/", "\\");

                    bool exists = gumProject.Components.Any(item => item.Name.ToLowerInvariant() == elementName.ToLowerInvariant());

                    if (!exists)
                    {
                        toRemove.Add(buildItem);
                    }
                }
                // or is it a screen?
                else if (extension == GumProjectSave.ScreenExtension)
                {
                    var screenFolder = gumFolderRelative + ElementReference.ScreenSubfolder + "/";

                    string elementName = FileManager.RemoveExtension(FileManager.MakeRelative(buildItem.Include, screenFolder))
                        .Replace("/", "\\");

                    bool exists = gumProject.Screens.Any(item => item.Name.ToLowerInvariant() == elementName.ToLowerInvariant());

                    if (!exists)
                    {
                        toRemove.Add(buildItem);
                    }
                }
            }
        }
    }
}
