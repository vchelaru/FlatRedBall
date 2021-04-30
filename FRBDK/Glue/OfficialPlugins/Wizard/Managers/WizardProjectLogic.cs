using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SetVariable;
using FlatRedBall.Glue.ViewModels;
using GlueFormsCore.Plugins.EmbeddedPlugins.AddScreenPlugin;
using GlueFormsCore.ViewModels;
using Newtonsoft.Json;
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
        #region Internal Classes
        class ElementAndNosList
        {
            public GlueElement Element { get; set; }
            public List<NamedObjectSave> NosList { get; set; }
        }
        #endregion

        public async Task Apply(WizardData vm)
        {
            var tasks = new List<TaskItemViewModel>();

            void AddTask(string name, Func<Task> task)
            {
                var toAdd = new TaskItemViewModel();
                toAdd.Description = name;
                toAdd.Task = task;
                tasks.Add(toAdd);
            }

            void Add(string name, Action action)
            {
                var toAdd = new TaskItemViewModel();
                toAdd.Description = name;
                toAdd.Action = action;
                tasks.Add(toAdd);
            }

            ScreenSave gameScreen = null;
            NamedObjectSave solidCollisionNos = null;
            NamedObjectSave cloudCollisionNos = null;

            List<Func<Task>> operations = new List<Func<Task>>();

            // Add Gum before adding a GameScreen, so the GameScreen gets its Gum screen
            if (vm.AddGum)
            {
                Add("Add Gum", () => HandleAddGum(vm));
            }

            if (vm.AddGameScreen)
            {
                Add("Add GameScreen", () =>
                {
                    gameScreen = HandleAddGameScreen(vm, ref solidCollisionNos, ref cloudCollisionNos);
                });
            }

            if (vm.AddPlayerEntity)
            {
                AddTask("Add Player", async () =>
                {
                    var playerEntity = await HandleAddPlayerEntity(vm);
                    HandleAddPlayerInstance(vm, gameScreen, solidCollisionNos, cloudCollisionNos, playerEntity);
                });
            }

            if (vm.CreateLevels)
            {
                Add("Create Levels", () =>
                    HandleCreateLevels(vm, gameScreen));
            }

            if (vm.AddCameraController && vm.AddGameScreen)
            {
                Add("Create Camera", () =>
                    ApplyCameraController(vm, gameScreen));
            }

            if(vm.ElementImportUrls.Count > 0)
            {
                Add("Importing Screens/Entities", () =>
                    ImportElements(vm));
            }

            if(!string.IsNullOrEmpty(vm.NamedObjectSavesSerialized))
            {
                Add("Adding additional Objects", () =>
                    ImportAdditionalObjects(vm.NamedObjectSavesSerialized));
            }


            AddTask("Flushing Files", async () =>
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

                if(task.Task != null) await task.Task();
                if (task.Action != null) task.Action();

                await TaskManager.Self.WaitForAllTasksFinished();
                task.IsInProgress = false;
                task.IsComplete = true;
            }

            TaskManager.Self.TaskAddedOrRemoved -= UpdateCurrentTask;

            // just in case, refresh everything
            GlueCommands.Self.RefreshCommands.RefreshTreeNodes();

        }

        private void ImportAdditionalObjects(string namedObjectSavesSerialized)
        {
            Dictionary<string, List<NamedObjectSave>> deserialized = null;

            Exception deserializeException = null;

            try
            {
                deserialized = JsonConvert.DeserializeObject<Dictionary<string, List<NamedObjectSave>>>(namedObjectSavesSerialized);
            }
            catch(Exception e)
            {
                // we currently don't have error handling, we need it
                deserializeException = e;
            }

            List<ElementAndNosList> imports = new List<ElementAndNosList>();

            foreach(var kvp in deserialized)
            {
                var elementName = kvp.Key;
                if(elementName.StartsWith("Screens\\"))
                {
                    var screen = ObjectFinder.Self.GetScreenSave(elementName);

                    imports.Add(new ElementAndNosList
                    {
                        Element = screen,
                        NosList = kvp.Value
                    });

                }
                else if(elementName.StartsWith("Entities\\"))
                {
                    var entity = ObjectFinder.Self.GetEntitySave(elementName);

                    imports.Add(new ElementAndNosList
                    {
                        Element = entity,
                        NosList = kvp.Value
                    });

                }

            }

            // we want base implementations first, then derived
            var sortedImports = imports.OrderBy(item => ObjectFinder.Self.GetHierarchyDepth(item.Element))
                .ToArray();

            foreach (var elementAndNosList in sortedImports)
            {
                AddNamedsObjectToElement(elementAndNosList.NosList, elementAndNosList.Element);
            }

            static void AddNamedsObjectToElement(List<NamedObjectSave> nosList, GlueElement glueElement)
            {
                if (glueElement != null)
                {
                    // lists come first, then everything else after
                    var sortedNoses = nosList.OrderBy(item => !item.IsList)
                        .ToArray();

                    foreach (var nos in sortedNoses)
                    {
                        NamedObjectSave listToAddTo = null;
                        if(nos.IsList == false)
                        {
                            // see if there are any lists in this object which have a shared type. Eventually we might care about inheritance, but not yet...
                            listToAddTo = glueElement.NamedObjects.FirstOrDefault(item => item.IsList && item.SourceClassGenericType == nos.SourceClassType);
                        }

                        GlueCommands.Self.GluxCommands.AddNamedObjectTo(nos, glueElement, listToAddTo);

                        if(nos.ExposedInDerived)
                        {
                            EditorObjects.IoC.Container.Get<NamedObjectSetVariableLogic>().ReactToNamedObjectChangedValue(
                                nameof(nos.ExposedInDerived), 
                                // pretend the value changed from false -> true
                                false,
                                namedObjectSave:nos);
                        }

                        // remove all children, and then re-add them through the GlueCommands so that all plugins are notified:
                        if(nos.ContainedObjects.Count > 0)
                        {
                            var children = nos.ContainedObjects.ToArray();

                            nos.ContainedObjects.Clear();

                            foreach (var subNos in nos.ContainedObjects)
                            {
                                GlueCommands.Self.GluxCommands.AddNamedObjectTo(subNos, glueElement, nos);

                            }

                        }
                    }
                }
            }
        }

        private static void ImportElements(WizardData vm)
        {
            var downloadFolder = FileManager.UserApplicationDataForThisApplication + "ImportDownload\\";

            foreach (var item in vm.ElementImportUrls)
            {
                var destinationFileName = downloadFolder + FileManager.RemovePath(item);
                TaskManager.Self.Add(() =>
                {
                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromDays(1), };
                    var downloadTask = NetworkManager.Self.DownloadWithProgress(
                        httpClient, item, destinationFileName, null);

                    downloadTask.Wait();

                    var result = downloadTask.Result;

                    if (result.Succeeded)
                    {
                        GlueCommands.Self.GluxCommands.ImportScreenOrEntityFromFile(destinationFileName);
                    }
                }, "Downloading " + item);
            }
        }

        private static void HandleAddGum(WizardData vm)
        {
            if (vm.AddFlatRedBallForms)
            {
                PluginManager.CallPluginMethod("Gum Plugin", "CreateGumProjectWithForms");
            }
            else
            {
                PluginManager.CallPluginMethod("Gum Plugin", "CreateGumProjectNoForms");
            }
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

        private static async Task<EntitySave> HandleAddPlayerEntity(WizardData vm)
        {
            EntitySave playerEntity;

            if (vm.PlayerCreationType == PlayerCreationType.ImportEntity)
            {
                playerEntity = await ImportPlayerEntity(vm);
            }
            else // create from options
            {
                playerEntity = CreatePlayerEntityFromOptions(vm);
            }

            if(playerEntity != null)
            {
                // If this is null, the download failed.
                // If the download fails, what do we do?

                // requires the current entity be set:
                GlueState.Self.CurrentElement = playerEntity;

                if(vm.PlayerCreationType == PlayerCreationType.SelectOptions)
                {
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
                }

            }

            return playerEntity;
        }

        private static async Task<EntitySave> ImportPlayerEntity(WizardData vm)
        {
            EntitySave playerEntity = null;
            var downloadFolder = FileManager.UserApplicationDataForThisApplication + "ImportDownload\\";

            var playerUrl = vm.PlayerEntityImportUrl;

            var destinationFileName = downloadFolder + FileManager.RemovePath(playerUrl);
                
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5), };
            var result = await NetworkManager.Self.DownloadWithProgress(
                httpClient, playerUrl, destinationFileName, null);

            if (result.Succeeded)
            {
                playerEntity = (EntitySave)GlueCommands.Self.GluxCommands.ImportScreenOrEntityFromFile(destinationFileName);
            }

            return playerEntity;
        }

        private static EntitySave CreatePlayerEntityFromOptions(WizardData vm)
        {
            EntitySave playerEntity;
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


            playerEntity = GlueCommands.Self.GluxCommands.EntityCommands.AddEntity(addEntityVm);
            if (vm.OffsetPlayerPosition)
            {
                playerEntity.SetCustomVariable("X", 64.0f);
                playerEntity.SetCustomVariable("Y", -64.0f);
            }

            return playerEntity;
        }

        private static void HandleAddPlayerInstance(WizardData vm, ScreenSave gameScreen, NamedObjectSave solidCollisionNos, 
            NamedObjectSave cloudCollisionNos, EntitySave playerEntity)
        {
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
                // rely on the entity rather than the view model, because the entity could have been
                // imported, so the view model doesn't know.
                var isPlatformer = playerEntity.Properties.GetValue<bool>("IsPlatformer");

                if (vm.CollideAgainstSolidCollision)
                {
                    PluginManager.ReactToCreateCollisionRelationshipsBetween(playerList, solidCollisionNos);

                    var nos = gameScreen.GetNamedObject("PlayerListVsSolidCollision");

                    // move is 1
                    // bounce is 2
                    // PlatformerSolid is 3
                    // PlatformerCloud is 4

                    if (isPlatformer)
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

                    if (isPlatformer)
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
