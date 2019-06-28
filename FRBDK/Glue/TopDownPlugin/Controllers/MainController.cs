using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO.Csv;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TopDownPlugin.CodeGenerators;
using TopDownPlugin.DataGenerators;
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

            if (shouldGenerateCsv)
            {
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
                    },"Saving Glue Project");
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

        private static void GenerateCsv(EntitySave entity, TopDownEntityViewModel viewModel)
        {
            TaskManager.Self.Add(
                                () => CsvGenerator.Self.GenerateFor(entity, viewModel),
                                "Generating Top Down CSV for " + entity.Name);


            TaskManager.Self.Add(
                () =>
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
            // the top down plugin switches between ground/air/double-jump
        }

        internal void UpdateTo(EntitySave currentEntitySave)
        {
            ignoresPropertyChanges = true;

            viewModel.IsTopDown = currentEntitySave.Properties.GetValue<bool>(nameof(viewModel.IsTopDown));

            var csvValues = GetCsvValues(currentEntitySave);

            viewModel.TopDownValues.Clear();

            foreach(var value in csvValues.Values)
            {
                var topDownValuesViewModel = new TopDownValuesViewModel();

                topDownValuesViewModel.PropertyChanged += HandleTopDownValuesChanged;

                topDownValuesViewModel.SetFrom(value);

                viewModel.TopDownValues.Add(topDownValuesViewModel);
            }

            ignoresPropertyChanges = false;
        }

        private void HandleTopDownValuesChanged(object sender, PropertyChangedEventArgs e)
        {

        }

        private static Dictionary<string, TopDownValues> GetCsvValues(EntitySave currentEntitySave)
        {
            var csvValues = new Dictionary<string, TopDownValues>();
            var filePath = CsvGenerator.Self.CsvFileFor(currentEntitySave);

            bool doesFileExist = filePath.Exists();

            if (doesFileExist)
            {
                try
                {
                    CsvFileManager.CsvDeserializeDictionary<string, TopDownValues>(filePath.FullPath, csvValues);
                }
                catch (Exception e)
                {
                    PluginManager.ReceiveError("Error trying to load top down csv:\n" + e.ToString());
                }
            }

            return csvValues;
        }

    }
}
