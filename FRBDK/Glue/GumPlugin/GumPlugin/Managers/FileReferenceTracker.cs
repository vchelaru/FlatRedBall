using EditorObjects.Parsing;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Graphics;
using FlatRedBall.IO;
using FlatRedBall.Utilities;
using Gum.DataTypes;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Plugins;
using Microsoft.Build.Evaluation;
using System.IO;

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
                    rootElementSave = ObjectFinder.Self.GetRootStandardElementSave(state.ParentContainer);
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

            foreach (var variable in state.Variables.Where(item => 
                (item.GetRootName() == "Font" ||
                    item.GetRootName() == "FontSize" || 
                    item.GetRootName() == "OutlineThickness" ) 
                && item.Value != null
                ))
            {
                string prefix = null;

                if (variable.Name.Contains('.'))
                {
                    prefix = FileManager.RemoveExtension(variable.Name) + ".";
                }

                bool useCustomFont = rvf.GetValue<bool>(prefix + "UseCustomFont");
                if (!useCustomFont)
                {
                    var fontSizeVariableName = prefix + "FontSize";
                    var fontNameVariableName = prefix + "Font";
                    var fontOutlineVariableName = prefix + "OutlineThickness";

                    int fontSizeValue = rvf.GetValue<int>(fontSizeVariableName);
                    string fontNameValue = rvf.GetValue<string>(fontNameVariableName);
                    int outlineThickness = rvf.GetValue<int>(fontOutlineVariableName);

                    TryAddFontFromSizeAndName(topLevelOrRecursive, listToFill, fontSizeValue, fontNameValue, outlineThickness);
                }
            }
        }

        private static void TryAddFontFromSizeAndName(TopLevelOrRecursive topLevelOrRecursive, List<string> listToFill, int fontSizeValue, string fontNameValue, int outlineThickness)
        {
            if (!string.IsNullOrEmpty(fontNameValue))
            {
                string fontFileName = global::RenderingLibrary.Graphics.Fonts.BmfcSave.GetFontCacheFileNameFor(fontSizeValue, fontNameValue, outlineThickness);

                fontFileName = FileManager.RelativeDirectory + fontFileName;

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
                                LoadGumxIfNecessaryFromDirectory(FileManager.RelativeDirectory, force:true);
                                GetFilesReferencedBy(Gum.Managers.ObjectFinder.Self.GumProjectSave, topLevelOrRecursive, listToFill, projectOrDisk);
                            }
                            break;
                        case "gucx":
                            {
                                ComponentSave gumComponentSave = null;
                                try
                                {
                                    gumComponentSave = FileManager.XmlDeserialize<ComponentSave>(absoluteFileName);
                                    gumComponentSave.FileName = absoluteFileName;
                                    // See an explanation for this in LoadGumxIfNecessaryFromDirectory
                                    gumComponentSave.Initialize(gumComponentSave.DefaultState);
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
                                    // See an explanation for this in LoadGumxIfNecessaryFromDirectory
                                    gumScreenSave.Initialize(gumScreenSave.DefaultState);

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
                                    // See an explanation for this in LoadGumxIfNecessaryFromDirectory
                                    standardElementSave.Initialize(standardElementSave.DefaultState);

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
                    string gumxFile = GetGumProjectFile(gumxDirectory);

                    // This can occur if the user has added a gumx file, then removed it, but the project still
                    // references gum files like gum screens or components.  This is an invalid setup, but we're
                    // going to handle it more gracefully than throwing exceptions.
                    if (!string.IsNullOrEmpty(gumxFile))
                    {

                        GumLoadResult result;

                        Gum.Managers.ObjectFinder.Self.GumProjectSave =
                            GumProjectSave.Load(gumxFile, out result);
                    }

                    InitializeElements();
                }

                // this used to be in the if-block above, but we want this
                // to always run because an already-loaded glux may have new
                // variables which are enums.
                // Update: If the .glux is loaded, we'll initialize the whole thing.
                // If an individual element changes (or we're looking for its references),
                // then we'll initialize that element specifically in the switch
                //InitializeElements();
            }
        }

        private static string GetGumProjectFile(string gumxDirectory)
        {
            var files = System.IO.Directory.GetFiles(gumxDirectory)
                .Where(item => item.ToLowerInvariant().EndsWith(".gumx"))
                .ToArray();

            bool multipleGumxFilesExist = files.Length > 1;

            string toReturn;

            if(files.Length > 1)
            {
                // Let's limit it to any files in the project.
                var gumxInProject = GlueState.Self.CurrentGlueProject.GetAllReferencedFiles().Where(item => item.Name.ToLowerInvariant().EndsWith("gumx"));

                var absoluteFiles = gumxInProject.Select(item => GlueCommands.Self.GetAbsoluteFileName(item).ToLowerInvariant());

                toReturn = files.FirstOrDefault(item => absoluteFiles.Contains(item.ToLowerInvariant()));
            }
            else
            {
                toReturn = files.FirstOrDefault();
            }

            return toReturn;
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

        internal void RemoveUnreferencedFilesFromVsProject()
        {
            var gumProject = ObjectFinder.Self.GumProjectSave;

            if (gumProject != null && FileManager.FileExists(gumProject.FullFileName) && GlueState.Self.CurrentGlueProject != null)
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
            List<ProjectItem> toRemove = new List<ProjectItem>();

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
        
        private static void FillWithCodeBuildItemsToRemove(GumProjectSave gumProject, List<ProjectItem> toRemove, ProjectBase project)
        {

            IEnumerable<ElementSave> allElements = gumProject.Components.OfType<ElementSave>().Concat(
                gumProject.StandardElements.OfType<ElementSave>()).Concat(
                gumProject.Screens.OfType<ElementSave>());

            var runtimeFolder = GlueState.Self.CurrentGlueProjectDirectory + "GumRuntimes/";

            foreach (var buildItem in project)
            {
                bool isRuntimeGenerated = buildItem.UnevaluatedInclude != null && buildItem.UnevaluatedInclude.ToLower().EndsWith("runtime.generated.cs");
                string includeDirectory = null;

                if(isRuntimeGenerated)
                {
                    includeDirectory = FileManager.GetDirectory(buildItem.UnevaluatedInclude);
                }
                    

                bool isInGumRuntimes = includeDirectory == runtimeFolder;

                if ( isRuntimeGenerated && isInGumRuntimes)
                {
                    // is there an element with this name?


                    var elementName = FileManager.RemoveExtension( FileManager.RemoveExtension( 
                        FileManager.RemovePath(buildItem.UnevaluatedInclude)));

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
        /// Obtains a list of files which can be removed from the Gum project. These include:
        ///  - Gum screens
        ///  - Gum components
        ///  - Gum standard elements
        ///  - Gum fonts
        ///  - Gum referenced files (so long as the .gumx file is in its own dedicated folder)
        /// </summary>
        /// <param name="gumProject">The Gum project to check.</param>
        /// <param name="toRemove">A list of which files to remove. The method populates this.</param>
        /// <param name="contentProject">The content project to check inside of for orphan references.</param>
        private static void FillWithContentBuildItemsToRemove(GumProjectSave gumProject, List<ProjectItem> toRemove, ProjectBase contentProject)

        {
            string fontCacheFolder = FileManager.GetDirectory(gumProject.FullFileName) + "FontCache/";

            string[] referencedGumFiles = null;

            bool hadMissingFile = false;

            try
            {
                referencedGumFiles = GlueCommands.Self.FileCommands.GetFilesReferencedBy(gumProject.FullFileName, TopLevelOrRecursive.Recursive)
                    .Select(item=>FileManager.Standardize(item).ToLowerInvariant())
                    .Distinct()
                    .ToArray();
            }
            catch(FileNotFoundException e)
            {
                // If an exception occurred here  becaus a file is missing from disk, just spit some output and leave it at that
                PluginManager.ReceiveError(e.Message);
                hadMissingFile = true;
            }

            if(!hadMissingFile)
            {
                var gumProjectFolder = FileManager.GetDirectory(gumProject.FullFileName);
                var glueContentFolder = FileManager.GetDirectory(contentProject.FullFileName);
                var gumFolderRelative = FileManager.MakeRelative(gumProjectFolder, glueContentFolder);

                bool isGumProjectInOwnFolder = glueContentFolder != gumProjectFolder && FileManager.MakeRelative(gumProjectFolder, glueContentFolder).Contains("..") == false;

                foreach (var buildItem in contentProject)
                {

                    bool shouldRemove = GetIfShouldRemoveFontFile(toRemove, buildItem, fontCacheFolder, contentProject, referencedGumFiles);

                    if (shouldRemove)
                    {
                        toRemove.Add(buildItem);
                    }

                    if(!shouldRemove && isGumProjectInOwnFolder)
                    {
                        shouldRemove = GetIfShouldRemoveGumRelativeFile(gumProject, buildItem, glueContentFolder, referencedGumFiles);
                    }

                    if(!shouldRemove)
                    {
                        shouldRemove = GetIfShouldRemoveStandardElement(gumProject, toRemove, buildItem, gumFolderRelative);
                    }

                    if(!shouldRemove)
                    {
                        shouldRemove = GetIfShouldRemoveComponent(gumProject, buildItem, gumFolderRelative);
                    }

                    if(!shouldRemove)
                    {
                        shouldRemove = GetIfShouldRemoveScreen(gumProject, buildItem, gumFolderRelative);
                    }

                    if (shouldRemove)
                    {
                        toRemove.Add(buildItem);
                    }
                }
            }
        }

        private static bool GetIfShouldRemoveGumRelativeFile(GumProjectSave gumProject, ProjectItem buildItem, string glueContentFolder, string[] referencedGumFiles)
        {
            string contentFullFileName = FileManager.Standardize(glueContentFolder + buildItem.UnevaluatedInclude).ToLowerInvariant();

            string gumFolder = FileManager.GetDirectory(gumProject.FullFileName);

            string fileRelativeToGum = FileManager.MakeRelative(contentFullFileName, gumFolder);

            bool isFileInGumContent = fileRelativeToGum.Contains("..") == false;

            if(isFileInGumContent && !referencedGumFiles.Contains(contentFullFileName) && FileManager.GetExtension(fileRelativeToGum) != "gumx")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool GetIfShouldRemoveFontFile(List<ProjectItem> toRemove, ProjectItem buildItem, string fontCacheFolder, ProjectBase contentProject, string[] fileReferencedByGumx)
        {
            string contentFullFileName = FileManager.GetDirectory(contentProject.FullFileName) + buildItem.UnevaluatedInclude;

            bool isFontCacheFile = FileManager.IsRelativeTo(contentFullFileName, fontCacheFolder);

            if(isFontCacheFile)
            {
                var invariant = FileManager.Standardize(contentFullFileName).ToLowerInvariant();

                bool isReferenced = fileReferencedByGumx.Contains(invariant);

                return !isReferenced;

            }

            return false;
        }

        private static bool GetIfShouldRemoveStandardElement(GumProjectSave gumProject, List<ProjectItem> toRemove, ProjectItem buildItem, string gumFolderRelative)
        {
            bool shouldRemove = false;

            string extension = FileManager.GetExtension(buildItem.UnevaluatedInclude);

            if (extension == GumProjectSave.StandardExtension)
            {
                var standardFolder = gumFolderRelative + ElementReference.StandardSubfolder + "/";

                string elementName = FileManager.RemoveExtension(FileManager.MakeRelative(buildItem.UnevaluatedInclude, standardFolder))
                    .Replace("/", "\\");

                bool exists = gumProject.StandardElements.Any(item => item.Name.Replace("/", "\\").ToLowerInvariant() == elementName.ToLowerInvariant());

                if (!exists)
                {
                    toRemove.Add(buildItem);
                }
            }

            return shouldRemove;
        }

        private static bool GetIfShouldRemoveComponent(GumProjectSave gumProject, ProjectItem buildItem, string gumFolderRelative)
        {
            bool shouldRemove = false;

            string extension = FileManager.GetExtension(buildItem.UnevaluatedInclude);

            if (extension == GumProjectSave.ComponentExtension)
            {
                var componentFolder = gumFolderRelative + ElementReference.ComponentSubfolder + "/";

                string elementName = FileManager.RemoveExtension(FileManager.MakeRelative(buildItem.UnevaluatedInclude, componentFolder))
                    .Replace("/", "\\");

                bool exists = gumProject.Components.Any(item => item.Name.Replace("/", "\\").ToLowerInvariant() == elementName.ToLowerInvariant());

                if (!exists)
                {
                    shouldRemove = true;
                }
            }

            return shouldRemove;
        }

        private static bool GetIfShouldRemoveScreen(GumProjectSave gumProject, ProjectItem buildItem, string gumFolderRelative)
        {
            bool shouldRemove = false;

            string extension = FileManager.GetExtension(buildItem.UnevaluatedInclude);

            if (extension == GumProjectSave.ScreenExtension)
            {
                var screenFolder = gumFolderRelative + ElementReference.ScreenSubfolder + "/";

                string elementName = FileManager.RemoveExtension(FileManager.MakeRelative(buildItem.UnevaluatedInclude, screenFolder))
                    .Replace("/", "\\");

                bool exists = gumProject.Screens.Any(item => item.Name.ToLowerInvariant() == elementName.ToLowerInvariant());

                shouldRemove = !exists;

            }

            return shouldRemove;
        }
    }
}
