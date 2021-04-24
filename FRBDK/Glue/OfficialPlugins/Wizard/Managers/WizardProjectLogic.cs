using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.ViewModels;
using GlueFormsCore.Plugins.EmbeddedPlugins.AddScreenPlugin;
using GlueFormsCore.ViewModels;
using OfficialPluginsCore.Wizard.Models;
using OfficialPluginsCore.Wizard.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ToolsUtilities;
using ToolsUtilitiesStandard.Network;

namespace OfficialPluginsCore.Wizard.Managers
{
    public class WizardProjectLogic : Singleton<WizardProjectLogic>
    {
        public async Task Apply(WizardData vm)
        {
            var tasks = new List<TaskItemViewModel>();

            void Add(string name, Func<Task> task)
            {
                var toAdd = new TaskItemViewModel();
                toAdd.Description = name;
                toAdd.Task = task;
                tasks.Add(toAdd);
            }

            ScreenSave gameScreen = null;
            NamedObjectSave solidCollisionNos = null;
            NamedObjectSave cloudCollisionNos = null;

            List<Func<Task>> operations = new List<Func<Task>>();

            // Add Gum before adding a GameScreen, so the GameScreen gets its Gum screen
            if (vm.AddGum)
            {
                Add("Add Gum", async () =>
                {
                    await HandleAddGum(vm);
                    await TaskManager.Self.WaitForAllTasksFinished();
                });
            }

            if (vm.AddGameScreen)
            {
                Add("Add GameScreen", async () =>
                {
                    gameScreen = HandleAddGameScreen(vm, ref solidCollisionNos, ref cloudCollisionNos);
                    await TaskManager.Self.WaitForAllTasksFinished();
                });
            }

            if (vm.AddPlayerEntity)
            {
                Add("Add Player", async () =>
                {
                    HandleAddPlayerEntity(vm, gameScreen, solidCollisionNos, cloudCollisionNos);
                    await TaskManager.Self.WaitForAllTasksFinished();
                });
            }

            if (vm.CreateLevels)
            {
                Add("Create Levels", async () =>
                {
                    HandleCreateLevels(vm, gameScreen);
                    await TaskManager.Self.WaitForAllTasksFinished();
                });
            }

            if (vm.AddCameraController && vm.AddGameScreen)
            {
                Add("Create Camera", async () =>
                {
                    ApplyCameraController(vm, gameScreen);
                    await TaskManager.Self.WaitForAllTasksFinished();
                });
            }

            if(vm.ElementImportUrls.Count > 0)
            {
                Add("Importing Screens/Entities", async () =>
                {
                    ImportElements(vm);
                    await TaskManager.Self.WaitForAllTasksFinished();
                });
            }


            Add("Flushing Files", async () =>
            {
                var didWait = false;

                const int msToWaitEachTime = 2500;
                await Task.Delay(msToWaitEachTime);
                do
                {
                    didWait = await FlatRedBall.Glue.Managers.TaskManager.Self.WaitForAllTasksFinished();

                    if (didWait)
                    {
                        // Glue checks for file changes every 2 seconds, so let's wait 2.5 seconds
                        // to make sure it's had enough time to look for file changes.
                        await Task.Delay(msToWaitEachTime);
                    }
                } while (didWait);
            });

            vm.Tasks = tasks;

            TaskItemViewModel currentTask = null;
            double maxTaskCount = 0;
            void UpdateCurrentTask(TaskEvent taskEvent, FlatRedBall.Glue.Tasks.GlueTask task)
            {
                var currentTaskCount = TaskManager.Self.TaskCount;
                maxTaskCount = Math.Max(maxTaskCount, currentTaskCount);
                var currentTaskInner = currentTask;
                if (currentTaskCount != 0 && currentTaskInner != null)
                {
                    var percentageLeft = currentTaskCount / maxTaskCount;
                    var newPercent = 100 * (1 - percentageLeft);
                    if (currentTaskInner.ProgressPercentage == null)
                    {
                        currentTaskInner.ProgressPercentage = newPercent;
                    }
                    else
                    {
                        currentTaskInner.ProgressPercentage = Math.Max(
                            currentTaskInner.ProgressPercentage.Value, newPercent);
                    }
                }
            }

            TaskManager.Self.TaskAddedOrRemoved += UpdateCurrentTask;

            foreach (var task in vm.Tasks)
            {
                task.IsInProgress = true;
                currentTask = task;
                maxTaskCount = 0;

                await task.Task();
                task.IsInProgress = false;
                task.IsComplete = true;
            }

            TaskManager.Self.TaskAddedOrRemoved -= UpdateCurrentTask;

        }

        private static void ImportElements(WizardData vm)
        {
            var downloadFolder = FileManager.UserApplicationDataForThisApplication + "ImportDownload\\";

            foreach (var item in vm.ElementImportUrls)
            {
                var destinationFileName = downloadFolder + FileManager.RemovePath(item);
                TaskManager.Self.Add(async () =>
                {
                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromDays(1), };
                    var result = await NetworkManager.Self.DownloadWithProgress(
                        httpClient, item, destinationFileName, null);

                    if (result.Succeeded)
                    {
                        GlueCommands.Self.GluxCommands.ImportScreenOrEntityFromFile(destinationFileName);
                    }
                }, "Downloading " + item);
            }
        }

        private static async Task HandleAddGum(WizardData vm)
        {
            if (vm.AddFlatRedBallForms)
            {
                PluginManager.CallPluginMethod("Gum Plugin", "CreateGumProjectWithForms");
            }
            else
            {
                PluginManager.CallPluginMethod("Gum Plugin", "CreateGumProjectNoForms");
            }

            await FlatRedBall.Glue.Managers.TaskManager.Self.WaitForAllTasksFinished();
        }

        private static ScreenSave HandleAddGameScreen(WizardData vm, ref NamedObjectSave solidCollisionNos, ref NamedObjectSave cloudCollisionNos)
        {
            ScreenSave gameScreen = GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("GameScreen");
            if (vm.AddTiledMap)
            {
                MainAddScreenPlugin.AddMapObject(gameScreen);
            }

            if (vm.AddSolidCollision)
            {
                solidCollisionNos = MainAddScreenPlugin.AddCollision(gameScreen, "SolidCollision",
                    setFromMapObject: vm.AddTiledMap);
            }
            if (vm.AddCloudCollision)
            {
                cloudCollisionNos = MainAddScreenPlugin.AddCollision(gameScreen, "CloudCollision",
                    setFromMapObject: vm.AddTiledMap);
            }


            return gameScreen;
        }

        private static void HandleAddPlayerEntity(WizardData vm, ScreenSave gameScreen, NamedObjectSave solidCollisionNos, NamedObjectSave cloudCollisionNos)
        {
            var addEntityVm = new AddEntityViewModel();
            addEntityVm.Name = "Player";

            if (vm.PlayerCollisionType == CollisionType.Rectangle)
            {
                addEntityVm.IsAxisAlignedRectangleChecked = true;
            }
            else if (vm.PlayerCollisionType == CollisionType.Circle)
            {
                addEntityVm.IsCircleChecked = true;
            }
            else
            {
                // none are checked, but we'll still have it be ICollidable
            }

            addEntityVm.IsICollidableChecked = true;

            addEntityVm.IsSpriteChecked = vm.AddPlayerSprite;

            var playerEntity = GlueCommands.Self.GluxCommands.EntityCommands.AddEntity(addEntityVm);

            if (vm.OffsetPlayerPosition)
            {
                playerEntity.SetCustomVariable("X", 64.0f);
                playerEntity.SetCustomVariable("Y", -64.0f);
            }

            // requires the current entity be set:
            GlueState.Self.CurrentElement = playerEntity;

            if (vm.PlayerControlType == GameType.Platformer)
            {
                // mark as platformer
                PluginManager.CallPluginMethod("Entity Input Movement Plugin", "MakeCurrentEntityPlatformer");

            }
            else if (vm.PlayerControlType == GameType.Topdown)
            {
                // mark as top down
                PluginManager.CallPluginMethod("Entity Input Movement Plugin", "MakeCurrentEntityTopDown");
            }

            NamedObjectSave playerList = null;
            if (vm.AddGameScreen && vm.AddPlayerListToGameScreen)
            {
                {
                    AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                    addObjectViewModel.ForcedElementToAddTo = gameScreen;
                    addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                    addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.PositionedObjectList;
                    addObjectViewModel.SourceClassGenericType = playerEntity.Name;
                    addObjectViewModel.ObjectName = $"{playerEntity.GetStrippedName()}List";

                    playerList = GlueCommands.Self.GluxCommands.AddNewNamedObjectTo(addObjectViewModel, gameScreen, null);
                }

                if (vm.AddPlayerToList)
                {
                    AddObjectViewModel addPlayerVm = new AddObjectViewModel();

                    addPlayerVm.SourceType = SourceType.Entity;
                    addPlayerVm.SourceClassType = playerEntity.Name;
                    addPlayerVm.ObjectName = "Player1";

                    GlueCommands.Self.GluxCommands.AddNewNamedObjectTo(addPlayerVm, gameScreen, playerList);
                }
            }

            if (vm.AddGameScreen && vm.AddPlayerListToGameScreen)
            {
                if (vm.CollideAgainstSolidCollision)
                {
                    PluginManager.ReactToCreateCollisionRelationshipsBetween(playerList, solidCollisionNos);

                    var nos = gameScreen.GetNamedObject("PlayerListVsSolidCollision");

                    // move is 1
                    // bounce is 2
                    // PlatformerSolid is 3
                    // PlatformerCloud is 4

                    if (vm.PlayerControlType == GameType.Platformer)
                    {
                        nos.Properties.SetValue("CollisionType", 3);
                    }
                    else
                    {
                        nos.Properties.SetValue("CollisionType", 2);
                        nos.Properties.SetValue("FirstCollisionMass", 0.0f);
                        nos.Properties.SetValue("SecondCollisionMass", 1.0f);
                        nos.Properties.SetValue("CollisionElasticity", 0.0f);

                    }

                    PluginManager.CallPluginMethod("Collision Plugin", "FixNamedObjectCollisionType", nos);
                }
                if (vm.CollideAgainstCloudCollision && vm.AddCloudCollision)
                {
                    PluginManager.ReactToCreateCollisionRelationshipsBetween(playerList, cloudCollisionNos);

                    var nos = gameScreen.GetNamedObject("PlayerListVsCloudCollision");

                    if (vm.PlayerControlType == GameType.Platformer)
                    {
                        nos.Properties.SetValue("CollisionType", 4);
                    }

                    PluginManager.CallPluginMethod("Collision Plugin", "FixNamedObjectCollisionType", nos);
                }
            }
        }

        private static void HandleCreateLevels(WizardData vm, ScreenSave gameScreen)
        {
            for (int i = 0; i < vm.NumberOfLevels; i++)
            {
                var levelName = "Level" + (i + 1);

                var levelScreen = GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen(levelName);
                levelScreen.BaseScreen = gameScreen.Name;
                levelScreen.UpdateFromBaseType();
                GlueState.Self.CurrentScreenSave = levelScreen;


                if (i == 0)
                {
                    GlueCommands.Self.GluxCommands.StartUpScreenName = levelScreen.Name;
                }

                if (vm.AddGameScreen && vm.AddTiledMap)
                {
                    // add a regular TMX
                    var addNewFileVm = new AddNewFileViewModel();

                    var tmxAti =
                        AvailableAssetTypes.Self.GetAssetTypeFromExtension("tmx");
                    addNewFileVm.SelectedAssetTypeInfo = tmxAti;

                    addNewFileVm.ForcedType = tmxAti;
                    addNewFileVm.FileName = levelName + "Map";
                    GlueCommands.Self.GluxCommands.CreateNewFileAndReferencedFileSave(addNewFileVm);

                    var mapObject = levelScreen.NamedObjects.FirstOrDefault(item => item.InstanceName == "Map" && item.GetAssetTypeInfo().FriendlyName.StartsWith("LayeredTileMap"));
                    if (mapObject != null)
                    {
                        mapObject.SourceType = SourceType.File;
                        mapObject.SourceFile = $"Screens/{levelName}/{levelName}Map.tmx";
                        mapObject.SourceName = "Entire File (LayeredTileMap)";
                    }

                    void SelectTmxRfs()
                    {
                        GlueState.Self.CurrentReferencedFileSave = levelScreen.ReferencedFiles
                            .FirstOrDefault(Item => Item.GetAssetTypeInfo()?.Extension == "tmx");
                    }

                    if (vm.IncludStandardTilesetInLevels)
                    {
                        SelectTmxRfs();
                        PluginManager.CallPluginMethod("Tiled Plugin", "AddStandardTilesetOnCurrentFile");
                    }
                    if (vm.IncludeGameplayLayerInLevels)
                    {
                        SelectTmxRfs();
                        PluginManager.CallPluginMethod("Tiled Plugin", "AddGameplayLayerToCurrentFile");
                    }

                    if (vm.IncludeCollisionBorderInLevels)
                    {
                        SelectTmxRfs();
                        PluginManager.CallPluginMethod("Tiled Plugin", "AddCollisionBorderToCurrentFile");
                    }

                }
            }
        }

        private static void ApplyCameraController(WizardData vm, ScreenSave gameScreen)
        {
            var addCameraControllerVm = new AddObjectViewModel();
            addCameraControllerVm.ForcedElementToAddTo = gameScreen;
            addCameraControllerVm.SourceType = SourceType.FlatRedBallType;
            addCameraControllerVm.SourceClassType = "FlatRedBall.Entities.CameraControllingEntity";
            addCameraControllerVm.ObjectName = "CameraControllingEntityInstance";

            var cameraNos = GlueCommands.Self.GluxCommands.AddNewNamedObjectTo(addCameraControllerVm, gameScreen, null);

            if (vm.FollowPlayersWithCamera && vm.AddPlayerListToGameScreen)
            {
                cameraNos.SetVariableValue(nameof(FlatRedBall.Entities.CameraControllingEntity.Targets), "PlayerList");
            }
            if (vm.KeepCameraInMap && vm.AddTiledMap)
            {
                cameraNos.SetVariableValue(nameof(FlatRedBall.Entities.CameraControllingEntity.Map), "Map");
            }
        }

    }
}
