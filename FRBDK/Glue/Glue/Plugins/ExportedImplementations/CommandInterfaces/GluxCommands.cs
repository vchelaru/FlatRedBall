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

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    class GluxCommands : IGluxCommands
    {
        #region Fields

        const string NoType = "<No Type>";


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
        #endregion

        #region Methods

        public void SaveGlux(bool sendPluginRefreshCommand = true)
        {
            MainGlueWindow.Self.BeginInvoke(new EventHandler(delegate { SaveGluxSync(sendPluginRefreshCommand); }));
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
                        FileWatchManager.IgnoreNextChangeOnFile(ProjectManager.GlueProjectFileName);
                        
                        Exception lastException;
                        var succeeded = ProjectManager.GlueProjectSave.Save("GLUE", ProjectManager.GlueProjectFileName, out lastException);

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
            // Is the file relative to the project?
            // If not, don't allow the addition.
            string projectRoot = ProjectManager.ProjectRootDirectory;

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

            if (!FileManager.IsRelativeTo(fileName, projectRoot) && isBuiltFile)
            {
                copied = PluginManager.TryCopyFile(fileName, targetFileWithOriginalExtension);



                if (!copied)
                {
                    MessageBox.Show("Could not add the file\n" + fileName + "\n\nBecause it is not relative to\n" + projectRoot + "\n\nPlease move this file to a folder inside your project and try again");
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
                }
                string creationReport;

                if (String.IsNullOrWhiteSpace(errorMessage))
                {
                    string directoryToUse = null;

                    if (!isBuiltFile)
                    {
                        directoryToUse = directoryOfTreeNode;
                    }

                    toReturn = ElementCommands.CreateReferencedFileSaveForExistingFile(
                        sourceElement, directoryToUse, targetFile, PromptHandleEnum.Prompt,
                        AvailableAssetTypes.Self.GetAssetTypeFromExtension(FileManager.GetExtension(targetFile)),
                        out creationReport, out errorMessage);

                    // If toReturn was null, that means the object wasn't created
                    // The user could have said No/Cancel to some option
                    if (toReturn != null)
                    {
                        TaskManager.Self.OnUiThread(() =>
                            {
                                ElementViewWindow.UpdateChangedElements();
                            });

                        if (sourceElement == null)
                        {
                            GlueCommands.Self.RefreshCommands.RefreshGlobalContent();
                        }

                        if (!string.IsNullOrEmpty(errorMessage))
                        {
                            // this is handled below
                            //MessageBox.Show(errorMessage);
                        }
                        else if (string.IsNullOrEmpty(toReturn.Name))
                        {
                            MessageBox.Show("There was an error creating the named object for\n" + fileName);

                        }
                        else
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
                            PluginManager.ReactToNewFile(toReturn);
                            GluxCommands.Self.SaveGlux();
                        }
                    }
                }

                
            }

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                PluginManager.ReceiveError(errorMessage);
                // I think we should show an error message.  I had a user
                // try to add a file and no popup appeared telling them that
                // the entity was named that.
                MessageBox.Show(errorMessage);
            }

            if (toReturn != null)
            {
                ApplyOptions(toReturn, options);
            }

            return toReturn;
        }

        public NamedObjectSave AddNewNamedObjectToSelectedElement(AddObjectViewModel addObjectViewModel)
        {
            MembershipInfo membershipInfo = NamedObjectSaveExtensionMethodsGlue.GetMemberMembershipInfo(addObjectViewModel.ObjectName);

            var newNos = NamedObjectSaveExtensionMethodsGlue.AddNewNamedObjectToSelectedElement(addObjectViewModel.ObjectName, membershipInfo, false);

            if (addObjectViewModel.SourceClassType != NoType && !string.IsNullOrEmpty(addObjectViewModel.SourceClassType))
            {
                newNos.SourceType = addObjectViewModel.SourceType;
                newNos.SourceClassType = addObjectViewModel.SourceClassType;
                newNos.SourceFile = addObjectViewModel.SourceFile;
                newNos.SourceName = addObjectViewModel.SourceNameInFile;
                newNos.UpdateCustomProperties();

                EditorLogic.CurrentElementTreeNode.UpdateReferencedTreeNodes();
            }
            else if (!string.IsNullOrEmpty(addObjectViewModel.SourceFile))
            {
                newNos.SourceType = addObjectViewModel.SourceType;
                newNos.SourceFile = addObjectViewModel.SourceFile;
                newNos.SourceName = addObjectViewModel.SourceNameInFile;
                newNos.UpdateCustomProperties();

                EditorLogic.CurrentElementTreeNode.UpdateReferencedTreeNodes();
            }

            newNos.SourceClassGenericType = addObjectViewModel.SourceClassGenericType;

            var ati = newNos.GetAssetTypeInfo();

            if (ati != null && ati.DefaultPublic)
            {
                newNos.HasPublicProperty = true;
            }

            var currentEntity = GlueState.Self.CurrentElement as EntitySave;

            if (currentEntity != null && currentEntity.CreatedByOtherEntities && currentEntity.PooledByFactory)
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
            ElementViewWindow.GenerateSelectedElementCode();

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

        public void RemoveReferencedFile(ReferencedFileSave referencedFileToRemove, List<string> additionalFilesToRemove)
        {
            RemoveReferencedFile(referencedFileToRemove, additionalFilesToRemove, true);
        }

        public void RemoveReferencedFile(ReferencedFileSave referencedFileToRemove, List<string> additionalFilesToRemove, bool regenerateCode)
        {
            // There are some things that need to happen:
            // 1.  Remove the ReferencedFileSave from the Glue project (GLUX)
            // 2.  Remove the GUI item
            // 3.  Remove the item from the Visual Studio project.

            #region Remove the file from the current Screen or Entity if there is a current Screen or Entity

            IElement container = referencedFileToRemove.GetContainer();

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
                            EditorLogic.CurrentEntityTreeNode.UpdateReferencedTreeNodes(false);
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

                    if (treeNode.Tag != referencedFileToRemove)
                    {
                        throw new Exception("Error removing the tree node - the selected tree node doesn't reference the file being removed");
                    }

                    treeNode.Parent.Nodes.Remove(treeNode);
                };

                MainGlueWindow.Self.Invoke((MethodInvoker)delegate
                    {
                        refreshUiAction();
                    }
                    );


                ContentLoadWriter.UpdateLoadGlobalContentCode();

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



            if (ProjectManager.IsContent(referencedFileToRemove.GetRelativePath()))
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


            PluginManager.ReactToChangedProperty(memberName, oldValue);
        }

        public void SaveSettings()
        {
            ProjectManager.GlueSettingsSave.Save();
        }




        #endregion

    }
}
