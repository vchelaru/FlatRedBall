using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.ViewModels;
using GlueFormsCore.Plugins.EmbeddedPlugins.AddScreenPlugin.ViewModels;
using GlueFormsCore.Plugins.EmbeddedPlugins.AddScreenPlugin.Views;
using GlueFormsCore.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.AddScreenPlugin
{
    [Export(typeof(PluginBase))]
    public class MainAddScreenPlugin : EmbeddedPlugin
    {
        public override void StartUp()
        {
            AssignEvents();
        }

        private void AssignEvents()
        {
            this.ModifyAddScreenWindow += HandleModifyAddScreenWindow;
            this.NewScreenCreatedWithUi += HandleNewScreenCreatedWithUi;
        }

        private void HandleModifyAddScreenWindow(AddScreenWindow window)
        {
            var optionsView = new AddScreenOptionsView();

            var viewModel = new ViewModels.AddScreenViewModel();
            viewModel.CanAddBaseLevelScreen = true;
            viewModel.CanAddLevelScreen = true;

            var gameScreen =
                ObjectFinder.Self.GetScreenSaveUnqualified("GameScreen");
            var hasGameScreen = gameScreen != null;
                

            viewModel.HasGameScreen = hasGameScreen;

            if(hasGameScreen)
            {
                viewModel.AddScreenType = AddScreenType.LevelScreen;
                viewModel.InheritFromGameScreen = true;

                if(GlueCommands.Self.GluxCommands.StartUpScreenName == gameScreen.Name)
                {
                    // no reason to have the game screen be the startup screen if we are going to have levels
                    viewModel.IsSetAsStartupChecked = true;
                }

            }
            else
            {
                viewModel.AddScreenType = AddScreenType.BaseLevelScreen;
            }



            optionsView.DataContext = viewModel;
            window.AddControl(optionsView);
        }

        private void HandleNewScreenCreatedWithUi(ScreenSave newScreen, AddScreenWindow window)
        {
            var foundControl = window.UserControlChildren.FirstOrDefault(item => item is AddScreenOptionsView);

            var viewModel = foundControl?.DataContext as ViewModels.AddScreenViewModel;

            if(viewModel != null)
            {
                ApplyViewModelToScreen(newScreen, viewModel);
            }
        }

        private void ApplyViewModelToScreen(ScreenSave newScreen, AddScreenViewModel viewModel)
        {
            var shouldSave = false;
            switch(viewModel.AddScreenType)
            {
                case AddScreenType.BaseLevelScreen:


                    if(viewModel.IsAddMapLayeredTileMapChecked)
                    {
                        var addObjectViewModel = new AddObjectViewModel();
                        addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                        addObjectViewModel.SourceClassType = "FlatRedBall.TileGraphics.LayeredTileMap";
                        addObjectViewModel.ObjectName = "Map";

                        var nos = GlueCommands.Self.GluxCommands.AddNewNamedObjectTo(addObjectViewModel, newScreen, null);
                        nos.SetByDerived = true;

                        shouldSave = true;
                    }
                    if(viewModel.IsAddSolidCollisionShapeCollectionChecked)
                    {
                        var addObjectViewModel = new AddObjectViewModel();
                        addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                        addObjectViewModel.SourceClassType = "FlatRedBall.TileCollisions.TileShapeCollection";
                        addObjectViewModel.ObjectName = "SolidCollision";

                        var nos = GlueCommands.Self.GluxCommands.AddNewNamedObjectTo(addObjectViewModel, newScreen, null);
                        nos.SetByDerived = true;

                        // todo - make it come from map, select "Map", Property, and "SolidCollision"
                        const int FromType = 4;
                        nos.Properties.SetValue("CollisionCreationOptions", FromType);
                        nos.Properties.SetValue("SourceTmxName", "Map");
                        nos.Properties.SetValue("CollisionTileTypeName", "SolidCollision");

                        shouldSave = true;
                    }

                    break;
                case AddScreenType.LevelScreen:

                    if(viewModel.InheritFromGameScreen)
                    {
                        var gameScreen = ObjectFinder.Self.GetScreenSaveUnqualified("GameScreen");
                        if(gameScreen != null)
                        {
                            newScreen.BaseScreen = gameScreen.Name;
                            newScreen.UpdateFromBaseType();
                            shouldSave = true;

                        }
                    }

                    if(viewModel.IsSetAsStartupChecked)
                    {
                        GlueCommands.Self.GluxCommands.StartUpScreenName = newScreen.Name;
                        //GlueCommands.Self.RefreshCommands.RefreshUi()
                        // meh, eventually we'll fix this:
                    }

                    if(viewModel.IsAddStandardTmxChecked)
                    {
                        // add standard TMX....how?
                        // Currently there's no way to run
                        // logic in plugins, so we'll just show the window and select the 
                        // TMX option:
                        var addNewFileViewModel = new AddNewFileViewModel();
                        addNewFileViewModel.SelectedAssetTypeInfo = 
                            AvailableAssetTypes.Self.GetAssetTypeFromExtension("tmx");
                        var newRfs = GlueCommands.Self.DialogCommands.ShowAddNewFileDialog(addNewFileViewModel);

                        if(newRfs != null)
                        {
                            var mapObject = newScreen.NamedObjects.FirstOrDefault(item => item.InstanceName == "Map" && item.GetAssetTypeInfo().FriendlyName.StartsWith("LayeredTileMap"));
                            if(mapObject != null)
                            {
                                mapObject.SourceType = SourceType.File;
                                mapObject.SourceFile = newRfs.Name;
                                mapObject.SourceName = "Entire File (LayeredTileMap)";
                            }
                        }
                    }
                    break;
            }

            if (shouldSave)
            {
                GlueCommands.Self.RefreshCommands.RefreshUiForSelectedElement();
                GlueCommands.Self.GenerateCodeCommands.GenerateElementCodeTask(newScreen);
                GlueCommands.Self.GluxCommands.SaveGlux();
                if(newScreen.Name == GlueCommands.Self.GluxCommands.StartUpScreenName)
                {

                    ElementViewWindow.StartUpScreen = GlueState.Self.CurrentElementTreeNode;
                }
            }
        }

    }
}
