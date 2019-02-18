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
        #endregion

        #region Methods

        public void SaveGlux(bool sendPluginRefreshCommand = true)
        {
            MainGlueWindow.Self.BeginInvoke(new EventHandler(delegate { SaveGluxSync(sendPluginRefreshCommand); }));
        }

        public void SaveGluxTask()
        {
            TaskManager.Self.Add(() => SaveGlux(), "Saving .glux", TaskExecutionPreference.AddOrMoveToEnd);
        }

        static void SaveGluxSync(bool sendMessageToRefresh)
        {
            if (ProjectManager.GlueProjectSave != null)
            {

                if (MainGlueWindow.Self.HasErrorOccurred)
                {
                    string projectName = FileManager.RemovePath(FileManager.RemoveExtension(ProjectManager.GlueProjectFileName));

                    MessageBox.Show("STOP RIGHT THERE!!!!  There was an error in Glue at some point.  To prevent " +
                        "corruption in the GLUX file, Glue will no longer save any changes that you make to the project.  " +
                        "You should exit out of Glue immediately and attempt to solve the problem.\n\n" +
                        "Glue has saved your work in a temporary file called " + projectName + ".gluxERROR"
                        );


                    ProjectManager.GlueProjectSave.Save("GLUE", ProjectManager.GlueProjectFileName + "ERROR");
                }
                else
                {
                    ProjectSyncer.SyncGlux();


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

                        MessageBox.Show("Error trying to save your .glux file.  Because of this error, Glue did not make any changes to the .glux file on disk.\n\nAn error log has been saved here:\n" + errorLogLocation);
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
                            MessageBox.Show("Error saving the .glux:\n" + lastException);
                        }
                        else
                        {

                            if (sendMessageToRefresh)
                            {
                                PluginManager.ReactToGluxSave();
                            }
                        }
                    }
                }
            }
        }

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

        public ReferencedFileSave AddReferencedFileToGlobalContent(string fileToAdd, bool useFullPathAsName)
        {
            return Glue.Plugins.ExportedImplementations.CommandInterfaces.ElementCommands.Self.AddReferencedFileToGlobalContent(fileToAdd, useFullPathAsName);
        }

        public ReferencedFileSave AddSingleFileTo(string fileName, string rfsName, string extraCommandLineArguments,
            BuildToolAssociation buildToolAssociation, bool isBuiltFile, string options, IElement sourceElement, string directoryOfTreeNode)
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

            if (!failed)
            {
                if (isBuiltFile)
                {
                    errorMessage = buildToolAssociation.PerformBuildOn(fileName, targetFile, extraCommandLineArguments, PluginManager.ReceiveOutput, PluginManager.ReceiveError);
                    failed = true;
                }
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

            if(!failed)
            {
                TaskManager.Self.OnUiThread(() =>
                {
                    ElementViewWindow.UpdateChangedElements();
                });

                if (sourceElement == null)
                {
                    GlueCommands.Self.RefreshCommands.RefreshGlobalContent();
                }

                PluginManager.ReactToNewFile(toReturn);
                GluxCommands.Self.SaveGlux();
                TaskManager.Self.AddSync(GluxCommands.Self.ProjectCommands.SaveProjects, "Saving projects after adding file");
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
            }

            return toReturn;
        }


        public ReferencedFileSave CreateReferencedFileSaveForExistingFile(IElement containerForFile, string directoryInsideContainer, string absoluteFileName,
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
                FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces.ElementCommands.CheckAndWarnAboutUnknownFileTypes(unknownTypeHandle, extension, out isValidExtensionOrIsConfirmedByUser, out isUnknownType);

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

        public NamedObjectSave AddNewNamedObjectToSelectedElement(AddObjectViewModel addObjectViewModel)
        {
            return AddNewNamedObjectTo(addObjectViewModel, GlueState.Self.CurrentElement, GlueState.Self.CurrentNamedObjectSave);
        }

        public NamedObjectSave AddNewNamedObjectTo(AddObjectViewModel addObjectViewModel, IElement element, NamedObjectSave namedObject)
        {

            MembershipInfo membershipInfo = NamedObjectSaveExtensionMethodsGlue.GetMemberMembershipInfo(addObjectViewModel.ObjectName);

            var newNos = NamedObjectSaveExtensionMethodsGlue.AddNewNamedObjectTo(addObjectViewModel.ObjectName,
                membershipInfo, element, namedObject, false);

            if (addObjectViewModel.SourceClassType != NoType && !string.IsNullOrEmpty(addObjectViewModel.SourceClassType))
            {
                newNos.SourceType = addObjectViewModel.SourceType;
                newNos.SourceClassType = addObjectViewModel.SourceClassType;
                newNos.SourceFile = addObjectViewModel.SourceFile;
                newNos.SourceName = addObjectViewModel.SourceNameInFile;
                newNos.UpdateCustomProperties();

                GlueCommands.Self.RefreshCommands.RefreshUi(element);
            }
            else if (!string.IsNullOrEmpty(addObjectViewModel.SourceFile))
            {
                newNos.SourceType = addObjectViewModel.SourceType;
                newNos.SourceFile = addObjectViewModel.SourceFile;
                newNos.SourceName = addObjectViewModel.SourceNameInFile;
                newNos.UpdateCustomProperties();

                GlueCommands.Self.RefreshCommands.RefreshUi(element);
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

            // it may already be selected, so force select it
            MainGlueWindow.Self.ElementTreeView.SelectedNode = null;
            MainGlueWindow.Self.ElementTreeView.SelectedNode = GlueState.Self.Find.NamedObjectTreeNode(newNos);
            GluxCommands.Self.SaveGlux();

            return newNos;
        }

        private static void ApplyOptions(ReferencedFileSave toReturn, string options)
        {
            if (toReturn.IsCsvOrTreatedAsCsv)
            {
                toReturn.CreatesDictionary = options == "Dictionary";
            }
        }


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



        private static bool GetIfFileIsFactory(EntitySave entitySave, string file)
        {
            return file.EndsWith("Factories/" + entitySave.ClassName + "Factory.Generated.cs");
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


        static bool MoveSingleCodeFileToDirectory(string relativeCodeFile, string directory)
        {
            string absoluteCodeFile = FileManager.MakeAbsolute(relativeCodeFile);
            bool succeeded = true;
            string targetFile = directory + FileManager.RemovePath(absoluteCodeFile);

            if (File.Exists(targetFile))
            {
                System.Windows.Forms.MessageBox.Show(
                    "Can't move the the file " + absoluteCodeFile + " because the following file exists in the target location: " + targetFile);
                succeeded = false;
            }

            if (succeeded)
            {
                File.Move(absoluteCodeFile, targetFile);

                ProjectManager.RemoveItemFromAllProjects(relativeCodeFile, false);
                ProjectManager.ProjectBase.AddCodeBuildItem(targetFile);
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
                    CodeWriter.GenerateCode(treeNode.SaveObjectAsElement);
                }
            }

            return succeeded;
        }


        public void RemoveReferencedFile(ReferencedFileSave referencedFileToRemove, List<string> additionalFilesToRemove)
        {
            RemoveReferencedFile(referencedFileToRemove, additionalFilesToRemove, true);
        }

        public void RemoveReferencedFile(ReferencedFileSave referencedFileToRemove, List<string> additionalFilesToRemove, bool regenerateCode)
        {

            var isContained = GlueState.Self.Find.IfReferencedFileSaveIsReferenced(referencedFileToRemove);
            /////////////////////////Early Out//////////////////////////////
            if(!isContained)
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
                            EditorLogic.CurrentScreenTreeNode.UpdateReferencedTreeNodes();
                        }
                        else if (EditorLogic.CurrentEntityTreeNode != null)
                        {
                            EditorLogic.CurrentEntityTreeNode.UpdateReferencedTreeNodes();
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
                    if(treeNode != null)
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
                GlueCommands.Self.FileCommands.GetAllReferencedFileNames().Select(item=>item.ToLowerInvariant()).ToList();

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

        
        public ReferencedFileSave GetReferencedFileSaveFromFile(string fileName)
        {
            return FlatRedBall.Glue.Elements.ObjectFinder.Self.GetReferencedFileSaveFromFile(fileName);

        }


    #endregion

    }
}
