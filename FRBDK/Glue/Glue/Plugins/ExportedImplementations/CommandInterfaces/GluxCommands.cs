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

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    class GluxCommands : IGluxCommands
    {
        #region Fields

        const string NoType = "<No Type>";

        IProjectCommands projectCommands = new ProjectCommands();

        ElementCommands mElementCommands = new ElementCommands();

        static GluxCommands mSelf;

        #endregion

        #region Properties

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

        public IScreenCommands ScreenCommands
        {
            get { return mElementCommands; }
        }

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
                    GlueState.Self.CurrentGlueProject.StartUpScreen = value;
                    GluxCommands.Self.SaveGlux();
                    if (string.IsNullOrEmpty(ProjectManager.GameClassFileName))
                    {
                        System.Windows.Forms.MessageBox.Show(
                            "Could not set the startup screen because Glue could not find the Game class.");
                    }
                    else
                    {
                        GlueCommands.Self.GenerateCodeCommands.GenerateStartupScreenCode();
                    }

                    var screen = GlueState.Self.CurrentGlueProject.Screens
                        .FirstOrDefault(item => item.Name == value);

                    ElementViewWindow.StartUpScreenTreeNode =
                        ElementViewWindow.GetTreeNodeFor(screen);

                    PluginManager.ReactToChangedStartupScreen();
                }
            }
        }
        #endregion

        #region Glux Methods

        /// <summary>
        /// Saves the glux if already in a task. Adds a glulx save task if not.
        /// </summary>
        public void SaveGlux(bool sendPluginRefreshCommand = true)
        {
            TaskManager.Self.AddOrRunIfTasked(
                () => SaveGluxImmediately(sendPluginRefreshCommand: sendPluginRefreshCommand),
                "Saving .glux", 
                // asap because otherwise this may get added
                // after a reload command
                TaskExecutionPreference.Asap);
        }

        /// <summary>
        /// Saves the current project immediately - this should not be called except in very rare circumstances as it will run right away and may result
        /// in multiple threads accessing the glux at the same time.
        /// </summary>
        /// <param name="sendPluginRefreshCommand"></param>
        public void SaveGluxImmediately(bool sendPluginRefreshCommand = true)
        {
            if (ProjectManager.GlueProjectSave != null)
            {

                if (MainGlueWindow.Self.HasErrorOccurred)
                {
                    string projectName = FileManager.RemovePath(FileManager.RemoveExtension(ProjectManager.GlueProjectFileName));

                    GlueCommands.Self.DialogCommands.ShowMessageBox("STOP RIGHT THERE!!!!  There was an error in Glue at some point.  To prevent " +
                        "corruption in the GLUX file, Glue will no longer save any changes that you make to the project.  " +
                        "You should exit out of Glue immediately and attempt to solve the problem.\n\n" +
                        "Glue has saved your work in a temporary file called " + projectName + ".gluxERROR"
                        );


                    ProjectManager.GlueProjectSave.Save("GLUE", ProjectManager.GlueProjectFileName + "ERROR");
                }
                else
                {
                    ProjectSyncer.SyncGlux();

                    // Jan 20, 2020
                    // .NET Core XmlSerialization
                    // requires no enums - values must
                    // be ints, so let's convert them:
                    ProjectManager.GlueProjectSave.ConvertEnumerationValuesToInts();


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
                        string errorLogLocation = FileManager.UserApplicationDataForThisApplication + "ExceptionInGlue.txt";

                        GlueCommands.Self.DialogCommands.ShowMessageBox(
                            "Error trying to save your .glux file.  Because of this error, Glue did not make any changes to the .glux file on disk.\n\nAn error log has been saved here:\n" + errorLogLocation);
                        try
                        {
                            FileManager.SaveText(e.ToString(), errorLogLocation);
                        }
                        catch
                        {
                            // If this fails that's okay, we're already in a failed state.
                        }
                        PluginManager.ReceiveError("Error saving glux:\n\n" + e.ToString());

                        MainGlueWindow.Self.HasErrorOccurred = true;
                    }

                    if (!MainGlueWindow.Self.HasErrorOccurred)
                    {
                        FileWatchManager.IgnoreNextChangeOnFile(GlueState.Self.GlueProjectFileName);
                        
                        Exception lastException;
                        var succeeded = ProjectManager.GlueProjectSave.Save("GLUE", GlueState.Self.GlueProjectFileName, out lastException);

                        if (!succeeded)
                        {
                            GlueCommands.Self.DialogCommands.ShowMessageBox("Error saving the .glux:\n" + lastException);
                        }
                        else
                        {
                            if (sendPluginRefreshCommand)
                            {
                                PluginManager.ReactToGluxSave();
                            }
                        }
                    }
                }
            }
        }

        #endregion

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

        #region ReferencedFileSave

        public ReferencedFileSave CreateNewFileAndReferencedFileSave(AddNewFileViewModel viewModel, object creationOptions = null)
        {
            ReferencedFileSave rfs;
            string name = viewModel.FileName;
            AssetTypeInfo resultAssetTypeInfo =
                viewModel.SelectedAssetTypeInfo;

            string errorMessage;
            string directory = null;
            var element = GlueState.Self.CurrentElement;

            if (EditorLogic.CurrentTreeNode.IsDirectoryNode())
            {
                directory = EditorLogic.CurrentTreeNode.GetRelativePath().Replace("/", "\\");
            }


            rfs = GlueProjectSaveExtensionMethods.AddReferencedFileSave(
                element, directory, name, resultAssetTypeInfo,
                creationOptions, out errorMessage);

            if (!string.IsNullOrEmpty(errorMessage))
            {
                MessageBox.Show(errorMessage);
            }
            else if (rfs != null)
            {

                var createdFile = ProjectManager.MakeAbsolute(rfs.GetRelativePath());

                if (createdFile.EndsWith(".csv"))
                {
                    CsvCodeGenerator.GenerateAndSaveDataClass(rfs, AvailableDelimiters.Comma);
                }


                ElementViewWindow.UpdateChangedElements();

                ElementViewWindow.SelectedNode = GlueState.Self.Find.ReferencedFileSaveTreeNode(rfs);

                PluginManager.ReactToNewFile(rfs);

                GluxCommands.Self.SaveGlux();
            }

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

        public void AddReferencedFileToGlobalContent(ReferencedFileSave referencedFileSave)
        {

            ProjectManager.GlueProjectSave.GlobalFiles.Add(referencedFileSave);
            ProjectManager.GlueProjectSave.GlobalContentHasChanged = true;

            GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(referencedFileSave);


            // Update any element that may reference this file because now it may mean the element
            // will simply reference it from GlobalContent instead of using the content manager.
            List<IElement> elements = ObjectFinder.Self.GetAllElementsReferencingFile(referencedFileSave.Name);

            foreach (IElement element in elements)
            {
                element.HasChanged = true;
            }

            GlueCommands.Self.DoOnUiThread(GlueCommands.Self.RefreshCommands.RefreshGlobalContent);

        }

        // Vic asks: What's the difference between AddSingleFileTo and CreateReferencedFileSaveForExistingFile? The name 
        // CreateReferencedFileSaveForExistingFile suggests it's newer/more complete, but why not obsolete this?
        public ReferencedFileSave AddSingleFileTo(string fileName, string rfsName, 
            string extraCommandLineArguments,
            BuildToolAssociation buildToolAssociation, bool isBuiltFile, 
            object options, IElement sourceElement, string directoryOfTreeNode,
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
                else if(toReturn != null && string.IsNullOrEmpty(toReturn.Name))
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
                    TaskManager.Self.AddAsyncTask(() =>
                        {
                            UpdateReactor.UpdateFile(ProjectManager.MakeAbsolute(toReturn.Name));
                        },
                        "Updating file " + toReturn.Name);
                    string directoryOfFile = FileManager.GetDirectory(ProjectManager.MakeAbsolute(fileName));

                    RightClickHelper.SetExternallyBuiltFileIfHigherThanCurrent(directoryOfFile, false);
                }
            }

            if(!failed && toReturn != null)
            {
                TaskManager.Self.OnUiThread(() =>
                {
                    ElementViewWindow.UpdateChangedElements();
                });

                if (sourceElement == null)
                {
                    GlueCommands.Self.RefreshCommands.RefreshGlobalContent();
                }
                TaskManager.Self.Add(() => GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(toReturn), $"Updating file membership for file {toReturn}");
                PluginManager.ReactToNewFile(toReturn);
                GluxCommands.Self.SaveGlux();
                TaskManager.Self.Add(GluxCommands.Self.ProjectCommands.SaveProjects, "Saving projects after adding file");

            }

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                PluginManager.ReceiveError(errorMessage);
                // I think we should show an error message.  I had a user
                // try to add a file and no popup appeared telling them that
                // the entity was named that.
                //MessageBox.Show(errorMessage);
                GlueCommands.Self.DialogCommands.ShowMessageBox(errorMessage);
            }

            if (toReturn != null)
            {
                ApplyOptions(toReturn, options);

                if(selectFileAfterCreation)
                {
                    TaskManager.Self.Add( () =>
                        TaskManager.Self.OnUiThread( () => 
                            GlueState.Self.CurrentReferencedFileSave = toReturn), "Select new file");
                }
            }

            return toReturn;
        }


        private ReferencedFileSave CreateReferencedFileSaveForExistingFile(IElement containerForFile, string directoryInsideContainer, string absoluteFileName,
            PromptHandleEnum unknownTypeHandle, AssetTypeInfo ati, out string creationReport, out string errorMessage)
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

                    if(!string.IsNullOrEmpty(fileToAdd))
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
                            string fileToAddAbsolute = ProjectManager.MakeAbsolute(fileToAdd);
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

            return referencedFileSaveToReturn;
        }

        private static void ApplyOptions(ReferencedFileSave toReturn, object options)
        {
            if (toReturn.IsCsvOrTreatedAsCsv)
            {
                toReturn.CreatesDictionary = ((string)options) == "Dictionary";
            }
        }

        public void RemoveReferencedFile(ReferencedFileSave referencedFileToRemove, List<string> additionalFilesToRemove)
        {
            RemoveReferencedFile(referencedFileToRemove, additionalFilesToRemove, true);
        }

        public void RemoveReferencedFile(ReferencedFileSave referencedFileToRemove, List<string> additionalFilesToRemove, bool regenerateCode)
        {

            var isContained = GlueState.Self.Find.IfReferencedFileSaveIsReferenced(referencedFileToRemove);
            /////////////////////////Early Out//////////////////////////////
            if (!isContained)
            {
                return;
            }
            ////////////////////////End Early Out/////////////////////////////



            // There are some things that need to happen:
            // 1.  Remove the ReferencedFileSave from the Glue project (GLUX)
            // 2.  Remove the GUI item
            // 3.  Remove the item from the Visual Studio project.
            IElement container = referencedFileToRemove.GetContainer();

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
                        MainGlueWindow.Self.Invoke(() =>
                        {
                            // Ask the user what to do here - remove it?  Keep it and not compile?
                            MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
                            mbmb.MessageText = "The object\n" + nos.ToString() + "\nreferences the file\n" + referencedFileToRemove.Name +
                                "\nWhat would you like to do?";
                            mbmb.AddButton("Remove this object", DialogResult.Yes);
                            mbmb.AddButton("Keep it (object will not be valid until changed)", DialogResult.No);

                            var result = mbmb.ShowDialog();

                            if (result == DialogResult.Yes)
                            {
                                container.NamedObjects.RemoveAt(i);
                            }
                        });
                    }
                    nos.ResetVariablesReferencing(referencedFileToRemove);
                }

                MainGlueWindow.Self.Invoke(() =>
                {
                    if (EditorLogic.CurrentScreenTreeNode != null)
                    {
                        EditorLogic.CurrentScreenTreeNode.RefreshTreeNodes();
                    }
                    else if (EditorLogic.CurrentEntityTreeNode != null)
                    {
                        EditorLogic.CurrentEntityTreeNode.RefreshTreeNodes();
                    }
                    if (regenerateCode)
                    {
                        ElementViewWindow.GenerateSelectedElementCode();
                    }
                });

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

                Action refreshUiAction = () =>
                {
                    TreeNode treeNode = GlueState.Self.Find.ReferencedFileSaveTreeNode(referencedFileToRemove);
                    if (treeNode != null)
                    {
                        // treeNode can be null if the user presses delete + enter really really fast, stacking 2 remove
                        // actions
                        if (treeNode.Tag != referencedFileToRemove)
                        {
                            throw new Exception("Error removing the tree node - the selected tree node doesn't reference the file being removed");
                        }

                        treeNode.Parent.Nodes.Remove(treeNode);
                    }
                };

                MainGlueWindow.Self.Invoke((MethodInvoker)delegate
                {
                    refreshUiAction();
                }
                    );


                GlobalContentCodeGenerator.UpdateLoadGlobalContentCode();

                List<IElement> elements = ObjectFinder.Self.GetAllElementsReferencingFile(referencedFileToRemove.Name);

                foreach (IElement element in elements)
                {
                    if (regenerateCode)
                    {
                        CodeWriter.GenerateCode(element);
                    }
                }
            }

            #endregion


            // November 10, 2015
            // I feel like this may
            // have been old code before
            // we had full dependency tracking
            // in Glue. This file should only be
            // removed from the project if nothing
            // else references it, including no entities.
            // This code does just entities/screens/global
            // content, but doesn't check the full dependency
            // tree. I think we can just remove it and depend on
            // the code below.
            // Actually, removing this seems to cause problems - files
            // that should be removed aren't. So instead we'll chnage the
            // call to use the dependency tree:
            // replace:

            List<string> referencedFiles =
                GlueCommands.Self.FileCommands.GetAllReferencedFileNames().Select(item => item.ToLowerInvariant()).ToList();

            string absoluteToLower = GlueCommands.Self.GetAbsoluteFileName(referencedFileToRemove).ToLowerInvariant();
            string relativeToProject = FileManager.MakeRelative(absoluteToLower, GlueState.Self.ContentDirectory);

            bool isReferencedByOtherContent = referencedFiles.Contains(relativeToProject);

            if (isReferencedByOtherContent == false)
            {
                additionalFilesToRemove.Add(referencedFileToRemove.GetRelativePath());

                string itemName = referencedFileToRemove.GetRelativePath();
                string absoluteName = ProjectManager.MakeAbsolute(referencedFileToRemove.Name, true);

                // I don't know why we were removing the file from the ProjectBase - it should
                // be from the Content project
                //ProjectManager.RemoveItemFromProject(ProjectManager.ProjectBase, itemName);
                ProjectManager.RemoveItemFromProject(ProjectManager.ProjectBase.ContentProject, itemName, performSave: false);

                foreach (ProjectBase syncedProject in ProjectManager.SyncedProjects)
                {
                    ProjectManager.RemoveItemFromProject(syncedProject.ContentProject, absoluteName);
                }
            }



            if (ProjectManager.IsContent(referencedFileToRemove.Name))
            {

                UnreferencedFilesManager.Self.RefreshUnreferencedFiles(false);
                foreach (var file in UnreferencedFilesManager.LastAddedUnreferencedFiles)
                {
                    additionalFilesToRemove.Add(file.FilePath);
                }
            }

            ReactToRemovalIfCsv(referencedFileToRemove, additionalFilesToRemove);

            GluxCommands.Self.SaveGlux();
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
            return FlatRedBall.Glue.Elements.ObjectFinder.Self.GetReferencedFileSaveFromFile(fileName);

        }

        public void AddReferencedFileToElement(ReferencedFileSave rfs, IElement element)
        {
            throw new NotImplementedException();
        }


        #endregion

        #region NamedObjectSave

        public NamedObjectSave AddNewNamedObjectToSelectedElement(AddObjectViewModel addObjectViewModel)
        {
            return AddNewNamedObjectTo(addObjectViewModel, 
                GlueState.Self.CurrentElement, 
                GlueState.Self.CurrentNamedObjectSave);
        }

        public NamedObjectSave AddNewNamedObjectTo(AddObjectViewModel addObjectViewModel, IElement element, NamedObjectSave listToAddTo = null)
        {
            if(element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
            addObjectViewModel.ForcedElementToAddTo = element;
            MembershipInfo membershipInfo = NamedObjectSaveExtensionMethodsGlue.GetMemberMembershipInfo(addObjectViewModel.ObjectName);

            var newNos = AddNewNamedObjectToInternal(addObjectViewModel.ObjectName,
                membershipInfo, element, listToAddTo, false);

            if (addObjectViewModel.SourceClassType != NoType && !string.IsNullOrEmpty(addObjectViewModel.SourceClassType))
            {
                newNos.SourceType = addObjectViewModel.SourceType;
                newNos.SourceClassType =
                    addObjectViewModel.SelectedAti?.QualifiedRuntimeTypeName.QualifiedType ??
                    addObjectViewModel.SourceClassType;
                newNos.SourceFile = addObjectViewModel.SelectedItem?.MainText;
                newNos.SourceName = addObjectViewModel.SourceNameInFile;
                newNos.UpdateCustomProperties();

                GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element);
            }
            else if (addObjectViewModel.SourceFile != null)
            {
                newNos.SourceType = addObjectViewModel.SourceType;
                newNos.SourceFile = addObjectViewModel.SelectedItem?.MainText;
                newNos.SourceName = addObjectViewModel.SourceNameInFile;
                newNos.UpdateCustomProperties();

                GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element);
            }

            newNos.SourceClassGenericType = addObjectViewModel.SourceClassGenericType;

            var ati = newNos.GetAssetTypeInfo();

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

            PluginManager.ReactToNewObject(newNos);
            MainGlueWindow.Self.PropertyGrid.Refresh();
            PropertyGridHelper.UpdateNamedObjectDisplay();
            GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);


            GluxCommands.Self.SaveGlux();

            return newNos;
        }

        private static NamedObjectSave AddNewNamedObjectToInternal(string objectName, MembershipInfo membershipInfo,
            IElement element, NamedObjectSave listToAddTo, bool raisePluginResponse = true)
        {
            NamedObjectSave namedObject = new NamedObjectSave();

            if (GlueState.Self.CurrentGlueProject.FileVersion >=
                (int)GlueProjectSave.GluxVersions.ListsHaveAssociateWithFactoryBool)
            {
                namedObject.AssociateWithFactory = true;
            }

            namedObject.InstanceName = objectName;

            namedObject.DefinedByBase = membershipInfo == MembershipInfo.ContainedInBase;

            #region Adding to a NamedObject (PositionedObjectList)

            if (listToAddTo != null)
            {
                NamedObjectSaveExtensionMethodsGlue.AddNamedObjectToList(namedObject, listToAddTo);

            }
            #endregion

            else if (element != null)
            {
                //AddExistingNamedObjectToElement(element, namedObject, true);
                element.NamedObjects.Add(namedObject);
                GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element);
                // eventually this method will die, but for now the caller is responsible
                //PluginManager.ReactToNewObject(namedObject);
                GlueCommands.Self.GenerateCodeCommands.GenerateElementCodeTask(element);
            }


            if (raisePluginResponse)
            {
                PluginManager.ReactToNewObject(namedObject);
            }
            MainGlueWindow.Self.PropertyGrid.Refresh();
            ElementViewWindow.GenerateSelectedElementCode();
            GluxCommands.Self.SaveGlux();

            return namedObject;
        }

        public void RemoveNamedObject(NamedObjectSave namedObjectToRemove, bool performSave = true, 
            bool updateUi = true, List<string> additionalFilesToRemove = null)
        {
            // caller doesn't care, so we're going to new it here and just fill it, but throw it away
            if(additionalFilesToRemove == null)
            {
                additionalFilesToRemove = new List<string>();
            }
            StringBuilder removalInformation = new StringBuilder();

            // The additionalFilesToRemove is included for consistency with other methods.  It may be used later

            // There are the following things that need to happen:
            // 1.  Remove the NamedObject from the Glue project (GLUX)
            // 2.  Remove any variables that use this NamedObject as their source
            // 3.  Remove the named object from the GUI
            // 4.  Update the variables for any NamedObjects that use this element containing this NamedObject
            // 5.  Find any Elements that contain NamedObjects that are DefinedByBase - if so, see if we should remove those or make them not DefinedByBase
            // 6.  Remove any events that tunnel into this.

            IElement element = namedObjectToRemove.GetContainer();

            if (element != null)
            {

                if (!namedObjectToRemove.RemoveSelfFromNamedObjectList(element.NamedObjects))
                {
                    throw new ArgumentException();
                }

                #region Remove all CustomVariables that reference the removed NamedObject
                for (int i = element.CustomVariables.Count - 1; i > -1; i--)
                {
                    CustomVariable variable = element.CustomVariables[i];

                    if (variable.SourceObject == namedObjectToRemove.InstanceName)
                    {
                        removalInformation.AppendLine("Removed variable " + variable.ToString());

                        element.CustomVariables.RemoveAt(i);
                    }
                }
                #endregion

                // Remove any events that use this
                for (int i = element.Events.Count - 1; i > -1; i--)
                {
                    EventResponseSave ers = element.Events[i];
                    if (ers.SourceObject == namedObjectToRemove.InstanceName)
                    {
                        removalInformation.AppendLine("Removed event " + ers.ToString());
                        element.Events.RemoveAt(i);
                    }
                }

               //  Remove any objects that use this as a layer
                for (int i = 0; i < element.NamedObjects.Count; i++)
                {
                    if (element.NamedObjects[i].LayerOn == namedObjectToRemove.InstanceName)
                    {
                        removalInformation.AppendLine("Removed the following object from the deleted Layer: " + element.NamedObjects[i].ToString());
                        element.NamedObjects[i].LayerOn = null;
                    }
                }

                element.RefreshStatesToCustomVariables();

                #region Ask the user what to do with all NamedObjects that are DefinedByBase

                List<IElement> derivedElements = new List<IElement>();
                if (element is EntitySave)
                {
                    derivedElements.AddRange(ObjectFinder.Self.GetAllEntitiesThatInheritFrom(element as EntitySave));
                }
                else
                {
                    derivedElements.AddRange(ObjectFinder.Self.GetAllScreensThatInheritFrom(element as ScreenSave));
                }

                foreach (IElement derivedElement in derivedElements)
                {
                    // At this point, namedObjectToRemove is already removed from the current Element, so this will only
                    // return NamedObjects that exist in the derived.
                    NamedObjectSave derivedNamedObject = derivedElement.GetNamedObjectRecursively(namedObjectToRemove.InstanceName);

                    if (derivedNamedObject != null && derivedNamedObject != namedObjectToRemove && derivedNamedObject.DefinedByBase)
                    {
                        MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
                        mbmb.MessageText = "What would you like to do with the object " + derivedNamedObject.ToString();
                        mbmb.AddButton("Keep it", DialogResult.OK);
                        mbmb.AddButton("Delete it", DialogResult.Cancel);

                        DialogResult result = mbmb.ShowDialog(MainGlueWindow.Self);

                        if (result == DialogResult.OK)
                        {
                            // Keep it
                            derivedNamedObject.DefinedByBase = false;
                            BaseElementTreeNode treeNode = GlueState.Self.Find.ElementTreeNode(derivedElement);

                            if (updateUi)
                            {
                                treeNode.RefreshTreeNodes();
                            }
                            CodeWriter.GenerateCode(derivedElement);
                        }
                        else
                        {
                            // Delete it
                            RemoveNamedObject(derivedNamedObject, performSave, updateUi, additionalFilesToRemove);
                        }


                    }

                }
                #endregion


                if (updateUi)
                {
                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element);
                }
                CodeWriter.GenerateCode(element);
                if (element is EntitySave)
                {
                    List<NamedObjectSave> entityNamedObjects = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(element.Name);

                    foreach (NamedObjectSave nos in entityNamedObjects)
                    {
                        nos.UpdateCustomProperties();
                    }
                }

                PluginManager.ReactToObjectRemoved(element, namedObjectToRemove);
            }

            if(element == null && GlueState.Self.CurrentElement != null)
            {
                // we're trying to delete something that isn't actually part of the object, so we should refresh the tree view
                //GlueCommands.Self.RefreshCommands.RefreshUi(GlueState.Self.CurrentElement);

                var elementTreeNode = GlueState.Self.Find.ElementTreeNode(GlueState.Self.CurrentElement);

                elementTreeNode?.RefreshTreeNodes();
            }

            if (performSave)
            {
                GluxCommands.Self.SaveGlux();
            }
        }

        #endregion

        #region Entity

        private static bool UpdateNamespaceOnCodeFiles(EntitySave entitySave)
        {
            var allFiles = CodeWriter.GetAllCodeFilesFor(entitySave);
            string newNamespace = GlueCommands.Self.GenerateCodeCommands.GetNamespaceForElement(entitySave);

            foreach (string file in allFiles)
            {
                bool doesFileExist = System.IO.File.Exists(file);
                bool isFactory = GetIfFileIsFactory(entitySave, file);

                if (doesFileExist && !isFactory)
                {
                    string contents = FileManager.FromFileText(file);

                    contents = CodeWriter.ReplaceNamespace(contents, newNamespace);

                    FileManager.SaveText(contents, file);


                }
            }

            return true;
        }

        private static bool MoveEntityCodeFilesToDirectory(EntitySave entitySave, string targetDirectory)
        {
            bool succeeded = true;

            var allFiles = CodeWriter.GetAllCodeFilesFor(entitySave);
            foreach (string file in allFiles)
            {
                bool isFactory = GetIfFileIsFactory(entitySave, file);

                if (!succeeded)
                {
                    break;
                }

                if (File.Exists(file) && !isFactory)
                {
                    string relative = FileManager.MakeRelative(file);
                    succeeded = MoveSingleCodeFileToDirectory(relative, targetDirectory);
                }
            }

            return succeeded;
        }

        public bool MoveEntityToDirectory(EntitySave entitySave, string newRelativeDirectory)
        {
            bool succeeded = true;

            string targetDirectory = FileManager.RelativeDirectory + newRelativeDirectory;
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
                    FactoryCodeGenerator.UpdateFactoryClass(entitySave);
                }

                List<NamedObjectSave> namedObjects = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(oldName);


                // Let's get all the TreeNodes to regenerate.
                // We want to store them in a list so we only generate
                // each tree node once.
                List<BaseElementTreeNode> treeNodesForElementsToRegenerate = new List<BaseElementTreeNode>();

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

                    IElement element = nos.GetContainer();

                    BaseElementTreeNode treeNode = GlueState.Self.Find.ElementTreeNode(element);
                    if (!treeNodesForElementsToRegenerate.Contains(treeNode))
                    {
                        treeNodesForElementsToRegenerate.Add(treeNode);
                    }
                }


                foreach (EntitySave esToTestForInheritance in ProjectManager.GlueProjectSave.Entities)
                {
                    if (esToTestForInheritance.BaseEntity == oldName)
                    {
                        esToTestForInheritance.BaseEntity = newName;

                        BaseElementTreeNode treeNode = GlueState.Self.Find.EntityTreeNode(esToTestForInheritance);
                        if (!treeNodesForElementsToRegenerate.Contains(treeNode))
                        {
                            treeNodesForElementsToRegenerate.Add(treeNode);
                        }
                    }
                }

                foreach (BaseElementTreeNode treeNode in treeNodesForElementsToRegenerate)
                {
                    CodeWriter.GenerateCode(treeNode.SaveObject);
                }
            }

            return succeeded;
        }

        public void RemoveEntity(EntitySave entityToRemove, List<string> filesThatCouldBeRemoved = null)
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
                GluxCommands.Self.RemoveReferencedFile(entityToRemove.ReferencedFiles[i], filesThatCouldBeRemoved);
            }


            ProjectManager.GlueProjectSave.Entities.Remove(entityToRemove);



            RemoveUnreferencedFiles(entityToRemove, filesThatCouldBeRemoved);

            for (int i = 0; i < namedObjectsToRemove.Count; i++)
            {
                MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
                NamedObjectSave nos = namedObjectsToRemove[i];
                mbmb.MessageText = "What would you like to do with the object\n\n" + nos.ToString();

                mbmb.AddButton("Remove this Object", DialogResult.OK);
                mbmb.AddButton("Keep this Object (the reference will be invalid)", DialogResult.Cancel);

                DialogResult namedObjectRemovalResult = mbmb.ShowDialog();

                if (namedObjectRemovalResult == DialogResult.OK)
                {
                    GlueCommands.Self.GluxCommands
                        .RemoveNamedObject(nos, false, true, filesThatCouldBeRemoved);
                }
            }
            for (int i = 0; i < inheritingEntities.Count; i++)
            {
                EntitySave inheritingEntity = inheritingEntities[i];

                DialogResult resetInheritance = MessageBox.Show("Reset the inheritance for " + inheritingEntity.Name + "?",
                    "Reset Inheritance?", MessageBoxButtons.YesNo);

                if (resetInheritance == DialogResult.Yes)
                {
                    inheritingEntity.BaseEntity = "";
                    CodeWriter.GenerateCode(inheritingEntity);
                }
            }

            ElementViewWindow.RemoveEntity(entityToRemove);

            ProjectManager.RemoveCodeFilesForElement(filesThatCouldBeRemoved, entityToRemove);

            PluginManager.ReactToEntityRemoved(entityToRemove, filesThatCouldBeRemoved);

            GlueCommands.Self.ProjectCommands.SaveProjects();

            GluxCommands.Self.SaveGlux();
        }

        #endregion

        #region Screen

        public void RemoveScreen(ScreenSave screenToRemove, List<string> filesThatCouldBeRemoved = null)
        {
            filesThatCouldBeRemoved = filesThatCouldBeRemoved ?? new List<string>();
            List<ScreenSave> inheritingScreens = ObjectFinder.Self.GetAllScreensThatInheritFrom(screenToRemove);

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
                GluxCommands.Self.RemoveReferencedFile(screenToRemove.ReferencedFiles[i], filesThatCouldBeRemoved);
            }

            ProjectManager.GlueProjectSave.Screens.Remove(screenToRemove);
            // If we're going to remove the Screen, we should remove all referenced objects that it references
            // as well as any ReferencedFiles

            RemoveUnreferencedFiles(screenToRemove, filesThatCouldBeRemoved);

            if (screenToRemove.Name == ProjectManager.GlueProjectSave.StartUpScreen)
            {
                ProjectManager.StartUpScreen = "";
            }

            for (int i = 0; i < inheritingScreens.Count; i++)
            {
                ScreenSave inheritingScreen = inheritingScreens[i];

                DialogResult resetInheritance = MessageBox.Show("Reset the inheritance for " + inheritingScreen.Name + "?",
                    "Reset Inheritance?", MessageBoxButtons.YesNo);

                if (resetInheritance == DialogResult.Yes)
                {
                    inheritingScreen.BaseScreen = "";

                    CodeWriter.GenerateCode(inheritingScreen);
                }
            }

            TaskManager.Self.OnUiThread(() =>
            {
                ElementViewWindow.RemoveScreen(screenToRemove);
            });
            IElement element = screenToRemove;

            PluginManager.ReactToScreenRemoved(screenToRemove, filesThatCouldBeRemoved);


            ProjectManager.RemoveCodeFilesForElement(filesThatCouldBeRemoved, element);


            GlueCommands.Self.ProjectCommands.SaveProjects();
            GluxCommands.Self.SaveGlux();
        }

        private static void RemoveUnreferencedFiles(IElement element, List<string> filesThatCouldBeRemoved)
        {
            List<string> allReferencedFiles = GlueCommands.Self.FileCommands.GetAllReferencedFileNames();

            for (int i = element.ReferencedFiles.Count - 1; i > -1; i--)
            {
                ReferencedFileSave rfs = element.ReferencedFiles[i];

                bool shouldRemove = true;
                foreach (string file in allReferencedFiles)
                {
                    if (file.ToLowerInvariant() == rfs.Name.ToLowerInvariant())
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
                System.Windows.Forms.MessageBox.Show(
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

        public bool GetPluginRequirement(Interfaces.IPlugin plugin)
        {
            var name = plugin.FriendlyName;

            var requiredPlugins = GlueState.Self.CurrentGlueProject.PluginData.RequiredPlugins;

            var isRequired = requiredPlugins.Any(item => item.Name == name);

            return isRequired;
        }

        public bool SetPluginRequirement(Interfaces.IPlugin plugin, bool requiredByProject)
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

        public void SetVariableOn(NamedObjectSave nos, string memberName, Type memberType, object value)
        {
            object oldValue = null;

            var instruction = nos.GetInstructionFromMember(memberName);

            if (instruction != null)
            {
                oldValue = instruction.Value;
            }
            NamedObjectPropertyGridDisplayer.SetVariableOn(nos, memberName, memberType, value);

            EditorObjects.IoC.Container.Get<NamedObjectSetVariableLogic>().ReactToNamedObjectChangedValue(
                memberName, nos.InstanceName, oldValue);


            PluginManager.ReactToChangedProperty(memberName, oldValue);
        }

        public void SaveSettings()
        {
            ProjectManager.GlueSettingsSave.Save();
        }


    }
}
