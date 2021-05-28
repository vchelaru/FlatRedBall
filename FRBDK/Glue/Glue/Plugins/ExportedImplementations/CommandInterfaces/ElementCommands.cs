using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.IO.Zip;
using System.Windows.Forms;
using FlatRedBall.Glue.StandardTypes;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Errors;
using EditorObjects.SaveClasses;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Utilities;
using FlatRedBall.Glue.Projects;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Controls;
using GlueFormsCore.ViewModels;
using FlatRedBall.Glue.ViewModels;
using Microsoft.Xna.Framework;
using Glue;
using FlatRedBall.Glue.SetVariable;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    class ElementCommands : IScreenCommands, IEntityCommands,IElementCommands
    {
        static ElementCommands mSelf;
        public static ElementCommands Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new ElementCommands();
                }
                return mSelf;
            }
        }


        public SaveClasses.ScreenSave AddScreen(string screenName)
        {
            ScreenSave screenSave = new ScreenSave();
            screenSave.Name = @"Screens\" + screenName;

            AddScreen(screenSave, suppressAlreadyExistingFileMessage:false);

            return screenSave;
        }

        public void AddScreen(ScreenSave screenSave, bool suppressAlreadyExistingFileMessage = false)
        {
            var glueProject = GlueState.Self.CurrentGlueProject;

            string screenName = FileManager.RemovePath(screenSave.Name);

            string fileName = screenSave.Name + ".cs";

            screenSave.Tags.Add("GLUE");
            screenSave.Source = "GLUE";

            glueProject.Screens.Add(screenSave);
            glueProject.Screens.SortByName();

            #region Create the Screen code (not the generated version)


            var fullNonGeneratedFileName = FileManager.RelativeDirectory + fileName;
            var addedScreen = 
                GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(fullNonGeneratedFileName, save:false);


            string projectNamespace = ProjectManager.ProjectNamespace;

            StringBuilder stringBuilder = new StringBuilder(CodeWriter.ScreenTemplateCode);

            CodeWriter.SetClassNameAndNamespace(
                projectNamespace + ".Screens",
                screenName,
                stringBuilder);

            string modifiedTemplate = stringBuilder.ToString();


            if (addedScreen == null)
            {
                if (!suppressAlreadyExistingFileMessage)
                {
                    MessageBox.Show("There is already a file named\n\n" + fullNonGeneratedFileName + "\n\nThis file will be used instead of creating a new one just in case you have code that you want to keep there.");
                }
            }
            else
            {

                FileManager.SaveText(
                    modifiedTemplate,
                    fullNonGeneratedFileName
                    );
            }


            #endregion

            #region Create <ScreenName>.Generated.cs

            string generatedFileName = @"Screens\" + screenName + ".Generated.cs";
            ProjectManager.CodeProjectHelper.CreateAndAddPartialCodeFile(generatedFileName, true);


            #endregion

            // We used to set the 
            // StartUpScreen whenever
            // the user made a new Screen.
            // The reason is we assumed that
            // the user wanted to work on this
            // Screen, so we set it as the startup
            // so they could run the game right away.
            // Now we only want to do it if there are no
            // other Screens.  Otherwise they can just use
            // GlueView.
            GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(screenSave);
            if (glueProject.Screens.Count == 1)
            {
                ElementViewWindow.StartUpScreenTreeNode =
                    GlueState.Self.Find.ElementTreeNode(screenSave);
            }

            GlueCommands.Self.GenerateCodeCommands.GenerateElementCodeTask(screenSave);

            PluginManager.ReactToNewScreenCreated(screenSave);


            GlueCommands.Self.ProjectCommands.SaveProjects();

            GluxCommands.Self.SaveGlux();
        }


        public SaveClasses.EntitySave AddEntity(string entityName, bool is2D = false)
        {

            string fileName = entityName + ".cs";

            if (!entityName.ToLower().StartsWith("entities\\") && !entityName.ToLower().StartsWith("entities/"))
            {
                fileName = @"Entities\" + fileName;
            }



            EntitySave entitySave = new EntitySave();
            entitySave.Is2D = is2D;
            entitySave.Name = FileManager.RemoveExtension(fileName);

            const bool AddXYZ = true;

            if (AddXYZ)
            {
                entitySave.CustomVariables.Add(new CustomVariable() { Name = "X", Type = "float" });
                entitySave.CustomVariables.Add(new CustomVariable() { Name = "Y", Type = "float" });
                entitySave.CustomVariables.Add(new CustomVariable() { Name = "Z", Type = "float" });
            }

            AddEntity(entitySave);

            return entitySave;

        }

        public SaveClasses.EntitySave AddEntity(AddEntityViewModel viewModel, string directory = null)
        {
            var gluxCommands = GlueCommands.Self.GluxCommands;

            var newElement = gluxCommands.EntityCommands.AddEntity(
                directory + viewModel.Name, is2D: true);

            GlueState.Self.CurrentElement = newElement;

            var hasInheritance = false;
            if(viewModel.HasInheritance)
            {
                newElement.BaseEntity = viewModel.SelectedBaseEntity;

                EditorObjects.IoC.Container.Get<SetPropertyManager>().ReactToPropertyChanged(
                    nameof(newElement.BaseEntity), false, nameof(newElement.BaseEntity), null);

                hasInheritance = true;
            }

            if (viewModel.IsSpriteChecked)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ObjectName = "SpriteInstance";
                addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.Sprite;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                gluxCommands.AddNewNamedObjectToSelectedElement(addObjectViewModel);
                GlueState.Self.CurrentElement = newElement;
            }

            if (viewModel.IsTextChecked)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ObjectName = "TextInstance";
                addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.Text;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                gluxCommands.AddNewNamedObjectToSelectedElement(addObjectViewModel);
                GlueState.Self.CurrentElement = newElement;
            }

            if (viewModel.IsCircleChecked)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ObjectName = "CircleInstance";
                addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.Circle;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                gluxCommands.AddNewNamedObjectToSelectedElement(addObjectViewModel);
                GlueState.Self.CurrentElement = newElement;
            }

            if (viewModel.IsAxisAlignedRectangleChecked)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ObjectName = "AxisAlignedRectangleInstance";
                addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.AxisAlignedRectangle;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                gluxCommands.AddNewNamedObjectToSelectedElement(addObjectViewModel);
                GlueState.Self.CurrentElement = newElement;
            }

            // There are a few important things to note about this function:
            // 1. Whenever gluxCommands.AddNewNamedObjectToSelectedElement is called, Glue performs a full
            //    refresh and save. The reason for this is that gluxCommands.AddNewNamedObjectToSelectedElement
            //    is the standard way to add a new named object to an element, and it may be called by other parts
            //    of the code (and plugins) that expect the add to be a complete set of logic (add, refresh, save, etc).
            //    This is less efficient than adding all of them and saving only once, but that would require a second add
            //    method, which would add complexity. For now, we deal with the slower calls because it's not really noticeable.
            // 2. Some actions, like adding Points to a polygon, are done after the polygon is created and added, and that requires
            //    an additional save. Therefore, we do one last save/refresh at the end of this method in certain situations.
            //    Again, this is less efficient than if we performed just a single call, but a single call would be more complicated.
            //    because we'd have to suppress all the other calls.
            bool needsRefreshAndSave = false;

            if (viewModel.IsPolygonChecked)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ObjectName = "PolygonInstance";
                addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.Polygon;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;

                var nos = gluxCommands.AddNewNamedObjectToSelectedElement(addObjectViewModel);
                CustomVariableInNamedObject instructions = null;
                instructions = nos.GetCustomVariable("Points");
                if (instructions == null)
                {
                    instructions = new CustomVariableInNamedObject();
                    instructions.Member = "Points";
                    nos.InstructionSaves.Add(instructions);
                }
                var points = new List<Vector2>();
                points.Add(new Vector2(-16, 16));
                points.Add(new Vector2(16, 16));
                points.Add(new Vector2(16, -16));
                points.Add(new Vector2(-16, -16));
                points.Add(new Vector2(-16, 16));
                instructions.Value = points;


                needsRefreshAndSave = true;

                GlueState.Self.CurrentElement = newElement;
            }

            if(!hasInheritance)
            {
                if (viewModel.IsIVisibleChecked)
                {
                    newElement.ImplementsIVisible = true;
                    needsRefreshAndSave = true;
                }

                if (viewModel.IsIClickableChecked)
                {
                    newElement.ImplementsIClickable = true;
                    needsRefreshAndSave = true;
                }

                if (viewModel.IsIWindowChecked)
                {
                    newElement.ImplementsIWindow = true;
                    needsRefreshAndSave = true;
                }

                if (viewModel.IsICollidableChecked)
                {
                    newElement.ImplementsICollidable = true;
                    needsRefreshAndSave = true;
                }
            }


            if (needsRefreshAndSave)
            {
                MainGlueWindow.Self.PropertyGrid.Refresh();
                ElementViewWindow.GenerateSelectedElementCode();
                GluxCommands.Self.SaveGlux();
            }

            return newElement;
        }


        public void AddEntity(EntitySave entitySave)
        {
            AddEntity(entitySave, false);
        }

        public void AddEntity(EntitySave entitySave, bool suppressAlreadyExistingFileMessage)
        {
            string fileName = entitySave.Name + ".cs";
            entitySave.Tags.Add("GLUE");
            entitySave.Source = "GLUE";

            var glueProject = GlueState.Self.CurrentGlueProject;

            glueProject.Entities.Add(entitySave);

            glueProject.Entities.SortByName();

            #region Create the Entity custom code file (not the generated version)

            var newItem = GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(fileName, false);

            string projectNamespace = GlueState.Self.ProjectNamespace;


            string directory = FileManager.GetDirectory(entitySave.Name);
            if (!directory.ToLower().EndsWith(projectNamespace.ToLower() + "/entities/"))
            {
                GlueCommands.Self.GenerateCodeCommands.GetNamespaceForElement(entitySave);
                // test this on doubly-embedded Entities.
                projectNamespace = GlueCommands.Self.GenerateCodeCommands.GetNamespaceForElement(entitySave);
                // += (".Entities." + FileManager.RemovePath(directory)).Replace("/", "");
            }

            StringBuilder stringBuilder = new StringBuilder(CodeWriter.EntityTemplateCode);

            CodeWriter.SetClassNameAndNamespace(
                projectNamespace,
                entitySave.ClassName,
                stringBuilder);

            string modifiedTemplate = stringBuilder.ToString();

            string nonGeneratedFileName = FileManager.RelativeDirectory + fileName;

            if (newItem == null)
            {
                if (!suppressAlreadyExistingFileMessage)
                {
                    MessageBox.Show("There is already a file named\n\n" + nonGeneratedFileName + "\n\nThis file will be used instead of creating a new one just in case you have code that you want to keep there.");
                }
            }
            else
            {
                FileManager.SaveText(modifiedTemplate, nonGeneratedFileName);
            }
            #endregion

            #region Create <EntityName>.Generated.cs

            string generatedFileName = FileManager.MakeRelative(directory).Replace("/", "\\") + entitySave.ClassName + ".Generated.cs";

            ProjectManager.CodeProjectHelper.CreateAndAddPartialCodeFile(generatedFileName, true);

            #endregion

            GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(entitySave);

            GlueCommands.Self.GenerateCodeCommands.GenerateElementCodeTask(entitySave);

            GlueCommands.Self.ProjectCommands.SaveProjects();

            GluxCommands.Self.SaveGlux();
        }

        [Obsolete("This function does way too much. Moving this to GluxCommands")]
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
                CheckAndWarnAboutUnknownFileTypes(unknownTypeHandle, extension, out isValidExtensionOrIsConfirmedByUser, out isUnknownType);

                string fileToAdd = null;
                if (isValidExtensionOrIsConfirmedByUser)
                {

                    string directoryThatFileShouldBeRelativeTo = GetFullPathContentDirectory(containerForFile, directoryInsideContainer);

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
                        fileToAdd = GetNameOfFileRelativeToContentFolder(absoluteFileName, directoryThatFileShouldBeRelativeTo, projectDirectory);

                    }

                }

                if(string.IsNullOrEmpty(errorMessage))
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
                        referencedFileSaveToReturn =
                            GlueCommands.Self.GluxCommands.AddReferencedFileToGlobalContent(fileToAdd, useFullPathAsName);
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

                        GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(referencedFileSaveToReturn);

                        PluginManager.ReactToNewFile(referencedFileSaveToReturn);
                        GluxCommands.Self.SaveGlux();
                        GlueCommands.Self.ProjectCommands.SaveProjects();
                        UnreferencedFilesManager.Self.RefreshUnreferencedFiles(false);

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

        public static string GetNameOfFileRelativeToContentFolder(string absoluteSourceFileName, string directoryThatFileShouldBeRelativeTo, string projectDirectory)
        {
            string fileToAdd = "";
            var rfs = ObjectFinder.Self.GetReferencedFileSaveFromFile(absoluteSourceFileName);

            if (rfs != null)
            {
                fileToAdd = rfs.Name;
            }
            else
            {
                fileToAdd = FileManager.MakeRelative(absoluteSourceFileName, ProjectManager.ContentProject.GetAbsoluteContentFolder());
            }
            return fileToAdd;
        }

        public static void CheckAndWarnAboutUnknownFileTypes(PromptHandleEnum unknownTypeHandle, string extension, out bool isValidExtensionOrIsConfirmedByUser, out bool isUnknownType)
        {
            isValidExtensionOrIsConfirmedByUser = true;
            isUnknownType = false;

            if (AvailableAssetTypes.Self.GetAssetTypeFromExtension(extension) == null && extension != "csv")
            {
                DialogResult dialogResult = DialogResult.Yes;
                bool addToList;

                if (!AvailableAssetTypes.Self.AdditionalExtensionsToTreatAsAssets.Contains(extension))
                {
                    switch (unknownTypeHandle)
                    {
                        case PromptHandleEnum.Prompt:
                            dialogResult = MessageBox.Show("The extension " + extension + " is not recognized by Glue.  " +
                                                           "Glue will not be able to generate code for this file, but will add it to your game project.\n\nDo you " +
                                                           "want to add this file?", "Add unknown type?", MessageBoxButtons.YesNo);
                            break;
                        case PromptHandleEnum.DoYes:
                            dialogResult = DialogResult.Yes;
                            break;
                        case PromptHandleEnum.DoNo:
                            dialogResult = DialogResult.No;
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    addToList = true;
                }
                else
                {
                    // This means the user has already said "yes" to adding this type
                    dialogResult = DialogResult.Yes;
                    addToList = false;
                }


                if (dialogResult == DialogResult.No)
                {
                    isValidExtensionOrIsConfirmedByUser = false;
                }
                else
                {
                    isUnknownType = true;
                    if (addToList)
                    {
                        AvailableAssetTypes.Self.AdditionalExtensionsToTreatAsAssets.Add(extension);
                    }
                }
            }
        }

        public static string GetFullPathContentDirectory(IElement element, string directoryRelativeToElement)
        {
            string resultNameInFolder = "";

            if (!String.IsNullOrEmpty(directoryRelativeToElement))
            {
                //string directory = directoryTreeNode.GetRelativePath().Replace("/", "\\");

                resultNameInFolder = directoryRelativeToElement;
            }
            else if (element != null)
            {
                //string directory = elementToAddTo.GetRelativePath().Replace("/", "\\");

                resultNameInFolder = element.Name.Replace(@"/", @"\");
            }
            else
            {
                resultNameInFolder = "GlobalContent/";
            }

            if (!resultNameInFolder.EndsWith("\\") && !resultNameInFolder.EndsWith("/"))
            {
                resultNameInFolder += "\\";
            }


            return ProjectManager.ContentDirectory + resultNameInFolder;
        }




    }
}
