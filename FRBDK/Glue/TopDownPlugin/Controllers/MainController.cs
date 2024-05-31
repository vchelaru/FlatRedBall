using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.IO.Csv;
using GlueCommon.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TopDownPlugin.CodeGenerators;
using TopDownPlugin.Data;
using TopDownPlugin.DataGenerators;
using TopDownPlugin.Logic;
using TopDownPlugin.Models;
using TopDownPlugin.ViewModels;
using TopDownPlugin.Views;
using TopDownPluginCore.CodeGenerators;

namespace TopDownPlugin.Controllers
{
    public class MainController : Singleton<MainController>
    {
        #region Fields

        TopDownEntityViewModel viewModel;
        TopDownAnimationData topDownAnimationData;

        bool ignoresPropertyChanges = false;

        CsvHeader[] lastHeaders;

        const string baseAnimationsName = "Base Animations";


        #endregion

        public MainController()
        {
        }

        public TopDownEntityViewModel GetViewModel()
        {
            if (viewModel == null)
            {
                viewModel = new TopDownEntityViewModel();
                viewModel.PropertyChanged += HandleViewModelPropertyChanged;
            }

            return viewModel;
        }

        public void MakeCurrentEntityTopDown()
        {
            if (viewModel != null)
            {
                viewModel.IsTopDown = true;
            }
        }

        private void AddTopDownGlueVariables(EntitySave entity)
        {
            // We don't make any variables because currently there's no concept of
            // different movement types that the plugin can switch between, the way
            // the platformer switches between ground/air/double-jump

            // Actually even though there's not air, ground, double jump, there is a CurrentMovementValues
            // property. But we'll just codegen that for now.
        }

        private async void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            /////////// early out ///////////
            if (ignoresPropertyChanges)
            {
                return;
            }
            ///////////// end early out ///////////

            var viewModel = sender as TopDownEntityViewModel;
            var entity = viewModel.BackingData;// GlueState.Self.CurrentEntitySave;
            bool shouldGenerateCsv, shouldGenerateEntity, shouldReAddTopDownVariables;
            bool shouldRemoveCsvRfs = false;

            if(viewModel.IsTopDown == false)
            {
                shouldRemoveCsvRfs = true;
            }

            DetermineWhatToGenerate(e.PropertyName, viewModel,
                out shouldGenerateCsv, out shouldGenerateEntity, out shouldReAddTopDownVariables);

            switch (e.PropertyName)
            {
                case nameof(TopDownEntityViewModel.IsTopDown):

                    if(viewModel.IsTopDown && entity.ImplementsICollidable == false)
                    {
                        entity.ImplementsICollidable = true;
                        await GlueCommands.Self.GluxCommands.ElementCommands.ReactToPropertyChanged(entity, nameof(entity.ImplementsICollidable), false);
                    }

                    HandleIsTopDownPropertyChanged(viewModel);
                    break;
                // already handled in a dedicated method
                case nameof(TopDownEntityViewModel.TopDownValues):
                    //RefreshAnimationValues(entity);
                    break;
            }

            if (shouldGenerateCsv)
            {
                if (viewModel.IsTopDown && viewModel.TopDownValues.Count == 0 &&
                    // don't re-add these if the user removed them. We can tell by looking at the platformer variable
                    e.PropertyName != nameof(TopDownEntityViewModel.TopDownValues))
                {
                    var newValues = PredefinedTopDownValues.GetValues("Default");
                    viewModel.TopDownValues.Add(newValues);
                }

                await GenerateAndAddCsv(entity, viewModel);
            }

            if (shouldReAddTopDownVariables)
            {
                AddTopDownGlueVariables(entity);
            }

            if (shouldGenerateEntity)
            {
                await TaskManager.Self.AddAsync(
                    () => GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(entity),
                    "Generating " + entity.Name);
            }

            if (shouldReAddTopDownVariables)
            {
                await TaskManager.Self.AddAsync(() =>
                {
                    GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();
                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(entity);
                }, "Refreshing UI after top down plugin values changed", doOnUiThread:true);
            }

            if (shouldGenerateCsv || shouldGenerateEntity || shouldReAddTopDownVariables)
            {
                GlueCommands.Self.GluxCommands.SaveProjectAndElements();
                await TaskManager.Self.AddAsync(
                    () =>
                    {
                        EnumFileGenerator.Self.GenerateAndSave();
                        InterfacesFileGenerator.Self.GenerateAndSave();
                        if (shouldGenerateCsv || shouldReAddTopDownVariables)
                        {
                            AiCodeGenerator.Self.GenerateAndSave();
                            AiTargetLogicCodeGenerator.Self.GenerateAndSave();
                        }
                        TopDownAnimationControllerGenerator.Self.GenerateAndSave();
                    }, "Generating all top-down code");
            }

            if(shouldRemoveCsvRfs)
            {
                await TaskManager.Self.AddAsync(() =>
                {
                    var csvFile = CsvGenerator.Self.CsvTopdownFileFor(entity);

                    var relativeToProject = csvFile.RelativeTo(GlueState.Self.ContentDirectory);

                    var rfses = entity.ReferencedFiles.Where(item => item.Name == relativeToProject).ToArray();

                    if(rfses.Length > 0)
                    {
                        foreach(var rfs in rfses)
                        {
                            entity.ReferencedFiles.Remove(rfs);
                        }
                        GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(entity);
                        GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(entity);
                        _=GlueCommands.Self.GluxCommands.SaveElementAsync(entity);
                    }



                }, "Removing top down files from entity");
            }

        }

        public void HandleElementRenamed(GlueElement renamedElement, string oldName)
        {
            var oldFile = AnimationController.TopDownAnimationsFileLocationFor(oldName);
            var newFile = AnimationController.TopDownAnimationsFileLocationFor(renamedElement);

            if(oldFile.Exists())
            {
                TaskManager.Self.AddAsync(() =>
                {
                    System.IO.Directory.CreateDirectory(newFile.GetDirectoryContainingThis().FullPath);
                    System.IO.File.Move(oldFile.FullPath, newFile.FullPath);
                }, $"Moving animation file for renamed entity\n{oldFile}->{newFile}");
            }
        }

        private void HandleIsTopDownPropertyChanged(TopDownEntityViewModel viewModel)
        {
            //if (viewModel.IsTopDown &&
            //                    GlueCommands.Self.GluxCommands.GetPluginRequirement(MainPlugin) == false)
            //{
            //    GlueCommands.Self.GluxCommands.SetPluginRequirement(MainPlugin, true);
            //    GlueCommands.Self.PrintOutput("Added Top Down Plugin as a required plugin because the entity was marked as a top down entity");
            //    GlueCommands.Self.GluxCommands.SaveProjectAndElements();
            //}

            if (viewModel.IsTopDown == false)
            {
                CheckForNoTopDownEntities();
            }
        }

        public void CheckForNoTopDownEntities()
        {
            var areAnyEntitiesTopDown =  GlueState.Self.CurrentGlueProject.Entities
                .Any(item => TopDownEntityPropertyLogic.GetIfIsTopDown(item));

            if (!areAnyEntitiesTopDown)
            {
                FilePath absoluteAiFile =
                    GlueState.Self.CurrentGlueProjectDirectory +
                    AiCodeGenerator.Self.RelativeFile;

                TaskManager.Self.Add(() => GlueCommands.Self.ProjectCommands.RemoveFromProjects(absoluteAiFile),
                    "Removing " + AiCodeGenerator.Self.RelativeFile);

                FilePath absoluteLogicFile =
                    GlueState.Self.CurrentGlueProjectDirectory +
                    AiTargetLogicCodeGenerator.Self.RelativeFile;

                TaskManager.Self.Add(() => GlueCommands.Self.ProjectCommands.RemoveFromProjects(absoluteLogicFile),
                    "Removing " + AiTargetLogicCodeGenerator.Self.RelativeFile);

                // todo - probably need to remove all the other files that are created for top down
            }
        }

        private void DetermineWhatToGenerate(string propertyName, TopDownEntityViewModel viewModel, out bool shouldGenerateCsv, out bool shouldGenerateEntity, out bool shouldAddTopDownVariables)
        {
            var entity = viewModel.BackingData;
            shouldGenerateCsv = false;
            shouldGenerateEntity = false;
            shouldAddTopDownVariables = false;
            if (entity != null)
            {
                switch (propertyName)
                {
                    case nameof(TopDownEntityViewModel.IsTopDown):
                        entity.Properties.SetValue(propertyName, viewModel.IsTopDown);
                        // Don't generate a CSV if it's not a top down
                        shouldGenerateCsv = viewModel.IsTopDown;
                        shouldAddTopDownVariables = viewModel.IsTopDown;
                        shouldGenerateEntity = true;
                        break;
                    case nameof(TopDownEntityViewModel.TopDownValues):
                        shouldGenerateCsv = true;
                        // I don't think we need this...yet
                        shouldGenerateEntity = false;
                        shouldAddTopDownVariables = false;
                        break;
                }
            }
        }

        public async Task GenerateAndAddCsv(EntitySave entity, TopDownEntityViewModel viewModel)
        {
            var didGenerate = false;
            try
            {
                await CsvGenerator.Self.GenerateFor(entity, GetIfInheritsFromTopDown(entity), viewModel, lastHeaders);
                didGenerate = true;
            }
            catch (System.IO.IOException ioException)
            {
                GlueCommands.Self.PrintError($"Could not generate CSV for entity {entity}:\n{ioException.Message}");
            }
            if (didGenerate)
            {
                await TaskManager.Self.AddAsync(() =>
                {
                    string rfsName = entity.Name.Replace("\\", "/") + "/" + CsvGenerator.RelativeCsvFile;
                    bool isAlreadyAdded = entity.ReferencedFiles.FirstOrDefault(item => item.Name == rfsName) != null;

                    if (!isAlreadyAdded)
                    {
                        var newCsvRfs = GlueCommands.Self.GluxCommands.AddSingleFileTo(
                            CsvGenerator.Self.CsvTopdownFileFor(entity).FullPath,
                            CsvGenerator.RelativeCsvFile,
                            "",
                            null,
                            false,
                            null,
                            entity,
                            null,
                            selectFileAfterCreation:false
                            );

                        if(newCsvRfs != null)
                        {
                            newCsvRfs.HasPublicProperty = true;
                        }
                    }

                    var rfs = entity.ReferencedFiles.FirstOrDefault(item => item.Name == rfsName);

                    if (rfs != null && rfs.CreatesDictionary == false)
                    {
                        rfs.CreatesDictionary = true;
                        GlueCommands.Self.GluxCommands.SaveProjectAndElements();
                        GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(entity);
                    }

                    const string customClassName = "TopDownValues";
                    if (GlueState.Self.CurrentGlueProject.CustomClasses.Any(item => item.Name == customClassName) == false)
                    {
                        CustomClassSave throwaway;
                        GlueCommands.Self.GluxCommands.AddNewCustomClass(customClassName, out throwaway);
                    }

                    var customClass = GlueState.Self.CurrentGlueProject.CustomClasses
                        .FirstOrDefault(item => item.Name == customClassName);

                    if (rfs != null)
                    {
                        if (customClass != null && customClass.CsvFilesUsingThis.Contains(rfs.Name) == false)
                        {
                            FlatRedBall.Glue.CreatedClass.CustomClassController.Self.SetCsvRfsToUseCustomClass(rfs, customClass, force: true);

                            GlueCommands.Self.GluxCommands.SaveProjectAndElements();
                        }
                    }
                },
                "Adding csv to top down entity"
                );
            }
        }


        #region Update To / Refresh From Model

        public static bool IsTopDown(EntitySave entitySave) => 
            entitySave.Properties.GetValue<bool>(nameof(TopDownEntityViewModel.IsTopDown));

        public bool GetIfInheritsFromTopDown(EntitySave entitySave) =>
            ObjectFinder.Self
                .GetAllBaseElementsRecursively(entitySave)
                .Any(item => item.Properties.GetValue<bool>(nameof(viewModel.IsTopDown)));

        public void UpdateTo(EntitySave currentEntitySave)
        {
            ignoresPropertyChanges = true;

            UpdateViewModelTo(currentEntitySave);


            if(IsTopDown(currentEntitySave))
            {
                if (TopDownPlugin.Controllers.AnimationController.TopDownViewModel == null)
                {
                    TopDownPlugin.Controllers.AnimationController.TopDownViewModel = GetViewModel();
                }
                TopDownPlugin.Controllers.AnimationController.LoadAnimationFilesFromDisk(currentEntitySave);
            }

            ignoresPropertyChanges = false;
        }

        private void UpdateViewModelTo(EntitySave currentEntitySave)
        {
            viewModel.IsTopDown = currentEntitySave.Properties.GetValue<bool>(nameof(viewModel.IsTopDown));
            viewModel.BackingData = currentEntitySave;
            var inheritsFromTopDownEntity = GetIfInheritsFromTopDown(currentEntitySave);
            viewModel.InheritsFromTopDown = inheritsFromTopDownEntity;


            RefreshTopDownValues(currentEntitySave);
        }

        private void HandleSetViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var changedVm = sender as AnimationSetViewModel;

            changedVm.SetValuesOnBackingData();

            SaveCurrentEntitySaveAnimationDataTask();

            GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
        }

        private void SaveCurrentEntitySaveAnimationDataTask()
        {
            // This was saving the old animation, need to save the new animation:

            //var filePath = GetAnimationFilePathFor(viewModel.BackingData);

            //TaskManager.Self.Add(() =>
            //{
            //    var contents = JsonConvert.SerializeObject(topDownAnimationData);

            //    GlueCommands.Self.TryMultipleTimes(() =>
            //    {
            //        System.IO.Directory.CreateDirectory(
            //            filePath.GetDirectoryContainingThis().FullPath);
            //        System.IO.File.WriteAllText(filePath.FullPath, contents);
            //        GlueCommands.Self.PrintOutput($"Saved animation file {filePath.FullPath}");
            //    });

            //}, $"Saving animation file for {viewModel.BackingData}");
        }


        private void RefreshTopDownValues(EntitySave currentEntitySave)
        {
            TopDownValuesCreationLogic.GetCsvValues(currentEntitySave,
                out Dictionary<string, TopDownValues> csvValues,
                out List<Type> additionalValueTypes,
                out CsvHeader[] csvHeaders);


            List<CsvHeader> tempList = csvHeaders?.ToList() ?? new List<CsvHeader>();
            //bool ContainsHeader(string name)
            //{
            //    return tempList.Any(item => item.Name == name);
            //}

            // What if a CSV is missing required headers? We don't want to include those in the "last" list right?
            //foreach(var requirement in TopDownValuesCreationLogic.RequiredCsvHeaders)
            //{
            //    if(!ContainsHeader(requirement.Name))
            //    {
            //        tempList.Add(requirement);
            //    }
            //}

            lastHeaders = tempList.ToArray();

            viewModel.TopDownValues.Clear();

            var baseTopDownEntities = ObjectFinder.Self
                .GetAllBaseElementsRecursively(currentEntitySave)
                .Where(item => item.Properties.GetValue<bool>(nameof(viewModel.IsTopDown)))
                .ToArray();

            var inheritsFromTopDown = baseTopDownEntities.Length > 0;

            if(inheritsFromTopDown)
            {
                foreach(EntitySave entity in baseTopDownEntities)
                {
                    TopDownValuesCreationLogic.GetCsvValues(entity,
                        out Dictionary<string, TopDownValues> baseCsvValues,
                        out List<Type> baseAdditionalValueTypes,
                        out CsvHeader[] baseCsvHeaders);

                    foreach(var value in baseCsvValues.Values)
                    {
                        TopDownValuesViewModel topDownValuesViewModel = null;

                        var existing = viewModel.TopDownValues.FirstOrDefault(item => item.Name == value.Name);
                        if(existing != null)
                        {
                            topDownValuesViewModel = existing;
                        }
                        else
                        {
                            topDownValuesViewModel = new TopDownValuesViewModel();
                            viewModel.TopDownValues.Add(topDownValuesViewModel);
                        }
                        topDownValuesViewModel.SetFrom(value, additionalValueTypes, inheritsFromTopDown);
                        // since it's coming from a derived, force it as "inherits"
                        topDownValuesViewModel.InheritOrOverwrite = InheritOrOverwrite.Inherit;
                    }
                }
            }

            foreach (var value in csvValues.Values)
            {
                var existing = viewModel.TopDownValues.FirstOrDefault(item => item.Name == value.Name);

                if(existing == null || value.InheritOrOverwrite == InheritOrOverwrite.Overwrite)
                {
                    var topDownValuesViewModel = new TopDownValuesViewModel();
                    topDownValuesViewModel.SetFrom(value, additionalValueTypes, inheritsFromTopDown);

                    if(existing != null)
                    {
                        var index = viewModel.TopDownValues.IndexOf(existing);
                        viewModel.TopDownValues.RemoveAt(index);
                        viewModel.TopDownValues.Insert(index, topDownValuesViewModel);
                    }
                    else
                    {
                        viewModel.TopDownValues.Add(topDownValuesViewModel);
                    }
                }
            }

            // now that they've all been set, += their property change
            foreach(var topDownValuesViewModel in viewModel.TopDownValues)
            {
                topDownValuesViewModel.PropertyChanged += HandleTopDownValuesViewModelChanged;
            }
        }

        #endregion

        private void HandleTopDownValuesViewModelChanged(object sender, PropertyChangedEventArgs e)
        {
            var senderAsViewModel = (TopDownValuesViewModel)sender;


            switch(e.PropertyName)
            {
                case nameof(TopDownValuesViewModel.Name):
                    var backingData = senderAsViewModel.BackingData;

                    var animationToRename = topDownAnimationData
                        .Animations
                        .FirstOrDefault(item => item.MovementValuesName == backingData.Name);

                    if(animationToRename != null)
                    {
                        animationToRename.MovementValuesName = senderAsViewModel.Name;

                        SaveCurrentEntitySaveAnimationDataTask();
                    }
                    // todo - regenerate

                    break;
            }
        }

    }
}
