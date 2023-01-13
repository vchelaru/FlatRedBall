using System.Windows.Forms;
using System.Linq;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using Glue;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.IO;
using FlatRedBall.Glue.FormHelpers;
using System;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.ViewModels;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Utilities;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using GlueFormsCore.ViewModels;
using GlueFormsCore.Controls;
using FlatRedBall.Glue.VSHelpers;
using GlueFormsCore.Extensions;
using System.Runtime.InteropServices;
using FlatRedBall.Glue.IO;
using System.Threading.Tasks;


namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    class DialogCommands : IDialogCommands
    {
        #region Project

        public async void ShowLoadProjectDialog()
        {
            if (ProjectManager.StatusCheck() == ProjectManager.CheckResult.Passed)
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog();

                openFileDialog1.InitialDirectory = "c:\\";
                openFileDialog1.Filter = "Project/Solution files (*.vcproj;*.csproj;*.sln;*.glux;*.gluj)|*.vcproj;*.csproj;*.sln;*.glux;*.gluj";
                openFileDialog1.FilterIndex = 1;
                openFileDialog1.RestoreDirectory = true;

                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string projectFileName = openFileDialog1.FileName;

                    var extension = FileManager.GetExtension(projectFileName);
                    if (extension == "sln")
                    {
                        var solution = VSSolution.FromFile(projectFileName);

                        string solutionName = projectFileName;

                        projectFileName = solution.ReferencedProjects.FirstOrDefault(item =>
                        {
                            var isRegularProject = FileManager.GetExtension(item) == "csproj" || FileManager.GetExtension(item) == "vsproj";

                            bool hasSameName = FileManager.RemovePath(FileManager.RemoveExtension(solutionName)).ToLowerInvariant() ==
                                FileManager.RemovePath(FileManager.RemoveExtension(item)).ToLowerInvariant();


                            return isRegularProject && hasSameName;
                        });

                        projectFileName = FileManager.GetDirectory(solutionName) + projectFileName;
                    }
                    else if(extension == "gluj")
                    {
                        projectFileName = FileManager.RemoveExtension(projectFileName) + ".csproj";
                    }

                    try
                    {
                        await GlueCommands.Self.LoadProjectAsync(projectFileName);
                    }
                    catch(Exception e)
                    {
                        GlueCommands.Self.DialogCommands.ShowMessageBox($"Attempted to open\n\n{projectFileName}\n\nbut failed:\n{e}");
                    }

                    // not sure why we need to do this....
                    //SaveSettings();
                }
            }
        }

        #endregion

        #region NamedObjectSave

        public async Task<NamedObjectSave> ShowAddNewObjectDialog(AddObjectViewModel addObjectViewModel = null)
        {
            NamedObjectSave newNamedObject = null;

            // add named object, add object, addnamedobject, add new object, addnewobject, createobject, addobject

            var shouldAdd = CreateAndShowAddNamedObjectWindow(ref addObjectViewModel);

            if (shouldAdd == true)
            {
                bool isValid = NameVerifier.IsNamedObjectNameValid(addObjectViewModel.ObjectName, out string whyItIsntValid);

                if (isValid)
                {
                    if (addObjectViewModel.SourceType == SourceType.Entity && !RecursionManager.Self.CanContainInstanceOf(GlueState.Self.CurrentElement, addObjectViewModel.SourceClassType))
                    {
                        isValid = false;
                        whyItIsntValid = "This type would result in infinite recursion";
                    }
                }

                if (isValid)
                {
                    newNamedObject = await GlueCommands.Self.GluxCommands.AddNewNamedObjectToSelectedElementAsync(addObjectViewModel);
                    GlueState.Self.CurrentNamedObjectSave = newNamedObject;
                }
                else
                {
                    GlueGui.ShowMessageBox(whyItIsntValid);
                }
            }

            return newNamedObject;
        }

        /// <summary>
        /// Creates and shows the new NamedObject dialog. 
        /// </summary>
        /// <param name="addObjectViewModel"></param>
        /// <returns>The WPF dialog result for showing the window.</returns>
        private static bool? CreateAndShowAddNamedObjectWindow(ref AddObjectViewModel addObjectViewModel)
        {
            var currentObject = GlueState.Self.CurrentNamedObjectSave;

            bool isTypePredetermined = (currentObject != null && currentObject.IsList) ||
                addObjectViewModel?.IsTypePredetermined == true;

            var isNewWindow = false;
            if (addObjectViewModel == null)
            {
                addObjectViewModel = new AddObjectViewModel();
                isNewWindow = true;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
            }

            var parentNamedObject = GlueState.Self.CurrentNamedObjectSave;
            if (parentNamedObject?.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.ShapeCollection)
            {
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                addObjectViewModel.IsObjectTypeRadioButtonPredetermined = true;
                addObjectViewModel.FlatRedBallAndCustomTypes.Clear();
                addObjectViewModel.FlatRedBallAndCustomTypes.Add(AvailableAssetTypes.CommonAtis.AxisAlignedRectangle);
                addObjectViewModel.FlatRedBallAndCustomTypes.Add(AvailableAssetTypes.CommonAtis.Circle);
                addObjectViewModel.FlatRedBallAndCustomTypes.Add(AvailableAssetTypes.CommonAtis.Polygon);
            }
            else if (isTypePredetermined && addObjectViewModel.SelectedAti != null)
            {
                addObjectViewModel.FlatRedBallAndCustomTypes.Add(addObjectViewModel.SelectedAti);
            }
            else
            {
                var addableTypes = AvailableAssetTypes.Self.AllAssetTypes
                    .Where(item => item.CanBeObject)
                    .OrderBy(item => item.FriendlyName)
                    .ToArray();

                var gumTypes = addableTypes.Where(item => ((bool)PluginManager.CallPluginMethod("Gum Plugin", "IsAssetTypeInfoGum", item)) == true)
                    .ToArray();
                var flatRedBallTypes = addableTypes.Except(gumTypes).ToArray();

                addObjectViewModel.FlatRedBallAndCustomTypes.AddRange(flatRedBallTypes);

                // for new objects, don't allow screens
                var filteredGumTypes = gumTypes
                    .Where(item => item.Tag is Gum.DataTypes.ScreenSave == false);

                addObjectViewModel.GumTypes.AddRange(filteredGumTypes);
            }
            addObjectViewModel.AvailableEntities =
                ObjectFinder.Self.GlueProject.Entities.ToList();

            addObjectViewModel.AvailableFiles =
                GlueState.Self.CurrentElement.ReferencedFiles.ToList();

            if (addObjectViewModel.SelectedItem != null)
            {

                var backingObject = addObjectViewModel.SelectedItem.BackingObject;

                // refresh the lists before trying to assing the object so the VM can select from the internal list, but do it
                // after grabbing the backingObject.

                addObjectViewModel.ForceRefreshToSourceType();
                // re-assign the backing object so it uses the current set of wrappers:
                if (backingObject is EntitySave backingEntitySave)
                {
                    addObjectViewModel.SelectedEntitySave = backingEntitySave;
                }
                else if (backingObject is AssetTypeInfo backingAti)
                {
                    addObjectViewModel.SelectedAti = backingAti;
                }
                else if (backingObject is ReferencedFileSave backingFile)
                {
                    addObjectViewModel.SourceFile = backingFile;
                }
            }
            if (isNewWindow)
            {
                addObjectViewModel.ForceRefreshToSourceType();
            }
            AvailableClassGenericTypeConverter converter = new AvailableClassGenericTypeConverter();

            var availableTypes = converter.GetAvailableValues(false);
            addObjectViewModel.AvailableListTypes.AddRange(availableTypes);

            addObjectViewModel.IsTypePredetermined = isTypePredetermined;


            if (isTypePredetermined)
            {

                var genericType = parentNamedObject?.SourceClassGenericType;

                if (!string.IsNullOrEmpty(genericType))
                {
                    var selectedAti =
                        AvailableAssetTypes.Self.AllAssetTypes.FirstOrDefault(item =>
                            item.FriendlyName == genericType || item.QualifiedRuntimeTypeName.QualifiedType == genericType);
                    addObjectViewModel.SelectedAti =
                        selectedAti;

                    var genericEntityType =
                        ObjectFinder.Self.GetEntitySave(genericType);
                    if (genericEntityType != null)
                    {
                        addObjectViewModel.SourceType = SourceType.Entity;
                        addObjectViewModel.SelectedEntitySave = genericEntityType;

                        // filter down all entities to anything that is of this type or inherits from this type:
                        var derived = ObjectFinder.Self.GetAllDerivedElementsRecursive(genericEntityType)
                            .Select(item => item as EntitySave);

                        addObjectViewModel.AvailableEntities.Clear();
                        addObjectViewModel.AvailableEntities.Add(genericEntityType);
                        addObjectViewModel.AvailableEntities.AddRange(derived);

                        addObjectViewModel.IsTypePredetermined = addObjectViewModel.AvailableEntities.Count < 2;
                        addObjectViewModel.IsObjectTypeRadioButtonPredetermined = true;

                        addObjectViewModel.RefreshAllSelectedItems();
                        addObjectViewModel.RefreshFilteredItems();
                    }
                    else
                    {
                        addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                        if (selectedAti != null)
                        {
                            addObjectViewModel.FlatRedBallAndCustomTypes.Clear();
                            addObjectViewModel.FlatRedBallAndCustomTypes.Add(selectedAti);
                            // re-select since clearing the list will deselect
                            addObjectViewModel.SelectedAti = selectedAti;
                            addObjectViewModel.IsObjectTypeRadioButtonPredetermined = true;

                            addObjectViewModel.RefreshAllSelectedItems();
                            addObjectViewModel.RefreshFilteredItems();
                        }
                    }
                }
            }

            var wpf = new NewObjectTypeSelectionControlWpf();
            wpf.DataContext = addObjectViewModel;
            var dialogResult = wpf.ShowDialog();
            return dialogResult;
        }

        public void AskToRemoveObject(NamedObjectSave namedObjectToRemove, bool saveAndRegenerate = true)
        {
            // Search terms: removefromproject, remove from project, remove file, remove referencedfilesave
            List<string> filesToRemove = new List<string>();

            DialogResult reallyRemoveResult = DialogResult.Yes;

            var canDelete = true;

            if (namedObjectToRemove.DefinedByBase)
            {
                var definingNos = ObjectFinder.Self.GetRootDefiningObject(namedObjectToRemove);

                // December 31, 2022
                // It's possible to set
                // ExposedInDerived and SetByDerived
                // indepdently, but conceptually SetByDerived
                // requires the property to be protected so it
                // can be set. SetByDerived is a "stronger" form
                // of ExposedInDerived, so we still want to keep the
                // relationship in tact.
                if (definingNos?.ExposedInDerived == true || definingNos?.SetByDerived == true)
                {
                    var message = $"The object {namedObjectToRemove} cannot be deleted because a base object has it marked as ExposedInDerived or SetByDerived";

                    GlueCommands.Self.DialogCommands.ShowMessageBox(message);

                    canDelete = false;
                }
            }

            if (canDelete)
            {
                var askAreYouSure = true;

                if (askAreYouSure)
                {
                    var window = new RemoveObjectWindow();
                    var viewModel = new RemoveObjectViewModel();
                    viewModel.SetFrom(namedObjectToRemove);
                    var owner = ObjectFinder.Self.GetElementContaining(namedObjectToRemove);
                    if (owner == null)
                    { System.Diagnostics.Debugger.Break(); }
                    if (owner != null)
                    {
                        var objectsToRemove = GluxCommands.GetObjectsToRemoveIfRemoving(namedObjectToRemove, owner);

                        viewModel.ObjectsToRemove.AddRange(objectsToRemove.CustomVariables.Select(item => item.ToString()));
                        viewModel.ObjectsToRemove.AddRange(objectsToRemove.SubObjectsInList.Select(item => item.ToString()));
                        viewModel.ObjectsToRemove.AddRange(objectsToRemove.CollisionRelationships.Select(item => item.ToString()));
                        viewModel.ObjectsToRemove.AddRange(objectsToRemove.DerivedNamedObjects.Select(item => item.ToString()));
                        viewModel.ObjectsToRemove.AddRange(objectsToRemove.EventResponses.Select(item => item.ToString()));

                        window.DataContext = viewModel;

                        var showDialogResult = window.ShowDialog();

                        reallyRemoveResult = showDialogResult == true ? DialogResult.Yes : DialogResult.No;
                    }


                    //string message = "Are you sure you want to remove this:\n\n" + namedObjectToRemove.ToString();

                    //reallyRemoveResult =
                    //    MessageBox.Show(message, "Remove?", MessageBoxButtons.YesNo);
                }
            }




            if (canDelete && reallyRemoveResult == DialogResult.Yes)
            {
                GlueCommands.Self.GluxCommands
                    .RemoveNamedObject(namedObjectToRemove, true, true, filesToRemove);

                if (filesToRemove.Count != 0 && true /*askToDeleteFiles*/)
                {

                    for (int i = 0; i < filesToRemove.Count; i++)
                    {
                        if (FileManager.IsRelative(filesToRemove[i]))
                        {
                            filesToRemove[i] = GlueCommands.Self.GetAbsoluteFileName(filesToRemove[i], false);
                        }
                        filesToRemove[i] = filesToRemove[i].Replace("\\", "/");
                    }

                    StringFunctions.RemoveDuplicates(filesToRemove, true);

                    var lbw = new ListBoxWindowWpf();

                    string messageString = "What would you like to do with the following files:\n";
                    lbw.Message = messageString;

                    foreach (string s in filesToRemove)
                    {

                        lbw.AddItem(s);
                    }
                    lbw.ClearButtons();
                    lbw.AddButton("Nothing - leave them as part of the game project", DialogResult.No);
                    lbw.AddButton("Remove them from the project but keep the files", DialogResult.OK);
                    lbw.AddButton("Remove and delete the files", DialogResult.Yes);

                    var dialogShowResult = lbw.ShowDialog();
                    DialogResult result = (DialogResult)lbw.ClickedOption;

                    if (result == DialogResult.OK || result == DialogResult.Yes)
                    {
                        foreach (string file in filesToRemove)
                        {
                            FilePath fileName = GlueCommands.Self.GetAbsoluteFileName(file, false);
                            // This file may have been removed
                            // in windows explorer, and now removed
                            // from Glue.  Check to prevent a crash.

                            GlueCommands.Self.ProjectCommands.RemoveFromProjects(fileName, false);
                        }
                    }

                    if (result == DialogResult.Yes)
                    {
                        foreach (string file in filesToRemove)
                        {
                            string fileName = GlueCommands.Self.GetAbsoluteFileName(file, false);
                            // This file may have been removed
                            // in windows explorer, and now removed
                            // from Glue.  Check to prevent a crash.
                            if (System.IO.File.Exists(fileName))
                            {
                                FileHelper.MoveToRecycleBin(fileName);
                            }
                        }
                    }
                }

                TaskManager.Self.AddOrRunIfTasked(() =>
                {
                    var glueState = GlueState.Self;

                    // Nodes aren't directly removed in the code above. Instead, 
                    // a "refresh nodes" method is called, which may remove unneeded
                    // nodes, but event raising is suppressed. Therefore, we have to explicitly 
                    // do it here:
                    if (glueState.CurrentElement != null)
                    {
                        GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(glueState.CurrentElement);
                    }
                    else
                    {
                        GlueCommands.Self.RefreshCommands.RefreshGlobalContent();
                    }
                }, "Refreshing tree nodes");


                if (saveAndRegenerate)
                {
                    if (GlueState.Self.CurrentElement != null)
                    {
                        GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(GlueState.Self.CurrentElement);
                    }
                    else //if (GlueState.Self.CurrentReferencedFileSave != null)
                    {
                        GlueCommands.Self.GenerateCodeCommands.GenerateGlobalContentCodeTask();

                        // Vic asks - do we have to do anything else here?  I don't think so...
                    }


                    GluxCommands.Self.ProjectCommands.SaveProjects();
                    GluxCommands.Self.SaveGlux();
                }
            }
        }


        #endregion

        #region ReferencedFileSave

        public async Task<ReferencedFileSave> ShowAddNewFileDialogAsync(AddNewFileViewModel viewModel = null, GlueElement element = null)
        {
            ReferencedFileSave rfs = null;

            if (viewModel == null)
            {
                viewModel = new AddNewFileViewModel();
            }
            var nfw = CreateNewFileWindow(viewModel);

            if (viewModel.SelectedAssetTypeInfo == null)
            {
                viewModel.SelectedAssetTypeInfo = viewModel.FilteredOptions.FirstOrDefault();
            }

            var result = nfw.ShowDialog();

            if (result == true)
            {
                AssetTypeInfo resultAssetTypeInfo =
                    viewModel.SelectedAssetTypeInfo;

                var option = nfw.GetOptionFor(resultAssetTypeInfo);
                rfs = await GlueCommands.Self.GluxCommands.CreateNewFileAndReferencedFileSaveAsync(viewModel, element ?? GlueState.Self.CurrentElement, option);

            }

            return rfs;
        }

        private static CustomizableNewFileWindow CreateNewFileWindow(AddNewFileViewModel viewModel)
        {
            var nfw = new CustomizableNewFileWindow();
            nfw.DataContext = viewModel;

            PluginManager.AddNewFileOptions(nfw);

            if (GlueState.Self.CurrentElement != null)
            {
                foreach (ReferencedFileSave fileInElement in GlueState.Self.CurrentElement.ReferencedFiles)
                {
                    nfw.NamesAlreadyUsed.Add(FileManager.RemovePath(FileManager.RemoveExtension(fileInElement.Name)));
                }
            }

            // Also add CSV files
            nfw.AddOption(new AssetTypeInfo("csv", "", null, "Spreadsheet (.csv)", "", ""));

            return nfw;
        }

        #endregion

        #region EntitySave

        public async void ShowAddNewEntityDialog()
        {
            // search:  addentity, add entity
            if (ProjectManager.GlueProjectSave == null)
            {
                System.Windows.Forms.MessageBox.Show("You need to create or load a project first.");
            }
            else
            {
                if (ProjectManager.StatusCheck() == ProjectManager.CheckResult.Passed)
                {
                    AddEntityWindow window = new Controls.AddEntityWindow();
                    var viewModel = new AddEntityViewModel();

                    var project = GlueState.Self.CurrentGlueProject;
                    var sortedEntities = project.Entities.ToList().OrderBy(item => item);

                    viewModel.BaseEntityOptions.Add("<NONE>");

                    foreach (var entity in project.Entities.ToList())
                    {
                        viewModel.BaseEntityOptions.Add(entity.Name);
                    }

                    viewModel.SelectedBaseEntity = "<NONE>";

                    window.DataContext = viewModel;

                    MoveToCursor(window);

                    PluginManager.ModifyAddEntityWindow(window);

                    string directory = "";

                    if (GlueState.Self.CurrentTreeNode?.IsDirectoryNode() == true)
                    {
                        directory = GlueState.Self.CurrentTreeNode.GetRelativeFilePath();
                        directory = directory.Replace('/', '\\');
                    }

                    var result = window.ShowDialog();

                    if (result == true)
                    {
                        string entityName = viewModel.Name;

                        string whyIsntValid;

                        if (!NameVerifier.IsEntityNameValid(entityName, null, out whyIsntValid))
                        {
                            MessageBox.Show(whyIsntValid);
                        }
                        else
                        {
                            var entity = await GlueCommands.Self.GluxCommands.EntityCommands.AddEntityAsync(viewModel, directory);

                            await TaskManager.Self.AddAsync(() =>
                                PluginManager.ReactToNewEntityCreatedWithUi(entity, window),
                                "Calling plugin ReactToNewEntityCreatedWithUi", doOnUiThread: true);

                            GlueState.Self.CurrentEntitySave = entity;
                        }
                    }
                }
            }
        }

        public void MoveToCursor(System.Windows.Window window)
        {
            var source = System.Windows.PresentationSource.FromVisual(MainGlueWindow.MainWpfControl);
            window.MoveToCursor(source);
        }

        #endregion

        #region Variable

        public void ShowAddNewVariableDialog(CustomVariableType variableType = CustomVariableType.Exposed,
            string tunnelingObject = "",
            string tunneledVariableName = "",
            GlueElement container = null)
        {
            container = container ?? GlueState.Self.CurrentElement;

            var viewModel = new AddCustomVariableViewModel(container);
            viewModel.SetByDerived = true;
            viewModel.SelectedTunneledObject = tunnelingObject;
            viewModel.SelectedTunneledVariableName = tunneledVariableName;
            viewModel.DesiredVariableType = variableType;

            if(variableType == CustomVariableType.New)
            {
                viewModel.SelectedNewType = viewModel.AvailableNewVariableTypes.FirstOrDefault();
            }

            var xyzVariables = container.CustomVariables.Where(item =>
                item.Name == "X" || item.Name == "Y" || item.Name == "Z");

            var areAllStatic = container.CustomVariables.Except(xyzVariables).Count() > 0 &&
                container.CustomVariables.Except(xyzVariables).All(item => item.IsShared);

            if(areAllStatic && 
                // If tunneling on an object then it must be an instance, so make sure there is no tunnelingObject if setting this to true:
                string.IsNullOrEmpty(tunnelingObject))
            {
                viewModel.IsStatic = true;
            }

            var window = new AddVariableWindowWpf();
            window.DataContext = viewModel;

            var result = window.ShowDialog();

            if (result == true)
            {
                HandleAddVariableOk(viewModel, container);
            }

            //// Search terms:  add new variable, addnewvariable, add variable
            //AddVariableWindow addVariableWindow = new AddVariableWindow(container);
            //addVariableWindow.DesiredVariableType = variableType;

            //addVariableWindow.TunnelingObject = tunnelingObject;
            //addVariableWindow.TunnelingVariable = tunneledVariableName;

            //if (addVariableWindow.ShowDialog(MainGlueWindow.Self) == DialogResult.OK)
            //{
            //    HandleAddVariableOk(addVariableWindow.GetViewModel(), container);
            //}
        }

        private static async void HandleAddVariableOk(AddCustomVariableViewModel viewModel, GlueElement currentElement)
        {

            string resultName = viewModel.ResultName;
            string failureMessage;

            bool didFailureOccur = IsVariableInvalid(viewModel, currentElement, out failureMessage);


            if (!didFailureOccur)
            {
                if (!string.IsNullOrEmpty(viewModel.SelectedTunneledObject) && string.IsNullOrEmpty(viewModel.SelectedTunneledVariableName))
                {
                    didFailureOccur = true;
                    failureMessage = $"You must select a variable on {viewModel.SelectedTunneledObject}";
                }
            }

            if (didFailureOccur)
            {
                MessageBox.Show(failureMessage);
            }
            else
            {

                await TaskManager.Self.AddAsync(() =>
                {
                    // See if there is already a variable in the base with this name
                    CustomVariable existingVariableInBase = currentElement.GetCustomVariableRecursively(resultName);

                    bool canCreate = true;
                    bool isDefinedByBase = false;
                    if (existingVariableInBase != null)
                    {
                        if (existingVariableInBase.SetByDerived)
                        {
                            isDefinedByBase = true;
                        }
                        else
                        {
                            MessageBox.Show("There is already a variable named\n\n" + resultName +
                                "\n\nin the base element, but it is not SetByDerived.\nGlue will not " +
                                "create a variable because it would result in a name conflict.");

                            canCreate = false;
                        }
                    }

                    if (canCreate)
                    {
                        string type = viewModel.ResultType;
                        string sourceObject = viewModel.SelectedTunneledObject;
                        if(string.IsNullOrEmpty(sourceObject))
                        {
                            sourceObject = null;
                        }
                        string sourceObjectProperty = null;
                        if (!string.IsNullOrEmpty(sourceObject))
                        {
                            sourceObjectProperty = viewModel.SelectedTunneledVariableName;
                        }
                        string overridingType = viewModel.SelectedOverridingType;
                        if(overridingType == "<none>")
                        {
                            overridingType = null;
                        }
                        string typeConverter = viewModel.SelectedTypeConverter;

                        CustomVariable newVariable = new CustomVariable();
                        newVariable.Name = resultName;
                        if(viewModel.IsList)
                        {
                            newVariable.Type = $"List<{type}>";
                        }
                        else
                        {
                            newVariable.Type = type;
                        }
                        newVariable.SourceObject = sourceObject;
                        newVariable.SourceObjectProperty = sourceObjectProperty;

                        newVariable.IsShared = viewModel.IsStatic && 
                            // User could have checked a source object after checking IsStatic 
                            string.IsNullOrEmpty(sourceObject);

                        newVariable.SetByDerived = viewModel.SetByDerived;
                        newVariable.DefinedByBase = isDefinedByBase;



                        if (!string.IsNullOrEmpty(overridingType))
                        {
                            newVariable.OverridingPropertyType = overridingType;
                            newVariable.TypeConverter = typeConverter;
                        }

                        object defaultValue = null;

                        if (!string.IsNullOrEmpty(sourceObject))
                        {
                            var namedObjectSource = currentElement.GetNamedObjectRecursively(sourceObject);
                            if (namedObjectSource != null)
                            {
                                var ati = namedObjectSource.GetAssetTypeInfo();

                                var variableDefinition = ati?.VariableDefinitions.FirstOrDefault(item => item.Name == sourceObjectProperty);

                                newVariable.Category = variableDefinition?.Category;

                                defaultValue = namedObjectSource.GetCustomVariable(sourceObjectProperty)?.Value;
                            }
                        }

                        newVariable.DefaultValue = defaultValue;

                        GlueCommands.Self.GluxCommands.ElementCommands.AddCustomVariableToElement(newVariable, currentElement);
                    }
                }, $"Adding variable {resultName} through UI");
            }
        }

        public static bool IsVariableInvalid(AddCustomVariableViewModel viewModel, IElement currentElement, out string failureMessage)
        {
            bool didFailureOccur = false;

            string whyItIsntValid = "";
            var resultName = viewModel.ResultName;
            didFailureOccur = NameVerifier.IsCustomVariableNameValid(resultName, null, currentElement, ref whyItIsntValid) == false;
            failureMessage = null;
            if (didFailureOccur)
            {
                failureMessage = whyItIsntValid;

            }

            if (!didFailureOccur && NameVerifier.DoesTunneledVariableAlreadyExist(viewModel.SelectedTunneledObject, viewModel.SelectedTunneledVariableName, currentElement))
            {
                didFailureOccur = true;
                failureMessage = "There is already a variable that is modifying " + viewModel.SelectedTunneledVariableName + " on " + viewModel.SelectedTunneledObject;
            }
            
            if (!didFailureOccur && viewModel != null && IsUserTryingToCreateNewWithExposableName(viewModel.ResultName, viewModel.DesiredVariableType == CustomVariableType.Exposed))
            {
                didFailureOccur = true;
                failureMessage = "The variable\n\n" + resultName + "\n\nis an expoable variable.  Please use a different variable name or select the variable through the Expose tab";
            }

            if (!didFailureOccur && ExposedVariableManager.IsReservedPositionedPositionedObjectMember(resultName) && currentElement is EntitySave)
            {
                didFailureOccur = true;
                failureMessage = "The variable\n\n" + resultName + "\n\nis reserved by FlatRedBall.";
            }

            if(!didFailureOccur && viewModel.DesiredVariableType == CustomVariableType.New && string.IsNullOrEmpty(viewModel.SelectedNewType) )
            {
                didFailureOccur = true;
                failureMessage = "A type must be selected for new variables";
            }

            return didFailureOccur;
        }

        private static bool IsUserTryingToCreateNewWithExposableName(string resultName, bool isExposeTabSelected)
        {
            List<string> exposables = ExposedVariableManager.GetExposableMembersFor(GlueState.Self.CurrentElement, false).Select(item => item.Member).ToList();
            if (exposables.Contains(resultName))
            {
                return isExposeTabSelected == false;
            }
            else
            {
                return false;
            }
        }


        #endregion

        #region Screen
        public async void ShowAddNewScreenDialog()
        {
            //////////////Early Out////////////
            if (ProjectManager.GlueProjectSave == null)
            {
                System.Windows.Forms.MessageBox.Show("You need to create or load a project first.");
                return;
            }
            if (ProjectManager.StatusCheck() != ProjectManager.CheckResult.Passed)
            {
                return;
            }
            ////////////End Early Out

            // AddScreen, add screen, addnewscreen, add new screen
            var addScreenWindow = new AddScreenWindow();

            MoveToCursor(addScreenWindow);

            addScreenWindow.Message = "Enter a name for the new Screen";

            string name = "NewScreen";

            if (GlueState.Self.CurrentGlueProject.Screens.Count == 0)
            {
                name = "GameScreen";
            }

            var allScreenNames =
                GlueState.Self.CurrentGlueProject.Screens
                .Select(item => item.GetStrippedName())
                .ToList();

            name = StringFunctions.MakeStringUnique(name,
                allScreenNames, 2);


            var viewModel = new AddScreenViewModel();
            viewModel.ScreenName = name;
            addScreenWindow.DataContext = viewModel;
            addScreenWindow.TextEntered += (not, used) => viewModel.HasChangedScreenTextBox = true;

            PluginManager.ModifyAddScreenWindow(addScreenWindow);


            var result = addScreenWindow.ShowDialog();
            if (result == true)
            {
                string whyItIsntValid;

                if (!NameVerifier.IsScreenNameValid(addScreenWindow.Result, null, out whyItIsntValid))
                {
                    MessageBox.Show(whyItIsntValid);
                }
                else
                {
                    var screen =
                        await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen(addScreenWindow.Result);

                    GlueState.Self.CurrentElement = screen;

                    await PluginManager.ReactToNewScreenCreatedWithUiAsync(screen, addScreenWindow);

                }

            }
        }

        #endregion

        #region Event

        public void ShowAddNewEventDialog(NamedObjectSave eventOwner)
        {
            var name = eventOwner.InstanceName;
            var element = ObjectFinder.Self.GetElementContaining(eventOwner);
            if (element != GlueState.Self.CurrentElement)
            {
                GlueState.Self.CurrentElement = element;
            }
            AddEventViewModel viewModel = new AddEventViewModel();
            viewModel.TunnelingObject = name;
            viewModel.DesiredEventType = CustomEventType.Tunneled;

            ShowAddNewEventDialog(viewModel);
        }

        public void ShowAddNewEventDialog(AddEventViewModel viewModel)
        {
            AddEventWindow addEventWindow = new AddEventWindow();
            addEventWindow.ViewModel = viewModel;

            if (addEventWindow.ShowDialog(MainGlueWindow.Self) == DialogResult.OK)
            {
                RightClickHelper.HandleAddEventOk(addEventWindow);
            }

        }

        #endregion

        #region Set Focus

        public void FocusTab(string dialogTitle)
        {
            GlueCommands.Self.DoOnUiThread(() =>
            {
                var focused = TryFocus(PluginManager.TabControlViewModel.TopTabItems);

                if (!focused) focused = TryFocus(PluginManager.TabControlViewModel.BottomTabItems);
                if (!focused) focused = TryFocus(PluginManager.TabControlViewModel.LeftTabItems);
                if (!focused) focused = TryFocus(PluginManager.TabControlViewModel.RightTabItems);
                if (!focused) focused = TryFocus(PluginManager.TabControlViewModel.CenterTabItems);
            });
            bool TryFocus(TabContainerViewModel items)
            {
                foreach (var tabPage in items.Tabs)
                {
                    if (tabPage.Title == dialogTitle)
                    {
                        tabPage.IsSelected = true;
                        tabPage.RecordLastClick();
                        return true;
                    }
                }
                return false;
            }
        }
        public void FocusOnTreeView()
        {
            PluginManager.ReactToFocusOnTreeView();
        }

        #endregion

        #region Show Message Box

        public void ShowMessageBox(string message, string caption = "")
        {
            GlueGui.ShowMessageBox(message, caption);
        }

        public System.Windows.MessageBoxResult ShowYesNoMessageBox(string message, string caption = "Confirm", Action yesAction = null, Action noAction = null)
        {
            System.Windows.MessageBoxResult result = System.Windows.MessageBoxResult.None;

            if (GlueGui.ShowGui)
            {
                GlueCommands.Self.DoOnUiThread(() =>
               {
                   //var result = MessageBox.Show(message, caption, MessageBoxButtons.YesNo);
                   result = System.Windows.MessageBox.Show(message, caption, System.Windows.MessageBoxButton.YesNo);

                   if (result == System.Windows.MessageBoxResult.Yes)
                   {
                       yesAction?.Invoke();
                   }
                   else if (result == System.Windows.MessageBoxResult.No)
                   {
                       noAction?.Invoke();
                   }
               });

            }

            return result;
        }

        #endregion

        #region State Categories

        public async void ShowAddNewCategoryDialog()
        {
            // add category, addcategory, add state category
            var tiw = new TextInputWindow();
            tiw.Message = "Enter a name for the new category";
            tiw.Text = "New Category";

            DialogResult result = tiw.ShowDialog(MainGlueWindow.Self);

            if (result == DialogResult.OK)
            {
                string whyItIsntValid;

                if (!NameVerifier.IsStateCategoryNameValid(tiw.Result, out whyItIsntValid))
                {
                    GlueGui.ShowMessageBox(whyItIsntValid);
                }
                else
                {
                    await GlueCommands.Self.GluxCommands.ElementCommands.AddStateSaveCategoryAsync(tiw.Result, GlueState.Self.CurrentElement);
                }
            }
        }

        #endregion

        #region Go to definition
        public void GoToDefinitionOfSelection()
        {
            var selectedNode = GlueState.Self.CurrentTreeNode;

            #region Named object

            if (selectedNode.IsNamedObjectNode())
            {
                NamedObjectSave nos = selectedNode.Tag as NamedObjectSave;

                if (nos.DefinedByBase)
                {
                    var baseNos = ObjectFinder.Self.GetRootDefiningObject(nos);
                    if (baseNos != null)
                    {
                        GlueState.Self.CurrentNamedObjectSave = baseNos;
                    }
                    else
                    {
                        GlueCommands.Self.PrintOutput($"Could not find defining base object for {baseNos}");
                    }
                }
                else if (nos.SourceType == SourceType.Entity)
                {
                    GlueState.Self.CurrentEntitySave = ObjectFinder.Self.GetEntitySave(nos.SourceClassType);

                }
                else if (nos.SourceType == SourceType.FlatRedBallType && nos.IsGenericType)
                {
                    // Is this an entity?
                    EntitySave genericEntityType = ObjectFinder.Self.GetEntitySave(nos.SourceClassGenericType);

                    if (genericEntityType != null)
                    {
                        GlueState.Self.CurrentEntitySave = genericEntityType;
                    }

                }
                else if (nos.SourceType == SourceType.File && !string.IsNullOrEmpty(nos.SourceFile))
                {
                    ReferencedFileSave rfs = nos.GetContainer().GetReferencedFileSave(nos.SourceFile);
                    GlueState.Self.CurrentReferencedFileSave = rfs;
                }
            }

            #endregion

            #region CustomVariable
            else if (selectedNode.IsCustomVariable())
            {
                CustomVariable customVariable = GlueState.Self.CurrentCustomVariable;

                if (!string.IsNullOrEmpty(customVariable.SourceObject))
                {
                    NamedObjectSave namedObjectSave = GlueState.Self.CurrentElement.GetNamedObjectRecursively(customVariable.SourceObject);

                    if (namedObjectSave != null)
                    {
                        GlueState.Self.CurrentNamedObjectSave = namedObjectSave;
                    }

                }
            }

            #endregion

            #region Event

            else if (selectedNode.IsEventResponseTreeNode())
            {
                var ers = GlueState.Self.CurrentEventResponseSave;

                if (!string.IsNullOrEmpty(ers.SourceObject))
                {
                    NamedObjectSave namedObjectSave = GlueState.Self.CurrentElement.GetNamedObjectRecursively(ers.SourceObject);

                    if (namedObjectSave != null)
                    {
                        GlueState.Self.CurrentNamedObjectSave = namedObjectSave;
                    }
                }
            }


            #endregion

            #region Enity/Screen

            else if (selectedNode.IsElementNode())
            {
                IElement element = selectedNode.Tag as IElement;

                string baseObject = element.BaseElement;

                if (!string.IsNullOrEmpty(baseObject))
                {
                    var baseElement = ObjectFinder.Self.GetElement(baseObject);

                    GlueState.Self.CurrentElement = baseElement;
                }
            }

            #endregion
        }

        #endregion

        public void SetFormOwner(Form form)
        {
            if (MainGlueWindow.Self != null)
                form.Owner = MainGlueWindow.Self;
        }

    }
}
