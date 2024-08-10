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
using System.Collections.Generic;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using GlueFormsCore.ViewModels;
using GlueFormsCore.Controls;
using FlatRedBall.Glue.VSHelpers;
using GlueFormsCore.Extensions;
using FlatRedBall.Glue.IO;
using System.Threading.Tasks;
using L = Localization;
using ShimSkiaSharp;
using System.Threading;
using System.Windows.Controls;


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

                        var foundProject = solution.ReferencedProjects.FirstOrDefault(item =>
                        {
                            var isRegularProject = FileManager.GetExtension(item.Name) == "csproj" || FileManager.GetExtension(item.Name) == "vsproj";

                            bool hasSameName = FileManager.RemovePath(FileManager.RemoveExtension(solutionName)).ToLowerInvariant() ==
                                FileManager.RemovePath(FileManager.RemoveExtension(item.Name)).ToLowerInvariant();


                            return isRegularProject && hasSameName;
                        });

                        projectFileName = FileManager.GetDirectory(solutionName) + foundProject.Name;
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
                        GlueCommands.Self.DialogCommands.ShowMessageBox(String.Format(L.Texts.ErrorOpenAttemptFailed, projectFileName, e));
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
                        whyItIsntValid = L.Texts.TypeInfiniteRecursion;
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
                addObjectViewModel.FlatRedBallAndCustomTypes.Add(AvailableAssetTypes.CommonAtis.CapsulePolygon);
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
                addObjectViewModel.EffectiveElement.ReferencedFiles.ToList();

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
            AskToRemoveObjectList(new List<NamedObjectSave>() { namedObjectToRemove }, saveAndRegenerate);
        }

        public async void AskToRemoveObjectList(List<NamedObjectSave> namedObjectsToRemove, bool saveAndRegenerate = true)
        {
            // Search terms: removefromproject, remove from project, remove file, remove referencedfilesave
            List<string> filesToRemove = new List<string>();

            DialogResult reallyRemoveResult = DialogResult.Yes;

            var canDelete = true;

            List<NamedObjectSave> baseRestrictingDeletes = new List<NamedObjectSave>();
            foreach(NamedObjectSave namedObjectToRemove in namedObjectsToRemove)
            {
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
                        baseRestrictingDeletes.Add(definingNos);
                    }
                }
            }
            if(baseRestrictingDeletes.Count > 0)
            {

                var message = $"{L.Texts.ExposedInDerivedCannotDelete}\n";

                foreach(var item in baseRestrictingDeletes)
                {
                    message += $"{item}\n";
                }

                GlueCommands.Self.DialogCommands.ShowMessageBox(message);

                canDelete = false;
            }

            var owners = new HashSet<GlueElement>();

            if (canDelete)
            {
                var window = new RemoveObjectWindow();
                var viewModel = new RemoveObjectViewModel();
                viewModel.SetFrom(namedObjectsToRemove);

                foreach(var namedObjectToRemove in namedObjectsToRemove)
                {
                    var owner = ObjectFinder.Self.GetElementContaining(namedObjectToRemove);
                    if (owner == null)
                    { System.Diagnostics.Debugger.Break(); }
                    if (owner != null)
                    {
                        owners.Add(owner);
                        var objectsToRemove = GluxCommands.GetObjectsToRemoveIfRemoving(namedObjectToRemove, owner);

                        viewModel.ObjectsToRemove.AddRange(objectsToRemove.CustomVariables.Select(item => item.ToString()));
                        viewModel.ObjectsToRemove.AddRange(objectsToRemove.SubObjectsInList.Select(item => item.ToString()));
                        viewModel.ObjectsToRemove.AddRange(objectsToRemove.CollisionRelationships.Select(item => item.ToString()));
                        viewModel.ObjectsToRemove.AddRange(objectsToRemove.DerivedNamedObjects.Select(item => item.ToString()));
                        viewModel.ObjectsToRemove.AddRange(objectsToRemove.EventResponses.Select(item => item.ToString()));

                    }

                }

                window.DataContext = viewModel;
                var showDialogResult = window.ShowDialog();
                reallyRemoveResult = showDialogResult == true ? DialogResult.Yes : DialogResult.No;
            }




            if (canDelete && reallyRemoveResult == DialogResult.Yes)
            {
                await GlueCommands.Self.GluxCommands
                    .RemoveNamedObjectListAsync(namedObjectsToRemove, true, true, filesToRemove);

                if (filesToRemove.Count != 0)
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

                    string messageString = L.Texts.WhatToDoWithFiles + "\n";
                    lbw.Message = messageString;

                    foreach (var s in filesToRemove)
                    {
                        lbw.AddItem(s);
                    }
                    lbw.ClearButtons();
                    lbw.AddButton(L.Texts.FilesLeaveAsPartOfProject, DialogResult.No);
                    lbw.AddButton(L.Texts.FilesRemoveFromProjectButKeep, DialogResult.OK);
                    lbw.AddButton(L.Texts.FilesRemoveAndDelete, DialogResult.Yes);

                    lbw.ShowDialog();
                    var result = (DialogResult)lbw.ClickedOption;

                    if (result is DialogResult.OK or DialogResult.Yes)
                    {
                        foreach (var file in filesToRemove)
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
                        foreach (var fileName in filesToRemove.Select(file => GlueCommands.Self.GetAbsoluteFileName(file, false)).Where(System.IO.File.Exists))
                        {
                            FileHelper.MoveToRecycleBin(fileName);
                        }
                    }
                }

                TaskManager.Self.AddOrRunIfTasked(() =>
                {
                    // Nodes aren't directly removed in the code above. Instead, 
                    // a "refresh nodes" method is called, which may remove unneeded
                    // nodes, but event raising is suppressed. Therefore, we have to explicitly 
                    // do it here:
                    foreach(var owner in owners)
                    {
                        GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(owner);
                    }
                }, L.Texts.RefreshingTreeNodes);


                if (saveAndRegenerate)
                {
                    foreach (var owner in owners)
                    {
                        GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(owner);
                    }

                    GluxCommands.Self.ProjectCommands.SaveProjects();
                    //GluxCommands.Self.SaveProjectAndElements();
                    foreach (var owner in owners)
                    {
                        _ = GluxCommands.Self.SaveElementAsync(owner);
                    }
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
            nfw.AddOption(new AssetTypeInfo("csv", "", null, L.Texts.Spreadsheet + " (.csv)", "", ""));

            return nfw;
        }

        #endregion

        #region EntitySave

        public async void ShowAddNewEntityDialog(AddEntityViewModel viewModel = null)
        {
            viewModel = viewModel ?? CreateAddNewEntityViewModel();

            GlueProjectSave project = GlueState.Self.CurrentGlueProject;

            // search:  addentity, add entity
            if (project == null)
            {
                System.Windows.Forms.MessageBox.Show(L.Texts.ErrorNewProjectFirst);
            }
            else
            {
                if (ProjectManager.StatusCheck() == ProjectManager.CheckResult.Passed)
                {
                    AddEntityWindow window = new Controls.AddEntityWindow();

                    window.DataContext = viewModel;

                    MoveToCursor(window);

                    PluginManager.ModifyAddEntityWindow(window);

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
                            var entity = await GlueCommands.Self.GluxCommands.EntityCommands.AddEntityAsync(viewModel);

                            await TaskManager.Self.AddAsync(() =>
                                PluginManager.ReactToNewEntityCreatedWithUi(entity, window),
                                L.Texts.PluginCallReactToNewEntity, doOnUiThread: true);

                            GlueState.Self.CurrentEntitySave = entity;
                        }
                    }
                }
            }
        }

        public AddEntityViewModel CreateAddNewEntityViewModel()
        {
            AddEntityViewModel viewModel = new AddEntityViewModel();
            viewModel.BaseEntityOptions.Add("<NONE>");
            var project = GlueState.Self.CurrentGlueProject;
            if (project != null)
            {
                var sortedEntities = project.Entities.ToList().OrderBy(item => item);

                foreach (var entity in project.Entities.ToList())
                {
                    viewModel.BaseEntityOptions.Add(entity.Name);
                }
            }

            viewModel.SelectedBaseEntity = "<NONE>";

            viewModel.Directory = "";

            if (GlueState.Self.CurrentTreeNode?.IsDirectoryNode() == true)
            {
                viewModel.Directory = GlueState.Self.CurrentTreeNode.GetRelativeFilePath();
                viewModel.Directory = viewModel.Directory.Replace('/', '\\');
            }

            viewModel.DirectoryOptions.Add("Entities\\");
            FillViewModelWithDirectoriesRecursively("Entities\\", viewModel);
            viewModel.Directory = viewModel.DirectoryOptions[0];

            return viewModel;
        }

        private void FillViewModelWithDirectoriesRecursively(string relativeDirectory, AddEntityViewModel viewModel)
        {
            FilePath absolute = GlueState.Self.CurrentGlueProjectDirectory + relativeDirectory;

            if(absolute.Exists())
            {
                foreach(var directory in System.IO.Directory.GetDirectories(absolute.FullPath))
                {
                    string relative = FileManager.MakeRelative(directory, GlueState.Self.CurrentGlueProjectDirectory);
                    relative = relative.Replace('/', '\\');

                    viewModel.DirectoryOptions.Add(relative + "\\");

                    FillViewModelWithDirectoriesRecursively(relative, viewModel);
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
            container ??= GlueState.Self.CurrentElement;

            if(container == null)
            {
                throw new NullReferenceException("The current element is null)");
            }

            var viewModel = new AddCustomVariableViewModel(container)
            {
                SetByDerived = true,
                SelectedTunneledObject = tunnelingObject,
                SelectedTunneledVariableName = tunneledVariableName,
                DesiredVariableType = variableType
            };

            var variableCategories = new HashSet<string>();
            variableCategories.AddRange(container.CustomVariables.Select(item => item.Category).Where(item => !string.IsNullOrEmpty(item)));
            var baseElements = ObjectFinder.Self.GetAllBaseElementsRecursively(container);
            foreach(var element in baseElements)
            {
                variableCategories.AddRange(element.CustomVariables.Select(item => item.Category).Where(item => !string.IsNullOrEmpty(item)));
            }
            viewModel.AvailableCategories.AddRange(variableCategories);

            if (variableType == CustomVariableType.New)
            {
                viewModel.SelectedNewType = viewModel.AvailableNewVariableTypes.FirstOrDefault();
            }

            var xyzVariables = container.CustomVariables.Where(item => item.Name is "X" or "Y" or "Z");

            var areAllStatic = container.CustomVariables.Except(xyzVariables).Any() &&
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
                    failureMessage = String.Format(L.Texts.VariableMustSelect, viewModel.SelectedTunneledObject);
                }
            }

            if (didFailureOccur)
            {
                MessageBox.Show(failureMessage);
            }
            else
            {

                await TaskManager.Self.AddAsync(async () =>
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
                            MessageBox.Show(String.Format(L.Texts.VariableInBaseAlreadyExists, resultName));
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

                        var newVariable = new CustomVariable
                        {
                            Name = resultName
                        };

                        newVariable.Type = viewModel.IsList ? $"List<{type}>" : type;
                        newVariable.SourceObject = sourceObject;
                        newVariable.SourceObjectProperty = sourceObjectProperty;

                        newVariable.IsShared = viewModel.IsStatic && 
                            // User could have checked a source object after checking IsStatic 
                            string.IsNullOrEmpty(sourceObject);

                        if(!viewModel.IsStatic)
                        {
                            newVariable.SetByDerived = viewModel.SetByDerived;
                        }

                        newVariable.DefinedByBase = isDefinedByBase;



                        if (!string.IsNullOrEmpty(overridingType))
                        {
                            newVariable.OverridingPropertyType = overridingType;
                            newVariable.TypeConverter = typeConverter;
                        }

                        object defaultValue = null;

                        newVariable.Summary = viewModel.NewVariableSummary;
                        newVariable.Category = viewModel.NewVariableCategory;

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

                        await GlueCommands.Self.GluxCommands.ElementCommands.AddCustomVariableToElementAsync(newVariable, currentElement);
                    }
                }, String.Format(L.Texts.VariableAddThroughUI, resultName));
            }
        }

        public static bool IsVariableInvalid(AddCustomVariableViewModel viewModel, IElement currentElement, out string failureMessage)
        {
            string whyItIsntValid = "";
            var resultName = viewModel.ResultName;
            var didFailureOccur = NameVerifier.IsCustomVariableNameValid(resultName, null, currentElement, ref whyItIsntValid) == false;
            failureMessage = null;
            if (didFailureOccur)
            {
                failureMessage = whyItIsntValid;

            }

            if (!didFailureOccur && NameVerifier.DoesTunneledVariableAlreadyExist(viewModel.SelectedTunneledObject, viewModel.SelectedTunneledVariableName, currentElement))
            {
                didFailureOccur = true;
                failureMessage = String.Format(L.Texts.VariableAlreadyModifying, viewModel.SelectedTunneledVariableName, viewModel.SelectedTunneledObject);
            }
            
            if (!didFailureOccur && viewModel != null && viewModel.DesiredVariableType != CustomVariableType.Exposed && IsUserTryingToCreateNewWithExposableName(viewModel.ResultName))
            {
                didFailureOccur = true;
                failureMessage = String.Format(L.Texts.VariableIsExposableUseDifferentName, resultName);
            }

            if (!didFailureOccur && ExposedVariableManager.IsReservedPositionedPositionedObjectMember(resultName) && currentElement is EntitySave)
            {
                didFailureOccur = true;
                failureMessage = String.Format(L.Texts.VariableFrbReserved, resultName);
            }

            if(!didFailureOccur && viewModel.DesiredVariableType == CustomVariableType.New && string.IsNullOrEmpty(viewModel.SelectedNewType) )
            {
                didFailureOccur = true;
                failureMessage = L.Texts.VariableTypeMustBeSelectedForNew;
            }

            return didFailureOccur;
        }

        private static bool IsUserTryingToCreateNewWithExposableName(string resultName)
        {
            return ExposedVariableManager.GetExposableMembersFor(GlueState.Self.CurrentElement, false)
                .Select(item => item.Member)
                .Contains(resultName);
        }


        #endregion

        #region Screen
        public async void ShowAddNewScreenDialog()
        {
            //////////////Early Out////////////
            if (ProjectManager.GlueProjectSave == null)
            {
                System.Windows.Forms.MessageBox.Show(L.Texts.ErrorNewProjectFirst);
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

            addScreenWindow.Message = L.Texts.EnterNewScreenName;

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

        public void ShowAddNewEventDialog(GlueElement glueElement)
        {
            AddEventViewModel viewModel = CreateAddEventViewModel(glueElement);
            ShowAddNewEventDialog(viewModel);
        }

        public void ShowAddNewEventDialog(NamedObjectSave eventOwner)
        {
            var name = eventOwner.InstanceName;
            var element = ObjectFinder.Self.GetElementContaining(eventOwner);

            // Vic asks - is this necessary anymore?
            if (element != GlueState.Self.CurrentElement)
            {
                GlueState.Self.CurrentElement = element;
            }
            AddEventViewModel viewModel = CreateAddEventViewModel(element);

            viewModel.TunnelingObject = name;
            viewModel.DesiredEventType = CustomEventType.Tunneled;
            ShowAddNewEventDialog(viewModel);
        }

        private static AddEventViewModel CreateAddEventViewModel(GlueElement element)
        {
            AddEventViewModel viewModel = new AddEventViewModel();

            if (element is EntitySave entity)
            {
                viewModel.ExposableEvents = ExposedEventManager.GetExposableEventsFor(entity, true);
            }
            else if (element is ScreenSave screen)
            {
                viewModel.ExposableEvents = ExposedEventManager.GetExposableEventsFor(screen, true);
            }

            return viewModel;
        }

        public void ShowAddNewEventDialog(AddEventViewModel viewModel)
        {
            var addEventWindow = new AddEventWindow
            {
                ViewModel = viewModel
            };


            if (addEventWindow.ShowDialog() == true)
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
                if (!focused) TryFocus(PluginManager.TabControlViewModel.CenterTabItems);
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

        public System.Windows.MessageBoxResult ShowYesNoMessageBox(string message, string caption = null, Action yesAction = null, Action noAction = null)
        {
            caption ??= L.Texts.Confirm;
            var result = System.Windows.MessageBoxResult.None;

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

        #region Spinner

        public void ShowSpinner(string text)
        {
            MainPanelControl.Self.SpinnerLabel.Content = text;
            MainPanelControl.Self.Spinner.Visibility = System.Windows.Visibility.Visible;
        }

        public void HideSpinner()
        {
            MainPanelControl.Self.Spinner.Visibility = System.Windows.Visibility.Collapsed;

        }

        #endregion

        #region Toast

        CancellationTokenSource lastToastCancellation = null;
        public async void ShowToast(string text, TimeSpan? timeToShowToast = null)
        {
            var panel = MainPanelControl.Self;

            panel.ToastLabel.Content = text;
            panel.Toast.Visibility = System.Windows.Visibility.Visible;

            if(lastToastCancellation != null)
            {
                lastToastCancellation.Cancel();
            }

            lastToastCancellation = new CancellationTokenSource();

            var timeToWait = timeToShowToast ?? TimeSpan.FromSeconds(5);

            var wasCancelled = false;
            try
            {
                await Task.Delay(timeToWait, lastToastCancellation.Token);
            }
            catch(OperationCanceledException)
            {
                wasCancelled = true;
            }

            if(!wasCancelled)
            {
                // if cancelled, the next caller will hide it...
                HideToast();
            }
        }

        public void HideToast()
        {
            var panel = MainPanelControl.Self;
            panel.Toast.Visibility = System.Windows.Visibility.Collapsed;
        }


        #endregion

        #region State Categories

        public async void ShowAddNewCategoryDialog()
        {
            // add category, addcategory, add state category
            var tiw = new TextInputWindow();
            tiw.Message = L.Texts.CategoryEnterName;
            tiw.Text = L.Texts.CategoryNew;

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

        #region StateSave

        public void ShowAddNewStateDialog()
        {
            // search: addstate, add new state, addnewstate, add state
            var tiw = new TextInputWindow();
            tiw.Message = L.Texts.StateEnterName;
            tiw.Text = L.Texts.StateNew;


            DialogResult result = tiw.ShowDialog(MainGlueWindow.Self);

            if (result == DialogResult.OK)
            {
                var currentElement = GlueState.Self.CurrentElement;

                string whyItIsntValid;
                if (!NameVerifier.IsStateNameValid(tiw.Result, currentElement, GlueState.Self.CurrentStateSaveCategory, GlueState.Self.CurrentStateSave, out whyItIsntValid))
                {
                    GlueGui.ShowMessageBox(whyItIsntValid);
                }
                else
                {
                    StateSave newState = new StateSave();
                    newState.Name = tiw.Result;
                    var category = GlueState.Self.CurrentStateSaveCategory;
                    GlueCommands.Self.GluxCommands.AddStateSave(newState, category, currentElement);
                }
            }
        }

        #endregion

        #region Go to definition
        public void GoToDefinitionOfSelection()
        {
            var selectedNode = GlueState.Self.CurrentTreeNode;

            ///////////////////early out////////////////
            if(selectedNode == null)
            {
                return;
            }
            //////////////////end early out/////////////

            #region Named object

            if (selectedNode?.IsNamedObjectNode() == true)
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
                        GlueCommands.Self.PrintOutput(String.Format(L.Texts.ObjectCouldNotDefineFor, baseNos));
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
                var element = GlueState.Self.CurrentElement;
                if (customVariable.DefinedByBase && element != null && !string.IsNullOrEmpty(element.BaseElement))
                {
                    var baseElement = ObjectFinder.Self.GetElement(element.BaseElement);
                    var variableInBase = baseElement?.GetCustomVariableRecursively(customVariable.Name);
                    if(variableInBase != null)
                    {
                        GlueState.Self.CurrentCustomVariable = variableInBase;
                    }
                }
                else if (!string.IsNullOrEmpty(customVariable.SourceObject))
                {
                    NamedObjectSave namedObjectSave = element.GetNamedObjectRecursively(customVariable.SourceObject);

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
