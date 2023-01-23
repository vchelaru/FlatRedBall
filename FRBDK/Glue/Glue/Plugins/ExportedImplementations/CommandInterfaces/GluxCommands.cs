using System;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using Glue;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.FormHelpers;
using System.Windows.Forms;
using FlatRedBall.Glue.Parsing;
using System.Collections.Generic;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.VSHelpers.Projects;
using System.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.IO;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.Glue.IO;
using System.Linq;
using FlatRedBall.Glue.StandardTypes;
using EditorObjects.SaveClasses;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.ViewModels;
using FlatRedBall.Glue.SetVariable;
using FlatRedBall.Glue.IO.Zip;
using FlatRedBall.Glue.Errors;
using GlueSaveClasses;
using System.Text;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Plugins.Interfaces;
using GlueFormsCore.ViewModels;
using GlueFormsCore.Managers;
//using FlatRedBall.Utilities;
//using ToolsUtilities;
using GeneralResponse = ToolsUtilities.GeneralResponse;
using System.Threading.Tasks;
using FlatRedBall.Utilities;
using static FlatRedBall.Glue.Plugins.PluginManager;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;
//using Gum.DataTypes;
using Newtonsoft.Json;
using FlatRedBall.Entities;
using System.Windows.Forms.Design;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    #region ObjectsToRemove Class
    public class ObjectsToRemove
    {
        public List<NamedObjectSave> DerivedNamedObjects { get; set; } = new List<NamedObjectSave>();
        public List<NamedObjectSave> SubObjectsInList { get; set; } = new List<NamedObjectSave>();
        public List<NamedObjectSave> CollisionRelationships { get; set; } = new List<NamedObjectSave>();

        public List<CustomVariable> CustomVariables { get; set; } = new List<CustomVariable>();
        public List<EventResponseSave> EventResponses { get; set; } = new List<EventResponseSave>();

    }
    #endregion

    public class GluxCommands : IGluxCommands
    {
        #region Fields / Properties

        const string NoType = "<No Type>";

        IProjectCommands projectCommands = new ProjectCommands();

        ElementCommands mElementCommands = new ElementCommands();

        static GluxCommands mSelf;

        public static GluxCommands Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new GluxCommands();
                }
                return mSelf;
            }
        }

        public IScreenCommands ScreenCommands => mElementCommands;


        public IEntityCommands EntityCommands
        {
            get { return mElementCommands; }
        }

        public IElementCommands ElementCommands
        {
            get { return mElementCommands; }
        }

        public IProjectCommands ProjectCommands
        {
            get { return projectCommands; }
        }

        public string StartUpScreenName
        {
            get { return GlueState.Self.CurrentGlueProject?.StartUpScreen; }
            set
            {
                // if statement is here to prevent unnecessary saves
                if (GlueState.Self.CurrentGlueProject.StartUpScreen != value)
                {
                    var oldStartupScreen = GlueState.Self.CurrentGlueProject.Screens
                        .FirstOrDefault(item => item.Name == GlueState.Self.CurrentGlueProject.StartUpScreen);


                    GlueState.Self.CurrentGlueProject.StartUpScreen = value;
                    GluxCommands.Self.SaveGlux();
                    if (string.IsNullOrEmpty(ProjectManager.GameClassFileName))
                    {
                        GlueCommands.Self.DialogCommands.ShowMessageBox(
                            "Could not set the startup screen because Glue could not find the Game class.");
                    }
                    else
                    {
                        GlueCommands.Self.GenerateCodeCommands.GenerateStartupScreenCode();
                    }

                    var screen = GlueState.Self.CurrentGlueProject.Screens
                        .FirstOrDefault(item => item.Name == value);

                    if (oldStartupScreen != null)
                    {
                        GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(oldStartupScreen);
                    }
                    if (screen != null)
                    {
                        GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(screen);
                    }
                    else
                    {
                        GlueCommands.Self.RefreshCommands.RefreshTreeNodes();
                    }
                    GlueCommands.Self.RefreshCommands.RefreshErrors();

                    PluginManager.ReactToChangedStartupScreen();
                }
            }
        }
        #endregion

        #region Save Glux Methods

        /// <summary>
        /// Saves the glux/gluj (and all elements) if already in a task. Adds a save task if not.
        /// </summary>
        public void SaveGlux(TaskExecutionPreference taskExecutionPreference = TaskExecutionPreference.Asap)
        {
            TaskManager.Self.AddOrRunIfTasked(
                () => SaveGlueProjectImmediately(),
                "Saving Glue Project",
                // asap because otherwise this may get added
                // after a reload command
                taskExecutionPreference);
        }

        public async Task SaveElementAsync(GlueElement element)
        {
            await TaskManager.Self.AddAsync(async () =>
            {
                var target = GlueState.Self.GlueProjectFileName;
                var glueDirectory = target.GetDirectoryContainingThis();

                var extension = element is SaveClasses.ScreenSave ? GlueProjectSave.ScreenExtension
                    : GlueProjectSave.EntityExtension;

                var fileToIgnore = glueDirectory + element.Name + "." + extension;

                FileWatchManager.IgnoreNextChangeOnFile(fileToIgnore);

                // todo - eventually need to handle wildcards here

                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.Formatting = Formatting.Indented;
                settings.DefaultValueHandling = DefaultValueHandling.Ignore;

                var serialized = JsonConvert.SerializeObject(element, settings);

                var locationToSave = glueDirectory + element.Name + "." + extension;

                if (element is EntitySave entitySave)
                {
                    await PluginManager.ReactToEntityJsonSaveAsync(entitySave.Name, serialized);
                }
                else if (element is ScreenSave screenSave)
                {
                    await PluginManager.ReactToScreenJsonSaveAsync(screenSave.Name, serialized);
                }
                FileManager.SaveText(serialized, locationToSave);

            }, $"{nameof(SaveElementAsync)} {element}");
        }

        /// <summary>
        /// Saves only the .gluj file and not the other elements. If the Glue project is using
        /// an old version, then the entire .glux is saved.
        /// </summary>
        /// <returns>An awaitable task which completes once the task is finished</returns>
        public async void SaveGlujFile(TaskExecutionPreference taskExecutionPreference = TaskExecutionPreference.Asap)
        {
            if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.SeparateJsonFilesForElements)
            {
                await TaskManager.Self.AddAsync(
                    () => GlueState.Self.CurrentGlueProject.SaveGlujFile("GLUE", GlueState.Self.GlueProjectFileName.FullPath),
                    nameof(SaveGlujFile),
                    taskExecutionPreference);
            }
            else
            {
                SaveGlux(taskExecutionPreference);
            }
        }

        /// <summary>
        /// Saves the current project immediately - this should not 
        /// be called except in very rare circumstances as it will run right away and may result
        /// in multiple threads accessing the glux at the same time.
        /// </summary>
        public void SaveGlueProjectImmediately()
        {
            if (ProjectManager.GlueProjectSave != null)
            {

                if (MainGlueWindow.Self.HasErrorOccurred)
                {
                    var projectName = GlueState.Self.GlueProjectFileName.NoPathNoExtension;

                    GlueCommands.Self.DialogCommands.ShowMessageBox("STOP RIGHT THERE!!!!  There was an error in Glue at some point.  To prevent " +
                        "corruption in the GLUX file, Glue will no longer save any changes that you make to the project.  " +
                        "You should exit out of Glue immediately and attempt to solve the problem.\n\n" +
                        "Glue has saved your work in a temporary file called " + projectName + ".gluxERROR"
                        );


                    ProjectManager.GlueProjectSave.Save("GLUE", GlueState.Self.GlueProjectFileName.FullPath + "ERROR");
                }
                else
                {
                    ProjectSyncer.UpdateSyncedProjectsInGlux();

                    // Jan 20, 2020
                    // .NET Core XmlSerialization
                    // requires no enums - values must
                    // be ints, so let's convert them:
                    if (ProjectManager.GlueProjectSave.FileVersion < (int)GlueProjectSave.GluxVersions.GlueSavedToJson)
                    {
                        ProjectManager.GlueProjectSave.ConvertEnumerationValuesToInts();
                    }


                    // October 27, 2011
                    // Instead of saving
                    // directly to disk we're
                    // going to serialize to a
                    // string.  If the serialization
                    // fails, we won't try to save the
                    // file to disk.
                    try
                    {
                        ProjectManager.GlueProjectSave.TestSave("GLUE");
                    }
                    catch (Exception e)
                    {
                        // break here because we want to look at other threads to see what's running at the same time...
                        System.Diagnostics.Debugger.Break();
                        var wasAbleToSaveError = false;
                        string errorLogLocation = FileManager.UserApplicationDataForThisApplication + "ExceptionInGlue.txt";
                        try
                        {
                            FileManager.SaveText(e.ToString(), errorLogLocation);
                            wasAbleToSaveError = true;
                        }
                        catch
                        {
                            // If this fails that's okay, we're already in a failed state.
                        }
                        GlueCommands.Self.DoOnUiThread(() =>
                        {

                            var message = "Error trying to save your Glue file.  Because of this error, Glue did not make any changes to the file on disk.";
                            if (wasAbleToSaveError)
                            {
                                message += "\n\nAn error log has been saved here:\n" + errorLogLocation;

                                var mbmb = new MultiButtonMessageBoxWpf();
                                mbmb.MessageText = message;
                                mbmb.AddButton("Open Error File", true);
                                mbmb.AddButton("Do nothing (Glue probably needs to be restarted)", false);

                                if (mbmb.ShowDialog() == true)
                                {
                                    var clickedResult = (bool)mbmb.ClickedResult;

                                    if (clickedResult)
                                    {
                                        try
                                        {
                                            //System.Diagnostics.Process.Start(errorLogLocation);
                                            var p = new System.Diagnostics.Process();
                                            p.StartInfo = new System.Diagnostics.ProcessStartInfo(errorLogLocation)
                                            {
                                                UseShellExecute = true
                                            };
                                            p.Start();
                                        }
                                        catch
                                        {
                                            try
                                            {
                                                System.Diagnostics.Process.Start(FileManager.GetDirectory(errorLogLocation));
                                            }
                                            catch
                                            {
                                                // do nothing
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                message += "\n\nGlue should probably be restarted.";
                                GlueCommands.Self.DialogCommands.ShowMessageBox(message);
                            }

                            PluginManager.ReceiveError("Error saving glux:\n\n" + e.ToString());
                        });

                        MainGlueWindow.Self.HasErrorOccurred = true;
                    }

                    if (!MainGlueWindow.Self.HasErrorOccurred)
                    {
                        List<FilePath> fileChangesToIgnore = GlueState.Self.CurrentGlueProject.GetAllSerializedFiles(GlueState.Self.GlueProjectFileName);
                        foreach (var fileToIgnore in fileChangesToIgnore)
                        {
                            FileWatchManager.IgnoreNextChangeOnFile(fileToIgnore);
                        }

                        Exception lastException;
                        var succeeded = ProjectManager.GlueProjectSave.Save("GLUE", GlueState.Self.GlueProjectFileName.FullPath, out lastException);

                        if (!succeeded)
                        {
                            GlueCommands.Self.DialogCommands.ShowMessageBox("Error saving the .glux:\n" + lastException);
                        }
                    }
                }
            }
        }

        #endregion

        #region Custom Classes

        public ValidationResponse AddNewCustomClass(string className, out CustomClassSave customClassSave)
        {
            ValidationResponse validationResponse = new ValidationResponse();
            customClassSave = null;
            string whyIsntValid;
            if (!NameVerifier.IsCustomClassNameValid(className, out whyIsntValid))
            {
                validationResponse.OperationResult = OperationResult.Failure;
                validationResponse.Message = whyIsntValid;
            }
            else if (ProjectManager.GlueProjectSave.GetCustomClass(className) != null)
            {
                validationResponse.OperationResult = OperationResult.Failure;
                validationResponse.Message = $"The custom class {className} already exists";
            }
            else
            {
                validationResponse.OperationResult = OperationResult.Success;
                customClassSave = new CustomClassSave();
                customClassSave.Name = className;
                ProjectManager.GlueProjectSave.CustomClasses.Add(customClassSave);

                GlueCommands.Self.GluxCommands.SaveGlux();
                GlueCommands.Self.GenerateCodeCommands.GenerateCustomClassesCode();

            }

            return validationResponse;
        }

        #endregion

        #region ReferencedFileSave

        public async Task<ReferencedFileSave> CreateNewFileAndReferencedFileSaveAsync(AddNewFileViewModel viewModel, GlueElement element, object creationOptions = null)
        {
            ReferencedFileSave rfs = null;

            element = element ?? GlueState.Self.CurrentElement;
            string directory = null;

            if (GlueState.Self.CurrentTreeNode?.IsDirectoryNode() == true)
            {
                directory = GlueState.Self.CurrentTreeNode.GetRelativeFilePath().Replace("/", "\\");
            }

            await TaskManager.Self.AddAsync(() =>
            {
                string name = viewModel.FileName;
                AssetTypeInfo resultAssetTypeInfo =
                    viewModel.SelectedAssetTypeInfo;

                string errorMessage;


                rfs = GlueProjectSaveExtensionMethods.AddReferencedFileSave(
                    element, directory, name, resultAssetTypeInfo,
                    creationOptions, out errorMessage);

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    GlueCommands.Self.DialogCommands.ShowMessageBox(errorMessage);
                }
                else if (rfs != null)
                {

                    var createdFile = GlueCommands.Self.GetAbsoluteFileName(rfs);

                    if (createdFile.EndsWith(".csv"))
                    {
                        CsvCodeGenerator.GenerateAndSaveDataClass(rfs, AvailableDelimiters.Comma);
                    }

                    if (element != null)
                    {
                        GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element);
                        GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
                    }
                    else
                    {
                        GlueCommands.Self.RefreshCommands.RefreshGlobalContent();
                        GlueCommands.Self.GenerateCodeCommands.GenerateGlobalContentCode();
                    }
                    GlueState.Self.CurrentReferencedFileSave = rfs;

                    PluginManager.ReactToNewFile(rfs);

                    GluxCommands.Self.SaveGlux();
                }

            }, $"Adding file with name {viewModel.FileName}");

            return rfs;
        }


        public ReferencedFileSave AddReferencedFileToGlobalContent(string fileToAdd, bool useFullPathAsName)
        {

            if (FileManager.IsRelative(fileToAdd) == false)
            {
                throw new ArgumentException("The argument fileToAdd must be relative to the Glue project");
            }


            var referencedFileSave = new ReferencedFileSave();
            referencedFileSave.DestroyOnUnload = false;
            referencedFileSave.SetNameNoCall(fileToAdd);
            referencedFileSave.IsSharedStatic = true;
            referencedFileSave.HasPublicProperty = true;

            // We include
            // the directory
            // as part of the
            // name because if
            // a user adds multiple
            // ReferencedFileSaves to
            // GlobalContent it's very
            // likely that there will be
            // some kind of naming conflict.
            // Doing this reduces the chances
            // of this to almost 0.
            // UPDATE July 18, 2011
            // Turns out this method
            // is called if either a new
            // RFS is added or if it is dragged
            // on to GlobalContent from an Entity.
            // Therefore, we only want to do this if
            // the useFullPathAsName argument is true;
            if (useFullPathAsName)
            {
                referencedFileSave.IncludeDirectoryRelativeToContainer = true;
            }

            AddReferencedFileToGlobalContent(referencedFileSave);

            return referencedFileSave;
        }

        public async Task<ReferencedFileSave> AddExistingReferencedFileToGlobalContent(ReferencedFileSave referencedFileSave, bool includeDirectoryInGlobalContentInName)
        {

            ReferencedFileSave newRfs = null;

            await TaskManager.Self.AddAsync(() =>
            {
                newRfs = AddReferencedFileToGlobalContent(referencedFileSave.Name, includeDirectoryInGlobalContentInName);

                // copy the properties from the old RFS to the new one:
                // Nov 15, 2022
                // Not sure if all properties should be used or if we should
                // explicitly assign properties. For now I'm going to address
                // the issues I'm aware of and this can be added to later. The
                // main issue I'm solving is that Gum screens added to global content
                // are not using the correct type when being loaded:
                newRfs.RuntimeType = referencedFileSave.RuntimeType;
            }, nameof(AddExistingReferencedFileToGlobalContent));

            return newRfs;
        }

        public Task AddReferencedFileToGlobalContentAsync(ReferencedFileSave referencedFileSave, bool generateAndSave = true, bool updateUi = true)
        {
            return TaskManager.Self.AddAsync(() =>
            {
                AddReferencedFileToGlobalContent(referencedFileSave, generateAndSave, updateUi);
            }, nameof(AddReferencedFileToGlobalContentAsync) + " " + referencedFileSave.Name);
        }


        [Obsolete("use AddReferencedFileToGlobalContentAsync")]
        public void AddReferencedFileToGlobalContent(ReferencedFileSave referencedFileSave)
        {
            AddReferencedFileToGlobalContent(referencedFileSave, generateAndSave: true, updateUi: true);
        }

        [Obsolete("use AddReferencedFileToGlobalContentAsync")]
        public void AddReferencedFileToGlobalContent(ReferencedFileSave referencedFileSave, bool generateAndSave, bool updateUi)
        {
            TaskManager.Self.WarnIfNotInTask();
            var project = GlueState.Self.CurrentGlueProject;
            project.GlobalFiles.Add(referencedFileSave);
            project.GlobalContentHasChanged = true;

            GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(referencedFileSave);


            // Update any element that may reference this file because now it may mean the element
            // will simply reference it from GlobalContent instead of using the content manager.
            var elements = ObjectFinder.Self.GetAllElementsReferencingFile(referencedFileSave.Name);

            foreach (var element in elements)
            {
                element.HasChanged = true;
            }

            if (generateAndSave)
            {
                GlueCommands.Self.GluxCommands.SaveGlux(TaskExecutionPreference.AddOrMoveToEnd);
                GlueCommands.Self.GenerateCodeCommands.GenerateGlobalContentCode();
            }

            if (updateUi)
            {
                GlueCommands.Self.DoOnUiThread(GlueCommands.Self.RefreshCommands.RefreshGlobalContent);
            }

            // Dec 22, 2022 - how did we get this far without notifying the plugin system?
            // Perhaps this is handled elsewhere? It should be here...
            PluginManager.ReactToNewFile(referencedFileSave);
        }


        // Vic asks: What's the difference between AddSingleFileTo and CreateReferencedFileSaveForExistingFile? The name 
        // CreateReferencedFileSaveForExistingFile suggests it's newer/more complete, but why not obsolete this?
        // I think we should obsolete this
        [Obsolete("Use GluxCommands.CreateReferencedFileSaveForExistingFile")]
        public ReferencedFileSave AddSingleFileTo(string fileName, string rfsName,
            string extraCommandLineArguments,
            BuildToolAssociation buildToolAssociation, bool isBuiltFile,
            object options, GlueElement sourceElement, string directoryOfTreeNode,
            bool selectFileAfterCreation = true
            )
        {
            ReferencedFileSave toReturn = null;

            //string directoryOfTreeNode = EditorLogic.CurrentTreeNode.GetRelativePath();

            string targetDirectory = FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces.ElementCommands.GetFullPathContentDirectory(sourceElement, directoryOfTreeNode);
            string targetFile = fileName;

            string errorMessage = null;
            bool failed = false;

            if (isBuiltFile)
            {
                targetFile = targetDirectory + rfsName + "." + buildToolAssociation.DestinationFileType;
            }

            string targetFileWithOriginalExtension = FileManager.RemoveExtension(targetFile) + "." + FileManager.GetExtension(fileName);

            bool copied = false;

            var projectRootDirectory = ProjectManager.ProjectRootDirectory;

            if (!FileManager.IsRelativeTo(fileName, projectRootDirectory) && isBuiltFile)
            {
                copied = PluginManager.TryCopyFile(fileName, targetFileWithOriginalExtension);

                if (!copied)
                {
                    errorMessage = $"Could not add the file\n{fileName}\n\nBecause it is not relative to\n{ProjectManager.ProjectRootDirectory}\n\nPlease move this file to a folder inside your project and try again";
                    failed = true;
                }
                else
                {
                    // the file was copied - from now on just use the copied file name:
                    fileName = targetFileWithOriginalExtension;
                }
            }

            if (!failed && isBuiltFile)
            {
                errorMessage = buildToolAssociation.PerformBuildOn(fileName, targetFile, extraCommandLineArguments, PluginManager.ReceiveOutput, PluginManager.ReceiveError);
                failed = true;
            }

            if (!failed)
            {
                string creationReport;

                string directoryToUse = null;

                if (!isBuiltFile)
                {
                    directoryToUse = directoryOfTreeNode;
                }

                var assetTypeInfo = AvailableAssetTypes.Self.GetAssetTypeFromExtension(FileManager.GetExtension(targetFile));


                toReturn = CreateReferencedFileSaveForExistingFile(
                    sourceElement, directoryToUse, targetFile, PromptHandleEnum.Prompt,
                    assetTypeInfo,
                    out creationReport, out errorMessage);

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    failed = true;
                }
                else if (toReturn != null && string.IsNullOrEmpty(toReturn.Name))
                {
                    errorMessage = "There was an error creating the named object for\n" + fileName;
                    failed = true;

                }
            }

            if (!failed)
            {
                if (isBuiltFile)
                {
                    toReturn.SourceFile = ProjectManager.MakeRelativeContent(fileName);
                    toReturn.AdditionalArguments = extraCommandLineArguments;
                    toReturn.BuildTool = buildToolAssociation.ToString();

                    // If a background sync is happening, this can lock the thread, so we want to
                    // make sure this doesn't happen at the same time as a background sync:
                    TaskManager.Self.AddParallelTask(() =>
                        {
                            UpdateReactor.UpdateFile(GlueCommands.Self.GetAbsoluteFileName(toReturn));
                        },
                        "Updating file " + toReturn.Name);
                    string directoryOfFile = FileManager.GetDirectory(GlueCommands.Self.GetAbsoluteFileName(fileName, 
                        // not sure why this is false...
                        false));

                    RightClickHelper.SetExternallyBuiltFileIfHigherThanCurrent(directoryOfFile, false);
                }
            }

            if (!failed && toReturn != null)
            {
                TaskManager.Self.OnUiThread(() =>
                {
                    var element = GlueState.Self.CurrentElement;
                    if (element != null)
                    {
                        GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element);
                        GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
                    }
                    else
                    {
                        GlueCommands.Self.RefreshCommands.RefreshGlobalContent();
                        GlueCommands.Self.GenerateCodeCommands.GenerateGlobalContentCode();
                    }
                });

                TaskManager.Self.Add(() => GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(toReturn), $"Updating file membership for file {toReturn}");
                PluginManager.ReactToNewFile(toReturn);
                GluxCommands.Self.SaveGlux();
                GluxCommands.Self.ProjectCommands.SaveProjects();

            }

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                PluginManager.ReceiveError(errorMessage);
                // I think we should show an error message.  I had a user
                // try to add a file and no popup appeared telling them that
                // the entity was named that.
                GlueCommands.Self.DialogCommands.ShowMessageBox(errorMessage);
            }

            if (toReturn != null)
            {
                ApplyOptions(toReturn, options);

                if (selectFileAfterCreation)
                {
                    TaskManager.Self.Add(() =>
                       TaskManager.Self.OnUiThread(() =>
                          GlueState.Self.CurrentReferencedFileSave = toReturn), "Select new file");
                }
            }

            return toReturn;
        }


        [Obsolete("Use CreateReferencedFileSaveForExistingFileAsync")]
        public ReferencedFileSave CreateReferencedFileSaveForExistingFile(GlueElement containerForFile, FilePath filePath, AssetTypeInfo ati = null)
        {
            return CreateReferencedFileSaveForExistingFile(containerForFile,
                null,
                filePath.FullPath,
                PromptHandleEnum.Prompt,
                ati ?? AvailableAssetTypes.Self.GetAssetTypeFromExtension(filePath.Extension),
                out string creationReport,
                out string errorMessage
                );
        }

        public Task<ReferencedFileSave> CreateReferencedFileSaveForExistingFileAsync(GlueElement containerForFile, FilePath filePath, AssetTypeInfo ati = null)
        {
            return TaskManager.Self.AddAsync(() => CreateReferencedFileSaveForExistingFile(containerForFile,
                null,
                filePath.FullPath,
                PromptHandleEnum.Prompt,
                ati ?? AvailableAssetTypes.Self.GetAssetTypeFromExtension(filePath.Extension),
                out string creationReport,
                out string errorMessage
                ), "CreateReferencedFileSaveForExistingFileAsync");
        }



        public ReferencedFileSave CreateReferencedFileSaveForExistingFile(GlueElement containerForFile, string directoryInsideContainer, string absoluteFileName,
            PromptHandleEnum unknownTypeHandle, AssetTypeInfo ati, out string creationReport, out string errorMessage, bool selectFileAfterCreation = true)
        {
            creationReport = "";
            errorMessage = null;

            ReferencedFileSave referencedFileSaveToReturn = null;

            string whyItIsntValid;
            // Let's see if there is already an Entity with the same name
            string fileWithoutPath = FileManager.RemovePath(FileManager.RemoveExtension(absoluteFileName));

            bool isValid =
                NameVerifier.IsReferencedFileNameValid(fileWithoutPath, ati, referencedFileSaveToReturn, containerForFile, out whyItIsntValid);

            if (!isValid)
            {
                errorMessage = "Invalid file name:\n" + fileWithoutPath + "\n" + whyItIsntValid;
            }
            else
            {
                Zipper.UnzipAndModifyFileIfZip(ref absoluteFileName);
                string extension = FileManager.GetExtension(absoluteFileName);

                bool isValidExtensionOrIsConfirmedByUser;
                bool isUnknownType;
                FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces.ElementCommands.CheckAndWarnAboutUnknownFileTypes(
                    unknownTypeHandle, extension, out isValidExtensionOrIsConfirmedByUser, out isUnknownType);

                string fileToAdd = null;
                if (isValidExtensionOrIsConfirmedByUser)
                {

                    string directoryThatFileShouldBeRelativeTo =
                        FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces.ElementCommands.GetFullPathContentDirectory(containerForFile, directoryInsideContainer);

                    string projectDirectory = ProjectManager.ContentProject.GetAbsoluteContentFolder();

                    bool needsToCopy = !FileManager.IsRelativeTo(absoluteFileName, projectDirectory);


                    if (needsToCopy)
                    {
                        fileToAdd = directoryThatFileShouldBeRelativeTo + FileManager.RemovePath(absoluteFileName);
                        fileToAdd = FileManager.MakeRelative(fileToAdd, ProjectManager.ContentProject.GetAbsoluteContentFolder());

                        try
                        {
                            FileHelper.RecursivelyCopyContentTo(absoluteFileName,
                                FileManager.GetDirectory(absoluteFileName),
                                directoryThatFileShouldBeRelativeTo);
                        }
                        catch (System.IO.FileNotFoundException fnfe)
                        {
                            errorMessage = "Could not copy the files because of a missing file: " + fnfe.Message;
                        }
                    }
                    else
                    {
                        fileToAdd =
                            FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces.ElementCommands.GetNameOfFileRelativeToContentFolder(absoluteFileName, directoryThatFileShouldBeRelativeTo, projectDirectory);

                    }

                }

                if (string.IsNullOrEmpty(errorMessage))
                {
                    BuildToolAssociation bta = null;

                    if (ati != null && !string.IsNullOrEmpty(ati.CustomBuildToolName))
                    {
                        bta =
                            BuildToolAssociationManager.Self.GetBuilderToolAssociationByName(ati.CustomBuildToolName);
                    }

                    if (!string.IsNullOrEmpty(fileToAdd))
                    {
                        if (containerForFile != null)
                        {
                            referencedFileSaveToReturn = containerForFile.AddReferencedFile(fileToAdd, ati, bta);
                        }
                        else
                        {
                            bool useFullPathAsName = false;
                            // todo - support built files here
                            referencedFileSaveToReturn = AddReferencedFileToGlobalContent(fileToAdd, useFullPathAsName);
                        }
                    }



                    // This will be null if there was an error above in creating this file
                    if (referencedFileSaveToReturn != null)
                    {
                        if (containerForFile != null)
                            containerForFile.HasChanged = true;

                        if (fileToAdd.EndsWith(".csv"))
                        {
                            string fileToAddAbsolute = GlueCommands.Self.GetAbsoluteFileName(fileToAdd,
                                // not sure why this is false
                                false);
                            CsvCodeGenerator.GenerateAndSaveDataClass(referencedFileSaveToReturn, referencedFileSaveToReturn.CsvDelimiter);
                        }
                        if (isUnknownType)
                        {
                            referencedFileSaveToReturn.LoadedAtRuntime = false;
                        }

                        string error;
                        referencedFileSaveToReturn.RefreshSourceFileCache(false, out error);

                        if (!string.IsNullOrEmpty(error))
                        {
                            ErrorReporter.ReportError(referencedFileSaveToReturn.Name, error, false);
                        }
                    }
                }
            }

            if (referencedFileSaveToReturn != null)
            {
                //ApplyOptions(toReturn, options);

                if (selectFileAfterCreation)
                {
                    TaskManager.Self.Add(() =>
                       TaskManager.Self.OnUiThread(() =>
                          GlueState.Self.CurrentReferencedFileSave = referencedFileSaveToReturn), "Select new file");
                }
            }

            return referencedFileSaveToReturn;
        }

        private static void ApplyOptions(ReferencedFileSave toReturn, object options)
        {
            if (toReturn.IsCsvOrTreatedAsCsv)
            {
                toReturn.CreatesDictionary = ((string)options) == "Dictionary";
            }
        }

        [Obsolete("Use RemoveReferencedFileAsync")]
        public void RemoveReferencedFile(ReferencedFileSave referencedFileToRemove, List<string> additionalFilesToRemove, bool regenerateAndSave = true)
        {
            TaskManager.Self.AddOrRunIfTasked(() =>
                RemoveReferencedFileInternal(referencedFileToRemove, additionalFilesToRemove, regenerateAndSave),
                $"Removing referenced file {referencedFileToRemove}");
        }

        public async Task RenameNamedObjectSave(NamedObjectSave namedObjectSave, string newName, bool performSaveAndGenerateCode = true, bool updateUi = true)
        {
            // This function wraps old functionality of handling renaming of NOS's. The old functionality is a reactive system, where you set the name
            // and then see if it works. If not, it gets undone. This is a little crappy but...for now we're at least going to wrap the functionality so
            // the caller is not exposed to the crappiness. Then, in the future we can adjust this.

            var canProceed = true;
            if(namedObjectSave.DefinedByBase)
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox($"{namedObjectSave} can not be renamed because it is defined by base");
                canProceed = false;
            }

            if(canProceed)
            {
                var oldName = namedObjectSave.InstanceName;
                namedObjectSave.InstanceName = newName;
                await EditorObjects.IoC.Container.Get<NamedObjectSetVariableLogic>().ReactToNamedObjectChangedInstanceName(namedObjectSave, oldName);

                if(performSaveAndGenerateCode)
                {
                    GlueCommands.Self.GluxCommands.SaveGlux(TaskExecutionPreference.AddOrMoveToEnd);
                }

                if(updateUi)
                {
                    GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();
                    GlueCommands.Self.RefreshCommands.RefreshVariables();
                }

            }
        }


        public async Task RemoveReferencedFileAsync(ReferencedFileSave referencedFileToRemove, List<string> additionalFilesToRemove, bool regenerateAndSave = true)
        {
            await TaskManager.Self.AddAsync(() =>
                RemoveReferencedFileInternal(referencedFileToRemove, additionalFilesToRemove, regenerateAndSave),
                $"Removing referenced file {referencedFileToRemove}");
        }

        async Task RemoveReferencedFileInternal(ReferencedFileSave referencedFileToRemove, List<string> additionalFilesToRemove, bool regenerateAndSave = true)
        {
            // January 13, 2023
            // note about removing
            // wildcard files - Wildcard
            // files are added to the project
            // if there is a wildcard pattern in
            // the .gluj. Therefore, the only way
            // to remove a file from the project is
            // to delete the file on disk. There is no
            // exclusion pattern support currently, which
            // would be required to have files on disk with
            // a wildcard pattern without being added to the
            // project. Therefore, until we do have exclusion
            // support, wildcard files will be deleted here.
            // Note that when files are removed through the tree
            // view, the additionalFilesToRemove is populated and
            // the files are deleted. However, there may be other code
            // that calls this and it should be a fully-featured call which
            // removes all reference files, so we'll add the logic to delete
            // the file here if it's a wildcard.

            var isContained = GlueState.Self.Find.IfReferencedFileSaveIsReferenced(referencedFileToRemove);
            /////////////////////////Early Out//////////////////////////////
            if (!isContained)
            {
                return;
            }
            ////////////////////////End Early Out/////////////////////////////

            // allow sending null here if the caller doesn't care:
            additionalFilesToRemove = additionalFilesToRemove ?? new List<string>();

            // There are some things that need to happen:
            // 1.  Remove the ReferencedFileSave from the Glue project (GLUX)
            // 2.  Remove the GUI item
            // 3.  Remove the item from the Visual Studio project.
            var container = referencedFileToRemove.GetContainer();

            #region Remove the file from the current Screen or Entity if there is a current Screen or Entity

            if (container != null)
            {
                // The referenced file better be a globally referenced file


                if (!container.ReferencedFiles.Contains(referencedFileToRemove))
                {
                    throw new ArgumentException();
                }
                else
                {
                    container.ReferencedFiles.Remove(referencedFileToRemove);

                }
                // Ask about any NamedObjects that reference this file.                
                for (int i = container.NamedObjects.Count - 1; i > -1; i--)
                {
                    var nos = container.NamedObjects[i];
                    if (nos.SourceType == SourceType.File && nos.SourceFile == referencedFileToRemove.Name)
                    {
                        GlueCommands.Self.DoOnUiThread(() =>
                        {
                            if (nos.DefinedByBase)
                            {
                                // don't remove it, just tell the user that the object will be broken until fixed
                                GlueCommands.Self.DialogCommands.ShowMessageBox($"The object {nos} is using the file {referencedFileToRemove}, but the file is being removed.\n\n" +
                                    $"The project may be broken until this object is fixed");
                            }
                            else
                            {
                                // Ask the user what to do here - remove it?  Keep it and not compile?
                                var mbmb = new MultiButtonMessageBoxWpf();
                                mbmb.MessageText = "The object\n" + nos.ToString() + "\nreferences the file\n" + referencedFileToRemove.Name +
                                    "\nWhat would you like to do?";
                                mbmb.AddButton("Remove this object", DialogResult.Yes);
                                mbmb.AddButton("Keep it (object will not be valid until changed)", DialogResult.No);

                                var result = mbmb.ShowDialog();

                                if (result == true && mbmb.ClickedResult is DialogResult dialogResult && dialogResult == DialogResult.Yes)
                                {
                                    container.NamedObjects.RemoveAt(i);
                                }
                            }

                        });

                    }
                    nos.ResetVariablesReferencing(referencedFileToRemove);
                }
                if (container != null)
                {
                    var isCurrentElement = container == GlueState.Self.CurrentElement;

                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(container);

                    if (container.ReferencedFiles.Count > 0 && isCurrentElement)
                    {
                        GlueState.Self.CurrentReferencedFileSave = container.ReferencedFiles.LastOrDefault();
                    }
                    else
                    {
                        // This should refresh the selection...
                        GlueState.Self.CurrentElement = container;
                    }
                }
                if (regenerateAndSave && container != null)
                {
                    await GlueCommands.Self.GenerateCodeCommands.GenerateElementCodeAsync(container);
                }

            }
            #endregion

            #region else, the file is likely part of the GlobalContentFile

            else
            {
                ProjectManager.GlueProjectSave.GlobalFiles.Remove(referencedFileToRemove);
                ProjectManager.GlueProjectSave.GlobalContentHasChanged = true;

                // Much faster to just remove the tree node.  This was done
                // to reuse code and make things reactive, but this has gotten
                // slow on bigger projects.
                //ElementViewWindow.UpdateGlobalContentTreeNodes(false); // don't save here because projects will get saved below 
                GlueCommands.Self.RefreshCommands.RefreshGlobalContent();

                GlobalContentCodeGenerator.UpdateLoadGlobalContentCode();

                var elements = ObjectFinder.Self.GetAllElementsReferencingFile(referencedFileToRemove.Name);

                if (regenerateAndSave)
                {
                    foreach (var element in elements)
                    {
                        await GlueCommands.Self.GenerateCodeCommands.GenerateElementCodeAsync(element);
                    }
                }
            }

            #endregion

            var allFilePaths = GlueCommands.Self.FileCommands.GetAllReferencedFileNames();

            var rfsFilePath = GlueCommands.Self.GetAbsoluteFilePath(referencedFileToRemove);

            var isFileReferenced = allFilePaths.Contains(rfsFilePath);

            if (isFileReferenced == false)
            {
                isFileReferenced = FileReferenceManager.Self.IsFileReferencedRecursively(rfsFilePath);
            }

            var isWildcard = referencedFileToRemove.IsCreatedByWildcard;
            string absoluteName = GlueCommands.Self.GetAbsoluteFileName(referencedFileToRemove);
            if(isWildcard && System.IO.File.Exists(absoluteName))
            {
                FileHelper.MoveToRecycleBin(absoluteName);
            }

            if (isFileReferenced == false)
            {
                if(!isWildcard)
                {
                    // It's already been removed, no need to ask the user about it as a file to delete
                    additionalFilesToRemove.Add(referencedFileToRemove.GetRelativePath());
                }

                string itemName = referencedFileToRemove.GetRelativePath();

                // I don't know why we were removing the file from the ProjectBase - it should
                // be from the Content project
                //ProjectManager.RemoveItemFromProject(ProjectManager.ProjectBase, itemName);
                ProjectManager.RemoveItemFromProject(ProjectManager.ProjectBase.ContentProject, itemName, performSave: false);

                foreach (ProjectBase syncedProject in ProjectManager.SyncedProjects)
                {
                    ProjectManager.RemoveItemFromProject(syncedProject.ContentProject, absoluteName);
                }
            }

            if (GlueCommands.Self.FileCommands.IsContent(rfsFilePath))
            {
                UnreferencedFilesManager.Self.RefreshUnreferencedFiles(false);
                foreach (var file in UnreferencedFilesManager.LastAddedUnreferencedFiles)
                {
                    additionalFilesToRemove.Add(file.FilePath);
                }
            }

            ReactToRemovalIfCsv(referencedFileToRemove, additionalFilesToRemove);

            PluginManager.ReactToFileRemoved(container, referencedFileToRemove);

            if (regenerateAndSave)
            {
                GluxCommands.Self.SaveGlux();
            }
        }

        private static void ReactToRemovalIfCsv(ReferencedFileSave referencedFileToRemove, List<string> additionalFilesToRemove)
        {
            if (referencedFileToRemove.IsCsvOrTreatedAsCsv)
            {
                string name = referencedFileToRemove.GetTypeForCsvFile();
                // If the CSV uses a custom class then the user should not
                // be asked if the file for that class should be removed.  That
                // class was created independent of any CSV (perhaps by hand, perhaps
                // by plugin), so the user should have to explicitly remove the class.

                var customClass = ObjectFinder.Self.GlueProject.GetCustomClassReferencingFile(referencedFileToRemove.Name);

                if (customClass == null)
                {

                    var first = ObjectFinder.Self.GetAllReferencedFiles().FirstOrDefault(item => item.IsCsvOrTreatedAsCsv && item.GetTypeForCsvFile() == name);

                    if (first == null)
                    {
                        // Remove the class
                        string whatToRemove = "DataTypes/" + name + ".Generated.cs";

                        additionalFilesToRemove.Add(whatToRemove);

                    }
                }

                // See if this uses a custom class.  If so, remove the CSV from
                // the class' list.
                if (customClass != null)
                {
                    customClass.CsvFilesUsingThis.Remove(referencedFileToRemove.Name);
                }
            }
        }

        public ReferencedFileSave GetReferencedFileSaveFromFile(string fileName)
        {
            FilePath filePath = new FilePath(fileName);
            if (FileManager.IsRelative(fileName))
            {
                filePath = GlueCommands.Self.GetAbsoluteFilePath(fileName);
            }
            return GetReferencedFileSaveFromFile(filePath);
        }

        public ReferencedFileSave GetReferencedFileSaveFromFile(FilePath filePath)
        {
            ////////////////Early Out//////////////////////////////////
            var invalidPathChars = Path.GetInvalidPathChars();
            if (invalidPathChars.Any(item => filePath.FullPath.Contains(item)))
            {
                // This isn't a RFS, because it's got a bad path. Early out here so that FileManager.IsRelative doesn't throw an exception
                return null;
            }

            //////////////End Early Out////////////////////////////////


            var project = ObjectFinder.Self.GlueProject;
            if (project != null)
            {
                // dont' foreach here. Technically this should be called on tasks, but if it's not, let's make this safe:

                foreach (ScreenSave screenSave in project.Screens.ToArray())
                {
                    foreach (ReferencedFileSave rfs in screenSave.ReferencedFiles)
                    {
                        var absoluteRfs = GlueCommands.Self.GetAbsoluteFilePath(rfs);
                        if (absoluteRfs == filePath)
                        {
                            return rfs;
                        }
                    }
                }

                lock (project.Entities)
                {
                    foreach (EntitySave entitySave in project.Entities.ToArray())
                    {
                        foreach (ReferencedFileSave rfs in entitySave.ReferencedFiles)
                        {
                            var absoluteRfs = GlueCommands.Self.GetAbsoluteFilePath(rfs);
                            if (absoluteRfs == filePath)
                            {
                                return rfs;
                            }
                        }
                    }
                }

                foreach (ReferencedFileSave rfs in project.GlobalFiles.ToArray())
                {
                    var absoluteRfs = GlueCommands.Self.GetAbsoluteFilePath(rfs);
                    if (absoluteRfs == filePath)
                    {
                        return rfs;
                    }
                }
            }

            return null;
        }

        [Obsolete("Use AddReferencedFileToElementAsync")]
        public void AddReferencedFileToElement(ReferencedFileSave rfs, GlueElement element)
        {
            element.ReferencedFiles.Add(rfs);
            element.HasChanged = true;

            GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(rfs);

            GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element);
        }

        public async Task AddReferencedFileToElementAsync(ReferencedFileSave rfs, GlueElement element, bool performSaveAndGenerateCode = true, bool updateUi = true)
        {
            element.ReferencedFiles.Add(rfs);
            element.HasChanged = true;

            GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(rfs);

            if(updateUi)
            {
                GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element);

                await GlueCommands.Self.GluxCommands.SaveElementAsync(element);
            }

            if(performSaveAndGenerateCode)
            {
                await GlueCommands.Self.GenerateCodeCommands.GenerateElementCodeAsync(element);

            }
        }

        public async Task DuplicateAsync(ReferencedFileSave rfs, GlueElement forcedContainer = null)
        {
            await TaskManager.Self.AddAsync(async () =>
            {
                var file = GlueCommands.Self.GetAbsoluteFileName(rfs);

                var newRfs = rfs.Clone();

                var stripped = FileManager.RemovePath(FileManager.RemoveExtension(newRfs.Name));

                var container = forcedContainer ?? rfs.GetContainer();

                var directoryOnDisk = FileManager.GetDirectory(file);
                var extension = FileManager.GetExtension(rfs.Name);

                while (!NameVerifier.IsReferencedFileNameValid(stripped,
                    newRfs.GetAssetTypeInfo(), 
                    newRfs, 
                    container, 
                    out string throwaway) ||
                    System.IO.File.Exists(directoryOnDisk + stripped + "." + extension)
                    )
                {
                    stripped = StringFunctions.IncrementNumberAtEnd(stripped);
                }

                newRfs.Name = FileManager.GetDirectory(rfs.Name, RelativeType.Relative) + stripped + "." + FileManager.GetExtension(rfs.Name);

                var destinationFile = FileManager.GetDirectory(file) + stripped + "." + FileManager.GetExtension(file);

                System.IO.File.Copy(file, destinationFile);


                var customClass = GlueState.Self.CurrentGlueProject.GetCustomClassReferencingFile(rfs.Name);

                if (customClass != null)
                {
                    customClass.CsvFilesUsingThis.Add(newRfs.Name);
                }
                if (container != null)
                {
                    await GlueCommands.Self.GluxCommands.AddReferencedFileToElementAsync(newRfs, container);
                }
                else
                {
                    await GlueCommands.Self.GluxCommands.AddReferencedFileToGlobalContentAsync(newRfs);
                }

                GlueCommands.Self.GluxCommands.SaveGlux();
                GlueCommands.Self.ProjectCommands.SaveProjects();

                GlueState.Self.CurrentReferencedFileSave = newRfs;

            }, $"Duplicating {rfs}");
        }

        #endregion

        #region NamedObjectSave

        public async Task<NamedObjectSave> AddNewNamedObjectToSelectedElementAsync(AddObjectViewModel addObjectViewModel)
        {
            var elementToAddTo = GlueState.Self.CurrentElement;

            var currentList =
                GlueState.Self.CurrentNamedObjectSave;


            var isMatchingList = currentList != null && currentList.IsList &&
                (currentList.SourceClassGenericType == addObjectViewModel.SourceClassType ||
                 currentList.SourceClassGenericType == addObjectViewModel.SelectedAti?.FriendlyName ||
                 currentList.SourceClassGenericType == addObjectViewModel.SelectedAti?.QualifiedRuntimeTypeName.QualifiedType);

            if (!isMatchingList && currentList != null)
            {
                var newAti = addObjectViewModel.SelectedAti;
                isMatchingList = currentList.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.ShapeCollection &&
                    (newAti == AvailableAssetTypes.CommonAtis.AxisAlignedRectangle ||
                     newAti == AvailableAssetTypes.CommonAtis.Circle ||
                     newAti == AvailableAssetTypes.CommonAtis.Polygon ||
                     newAti == AvailableAssetTypes.CommonAtis.Line);

            }

            if (!isMatchingList)
            {
                currentList = ObjectFinder.Self.GetDefaultListToContain(addObjectViewModel.SourceClassType, elementToAddTo);
            }


            return await AddNewNamedObjectToAsync(addObjectViewModel,
                elementToAddTo,
                currentList);
        }

        public async Task<NamedObjectSave> AddNewNamedObjectToAsync(AddObjectViewModel addObjectViewModel, GlueElement element, NamedObjectSave listToAddTo = null, bool selectNewNos = true)
        {
            NamedObjectSave newNos = new NamedObjectSave();
            newNos.SetDefaults();
            newNos.AttachToContainer = true;
            await TaskManager.Self.AddAsync(async () =>
            {
                if (element == null)
                {
                    throw new ArgumentNullException(nameof(element));
                }

                addObjectViewModel.ForcedElementToAddTo = element;
                MembershipInfo membershipInfo = NamedObjectSaveExtensionMethodsGlue.GetMemberMembershipInfo(addObjectViewModel.ObjectName);


                if (GlueState.Self.CurrentGlueProject.FileVersion >=
                    (int)GlueProjectSave.GluxVersions.ListsHaveAssociateWithFactoryBool)
                {
                    newNos.AssociateWithFactory = true;
                }

                newNos.InstanceName = addObjectViewModel.ObjectName;
                newNos.DefinedByBase = membershipInfo == MembershipInfo.ContainedInBase;

                if (addObjectViewModel.SourceType == SourceType.File)
                {
                    newNos.SourceType = addObjectViewModel.SourceType;
                    newNos.SourceFile = addObjectViewModel.SelectedItem?.MainText;
                    newNos.SourceName = addObjectViewModel.SourceNameInFile;
                    newNos.UpdateCustomProperties();
                }
                else if (addObjectViewModel.SourceClassType != NoType && !string.IsNullOrEmpty(addObjectViewModel.SourceClassType))
                {
                    newNos.SourceType = addObjectViewModel.SourceType;
                    newNos.SourceClassType =
                        addObjectViewModel.SelectedAti?.QualifiedRuntimeTypeName.QualifiedType ??
                        addObjectViewModel.SourceClassType;
                    newNos.SourceFile = addObjectViewModel.SelectedItem?.MainText;
                    newNos.SourceName = addObjectViewModel.SourceNameInFile;
                    newNos.UpdateCustomProperties();
                }

                newNos.SourceClassGenericType = addObjectViewModel.SourceClassGenericType;

                newNos.Properties.AddRange(addObjectViewModel.Properties);

                await AddNamedObjectToAsync(newNos, element, listToAddTo, selectNewNos);

            }, $"Adding Named Object {addObjectViewModel.ObjectName} to {element.Name}");
            return newNos;
        }

        public async Task AddNamedObjectToAsync(NamedObjectSave newNos, GlueElement element, NamedObjectSave listToAddTo = null, bool selectNewNos = true,
             bool performSaveAndGenerateCode = true, bool updateUi = true)
        {
            await TaskManager.Self.AddAsync(() =>
            {
                var ati = newNos.GetAssetTypeInfo();

                if (listToAddTo != null)
                {
                    NamedObjectSaveExtensionMethodsGlue.AddNamedObjectToList(newNos, listToAddTo);

                }
                else if (element != null)
                {
                    if (newNos.IsList)
                    {
                        var firstInstance = element.NamedObjects.FirstOrDefault(
                            item => item.IsList == false && item.IsLayer == false && item.IsCollisionRelationship() == false);

                        if (firstInstance != null)
                        {
                            var index = element.NamedObjects.IndexOf(firstInstance);
                            element.NamedObjects.Insert(index, newNos);
                        }
                        else
                        {
                            element.NamedObjects.Add(newNos);
                        }
                    }
                    else
                    {
                        element.NamedObjects.Add(newNos);
                    }
                }
                if (ati != null && ati.DefaultPublic)
                {
                    newNos.HasPublicProperty = true;
                }

                var entity = element as EntitySave;

                if (entity != null && entity.CreatedByOtherEntities && entity.PooledByFactory)
                {
                    bool wasAnythingAdded =
                        FlatRedBall.Glue.Factories.FactoryManager.AddResetVariablesFor(newNos);

                    if (wasAnythingAdded)
                    {
                        PluginManager.ReceiveOutput("Added reset variables for " + newNos);
                    }
                }

                if (performSaveAndGenerateCode)
                {
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
                }

                // run after generated code so plugins like level editor work off latest code
                PluginManager.ReactToNewObject(newNos);
                if (listToAddTo != null)
                {
                    PluginManager.ReactToObjectContainerChanged(newNos, listToAddTo);
                }

                if (updateUi)
                {
                    GlueCommands.Self.DoOnUiThread(() =>
                    {
                        MainGlueWindow.Self.PropertyGrid.Refresh();
                        PropertyGridHelper.UpdateNamedObjectDisplay();
                        GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element);

                        if (selectNewNos)
                        {
                            GlueState.Self.CurrentNamedObjectSave = newNos;
                        }
                    });
                }

                if (performSaveAndGenerateCode)
                {
                    GlueCommands.Self.GluxCommands.SaveGlux();
                }
            }, $"Adding named object {newNos.InstanceName} to {element.Name}");
        }

        public void RemoveNamedObject(NamedObjectSave namedObjectToRemove, bool performSaveAndGenerateCode = true,
            bool updateUi = true, List<string> additionalFilesToRemove = null)
        {
            StringBuilder removalInformation = new StringBuilder();

            var wasSelected = GlueState.Self.CurrentNamedObjectSave == namedObjectToRemove;

            int indexInChild = -1;
            NamedObjectSave containerOfRemoved = null;
            var element = namedObjectToRemove.GetContainer();
            if (wasSelected)
            {
                if (element.NamedObjects.Contains(namedObjectToRemove))
                {
                    indexInChild = element.NamedObjects.IndexOf(namedObjectToRemove);
                }
                else
                {
                    containerOfRemoved = element.NamedObjects.FirstOrDefault(item => item.ContainedObjects.Contains(namedObjectToRemove));

                    if (containerOfRemoved != null)
                    {
                        indexInChild = containerOfRemoved.ContainedObjects.IndexOf(namedObjectToRemove);
                    }
                }
            }

            // The additionalFilesToRemove is included for consistency with other methods.  It may be used later

            // There are the following things that need to happen:
            // 1.  Remove the NamedObject from the Glue project (GLUX)
            // 2.  Remove any variables that use this NamedObject as their source
            // 3.  Remove the named object from the GUI
            // 4.  Update the variables for any NamedObjects that use this element containing this NamedObject
            // 5.  Find any Elements that contain NamedObjects that are DefinedByBase - if so, see if we should remove those or make them not DefinedByBase
            // 6.  Remove any events that tunnel into this.
            TaskManager.Self.AddOrRunIfTasked(() =>
            {
                // caller doesn't care, so we're going to new it here and just fill it, but throw it away
                additionalFilesToRemove = additionalFilesToRemove ?? new List<string>();

                DoRemovalInternal(namedObjectToRemove, performSaveAndGenerateCode, updateUi, additionalFilesToRemove, removalInformation, wasSelected, indexInChild, containerOfRemoved, element);
            },
            "Performing object removal logic");
        }

        public async Task RemoveNamedObjectAsync(NamedObjectSave namedObjectToRemove, bool performSaveAndGenerateCode = true,
            bool updateUi = true, List<string> additionalFilesToRemove = null, bool notifyPluginsOfRemoval = true)
        {
            StringBuilder removalInformation = new StringBuilder();

            bool wasSelected;
            int indexInChild;
            NamedObjectSave containerOfRemoved;
            GlueElement element;
            GetSelectionInfo(namedObjectToRemove, out wasSelected, out indexInChild, out containerOfRemoved, out element);

            // The additionalFilesToRemove is included for consistency with other methods.  It may be used later

            // There are the following things that need to happen:
            // 1.  Remove the NamedObject from the Glue project (GLUX)
            // 2.  Remove any variables that use this NamedObject as their source
            // 3.  Remove the named object from the GUI
            // 4.  Update the variables for any NamedObjects that use this element containing this NamedObject
            // 5.  Find any Elements that contain NamedObjects that are DefinedByBase - if so, see if we should remove those or make them not DefinedByBase
            // 6.  Remove any events that tunnel into this.
            await TaskManager.Self.AddAsync(() =>
            {
                // caller doesn't care, so we're going to new it here and just fill it, but throw it away
                additionalFilesToRemove = additionalFilesToRemove ?? new List<string>();

                DoRemovalInternal(namedObjectToRemove, performSaveAndGenerateCode, updateUi, additionalFilesToRemove, removalInformation, wasSelected, indexInChild, containerOfRemoved, element, notifyPluginsOfRemoval);
            },
            "Performing object removal logic");
        }

        private static void GetSelectionInfo(NamedObjectSave namedObjectToRemove, out bool wasSelected, out int indexInChild, out NamedObjectSave containerOfRemoved, out GlueElement element)
        {
            wasSelected = GlueState.Self.CurrentNamedObjectSave == namedObjectToRemove;
            indexInChild = -1;
            containerOfRemoved = null;
            element = namedObjectToRemove.GetContainer();
            if (wasSelected)
            {
                if (element.NamedObjects.Contains(namedObjectToRemove))
                {
                    indexInChild = element.NamedObjects.IndexOf(namedObjectToRemove);
                }
                else
                {
                    containerOfRemoved = element.NamedObjects.FirstOrDefault(item => item.ContainedObjects.Contains(namedObjectToRemove));

                    if (containerOfRemoved != null)
                    {
                        indexInChild = containerOfRemoved.ContainedObjects.IndexOf(namedObjectToRemove);
                    }
                }
            }
        }

        public async Task RemoveNamedObjectListAsync(List<NamedObjectSave> namedObjectListToRemove, bool performSaveAndGenerateCode = true,
            bool updateUi = true, List<string> additionalFilesToRemove = null)
        {
            bool wasSelected =
                namedObjectListToRemove.Contains(GlueState.Self.CurrentNamedObjectSave);

            List<GlueElement> ownerList = null;

            await TaskManager.Self.AddAsync(() =>
                ownerList = namedObjectListToRemove.Select(item => ObjectFinder.Self.GetElementContaining(item)).ToList(),
                "Getting list of owners");

            foreach (var item in namedObjectListToRemove)
            {
                await RemoveNamedObjectAsync(item, performSaveAndGenerateCode: false, updateUi: false, additionalFilesToRemove: null, notifyPluginsOfRemoval: false);
            }

            var ownerHashSet = ownerList.ToHashSet();

            if (updateUi)
            {
                foreach (var owner in ownerHashSet)
                {
                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(owner);
                }
            }



            // If we aren't going to save the project, we probably don't
            // care about generating code either. I don't know if we ever
            // want to separate these variables, but we'll link them for now.
            if (performSaveAndGenerateCode)
            {
                foreach (var owner in ownerHashSet)
                {
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(owner);
                }
            }


            if (wasSelected)
            {
                var owner = ownerHashSet.FirstOrDefault();
                if (owner != null)
                {
                    var nos = owner.NamedObjects.FirstOrDefault();
                    if (nos != null)
                    {
                        GlueState.Self.CurrentNamedObjectSave = nos;
                    }
                    else
                    {
                        GlueState.Self.CurrentElement = owner;
                    }
                }
            }

            await PluginManager.ReactToObjectListRemovedAsync(ownerList, namedObjectListToRemove);

            if (performSaveAndGenerateCode)
            {
                if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.SeparateJsonFilesForElements)
                {
                    foreach(var element in ownerHashSet)
                    {
                        await GlueCommands.Self.GluxCommands.SaveElementAsync(element);
                    }
                }
                else
                {
                    GlueCommands.Self.GluxCommands.SaveGlux();
                }
            }
        }

        public async Task<List<ToolsUtilities.GeneralResponse<NamedObjectSave>>> CopyNamedObjectListIntoElement(List<NamedObjectSave> nosList, GlueElement targetElement, bool performSaveAndGenerateCode = true, bool updateUi = true)
        {
            var toReturn = new List<ToolsUtilities.GeneralResponse<NamedObjectSave>>();
            foreach (var originalNos in nosList)
            {
                var response = await CopyNamedObjectIntoElementInner(originalNos, targetElement, performSaveAndGenerateCode:false, updateUi: false, notifyPlugins: false);
                toReturn.Add(response);
            }

            if (updateUi)
            {
                GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(targetElement);
            }

            if (performSaveAndGenerateCode)
            {
                await GlueCommands.Self.GenerateCodeCommands
                    .GenerateElementAndReferencedObjectCode(targetElement);
            }

            var newNosList = toReturn.Select(item => item.Data).Where(item => item != null).ToList();

            await PluginManager.ReactToNewObjectListAsync(newNosList);

            List<ObjectContainerChange> changeList = new List<ObjectContainerChange>();

            foreach (var item in toReturn)
            {
                if (item.Succeeded)
                {
                    // this could be faster but I suspect it's not too slow:
                    var newListForObject = targetElement.NamedObjects.FirstOrDefault(candidateList => candidateList.ContainedObjects.Contains(item.Data));
                    if (newListForObject != null)
                    {
                        changeList.Add(new ObjectContainerChange
                        { 
                            ObjectMoved = item.Data,
                            NewContainer = newListForObject
                            
                        });
                    }
                }
            }

            await PluginManager.ReactToObjectListContainerChanged(changeList);


            if (performSaveAndGenerateCode)
            {
                if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.SeparateJsonFilesForElements)
                {
                    // Much faster to save just the changed element, especially
                    // when we're doing copy/paste and we want it to go fast
                    await GlueCommands.Self.GluxCommands.SaveElementAsync(targetElement);
                }
                else
                {
                    GlueCommands.Self.GluxCommands.SaveGlux();

                }
            }
            return toReturn;
        }

        public async Task<ToolsUtilities.GeneralResponse<NamedObjectSave>> CopyNamedObjectIntoElement(NamedObjectSave nos, GlueElement targetElement, bool performSaveAndGenerateCode = true, bool updateUi = true)
        {
            return await CopyNamedObjectIntoElementInner(nos, targetElement, performSaveAndGenerateCode, updateUi, notifyPlugins: true);
        }

        public async Task<ToolsUtilities.GeneralResponse<NamedObjectSave>> CopyNamedObjectIntoElementInner(NamedObjectSave nos, GlueElement targetElement, bool performSaveAndGenerateCode, bool updateUi,
            bool notifyPlugins)
        {
            bool succeeded = true;

            //// moving to another element, so let's copy
            NamedObjectSave newNos = nos.Clone();

            UpdateNosAttachmentAfterDragDrop(newNos, targetElement);

            //clonedNos.InstanceName = IncrementNumberAtEndOfNewObject(elementMovingInto, clonedNos.InstanceName);

            // For games with a lot of items in a screen, this can be REALLY slow!!!
            // FlatRedBall.Utilities.StringFunctions.MakeNameUnique(newNos, targetElement.AllNamedObjects);
            // let's go faster:
            var allNamedObjects = targetElement.AllNamedObjects;
            HashSet<string> allNames = new HashSet<string>();
            foreach(var allNamedObjectsInstance in allNamedObjects)
            {
                allNames.Add(allNamedObjectsInstance.InstanceName);
            }
            while (allNames.Contains(newNos.InstanceName))
            {
                newNos.InstanceName = StringFunctions.IncrementNumberAtEnd(newNos.InstanceName);
            }

            var listOfThisType = ObjectFinder.Self.GetDefaultListToContain(newNos, targetElement);

            if (listOfThisType != null)
            {
                listOfThisType.ContainedObjects.Add(newNos);
            }
            else
            {
                targetElement.NamedObjects.Add(newNos);
            }

            var referenceCheck = ProjectManager.CheckForCircularObjectReferences(targetElement);

            var generalResponse = new ToolsUtilities.GeneralResponse<NamedObjectSave>();

            if (referenceCheck == ProjectManager.CheckResult.Failed)
            {
                generalResponse.Message = $"Could not copy {nos.InstanceName} because it would result in a circular reference";
                succeeded = false;
                // VerifyReferenceGraph (currently) shows a popup so we don't have to here
                if (listOfThisType != null)
                {
                    listOfThisType.ContainedObjects.Remove(newNos);
                }
                else
                {
                    targetElement.NamedObjects.Remove(newNos);
                }
            }
            if (succeeded && nos.DefinedByBase)
            {
                succeeded = false;
                generalResponse.Message = $"Could not copy {nos.InstanceName} because it is defined by base. Select the object in the base screen/entity to copy it";
            }

            if (succeeded)
            {
                // If an object which was on a Layer
                // is moved into another Element, then
                // the cloned object probably shouldn't
                // be on a layer.  Not sure if we want to 
                // see if there is a Layer with the same-name
                // but we maybe shouldn't assume that they mean
                // the same thing.
                newNos.LayerOn = null;

                if (updateUi)
                {
                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(targetElement);
                }

                if (performSaveAndGenerateCode)
                {
                    await GlueCommands.Self.GenerateCodeCommands
                        .GenerateElementAndReferencedObjectCode(targetElement);
                }

                if (notifyPlugins)
                {
                    PluginManager.ReactToNewObject(newNos);
                    if (listOfThisType != null)
                    {
                        PluginManager.ReactToObjectContainerChanged(newNos, listOfThisType);
                    }
                }

                if (performSaveAndGenerateCode)
                {
                    if(GlueState.Self.CurrentGlueProject.FileVersion > (int)GluxVersions.SeparateJsonFilesForElements )
                    {
                        await GlueCommands.Self.GluxCommands.SaveElementAsync(targetElement);
                    }
                    else
                    {
                        GlueCommands.Self.GluxCommands.SaveGlux();
                    }
                }
                generalResponse.Data = newNos;
            }

            generalResponse.Succeeded = succeeded;
            return generalResponse;
        }

        private static void UpdateNosAttachmentAfterDragDrop(NamedObjectSave clonedNos, GlueElement elementMovingInto)
        {
            if (elementMovingInto is EntitySave)
            {
                clonedNos.AttachToCamera = false;
                clonedNos.AttachToContainer = true;
            }
            else if (elementMovingInto is ScreenSave)
            {
                clonedNos.AttachToContainer = false;
            }
        }


        public static ObjectsToRemove GetObjectsToRemoveIfRemoving(NamedObjectSave namedObject, GlueElement owner)
        {
            var toReturn = new ObjectsToRemove();

            toReturn.SubObjectsInList.AddRange(namedObject.ContainedObjects);

            // Only check the top level 
            foreach (var item in owner.NamedObjects)
            {
                if (item.IsCollisionRelationship())
                {
                    var matches =
                        item.Properties.GetValue<string>("FirstCollisionName") == namedObject.FieldName ||
                        item.Properties.GetValue<string>("SecondCollisionName") == namedObject.FieldName
                        ;

                    if (matches)
                    {
                        toReturn.CollisionRelationships.Add(item);
                    }
                }
            }

            // The owner of this NOS could be an entity which is referenced in other collision relationships, and 
            // those collision relationships may reference this NOS as a sub type
            FillWithCollisionRelationshipsReferencing(namedObject, owner, toReturn);

            var derivedElements = new List<GlueElement>();
            derivedElements.AddRange(ObjectFinder.Self.GetAllElementsThatInheritFrom(owner));

            List<NamedObjectSave> derivedNamedObjectsToRemove = new List<NamedObjectSave>();

            foreach (var derivedElement in derivedElements)
            {
                // At this point, namedObjectToRemove is already removed from the current Element, so this will only
                // return NamedObjects that exist in the derived.
                NamedObjectSave derivedNamedObject = derivedElement.GetNamedObjectRecursively(namedObject.InstanceName);

                if (derivedNamedObject != null && derivedNamedObject != namedObject && derivedNamedObject.DefinedByBase)
                {
                    toReturn.DerivedNamedObjects.Add(derivedNamedObject);
                }
            }



            var customVariablesToRemove = owner.CustomVariables
                .Where(item => item.SourceObject == namedObject.InstanceName)
                .ToArray();

            toReturn.CustomVariables.AddRange(customVariablesToRemove);

            var eventsToRemove = owner.Events
                .Where(item => item.SourceObject == namedObject.InstanceName)
                .ToArray();

            toReturn.EventResponses.AddRange(eventsToRemove);



            return toReturn;
        }

        private static void FillWithCollisionRelationshipsReferencing(NamedObjectSave namedObject, GlueElement owner, ObjectsToRemove toReturn)
        {
            var nosesReferencingOwner = ObjectFinder.Self.GetAllNamedObjectsThatUseElement(owner);

            foreach (var possibleCollisionRelationship in nosesReferencingOwner)
            {
                if (possibleCollisionRelationship.IsCollisionRelationship())
                {
                    var collisionRelationshipOwner = ObjectFinder.Self.GetElementContaining(possibleCollisionRelationship);
                    var firstReferencedNos = possibleCollisionRelationship.Properties.GetValue<string>("FirstCollisionName");
                    var secondReferencedNos = possibleCollisionRelationship.Properties.GetValue<string>("SecondCollisionName");

                    var firstNos = collisionRelationshipOwner.GetNamedObjectRecursively(firstReferencedNos);
                    var secondNos = collisionRelationshipOwner.GetNamedObjectRecursively(secondReferencedNos);

                    bool DoesReferenceOwner(NamedObjectSave nosToCheck)
                    {
                        return nosToCheck?.SourceClassType == owner.Name || nosToCheck?.SourceClassGenericType == owner.Name;
                    }

                    var shouldAdd = false;
                    if (DoesReferenceOwner(firstNos))
                    {
                        var firstSub = possibleCollisionRelationship.Properties.GetValue<string>("FirstSubCollisionSelectedItem");

                        if (firstSub == namedObject.InstanceName)
                        {
                            shouldAdd = true;
                        }
                    }
                    if (!shouldAdd && DoesReferenceOwner(secondNos))
                    {
                        var secondSub = possibleCollisionRelationship.Properties.GetValue<string>("SecondSubCollisionSelectedItem");

                        if (secondSub == namedObject.InstanceName)
                        {
                            shouldAdd = true;
                        }
                    }

                    if (shouldAdd)
                    {
                        toReturn.CollisionRelationships.Add(possibleCollisionRelationship);
                    }
                }
            }
        }

        private void DoRemovalInternal(NamedObjectSave namedObjectToRemove, bool performSaveAndGenerateCode, bool updateUi,
            List<string> additionalFilesToRemove,
            StringBuilder removalInformation, bool wasSelected,
            int indexInChild, NamedObjectSave containerOfRemoved,
            GlueElement element,
            bool notifyPlugins = true)
        {
            if (element != null)
            {
                var removedItselfFromList = namedObjectToRemove.RemoveSelfFromNamedObjectList(element.NamedObjects);
                // November 12, 2021
                // This used to be an indication of a problem. Now it's okay because removal
                // is done tasked, and that means the object could have changed in the meantime.
                // We should tolerate:
                //if (!removedItselfFromList)
                //{
                //throw new ArgumentException($"Tried to remove {namedObjectToRemove} from {element} but it wasn't removed from anything.");
                //}

                var objectsToRemove = GetObjectsToRemoveIfRemoving(namedObjectToRemove, element);


                #region Remove all CustomVariables that reference the removed NamedObject

                foreach (var variable in objectsToRemove.CustomVariables)
                {
                    removalInformation.AppendLine("Removed variable " + variable.ToString());
                    element.CustomVariables.Remove(variable);
                }
                #endregion

                // Remove any events that use this
                foreach (var ers in objectsToRemove.EventResponses)
                {
                    removalInformation.AppendLine("Removed event " + ers.ToString());
                    element.Events.Remove(ers);
                }

                if (namedObjectToRemove.IsLayer)
                {
                    //  Remove any objects that use this as a layer
                    for (int i = 0; i < element.NamedObjects.Count; i++)
                    {
                        if (element.NamedObjects[i].LayerOn == namedObjectToRemove.InstanceName)
                        {
                            removalInformation.AppendLine("Removed the following object from the deleted Layer: " + element.NamedObjects[i].ToString());
                            element.NamedObjects[i].LayerOn = null;
                        }
                    }
                }

                element.SortStatesToCustomVariables();

                foreach (var derivedNamedObject in objectsToRemove.CollisionRelationships)
                {
                    // Delete it
                    RemoveNamedObject(derivedNamedObject, performSaveAndGenerateCode, updateUi, additionalFilesToRemove);
                }

                foreach (var derivedNamedObject in objectsToRemove.DerivedNamedObjects)
                {
                    // Delete it
                    RemoveNamedObject(derivedNamedObject, performSaveAndGenerateCode, updateUi, additionalFilesToRemove);
                }


                if (updateUi)
                {
                    GlueCommands.Self.DoOnUiThread(() =>
                    {
                        GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element);

                        if (wasSelected)
                        {
                            List<NamedObjectSave> containerList = containerOfRemoved?.ContainedObjects ?? element.NamedObjects;

                            if (containerList.Count == 0)
                            {
                                if (containerOfRemoved != null)
                                {
                                    GlueState.Self.CurrentNamedObjectSave = containerOfRemoved;
                                }
                                else
                                {
                                    // do nothing...
                                }
                            }
                            else
                            {
                                if (indexInChild < containerList.Count)
                                {
                                    GlueState.Self.CurrentNamedObjectSave = containerList[indexInChild];
                                }
                                else
                                {
                                    GlueState.Self.CurrentNamedObjectSave = containerList.LastOrDefault();

                                }

                            }
                        }

                        GlueCommands.Self.DialogCommands.FocusOnTreeView();

                    });
                }

                // If we aren't going to save the project, we probably don't
                // care about generating code either. I don't know if we ever
                // want to separate these variables, but we'll link them for now.
                if (performSaveAndGenerateCode)
                {
                    CodeWriter.GenerateCode(element);
                }

                if (element is EntitySave)
                {
                    List<NamedObjectSave> entityNamedObjects = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(element.Name);

                    foreach (NamedObjectSave nos in entityNamedObjects)
                    {
                        nos.UpdateCustomProperties();
                    }
                }

                EditorObjects.IoC.Container.Get<GlueErrorManager>().ClearFixedErrors();

                if (notifyPlugins)
                {
                    PluginManager.ReactToObjectRemoved(element, namedObjectToRemove);
                }
            }

            if (element == null && GlueState.Self.CurrentElement != null && updateUi)
            {
                GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(GlueState.Self.CurrentElement);
            }



            if (performSaveAndGenerateCode)
            {
                GluxCommands.Self.SaveGlux();
            }
        }



        public async Task SetVariableOnList(List<NosVariableAssignment> nosVariableAssignments,
            bool performSaveAndGenerateCode = true,
            bool updateUi = true)
        {
            HashSet<GlueElement> nosContainers = new HashSet<GlueElement>();


            var changes = new List<VariableChangeArguments>();
            foreach (var assignment in nosVariableAssignments)
            {
                // This could be malformed somehow? This could come from the game which got out of sync with Glue:
                if(assignment.NamedObjectSave == null)
                {
                    continue;
                }
                // get the old value before calling SetVariableOnInner:
                object oldValue = assignment.NamedObjectSave.GetCustomVariable(assignment.VariableName)?.Value;

                await SetVariableOnInner(assignment.NamedObjectSave, assignment.VariableName, assignment.Value, performSaveAndGenerateCode: false, updateUi: false,
                    notifyPlugins: false);
                nosContainers.Add(ObjectFinder.Self.GetElementContaining(assignment.NamedObjectSave));


                changes.Add(new VariableChangeArguments
                {
                    ChangedMember = assignment.VariableName,
                    NamedObject = assignment.NamedObjectSave,
                    OldValue = oldValue
                });

            }

            foreach (var nosContainer in nosContainers)
            {
                if (performSaveAndGenerateCode)
                {
                    await GlueCommands.Self.GenerateCodeCommands.GenerateElementCodeAsync(nosContainer);
                }

                if (updateUi)
                {
                    GlueCommands.Self.DoOnUiThread(() =>
                    {
                        GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(nosContainer);
                    });
                }
            }

            //PluginManager.ReactToNamedObjectChangedValue(changedMember, oldValue, namedObjectSave);
            //PluginManager.ReactToNamedObjectChangedValueList()
            PluginManager.ReactToNamedObjectChangedValueList(changes);

            if (updateUi)
            {
                GlueCommands.Self.DoOnUiThread(() =>
                {
                    MainGlueWindow.Self.PropertyGrid.Refresh();
                    GlueCommands.Self.RefreshCommands.RefreshVariables();
                    PropertyGridHelper.UpdateNamedObjectDisplay();
                });
            }
            if (performSaveAndGenerateCode)
            {
                if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.SeparateJsonFilesForElements)
                {
                    foreach(var element in nosContainers)
                    {
                        await GlueCommands.Self.GluxCommands.SaveElementAsync(element);
                    }
                }
                else
                {
                    GlueCommands.Self.GluxCommands.SaveGlux(TaskExecutionPreference.AddOrMoveToEnd);
                }
            }
        }

        [Obsolete("Use SetVariableOnAsync")]
        public async void SetVariableOn(NamedObjectSave nos, string memberName, object value, bool performSaveAndGenerateCode = true,
            bool updateUi = true)
        {
            await SetVariableOnInner(nos, memberName, value, performSaveAndGenerateCode, updateUi, notifyPlugins: true);
        }


        public async Task SetVariableOnAsync(NamedObjectSave nos, string memberName, object value, bool performSaveAndGenerateCode = true,
            bool updateUi = true)
        {
            await TaskManager.Self.AddAsync(
                () => SetVariableOnInner(nos, memberName, value, performSaveAndGenerateCode, updateUi, notifyPlugins: true),
                nameof(SetVariableOnAsync));
        }

        private async Task SetVariableOnInner(NamedObjectSave nos, string memberName, object value, bool performSaveAndGenerateCode = true,
            bool updateUi = true, bool notifyPlugins = true)
        {
            // XML serialization doesn't like enums
            var needsEnum = GlueState.Self.CurrentGlueProject.FileVersion < (int)GlueProjectSave.GluxVersions.GlueSavedToJson;
            if (value?.GetType().IsEnum() == true && needsEnum)
            {
                value = (int)value;
            }

            object oldValue = null;

            var instruction = nos.GetCustomVariable(memberName);

            oldValue = instruction?.Value;

            bool shouldConvertValue = false;

            var ati = nos.GetAssetTypeInfo();
            var variableDefinition = ati?.VariableDefinitions.FirstOrDefault(item => item.Name == memberName);

            if (variableDefinition != null)
            {

                if (value is string &&

                    variableDefinition.Type != "string" &&
                    variableDefinition.Type != "Microsoft.Xna.Framework.Color" &&
                    variableDefinition.Type != "Color" &&
                    !CustomVariableExtensionMethods.GetIsFile(variableDefinition.Type) && // If it's a file, we just want to set the string value and have the underlying system do the loading                         
                    !CustomVariableExtensionMethods.GetIsObjectType(variableDefinition.Type)
                    )
                {
                    bool isCsv = NamedObjectPropertyGridDisplayer.GetIfIsCsv(nos, memberName);
                    shouldConvertValue = !isCsv &&
                        variableDefinition.Name != "object" &&
                        // variable could be an object
                        (value is PositionedObject) == false;
                    // If the MemberType is object, then it's something we can't convert to - it's likely a state
                }

                if (shouldConvertValue && variableDefinition.Type != null)
                {
                    value = PropertyValuePair.ConvertStringToType((string)value, variableDefinition.Type);
                }
            }

            // March 17, 2022
            // If the NOS is an
            // EntitySave and the
            // variable being assigned
            // is not an exposed variable,
            // then this has a few problems:
            // 1. The value cannot be viewed on
            //    the object instance or edited in
            //    the UI.
            // 2. The type may not be known on the instance
            //    because a variable hasn't been added so the
            //    TypeMemberBases have not been updated. Therefore,
            //    pushing this to the game could cause errors due to
            //    the type not being properly converted by the game.
            // To solve this, we're going to check if it's an entity instance
            // and throw an exception if the variable hasn't yet been exposed:
            var shouldProceed = true;
            var nosEntity = ObjectFinder.Self.GetEntitySave(nos);
            if (nosEntity != null)
            {
                var variable = nosEntity.GetCustomVariableRecursively(memberName);
                if (variable == null)
                {
                    var message =
                        $"Attempting to set variable {memberName} on object {nos}, " +
                        $"but this object uses entity type {nosEntity} which does not have this variable added or exposed";
                    GlueCommands.Self.PrintError(message);
                    shouldProceed = false;
                }
            }
            if (shouldProceed)
            {
                nos.SetVariable(memberName, value);

                if (notifyPlugins)
                {
                    // This does more than notify plugins, but the "more" doesn't apply to custom variables
                    // I think this should be refactored to handle NamedObjectProperties specifically anyway
                    EditorObjects.IoC.Container.Get<NamedObjectSetVariableLogic>().ReactToNamedObjectChangedValue(
                        memberName, oldValue, namedObjectSave: nos);
                }

                var nosContainer = ObjectFinder.Self.GetElementContaining(nos);

                if (performSaveAndGenerateCode)
                {
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(nosContainer);
                }

                if (updateUi)
                {
                    // Avoids accumulation when dragging a slider around:
                    // Even though this is inside a task, still add so we can move to end
                    TaskManager.Self.AddOrRunIfTasked(() => EditorObjects.IoC.Container.Get<GlueErrorManager>().ClearFixedErrors(), "Clear fixed errors", TaskExecutionPreference.AddOrMoveToEnd);
                }

                if (notifyPlugins)
                {
                    var variableChange = new NamedObjectSaveVariableChange
                    {
                        NamedObjectSave = nos,
                        ChangedMember = memberName
                    };
                    PluginManager.ReactToChangedProperty(memberName, oldValue, nosContainer, variableChange);

                    PluginManager.ReactToNamedObjectChangedValueList(new List<VariableChangeArguments>
                    {
                        new VariableChangeArguments
                        {
                            NamedObject = nos,
                            ChangedMember = memberName,
                            OldValue = oldValue
                        }
                    });
                }

                if (updateUi)
                {
                    GlueCommands.Self.DoOnUiThread(() =>
                    {
                        MainGlueWindow.Self.PropertyGrid.Refresh();
                        GlueCommands.Self.RefreshCommands.RefreshVariables();
                        // Do we need this?
                        //PropertyGridHelper.UpdateNamedObjectDisplay();

                        // If the user enters text in a text box (such as the X or Y value on
                        // the Points tab, that causes a refresh for the tree node, which refreshes
                        // everything and causes the text box to lose focus. Why do we need to update here?
                        // Is it only if the Name changes? I can't think of any other properties that may require
                        // tree node refreshes, so let's limit that:
                        if (memberName == nameof(NamedObjectSave.InstanceName) ||
                            memberName == "Name")
                        {
                            GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(nosContainer);

                        }
                    });
                }

                if (performSaveAndGenerateCode)
                {
                    GlueCommands.Self.GluxCommands.SaveGlux(TaskExecutionPreference.AddOrMoveToEnd);
                }
            }

        }

        public async Task SetPropertyOnAsync(NamedObjectSave nos, string propertyName, object value, bool performSaveAndGenerateCode = true,
            bool updateUi = true)
        {
            object oldValue;

            oldValue = nos.Properties.GetValue(propertyName);
            nos.SetProperty(propertyName, value);

            await ReactToPropertyChanged(nos, propertyName, oldValue, performSaveAndGenerateCode, updateUi);
        }

        public async Task ReactToPropertyChanged(NamedObjectSave nos, string propertyName, object oldValue, bool performSaveAndGenerateCode = true, bool updateUi = true)
        {
            await EditorObjects.IoC.Container.Get<NamedObjectSetVariableLogic>().ReactToNamedObjectChangedValue(
                propertyName, oldValue, namedObjectSave: nos);

            if (updateUi)
            {
                GlueCommands.Self.DoOnUiThread(() =>
                {
                    MainGlueWindow.Self.PropertyGrid.Refresh();
                    PropertyGridHelper.UpdateNamedObjectDisplay();

                    // If the user enters text in a text box (such as the X or Y value on
                    // the Points tab, that causes a refresh for the tree node, which refreshes
                    // everything and causes the text box to lose focus. Why do we need to update here?
                    // Is it only if the Name changes? I can't think of any other properties that may require
                    // tree node refreshes, so let's limit that:
                    if (propertyName == nameof(NamedObjectSave.InstanceName) ||
                        propertyName == "Name")
                    {
                        var container = ObjectFinder.Self.GetElementContaining(nos);
                        if (container != null)
                        {
                            GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(container);
                        }
                    }
                });
            }

            if (performSaveAndGenerateCode)
            {
                GlueCommands.Self.GluxCommands.SaveGlux(TaskExecutionPreference.AddOrMoveToEnd);
            }
        }

        #endregion

        #region Custom Variable

        public void RemoveCustomVariable(CustomVariable customVariable, List<string> additionalFilesToRemove = null)
        {
            bool updateUi = true;
            int indexInContainer = -1;
            var wasSelected = customVariable == GlueState.Self.CurrentCustomVariable;
            // additionalFilesToRemove is added to keep this consistent with other remove methods

            var element = ObjectFinder.Self.GetElementContaining(customVariable);

            if (element == null || !element.CustomVariables.Contains(customVariable))
            {
                throw new ArgumentException();
            }
            else
            {
                if (wasSelected)
                {
                    indexInContainer = element.CustomVariables.IndexOf(customVariable);
                }
                element.CustomVariables.Remove(customVariable);
                element.SortStatesToCustomVariables();

                List<EventResponseSave> eventsReferencedByVariable = element.GetEventsOnVariable(customVariable.Name);

                foreach (EventResponseSave ers in eventsReferencedByVariable)
                {
                    element.Events.Remove(ers);
                }
            }

            if (updateUi)
            {
                GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element);

                if (wasSelected)
                {
                    if (element?.CustomVariables.Count != 0)
                    {
                        if (indexInContainer < element.CustomVariables.Count)
                        {
                            GlueState.Self.CurrentCustomVariable = element.CustomVariables[indexInContainer];
                        }
                        else
                        {
                            GlueState.Self.CurrentCustomVariable = element.CustomVariables.LastOrDefault();
                        }
                    }
                    else
                    {
                        GlueState.Self.CurrentCustomVariable = null;
                    }
                }
                GlueCommands.Self.DialogCommands.FocusOnTreeView();
            }



            InheritanceManager.UpdateAllDerivedElementFromBaseValues(true, element);

            EditorObjects.IoC.Container.Get<GlueErrorManager>().ClearFixedErrors();

            PluginManager.ReactToVariableRemoved(customVariable);
        }

        public async Task DuplicateAsync(CustomVariable customVariable)
        {
            await TaskManager.Self.AddAsync(async () =>
            {
                var parentElement = ObjectFinder.Self.GetElementContaining(customVariable);
                if(parentElement != null)
                {
                    var duplicate = FileManager.CloneObject(customVariable);
                    var existingNames = parentElement.CustomVariables.Select(item => item.Name).ToList();
                    duplicate.Name = StringFunctions.MakeStringUnique(duplicate.Name, existingNames);

                    duplicate.FixAllTypes();

                    await GluxCommands.Self.ElementCommands.AddCustomVariableToElementAsync(duplicate, parentElement);
                }
            }, $"Duplicate variable {customVariable}");
            
        }

        #endregion

        #region StateSaveCategory

        public void RemoveStateSaveCategory(StateSaveCategory category)
        {
            var owner = ObjectFinder.Self.GetElementContaining(category);
            GlueState.Self.CurrentElement.StateCategoryList.Remove(category);

            var project = GlueState.Self.CurrentGlueProject;

            var qualifiedCategoryName = owner.Name.Replace("\\", ".") + "." + category.Name;



            var screenVariables = project.Screens
                .SelectMany(item => item.CustomVariables)
                .Where(item => item.Type == qualifiedCategoryName);
            var entityVariables = project.Entities
                .SelectMany(item => item.CustomVariables)
                .Where(item => item.Type == qualifiedCategoryName);

            var combined = screenVariables.Concat(entityVariables).ToArray();

            HashSet<GlueElement> impactedObjects = new HashSet<GlueElement>();

            foreach (var variable in combined)
            {
                GlueCommands.Self.PrintOutput($"Removing {variable} because it references the category {category.Name}");
                GlueCommands.Self.GluxCommands.RemoveCustomVariable(variable);
            }

            if (owner != null)
            {
                GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(owner);
            }

            GluxCommands.Self.SaveGlux();
        }

        #endregion

        #region GlueElements

        public FilePath GetElementJsonLocation(GlueElement element)
        {
            FilePath rootDirectory = GlueState.Self.CurrentGlueProjectDirectory;

            rootDirectory += element.Name;

            if (element is EntitySave)
            {
                return rootDirectory + "." + GlueProjectSave.EntityExtension;
            }
            else // screen save
            {
                return rootDirectory + "." + GlueProjectSave.ScreenExtension;
            }

        }

        public FilePath GetPreviewLocation(GlueElement glueElement, StateSave stateSave)
        {
            var folder = GlueCommands.Self.GluxCommands.GetElementJsonLocation(glueElement).GetDirectoryContainingThis();

            string fileName = glueElement.GetStrippedName();

            if (stateSave != null)
            {
                var category = ObjectFinder.Self.GetStateSaveCategory(stateSave);
                if (category != null)
                {
                    fileName += $".{category.Name}";
                }
                fileName += $".{stateSave.Name}";
            }



            FilePath filePath = folder + fileName + ".generatedpreview.png";
            return filePath;
        }

        // Eventually support copy/paste into different folders
        public async Task CopyGlueElement(GlueElement original)
        {
            await TaskManager.Self.AddAsync(async () =>
            {
                GlueElement newElement = null;

                if (original is EntitySave asEntitySave)
                {
                    newElement = asEntitySave.Clone();
                }
                else if (original is ScreenSave asScreenSave)
                {
                    newElement = asScreenSave.Clone();
                }

                newElement.Name = original.Name + "Copy";

                while (ObjectFinder.Self.GetElement(newElement.Name) != null)
                {
                    newElement.Name = StringFunctions.IncrementNumberAtEnd(newElement.Name);
                }

                if (newElement is ScreenSave newScreenSave)
                {
                    await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen(newScreenSave);
                }
                else if (newElement is EntitySave newEntitySave)
                {
                    GlueCommands.Self.GluxCommands.EntityCommands.AddEntity(newEntitySave);
                }

            }, $"Adding copy of {original}");
        }

        #endregion

        #region Entity

        private static bool MoveEntityCodeFilesToDirectory(EntitySave entitySave, string targetDirectory)
        {
            bool succeeded = true;

            var allFiles = CodeWriter.GetAllCodeFilesFor(entitySave);
            foreach (var file in allFiles)
            {
                bool isFactory = GetIfFileIsFactory(entitySave, file.FullPath);

                if (!succeeded)
                {
                    break;
                }

                if (file.Exists() && !isFactory)
                {
                    string relative = FileManager.MakeRelative(file.FullPath);
                    succeeded = MoveSingleCodeFileToDirectory(relative, targetDirectory);
                }
            }

            return succeeded;
        }

        public bool MoveEntityToDirectory(EntitySave entitySave, string newRelativeDirectory)
        {
            bool succeeded = true;
            var fileNameBeforeMove = GlueCommands.Self.FileCommands.GetJsonFilePath(entitySave);

            string targetDirectory = GlueState.Self.CurrentGlueProjectDirectory + newRelativeDirectory;
            string oldName = entitySave.Name;
            string newName = newRelativeDirectory.Replace("/", "\\") + entitySave.ClassName;
            succeeded = MoveEntityCodeFilesToDirectory(entitySave, targetDirectory);

            if (succeeded)
            {
                entitySave.Name = newName;
            }

            if (succeeded)
            {
                // Do this after changing the name of the Entity so
                // namespaces come over properly
                succeeded = UpdateNamespaceOnCodeFiles(entitySave);
            }

            if (succeeded)
            {
                // 5: Change namespaces
                string newNamespace = ProjectManager.ProjectNamespace + "." + FileManager.MakeRelative(targetDirectory).Replace("/", ".");
                newNamespace = newNamespace.Substring(0, newNamespace.Length - 1);
                string customFileContents = FileManager.FromFileText(FileManager.RelativeDirectory + newName + ".cs");
                customFileContents = CodeWriter.ReplaceNamespace(customFileContents, newNamespace);
                FileManager.SaveText(customFileContents, FileManager.RelativeDirectory + newName + ".cs");

                // Generated will automatically have its namespace changed when it is re-generated

                // 6:  Find all objects referending this NamedObjectSave and re-generate the code

                if (entitySave.CreatedByOtherEntities)
                {
                    // Vic says: I'm tired.  For now just ignore the directory.  Fix this when it becomes a problem.
                    FactoryCodeGenerator.GenerateAndAddFactoryToProjectClass(entitySave);
                }

                if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.SeparateJsonFilesForElements)
                {
                    // delete the old file (put it in recycle bin)
                    // From https://stackoverflow.com/questions/2342628/deleting-file-to-recycle-bin-on-windows-x64-in-c-sharp


                    if(fileNameBeforeMove?.Exists()== true)
                    {
                        try
                        {
                            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                                fileNameBeforeMove.FullPath, 
                                Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                                Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);

                        }
                        catch(Exception e)
                        {
                            GlueCommands.Self.PrintError(e.ToString());
                        }
                    }
                }

                List<NamedObjectSave> namedObjects = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(oldName);


                // Let's get all the TreeNodes to regenerate.
                // We want to store them in a list so we only generate
                // each tree node once.
                var elementsToRegenerate = new HashSet<GlueElement>();

                foreach (NamedObjectSave nos in namedObjects)
                {
                    if (nos.SourceClassGenericType == oldName)
                    {
                        nos.SourceClassGenericType = newName;
                    }

                    if (nos.SourceClassType == oldName)
                    {
                        nos.SourceClassType = newName;
                    }

                    var element = nos.GetContainer();
                    elementsToRegenerate.Add(element);
                }


                foreach (EntitySave esToTestForInheritance in ProjectManager.GlueProjectSave.Entities)
                {
                    if (esToTestForInheritance.BaseEntity == oldName)
                    {
                        esToTestForInheritance.BaseEntity = newName;
                        elementsToRegenerate.Add(esToTestForInheritance);
                    }
                }

                foreach (var element in elementsToRegenerate)
                {
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
                }
            }

            return succeeded;
        }

        public async Task RemoveEntityAsync(EntitySave entityToRemove, List<string> filesThatCouldBeRemoved = null)
        {
            List<NamedObjectSave> namedObjectsToRemove = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(entityToRemove.Name);
            List<EntitySave> inheritingEntities = ObjectFinder.Self.GetAllEntitiesThatInheritFrom(entityToRemove);

            for (int i = entityToRemove.NamedObjects.Count - 1; i > -1; i--)
            {
                NamedObjectSave nos = entityToRemove.NamedObjects[i];



                GlueCommands.Self.GluxCommands
                    .RemoveNamedObject(nos, false, false, null);
            }


            if (entityToRemove.CreatedByOtherEntities == true)
            {
                FactoryCodeGenerator.RemoveFactory(entityToRemove);
            }

            // We used to rely on RemoveUnreferencedFiles to do the removal of all RFS's 
            // However, RemoveUnreferencedFiles looks for the file's container to remove it,
            // and by this point the entityToRemove has already been removed from the project.
            // So we'll manually remove the RFS's first before removing the entire entity
            for (int i = entityToRemove.ReferencedFiles.Count - 1; i > -1; i--)
            {
                await GluxCommands.Self.RemoveReferencedFileAsync(entityToRemove.ReferencedFiles[i], filesThatCouldBeRemoved, regenerateAndSave: false);
            }


            ProjectManager.GlueProjectSave.Entities.Remove(entityToRemove);



            RemoveUnreferencedFiles(entityToRemove, filesThatCouldBeRemoved);

            for (int i = 0; i < namedObjectsToRemove.Count; i++)
            {
                NamedObjectSave nos = namedObjectsToRemove[i];
                GlueCommands.Self.GluxCommands
                    .RemoveNamedObject(nos, false, true, filesThatCouldBeRemoved);
            }
            for (int i = 0; i < inheritingEntities.Count; i++)
            {
                EntitySave inheritingEntity = inheritingEntities[i];

                var result = GlueCommands.Self.DialogCommands.ShowYesNoMessageBox("Reset the inheritance for " + inheritingEntity.Name + "?",
                    caption: "Reset Inheritance?");

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    inheritingEntity.BaseEntity = "";
                    await CodeWriter.GenerateCode(inheritingEntity);
                }
            }

            FillWithCodeFilesForElement(filesThatCouldBeRemoved, entityToRemove);
            FillWithJsonFilesForElement(filesThatCouldBeRemoved, entityToRemove);

            GlueCommands.Self.GenerateCodeCommands.GenerateAllCode();

            EditorObjects.IoC.Container.Get<GlueErrorManager>().ClearFixedErrors();

            PluginManager.ReactToEntityRemoved(entityToRemove, filesThatCouldBeRemoved);

            GlueCommands.Self.ProjectCommands.SaveProjects();

            GluxCommands.Self.SaveGlux();
        }

        private static bool UpdateNamespaceOnCodeFiles(EntitySave entitySave)
        {
            var allFiles = CodeWriter.GetAllCodeFilesFor(entitySave);
            string newNamespace = GlueCommands.Self.GenerateCodeCommands.GetNamespaceForElement(entitySave);

            foreach (var file in allFiles)
            {
                bool doesFileExist = file.Exists();
                bool isFactory = GetIfFileIsFactory(entitySave, file.FullPath);

                if (doesFileExist && !isFactory)
                {
                    string contents = FileManager.FromFileText(file.FullPath);

                    contents = CodeWriter.ReplaceNamespace(contents, newNamespace);

                    FileManager.SaveText(contents, file.FullPath);


                }
            }

            return true;
        }

        #endregion

        #region Screen

        public async void RemoveScreen(ScreenSave screenToRemove, List<string> filesThatCouldBeRemoved = null)
        {
            filesThatCouldBeRemoved = filesThatCouldBeRemoved ?? new List<string>();
            List<ScreenSave> inheritingScreens = ObjectFinder.Self.GetAllScreensThatInheritFrom(screenToRemove);

            if (GlueCommands.Self.GluxCommands.StartUpScreenName == screenToRemove.Name)
            {
                var newScreen = ObjectFinder.Self.GlueProject.Screens.FirstOrDefault(x => x != screenToRemove && !x.IsAbstract && x.BaseScreen != null);
                if (newScreen == null)
                {
                    newScreen = ObjectFinder.Self.GlueProject.Screens.FirstOrDefault(x => x != screenToRemove && !x.IsAbstract);
                }
                if (newScreen == null)
                {
                    newScreen = ObjectFinder.Self.GlueProject.Screens.FirstOrDefault(x => x != screenToRemove);
                }
                GlueCommands.Self.GluxCommands.StartUpScreenName = newScreen == null ? "" : newScreen.Name;
            }

            // Remove objects before removing files.  Otherwise Glue will complain if any objects reference the files.
            #region Remove the NamedObjectSaves

            for (int i = screenToRemove.NamedObjects.Count - 1; i > -1; i--)
            {
                NamedObjectSave nos = screenToRemove.NamedObjects[i];

                GlueCommands.Self.GluxCommands
                    .RemoveNamedObject(nos, false, false, null);
            }

            #endregion

            // remove all the files this references first before removing the Screen itself.
            // For more information see the RemoveEntity function
            for (int i = screenToRemove.ReferencedFiles.Count - 1; i > -1; i--)
            {
                await GluxCommands.Self.RemoveReferencedFileAsync(screenToRemove.ReferencedFiles[i], filesThatCouldBeRemoved, regenerateAndSave: false);
            }

            ProjectManager.GlueProjectSave.Screens.Remove(screenToRemove);
            // If we're going to remove the Screen, we should remove all referenced objects that it references
            // as well as any ReferencedFiles

            RemoveUnreferencedFiles(screenToRemove, filesThatCouldBeRemoved);

            for (int i = 0; i < inheritingScreens.Count; i++)
            {
                ScreenSave inheritingScreen = inheritingScreens[i];

                var result = GlueCommands.Self.DialogCommands.ShowYesNoMessageBox("Reset the inheritance for " + inheritingScreen.Name + "?",
                    caption: "Reset Inheritance?");

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    inheritingScreen.BaseScreen = "";

                    await CodeWriter.GenerateCode(inheritingScreen);
                }
            }

            PluginManager.ReactToScreenRemoved(screenToRemove, filesThatCouldBeRemoved);


            FillWithCodeFilesForElement(filesThatCouldBeRemoved, screenToRemove);
            FillWithJsonFilesForElement(filesThatCouldBeRemoved, screenToRemove);

            GlueCommands.Self.ProjectCommands.SaveProjects();
            GluxCommands.Self.SaveGlux();
        }

        private static void RemoveUnreferencedFiles(IElement element, List<string> filesThatCouldBeRemoved)
        {
            var allReferencedFiles = GlueCommands.Self.FileCommands.GetAllReferencedFileNames();

            for (int i = element.ReferencedFiles.Count - 1; i > -1; i--)
            {
                ReferencedFileSave rfs = element.ReferencedFiles[i];

                bool shouldRemove = true;
                foreach (var file in allReferencedFiles)
                {
                    if (file == GlueCommands.Self.GetAbsoluteFilePath(rfs))
                    {
                        shouldRemove = false;
                        break;
                    }
                }

                if (shouldRemove)
                {
                    GluxCommands.Self.RemoveReferencedFile(rfs, filesThatCouldBeRemoved);
                }
            }
        }

        static void FillWithJsonFilesForElement(List<string> filesThatCouldBeRemoved, GlueElement element)
        {
            string extension = element is ScreenSave
                ? GlueProjectSave.ScreenExtension
                : GlueProjectSave.EntityExtension;
            filesThatCouldBeRemoved.Add(element.Name + "." + extension);
        }

        static void FillWithCodeFilesForElement(List<string> filesThatCouldBeRemoved, GlueElement element)
        {
            string elementName = element.Name;



            filesThatCouldBeRemoved.Add(elementName + ".cs");


            filesThatCouldBeRemoved.Add(elementName + ".Generated.cs");

            string eventFile = elementName + ".Event.cs";
            string absoluteEvent = GlueCommands.Self.GetAbsoluteFileName(eventFile, false);
            if (System.IO.File.Exists(absoluteEvent))
            {
                filesThatCouldBeRemoved.Add(eventFile);
            }

            string generatedEventFile = elementName + ".Generated.Event.cs";
            string absoluteGeneratedEventFile = GlueCommands.Self.GetAbsoluteFileName(generatedEventFile, false);
            if (System.IO.File.Exists(absoluteGeneratedEventFile))
            {
                filesThatCouldBeRemoved.Add(generatedEventFile);
            }

            string factoryName = "Factories/" + FileManager.RemovePath(elementName) + "Factory.Generated.cs";
            string absoluteFactoryNameFile = GlueCommands.Self.GetAbsoluteFileName(factoryName, false);
            if (System.IO.File.Exists(absoluteFactoryNameFile))
            {
                filesThatCouldBeRemoved.Add(absoluteFactoryNameFile);
            }
        }

        #endregion

        #region Import

        public async Task<GlueElement> ImportScreenOrEntityFromFile(FilePath filePath)
        {
            return await ElementImporter.ImportElementFromFile(filePath.FullPath, moveToSelectedFolderTreeNode: false);
        }


        #endregion

        #region Code Files

        private static bool GetIfFileIsFactory(EntitySave entitySave, string file)
        {
            return file.EndsWith("Factories/" + entitySave.ClassName + "Factory.Generated.cs");
        }

        static bool MoveSingleCodeFileToDirectory(string relativeCodeFile, string directory)
        {
            string absoluteCodeFile = FileManager.MakeAbsolute(relativeCodeFile);
            bool succeeded = true;
            FilePath targetFile = directory + FileManager.RemovePath(absoluteCodeFile);

            if (targetFile.Exists())
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox(
                    "Can't move the the file " + absoluteCodeFile + " because the following file exists in the target location: " + targetFile);
                succeeded = false;
            }

            if (succeeded)
            {
                File.Move(absoluteCodeFile, targetFile.FullPath);

                ProjectManager.RemoveItemFromAllProjects(relativeCodeFile, false);

                GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(targetFile, save: false);
            }
            return succeeded;
        }

        #endregion

        #region Plugin Requirements

        public bool GetPluginRequirement(PluginBase plugin)
        {
            var name = plugin.FriendlyName;

            var requiredPlugins = GlueState.Self.CurrentGlueProject.PluginData.RequiredPlugins;

            var isRequired = requiredPlugins.Any(item => item.Name == name);

            return isRequired;
        }

        public bool SetPluginRequirement(PluginBase plugin, bool requiredByProject)
        {
            var name = plugin.FriendlyName;

            return SetPluginRequirement(name, requiredByProject, plugin.Version);
        }

        public bool SetPluginRequirement(string name, bool requiredByProject, Version version)
        {
            var requiredPlugins = GlueState.Self.CurrentGlueProject.PluginData.RequiredPlugins;

            bool didChange = false;

            if (requiredByProject && requiredPlugins.Any(item => item.Name == name) == false)
            {
                var pluginToAdd = new PluginRequirement
                {
                    Name = name,
                    Version = version.ToString()
                };

                requiredPlugins.Add(pluginToAdd);
                didChange = true;
            }
            else if (requiredByProject == false && requiredPlugins.Any(item => item.Name == name))
            {
                var toRemove = requiredPlugins.First(item => item.Name == name);
                requiredPlugins.Remove(toRemove);
                didChange = true;
            }

            return didChange;
        }

        #endregion

        #region Folders

        public async Task RenameFolder(ITreeNode treeNode, string newName)
        {
            bool shouldPerformMove = false;
            string directoryRenaming = null;
            string newDirectoryNameRelative = null;
            string newDirectoryNameAbsolute = null;

            //if (dialogResult == DialogResult.OK)
            // entities use backslash:
            directoryRenaming = treeNode.GetRelativeFilePath().Replace("/", "\\");
            newDirectoryNameRelative = FileManager.GetDirectory(directoryRenaming, RelativeType.Relative) + newName + "\\";

            // This depends on whether it's a content or code folder
            // January 2, 2023
            // For now let's just handle global content - eventually files in entities/screens, but we'll worry about one thing at a time
            var isFileNode = treeNode.IsChildOfGlobalContent();
            if(isFileNode)
            {
                newDirectoryNameAbsolute = GlueCommands.Self.GetAbsoluteFilePath(newDirectoryNameRelative, forceAsContent:true).FullPath;
            }
            else
            {
                newDirectoryNameAbsolute = GlueState.Self.CurrentGlueProjectDirectory + newDirectoryNameRelative;
            }

            string whyIsInvalid = null;
            NameVerifier.IsDirectoryNameValid(newName, out whyIsInvalid);

            if (string.IsNullOrEmpty(whyIsInvalid) && Directory.Exists(newDirectoryNameAbsolute))
            {
                whyIsInvalid = $"The directory {newName} already exists.";
            }

            if (!string.IsNullOrEmpty(whyIsInvalid))
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox(whyIsInvalid);
                shouldPerformMove = false;
            }
            else
            {
                shouldPerformMove = true;
            }

            // If it's not a file node, we'll just move some of the entities over, but leave the old one around...
            if (shouldPerformMove && !Directory.Exists(newDirectoryNameAbsolute) && !isFileNode)
            {
                try
                {
                    Directory.CreateDirectory(newDirectoryNameAbsolute);
                }
                catch (Exception ex)
                {
                    PluginManager.ReceiveError(ex.ToString());
                    shouldPerformMove = false;
                }
            }

            if (shouldPerformMove)
            {
                bool didAllSucceed = true;

                if (isFileNode)
                {
                    var relativePath = treeNode.GetRelativeFilePath();
                    var oldDirectoryAbsolute = GlueCommands.Self.GetAbsoluteFileName(treeNode.GetRelativeFilePath(), isContent: true);
                    // just do a rename
                    System.IO.Directory.Move(oldDirectoryAbsolute, newDirectoryNameAbsolute);

                    // update all RFS's:
                    var allRfses = ObjectFinder.Self.GetAllReferencedFiles();

                    foreach(var rfs in allRfses)
                    {
                        // is this in the old location?
                        if(rfs.Name.StartsWith(relativePath))
                        {
                            var suffix = rfs.Name.Substring(relativePath.Length);
                            var oldName = rfs.Name;
                            rfs.Name = (newDirectoryNameRelative + suffix).Replace("\\", "/");
                            // dont' move, it's already been moved based on the folder name change
                            const bool shouldMove = false;
                            // This is in global content currently
                            await ReferencedFileSaveSetPropertyManager.ForceReactToRenamedReferencedFileAsync(
                                oldName, rfs.Name, rfs, container:null, shouldMove:shouldMove);
                        }
                    }
                }
                else
                { 
                    var allContainedEntities = GlueState.Self.CurrentGlueProject.Entities
                        .Where(entity => entity.Name.StartsWith(directoryRenaming)).ToList();

                    newDirectoryNameRelative = newDirectoryNameRelative.Replace('/', '\\');


                    foreach (var entity in allContainedEntities)
                    {
                        bool succeeded = GlueCommands.Self.GluxCommands.MoveEntityToDirectory(entity, newDirectoryNameRelative);

                        if (!succeeded)
                        {
                            didAllSucceed = false;
                            break;
                        }
                    }
                }

                // todo - the old folder is not deleted. It should be right?
                // Dec 31, 2022 - Vic says
                // I found this bug when working
                // on F2 rename of folders. I don't
                // think I caused during this change 
                // so I'm not going to worry about it
                // for now.


                if (didAllSucceed)
                {
                    treeNode.Text = newName;

                    GlueCommands.Self.ProjectCommands.MakeGeneratedCodeItemsNested();
                    GlueCommands.Self.GenerateCodeCommands.GenerateAllCode();

                    GluxCommands.Self.SaveGlux();
                    GlueCommands.Self.ProjectCommands.SaveProjects();

                }
            }
        }

        #endregion

        public void SaveSettings()
        {
            ProjectManager.GlueSettingsSave.Save();
        }

    }
}
