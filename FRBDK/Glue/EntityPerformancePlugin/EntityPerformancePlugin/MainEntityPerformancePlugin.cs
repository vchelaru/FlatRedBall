using EntityPerformancePlugin.CodeGenerators;
using EntityPerformancePlugin.Converters;
using EntityPerformancePlugin.Models;
using EntityPerformancePlugin.ViewModels;
using EntityPerformancePlugin.Views;
using EntityPerformancePluginCore.CodeGenerators;
using FlatRedBall.Entities;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EntityPerformancePlugin
{
    [Export(typeof(PluginBase))]
    public class MainEntityPerformancePlugin : PluginBase
    {
        #region Properties

        bool saveOnViewModelChanges = true;

        MainControl mainControl;
        MainViewModel viewModel;

        ProjectManagementValues model;

        PluginTab tab;

        public override string FriendlyName
        {
            get { return "Entity Performance Plugin"; }
        }

        public override Version Version
        {
            get { return new Version(1, 0); }
        }

        VariableActivityCodeGenerator variableActivityCodeGenerator;
        UpdateDependenciesCodeGenerator updateDependenciesCodeGenerator;

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            this.UnregisterAllCodeGenerators();
            return true;
        }

        public override void StartUp()
        {
            variableActivityCodeGenerator = new VariableActivityCodeGenerator();
            this.RegisterCodeGenerator(variableActivityCodeGenerator);

            updateDependenciesCodeGenerator = new UpdateDependenciesCodeGenerator();
            this.RegisterCodeGenerator(updateDependenciesCodeGenerator);

            AssignEvents();
        }

        private void AssignEvents()
        {
            this.ReactToLoadedGluxEarly += HandleLoadGlux;
            this.ReactToUnloadedGlux += HandleGluxUnload;
            this.ReactToItemSelectHandler += HandleGlueItemSelected;
            this.ReactToChangedPropertyHandler += HandleGluePropertyChanged;
            this.ReactToFileChange += HandleFileChange;
            this.ReactToChangedPropertyHandler += HandlePropertyChanged;
        }

        private void HandleFileChange(FilePath filePath, FileChangeType changeType)
        {
            if(filePath == GetPerformanceFilePath() && changeType != FileChangeType.Deleted)
            {
                LoadManagementValues();

                var entity = GlueState.Self.CurrentEntitySave;

                if(entity != null)
                {
                    RefreshView(GlueState.Self.CurrentEntitySave);

                    GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
                }
            }
        }

        private void HandlePropertyChanged(string changedMember, object oldValue, GlueElement owningElement)
        {
            var currentEntity = owningElement as EntitySave;
            var instance = GlueState.Self.CurrentNamedObjectSave;

            if (currentEntity != null && changedMember == nameof(EntitySave.Name) && instance == null)
            {

                string oldName = (string)oldValue;

                if (oldName != null)
                {
                    // Not sure why but Glue doesn't send down the entities prefix
                    oldName = "Entities\\" + oldName;
                }

                var foundModel = model.EntityManagementValueList.FirstOrDefault(item => item.Name == oldName);

                if (foundModel != null)
                {
                    foundModel.Name = currentEntity.Name;

                    RefreshView(currentEntity);

                    SavePerformanceData();
                }
            }
            else if(instance != null && changedMember == nameof(NamedObjectSave.InstanceName))
            {
                string oldName = (string)oldValue;

                var foundModel = model.EntityManagementValueList.FirstOrDefault(item => item.Name == owningElement.Name);

                var foundInstance = foundModel?.InstanceManagementValuesList.FirstOrDefault(item => item.Name == oldName);

                if (foundInstance != null)
                {
                    foundInstance.Name = instance.InstanceName;

                    RefreshView(currentEntity);

                    if (viewModel == null)
                    {
                        throw new NullReferenceException($"{nameof(viewModel)} is null, entity is {currentEntity?.ToString() ?? "null"}");
                    }

                    SavePerformanceData(currentEntity);
                }
            }
        }

        private void HandleGluePropertyChanged(string changedMember, object oldValue, GlueElement element)
        {
            if(changedMember == nameof(EntitySave.IsManuallyUpdated) && 
                element != null &&
                // make sure the entity is selected and not a named object,
                // because if a named object is selected the plugin doesn't (currently) show
                GlueState.Self.CurrentNamedObjectSave == null)
            {
                var isRootSelected = viewModel?.IsRootSelected;
                var selectedInstance = viewModel?.SelectedInstance;
                RefreshView(element as EntitySave);

                if(isRootSelected == true)
                {
                    viewModel.IsRootSelected = true;
                }
                else if(selectedInstance != null)
                {
                    viewModel.SelectedInstance =
                        // we have new objects in the list
                        viewModel.Instances.FirstOrDefault(item => item.Name == selectedInstance.Name);
                    mainControl.RefreshSelection();
                }
            }
        }

        private void HandleGluxUnload()
        {
            model = null;
            variableActivityCodeGenerator.Values = null;
            updateDependenciesCodeGenerator.Values = null;
        }

        private void HandleLoadGlux()
        {
            LoadManagementValues();
        }

        private void LoadManagementValues()
        {
            var performanceFilePath = GetPerformanceFilePath();
            var doesPerformanceFileExist = performanceFilePath.Exists();

            if (doesPerformanceFileExist)
            {
                var text = System.IO.File.ReadAllText(performanceFilePath.FullPath);

                model = JsonConvert.DeserializeObject<ProjectManagementValues>(text);

                foreach(var element in GlueState.Self.CurrentGlueProject.Entities)
                {
                    var managementValues =
                        model?.EntityManagementValueList?.FirstOrDefault(item => item.Name == element.Name);

                    if(managementValues != null)
                    {
                        if(element.IsManuallyUpdated)
                        {
                            managementValues.PropertyManagementMode = Enums.PropertyManagementMode.SelectManagedProperties;
                        }
                        else
                        {
                            managementValues.PropertyManagementMode = Enums.PropertyManagementMode.FullyManaged;
                        }

                        foreach(var instance in element.AllNamedObjects)
                        {
                            var instanceModel = managementValues.InstanceManagementValuesList
                                .FirstOrDefault(item => item.Name == instance.InstanceName);

                            if(instanceModel != null)
                            {
                                UpdateInstanceValuesToInstance(instanceModel, instance);
                            }
                        }
                    }
                }
            }

            if (model == null)
            {
                model = new ProjectManagementValues();
            }
            variableActivityCodeGenerator.Values = model;
            updateDependenciesCodeGenerator.Values = model;
        }

        private void HandleViewModelValueChanged(string propertyName)
        {
            // early out:
            if (saveOnViewModelChanges == false ||
                GlueState.Self.CurrentGlueProject == null ||
                GlueState.Self.CurrentEntitySave == null)
            {
                return;
            }

            bool shouldRegenerateSelectedEntity = false;
            bool shouldSaveGlux = false;
            if(propertyName == nameof(MainViewModel.SelectedPropertyManagementMode) && viewModel.SelectedInstance != null)
            {
                var instanceName = viewModel.SelectedInstance.Name;

                var namedObject = GlueState.Self.CurrentEntitySave?.GetNamedObjectRecursively(instanceName);

                if(namedObject != null)
                {
                    if(viewModel.SelectedInstance.PropertyManagementMode == Enums.PropertyManagementMode.FullyManaged)
                    {
                        namedObject.IsManuallyUpdated = false;
                    }
                    else
                    {
                        namedObject.IsManuallyUpdated = true;
                    }

                    shouldRegenerateSelectedEntity = true;
                    shouldSaveGlux = true;
                }
            }
            else if(propertyName == nameof(MainViewModel.SelectedPropertyManagementMode) && viewModel.IsRootSelected)
            {
                var entity = GlueState.Self.CurrentEntitySave;
                if(entity != null)
                {
                    if (viewModel.SelectedPropertyManagementMode == Enums.PropertyManagementMode.FullyManaged)
                    {
                        entity.IsManuallyUpdated = false;
                    }
                    else
                    {
                        entity.IsManuallyUpdated = true;
                    }

                    shouldRegenerateSelectedEntity = true;
                    shouldSaveGlux = true;

                    GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();
                }
            }

            if(propertyName == nameof(VelocityPropertyViewModel.IsChecked))
            {
                shouldSaveGlux = false;
                shouldRegenerateSelectedEntity = true;
            }

            if(shouldRegenerateSelectedEntity)
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
            }
            if(shouldSaveGlux)
            {
                GlueCommands.Self.GluxCommands.SaveProjectAndElements();
            }

            SavePerformanceData();

        }

        private void SavePerformanceData(GlueElement entity = null)
        {
            if(viewModel ==null)
            {
                throw new NullReferenceException($"{nameof(viewModel)} is null, entity is {entity?.ToString() ?? "null"}");
            }
            var currentEntityViewModel = ViewModelToModelConverter.ToModel(viewModel);

            model.EntityManagementValueList.RemoveAll(item => item.Name == viewModel.EntityName);
            model.EntityManagementValueList.Add(currentEntityViewModel);

            var serialized = JsonConvert.SerializeObject(model, Formatting.Indented);

            // remove it if it exists:

            FilePath filePath = GetPerformanceFilePath();

            TaskManager.Self.Add(() =>
            {
                System.IO.Directory.CreateDirectory(filePath.GetDirectoryContainingThis().FullPath);

                // Now ignore it for the write:
                GlueCommands.Self.FileCommands.IgnoreNextChangeOnFile(filePath.FullPath);
                // Again again. Not sure why we get 2 writes on Windows, even though we're using FileManager.SaveText.
                GlueCommands.Self.FileCommands.IgnoreNextChangeOnFile(filePath.FullPath);
                GlueCommands.Self.TryMultipleTimes(() => 
                    // According to FileManager.cs, WriteAllText causes 2 file changes to get raised, so instead use the FileManager
                    //System.IO.File.WriteAllText(filePath.FullPath, serialized)
                    FlatRedBall.IO.FileManager.SaveText(serialized, filePath.FullPath)
                );
            },
            $"Saving performance file {filePath}",
            TaskExecutionPreference.Asap);
        }

        private static FilePath GetPerformanceFilePath()
        {
            return GlueState.Self.CurrentGlueProjectDirectory +
                "EntityPerformance.json";
        }


        private void HandleGlueItemSelected(ITreeNode selectedTreeNode)
        {
            var shouldShow = selectedTreeNode?.IsEntityNode() == true || selectedTreeNode?.IsRootNamedObjectNode() == true;

            if (shouldShow)
            {
                if(mainControl == null)
                {
                    mainControl = new MainControl();
                    tab = this.CreateTab(mainControl, "Entity Performance");
                }
                tab.Show();
                RefreshView(GlueState.Self.CurrentEntitySave);
            }
            else
            {
                tab?.Hide();
            }

        }

        private void RefreshView(EntitySave entitySave)
        {
            saveOnViewModelChanges = false;
            {
                entitySave = entitySave ?? GlueState.Self.CurrentEntitySave;
                if (entitySave != null)
                {
                    var entityModel = GetOrCreateEntityManagementValuesFor(entitySave);

                    viewModel = ModelToViewModelConverter.ToViewModel(entityModel);
                    AssignInstancetypesOn(viewModel, entitySave);
                    viewModel.AssignPropertyChangedEventsOnInstanceViewModels();
                    viewModel.AnyValueChanged += HandleViewModelValueChanged;
                    if ( mainControl != null)
                    {
                        mainControl.DataContext = viewModel;
                    }
                }

                if(GlueState.Self.CurrentNamedObjectSave != null)
                {
                    // todo - select the item for convenience
                }
            }
            saveOnViewModelChanges = true;
        }

        /// <summary>
        /// Assigns the instance type onto the instance view models contained in the main view model.
        /// This is needed so that the Glue data is the authority on the type of an instance, rather than
        /// relying on stored values on a model or view model.
        /// </summary>
        /// <param name="viewModel"></param>
        /// <param name="entitySave"></param>
        private void AssignInstancetypesOn(MainViewModel viewModel, EntitySave entitySave)
        {
            var allNamedObjects = entitySave.AllNamedObjects;

            foreach(var instanceViewModel in viewModel.Instances)
            {
                var namedObject = allNamedObjects
                    .FirstOrDefault(item => item.InstanceName == instanceViewModel.Name);

                instanceViewModel.Type = namedObject?.InstanceType;
            }
        }

        private EntityManagementValues GetOrCreateEntityManagementValuesFor(EntitySave entitySave)
        {
            EntityManagementValues values = model.EntityManagementValueList.FirstOrDefault(item => item.Name == entitySave.Name);

            if(values == null)
            {
                values = new EntityManagementValues();
                values.Name = entitySave.Name;
                model.EntityManagementValueList.Add(values);

                foreach(var instance in entitySave.AllNamedObjects)
                {
                    AddInstanceManagementValuesFor(values, instance);
                }
            }

            if(entitySave.IsManuallyUpdated)
            {
                values.PropertyManagementMode = Enums.PropertyManagementMode.SelectManagedProperties;
            }
            else
            {
                values.PropertyManagementMode = Enums.PropertyManagementMode.FullyManaged;
            }

            foreach(var instance in entitySave.AllNamedObjects)
            {
                var instanceValues = values.InstanceManagementValuesList.FirstOrDefault(
                    item => item.Name == instance.InstanceName);

                // Make sure there are no missing instance values
                if (instanceValues == null)
                {
                    // an instance was added, but there is no matching model here:
                    AddInstanceManagementValuesFor(values, instance);

                    instanceValues = values.InstanceManagementValuesList.FirstOrDefault(
                        item => item.Name == instance.InstanceName);
                }

                UpdateInstanceValuesToInstance(instanceValues, instance);
            }

            // Instances can get removed, so let's check for that too:
            for(int i = values.InstanceManagementValuesList.Count- 1; i > -1; i--)
            {

                // Is there a matching item?
                var matchingItem = entitySave.AllNamedObjects.FirstOrDefault(item => item.InstanceName == values.InstanceManagementValuesList[i].Name);
                if(matchingItem == null)
                {
                    values.InstanceManagementValuesList.RemoveAt(i);
                }
            }


            return values;
        }

        private static void UpdateInstanceValuesToInstance(InstanceManagementValues instanceValues, NamedObjectSave instance)
        {
            if (instance.IsManuallyUpdated)
            {
                instanceValues.PropertyManagementMode = Enums.PropertyManagementMode.SelectManagedProperties;
            }
            else
            {
                instanceValues.PropertyManagementMode = Enums.PropertyManagementMode.FullyManaged;
            }

            instanceValues.IsContainer = instance.IsContainer;
        }

        private static void AddInstanceManagementValuesFor(EntityManagementValues values, NamedObjectSave instance)
        {
            var instanceValues = new InstanceManagementValues();

            instanceValues.Name = instance.InstanceName;

            UpdateInstanceValuesToInstance(instanceValues, instance);


            values.InstanceManagementValuesList.Add(instanceValues);
        }
    }
}
