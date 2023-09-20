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
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.IO;
using RenderingLibrary.Graphics;
using GeneralResponse = ToolsUtilities.GeneralResponse;

namespace GumPlugin.Managers
{
    public class FileReferenceTracker : Singleton<FileReferenceTracker>
    {
        #region Enums

        enum ProjectOrDisk
        {
            Project,
            Disk
        }

        #endregion

        #region Fields/Properties

        public bool UseAtlases
        {
            get;
            set;

        }

        #endregion

        public static bool CanTrackDependenciesOn(string fileName)
        {

            string extension = FileManager.GetExtension(fileName);
            var isOfGumExtension = 
                extension == GumProjectSave.ComponentExtension ||
                extension == "gumx" ||
                extension == GumProjectSave.ScreenExtension ||
                extension == GumProjectSave.StandardExtension ||
                // We want to refresh code when this gets changed, so we have to return true for .ganx files:
                extension == "ganx"
                ;

            bool shouldTrackDependencies = false;

            if (isOfGumExtension && Gum.Managers.ObjectFinder.Self.GumProjectSave != null)
            {
                var gumProjectDirectory =
                    FileManager.GetDirectory(Gum.Managers.ObjectFinder.Self.GumProjectSave.FullFileName);

                shouldTrackDependencies = FileManager.IsRelativeTo(fileName, gumProjectDirectory);
            }

            return shouldTrackDependencies;
        }

        
        private void GetFilesReferencedBy(GumProjectSave gumProjectSave, TopLevelOrRecursive topLevelOrRecursive, List<string> listToFill, ProjectOrDisk projectOrDisk)
        {
            AppState.Self.GumProjectSave = gumProjectSave;

            foreach (var element in gumProjectSave.Screens)
            {
                AddElementFileFor(listToFill, element, projectOrDisk);
            }

            foreach (var element in gumProjectSave.Components)
            {
                AddElementFileFor(listToFill, element, projectOrDisk);
            }

            foreach (var element in gumProjectSave.StandardElements)
            {
                AddElementFileFor(listToFill, element, projectOrDisk);
            }

            StringFunctions.RemoveDuplicates(listToFill);
        }

        private void AddElementFileFor(List<string> listToFill, ElementSave element, ProjectOrDisk projectOrDisk)
        {
            var gumProject = AppState.Self.GumProjectSave;

            // This could be a link:
            // Even though it could be linked, we require files be copied over to the current location so they can be added to a .csproj and the project can be kept in-tact
            //var reference =
            //    gumProject.ScreenReferences.FirstOrDefault(item => item.Name == element.Name) ??
            //    gumProject.ComponentReferences.FirstOrDefault(item => item.Name == element.Name) ??
            //    gumProject.StandardElementReferences.FirstOrDefault(item => item.Name == element.Name);

            //if(reference?.Link != null)
            //{
            //    var fullFileName = new FilePath(FileManager.RelativeDirectory + reference.Link).FullPath;
            //    listToFill.Add(fullFileName);

            //}
            //else
            {
                string fullFileName = FileManager.RelativeDirectory + element.Subfolder + "\\" + element.Name +
                    "." + element.FileExtension;

                fullFileName = FileManager.RemoveDotDotSlash(fullFileName);

                listToFill.Add(fullFileName);

            }
        }

        private void GetFilesReferencedBy(ElementSave element, List<string> listToFill, ProjectOrDisk projectOrDisk)
        {
            TopLevelOrRecursive topLevelOrRecursive = TopLevelOrRecursive.TopLevel;
            if (!string.IsNullOrEmpty(element.FileName) && projectOrDisk == ProjectOrDisk.Disk)
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

                TryGetFontReferences(topLevelOrRecursive, listToFill, state, false);

                GetRegularVariableFileReferences(listToFill, state, element.Instances, projectOrDisk);

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
                    AddElementFileFor(listToFill, referencedElement, projectOrDisk);

                    if (topLevelOrRecursive == TopLevelOrRecursive.Recursive)
                    {
                        GetFilesReferencedBy(referencedElement, listToFill, projectOrDisk);
                    }
                }
            }

            StringFunctions.RemoveDuplicates(listToFill);
        }

        private void GetRegularVariableFileReferences(List<string> listToFill, Gum.DataTypes.Variables.StateSave state, IList<InstanceSave> instances, ProjectOrDisk projectOrDisk)
        {
            var fileVariables = state.Variables
                .Where(item => item.IsFile &&  !string.IsNullOrEmpty(item.Value as string))
                .ToArray();

            foreach (var variable in fileVariables)
            {
                bool isGraphicFile = FileManager.IsGraphicFile(variable.Value as string);

                bool shouldConsider = !UseAtlases || !isGraphicFile || projectOrDisk == ProjectOrDisk.Disk;

                var instanceName = variable.SourceObject;

                if(shouldConsider && !string.IsNullOrEmpty(instanceName))
                {
                    // make sure this isn't a left-over variable reference 
                    var foundInstance = instances.FirstOrDefault(item => item.Name == instanceName);

                    shouldConsider = foundInstance != null;
                }

                if (shouldConsider)
                {

                    if (IsNineSliceSource(variable, state))
                    {
                        string variableValue = variable.Value as string;

                        var shouldUsePattern = NineSliceExtensions.GetIfShouldUsePattern(variableValue);

                        if (shouldUsePattern)
                        {
                            string extension = FileManager.GetExtension(variableValue);

                            string variableWithoutExtension = FileManager.RemoveExtension(variableValue);

                            string bareTexture = NineSliceExtensions.GetBareTextureForNineSliceTexture(
                                variableValue);

                            foreach (var side in NineSliceExtensions.PossibleNineSliceEndings)
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
                    if(instance != null)
                    {
                        rootElementSave = ObjectFinder.Self.GetElementSave(instance.BaseType);
                    }
                }

                return rootElementSave is StandardElementSave && rootElementSave.Name == "NineSlice";
            }

            return false;

        }

        private static void TryGetFontReferences(TopLevelOrRecursive topLevelOrRecursive, List<string> listToFill, Gum.DataTypes.Variables.StateSave state, bool includeReferenceInfo)
        {

            Gum.DataTypes.RecursiveVariableFinder rvf = new RecursiveVariableFinder(state);

            var element = state.ParentContainer;
            var isParentElementText = false;
            if(element is StandardElementSave && element.Name == "Text")
            {
                isParentElementText = true;
            }
            else
            {
                var baseStandardElement = ObjectFinder.Self.GetRootStandardElementSave(element);
                isParentElementText = baseStandardElement?.Name == "Text";
            }

            var fontVariables = state.Variables.Where(item =>
                (item.GetRootName() == "Font" ||
                    item.GetRootName() == "FontSize" ||
                    item.GetRootName() == "OutlineThickness" ||
                    item.GetRootName() == "IsItalic" ||
                    item.GetRootName() == "IsBold" ||
                    item.GetRootName() == "UseFontSmoothing"
                    )
                && item.Value != null
                );

            foreach (var variable in fontVariables)
            {
                string prefix = null;

                // Just because this has a variable for Font or FontSize or whatever doesn't
                // necessarily mean that this is a text object - it could have been an instance
                // that at one time was a text object, but was later converted to a different type
                // which no longer uses fonts. 

                var isTextObject = false;


                if (variable.Name.Contains('.'))
                {
                    var instanceName = FileManager.RemoveExtension(variable.Name);

                    prefix = FileManager.RemoveExtension(variable.Name) + ".";

                    var instance = state.ParentContainer.GetInstance(instanceName);

                    if(instance != null)
                    {
                        var basicElement = ObjectFinder.Self.GetRootStandardElementSave( instance.GetBaseElementSave());

                        isTextObject = basicElement?.Name == "Text";
                    }
                    else
                    {
                        // This code is used to
                        // determine whether a referenced
                        // file is necessary in the project.
                        // Since the instance doesn't exist, we
                        // won't actually use the variable for the
                        // instance, so we don't want to track the file.
                        // We can do this by marking isTextObject as false.
                        isTextObject = false; 
                    }
                }
                else
                {
                    isTextObject = isParentElementText;
                }


                if(isTextObject)
                {
                    bool useCustomFont = rvf.GetValue<bool>(prefix + "UseCustomFont");
                    if (!useCustomFont)
                    {
                        var fontSizeVariableName = prefix + "FontSize";
                        var fontNameVariableName = prefix + "Font";
                        var fontOutlineVariableName = prefix + "OutlineThickness";
                        var fontSmoothingVariableName = prefix + "UseFontSmoothing";
                        var isBoldVariableName = prefix + "IsBold";
                        var isItalicVariableName = prefix + "IsItalic";


                        int fontSizeValue = rvf.GetValue<int>(fontSizeVariableName);
                        string fontNameValue = rvf.GetValue<string>(fontNameVariableName);
                        int outlineThickness = rvf.GetValue<int>(fontOutlineVariableName);
                        bool useFontSmoothing = rvf.GetValue<bool>(fontSmoothingVariableName);
                        
                        bool isBold = rvf.GetValue<bool>(isBoldVariableName);
                        bool isItalic = rvf.GetValue<bool>(isItalicVariableName);

                        string additionalInfo = null;
                        if(includeReferenceInfo)
                        {
                            additionalInfo = $" by {variable} in {state}";
                        }

                        TryAddFontFromSizeAndName(topLevelOrRecursive, listToFill, fontSizeValue, fontNameValue, outlineThickness, useFontSmoothing, isBold, isItalic, additionalInfo);
                    }
                }
            }
        }

        private static void TryAddFontFromSizeAndName(TopLevelOrRecursive topLevelOrRecursive, List<string> listToFill, 
            int fontSizeValue, string fontNameValue, int outlineThickness, bool useFontSmoothing, bool isBold, bool isItalic,  string suffix)
        {
            if (!string.IsNullOrEmpty(fontNameValue))
            {
                // copy this to get rid of XNA nonsense, will need to update this if we ever add more font support
                string fontFileName = GetFontCacheFileNameFor(fontSizeValue, fontNameValue, outlineThickness, useFontSmoothing,  isItalic, isBold);

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

                        listToFill.Add(absoluteFileName + suffix);
                    }

                }
            }
        }

        static string GetFontCacheFileNameFor(int fontSize, string fontName, int outline, bool useFontSmoothing, bool isItalic, bool isBold)
        {
            string fileName = null;


            // don't allow some charactersin the file name:
            fontName = fontName.Replace(' ', '_');

            fileName = "Font" + fontSize + fontName;
            if (outline != 0)
            {
                fileName = "Font" + fontSize + fontName + "_o" + outline;
            }

            if (useFontSmoothing == false)
            {
                fileName += "_noSmooth";
            }

            if (isItalic)
            {
                fileName += "_Italic";
            }

            if (isBold)
            {
                fileName += "_Bold";
            }

            fileName += ".fnt";

            fileName = System.IO.Path.Combine("FontCache", fileName);

            return fileName;
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
                GetReferencesInProjectOrDisk(fileName, listToFill, projectOrDisk);
            }
            catch(Exception e)
            {
                PluginManager.ReceiveError("Non-critical error: " + e.Message);
                FileManager.RelativeDirectory = oldRelativeDirectory;
            }
        }
        
        internal GeneralResponse HandleFillWithReferencedFiles(FilePath filePath, List<FilePath> listToFill)
        {
            ProjectOrDisk projectOrDisk = ProjectOrDisk.Project;

            List<string> stringListToFill = new List<string>();

            var respnse = GetReferencesInProjectOrDisk(filePath.Standardized,
                stringListToFill, projectOrDisk);

            listToFill.AddRange(stringListToFill
                .Select(item => new FilePath(item)));

            return respnse;
        }

        private GeneralResponse GetReferencesInProjectOrDisk(string fileName, List<string> listToFill, ProjectOrDisk projectOrDisk)
        {
            GeneralResponse generalResponse = GeneralResponse.SuccessfulResponse;

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
                var topLevelOrRecursive = TopLevelOrRecursive.TopLevel;
                string errors = null;
                if (System.IO.File.Exists(absoluteFileName))
                {
                    switch (extension)
                    {
                        case "gumx":
                            {
                                try
                                {
                                    LoadGumxIfNecessaryFromDirectory(FileManager.RelativeDirectory, force:true);

                                    var gumProject = Gum.Managers.ObjectFinder.Self.GumProjectSave;
                                    if(gumProject != null)
                                    {
                                        GetFilesReferencedBy(gumProject, topLevelOrRecursive, listToFill, projectOrDisk);
                                    }
                                }
                                catch(Exception e)
                                {
                                    errors =
                                        "Error tracking Gum references for " + absoluteFileName + "\n" + e.ToString();
                                }
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
                                    GetFilesReferencedBy(gumComponentSave, listToFill, projectOrDisk);
                                }
                                catch (Exception e)
                                {
                                    errors =
                                        "Error tracking Gum references for " + absoluteFileName + "\n" + e.ToString();
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

                                    GetFilesReferencedBy(gumScreenSave, listToFill, projectOrDisk);
                                }
                                catch (Exception e)
                                {
                                    errors =
                                        "Error tracking Gum references for " + absoluteFileName + "\n" + e.ToString();
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

                                    GetFilesReferencedBy(standardElementSave, listToFill, projectOrDisk);
                                }
                                catch (Exception e)
                                {
                                    errors =
                                        "Error tracking Gum references for " + absoluteFileName + "\n" + e.ToString();
                                }
                            }
                            break;
                    }
                }

                if(errors != null)
                {
                    generalResponse = new GeneralResponse
                    {
                        Succeeded = false,
                        Message = errors
                    };
                }

                FileManager.RelativeDirectory = oldRelative;

            }

            return generalResponse;
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

                        if(!string.IsNullOrWhiteSpace(result?.ErrorMessage))
                        {
                            GlueCommands.Self.PrintError(result.ErrorMessage);
                        }

                    }
                    if(Gum.Managers.ObjectFinder.Self.GumProjectSave != null)
                    {
                        ObjectFinder.Self.EnableCache();
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

                bool wasAnythingChanged = RemoveUnreferencedCodeAndContentFilesFromProjects(gumProject, codeProject, contentProject);
                shouldSave |= wasAnythingChanged;

                foreach(VisualStudioProject syncedProject in GlueState.Self.SyncedProjects)
                {
                    wasAnythingChanged = RemoveUnreferencedCodeAndContentFilesFromProjects(gumProject, syncedProject,
                        (VisualStudioProject)syncedProject.ContentProject);
                    shouldSave |= wasAnythingChanged;
                }


                if (shouldSave)
                {
                    GlueCommands.Self.ProjectCommands.SaveProjects();
                }
            }
        }

        private static bool RemoveUnreferencedCodeAndContentFilesFromProjects(GumProjectSave gumProject, VisualStudioProject codeProject, VisualStudioProject contentProject)
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
        
        private static void FillWithCodeBuildItemsToRemove(GumProjectSave gumProject, List<ProjectItem> toRemove, VisualStudioProject project)
        {

            var allElements = gumProject.Components.OfType<ElementSave>().Concat(
                gumProject.StandardElements.OfType<ElementSave>()).Concat(
                gumProject.Screens.OfType<ElementSave>())
                .ToArray();

            var gumRuntimeFolder = new FilePath(GlueState.Self.CurrentGlueProjectDirectory + "GumRuntimes/");
            var formsFolder = new FilePath( GlueState.Self.CurrentGlueProjectDirectory + "Forms/");

            bool IsFormsOrGumRuntime(ProjectItem projectItem)
            {
                return  projectItem.UnevaluatedInclude?.EndsWith("runtime.generated.cs", StringComparison.OrdinalIgnoreCase) ?? 
                        projectItem.UnevaluatedInclude?.EndsWith("runtime.cs", StringComparison.OrdinalIgnoreCase) ??
                        projectItem.UnevaluatedInclude?.EndsWith("forms.generated.cs", StringComparison.OrdinalIgnoreCase) ?? 
                        projectItem.UnevaluatedInclude?.EndsWith("forms.cs", StringComparison.OrdinalIgnoreCase) ?? false;
            }

            var codeItemsMadeForGumObjects = project.EvaluatedItems.Where(IsFormsOrGumRuntime).ToArray();

            foreach (var buildItem in codeItemsMadeForGumObjects)
            {
                var includeDirectory = new FilePath( FileManager.GetDirectory(buildItem.UnevaluatedInclude));
                    
                bool isInGumRuntimes = gumRuntimeFolder.IsRootOf(includeDirectory);
                bool isInForms = formsFolder.IsRootOf(includeDirectory);

                ElementSave GetElement()
                {
                    // strip off forms
                    var elementName = FileManager.RemoveExtension(buildItem.UnevaluatedInclude);
                    if(elementName.EndsWith(".Generated"))
                    {
                        elementName = FileManager.RemoveExtension(elementName);
                    }
                    // strip off the element type:
                    elementName = elementName.Replace("\\", "/");

                    string RemoveFirstDirectory(string path)
                    {
                        var firstDirectory = path.Split('/')[0];
                        return FileManager.MakeRelative(path, firstDirectory);
                    }

                    if (isInForms)
                    {
                        // it's going to be in forms/component type
                        // "elementName" will end with "Forms"
                        elementName = RemoveFirstDirectory(elementName);
                        // and the type
                        elementName = RemoveFirstDirectory(elementName);

                        elementName = elementName.Substring(0, elementName.Length - "Forms".Length);
                    }
                    else if(isInGumRuntimes)
                    {
                        // for gum runtimes we don't include the type directory (for now?)
                        elementName = RemoveFirstDirectory(elementName);
                        elementName = elementName.Substring(0, elementName.Length - "Runtime".Length);
                    }

                    var foundElement = allElements.FirstOrDefault(item => item.Name == elementName);
                    return foundElement;
                }


                if ( isInGumRuntimes && GetElement() == null)
                {
                    toRemove.Add(buildItem);
                }
                else if(isInForms && GetElement() == null)
                {
                    toRemove.Add(buildItem);
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
        private static void FillWithContentBuildItemsToRemove(GumProjectSave gumProject, List<ProjectItem> toRemove, VisualStudioProject contentProject)

        {
            string fontCacheFolder = FileManager.GetDirectory(gumProject.FullFileName) + "FontCache/";

            string[] referencedGumFiles = null;

            bool hadMissingFile = false;

            try
            {
                referencedGumFiles = GlueCommands.Self.FileCommands.GetFilesReferencedBy(gumProject.FullFileName, TopLevelOrRecursive.Recursive)
                    .Select(item=>FileManager.Standardize(item.FullPath).ToLowerInvariant())
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
                var glueContentFolder = FileManager.GetDirectory(contentProject.FullFileName.FullPath);
                var gumFolderRelative = FileManager.MakeRelative(gumProjectFolder, glueContentFolder);

                bool isGumProjectInOwnFolder = glueContentFolder != gumProjectFolder && FileManager.MakeRelative(gumProjectFolder, glueContentFolder).Contains("..") == false;

                foreach (var buildItem in contentProject.EvaluatedItems)
                {

                    bool shouldRemove = GetIfShouldRemoveFontFile(toRemove, buildItem, fontCacheFolder, contentProject, referencedGumFiles);

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
            string contentFullFileName = FileManager.GetDirectory(contentProject.FullFileName.FullPath) + buildItem.UnevaluatedInclude;

            contentFullFileName = FileManager.RemoveDotDotSlash(contentFullFileName);

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
