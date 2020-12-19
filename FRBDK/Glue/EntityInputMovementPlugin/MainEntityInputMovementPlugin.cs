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

        public override void StartUp()
        {
            base.RegisterCodeGenerator(new TopDownPlugin.CodeGenerators.EntityCodeGenerator());

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
        }

        private void HandleNewEntityCreatedWithUi(EntitySave entitySave, AddEntityWindow window)
        {

            TopDownPlugin.Logic.NewEntityCreatedReactionLogic.ReactToNewEntityCreated(entitySave, window);

            GlueCommands.Self.DialogCommands.FocusTab("Entity Input Movement");



        }

        private void HandleGluxLoaded()
        {
            var entities = GlueState.Self.CurrentGlueProject.Entities;

            var anyTopDownEntities = entities.Any(item =>
            {
                var properties = item.Properties;
                return properties.GetValue<bool>(nameof(TopDownPlugin.ViewModels.TopDownEntityViewModel.IsTopDown));
            });

            if (anyTopDownEntities)
            {
                // just in case it's not there:
                TopDownPlugin.CodeGenerators.EnumFileGenerator.Self.GenerateAndSave();
                TopDownPlugin.CodeGenerators.InterfacesFileGenerator.Self.GenerateAndSave();
                TopDownPlugin.CodeGenerators.AiCodeGenerator.Self.GenerateAndSave();
                TopDownPluginCore.CodeGenerators.AiTargetLogicCodeGenerator.Self.GenerateAndSave();
                TopDownPlugin.CodeGenerators.AnimationCodeGenerator.Self.GenerateAndSave();

            }

            // remove requirement for the old top-down plugin otherwise projects will get a message forever about it:
            var didChange = GlueCommands.Self.GluxCommands.SetPluginRequirement(
                "Top Down Plugin",
                false,
                new Version(1, 0));

            if(didChange)
            {

            }
        }


        private void HandleItemSelected(System.Windows.Forms.TreeNode selectedTreeNode)
        {
            bool shouldShow = GlueState.Self.CurrentEntitySave != null &&
                // So this only shows if the entity itself is selected:
                selectedTreeNode?.Tag == GlueState.Self.CurrentEntitySave;


            if (shouldShow)
            {
                if (mainView == null)
                {
                    CreateMainView();
                    this.ShowTab(mainTab, TabLocation.Center);
                }
                else
                {
                    this.ShowTab(mainTab);
                }
                var currentEntity = GlueState.Self.CurrentEntitySave;
                TopDownPlugin.Controllers.MainController.Self.UpdateTo(currentEntity);

                mainViewModel.RefreshRadioButtonValues();
            }
            else
            {
                this.RemoveTab(mainTab);
            }
        }

        private void CreateMainView()
        {
            mainView = new Views.MainView();
            mainViewModel = new MainViewModel();
            var topDownViewModel = TopDownPlugin.Controllers.MainController.Self.GetViewModel();
            mainViewModel.TopDownViewModel = topDownViewModel;

            mainView.DataContext = mainViewModel;

            mainView.TopDownView.DataContext = topDownViewModel;

            mainTab = this.CreateTab(mainView, "Entity Input Movement");
        }

        private void HandleElementRenamed(IElement renamedElement, string oldName)
        {
            if (renamedElement is EntitySave renamedEntity)
            {
                TopDownPlugin.Controllers.MainController.Self.HandleElementRenamed(renamedElement, oldName);
            }
        }

        private void HandleElementRemoved(EntitySave removedElement, List<string> additionalFiles)
        {
            // This could be the very last entity that was a top-down, but isn't
            // anymore.
            TopDownPlugin.Controllers.MainController.Self.CheckForNoTopDownEntities();
        }
    }
}
