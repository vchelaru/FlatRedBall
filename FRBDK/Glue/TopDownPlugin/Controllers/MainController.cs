using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO.Csv;
using System;
using System.Collections.Generic;
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

namespace TopDownPlugin.Controllers
{
    public class MainController : Singleton<MainController>
    {
        #region Fields

        TopDownEntityViewModel viewModel;
        MainEntityView mainControl;

        bool ignoresPropertyChanges = false;

        public PluginBase MainPlugin { get; set; }

        CsvHeader[] lastHeaders;

        #endregion

        public MainController()
        {
        }

        public MainEntityView GetControl()
        {
            if (mainControl == null)
            {
                viewModel = new TopDownEntityViewModel();
                viewModel.PropertyChanged += HandleViewModelPropertyChange;
                mainControl = new MainEntityView();

                mainControl.DataContext = viewModel;
            }

            return mainControl;
        }

        private void HandleViewModelPropertyChange(object sender, PropertyChangedEventArgs e)
        {
            /////////// early out ///////////
            if (ignoresPropertyChanges)
            {
                return;
            }
            ///////////// end early out ///////////

            var entity = GlueState.Self.CurrentEntitySave;
            var viewModel = sender as TopDownEntityViewModel;
            bool shouldGenerateCsv, shouldGenerateEntity, shouldAddTopDownVariables;

            DetermineWhatToGenerate(e.PropertyName, viewModel,
                out shouldGenerateCsv, out shouldGenerateEntity, out shouldAddTopDownVariables);

            if(e.PropertyName == nameof(TopDownEntityViewModel.IsTopDown))
            {
                HandleIsTopDownPropertyChanged(viewModel);
            }

            if (shouldGenerateCsv)
            {
                if(viewModel.IsTopDown && viewModel.TopDownValues.Count == 0)
                {
                    var newValues = PredefinedTopDownValues.GetValues("Default");
                    viewModel.TopDownValues.Add(newValues);
                }

                GenerateCsv(entity, viewModel);
            }

            if (shouldAddTopDownVariables)
            {
                AddTopDownVariables(entity);
            }

            if (shouldGenerateEntity)
            {
                TaskManager.Self.AddSync(
                    () => GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(entity),
                    "Generating " + entity.Name);
            }

            if (shouldAddTopDownVariables)
            {
                TaskManager.Self.AddSync(() =>
                {
                    GlueCommands.Self.DoOnUiThread(() =>
                    {
                        GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();
                        GlueCommands.Self.RefreshCommands.RefreshUiForSelectedElement();
                    });

                }, "Refreshing UI after top down plugin values changed");
            }

            if (shouldGenerateCsv || shouldGenerateEntity || shouldAddTopDownVariables)
            {
                TaskManager.Self.AddAsyncTask(
                    () =>
                    {
                        GlueCommands.Self.GluxCommands.SaveGlux();
                        EnumFileGenerator.Self.GenerateAndSaveEnumFile();
                        InterfacesFileGenerator.Self.GenerateAndSave();
                        if(shouldGenerateCsv || shouldAddTopDownVariables)
                        {
                            AiCodeGenerator.Self.GenerateAndSave();
                        }
                    },"Saving Glue Project");
            }

        }

        private void HandleIsTopDownPropertyChanged(TopDownEntityViewModel viewModel)
        {
            if (viewModel.IsTopDown &&
                                GlueCommands.Self.GluxCommands.GetPluginRequirement(MainPlugin) == false)
            {
                GlueCommands.Self.GluxCommands.SetPluginRequirement(MainPlugin, true);
                GlueCommands.Self.PrintOutput("Added Top Down Plugin as a required plugin because the entity was marked as a top down entity");
                GlueCommands.Self.GluxCommands.SaveGluxTask();
            }

            if(viewModel.IsTopDown == false)
            {
                var areAnyEntitiesTopDown = GlueState.Self.CurrentGlueProject.Entities
                    .Any(item => TopDownEntityPropertyLogic.GetIfIsTopDown(item));

                if(!areAnyEntitiesTopDown)
                {
                    FilePath absoluteFile =
                        GlueState.Self.CurrentGlueProjectDirectory +
                        AiCodeGenerator.RelativeFile;

                    TaskManager.Self.Add(() => GlueCommands.Self.ProjectCommands.RemoveFromProjects(absoluteFile),
                        "Removing " + AiCodeGenerator.RelativeFile);
                }
            }
        }

        private void DetermineWhatToGenerate(string propertyName, TopDownEntityViewModel viewModel, out bool shouldGenerateCsv, out bool shouldGenerateEntity, out bool shouldAddTopDownVariables)
        {
            var entity = GlueState.Self.CurrentEntitySave;
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

        private void GenerateCsv(EntitySave entity, TopDownEntityViewModel viewModel)
        {
            TaskManager.Self.Add(
                                () => CsvGenerator.Self.GenerateFor(entity, viewModel, lastHeaders),
                                "Generating Top Down CSV for " + entity.Name);


            TaskManager.Self.Add(() =>
            {
                string rfsName = entity.Name.Replace("\\", "/") + "/" + CsvGenerator.RelativeCsvFile;
                bool isAlreadyAdded = entity.ReferencedFiles.FirstOrDefault(item => item.Name == rfsName) != null;

                if (!isAlreadyAdded)
                {
                    GlueCommands.Self.GluxCommands.AddSingleFileTo(
                        CsvGenerator.Self.CsvFileFor(entity).FullPath,
                        CsvGenerator.RelativeCsvFile,
                        "",
                        null,
                        false,
                        null,
                        entity,
                        null
                        );
                }

                var rfs = entity.ReferencedFiles.FirstOrDefault(item => item.Name == rfsName);

                if (rfs != null && rfs.CreatesDictionary == false)
                {
                    rfs.CreatesDictionary = true;
                    GlueCommands.Self.GluxCommands.SaveGlux();
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
                        FlatRedBall. Glue.CreatedClass.CustomClassController.Self.SetCsvRfsToUseCustomClass(rfs, customClass, force: true);

                        GlueCommands.Self.GluxCommands.SaveGlux();
                    }
                }
            },
            "Adding csv to top down entity"
            );
        }

        private void AddTopDownVariables(EntitySave entity)
        {
            // We don't make any variables because currently there's no concept of
            // different movement types that the plugin can switch between, the way
            // the platformer switches between ground/air/double-jump
        }

        internal void UpdateTo(EntitySave currentEntitySave)
        {
            ignoresPropertyChanges = true;

            viewModel.IsTopDown = currentEntitySave.Properties.GetValue<bool>(nameof(viewModel.IsTopDown));

            TopDownValuesCreationLogic.GetCsvValues(currentEntitySave,
                out Dictionary<string, TopDownValues> csvValues,
                out List<Type> additionalValueTypes,
                out CsvHeader[] csvHeaders);

            lastHeaders = csvHeaders;

            viewModel.TopDownValues.Clear();

            foreach(var value in csvValues.Values)
            {
                var topDownValuesViewModel = new TopDownValuesViewModel();

                topDownValuesViewModel.PropertyChanged += HandleTopDownValuesChanged;

                topDownValuesViewModel.SetFrom(value, additionalValueTypes);

                viewModel.TopDownValues.Add(topDownValuesViewModel);
            }

            ignoresPropertyChanges = false;
        }

        private void HandleTopDownValuesChanged(object sender, PropertyChangedEventArgs e)
        {
            
        }

    }
}
