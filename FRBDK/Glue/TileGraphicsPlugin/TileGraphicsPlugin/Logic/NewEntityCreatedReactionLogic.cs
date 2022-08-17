using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EditorObjects.IoC;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SetVariable;
using FlatRedBall.Glue.ViewModels;
using FlatRedBall.IO;
using TileGraphicsPlugin.ViewModels;
using TileGraphicsPlugin.Views;

namespace TileGraphicsPlugin.Logic
{
    class NewEntityCreatedReactionLogic
    {
        internal static async void ReactToNewEntityCreated(EntitySave newEntity, AddEntityWindow window)
        {
            var control = window.UserControlChildren.FirstOrDefault(item => item is AdditionalEntitiesControls);
            var viewModel = control?.DataContext as AdditionalEntitiesControlViewModel;

            if(viewModel?.AllTileMapUiVisibility == System.Windows.Visibility.Visible)
            {
                if (viewModel.IncludeListsInScreens)
                {
                    // loop through all screens that have a TMX object and add them.
                    // be smart - if the base screen does, don't do it in the derived
                    var allScreens = GlueState.Self.CurrentGlueProject.Screens;

                    foreach(var screen in allScreens)
                    {
                        var needsList = GetIfScreenNeedsList(screen);

                        if(needsList)
                        {
                            AddObjectViewModel addObjectViewModel = new AddObjectViewModel();

                            addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                            addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.PositionedObjectList;
                            addObjectViewModel.SourceClassGenericType = newEntity.Name;
                            addObjectViewModel.ObjectName = $"{newEntity.GetStrippedName()}List";
                                

                            var newNos = await GlueCommands.Self.GluxCommands.AddNewNamedObjectToAsync(
                                addObjectViewModel, screen, listToAddTo:null, selectNewNos:false);
                            newNos.ExposedInDerived = true;

                            Container.Get<NamedObjectSetVariableLogic>().ReactToNamedObjectChangedValue(nameof(newNos.ExposedInDerived), false,
                                namedObjectSave: newNos);

                            GlueCommands.Self.PrintOutput(
                                $"Tiled Plugin added {addObjectViewModel.ObjectName} to {screen}");

                            GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(screen);
                        }
                    }
                }

                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(newEntity);
                GlueCommands.Self.ProjectCommands.SaveProjects();
                GlueState.Self.CurrentEntitySave = newEntity;
                
            }
        }

        private static bool GetIfScreenNeedsList(ScreenSave screen)
        {
            var hasTmx = GetIfScreenHasTmxDirectly(screen);

            //var doBaseScreensHaveTmx = GetIfBaseScreensHaveTmx(screen);

            var isDerived = string.IsNullOrEmpty(screen.BaseScreen) == false;

            return hasTmx == true && !isDerived;
        }

        private static bool GetIfBaseScreensHaveTmx(ScreenSave screen)
        {
            var baseScreen = ObjectFinder.Self.GetScreenSave(screen.BaseScreen);

            if(baseScreen == null)
            {
                return false;
            }
            else
            {
                if(GetIfScreenHasTmxDirectly(baseScreen))
                {
                    return true;
                }
                else
                {
                    return GetIfBaseScreensHaveTmx(baseScreen);
                }

            }
            throw new NotImplementedException();
        }

        private static bool GetIfScreenHasTmxDirectly(ScreenSave screen)
        {
            var hasTmxFile = screen.ReferencedFiles.Any(item => FileManager.GetExtension(item.Name) == "tmx");
            var hasTmx = hasTmxFile;


            if(!hasTmx)
            {

                hasTmx = screen.AllNamedObjects.Any(item => item.GetAssetTypeInfo()?.FriendlyName == "LayeredTileMap (.tmx)");
            }
            return hasTmx;
        }
    }
}
