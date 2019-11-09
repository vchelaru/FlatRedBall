using FlatRedBall.Glue.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.PlatformerPlugin.Views;
using FlatRedBall.PlatformerPlugin.ViewModels;
using System.ComponentModel;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO.Csv;
using FlatRedBall.PlatformerPlugin.SaveClasses;
using FlatRedBall.PlatformerPlugin.Generators;
using FlatRedBall.Glue.Plugins;

namespace FlatRedBall.PlatformerPlugin.Controllers
{
    public class MainController : Singleton<MainController>
    {
        #region Fields

        PlatformerEntityViewModel viewModel;
        MainControl mainControl;

        bool ignoresPropertyChanges = false;

        #endregion

        public MainController()
        {
        }

        public MainControl GetControl()
        {
            if(mainControl == null)
            {
                viewModel = new PlatformerEntityViewModel();
                viewModel.PropertyChanged += HandleViewModelPropertyChange;
                mainControl = new MainControl();

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
            var viewModel = sender as PlatformerEntityViewModel;
            bool shouldGenerateCsv, shouldGenerateEntity, shouldAddPlatformerVariables;
            DetermineWhatToGenerate(e.PropertyName, viewModel, 
                out shouldGenerateCsv, out shouldGenerateEntity, out shouldAddPlatformerVariables);

            if (shouldGenerateCsv)
            {
                GenerateCsv(entity, viewModel);
            }

            if(shouldAddPlatformerVariables)
            {
                AddPlatformerVariables(entity);
            }

            if (shouldGenerateEntity)
            {
                TaskManager.Self.AddAsyncTask(
                    () => GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(entity),
                    "Generating " + entity.Name);

            }

            if(shouldAddPlatformerVariables)
            {
                GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();
                GlueCommands.Self.RefreshCommands.RefreshUiForSelectedElement();
            }

            if (shouldGenerateCsv || shouldGenerateEntity || shouldAddPlatformerVariables)
            {
                EnumFileGenerator.Self.GenerateAndSaveEnumFile();
                TaskManager.Self.AddAsyncTask(
                    () => GlueCommands.Self.GluxCommands.SaveGlux(),
                    "Saving Glue Project");
            }
        }

        private void AddPlatformerVariables(EntitySave entity)
        {

            const string GroundMovement = "GroundMovement";
            const string AirMovement = "AirMovement";
            const string AfterDoubleJump = "AfterDoubleJump";
            {

                bool alreadyHasVariable = entity.CustomVariables.Any(
                    item => item.Name == GroundMovement);
                if(!alreadyHasVariable)
                {
                    CustomVariable newVariable = new CustomVariable();
                    newVariable.Type = GlueState.Self.ProjectNamespace + ".DataTypes.PlatformerValues";
                    newVariable.Name = GroundMovement;
                    newVariable.CreatesEvent = true;
                    entity.CustomVariables.Add(newVariable);
                }
            }

            {
                bool alreadyHasVariable = entity.CustomVariables.Any(
                    item => item.Name == AirMovement);
                if(!alreadyHasVariable)
                {
                    CustomVariable newVariable = new CustomVariable();
                    newVariable.Type = GlueState.Self.ProjectNamespace + ".DataTypes.PlatformerValues";
                    newVariable.Name = AirMovement;
                    newVariable.CreatesEvent = true;

                    entity.CustomVariables.Add(newVariable);
                }
            }

            {
                bool alreadyHasVariable = entity.CustomVariables.Any(
                    item => item.Name == AfterDoubleJump);
                if(!alreadyHasVariable)
                {
                    CustomVariable newVariable = new CustomVariable();
                    newVariable.Type = GlueState.Self.ProjectNamespace + ".DataTypes.PlatformerValues";
                    newVariable.Name = AfterDoubleJump;
                    newVariable.CreatesEvent = true;

                    entity.CustomVariables.Add(newVariable);
                }
            }
        }

        private static void GenerateCsv(EntitySave entity, PlatformerEntityViewModel viewModel)
        {
            TaskManager.Self.AddAsyncTask(
                                () => CsvGenerator.Self.GenerateFor(entity, viewModel),
                                "Generating Platformer CSV for " + entity.Name);


            TaskManager.Self.AddAsyncTask(
                () =>
                {
                    string rfsName = entity.Name.Replace("\\", "/") + "/" + CsvGenerator.RelativeCsvFile;
                    bool isAlreadyAdded = entity.ReferencedFiles.FirstOrDefault(item => item.Name == rfsName) != null;

                    if (!isAlreadyAdded)
                    {
                        GlueCommands.Self.GluxCommands.AddSingleFileTo(
                            CsvGenerator.Self.CsvFileFor(entity),
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

                    const string customClassName = "PlatformerValues";
                    if (GlueState.Self.CurrentGlueProject.CustomClasses.Any(item => item.Name == customClassName) == false)
                    {
                        CustomClassSave throwaway;
                        GlueCommands.Self.GluxCommands.AddNewCustomClass(customClassName, out throwaway);
                    }

                    var customClass = GlueState.Self.CurrentGlueProject.CustomClasses
                        .FirstOrDefault(item => item.Name == customClassName);

                    if (customClass != null && customClass.CsvFilesUsingThis.Contains(rfs.Name) == false)
                    {
                        if (rfs != null)
                        {
                            Glue.CreatedClass.CustomClassController.Self.SetCsvRfsToUseCustomClass(rfs, customClass, force: true);
                        }
                    }
                },
                "Adding csv to platformer entity"
                );
        }

        private static void DetermineWhatToGenerate(string propertyName, PlatformerEntityViewModel viewModel, 
            out bool shouldGenerateCsv, out bool shouldGenerateEntity, out bool shouldAddMovementVariables)
        {
            var entity = GlueState.Self.CurrentEntitySave;
            shouldGenerateCsv = false;
            shouldGenerateEntity = false;
            shouldAddMovementVariables = false;
            if (entity != null)
            {
                switch (propertyName)
                {   
                    case nameof(PlatformerEntityViewModel.IsPlatformer):
                        entity.Properties.SetValue(propertyName, viewModel.IsPlatformer);
                        // Don't generate a CSV if it's not a platformer
                        shouldGenerateCsv = viewModel.IsPlatformer;
                        shouldAddMovementVariables = viewModel.IsPlatformer;
                        shouldGenerateEntity = true;
                        break;
                    case nameof(PlatformerEntityViewModel.PlatformerValues):
                        shouldGenerateCsv = true;
                        // I don't think we need this...yet
                        shouldGenerateEntity = false;
                        shouldAddMovementVariables = false;
                        break;
                }
            }
        }

        internal void UpdateTo(EntitySave currentEntitySave)
        {
            ignoresPropertyChanges = true;

            viewModel.IsPlatformer = currentEntitySave.Properties.GetValue<bool>(nameof(viewModel.IsPlatformer));

            var csvValues = GetCsvValues(currentEntitySave);

            viewModel.PlatformerValues.Clear();

            foreach (var value in csvValues.Values)
            {
                var platformerValuesViewModel = new PlatformerValuesViewModel();

                platformerValuesViewModel.PropertyChanged += HandlePlatformerValuesChanged;

                platformerValuesViewModel.SetFrom(value);

                viewModel.PlatformerValues.Add(platformerValuesViewModel);
            }
            
            ignoresPropertyChanges = false;
        }

        private void HandlePlatformerValuesChanged(object sender, PropertyChangedEventArgs e)
        {

        }

        private static Dictionary<string, PlatformerValues> GetCsvValues(EntitySave currentEntitySave)
        {
            Dictionary<string, PlatformerValues> csvValues = new Dictionary<string, PlatformerValues>();
            string absoluteFileName = CsvGenerator.Self.CsvFileFor(currentEntitySave);

            bool doesFileExist = System.IO.File.Exists(absoluteFileName);

            if (doesFileExist)
            {
                try
                {
                    CsvFileManager.CsvDeserializeDictionary<string, PlatformerValues>(absoluteFileName, csvValues);
                }
                catch(Exception e)
                {
                    PluginManager.ReceiveError("Error trying to load platformer csv:\n" + e.ToString());
                }
            }

            return csvValues;
        }
    }
}
