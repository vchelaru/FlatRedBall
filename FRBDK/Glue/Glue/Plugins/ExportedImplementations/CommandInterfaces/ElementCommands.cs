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
using FlatRedBall.Glue.SaveClasses.Helpers;
using GlueFormsCore.Managers;
using System.Threading.Tasks;
using System.IO;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Events;
using EditorObjects.IoC;
using System.Windows.Data;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.Refactoring.Views;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;
using Microsoft.VisualBasic;
using GeneralResponse = ToolsUtilities.GeneralResponse;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;


public class ChangedNamedObjectVariable
{
    public NamedObjectSave NamedObjectSave;
    public string VariableName;

    public override string ToString()
    {
        var newValue = NamedObjectSave.GetCustomVariable(VariableName)?.Value?.ToString() ?? "";
        return NamedObjectSave + "." + VariableName + " = "  + newValue;
    }
}

public class FileChange
{
    public FilePath OldFile;
    public FilePath NewFile;

    public override string ToString()
    {
        return OldFile + " -> " + NewFile;
    }
}

public class RenameModifications
{
    public List<FileChange> CodeFilesAffectedByRename = new List<FileChange>();
    public List<GlueElement> ElementsWithChangedBaseType = new List<GlueElement>();
    public List<NamedObjectSave> ObjectsWithChangedBaseEntity = new List<NamedObjectSave>();
    public List<NamedObjectSave> ObjectsWithChangedGenericBaseEntity = new List<NamedObjectSave>();
    public List<NamedObjectSave> ChangedCollisionRelationships = new List<NamedObjectSave>();
    public List<ChangedNamedObjectVariable> ChangedNamedObjectVariables = new List<ChangedNamedObjectVariable>();
    public List<CustomVariable> ChangedCustomVariables = new List<CustomVariable>();
    public string StartupScreenChange;
}
public class ElementCommands : IScreenCommands, IEntityCommands, IElementCommands
{
    #region Fields/Properties

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

    #endregion

    #region RenameElement (both screens and entities)

    /// <summary>
    /// Performs all logic related to renaming an element. The name should not have the "Screens\\" or "Entities\\" prefix, nor any prefixes
    /// for the entity's folder. In other words, GameScreen would be "GameScreen" rather than "Screens\\GameScreen".
    /// </summary>
    /// <param name="elementToRename">The element to rename.</param>
    /// <param name="newElementName">The new full name. "Entities\\Subfolder\\NewName".</param>
    /// <returns>A task which completes when all logic and UI are finished.</returns>
    public async Task RenameElement(GlueElement elementToRename, string newFullElementName, bool showRenameWindow = true)
    {
        newFullElementName = newFullElementName.Replace("/", "\\");
        await TaskManager.Self.AddAsync(() =>
        {
            bool isValid = true;
            string whyItIsntValid;
            if (elementToRename is ScreenSave)
            {
                isValid = NameVerifier.IsScreenNameValid(newFullElementName, elementToRename as ScreenSave, out whyItIsntValid);
            }
            else
            {
                isValid = NameVerifier.IsEntityNameValid(newFullElementName, elementToRename as EntitySave, out whyItIsntValid);

            }

            if (!isValid)
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox(whyItIsntValid);
            }
            else
            {
                DoRenameInner(elementToRename, newFullElementName, showRenameWindow);
            }
        }, $"Renaming {elementToRename} to {newFullElementName}");
    }

    private void DoRenameInner(GlueElement elementToRename, string newElementName, bool showRenameWindow)
    {
        RenameModifications renameModifications = new RenameModifications();

        string oldElementName = elementToRename.Name;
        var fileNameBeforeMove = GlueCommands.Self.FileCommands.GetJsonFilePath(elementToRename);

        var oldFileNames = CodeWriter.GetAllCodeFilesFor(elementToRename);

        var changeClassNamesResponse = ChangeClassNamesAndNamespaceInCodeAndFileName(oldFileNames, oldElementName, newElementName);

        var oldDirectory = FileManager.GetDirectory(oldElementName, RelativeType.Relative);
        var newDirectory = FileManager.GetDirectory(newElementName, RelativeType.Relative);
        var didChangeDirectory = oldDirectory != newDirectory;

        if (GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.SeparateJsonFilesForElements)
        {
            // delete the old file (put it in recycle bin)
            // From https://stackoverflow.com/questions/2342628/deleting-file-to-recycle-bin-on-windows-x64-in-c-sharp

            if (fileNameBeforeMove?.Exists() == true)
            {
                try
                {
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                        fileNameBeforeMove.FullPath,
                        Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                        Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                }
                catch (Exception e)
                {
                    GlueCommands.Self.PrintError(e.ToString());
                }
            }
        }

        if (changeClassNamesResponse.Succeeded)
        {
            renameModifications.CodeFilesAffectedByRename.AddRange(changeClassNamesResponse.Data);

            // Set the name first because that's going
            // to be used by code that follows to modify
            // inheritance.
            elementToRename.Name = newElementName;

            var elementsToRegenerate = new HashSet<GlueElement>();

            // The Types object is in the root object, so we need to generate the root-most object
            elementsToRegenerate.Add(ObjectFinder.Self.GetRootBaseElement(elementToRename));

            if (elementToRename is EntitySave entityToRename)
            {
                // Change any Entities that depend on this
                for (int i = 0; i < ProjectManager.GlueProjectSave.Entities.Count; i++)
                {
                    var entitySave = ProjectManager.GlueProjectSave.Entities[i];
                    if (entitySave.BaseElement == oldElementName)
                    {
                        entitySave.BaseEntity = newElementName;
                        renameModifications.ElementsWithChangedBaseType.Add(entitySave);
                    }
                }

                // Change any NamedObjects that use this as their type (whether in Entity, or as a generic class)
                List<NamedObjectSave> namedObjectsWithElementSourceClassType = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(oldElementName);

                foreach (NamedObjectSave nos in namedObjectsWithElementSourceClassType)
                {
                    elementsToRegenerate.Add(ObjectFinder.Self.GetElementContaining(nos));
                    if (nos.SourceType == SourceType.Entity && nos.SourceClassType == oldElementName)
                    {
                        nos.SourceClassType = newElementName;
                        renameModifications.ObjectsWithChangedBaseEntity.Add(nos);
                        nos.UpdateCustomProperties();
                    }
                    else if (nos.SourceType == SourceType.FlatRedBallType && nos.SourceClassGenericType == oldElementName)
                    {
                        nos.SourceClassGenericType = newElementName;
                        renameModifications.ObjectsWithChangedGenericBaseEntity.Add(nos);

                    }
                    else if (nos.IsCollisionRelationship())
                    {
                        var didChange = (bool)PluginManager.CallPluginMethod(
                            "Collision Plugin",
                            "FixNamedObjectCollisionType",
                            new object[] { nos });

                        if (didChange)
                        {
                            renameModifications.ChangedCollisionRelationships.Add(nos);
                        }
                    }
                }

                List<NamedObjectSave> namedObjectsWithElementAsVariableType = ObjectFinder.Self.GetAllNamedObjectsThatUseEntityAsVariableType(oldElementName);
                foreach (var nos in namedObjectsWithElementAsVariableType)
                {
                    elementsToRegenerate.Add(ObjectFinder.Self.GetElementContaining(nos));

                    foreach (var variable in nos.InstructionSaves)
                    {
                        if ((variable.Value as string) == oldElementName)
                        {
                            variable.Value = newElementName;

                            renameModifications.ChangedNamedObjectVariables.Add(new ChangedNamedObjectVariable
                            {
                                NamedObjectSave = nos,
                                VariableName = variable.Member
                            });
                        }
                    }
                }

                // If this has a base entity, then the most base entity might be used in a list associated with factories.
                if (!string.IsNullOrEmpty(elementToRename.BaseElement) && entityToRename.CreatedByOtherEntities)
                {
                    var rootBase = ObjectFinder.Self.GetBaseElementRecursively(elementToRename);

                    if (rootBase != elementToRename)
                    {
                        foreach (var nosUsingRoot in ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(rootBase as EntitySave))
                        {
                            elementsToRegenerate.Add(ObjectFinder.Self.GetElementContaining(nosUsingRoot));
                        }
                    }
                }

                // todo - what about free-floating variables that aren't tied to NOS's? We have this code for screens, but not for entities?
            }
            else
            {
                // Change any Screens that depend on this
                for (int i = 0; i < ProjectManager.GlueProjectSave.Screens.Count; i++)
                {
                    var screenSave = ProjectManager.GlueProjectSave.Screens[i];
                    if (screenSave.BaseScreen == oldElementName)
                    {
                        screenSave.BaseScreen = newElementName;

                        renameModifications.ElementsWithChangedBaseType.Add(screenSave);

                    }
                }

                if (GlueCommands.Self.GluxCommands.StartUpScreenName == oldElementName)
                {
                    GlueCommands.Self.GluxCommands.StartUpScreenName = newElementName;
                    renameModifications.StartupScreenChange = newElementName;

                }
                // Don't do anything with NamedObjects and Screens since they can't (currently) be named objects
            }

            var variablesReferencingElement = ObjectFinder.Self.GetVariablesReferencingElementType(oldElementName);

            var newVariantName = elementToRename.Name.Replace("\\", ".") + "Variant";
            var oldVariantName = oldElementName.Replace("\\", ".") + "Variant";

            foreach (var variable in variablesReferencingElement)
            {

                if((variable.DefaultValue as string) == oldElementName)
                {
                    variable.DefaultValue = newElementName;
                }
                if(variable.Type == oldVariantName)
                {
                    variable.Type = newVariantName;
                }

                renameModifications.ChangedCustomVariables.Add(variable);

                elementsToRegenerate.Add(ObjectFinder.Self.GetElementContaining(variable));
            }

            foreach (var element in elementsToRegenerate)
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
            }

            GlueCommands.Self.GenerateCodeCommands.GenerateGame1();

            GlueCommands.Self.ProjectCommands.SaveProjects();

            GlueState.Self.CurrentGlueProject.Entities.SortByName();
            GlueState.Self.CurrentGlueProject.Screens.SortByName();

            GlueCommands.Self.GluxCommands.SaveProjectAndElements();


            GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(elementToRename);

            PluginManager.ReactToElementRenamed(elementToRename, oldElementName);

            if(showRenameWindow)
            {
                GlueCommands.Self.DoOnUiThread(() =>
                {
                    // show a wrap-up of what happened
                    var window = new RenameModificationWindow();
                    window.SetFrom(renameModifications);    
                    window.ShowDialog();
                });
            }
        }
    }

    private ToolsUtilities.GeneralResponse<List<FileChange>> ChangeClassNamesAndNamespaceInCodeAndFileName(List<FilePath> validFiles, string oldName, string newName)
    {

        string oldStrippedName = FileManager.RemovePath(oldName);
        string newStrippedName = FileManager.RemovePath(newName);


        List<FilePath> filesThatWillGetOverwritten = new List<FilePath>();
        List<FileChange> oldNewAbsoluteFiles = new List<FileChange>();

        foreach (var file in validFiles)
        {
            string newFile = file.FullPath.Replace(oldName.Replace("\\", "/"), newName.Replace("\\", "/"));

            // replace it if it's a factory:
            if (newFile.Contains("/Factories/"))
            {
                newFile = newFile.Replace($"/Factories/{oldStrippedName}Factory.Generated.cs", $"/Factories/{newStrippedName}Factory.Generated.cs");
            }

            oldNewAbsoluteFiles.Add(new FileChange { OldFile = file, NewFile = newFile });

            if (File.Exists(newFile) && 
                // No need to warn the user about generated files getting overwritten.
                // They get ovewritten every time Glue is opened.
                newFile.Contains(".Generated.") == false)
            {
                filesThatWillGetOverwritten.Add(newFile);
            }

        }
        var response = ToolsUtilities.GeneralResponse<List<FileChange>>.SuccessfulResponse;
        response.Data = oldNewAbsoluteFiles;

        if (filesThatWillGetOverwritten.Count > 0)
        {
            var message = "This rename would result in existing files being overwritten.";

            foreach(var file in filesThatWillGetOverwritten)
            {
                message += "\n" + file;
            }
            
            message += "\n\nOverwrite?";
            var result = MessageBox.Show(message, "Overwrite",
                MessageBoxButtons.YesNo);

            response.Succeeded = result == DialogResult.Yes;
        }


        if (response.Succeeded)
        {
            var newNamespace = GlueCommands.Self.GenerateCodeCommands.GetNamespaceForElementName(newName);
            var oldNamespace = GlueCommands.Self.GenerateCodeCommands.GetNamespaceForElementName(oldName);
            foreach (var pair in oldNewAbsoluteFiles)
            {
                string absoluteOldFile = pair.OldFile.FullPath;
                string absoluteNewFile = pair.NewFile.FullPath;

                bool isCapitalizationOnlyChange = absoluteOldFile.Equals(absoluteNewFile, StringComparison.InvariantCultureIgnoreCase);

                if (isCapitalizationOnlyChange == false && File.Exists(absoluteNewFile))
                {
                    FileHelper.MoveToRecycleBin(absoluteNewFile);
                }

                // The old files may not exist
                // for a variety of reasons (Glue
                // error, user manually removed the file,
                // etc).
                if (File.Exists(absoluteOldFile))
                {
                    File.Move(absoluteOldFile, absoluteNewFile);
                }

                if (File.Exists(absoluteNewFile))
                {
                    // Change the class name in the non-generated .cs
                    string fileContents = FileManager.FromFileText(absoluteNewFile);
                    // We call RemovePath because the name is going to be "Namespace/ClassName" and we want
                    // to find just "ClassName".
                    RefactorManager.Self.RenameClassInCode(
                        FileManager.RemovePath(oldName),
                        newStrippedName,
                        ref fileContents);

                    if(oldNamespace != newNamespace)
                    {
                        fileContents = CodeWriter.ReplaceNamespace(fileContents, newNamespace);
                    }

                    FileManager.SaveText(fileContents, absoluteNewFile);

                    string relativeOld = FileManager.MakeRelative(absoluteOldFile);
                    string relativeNew = FileManager.MakeRelative(absoluteNewFile);

                    ProjectManager.ProjectBase.RenameItem(relativeOld, relativeNew);

                    foreach (VisualStudioProject syncedProject in GlueState.Self.SyncedProjects)
                    {
                        string syncedRelativeOld = FileManager.MakeRelative(absoluteOldFile, syncedProject.Directory);
                        string syncedRelativeNew = FileManager.MakeRelative(absoluteNewFile, syncedProject.Directory);
                        syncedProject.RenameItem(syncedRelativeOld, syncedRelativeNew);
                    }
                }
            }
        }
        return response;
    }


    #endregion

    #region Add Screen

    public async Task<SaveClasses.ScreenSave> AddScreen(string screenName)
    {
        ScreenSave screenSave = new ScreenSave();
        screenSave.Name = @"Screens\" + screenName;

        await AddScreen(screenSave, suppressAlreadyExistingFileMessage:false);

        return screenSave;
    }

    public async Task AddScreen(ScreenSave screenSave, bool suppressAlreadyExistingFileMessage = false)
    {
        await TaskManager.Self.AddAsync(async () =>
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
                GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(fullNonGeneratedFileName, save: false);


            var shouldGenerateCustomCode = addedScreen != null;

            if (addedScreen == null)
            {
                if (!suppressAlreadyExistingFileMessage)
                {
                    MessageBox.Show("There is already a file named\n\n" + fullNonGeneratedFileName + "\n\nThis file will be used instead of creating a new one just in case you have code that you want to keep there.");
                }
            }
            
            if(shouldGenerateCustomCode)
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateElementCustomCode(screenSave);
            }


            #endregion

            #region Create <ScreenName>.Generated.cs

            string generatedFileName = @"Screens\" + screenName + ".Generated.cs";
            ProjectManager.CodeProjectHelper.CreateAndAddPartialGeneratedCodeFile(generatedFileName, true);


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
            if (glueProject.Screens.Count == 1)
            {
                GlueState.Self.CurrentGlueProject.StartUpScreen = screenSave.Name;
                GlueCommands.Self.GenerateCodeCommands.GenerateStartupScreenCode();
            }
            // Plugin should react to new screen before generating or refreshing tree node so that tree nodes can show new files
            await PluginManager.ReactToNewScreenCreated(screenSave);

            // Refresh tree node after plugin manager has a chance to make changes according to the screen
            GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(screenSave);

            GlueCommands.Self.RefreshCommands.RefreshErrors();

            GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(screenSave, false);

            var allBase = ObjectFinder.Self.GetAllBaseElementsRecursively(screenSave);
            foreach(var baseItem in allBase)
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(baseItem, false);
            }

            GlueCommands.Self.ProjectCommands.SaveProjects();

            _ = GluxCommands.Self.SaveElementAsync(screenSave);

            GluxCommands.Self.SaveGlujFile();
        }, nameof(AddScreen));
        
    }

    #endregion

    #region Add Entity

    public SaveClasses.EntitySave AddEntity(string entityName, bool is2D = false, bool notifyPluginsOfNewEntity = true)
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
            entitySave.CustomVariables.Add(new CustomVariable() { Name = "X", Type = "float", SetByDerived = true });
            entitySave.CustomVariables.Add(new CustomVariable() { Name = "Y", Type = "float", SetByDerived = true });
            entitySave.CustomVariables.Add(new CustomVariable() { Name = "Z", Type = "float", SetByDerived = true });
        }

        AddEntity(entitySave, notifyPluginsOfNewEntity);

        return entitySave;

    }

    public async Task<SaveClasses.EntitySave> AddEntityAsync(AddEntityViewModel viewModel)
    {
        var gluxCommands = GlueCommands.Self.GluxCommands;

        var directory = viewModel.Directory;

        var newElement = gluxCommands.EntityCommands.AddEntity(
            directory + viewModel.Name, is2D: true,
            // Don't notify, we'll do so lower in this method after applying the ViewModel's properties.
            notifyPluginsOfNewEntity: false);

        // Why select it here? This causes the tree view to not yet show the inherited variables.
        // Maybe this was done because the property ReactToPropertyChanged required it to be selected?
        //GlueState.Self.CurrentElement = newElement;

        var hasInheritance = false;
        if(viewModel.HasInheritance)
        {
            newElement.BaseEntity = viewModel.SelectedBaseEntity;

            var baseEntity = ObjectFinder.Self.GetEntitySave(viewModel.SelectedBaseEntity);

            List<CustomVariable> variablesToRemove = new List<CustomVariable>();
            // continue here...

            // make X, Y, Z DefinedByBase
            foreach(var customVariable in newElement.CustomVariables)
            {
                if(customVariable.Name == "X" || customVariable.Name == "Y" || customVariable.Name == "Z")
                {
                    // See if the base has this:

                    var foundVariableInBase = baseEntity?.GetCustomVariableRecursively(customVariable.Name) != null;

                    if(!foundVariableInBase)
                    {
                        variablesToRemove.Add(customVariable);
                    }
                    else
                    {
                        customVariable.DefinedByBase = true;
                    }
                }
            }

            newElement.CustomVariables.RemoveAll(item => variablesToRemove.Contains(item));

            //EditorObjects.IoC.Container.Get<SetPropertyManager>().ReactToPropertyChanged(
            //    nameof(newElement.BaseEntity), false, nameof(newElement.BaseEntity), null);

            Container.Get<EntitySaveSetPropertyLogic>().ReactToEntityChangedProperty(nameof(newElement.BaseEntity), 
                null, newElement);


            hasInheritance = true;
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

        if (!hasInheritance)
        {
            if (viewModel.IsSpriteChecked)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ForcedElementToAddTo = newElement;
                addObjectViewModel.ObjectName = "SpriteInstance";
                addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.Sprite;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                await gluxCommands.AddNewNamedObjectToAsync(addObjectViewModel, newElement);
            }

            if (viewModel.IsTextChecked)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ForcedElementToAddTo = newElement;
                addObjectViewModel.ObjectName = "TextInstance";
                addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.Text;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                await gluxCommands.AddNewNamedObjectToAsync(addObjectViewModel, newElement);
            }

            if (viewModel.IsCircleChecked)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ForcedElementToAddTo = newElement;
                addObjectViewModel.ObjectName = "CircleInstance";
                addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.Circle;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                await gluxCommands.AddNewNamedObjectToAsync(addObjectViewModel, newElement);
            }

            if (viewModel.IsAxisAlignedRectangleChecked)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ForcedElementToAddTo = newElement;
                addObjectViewModel.ObjectName = "AxisAlignedRectangleInstance";
                addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.AxisAlignedRectangle;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                await gluxCommands.AddNewNamedObjectToAsync(addObjectViewModel, newElement);
            }
            if (viewModel.IsPolygonChecked)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ForcedElementToAddTo = newElement;
                addObjectViewModel.ObjectName = "PolygonInstance";
                addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.Polygon;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;

                var nos = await gluxCommands.AddNewNamedObjectToAsync(addObjectViewModel, newElement);
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
            }

            if (viewModel.IsIVisibleChecked)
            {
                newElement.ImplementsIVisible = true;
                needsRefreshAndSave = true;
                await GlueCommands.Self.GluxCommands.ElementCommands.ReactToPropertyChanged(newElement, nameof(newElement.ImplementsIVisible), false);
            }

            if (viewModel.IsIClickableChecked)
            {
                newElement.ImplementsIClickable = true;
                needsRefreshAndSave = true;
                await GlueCommands.Self.GluxCommands.ElementCommands.ReactToPropertyChanged(newElement, nameof(newElement.ImplementsIClickable), false);
            }

            if (viewModel.IsIWindowChecked)
            {
                newElement.ImplementsIWindow = true;
                needsRefreshAndSave = true;
                await GlueCommands.Self.GluxCommands.ElementCommands.ReactToPropertyChanged(newElement, nameof(newElement.ImplementsIWindow), false);
            }

            if (viewModel.IsICollidableChecked)
            {
                newElement.ImplementsICollidable = true;
                needsRefreshAndSave = true;

                await GlueCommands.Self.GluxCommands.ElementCommands.ReactToPropertyChanged(newElement, nameof(newElement.ImplementsICollidable), false);
            }

            if (viewModel.IncludeListsInScreens)
            {
                await IncludeListsFor(newElement);
            }


            if (viewModel.IsIDamageableChecked)
            {
                newElement.Properties.SetValue<bool>("ImplementsIDamageable", true);
                needsRefreshAndSave = true;
                await GlueCommands.Self.GluxCommands.ElementCommands.ReactToPropertyChanged(newElement, "ImplementsIDamageable", false);
            }
            if (viewModel.IsIDamageAreaChecked)
            {
                newElement.Properties.SetValue<bool>("ImplementsIDamageArea", true);
                needsRefreshAndSave = true;
                await GlueCommands.Self.GluxCommands.ElementCommands.ReactToPropertyChanged(newElement, "ImplementsIDamageArea", false);
            }

            if(viewModel.IsIDamageableChecked || viewModel.IsIDamageAreaChecked)
            {
                var variable = newElement.GetCustomVariable("TeamIndex");
                await GlueCommands.Self.GluxCommands.ElementCommands.HandleSetVariable(variable, viewModel.EffectiveTeamIndex);

                if(viewModel.IsOpposingTeamIndexDamageCollisionChecked)
                {
                    var gameScreen = ObjectFinder.Self.GetScreenSave("GameScreen");

                    if(gameScreen != null)
                    {
                        await AddGameScreenOpposingTeamIndexCollisionRelationships(newElement, viewModel);
                    }
                }
            }


        }

        // even derived entities can have factories
        if(viewModel.IsCreateFactoryChecked)
        {
            newElement.CreatedByOtherEntities = true;
            needsRefreshAndSave = true;
        }

        PluginManager.ReactToNewEntityCreated(newElement);

        GlueState.Self.CurrentElement = newElement;

        if(hasInheritance || needsRefreshAndSave)
        {
            GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(newElement);
        }

        if (needsRefreshAndSave)
        {
            GlueCommands.Self.DoOnUiThread(() =>
            {
                MainGlueWindow.Self.PropertyGrid.Refresh();

                if(hasInheritance)
                {
                    // if it has inheritance then the tree item should update to show the triangle:
                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(newElement);
                }
            });
            //var throwaway = GlueCommands.Self.GenerateCodeCommands.GenerateElementCodeAsync(newElement);
            // Bases need to be generated because they may now contain the Type 
            GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(ObjectFinder.Self.GetRootBaseElement( newElement ));

            if (newElement.CreatedByOtherEntities && viewModel.IncludeListsInScreens)
            {
                var allScreens = GlueState.Self.CurrentGlueProject.Screens;

                foreach (var screen in allScreens)
                {
                    var needsList = GetIfScreenNeedsList(screen);

                    if(needsList)
                    {
                        GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(screen);
                    }
                }
            }

            GluxCommands.Self.SaveProjectAndElements();
        }



        return newElement;
    }

    private static async Task IncludeListsFor(EntitySave newElement)
    {
        // loop through all screens that have a TMX object and add them.
        // be smart - if the base screen does, don't do it in the derived
        var allScreens = GlueState.Self.CurrentGlueProject.Screens;

        foreach (var screen in allScreens)
        {
            var needsList = GetIfScreenNeedsList(screen);

            if (needsList)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();

                addObjectViewModel.ForcedElementToAddTo = screen;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.PositionedObjectList;
                addObjectViewModel.SourceClassGenericType = newElement.Name;
                addObjectViewModel.ObjectName = $"{newElement.GetStrippedName()}List";


                var newNos = await GlueCommands.Self.GluxCommands.AddNewNamedObjectToAsync(
                    addObjectViewModel, screen, listToAddTo: null, selectNewNos: false);
                newNos.ExposedInDerived = true;

                await Container.Get<NamedObjectSetVariableLogic>().ReactToNamedObjectChangedValue(nameof(newNos.ExposedInDerived), false,
                    namedObjectSave: newNos);

                GlueCommands.Self.PrintOutput(
                    $"Tiled Plugin added {addObjectViewModel.ObjectName} to {screen}");

                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(screen);
            }
        }
    }

    private static bool GetIfScreenNeedsList(ScreenSave screen)
    {
        if (screen.Name.EndsWith("\\GameScreen"))
        {
            return true;
        }
        else
        {
            var hasTmx = GetIfScreenHasTmxDirectly(screen);

            //var doBaseScreensHaveTmx = GetIfBaseScreensHaveTmx(screen);

            var isDerived = string.IsNullOrEmpty(screen.BaseScreen) == false;



            return hasTmx == true && !isDerived;
        }
    }


    private static bool GetIfScreenHasTmxDirectly(ScreenSave screen)
    {
        var hasTmxFile = screen.ReferencedFiles.Any(item => FileManager.GetExtension(item.Name) == "tmx");
        var hasTmx = hasTmxFile;


        if (!hasTmx)
        {

            hasTmx = screen.AllNamedObjects.Any(item => item.GetAssetTypeInfo()?.FriendlyName == "LayeredTileMap (.tmx)");
        }
        return hasTmx;
    }



    private async Task AddGameScreenOpposingTeamIndexCollisionRelationships(EntitySave newElement, AddEntityViewModel viewModel)
    {
        var newTeamIndex = newElement.GetVariableValueRecursively("TeamIndex") as int?;
        var newElementName = newElement.Name;


        var gameScreen = ObjectFinder.Self.GetScreenSave("GameScreen");
        var newElementList = gameScreen.NamedObjects.FirstOrDefault(item => item.IsList && item.SourceClassGenericType == newElementName);
        ////////////////////////////Early Out///////////////////////////
        if (newElementList == null)
        {
            return;
        }
        /////////////////////////End Early Out//////////////////////////



        var pairs = GetGameScreenOpposingTeamIndexCollisionPairs(newTeamIndex, newElementList, viewModel);

        foreach(var pair in pairs)
        {
            var collisionRelationshipNos = await PluginManager.ReactToCreateCollisionRelationshipsBetween(pair.First, pair.Second);

            if (collisionRelationshipNos != null)
            {
                collisionRelationshipNos.SetProperty("IsDealDamageChecked", true);

                // These can cause objects to disapppear (like the player) unexpectedly. Let's
                // set these to false and let tutorials explain how to turn this on.
                //collisionRelationshipNos.SetProperty("IsDestroyFirstOnDamageChecked", true);
                //collisionRelationshipNos.SetProperty("IsDestroySecondOnDamageChecked", true);
            }
        }
    }


    public List<OrderedNamedObjectPair> GetGameScreenOpposingTeamIndexCollisionPairs(int? newTeamIndex, NamedObjectSave newElementList, AddEntityViewModel viewModel)
    {
        List<OrderedNamedObjectPair> pairs = new List<OrderedNamedObjectPair>();

        var isNewElementDamageable = viewModel.IsIDamageableChecked;
        var isNewElementDamageArea = viewModel.IsIDamageAreaChecked;


        var gameScreen = ObjectFinder.Self.GetScreenSave("GameScreen");

        /////////////////early out//////////////////
        if(gameScreen == null)
        {
            return pairs;
        }
        //////////////end early out///////////////////////

        var gameScreenNamedObjects = gameScreen.NamedObjects.ToArray();
        foreach (var item in gameScreenNamedObjects)
        {
            var isList = item.IsList;
            var genericTypeName = item.SourceClassGenericType;

            EntitySave entityForList = null;
            if (!string.IsNullOrEmpty(genericTypeName))
            {
                entityForList = ObjectFinder.Self.GetEntitySave(genericTypeName);
            }

            if (entityForList != null)
            {
                var entityForListIndex = entityForList?.GetVariableValueRecursively("TeamIndex") as int?;

                var isEntityDamageable = entityForList.GetPropertyValue("ImplementsIDamageable") as bool? ?? false;
                var isEntityDamageArea = entityForList.GetPropertyValue("ImplementsIDamageArea") as bool? ?? false;

                var isCollidable = entityForList.GetPropertyValue("ImplementsICollidable") as bool? ?? false;

                var areApposingDamageInterfaces =
                    (isEntityDamageable && isNewElementDamageArea) ||
                    (isEntityDamageArea && isNewElementDamageable);

                if (isCollidable && entityForListIndex != null && newTeamIndex != entityForListIndex && areApposingDamageInterfaces)
                {
                    // do it - add a collision relationship
                    // Damageable should be first since that's a standard we're pushing
                    if (isNewElementDamageable && isEntityDamageArea)
                    {
                        pairs.Add(new OrderedNamedObjectPair { First = newElementList, Second = item });
                    }
                    else
                    {
                        pairs.Add(new OrderedNamedObjectPair { First = item, Second = newElementList });
                    }
                }
            }
        }

        return pairs;
    }

    public void AddEntity(EntitySave entitySave, bool suppressAlreadyExistingFileMessage = false, bool notifyPluginsOfNewEntity = true)
    {

        entitySave.Tags.Add("GLUE");
        entitySave.Source = "GLUE";

        var glueProject = GlueState.Self.CurrentGlueProject;

        glueProject.Entities.Add(entitySave);

        glueProject.Entities.SortByName();

        var customCodeFilePath =
            GlueCommands.Self.FileCommands.GetCustomCodeFilePath(entitySave);
        #region Create the Entity custom code file (not the generated version)

        var newItem = GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(
            customCodeFilePath, false);

        var shouldRewriteCode = newItem != null;

        if (newItem == null)
        {
            if (!suppressAlreadyExistingFileMessage)
            {
                MessageBox.Show("There is already a file named\n\n" + customCodeFilePath + "\n\nThis file will be used instead of creating a new one just in case you have code that you want to keep there.");
            }
        }

        if(shouldRewriteCode)
        {
            GlueCommands.Self.GenerateCodeCommands.GenerateElementCustomCode(entitySave);
        }
        #endregion

        #region Create <EntityName>.Generated.cs

        var customCodePath = GlueCommands.Self.FileCommands.GetCustomCodeFilePath(entitySave);
        var directory = customCodePath.GetDirectoryContainingThis();

        string generatedFileName = FileManager.MakeRelative(directory.FullPath).Replace("/", "\\") + entitySave.ClassName + ".Generated.cs";

        ProjectManager.CodeProjectHelper.CreateAndAddPartialGeneratedCodeFile(generatedFileName, true);

        #endregion

        if(notifyPluginsOfNewEntity)
        {
            PluginManager.ReactToNewEntityCreated(entitySave);
        }

        GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(entitySave);

        GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(entitySave);

        GlueCommands.Self.ProjectCommands.SaveProjects();

        GluxCommands.Self.SaveProjectAndElements();
    }

    #endregion

    #region Add CustomVariable

    public void AddCustomVariableToCurrentElement(CustomVariable newVariable, bool save = true)
    {
        var element = GlueState.Self.CurrentElement;
        AddCustomVariableToElementImmediate(newVariable, element, save);
    }

    public async Task<GeneralResponse> AddStateCategoryCustomVariableToElementAsync(StateSaveCategory category, GlueElement element, bool save = true)
    {
        GeneralResponse response = new GeneralResponse();

        await TaskManager.Self.AddAsync(() =>
        {
            // This creates an exposed variable which must have a specific name:
            var requiredName = "Current" + category.Name + "State";
            // ...if there is already a variable with this name, we should not add it again
            if (element.GetCustomVariableRecursively(requiredName) != null)
            {
                response.Succeeded = false;
                response.Message = $"The variable {requiredName} already exists in {element}.";
            }
            else
            {
                // expose a variable that exposes the category
                CustomVariable customVariable = new CustomVariable();

                var categoryOwner = ObjectFinder.Self.GetElementContaining(category);

                var name = category.Name;

                // Update September 17, 2024
                // We want to fully-qualify the
                // type even if the new variable
                // is in the same element as the category.
                // This is required for live edit.
                //if(categoryOwner != null && categoryOwner != element)
                if(categoryOwner != null)
                {
                    name = categoryOwner.Name.Replace("\\", ".") + "." + name;
                }

                customVariable.Type = name;
                customVariable.Name = requiredName;
                customVariable.SetByDerived = true;

                AddCustomVariableToElementImmediate(
                    customVariable, element, save);
                response.Succeeded = true;
            }

        }, $"Adding category {category} as variable to {element}");
        return response;
    }

    void AddCustomVariableToElementImmediate(CustomVariable newVariable, GlueElement element, bool save = true)
    { 
        element.CustomVariables.Add(newVariable);

        // by default new variables should not be included in states. 
        foreach(var category in element.StateCategoryList)
        {
            if (!category.ExcludedVariables.Contains(newVariable.Name))
            { 
                category.ExcludedVariables.Add(newVariable.Name);
            }
        }

        InheritanceManager.UpdateAllDerivedElementFromBaseValues(regenerateCode:false, currentElement: element);

        CustomVariableHelper.SetDefaultValueFor(newVariable, element);

        GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element);

        GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();

        UpdateInstanceCustomVariables(element);

        PluginManager.ReactToVariableAdded(newVariable);

        // Generate code after PluginMangager.React so that the code can include any changes made by plugins.
        GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);

        if (GlueState.Self.CurrentElement == element)
        {
            // Vic asks = why do we call ReactToItemSelect instead of setting the custom variable. Is it to force a refresh?
            // On because actually people usually don't want to select the variable because it's rare to actually modify the variable
            // through its properties. Instead, it's more common to select the variables folder and use the variables tab
            //GlueState.Self.CurrentCustomVariable = newVariable;
            PluginManager.ReactToItemSelect(GlueState.Self.CurrentTreeNode);
        }

        if (save)
        {
            GluxCommands.Self.SaveProjectAndElements();
        }

    }

    public async Task AddCustomVariableToElementAsync(CustomVariable newVariable, GlueElement element, bool save = true)
    {
        await TaskManager.Self.AddAsync(() => AddCustomVariableToElementImmediate(newVariable, element, save),
            $"Adding variable {newVariable.Name} to {element}");
    }



    private void UpdateInstanceCustomVariables(IElement currentElement)
    {
        List<NamedObjectSave> namedObjectsToUpdate = null;

        if (currentElement is EntitySave)
        {
            namedObjectsToUpdate = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(currentElement.Name);
        }

        if (namedObjectsToUpdate != null)
        {
            foreach (NamedObjectSave nos in namedObjectsToUpdate)
            {
                nos.UpdateCustomProperties();
            }
        }
    }
    #endregion

    #region Set CustomVariable

    public async Task HandleSetVariable(CustomVariable variable, object value, bool performSaveAndGenerateCode = true,
        bool updateUi = true)
    {
        var element = ObjectFinder.Self.GetElementContaining(variable);
        var oldValue = variable.DefaultValue;

        variable.DefaultValue = value;

        await EditorObjects.IoC.Container.Get<CustomVariableSaveSetPropertyLogic>().ReactToCustomVariableChangedValue(
            "DefaultValue", variable, oldValue);


        if(performSaveAndGenerateCode)
        {

            if(element != null)
            {
                var throwaway = GlueCommands.Self.GluxCommands.SaveElementAsync(element);
                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
                if(element.BaseElement != null)
                {
                    var baseElement = ObjectFinder.Self.GetRootBaseElement(element);
                    if(baseElement != null)
                    {
                        // Generate the root base because it may have definitions for the types that have changed... 
                        GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(baseElement);
                    }
                }
            }
            else
            {
                GlueCommands.Self.GluxCommands.SaveProjectAndElements();
                GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
            }
        }

        if(updateUi)
        {
            GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();
            GlueCommands.Self.RefreshCommands.RefreshVariables();
        }

    }

    #endregion

    #region StateSaveCategory

    public async Task AddStateSaveCategoryAsync(string categoryName, GlueElement element)
    {
        await TaskManager.Self.AddAsync(() =>
        {
            var newCategory = new StateSaveCategory();
            newCategory.Name = categoryName;

            foreach (var variable in element.CustomVariables)
            {
                // new categories should have all variables excluded initially.
                newCategory.ExcludedVariables.Add(variable.Name);
            }

            element.StateCategoryList.Add(newCategory);

            List<NamedObjectSave> nosList = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(element.Name);
            List<EntitySave> derivedEntities = ObjectFinder.Self.GetAllEntitiesThatInheritFrom(element.Name);
            for (int i = 0; i < derivedEntities.Count; i++)
            {
                EntitySave entitySave = derivedEntities[i];

                nosList.AddRange(ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(entitySave.Name));
            }

            foreach (NamedObjectSave nos in nosList)
            {
                nos.UpdateCustomProperties();
            }

            GlueCommands.Self.RefreshCommands.RefreshCurrentElementTreeNode();
            GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();

            GlueState.Self.CurrentStateSaveCategory = newCategory;

            GluxCommands.Self.SaveProjectAndElements();
        }, nameof(AddStateSaveCategoryAsync));
    }

    public GeneralResponse CanVariableBeIncludedInStates(string variableName, GlueElement element)
    {
        var variable = element.GetCustomVariableRecursively(variableName);

        if(variable == null)
        {
            return GeneralResponse.UnsuccessfulWith("Variable does not exist, so it cannot be included");
        }
        else if(variable.SourceObject != null)
        {
            var rootVariable = ObjectFinder.Self.GetRootCustomVariable(variable);

            if(rootVariable?.Name == "Points")
            {
                // for now we assume this is for a polygon, and we don't allow it:
                return GeneralResponse.UnsuccessfulWith("Cannot include Points which is a list.");
            }
            else if(rootVariable?.Type.StartsWith("List<") == true)
            {
                return GeneralResponse.UnsuccessfulWith($"Variables of list types ({rootVariable?.Type}) cannot be included in states.");

            }
        }
        return GeneralResponse.SuccessfulResponse;
    }

    #endregion

    #region ReferencedFile

    [Obsolete("This function does way too much. Moving this to GluxCommands")]
    public ReferencedFileSave CreateReferencedFileSaveForExistingFile(GlueElement containerForFile, string directoryInsideContainer, string absoluteFileName,
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
                        string fileToAddAbsolute = GlueCommands.Self.GetAbsoluteFileName(fileToAdd, true);
                        CsvCodeGenerator.GenerateAndSaveDataClass(referencedFileSaveToReturn, referencedFileSaveToReturn.CsvDelimiter);
                    }
                    if (isUnknownType)
                    {
                        referencedFileSaveToReturn.LoadedAtRuntime = false;
                    }

                    GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(referencedFileSaveToReturn);

                    PluginManager.ReactToNewFile(referencedFileSaveToReturn, ati);
                    GluxCommands.Self.SaveProjectAndElements();
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
        var rfs = GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(absoluteSourceFileName);

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

    public static string GetFullPathContentDirectory(GlueElement element, string directoryRelativeToElement)
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

    #endregion

    #region Events

    public async Task AddEventToElement(AddEventViewModel viewModel, GlueElement glueElement)
    {

        string eventName = viewModel.EventName;

        string failureMessage;
        bool isInvalid = NameVerifier.IsEventNameValid(eventName,
            glueElement, out failureMessage);

        if (isInvalid)
        {
            GlueCommands.Self.DialogCommands.ShowMessageBox(failureMessage);
        }
        else if (!isInvalid)
        {
            await TaskManager.Self.AddAsync(() =>
            {
                EventResponseSave eventResponseSave = new EventResponseSave();
                eventResponseSave.EventName = eventName;

                eventResponseSave.SourceObject = viewModel.TunnelingObject;
                eventResponseSave.SourceObjectEvent = viewModel.TunnelingEvent;

                eventResponseSave.SourceVariable = viewModel.SourceVariable;
                eventResponseSave.BeforeOrAfter = viewModel.BeforeOrAfter;

                eventResponseSave.DelegateType = viewModel.DelegateType;

                AddEventToElement(glueElement, eventResponseSave);
            }, $"Adding element {viewModel.EventName}");
        }
    }

    public void AddEventToElement(GlueElement currentElement, EventResponseSave eventResponseSave)
    {
        currentElement.Events.Add(eventResponseSave);

        string fullGeneratedFileName = ProjectManager.ProjectBase.Directory + EventManager.GetGeneratedEventFileNameForElement(currentElement);

        if (!File.Exists(fullGeneratedFileName))
        {
            CodeWriter.AddEventGeneratedCodeFileForElement(currentElement);
        }

        GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(currentElement);

        GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(currentElement);

        _=GluxCommands.Self.SaveElementAsync(currentElement);

        GlueState.Self.CurrentEventResponseSave = eventResponseSave;
    }
    #endregion

    #region Property Set

    public Task ReactToPropertyChanged(GlueElement element, string propertyName, object oldValue)
    {
        if(element is EntitySave entitySave)
        {
            Container.Get<EntitySaveSetPropertyLogic>().ReactToEntityChangedProperty(propertyName, oldValue, entitySave);
        }
        else if(element is ScreenSave screenSave)
        {
            Container.Get<ScreenSaveSetVariableLogic>().ReactToScreenPropertyChanged(screenSave, propertyName, oldValue);
        }
        return Task.CompletedTask;
    }

    #endregion

    #region Inheritance

    /// <summary>
    /// Updates the argument glueElement from its base types. This updates variables and named objects. This is called whenever an element's base type changes,
    /// which can result in the element having new variables and named objects (automatically inherited), or existing variables and named objects being modified
    /// (such as being marked as instantiated by base).
    /// </summary>
    /// <param name="glueElement">The base Glue element to update.</param>
    /// <returns>Whether the object updated</returns>
    public bool UpdateFromBaseType(GlueElement glueElement, bool showPopupAboutObjectErrors = true)
    {
        bool haveChangesOccurred = false;
        if (ObjectFinder.Self.GlueProject != null)
        {
            haveChangesOccurred |= UpdateNamedObjectsFromBaseType(glueElement, showPopupAboutObjectErrors);

            UpdateCustomVariablesFromBaseType(glueElement);
        }
        return haveChangesOccurred;
    }

    private void UpdateCustomVariablesFromBaseType(GlueElement elementToUpdate)
    {
        GlueElement baseElement = ObjectFinder.Self.GetBaseElement(elementToUpdate);

        var customVariablesBeforeUpdate = new List<CustomVariable>();

        for (int i = 0; i < elementToUpdate.CustomVariables.Count; i++)
        {
            if (elementToUpdate.CustomVariables[i].DefinedByBase)
            {
                customVariablesBeforeUpdate.Add(elementToUpdate.CustomVariables[i]);
            }
        }

        List<CustomVariable> newCustomVariables = null;

        //EntitySave entity = ProjectManager.GetEntitySave(mBaseEntity);

        if (baseElement != null)
        {
            newCustomVariables = baseElement.GetCustomVariablesToBeSetByDerived();
        }
        else
        {
            newCustomVariables = new List<CustomVariable>();
        }

        // See if there are any variables to be removed.
        for (int i = customVariablesBeforeUpdate.Count - 1; i > -1; i--)
        {
            bool contains = false;

            for (int j = 0; j < newCustomVariables.Count; j++)
            {
                if (customVariablesBeforeUpdate[i].Name == newCustomVariables[j].Name &&
                    customVariablesBeforeUpdate[i].DefinedByBase)
                {
                    contains = true;
                    break;
                }
            }

            if (!contains)
            {
                // We got a NamedObject we should remove
                elementToUpdate.CustomVariables.Remove(customVariablesBeforeUpdate[i]);
                customVariablesBeforeUpdate.RemoveAt(i);
            }
        }

        // Next, see if there are any objects to be added
        for (int i = 0; i < newCustomVariables.Count; i++)
        {
            bool alreadyContainedAsDefinedByBase = false;
            for (int j = 0; j < customVariablesBeforeUpdate.Count; j++)
            {
                if (customVariablesBeforeUpdate[j].Name == newCustomVariables[i].Name &&
                    customVariablesBeforeUpdate[j].DefinedByBase)
                {
                    alreadyContainedAsDefinedByBase = true;
                }
            }

            if (!alreadyContainedAsDefinedByBase)
            {
                // There isn't a variable by this
                // name that is already DefinedByBase, 
                // but there may still be a variable that
                // is just a regular variable - and in that
                // case we want to connect the existing variable
                // with the variable in the base
                CustomVariable existingInDerived = elementToUpdate.GetCustomVariable(newCustomVariables[i].Name);
                if (existingInDerived != null)
                {
                    existingInDerived.DefinedByBase = true;
                }
                else
                {
                    CustomVariable customVariable = newCustomVariables[i].Clone();

                    // March 4, 2012
                    // We used to not
                    // change the SourceObject
                    // or the SourceObjectProperty
                    // values; however, this new variable
                    // should behave like an exposed variable,
                    // so it shouldn't have any SourceObject or
                    // SourceObjectProperty.  If it did, then it
                    // will access the base NamedObjectSave to set
                    // its property, and this could cause compilation
                    // errors if the NOS in the base is marked as private.
                    // It may also avoid raising events defined in the base.
                    customVariable.SourceObject = null;
                    customVariable.SourceObjectProperty = null;

                    customVariable.DefinedByBase = true;
                    // We'll assume that this thing is going to be the acutal definition
                    // Update April 11, 2023
                    // Setting this to false doesn't
                    // prevent this from being the final
                    // definition. We want the SetByDerived
                    // to be true so that the derived variable
                    // can get removed when saving the .json file.
                    //customVariable.SetByDerived = false;
                    customVariable.SetByDerived = true;

                    var indexToInsertAt = elementToUpdate.CustomVariables.Count;
                    if (i == 0)
                    {
                        indexToInsertAt = 0;
                    }
                    else
                    {
                        var itemBefore = newCustomVariables[i - 1];
                        var withMatchingName = elementToUpdate.CustomVariables.Find(item => item.Name == itemBefore.Name);
                        if (withMatchingName != null)
                        {
                            indexToInsertAt = 1 + elementToUpdate.CustomVariables.IndexOf(withMatchingName);
                        }
                    }

                    elementToUpdate.CustomVariables.Insert(indexToInsertAt, customVariable);
                }
            }
        }

        // update the category
        foreach (var customVariable in elementToUpdate.CustomVariables)
        {
            if (customVariable.DefinedByBase)
            {
                var baseVariable = baseElement.CustomVariables.FirstOrDefault(item => item.Name == customVariable.Name);
                if (!string.IsNullOrEmpty(baseVariable?.Category))
                {
                    customVariable.Category = baseVariable.Category;
                }
            }
        }
    }

    /// <summary>
    /// This method is called whenever the derivedGlueElement has its base type changed, resulting in
    /// inheritance-based properties on the NamedObjects needing to be updated.
    /// </summary>
    /// <param name="derivedGlueElement">The derived element which conains named objects which should be upated.</param>
    /// <param name="showPopupAboutObjectErrors">Whether to show popups on errors. This should be true if this is called in response to a UI action.</param>
    /// <returns>Whether any changes have happened on the NamedObjects, which means a save is needed.</returns>
    private bool UpdateNamedObjectsFromBaseType(GlueElement derivedGlueElement, bool showPopupAboutObjectErrors)
    {
        bool haveChangesOccurred = false;

        List<NamedObjectSave> referencedObjectsBeforeUpdate = derivedGlueElement.AllNamedObjects.Where(item => item.DefinedByBase).ToList();

        List<NamedObjectSave> namedObjectsInBaseSetByDerived = new List<NamedObjectSave>();
        List<NamedObjectSave> namedObjectsExposedInDerived = new List<NamedObjectSave>();

        List<INamedObjectContainer> baseElements = new List<INamedObjectContainer>();

        // July 24, 2011
        // Before today, this
        // code would loop through
        // all base Entities and search
        // for SetByDerived properties in
        // any NamedObjectSave. This caused
        // bugs.  Basically if you had 3 Elements
        // in an inheritance chain and the one at the
        // very base defined a NOS to be SetByDerived, then
        // anything that inherited directly from the base should
        // be forced to define it.  If it does, then the 3rd Element
        // in the inheritance chain shouldn't have to define it, but before
        // today it did.  This caused a lot of problems including generated
        // code creating the element twice.
        if (derivedGlueElement is EntitySave)
        {
            if (!string.IsNullOrEmpty(derivedGlueElement.BaseObject))
            {
                baseElements.Add(ObjectFinder.Self.GetElement(derivedGlueElement.BaseObject));
            }
            //List<EntitySave> allBase = ((EntitySave)namedObjectContainer).GetAllBaseEntities();
            //foreach (EntitySave baseEntitySave in allBase)
            //{
            //    baseElements.Add(baseEntitySave);
            //}
        }
        else
        {
            if (!string.IsNullOrEmpty(derivedGlueElement.BaseObject))
            {
                baseElements.Add(ObjectFinder.Self.GetElement(derivedGlueElement.BaseObject));
            }
            //List<ScreenSave> allBase = ((ScreenSave)namedObjectContainer).GetAllBaseScreens();
            //foreach (ScreenSave baseScreenSave in allBase)
            //{
            //    baseElements.Add(baseScreenSave);
            //}
        }


        foreach (INamedObjectContainer baseNamedObjectContainer in baseElements)
        {

            if (baseNamedObjectContainer != null)
            {
                namedObjectsInBaseSetByDerived.AddRange(baseNamedObjectContainer.GetNamedObjectsToBeSetByDerived());
                namedObjectsExposedInDerived.AddRange(baseNamedObjectContainer.GetNamedObjectsToBeExposedInDerived());
            }
        }


        var derivedNosesToAskAbout = new List<NamedObjectSave>();


        for (int i = referencedObjectsBeforeUpdate.Count - 1; i > -1; i--)
        {
            var atI = referencedObjectsBeforeUpdate[i];

            var contains = atI.DefinedByBase && namedObjectsInBaseSetByDerived.Any(item => item.InstanceName == atI.InstanceName);

            if (!contains)
            {
                contains = namedObjectsExposedInDerived.Any(item => item.InstanceName == atI.InstanceName);
            }

            if (!contains)
            {

                NamedObjectSave nos = referencedObjectsBeforeUpdate[i];

                derivedNosesToAskAbout.Add(nos);
            }
        }


        #region See if there are any objects to be removed from the derived.
        if (derivedNosesToAskAbout.Count > 0 && showPopupAboutObjectErrors)
        {
            // This can happen whenever, like if a project is reloaded and data is changed on disk. However, this
            // code should never hit as a result of the user making changes in the UI, that should be caught earlier.
            // See SetByDerivedSetLogic
            var singleOrPluralPhrase = derivedNosesToAskAbout.Count == 1 ? "object is" : "objects are";
            var thisOrTheseObjects = derivedNosesToAskAbout.Count == 1 ? "this object" : "these objects";


            string message = "The following object is marked as \"defined by base\" but not contained in " +
                "any base elements\n\n";

            foreach (var nos in derivedNosesToAskAbout)
            {
                message += nos.ToString() + "\n";
            }

            message += "\nWhat would you like to do?";

            var mbmb = new MultiButtonMessageBoxWpf();

            mbmb.MessageText = message;

            mbmb.AddButton($"Remove {thisOrTheseObjects}", DialogResult.Yes);
            mbmb.AddButton($"Keep {thisOrTheseObjects}, set \"defined by base\" to false", DialogResult.No);

            var dialogResult = mbmb.ShowDialog();

            if (dialogResult == true && (DialogResult)mbmb.ClickedResult == DialogResult.Yes)
            {
                foreach (var nos in derivedNosesToAskAbout)
                {
                    if (derivedGlueElement.NamedObjects.Contains(nos))
                    {
                        derivedGlueElement.NamedObjects.Remove(nos);
                    }
                    else
                    {
                        derivedGlueElement.NamedObjects
                            .FirstOrDefault(item => item.ContainedObjects.Contains(nos))
                            ?.ContainedObjects.Remove(nos);
                    }
                    // We got a NamedObject we should remove
                    referencedObjectsBeforeUpdate.Remove(nos);
                }
            }
            else if (mbmb.ClickedResult is DialogResult clickedDialogResult && clickedDialogResult == DialogResult.No)
            {
                foreach (var nos in derivedNosesToAskAbout)
                {
                    nos.DefinedByBase = false;
                    nos.InstantiatedByBase = false;
                }
            }
            haveChangesOccurred = true;
        }
        #endregion

        #region Next, see if there are any objects to be added
        for (int i = 0; i < namedObjectsInBaseSetByDerived.Count; i++)
        {
            NamedObjectSave namedObjectInBase = namedObjectsInBaseSetByDerived[i];

            NamedObjectSave matchingDefinedByBase = null;// contains = false;
            for (int j = 0; j < referencedObjectsBeforeUpdate.Count; j++)
            {
                if (referencedObjectsBeforeUpdate[j].InstanceName == namedObjectInBase.InstanceName &&
                    referencedObjectsBeforeUpdate[j].DefinedByBase)
                {
                    matchingDefinedByBase = referencedObjectsBeforeUpdate[j];
                    break;
                }
            }

            if (matchingDefinedByBase == null)
            {
                AddSetByDerivedNos(derivedGlueElement, namedObjectInBase, false);
            }
            else
            {
                MatchDerivedToBase(namedObjectInBase, matchingDefinedByBase);
            }
        }

        for (int i = 0; i < namedObjectsExposedInDerived.Count; i++)
        {
            NamedObjectSave nosInBase = namedObjectsExposedInDerived[i];

            NamedObjectSave nosInDerived = referencedObjectsBeforeUpdate
                .FirstOrDefault(item => item.InstanceName == nosInBase.InstanceName && item.DefinedByBase);


            if (nosInDerived == null)
            {
                nosInDerived = AddSetByDerivedNos(derivedGlueElement, nosInBase, true);
            }
            else
            {
                MatchDerivedToBase(nosInBase, nosInDerived);
            }

            foreach (var containedInBaseNos in nosInBase.ContainedObjects)
            {
                if (containedInBaseNos.ExposedInDerived)
                {
                    var containedInDerived = referencedObjectsBeforeUpdate
                        .FirstOrDefault(item => item.InstanceName == containedInBaseNos.InstanceName && item.DefinedByBase);
                    if (containedInDerived == null)
                    {
                        AddSetByDerivedNos(derivedGlueElement, containedInBaseNos, true, nosInDerived);
                    }
                    else
                    {
                        MatchDerivedToBase(containedInBaseNos, containedInDerived);
                    }
                }
            }
        }

        #endregion

        return haveChangesOccurred;
    }

    private static void MatchDerivedToBase(NamedObjectSave inBase, NamedObjectSave inDerived)
    {
        inDerived.SourceClassGenericType = inBase.SourceClassGenericType;
    }

    private static NamedObjectSave AddSetByDerivedNos(INamedObjectContainer derivedContainer,
        NamedObjectSave namedObjectInBase, bool instantiatedByBase, NamedObjectSave containerToAddTo = null)
    {
        NamedObjectSave existingNamedObject = derivedContainer.AllNamedObjects
            .FirstOrDefault(item => item.InstanceName == namedObjectInBase.InstanceName);

        if (existingNamedObject != null)
        {
            existingNamedObject.DefinedByBase = true;
            existingNamedObject.InstantiatedByBase = instantiatedByBase;
            return existingNamedObject;
        }
        else
        {
            NamedObjectSave newNamedObject = namedObjectInBase.Clone();

            // This code may be cloning a list with contained objects, and the
            // contained objects may not SetByDerived
            newNamedObject.ContainedObjects.Clear();
            foreach (var containedCandidate in namedObjectInBase.ContainedObjects)
            {
                if (containedCandidate.SetByDerived)
                {
                    newNamedObject.ContainedObjects.Add(containedCandidate);
                }
            }

            // For more information on this property, see GlueProjectSaveExtensions.DetermineIfShouldStripNos
            newNamedObject.SetDefinedByBaseRecursively(true);
            newNamedObject.SetInstantiatedByBaseRecursively(instantiatedByBase);

            // This can't be set by derived because an object it inherits from has that already set
            newNamedObject.SetSetByDerivedRecursively(false);

            if (containerToAddTo == null)
            {
                var indexToAddAt = derivedContainer.NamedObjects.Count;

                var baseContainer = ObjectFinder.Self.GetElementContaining(namedObjectInBase);
                if(baseContainer != null)
                {
                    var indexOfNosInBase = baseContainer.NamedObjects.IndexOf(namedObjectInBase);
                    if(indexOfNosInBase == -1)
                    {
                        var container = baseContainer.NamedObjects.FirstOrDefault(item => 
                            item.ContainedObjects.Contains(namedObjectInBase));
                        // todo - this is rare, but we need to find what index this is after in the base, and make this get added
                        // after in the derived:
                    }
                    else
                    {
                        for(int i = indexOfNosInBase - 1; i > -1; i--)
                        {
                            var itemInBase = baseContainer.NamedObjects[i];
                            var name = itemInBase.InstanceName;

                            var foundMatchInDerived = derivedContainer.NamedObjects.Find(item => item.InstanceName ==  name);
                            if(foundMatchInDerived != null)
                            {
                                indexToAddAt = derivedContainer.NamedObjects.IndexOf(foundMatchInDerived) + 1;
                                break;
                            }
                        }
                    }
                }

                derivedContainer.NamedObjects.Insert(indexToAddAt, newNamedObject);
            }
            else
            {
                containerToAddTo.ContainedObjects.Add(newNamedObject);
            }

            return newNamedObject;
        }
    }


    #endregion

}
