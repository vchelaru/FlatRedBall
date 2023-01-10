using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SetVariable;
using FlatRedBall.Glue.ViewModels;
using FlatRedBall.Math.Geometry;
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

        public async Task Apply(WizardViewModel vm)
        {
            #region Initialization and utility methods

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

            #endregion

            #region AddGum

            // Add Gum before adding a GameScreen, so the GameScreen gets its Gum screen
            if (vm.AddGum)
            {
                AddTask("Add Gum", () => HandleAddGum(vm));
            }

            #endregion

            #region Add GameScreen

            if (vm.AddGameScreen)
            {
                AddTask("Add GameScreen", async () =>
                {
                    var response = await HandleAddGameScreen(vm);
                    gameScreen = response.gameScreen;
                    solidCollisionNos = response.solidCollision;
                    cloudCollisionNos = response.cloudCollisionNos;

                    if (vm.IsAddGumScreenToLayerVisible && vm.AddGameScreenGumToHudLayer)
                    {
                        await HandleAddGumScreenToLayer(gameScreen);
                    }
                });
            }

            #endregion

            #region Handle AddPlayerEntity

            if (vm.AddPlayerEntity)
            {
                AddTask("Add Player", async () =>
                {
                    var playerEntity = await HandleAddPlayerEntity(vm);

                    await HandleAddPlayerInstance(vm, gameScreen, solidCollisionNos, cloudCollisionNos, playerEntity);
                });
            }

            #endregion

            #region Create Levels

            // Create the levels *after* the player, so the player gets exposed in the levels
            if (vm.AddGameScreen && vm.CreateLevels)
            {
                AddTask("Create Levels", async () =>
                    await HandleCreateLevels(vm, gameScreen));
            }

            #endregion

            #region AddCameraController

            if (vm.AddCameraController && vm.AddGameScreen)
            {
                AddTask("Create Camera", async () =>
                    await ApplyCameraValues(vm, gameScreen));
            }

            #endregion

            #region Adding Additional Screens
            if (vm.AdditionalNonGameScreens?.Count > 0)
            {
                AddTask("Adding Additional Screens", () =>
                    AddAdditionalScreens(vm));
            }
            #endregion

            #region Importing Screens/Entities
            if (vm.ElementImportUrls.Count > 0)
            {
                Add("Importing Screens/Entities", () =>
                    ImportElements(vm));
            }
            #endregion

            #region Additional serialized Objects (from json)
            if (!string.IsNullOrEmpty(vm.NamedObjectSavesSerialized))
            {
                AddTask("Adding additional Objects", () =>
                    ImportAdditionalObjects(vm.NamedObjectSavesSerialized));
            }
            #endregion

            #region Generate all code
            Add("Regenerating All Code", () =>
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateAllCode();

            });
            #endregion

            #region Flush Files

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

            #endregion

            #region Save Project

            AddTask("Saving Project", () =>
            {
                GlueCommands.Self.GluxCommands.SaveGlux(TaskExecutionPreference.AddOrMoveToEnd);
                return Task.CompletedTask;
            });

            #endregion


            #region Run All Tasks
            vm.Tasks = tasks;

            TaskItemViewModel currentTask = null;
            double maxTaskCount = 0;
            void UpdateCurrentTask(TaskEvent taskEvent, FlatRedBall.Glue.Tasks.GlueTaskBase task)
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

                if (task.Task != null) await task.Task();
                if (task.Action != null) task.Action();

                await TaskManager.Self.WaitForAllTasksFinished();
                task.IsInProgress = false;
                task.IsComplete = true;
            }

            TaskManager.Self.TaskAddedOrRemoved -= UpdateCurrentTask;

            // just in case, refresh everything
            GlueCommands.Self.RefreshCommands.RefreshTreeNodes();

            #endregion

        }

        #region Add Gum

        private static async Task HandleAddGum(WizardViewModel vm)
        {
            if (vm.AddFlatRedBallForms)
            {
                // false argument is for askToOverwrite parameter
                await PluginManager.CallPluginMethodAsync("Gum Plugin", "CreateGumProjectWithForms", false);
            }
            else
            {
                await PluginManager.CallPluginMethodAsync("Gum Plugin", "CreateGumProjectNoForms", false);
            }
        }

        #endregion

        #region Add GameScreen

        private static async Task<(ScreenSave gameScreen, NamedObjectSave solidCollision, NamedObjectSave cloudCollisionNos)> HandleAddGameScreen(WizardViewModel vm)
        {
            ScreenSave gameScreen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("GameScreen");
            NamedObjectSave solidCollisionNos = null;
            NamedObjectSave cloudCollisionNos = null;

            if (vm.AddTiledMap)
            {
                await MainAddScreenPlugin.AddMapObjectAsync(gameScreen);
            }

            if (vm.AddSolidCollision)
            {
                solidCollisionNos = await MainAddScreenPlugin.AddCollision(gameScreen, "SolidCollision",
                    setFromMapObject: vm.AddTiledMap);
            }
            if (vm.AddCloudCollision)
            {
                cloudCollisionNos = await MainAddScreenPlugin.AddCollision(gameScreen, "CloudCollision",
                    setFromMapObject: vm.AddTiledMap);
            }

            if (vm.AddHudLayer)
            {
                await AddHudLayer(gameScreen);
            }


            return (gameScreen, solidCollisionNos, cloudCollisionNos);
        }

        private async Task<NamedObjectSave> HandleAddGumScreenToLayer(ScreenSave gameScreen)
        {
            // create an object named GumScreen, add it, and then 
            var namedObjectSave = new NamedObjectSave();
            namedObjectSave.SetDefaults();
            namedObjectSave.InstanceName = "GumScreen";
            namedObjectSave.SourceType = SourceType.File;
            namedObjectSave.SourceFile = "gumproject/Screens/GameScreenGum.gusx";
            namedObjectSave.SourceName = "Entire File (GameScreenGumRuntime)";
            namedObjectSave.LayerOn = "HudLayer";

            await GlueCommands.Self.GluxCommands.AddNamedObjectToAsync(namedObjectSave, gameScreen);

            return namedObjectSave;
        }

        private static async Task<NamedObjectSave> AddHudLayer(ScreenSave gameScreen)
        {
            var addObjectViewModel = new AddObjectViewModel();
            addObjectViewModel.ForcedElementToAddTo = gameScreen;
            addObjectViewModel.SourceType = SourceType.FlatRedBallType;
            addObjectViewModel.SourceClassType = "FlatRedBall.Graphics.Layer";
            addObjectViewModel.ObjectName = "HudLayer";

            NamedObjectSave nos = null;
            nos = await GlueCommands.Self.GluxCommands.AddNewNamedObjectToAsync(addObjectViewModel, gameScreen, null);
            return nos;
        }

        #endregion

        #region Create Levels

        private static async Task HandleCreateLevels(WizardViewModel vm, ScreenSave gameScreen)
        {
            for (int i = 0; i < vm.NumberOfLevels; i++)
            {
                var levelName = "Level" + (i + 1);
                await CreateLevel(vm, gameScreen, i, levelName);
            }
        }

        private static async Task CreateLevel(WizardViewModel vm, ScreenSave gameScreen, int levelIndex0Based, string levelName)
        {
            var levelScreen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen(levelName);
            levelScreen.BaseScreen = gameScreen.Name;
            GlueCommands.Self.GluxCommands.ElementCommands.UpdateFromBaseType(levelScreen);
            GlueState.Self.CurrentScreenSave = levelScreen;


            if (levelIndex0Based == 0)
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
                var newRfs = await GlueCommands.Self.GluxCommands.CreateNewFileAndReferencedFileSaveAsync(addNewFileVm, levelScreen);

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

                bool handledByPremadeLevel = false;
                if(vm.IncludStandardTilesetInLevels && vm.IncludeGameplayLayerInLevels && vm.IncludeCollisionBorderInLevels)
                {
                    var levelIndex1Based = levelIndex0Based + 1;

                    var resourceName = $"OfficialPlugins.Wizard.EmbeddedContent.Levels.{levelName}Map.tmx";
                    var resourceStream = typeof(WizardProjectLogic).Assembly.GetManifestResourceStream(resourceName);

                    var hasPremade = resourceStream != null;
                    // dispose it, we'll use standard code to save it:
                    resourceStream?.Dispose();

                    if(hasPremade)
                    {
                        PluginManager.CallPluginMethod("Tiled Plugin", "SaveTilesetFilesToDisk");
                        
                        try
                        {
                            var rfsAbsolute = GlueCommands.Self.GetAbsoluteFilePath(newRfs);
                            FileManager.SaveEmbeddedResource(typeof(WizardProjectLogic).Assembly, resourceName, rfsAbsolute.FullPath);
                            handledByPremadeLevel = true;
                        }
                        catch
                        {
                            // couldn't save it, oh well...
                        }

                    }
                }

                if(!handledByPremadeLevel)
                {
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

        #endregion

        #region Importing Screens/Entities

        private static void ImportElements(WizardViewModel vm)
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

        #endregion

        #region Additional serialized Objects(from json)
        private async Task ImportAdditionalObjects(string namedObjectSavesSerialized)
        {
            Dictionary<string, List<NamedObjectSave>> deserialized = null;

            Exception deserializeException = null;

            try
            {
                deserialized = JsonConvert.DeserializeObject<Dictionary<string, List<NamedObjectSave>>>(namedObjectSavesSerialized);
            }
            catch (Exception e)
            {
                // we currently don't have error handling, we need it
                deserializeException = e;
            }

            List<ElementAndNosList> imports = new List<ElementAndNosList>();

            foreach (var kvp in deserialized)
            {
                var elementName = kvp.Key;
                if (elementName.StartsWith("Screens\\"))
                {
                    var screen = ObjectFinder.Self.GetScreenSave(elementName);

                    imports.Add(new ElementAndNosList
                    {
                        Element = screen,
                        NosList = kvp.Value
                    });

                }
                else if (elementName.StartsWith("Entities\\"))
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
                await AddNamedsObjectToElement(elementAndNosList.NosList, elementAndNosList.Element);
            }

            static async Task AddNamedsObjectToElement(List<NamedObjectSave> nosList, GlueElement glueElement)
            {
                if (glueElement != null)
                {
                    // lists come first, then everything else after
                    var sortedNoses = nosList.OrderBy(item => !item.IsList)
                        .ToArray();

                    foreach (var nos in sortedNoses)
                    {
                        NamedObjectSave listToAddTo = ObjectFinder.Self.GetDefaultListToContain(nos, glueElement);

                        await GlueCommands.Self.GluxCommands.AddNamedObjectToAsync(nos, glueElement, listToAddTo);

                        if (nos.ExposedInDerived)
                        {
                            await EditorObjects.IoC.Container.Get<NamedObjectSetVariableLogic>().ReactToNamedObjectChangedValue(
                                nameof(nos.ExposedInDerived),
                                // pretend the value changed from false -> true
                                false,
                                namedObjectSave: nos);
                        }

                        // remove all children, and then re-add them through the GlueCommands so that all plugins are notified:
                        if (nos.ContainedObjects.Count > 0)
                        {
                            var children = nos.ContainedObjects.ToArray();

                            nos.ContainedObjects.Clear();

                            foreach (var subNos in nos.ContainedObjects)
                            {
                                await GlueCommands.Self.GluxCommands.AddNamedObjectToAsync(subNos, glueElement, nos);

                            }

                        }
                    }
                }
            }
        }

        #endregion

        #region Player

        private static async Task<EntitySave> HandleAddPlayerEntity(WizardViewModel vm)
        {
            EntitySave playerEntity;

            if (vm.PlayerCreationType == PlayerCreationType.ImportEntity)
            {
                var importTask = ImportPlayerEntity(vm);
                importTask.Wait();
                playerEntity = importTask.Result;
            }
            else // create from options
            {
                playerEntity = await CreatePlayerEntityFromOptions(vm);
            }


            if (playerEntity != null)
            {
                // If this is null, the download failed.
                // If the download fails, what do we do?

                // requires the current entity be set:
                GlueState.Self.CurrentElement = playerEntity;

                if (vm.PlayerCreationType == PlayerCreationType.SelectOptions)
                {
                    if (vm.PlayerControlType == GameType.Platformer)
                    {
                        // mark as platformer
                        PluginManager.CallPluginMethod("Entity Input Movement Plugin", "MakeCurrentEntityPlatformer");

                        if(vm.ShowAddPlayerSpritePlatformerAnimations && vm.AddPlayerSpritePlatformerAnimations)
                        {
                            await AddPlayerPlatformerAnimations(playerEntity);
                        }

                        if(vm.ShowAddPlatformAnimatorController && vm.AddPlatformerAnimationController)
                        {
                            await AddPlayerPlatformerAnimationController(playerEntity);
                        }

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

        private static async Task AddPlayerPlatformerAnimations(EntitySave playerEntity)
        {
            var playerContentFolder = GlueCommands.Self.FileCommands.GetContentFolder(playerEntity);
            System.IO.Directory.CreateDirectory(playerContentFolder);

            // 1 - save the PNG to the right location
            FileManager.SaveEmbeddedResource(typeof(WizardProjectLogic).Assembly,
                "OfficialPlugins.Wizard.EmbeddedContent.Platformer.FRBeefcakeSpritesheet.png",
                playerContentFolder + "FRBeefcakeSpriteSheet.png");

            // 2 - save the .achx to the right locatoin
            FlatRedBall.IO.FilePath achxDestinationPath = playerContentFolder + "PlatformerAnimations.achx";
            FileManager.SaveEmbeddedResource(typeof(WizardProjectLogic).Assembly,
                "OfficialPlugins.Wizard.EmbeddedContent.Platformer.PlatformerAnimations.achx",
                achxDestinationPath.FullPath);

            // 3 - add the .achx to the Player entity
            await GlueCommands.Self.GluxCommands.CreateReferencedFileSaveForExistingFileAsync(playerEntity, achxDestinationPath);

            // 4 - Set the animations on the sprite
            var sprite = playerEntity.AllNamedObjects.FirstOrDefault(item => item.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Sprite);
            if(sprite != null)
            {
                await GlueCommands.Self.GluxCommands.SetVariableOnAsync(
                    sprite,
                    nameof(FlatRedBall.Sprite.AnimationChains),
                    "PlatformerAnimations",
                    false, false
                    );

                await GlueCommands.Self.GluxCommands.SetVariableOnAsync(
                    sprite,
                    nameof(FlatRedBall.Sprite.CurrentChainName),
                    "CharacterWalkRight",
                    false, false
                    );

            }
        }

        private static Task AddPlayerPlatformerAnimationController(EntitySave playerEntity)
        {
            var whereToSave = GlueCommands.Self.GetAbsoluteFilePath(playerEntity).GetDirectoryContainingThis() +
                "Player.PlatformerAnimations.json";
            var resourceName = "OfficialPlugins.Wizard.EmbeddedContent.Platformer.Player.PlatformerAnimations.json";

            GlueCommands.Self.TryMultipleTimes(() =>
                FileManager.SaveEmbeddedResource(typeof(WizardProjectLogic).Assembly, resourceName, whereToSave));

            return Task.CompletedTask;
        }

        private static async Task<EntitySave> ImportPlayerEntity(WizardViewModel vm)
        {
            EntitySave playerEntity = null;
            var downloadFolder = FileManager.UserApplicationDataForThisApplication + "ImportDownload\\";

            if (FileManager.IsUrl(vm.PlayerEntityImportUrlOrFile) == false)
            {
                playerEntity = (EntitySave)
                    (await GlueCommands.Self.GluxCommands.ImportScreenOrEntityFromFile(vm.PlayerEntityImportUrlOrFile));
            }
            else
            {
                var playerUrl = vm.PlayerEntityImportUrlOrFile;

                var destinationFileName = downloadFolder + FileManager.RemovePath(playerUrl);

                using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5), };
                var result = await NetworkManager.Self.DownloadWithProgress(
                    httpClient, playerUrl, destinationFileName, null);

                if (result.Succeeded)
                {
                    playerEntity = (EntitySave)
                        (await GlueCommands.Self.GluxCommands.ImportScreenOrEntityFromFile(destinationFileName));
                }
            }

            return playerEntity;
        }

        private static async Task<EntitySave> CreatePlayerEntityFromOptions(WizardViewModel vm)
        {

            EntitySave playerEntity = null;
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

            if(vm.IsPlayerDamageableChecked)
            {
                addEntityVm.IsIDamageableChecked= true;
            }

            addEntityVm.IsICollidableChecked = true;

            addEntityVm.IsSpriteChecked = vm.AddPlayerSprite;

            addEntityVm.IncludeListsInScreens = vm.AddPlayerListToGameScreen;

            playerEntity = await GlueCommands.Self.GluxCommands.EntityCommands.AddEntityAsync(addEntityVm);



            if (playerEntity == null)
            {
                int m = 3;
            }

            if (vm.PlayerControlType == GameType.Platformer && vm.PlayerCollisionType == CollisionType.Rectangle)
            {
                // this should have an AARect, so let's adjust it to match the right size/position as explained here:
                // https://github.com/vchelaru/FlatRedBall/issues/651
                var aaRectNos = playerEntity.AllNamedObjects.FirstOrDefault(item => item.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.AxisAlignedRectangle);

                if (aaRectNos != null)
                {
                    await GlueCommands.Self.GluxCommands.SetVariableOnAsync(
                        aaRectNos,
                        nameof(AxisAlignedRectangle.Y),
                        8.0f,
                        performSaveAndGenerateCode: false,
                        updateUi: false);
                    await GlueCommands.Self.GluxCommands.SetVariableOnAsync(
                        aaRectNos,
                        nameof(AxisAlignedRectangle.Width),
                        10.0f,
                        performSaveAndGenerateCode: false,
                        updateUi: false);
                    await GlueCommands.Self.GluxCommands.SetVariableOnAsync(
                        aaRectNos,
                        nameof(AxisAlignedRectangle.Height),
                        16.0f,
                        performSaveAndGenerateCode: false,
                        updateUi: false);
                }
            }

            return playerEntity;
        }

        private static async Task HandleAddPlayerInstance(WizardViewModel vm, ScreenSave gameScreen, NamedObjectSave solidCollisionNos,
            NamedObjectSave cloudCollisionNos, EntitySave playerEntity)
        {
            NamedObjectSave playerList = null;
            if (vm.AddGameScreen && vm.AddPlayerListToGameScreen)
            {
                playerList = gameScreen.NamedObjects.FirstOrDefault(item => item.IsList && item.SourceClassGenericType == playerEntity.Name);
                // This is handled above by the variable being assigned on the entity creation.
                //{
                //    AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                //    addObjectViewModel.ForcedElementToAddTo = gameScreen;
                //    addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                //    addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.PositionedObjectList;
                //    addObjectViewModel.SourceClassGenericType = playerEntity.Name;
                //    addObjectViewModel.ObjectName = $"{playerEntity.GetStrippedName()}List";

                //    playerList = await GlueCommands.Self.GluxCommands.AddNewNamedObjectToAsync(addObjectViewModel, gameScreen, null);
                //}

                if (vm.AddPlayerToList)
                {
                    AddObjectViewModel addPlayerVm = new AddObjectViewModel();

                    addPlayerVm.SourceType = SourceType.Entity;
                    addPlayerVm.SourceClassType = playerEntity.Name;
                    addPlayerVm.ObjectName = "Player1";

                    var playerNos = await GlueCommands.Self.GluxCommands.AddNewNamedObjectToAsync(addPlayerVm, gameScreen, playerList);

                    playerNos.SetVariable("X", 64.0f);
                    playerNos.SetVariable("Y", -64.0f);
                    playerNos.ExposedInDerived = true; // do we have to push this to the derived screens? No, because screens are created after

                }
            }

            if (vm.AddGameScreen && vm.AddPlayerListToGameScreen)
            {
                // rely on the entity rather than the view model, because the entity could have been
                // imported, so the view model doesn't know.
                var isPlatformer = playerEntity.Properties.GetValue<bool>("IsPlatformer");

                // Add cloud collision first, then solid collision so that solid collision
                // is performed after cloud. Otherwise when cloud and solid meet, snagging can happen

                if (vm.CollideAgainstCloudCollision && vm.AddCloudCollision)
                {
                    if (cloudCollisionNos == null)
                    {
                        throw new NullReferenceException(nameof(cloudCollisionNos));
                    }
                    await PluginManager.ReactToCreateCollisionRelationshipsBetween(playerList, cloudCollisionNos);

                    var nos =
                        gameScreen.GetNamedObject("PlayerVsCloudCollision") ??
                        gameScreen.GetNamedObject("PlayerListVsCloudCollision");

                    if (isPlatformer)
                    {
                        nos.Properties.SetValue("CollisionType", 4);
                    }

                    PluginManager.CallPluginMethod("Collision Plugin", "FixNamedObjectCollisionType", nos);
                }

                if (vm.CollideAgainstSolidCollision && vm.AddSolidCollision)
                {
                    if (solidCollisionNos == null)
                    {
                        throw new NullReferenceException(nameof(solidCollisionNos));
                    }
                    await PluginManager.ReactToCreateCollisionRelationshipsBetween(playerList, solidCollisionNos);

                    var nos =
                        gameScreen.GetNamedObject("PlayerVsSolidCollision") ??
                        gameScreen.GetNamedObject("PlayerListVsSolidCollision");

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
            }
        }


        #endregion

        #region Additional Screens

        private static async Task AddAdditionalScreens(WizardViewModel vm)
        {
            foreach (var screenName in vm.AdditionalNonGameScreens)
            {
                await TaskManager.Self.AddAsync(async () => await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen(screenName), $"Adding screen {screenName}");
            }
        }

        #endregion

        private static async Task ApplyCameraValues(WizardViewModel vm, ScreenSave gameScreen)
        {
            await ApplyCameraController(vm, gameScreen);

            await ApplyMainCameraSettings(vm);
        }

        private static async Task ApplyMainCameraSettings(WizardViewModel vm)
        {
            int width = 800;
            int height = 600;
            decimal aspectRatioWidth = 4;
            decimal aspectRatioHeight = 3;

            int scalePercent = vm.ScalePercent;
            if(scalePercent <= 0)
            {
                scalePercent = 100;
            }

            switch(vm.SelectedCameraResolution)
            {
                case CameraResolution._256x224:
                    width = 256;
                    height = 224;
                    aspectRatioWidth = 8;
                    aspectRatioHeight = 7;
                    break;
                case CameraResolution._360x240:
                    width = 360;
                    height = 240;
                    aspectRatioWidth = 3;
                    aspectRatioHeight = 2;
                    break;
                case CameraResolution._480x360:
                    width = 480;
                    height = 360;
                    aspectRatioWidth = 4;
                    aspectRatioHeight = 3;
                    break;
                case CameraResolution._640x480:
                    width = 640;
                    height = 480;
                    aspectRatioWidth = 4;
                    aspectRatioHeight = 3; 
                    break;
                case CameraResolution._800x600:
                    width = 800;
                    height = 600;
                    aspectRatioWidth = 4;
                    aspectRatioHeight = 3; 
                    break;
                case CameraResolution._1024x768:
                    width = 1024;
                    height = 768;
                    aspectRatioWidth = 4;
                    aspectRatioHeight = 3; 
                    break;
                case CameraResolution._1920x1080:
                    width = 1920;
                    height = 1080;
                    aspectRatioWidth = 16;
                    aspectRatioHeight = 9; 
                    break;

            }

            await TaskManager.Self.AddAsync(() =>
            {
                var displaySettings = GlueState.Self.CurrentGlueProject.DisplaySettings;
                displaySettings.ResolutionWidth = width;
                displaySettings.ResolutionHeight = height;

                displaySettings.AspectRatioWidth = aspectRatioWidth;
                displaySettings.AspectRatioHeight = aspectRatioHeight;
                displaySettings.FixedAspectRatio = true;

                displaySettings.Scale = scalePercent;

            }, "Setting display settings");
        }

        private static async Task ApplyCameraController(WizardViewModel vm, ScreenSave gameScreen)
        {
            var addCameraControllerVm = new AddObjectViewModel();
            addCameraControllerVm.ForcedElementToAddTo = gameScreen;
            addCameraControllerVm.SourceType = SourceType.FlatRedBallType;
            addCameraControllerVm.SourceClassType = "FlatRedBall.Entities.CameraControllingEntity";
            addCameraControllerVm.ObjectName = "CameraControllingEntityInstance";

            var cameraNos = await GlueCommands.Self.GluxCommands.AddNewNamedObjectToAsync(addCameraControllerVm, gameScreen, null, selectNewNos: false);

            if (vm.FollowPlayersWithCamera && vm.AddPlayerListToGameScreen)
            {
                await GlueCommands.Self.GluxCommands.SetVariableOnAsync(
                    cameraNos,
                    nameof(FlatRedBall.Entities.CameraControllingEntity.Targets),
                    value: "PlayerList",
                    performSaveAndGenerateCode: false,
                    updateUi: false);
            }
            if (vm.KeepCameraInMap && vm.AddTiledMap)
            {
                await GlueCommands.Self.GluxCommands.SetVariableOnAsync(
                    cameraNos,
                    nameof(FlatRedBall.Entities.CameraControllingEntity.Map),
                    value: "Map",
                    performSaveAndGenerateCode: false,
                    updateUi: false
                    );
            }
        }
    }
}
