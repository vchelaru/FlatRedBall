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
using PlatformerPluginCore.Logic;
using GlueCommon.Models;

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

        private async void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            /////////// early out ///////////
            if (ignoresPropertyChanges)
            {
                return;
            }
            ///////////// end early out ///////////

            var entity = GlueState.Self.CurrentEntitySave;
            var element = GlueState.Self.CurrentElement;
            var viewModel = sender as PlatformerEntityViewModel;
            bool shouldGenerateCsv, shouldGenerateEntity, shouldAddPlatformerVariables;

            DetermineWhatToGenerate(e.PropertyName, viewModel, 
                out shouldGenerateCsv, out shouldGenerateEntity, out shouldAddPlatformerVariables);

            if (shouldGenerateCsv)
            {

                if(viewModel.IsPlatformer && viewModel.PlatformerValues.Count == 0 &&
                    // don't re-add these if the user removed them. We can tell by looking at the platformer variable
                    e.PropertyName != nameof(PlatformerEntityViewModel.PlatformerValues) )
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

                await GenerateAndAddCsv(entity, viewModel);
            }

            if(shouldAddPlatformerVariables)
            {
                AddPlatformerVariables(entity);
            }

            if (shouldGenerateEntity)
            {
                await TaskManager.Self.AddAsync(
                    () => GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(entity),
                    "Generating " + entity.Name);

            }

            if(shouldAddPlatformerVariables)
            {
                GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();
                if(element != null)
                {
                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element);
                }
            }

            if (shouldGenerateCsv || shouldGenerateEntity || shouldAddPlatformerVariables)
            {
                EnumFileGenerator.Self.GenerateAndSave();
                IPlatformerCodeGenerator.Self.GenerateAndSave();
                PlatformerAnimationControllerGenerator.Self.GenerateAndSave();
                GlueCommands.Self.GluxCommands.SaveGlux();
            }
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

        private static async Task GenerateAndAddCsv(EntitySave entity, PlatformerEntityViewModel viewModel)
        {
            // this could fail so we're going to try multiple times, but we need it immediately because
            // subsequent selections depend on it
            var didGenerate = false;
            try
            {
                await CsvGenerator.Self.GenerateFor(entity, GetIfInheritsFromPlatformer(entity), viewModel);
                didGenerate = true;
            }
            catch(System.IO.IOException ioException)
            {
                GlueCommands.Self.PrintError($"Could not generate CSV for entity {entity}:\n{ioException.Message}");
            }

            if(didGenerate)
            {
                await TaskManager.Self.AddAsync(
                    () =>
                    {
                        string rfsName = entity.Name.Replace("\\", "/") + "/" + CsvGenerator.RelativeCsvFile;
                        bool isAlreadyAdded = entity.ReferencedFiles.FirstOrDefault(item => item.Name == rfsName) != null;

                        if (!isAlreadyAdded)
                        {
                            var newCsvRfs = GlueCommands.Self.GluxCommands.AddSingleFileTo(
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

                            newCsvRfs.HasPublicProperty = true;
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
        }

        public static async Task ForceCsvGenerationFor(EntitySave entitySave)
        {
            // assume this isn't the current entity so we'll create a new VM on the spot and go from there:
            var vm = new PlatformerEntityViewModel();
            UpdateViewModelTo(entitySave, vm);

            var inheritsFromPlatformerEntity = GetIfInheritsFromPlatformer(entitySave);

            // now that we have a prepared VM, generate it
            await CsvGenerator.Self.GenerateFor(entitySave, inheritsFromPlatformerEntity, vm);
        }

        #region Update To / Refresh From Model

        public void UpdateTo(EntitySave currentEntitySave)
        {
            ignoresPropertyChanges = true;

            UpdateViewModelTo(currentEntitySave, viewModel);
            
            // now that they've all been set, += their property change
            foreach (var platformerValuesViewModel in viewModel.PlatformerValues)
            {
                platformerValuesViewModel.PropertyChanged += HandlePlatformerValuesChanged;
            }

            // must be called after refreshing the platformer values...at least that's what the top down controller suggests, so I'm following that here.
            if(IsPlatformer(currentEntitySave))
            {
                PlatformerPluginCore.Controllers.AnimationController.LoadAnimationFilesFromDisk(currentEntitySave);
            }

            ignoresPropertyChanges = false;
        }

        #endregion

        public static bool IsPlatformer(EntitySave entitySave) =>
            entitySave.Properties.GetValue<bool>(nameof(PlatformerEntityViewModel.IsPlatformer));

        static void UpdateViewModelTo(EntitySave entitySave, PlatformerEntityViewModel viewModel)
        {
            viewModel.IsPlatformer = IsPlatformer(entitySave);
            var inheritsFromPlatformerEntity = GetIfInheritsFromPlatformer(entitySave);
            viewModel.InheritsFromPlatformer = inheritsFromPlatformerEntity;

            RefreshPlatformerValues(entitySave, viewModel);

        }

        private static void RefreshPlatformerValues(EntitySave currentEntitySave, PlatformerEntityViewModel viewModel)
        {
            PlatformerValuesCreationLogic.GetCsvValues(currentEntitySave,
                out Dictionary<string, PlatformerValues> csvValues);

            var basePlatformerEntities = ObjectFinder.Self
                .GetAllBaseElementsRecursively(currentEntitySave)
                .Where(item => item.Properties.GetValue<bool>(nameof(viewModel.IsPlatformer)))
                .ToArray();

            viewModel.PlatformerValues.Clear();

            var inheritsFromPlatformer = basePlatformerEntities.Length > 0;

            if(inheritsFromPlatformer)
            {
                foreach(EntitySave entity in basePlatformerEntities)
                {
                    PlatformerValuesCreationLogic.GetCsvValues(entity,
                        out Dictionary<string, PlatformerValues> baseCsvValues);

                    foreach(var value in baseCsvValues.Values)
                    {
                        PlatformerValuesViewModel platformerValuesViewModel = null;

                        var existing = viewModel.PlatformerValues.FirstOrDefault(item => item.Name == value.Name);
                        if(existing != null)
                        {
                            platformerValuesViewModel = existing;
                        }
                        else
                        {
                            platformerValuesViewModel = new PlatformerValuesViewModel();
                            viewModel.PlatformerValues.Add(platformerValuesViewModel);
                        }
                        platformerValuesViewModel.SetFrom(value, inheritsFromPlatformer, 
                            doesBaseEntityHaveSameNamedValues:true);
                        // since it's coming from a derived, force it as "inherits"
                        platformerValuesViewModel.InheritOrOverwrite = InheritOrOverwrite.Inherit;
                    }
                }
            }

            foreach (var value in csvValues.Values)
            {
                var existing = viewModel.PlatformerValues.FirstOrDefault(item => item.Name == value.Name);

                if(existing == null || value.InheritOrOverwrite == InheritOrOverwrite.Overwrite)
                {
                    var platformerValuesViewModel = new PlatformerValuesViewModel();



                    platformerValuesViewModel.SetFrom(value, inheritsFromPlatformer, 
                        doesBaseEntityHaveSameNamedValues:existing != null);

                    if(existing != null)
                    {
                        var index = viewModel.PlatformerValues.IndexOf(existing);
                        viewModel.PlatformerValues.RemoveAt(index);
                        viewModel.PlatformerValues.Insert(index, platformerValuesViewModel);
                    }
                    else
                    {
                        viewModel.PlatformerValues.Add(platformerValuesViewModel);
                    }

                }
            }


        }

        private void HandlePlatformerValuesChanged(object sender, PropertyChangedEventArgs e)
        {

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
