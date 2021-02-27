using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.ViewModels;
using GlueFormsCore.Plugins.EmbeddedPlugins.AddScreenPlugin;
using GlueFormsCore.ViewModels;
using OfficialPluginsCore.Wizard.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using WpfDataUi;

namespace OfficialPluginsCore.Wizard
{
    [Export(typeof(PluginBase))]
    public class MainWizardPlugin : PluginBase
    {
        public override string FriendlyName => "New Project Wizard";

        public override Version Version => new Version(1, 1);

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            AddMenuItemTo("Start New Project Wizard", RunWizard, "Plugins");
        }

        private void RunWizard(object sender, EventArgs e)
        {
            var vm = new WizardData();

            Apply(vm);
        }

        private void Apply(WizardData vm)
        {
            ScreenSave gameScreen = null;
            NamedObjectSave solidCollisionNos = null;
            NamedObjectSave cloudCollisionNos = null;

            if(vm.AddGameScreen)
            {
                gameScreen = GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("GameScreen");

                if(vm.AddTiledMap)
                {
                    MainAddScreenPlugin.AddMapObject(gameScreen);
                }
                if(vm.AddSolidCollision)
                {
                    solidCollisionNos = MainAddScreenPlugin.AddCollision(gameScreen, "SolidCollision");
                }
                if(vm.AddCloudCollision)
                {
                    cloudCollisionNos = MainAddScreenPlugin.AddCollision(gameScreen, "CloudCollision");
                }
            }

            if(vm.AddPlayerEntity)
            {
                var addEntityVm = new AddEntityViewModel();
                addEntityVm.Name = "Player";
                // todo - ask!
                addEntityVm.IsAxisAlignedRectangleChecked = true;
                addEntityVm.IsICollidableChecked = true;

                var playerEntity = GlueCommands.Self.GluxCommands.EntityCommands.AddEntity(addEntityVm);

                // requires the current entity be set:
                GlueState.Self.CurrentElement = playerEntity;

                if(vm.PlayerControlType == GameType.Platformer)
                {
                    // mark as platformer
                    PluginManager.CallPluginMethod("Entity Input Movement Plugin", "MakeCurrentEntityPlatformer" );

                }
                else if(vm.PlayerControlType == GameType.Topdown)
                {
                    // mark as top down
                    PluginManager.CallPluginMethod("Entity Input Movement Plugin", "MakeCurrentEntityTopDown");
                }

                NamedObjectSave playerList = null;
                if(vm.AddGameScreen && vm.AddPlayerListToGameScreen)
                {
                    {
                        AddObjectViewModel addObjectViewModel = new AddObjectViewModel();

                        addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                        addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.PositionedObjectList;
                        addObjectViewModel.SourceClassGenericType = playerEntity.Name;
                        addObjectViewModel.ObjectName = $"{playerEntity.GetStrippedName()}List";

                        playerList = GlueCommands.Self.GluxCommands.AddNewNamedObjectTo(addObjectViewModel, gameScreen, null);
                    }

                    if(vm.AddPlayerToList)
                    {
                        AddObjectViewModel addPlayerVm = new AddObjectViewModel();

                        addPlayerVm.SourceType = SourceType.Entity;
                        addPlayerVm.SourceClassType = nameof(playerEntity.Name);
                        addPlayerVm.ObjectName = "Player1";

                        GlueCommands.Self.GluxCommands.AddNewNamedObjectTo(addPlayerVm, gameScreen, playerList);
                    }
                }

                if(vm.AddGameScreen && vm.AddPlayerListToGameScreen)
                {
                    if(vm.CollideAgainstSolidCollision)
                    {
                        PluginManager.ReactToCreateCollisionRelationshipsBetween(playerList, solidCollisionNos);
                    }
                    if(vm.CollideAgainstCloudCollision)
                    {
                        PluginManager.ReactToCreateCollisionRelationshipsBetween(playerList, cloudCollisionNos);
                    }
                }

            }

            if(vm.CreateLevels)
            {
                for(int i= 0; i < vm.NumberOfLevels; i++)
                {
                    var levelName = "Level" + (i + 1);

                    var levelScreen = GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen(levelName);
                    levelScreen.BaseScreen = gameScreen.Name;
                    levelScreen.UpdateFromBaseType();

                    if(i == 0)
                    {
                        GlueCommands.Self.GluxCommands.StartUpScreenName = levelScreen.Name;
                    }
                }
            }
        }
    }
}
