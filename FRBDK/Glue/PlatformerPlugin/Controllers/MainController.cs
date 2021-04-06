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
using FlatRedBall.Glue.Elements;
using FlatRedBall.PlatformerPlugin.Data;

namespace FlatRedBall.PlatformerPlugin.Controllers
{
    public class MainController : Singleton<MainController>
    {
        #region Fields

        PlatformerEntityViewModel viewModel;

        bool ignoresPropertyChanges = false;

        #endregion

        public MainController()
        {
        }

        public PlatformerEntityViewModel GetViewModel()
        {
            if(viewModel == null)
            {
                viewModel = new PlatformerEntityViewModel();
                viewModel.PropertyChanged += HandleViewModelPropertyChanged;
            }

            return viewModel;
        }

        public void MakeCurrentEntityPlatformer()
        {
            if(viewModel != null)
            {
                viewModel.IsPlatformer = true;
            }
        }

        private void AddPlatformerVariables(EntitySave entity)
        {

            const string GroundMovement = "GroundMovement";
            const string AirMovement = "AirMovement";
            const string AfterDoubleJump = "AfterDoubleJump";


            // assign Ground in PlatformerValues.csv and...
            // Air in PlatformerValues.csv, unless it has a static name....do we care? I guess we can just use the current name, that's easy enough, no need to make it more complex

            {

                bool alreadyHasVariable = entity.CustomVariables.Any(
                    item => item.Name == GroundMovement) || GetIfInheritsFromPlatformer(entity);
                if(!alreadyHasVariable)
                {
                    CustomVariable newVariable = new CustomVariable();
                    newVariable.Type = GlueState.Self.ProjectNamespace + ".DataTypes.PlatformerValues";
                    newVariable.Name = GroundMovement;
                    newVariable.CreatesEvent = true;
                    newVariable.SetByDerived = true;

                    newVariable.DefaultValue = $"Ground in {CsvGenerator.StrippedCsvFile}.csv";

                    entity.CustomVariables.Add(newVariable);
                }
            }

            {
                bool alreadyHasVariable = entity.CustomVariables.Any(
                    item => item.Name == AirMovement) || GetIfInheritsFromPlatformer(entity);
                if(!alreadyHasVariable)
                {
                    CustomVariable newVariable = new CustomVariable();
                    newVariable.Type = GlueState.Self.ProjectNamespace + ".DataTypes.PlatformerValues";
                    newVariable.Name = AirMovement;
                    newVariable.CreatesEvent = true;
                    newVariable.SetByDerived = true;

                    newVariable.DefaultValue = $"Air in {CsvGenerator.StrippedCsvFile}.csv";

                    entity.CustomVariables.Add(newVariable);
                }
            }

            {
                bool alreadyHasVariable = entity.CustomVariables.Any(
                    item => item.Name == AfterDoubleJump) || GetIfInheritsFromPlatformer(entity);
                if(!alreadyHasVariable)
                {
                    CustomVariable newVariable = new CustomVariable();
                    newVariable.Type = GlueState.Self.ProjectNamespace + ".DataTypes.PlatformerValues";
                    newVariable.Name = AfterDoubleJump;
                    newVariable.CreatesEvent = true;
                    newVariable.SetByDerived = true;

                    entity.CustomVariables.Add(newVariable);
                }
            }
        }

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
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

                if(viewModel.IsPlatformer && viewModel.PlatformerValues.Count == 0)
                {
                    // ignore changes while adding these two, otherwise this function becomes recursive
                    // resulting in the same CSV being added multiple times:
                    ignoresPropertyChanges = true;
                    viewModel.PlatformerValues.Add(
                        PredefinedPlatformerValues.GetValues("Ground"));

                    viewModel.PlatformerValues.Add(
                        PredefinedPlatformerValues.GetValues("Air"));
                    ignoresPropertyChanges = false;

                }

                GenerateCsv(entity, viewModel);
            }

            if(shouldAddPlatformerVariables)
            {
                AddPlatformerVariables(entity);
            }

            if (shouldGenerateEntity)
            {
                TaskManager.Self.Add(
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
                GlueCommands.Self.GluxCommands.SaveGlux();
            }
        }

        private static void GenerateCsv(EntitySave entity, PlatformerEntityViewModel viewModel)
        {
            TaskManager.Self.Add(
                            () => CsvGenerator.Self.GenerateFor(entity, GetIfInheritsFromPlatformer(entity), viewModel),
                            "Generating Platformer CSV for " + entity.Name);


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
                            null,
                            selectFileAfterCreation:false
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

                    if (rfs != null)
                    {
                        if (customClass != null && customClass.CsvFilesUsingThis.Contains(rfs.Name) == false)
                        {
                            Glue.CreatedClass.CustomClassController.Self.SetCsvRfsToUseCustomClass(rfs, customClass, force: true);

                            GlueCommands.Self.GluxCommands.SaveGlux();
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

        public void UpdateTo(EntitySave currentEntitySave)
        {
            ignoresPropertyChanges = true;

            viewModel.IsPlatformer = currentEntitySave.Properties.GetValue<bool>(nameof(viewModel.IsPlatformer));

            GetCsvValues(currentEntitySave, out Dictionary<string, PlatformerValues> csvValues);

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

        private static void GetCsvValues(EntitySave currentEntitySave,
            out Dictionary<string, PlatformerValues> csvValues)
        {
            csvValues = new Dictionary<string, PlatformerValues>();
            var filePath = CsvGenerator.Self.CsvFileFor(currentEntitySave);

            if (filePath.Exists())
            {
                try
                {
                    CsvFileManager.CsvDeserializeDictionary<string, PlatformerValues>(filePath.FullPath, csvValues);
                }
                catch(Exception e)
                {
                    PluginManager.ReceiveError("Error trying to load platformer csv:\n" + e.ToString());
                }
            }

        }

        static bool GetIfIsPlatformer(IElement element)
        {
            return element.Properties
                .GetValue<bool>(nameof(PlatformerEntityViewModel.IsPlatformer));
        }

        static bool GetIfInheritsFromPlatformer(IElement element)
        {
            if (string.IsNullOrEmpty(element.BaseElement))
            {
                return false;
            }

            var allBase = ObjectFinder.Self.GetAllBaseElementsRecursively(element);

            return allBase.Any(GetIfIsPlatformer);
        }
    }
}
