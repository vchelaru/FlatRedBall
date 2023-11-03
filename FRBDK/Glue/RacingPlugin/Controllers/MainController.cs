using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using RacingPlugin.CodeGenerators;
using RacingPlugin.DataGenerators;
using RacingPlugin.ViewModels;
using RacingPlugin.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RacingPlugin.Controllers
{
    public class MainController : Singleton<MainController>
    {
        #region Fields

        RacingEntityViewModel viewModel;
        MainEntityView mainControl;

        bool ignoresPropertyChanges = false;

        public PluginBase MainPlugin { get; set; }

        #endregion

        public MainController()
        {

        }

        public MainEntityView GetControl()
        {
            if(mainControl == null)
            {
                viewModel = new RacingEntityViewModel();
                viewModel.PropertyChanged += HandleViewModelPropertyChange;
                mainControl = new MainEntityView();

                mainControl.DataContext = viewModel;
            }

            return mainControl;
        }

        private void HandleViewModelPropertyChange(object sender, PropertyChangedEventArgs e)
        {
            ///////////// early out ////////////
            if(ignoresPropertyChanges)
            {
                return;
            }
            ///////////end early out/////////////

            var entity = GlueState.Self.CurrentEntitySave;
            var viewModel = sender as RacingEntityViewModel;

            bool shouldGenerateCsv, shouldGenerateEntity, shouldAddRacingVariables;

            DetermineWhatToGenerate(e.PropertyName, viewModel,
                out shouldGenerateCsv, out shouldGenerateEntity, out shouldAddRacingVariables);

            if (e.PropertyName == nameof(RacingEntityViewModel.IsRacingEntity))
            {
                if (viewModel.IsRacingEntity &&
                    GlueCommands.Self.GluxCommands.GetPluginRequirement(MainPlugin) == false)
                {
                    GlueCommands.Self.GluxCommands.SetPluginRequirement(MainPlugin, true);
                    GlueCommands.Self.PrintOutput("Added Racing Plugin as a required plugin because the entity was marked as a racing entity");
                    GlueCommands.Self.GluxCommands.SaveProjectAndElements();
                }
            }

            if (shouldGenerateCsv)
            {
                GenerateCsv(entity, viewModel);
            }

            if (shouldAddRacingVariables)
            {
                AddRacingVariables(entity);

                TaskManager.Self.Add(AddCollisionHistoryFile, "Adding CollisionHistory.cs");
            }

            if (shouldGenerateEntity)
            {
                TaskManager.Self.Add(
                    () => GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(entity),
                    "Generating " + entity.Name);
            }


            if (shouldAddRacingVariables)
            {
                TaskManager.Self.Add(() =>
                {
                    GlueCommands.Self.DoOnUiThread(() =>
                    {
                        GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();
                        GlueCommands.Self.RefreshCommands.RefreshCurrentElementTreeNode();
                    });

                }, "Refreshing UI after racing plugin values changed");
            }

            if (shouldGenerateCsv || shouldGenerateEntity || shouldAddRacingVariables)
            {
                GlueCommands.Self.GluxCommands.SaveProjectAndElements();
                TaskManager.Self.Add(
                    () =>
                    {
                        EnumFileGenerator.Self.GenerateAndSaveEnumFile();

                        // not sure if this needs any interfaces
                        //InterfacesFileGenerator.Self.GenerateAndSave();

                        // AI...eventually...
                        //AiCodeGenerator.Self.GenerateAndSave();
                    }, "Generating Rading Enums");
            }

        }

        public void AddCollisionHistoryFile()
        {
            var filePath = CollisionHistoryCodeGenerator.Self.GetFilePath();
            GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(filePath, save:false);

            var contents = CollisionHistoryCodeGenerator.Self.GetFileContents();
            GlueCommands.Self.TryMultipleTimes(() =>
            {
                System.IO.File.WriteAllText(filePath.FullPath, contents);
            });


        }

        private void DetermineWhatToGenerate(string propertyName, RacingEntityViewModel viewModel, out bool shouldGenerateCsv, out bool shouldGenerateEntity, out bool shouldAddTopDownVariables)
        {
            var entity = GlueState.Self.CurrentEntitySave;
            shouldGenerateCsv = false;
            shouldGenerateEntity = false;
            shouldAddTopDownVariables = false;
            if (entity != null)
            {
                switch (propertyName)
                {
                    case nameof(RacingEntityViewModel.IsRacingEntity):
                        // Don't generate a CSV if it's not a top down
                        shouldGenerateCsv = viewModel.IsRacingEntity;
                        shouldAddTopDownVariables = viewModel.IsRacingEntity;
                        shouldGenerateEntity = true;
                        break;
                    //case nameof(TopDownEntityViewModel.TopDownValues):
                    //    shouldGenerateCsv = true;
                    //    // I don't think we need this...yet
                    //    shouldGenerateEntity = false;
                    //    shouldAddTopDownVariables = false;
                    //    break;
                }
            }
        }

        private static void GenerateCsv(EntitySave entity, RacingEntityViewModel viewModel)
        {
            TaskManager.Self.Add(
                    () => CsvGenerator.Self.GenerateFor(entity, viewModel),
                    "Generating Racing CSV for " + entity.Name);

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
                    GlueCommands.Self.GluxCommands.SaveProjectAndElements();
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(entity);
                }

                const string customClassName = "RacingEntityValues";
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
            "Adding csv to racing entity");
        }

        private void AddRacingVariables(EntitySave entity)
        {
            const string variableName = "CarData";

            var alreadyHasVariable = entity.CustomVariables.Any(
                item => item.Name == variableName);

            if(!alreadyHasVariable)
            {
                var newVariable = new CustomVariable();
                newVariable.Type = GlueState.Self.ProjectNamespace + ".DataTypes.RacingEntityValues";
                newVariable.Name = variableName;
                newVariable.CreatesEvent = false;
                newVariable.DefaultValue = "DefaultValues in RacingEntityValues.csv";
                entity.CustomVariables.Add(newVariable);

            }
        }

        internal void UpdateTo(EntitySave currentEntitySave)
        {
            ignoresPropertyChanges = true;

            viewModel.GlueObject = currentEntitySave;
            viewModel.UpdateFromGlueObject();

            ignoresPropertyChanges = false;
        }

    }
}
