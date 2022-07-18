using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Controls;
using EntityInputMovementPlugin.ViewModels;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.FormHelpers;
using System.Threading.Tasks;
using PlatformerPluginCore.Views;

namespace EntityInputMovementPlugin
{
    [Export(typeof(PluginBase))]
    public class MainEntityInputMovementPlugin : PluginBase
    {
        #region Fields/Properties

        public override string FriendlyName => "Entity Input Movement Plugin";

        public override Version Version => new Version(1,0,0);

        Views.MainView mainView;
        PluginTab mainTab;
        MainViewModel mainViewModel;

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        #region Startup/Assign Events

        public override void StartUp()
        {
            base.RegisterCodeGenerator(new TopDownPlugin.CodeGenerators.EntityCodeGenerator());
            base.RegisterCodeGenerator(new FlatRedBall.PlatformerPlugin.Generators.EntityCodeGenerator());
            base.RegisterCodeGenerator(new PlatformerPluginCore.CodeGenerators.EntityPlatformerAnimationCodeGenerator());
            base.RegisterCodeGenerator(new CodeGenerators.EntityCodeGenerator());
            AssignEvents();
        }

        private void AssignEvents()
        {
            this.ReactToLoadedGlux += HandleGluxLoaded;
            this.ReactToItemSelectHandler += HandleItemSelected;
            this.ReactToEntityRemoved += HandleElementRemoved;
            this.ReactToElementRenamed += HandleElementRenamed;
            this.ModifyAddEntityWindow += TopDownPlugin.Logic.ModifyAddEntityWindowLogic.HandleModifyAddEntityWindow;
            this.NewEntityCreatedWithUi += HandleNewEntityCreatedWithUi;
            this.ReactToImportedElement += HandleEntityImported;
        }

        #endregion

        private void HandleNewEntityCreatedWithUi(EntitySave entitySave, AddEntityWindow window)
        {

            EntityInputMovementPlugin.Logic.NewEntityCreatedReactionLogic.ReactToNewEntityCreated(entitySave, window);

            GlueCommands.Self.DialogCommands.FocusTab("Entity Input Movement");
        }

        private async void HandleGluxLoaded()
        {
            bool didChangeGlux = await UpdateTopDownCodePresenceInProject();

            UpdatePlatformerCodePresenceInProject();

            if (didChangeGlux)
            {
                GlueCommands.Self.GluxCommands.SaveGlux();
            }

            var firstPlatformerEntity = GlueState.Self.CurrentGlueProject.Entities.FirstOrDefault(item =>
                // February 26, 2022
                // Initially I wanted to get only entities which have no base class in case there
                // are base platformers and derived platformers. But, a game may have the platformer
                // entity (like Player) inherit from another base entity, and we still want to use that
                // entity to regenerate CSVs. So we ignore inheritance and only check if it's a platformer.
                FlatRedBall.PlatformerPlugin.Controllers.MainController.IsPlatformer(item));
            if(firstPlatformerEntity != null)
            {
                await FlatRedBall.PlatformerPlugin.Controllers.MainController.ForceCsvGenerationFor(firstPlatformerEntity);
            }
        }

        private async void HandleEntityImported(GlueElement newElement)
        {
            await UpdateTopDownCodePresenceInProject();
            UpdatePlatformerCodePresenceInProject();
        }

        private static void UpdatePlatformerCodePresenceInProject()
        {
            var entities = GlueState.Self.CurrentGlueProject.Entities;

            var anyPlatformer = entities.Any(item =>
            {
                var properties = item.Properties;
                return properties.GetValue<bool>("IsPlatformer");
            });

            if (anyPlatformer)
            {
                // just in case it's not there:
                FlatRedBall.PlatformerPlugin.Generators.EnumFileGenerator.Self.GenerateAndSave();
                FlatRedBall.PlatformerPlugin.Generators.IPlatformerCodeGenerator.Self.GenerateAndSave();
                FlatRedBall.PlatformerPlugin.Generators.PlatformerAnimationControllerGenerator.Self.GenerateAndSave();
                
            }
        }

        private static async Task<bool> UpdateTopDownCodePresenceInProject()
        {
            var entities = GlueState.Self.CurrentGlueProject.Entities;

            var firstTopDownEntity = entities.FirstOrDefault(item =>
            {
                var properties = item.Properties;
                return properties.GetValue<bool>(nameof(TopDownPlugin.ViewModels.TopDownEntityViewModel.IsTopDown));
            });

            if (firstTopDownEntity != null)
            {
                // just in case it's not there:
                TopDownPlugin.CodeGenerators.EnumFileGenerator.Self.GenerateAndSave();
                TopDownPlugin.CodeGenerators.InterfacesFileGenerator.Self.GenerateAndSave();
                TopDownPlugin.CodeGenerators.AiCodeGenerator.Self.GenerateAndSave();
                TopDownPluginCore.CodeGenerators.AiTargetLogicCodeGenerator.Self.GenerateAndSave();
                TopDownPlugin.CodeGenerators.AnimationCodeGenerator.Self.GenerateAndSave();

                var topDownController = TopDownPlugin.Controllers.MainController.Self;

                // This guarantees a instance exists in the controller and returns it...
                var viewModel =
                    TopDownPlugin.Controllers.MainController.Self.GetViewModel();
                // ...updating to the argument entity will update the view model that was returned in the last call.
                TopDownPlugin.Controllers.MainController.Self.UpdateTo(firstTopDownEntity);
                
                await topDownController.GenerateAndAddCsv(
                    firstTopDownEntity,
                    viewModel);
            }

            // remove requirement for the old top-down plugin otherwise projects will get a message forever about it:
            var didChangeGlux = GlueCommands.Self.GluxCommands.SetPluginRequirement(
                "Top Down Plugin",
                false,
                new Version(1, 0));
            return didChangeGlux;
        }

        private void HandleItemSelected(ITreeNode selectedTreeNode)
        {
            bool shouldShow = GlueState.Self.CurrentEntitySave != null &&
                // So this only shows if the entity itself is selected:
                selectedTreeNode?.Tag == GlueState.Self.CurrentEntitySave;


            if (shouldShow)
            {
                if (mainView == null)
                {
                    CreateMainView();
                }

                mainTab.Show();
                var currentEntity = GlueState.Self.CurrentEntitySave;


                mainViewModel.GlueObject = currentEntity;
                mainViewModel.UpdateFromGlueObject();

                TopDownPlugin.Controllers.MainController.Self.UpdateTo(currentEntity);
                FlatRedBall.PlatformerPlugin.Controllers.MainController.Self.UpdateTo(currentEntity);

                mainViewModel.RefreshRadioButtonValues();
            }
            else
            {
                mainTab?.Hide();
            }
        }

        private void CreateMainView()
        {
            mainView = new Views.MainView();
            mainViewModel = Controllers.MainController.Self.GetViewModel();
            mainView.DataContext = mainViewModel;

            #region Top Down
            var topDownViewModel = TopDownPlugin.Controllers.MainController.Self.GetViewModel();
            mainViewModel.TopDownViewModel = topDownViewModel;
            mainView.TopDownView.DataContext = topDownViewModel;
            #endregion

            #region Platformer
            var platformerViewModel = FlatRedBall.PlatformerPlugin.Controllers.MainController.Self.GetViewModel();
            mainViewModel.PlatformerViewModel = platformerViewModel;
            mainView.PlatformerView.DataContext = platformerViewModel;
            PlatformerPluginCore.Controllers.AnimationController.PlatformerViewModel = platformerViewModel;
            #endregion


            mainTab = this.CreateTab(mainView, "Entity Input Movement");
        }

        private void HandleElementRenamed(IElement renamedElement, string oldName)
        {
            if (renamedElement is EntitySave renamedEntity)
            {
                TopDownPlugin.Controllers.MainController.Self.HandleElementRenamed(renamedElement, oldName);
                //FlatRedBall.PlatformerPlugin.Controllers.MainController.Self.HandleElementRenamed(renamedElement, oldName);

            }
        }

        private void HandleElementRemoved(EntitySave removedElement, List<string> additionalFiles)
        {
            // This could be the very last entity that was a top-down, but isn't
            // anymore.
            TopDownPlugin.Controllers.MainController.Self.CheckForNoTopDownEntities();
            //FlatRedBall.PlatformerPlugin.Controllers.MainController.Self.CheckForNoPlatformerEntities();
        }

        public void MakeCurrentEntityPlatformer()
        {
            mainViewModel.PlatformerViewModel.IsPlatformer = true;
        }

        public void MakeCurrentEntityTopDown()
        {
            mainViewModel.TopDownViewModel.IsTopDown = true;
        }
    }
}
