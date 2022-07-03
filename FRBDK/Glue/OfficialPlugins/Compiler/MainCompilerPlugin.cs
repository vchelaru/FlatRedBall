using FlatRedBall.Glue.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.ComponentModel.Composition;
using OfficialPlugins.Compiler.ViewModels;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Managers;
using System.Windows;
using OfficialPlugins.Compiler.CodeGeneration;
using System.Net.Sockets;
using OfficialPlugins.Compiler.Managers;
using FlatRedBall.Glue.Controls;
using System.ComponentModel;
using FlatRedBall.Glue.IO;
using Newtonsoft.Json;
using OfficialPlugins.Compiler.Models;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using OfficialPluginsCore.Compiler.ViewModels;
using OfficialPluginsCore.Compiler.Managers;
using System.Diagnostics;
using System.Timers;
using Glue;
using OfficialPluginsCore.Compiler.CommandReceiving;
using FlatRedBall.Glue.Elements;
using OfficialPlugins.Compiler.Dtos;
using OfficialPlugins.Compiler.CommandSending;
using System.Runtime.InteropServices;
using OfficialPlugins.GameHost.Views;
using OfficialPlugins.Compiler.Views;
using FlatRedBall.Glue.FormHelpers;
using GlueFormsCore.FormHelpers;
using System.Windows.Media.Imaging;
using FlatRedBall.Glue.ViewModels;
using OfficialPlugins.Compiler.CodeGeneration.GlueCalls;

namespace OfficialPlugins.Compiler
{
    [Export(typeof(PluginBase))]
    public class MainCompilerPlugin : PluginBase
    {
        #region Fields/Properties


        Compiler compiler;
        Runner runner;

        public CompilerViewModel CompilerViewModel { get; private set; }
        public GlueViewSettingsViewModel GlueViewSettingsViewModel { get; private set; }
        public BuildTabView MainControl { get; private set; } 

        public static CompilerViewModel MainViewModel { get; private set; }


        PluginTab buildTab;
        PluginTab glueViewSettingsTab;
        GlueViewSettings glueViewSettingsView;

        Game1GlueControlGenerator game1GlueControlGenerator;

        public override string FriendlyName => "Glue Compiler";

        public override Version Version
        {
            get
            {
                // 0.4 introduces:
                // - multicore building
                // - Removed warnings and information when building - now we just show start, end, and errors
                // - If an error occurs, a popup appears telling the user that the game crashed, and to open Visual Studio
                // 0.5
                // - Support for running content-only builds
                // 0.6
                // - Added VS 2017 support
                // 0.7
                // - Added a list of MSBuild locations
                // 1.0
                // - This was added long ago, but might as well mark it 1.0
                return new Version(1, 0);
            }
        }

        FilePath JsonSettingsFilePath => GlueState.Self.ProjectSpecificSettingsFolder + "CompilerSettings.json";

        bool ignoreViewModelChanges = false;

        Timer busyUpdateTimer;
        Timer dragDropTimer;

        System.Threading.SemaphoreSlim getCommandsSemaphore = new System.Threading.SemaphoreSlim(1, 1);
        DateTime lastGetCall;

        #endregion

        #region Startup

        public override void StartUp()
        {
            CreateBuildControl();

            CreateToolbar();

            RefreshManager.Self.InitializeEvents(this.MainControl.PrintOutput, this.MainControl.PrintOutput);

            Output.Initialize(this.MainControl.PrintOutput);


            compiler = Compiler.Self;
            runner = Runner.Self;

            runner.AfterSuccessfulRun += async () =>
            {
                // If we aren't generating the code, we shouldn't try to move the game to Glue since the borders can't be adjusted
                if(CompilerViewModel.IsGenerateGlueControlManagerInGame1Checked && GlueViewSettingsViewModel.EmbedGameInGameTab)
                {
                    MoveGameToHost();
                }
                

                if(CompilerViewModel.PlayOrEdit == PlayOrEdit.Edit)
                {
                    await ReactToPlayOrEditSet();
                }

            };
            runner.OutputReceived += (output) =>
            {
                MainControl.PrintOutput(output);
            };

            game1GlueControlGenerator = new Game1GlueControlGenerator();
            this.RegisterCodeGenerator(game1GlueControlGenerator);

            this.RegisterCodeGenerator(new CompilerPluginElementCodeGenerator());




            // winforms stuff is here:
            // https://social.msdn.microsoft.com/Forums/en-US/f6e28fe1-03b2-4df5-8cfd-7107c2b6d780/hosting-external-application-in-windowsformhost?forum=wpf
            gameHostView = new GameHostView();
            gameHostView.DataContext = CompilerViewModel;
            gameHostView.TreeNodedDroppedInEditBar += (treeNode) =>
            {
                if(treeNode.Tag is StateSave stateSave)
                {
                    var container = ObjectFinder.Self.GetElementContaining(stateSave);
                    if(container is EntitySave entitySave)
                    {
                        HandleAddToEditToolbar(stateSave, entitySave, null);
                    }
                }
                else if(treeNode.Tag is EntitySave entitySave)
                {
                    // todo finish here
                    HandleAddToEditToolbar(null, entitySave, null);
                }
            };

            pluginTab = base.CreateTab(gameHostView, "Game", TabLocation.Center);
            pluginTab.CanClose = false;
            pluginTab.AfterHide += (_, __) => TryKillGame();
            //pluginTab = base.CreateAndAddTab(GameHostView, "Game Contrll", TabLocation.Bottom);

            // do this after creating the compiler, view model, and control
            AssignEvents();



            GameHostController.Self.Initialize(gameHostView, MainControl, 
                CompilerViewModel, 
                GlueViewSettingsViewModel,
                glueViewSettingsTab);

            #region Start the timer, do it after the gameHostView is created

            var busyTimerFrequency = 250; // ms
            busyUpdateTimer = new Timer(busyTimerFrequency);
            busyUpdateTimer.Elapsed += async (not, used) => await UpdateIsBusyStatus();
            busyUpdateTimer.SynchronizingObject = MainGlueWindow.Self;
            busyUpdateTimer.Start();

            // This was 250 but it wasn't fast enough to feel responsive
            var dragDropTimerFrequency = 100; // ms
            dragDropTimer = new Timer(dragDropTimerFrequency);
            dragDropTimer.Elapsed += (not, used) => DragDropManagerGameWindow.HandleDragDropTimerElapsed(gameHostView);
            dragDropTimer.SynchronizingObject = MainGlueWindow.Self;
            dragDropTimer.Start();

            #endregion
        }

        private void AssignEvents()
        {
            var manager = new FileChangeManager(MainControl, compiler, CompilerViewModel);
            
            this.ReactToLoadedGlux += HandleGluxLoaded;
            this.ReactToUnloadedGlux += HandleGluxUnloaded;
            
            this.ReactToNewFileHandler += RefreshManager.Self.HandleNewFile;
            this.ReactToFileChangeHandler += manager.HandleFileChanged;
            this.ReactToCodeFileChange += RefreshManager.Self.HandleFileChanged;

            this.ReactToChangedPropertyHandler += HandlePropertyChanged;

            this.NewEntityCreated += RefreshManager.Self.HandleNewEntityCreated;
            this.NewScreenCreated += async (newScreen) =>
            {
                ToolbarController.Self.HandleNewScreenCreated(newScreen);
                await RefreshManager.Self.HandleNewScreenCreated();
            };
            this.ReactToScreenRemoved += ToolbarController.Self.HandleScreenRemoved;
            // todo - handle startup changed...

            this.ReactToNewObjectHandler += RefreshManager.Self.HandleNewObjectCreated;
            this.ReactToNewObjectListAsync += RefreshManager.Self.HandleNewObjectList;

            this.ReactToObjectRemoved += async (owner, nos) =>
                await RefreshManager.Self.HandleObjectRemoved(owner, nos);
            this.ReactToObjectListRemoved += async (ownerList, list) =>
                await RefreshManager.Self.HandleObjectListRemoved(ownerList, list);


            this.ReactToElementVariableChange += HandleElementVariableChanged;
            this.ReactToNamedObjectChangedValueList += (changeList) => RefreshManager.Self.ReactToNamedObjectChangedValueList(changeList, AssignOrRecordOnly.Assign);
            this.ReactToNamedObjectChangedValue += HandleNamedObjectVariableOrPropertyChanged;
            this.ReactToChangedStartupScreen += ToolbarController.Self.ReactToChangedStartupScreen;
            this.ReactToItemSelectHandler += HandleItemSelected;
            this.ReactToObjectContainerChanged += RefreshManager.Self.HandleObjectContainerChanged;
            // If a variable is added, that may be used later to control initialization.
            // The game won't reflect that until it has been restarted, so let's just take 
            // care of it now. For variable removal I don't know if any restart is needed...
            this.ReactToVariableAdded += RefreshManager.Self.HandleVariableAdded;
            this.ReactToChangedPropertyHandler += (changedMember, oldValue, owner) =>
            {
                if(changedMember == nameof(CustomVariable.Name) && GlueState.Self.CurrentCustomVariable != null)
                {
                    RefreshManager.Self.HandleVariableRenamed(GlueState.Self.CurrentCustomVariable);
                }
            };
            this.ReactToStateCreated += RefreshManager.Self.HandleStateCreated;
            this.ReactToStateVariableChanged += RefreshManager.Self.HandleStateVariableChanged;
            this.ReactToStateCategoryExcludedVariablesChanged += RefreshManager.Self.HandleStateCategoryExcludedVariablesChanged;
            //this.ReactToMainWindowMoved += gameHostView.ReactToMainWindowMoved;
            this.ReactToMainWindowResizeEnd += gameHostView.ReactToMainWindowResizeEnd;
            this.TryHandleTreeNodeDoubleClicked += RefreshManager.Self.HandleTreeNodeDoubleClicked;
            this.GrabbedTreeNodeChanged += HandleGrabbedTreeNodeChanged;

            this.ReactToLoadedGlux += () => pluginTab.Show();
            this.ReactToUnloadedGlux += () => pluginTab.Hide();
            this.ReactToTreeViewRightClickHandler += HandleTreeViewRightClick;
        }

        private void HandleTreeViewRightClick(ITreeNode rightClickedTreeNode, List<GeneralToolStripMenuItem> listToAddTo)
        {
            var tag = rightClickedTreeNode.Tag;

            if(tag != null)
            {
                if(tag is StateSave asStateSave)
                {
                    listToAddTo.Add("Add State to Edit Toolbar", HandleAddStateToEditToolbar);
                }
            }
        }

        private async void HandleAddStateToEditToolbar(object sender, EventArgs e)
        {
            var state = GlueState.Self.CurrentStateSave;
            var entitySave = GlueState.Self.CurrentEntitySave;
            var namedObject = GlueState.Self.CurrentNamedObjectSave;



            HandleAddToEditToolbar(state, entitySave, namedObject);

        }

        private async void HandleAddToEditToolbar(StateSave state, EntitySave entitySave, NamedObjectSave namedObject)
        {

            //////////////////////////Early Out////////////////////////////
            var alreadyHasMatch = CompilerViewModel.ToolbarEntitiesAndStates.Any(item =>
                item.StateSave == state &&
                item.GlueElement == entitySave);
            if(alreadyHasMatch)
            {
                return;
            }
            ////////////////////////End Early Out//////////////////////////

            if (entitySave != null)
            {
                var category = ObjectFinder.Self.GetStateSaveCategory(state);
                // As of 2022 we really don't mess with uncategorized states anymore. Add this check for old projects:
                if (category != null)
                {
                    // The state category must be exposed as a variable...
                    var variableName = "Current" + category.Name + "State";
                    if (entitySave.GetCustomVariableRecursively(variableName) == null)
                    {
                        await GlueCommands.Self.GluxCommands.ElementCommands.AddStateCategoryCustomVariableToElementAsync(category, entitySave);
                    }
                }


                var newViewModel = new ToolbarEntityAndStateViewModel();
                newViewModel.GlueElement = entitySave;
                newViewModel.StateSave = state;
                newViewModel.Clicked += () =>
                {
                    var canEdit = CompilerViewModel.IsRunning && CompilerViewModel.IsEditChecked;
                    if(!canEdit)
                    {
                        return;
                    }

                    var element = GlueState.Self.CurrentElement;

                    NamedObjectSave newNos = null;

                    if (element != null)
                    {
                        var addObjectViewModel = new AddObjectViewModel();
                        addObjectViewModel.SourceType = SourceType.Entity;
                        addObjectViewModel.SelectedEntitySave = entitySave;

                        var listToAddTo = ObjectFinder.Self.GetDefaultListToContain(entitySave.Name, element);

                        newNos = GlueCommands.Self.GluxCommands.AddNewNamedObjectTo(
                            addObjectViewModel,
                            element,
                            listToAddTo);
                    }

                    if (newNos != null && state != null)
                    {
                        // Set the state variable on the new NOS 

                        var category = ObjectFinder.Self.GetStateSaveCategory(state);

                        if (category != null)
                        {
                            var variableName = $"Current{category.Name}State";
                            GlueCommands.Self.GluxCommands.SetVariableOn(newNos, variableName, state.Name);
                        }
                    }
                };
                newViewModel.RemovedFromToolbar += () =>
                {
                    CompilerViewModel.ToolbarEntitiesAndStates.Remove(newViewModel);
                };
                newViewModel.ForceRefreshPreview += () => 
                {
                    newViewModel.SetSourceFromElementAndState(force:true);
                };
                newViewModel.ViewInExplorer += () =>
                {
                    var filePath = GlueCommands.Self.GluxCommands.GetPreviewLocation(entitySave, state);
                    GlueCommands.Self.FileCommands.ViewInExplorer(filePath);
                };
                newViewModel.DragLeave += () =>
                {
                    if(GlueState.Self.DraggedTreeNode == null)
                    {
                        // Simulate having grabbed the tree node
                        var tag = (object)state ?? entitySave;
                        var treeNode = GlueState.Self.Find.TreeNodeByTag(tag);
                        GlueState.Self.DraggedTreeNode = treeNode;
                    }

                };

                newViewModel.SetSourceFromElementAndState();


                CompilerViewModel.ToolbarEntitiesAndStates.Add(newViewModel);
            }
        }

        private void HandlePropertyChanged(string changedMember, object oldValue, GlueElement glueElement)
        {
            var currentEntity = glueElement as EntitySave;
            if(changedMember == nameof(EntitySave.CreatedByOtherEntities) && currentEntity != null)
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateGame1();
            }
        }

        private void HandleElementVariableChanged(IElement element, CustomVariable variable)
        {
            RefreshManager.Self.HandleVariableChanged(element, variable);
        }

        private void HandleNamedObjectVariableOrPropertyChanged(string changedMember, object oldValue, NamedObjectSave namedObject)
        {
            RefreshManager.Self.HandleNamedObjectVariableOrPropertyChanged(changedMember, oldValue, namedObject, Dtos.AssignOrRecordOnly.Assign);
        }

        private void HandleItemSelected(ITreeNode selectedTreeNode)
        {
            RefreshManager.Self.HandleItemSelected(selectedTreeNode);
            this.gameHostView.UpdateToItemSelected();
        }


        #endregion

        #region Public events (called externally)

        public async Task BuildAndRun()
        {
            if (CompilerViewModel.IsToolbarPlayButtonEnabled)
            {
                GlueCommands.Self.DialogCommands.FocusTab("Build");
                var succeeded = await GameHostController.Self.Compile();

                if (succeeded)
                {
                    bool hasErrors = GetIfHasErrors();
                    if (hasErrors)
                    {
                        var runAnywayMessage = "Your project has content errors. To fix them, see the Errors tab. You can still run the game but you may experience crashes. Run anyway?";
                        GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(runAnywayMessage, async () =>
                        {
                            await runner.Run(preventFocus: false);
                            CompilerViewModel.IsEditChecked = false;
                        });
                    }
                    else
                    {
                        PluginManager.ReceiveOutput("Building succeeded. Running project...");

                        CompilerViewModel.IsEditChecked = false;
                        await runner.Run(preventFocus: false);
                    }
                }
                else
                {
                    GlueCommands.Self.DialogCommands.FocusTab("Build");
                }
            }
        }

        public bool GetIfIsRunningInEditMode()
        {
            return CompilerViewModel.IsEditChecked && CompilerViewModel.IsRunning;
        }

        public async Task<string> MakeGameBorderless(bool isBorderless)
        {
            var dto = new Dtos.SetBorderlessDto
            {
                IsBorderless = isBorderless
            };

            var sendResponse = await CommandSending.CommandSender.Send(dto);
            return sendResponse.Succeeded ? sendResponse.Data : String.Empty;
        }

        #endregion

        private async Task UpdateIsBusyStatus()
        {
            this.CompilerViewModel.LastWaitTimeInSeconds = (DateTime.Now - lastGetCall).TotalSeconds;
            var isBusy = (await getCommandsSemaphore.WaitAsync(0)) == false;

            if (!isBusy)
            {
                try
                {
                    if (CompilerViewModel.IsRunning)
                    {
                        lastGetCall = DateTime.Now;


                        var sendResponse =
                            await CommandSending.CommandSender
                            .Send<GetCommandsDtoResponse>(new GetCommandsDto(), isImportant: false);
                        var response = sendResponse?.Data;


                        var getTime = DateTime.Now;
                        var getDuration = getTime - lastGetCall;

                        if (response?.Commands.Count > 0)
                        {
                            await CommandReceiver.HandleCommandsFromGame(response.Commands,
                                GlueViewSettingsViewModel.PortNumber);
                        }
                        else
                        {

                        }

                        var handleTime = DateTime.Now;
                        var handleDuration = handleTime - getTime;

                        this.CompilerViewModel.LastWaitTimeInSeconds = (DateTime.Now - lastGetCall).TotalSeconds;

                        // Vic says - this causes problems when a game crashes. It continues to print this out
                        // which makes it harder to see the callstack. I don't know if this is needed anymore now
                        // that we have a more reliable communication system from glue<->game, so I'm going to comment
                        // this out. If it's needed in the future, maybe we need some way to know the game has crashed.
                        //if (this.CompilerViewModel.LastWaitTimeInSeconds > 1)
                        //{

                        //    MainControl.PrintOutput(
                        //        $"Warning - it took {this.CompilerViewModel.LastWaitTimeInSeconds:0.00} seconds to get " +
                        //        $"{response?.Commands.Count}" +
                        //        $"\n\tGet: {getDuration}" +
                        //        $"\n\tHandle: {handleDuration}");
                        //}
                    }
                }
                catch
                {
                    // it's okay
                }
                finally
                {
                    getCommandsSemaphore.Release();
                }

            }
            else
            {
                System.Diagnostics.Debug.WriteLine("   isBusy = true");

            }
        }

        private void HandleGluxUnloaded()
        {
            CompilerViewModel.HasLoadedGlux = false;
            CompilerViewModel.ToolbarEntitiesAndStates.Clear();

            glueViewSettingsTab.Hide();

            gameHostView.HandleGluxUnloaded();


            ToolbarController.Self.HandleGluxUnloaded();
        }

        private CompilerSettingsModel LoadOrCreateCompilerSettings()
        {
            CompilerSettingsModel compilerSettings = null;
            var filePath = JsonSettingsFilePath;
            if (filePath.Exists())
            {
                try
                {
                    var text = System.IO.File.ReadAllText(filePath.FullPath);
                    compilerSettings = JsonConvert.DeserializeObject<CompilerSettingsModel>(text);
                }
                catch
                {
                    // do nothing, it'll just get wiped out and re-saved later
                }
            }

            if(compilerSettings == null)
            {
                var random = new Random();
                compilerSettings = new CompilerSettingsModel();
                compilerSettings.SetDefaults();
                // randomize it a little to reduce the likelihood of it being the same as a different game.
                // Before, it was always 8021
                compilerSettings.PortNumber = 8000 + random.Next(1000);
            }

            return compilerSettings;
        }

        private bool IsFrbNewEnough()
        {
            var mainProject = GlueState.Self.CurrentMainProject;
            if(mainProject.IsFrbSourceLinked())
            {
                return true;
            }
            else
            {
                return GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.SupportsEditMode;
            }
        }

        private void HandleGluxLoaded()
        {
            var model = LoadOrCreateCompilerSettings();
            ignoreViewModelChanges = true;
            GlueViewSettingsViewModel.SetFrom(model);
            glueViewSettingsView.DataUiGrid.Refresh();

            CompilerViewModel.IsGenerateGlueControlManagerInGame1Checked = GlueViewSettingsViewModel.EnableGameEditMode;
            ignoreViewModelChanges = false;

            CompilerViewModel.IsGluxVersionNewEnoughForGlueControlGeneration =
                GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.AddedGeneratedGame1;
            CompilerViewModel.ToolbarEntitiesAndStates.Clear();
            CompilerViewModel.HasLoadedGlux = true;
            //CompilerViewModel.SetFrom(model);
            foreach (var toolbarModel in model.ToolbarEntitiesAndStates)
            {
                var entitySave = ObjectFinder.Self.GetEntitySave(toolbarModel.EntityName);

                if(entitySave != null)
                {
                    StateSaveCategory category = null;
                    category = entitySave.GetStateCategory(toolbarModel.CategoryName);
                    var state = category?.GetState(toolbarModel.StateName) ?? entitySave.States.FirstOrDefault(item => item.Name == toolbarModel.StateName);
                    HandleAddToEditToolbar(state, entitySave, null);

                }

            }

            game1GlueControlGenerator.PortNumber = model.PortNumber;
            game1GlueControlGenerator.IsGlueControlManagerGenerationEnabled = model.GenerateGlueControlManagerCode && IsFrbNewEnough();

            RefreshManager.Self.PortNumber = model.PortNumber;

            ToolbarController.Self.HandleGluxLoaded();

            if(IsFrbNewEnough())
            {
                TaskManager.Self.Add(() => EmbeddedCodeManager.EmbedAll(model.GenerateGlueControlManagerCode), "Generate Glue Control Code");
                TaskManager.Self.Add(() => GlueCallsCodeGenerator.GenerateAll(), "Generate Glue Control Code New");
            }

            GlueCommands.Self.ProjectCommands.AddNugetIfNotAdded("Newtonsoft.Json", "12.0.3");

            gameHostView.HandleGluxLoaded();
        }

        private void CreateToolbar()
        {
            var toolbar = new RunnerToolbar();
            toolbar.RunClicked += HandleToolbarRunClicked;

            ToolbarController.Self.Initialize(toolbar);

            toolbar.DataContext = ToolbarController.Self.GetViewModel();

            base.AddToToolBar(toolbar, "Standard");
        }

        private async void HandleToolbarRunClicked(object sender, EventArgs e)
        {
            // force the view model to not be in edit mode if this was clicked
            CompilerViewModel.PlayOrEdit = PlayOrEdit.Play;
            await BuildAndRun();
        }


        private void CreateBuildControl()
        {
            CompilerViewModel = new CompilerViewModel();
            CompilerViewModel.Configuration = "Debug";
            GlueViewSettingsViewModel = new GlueViewSettingsViewModel();
            GlueViewSettingsViewModel.PropertyChanged += HandleGlueViewSettingsViewModelPropertyChanged;
            CompilerViewModel.PropertyChanged += HandleCompilerViewModelPropertyChanged;

            MainViewModel = CompilerViewModel;

            MainControl = new BuildTabView();
            MainControl.DataContext = CompilerViewModel;

            Runner.Self.ViewModel = CompilerViewModel;
            RefreshManager.Self.ViewModel = CompilerViewModel;
            DragDropManagerGameWindow.CompilerViewModel = CompilerViewModel;
            CommandReceiver.CompilerViewModel = CompilerViewModel;
            CommandReceiver.PrintOutput = MainControl.PrintOutput;
            RefreshManager.Self.GlueViewSettingsViewModel = GlueViewSettingsViewModel;

            VariableSendingManager.Self.ViewModel = CompilerViewModel;
            VariableSendingManager.Self.GlueViewSettingsViewModel = GlueViewSettingsViewModel;

            CommandSender.GlueViewSettingsViewModel = GlueViewSettingsViewModel;
            CommandSender.CompilerViewModel = CompilerViewModel;
            CommandSender.PrintOutput = MainControl.PrintOutput;

            buildTab = base.CreateTab(MainControl, "Build", TabLocation.Bottom);
            buildTab.Show();


            glueViewSettingsView = new Views.GlueViewSettings();
            glueViewSettingsView.ViewModel = GlueViewSettingsViewModel;

            glueViewSettingsTab = base.CreateTab(glueViewSettingsView, "Editor Settings");

            AssignControlEvents();
        }

        private async void HandleGlueViewSettingsViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //////////Early Out////////////////////
            if (ignoreViewModelChanges)
            {
                return;
            }

            /////////End Early Out//////////////// 
            var propertyName = e.PropertyName;
            switch(propertyName)
            {
                case nameof(ViewModels.GlueViewSettingsViewModel.PortNumber):
                case nameof(ViewModels.GlueViewSettingsViewModel.EnableGameEditMode):
                    CompilerViewModel.IsGenerateGlueControlManagerInGame1Checked = GlueViewSettingsViewModel.EnableGameEditMode;
                    await HandlePortOrGenerateCheckedChanged(propertyName);
                    break;
                case nameof(ViewModels.GlueViewSettingsViewModel.GridSize):
                case nameof(ViewModels.GlueViewSettingsViewModel.SetBackgroundColor):
                case nameof(ViewModels.GlueViewSettingsViewModel.BackgroundRed):
                case nameof(ViewModels.GlueViewSettingsViewModel.BackgroundGreen):
                case nameof(ViewModels.GlueViewSettingsViewModel.BackgroundBlue):
                case nameof(ViewModels.GlueViewSettingsViewModel.SnapSize):
                case nameof(ViewModels.GlueViewSettingsViewModel.EnableSnapping):
                case nameof(ViewModels.GlueViewSettingsViewModel.ShowScreenBoundsWhenViewingEntities):
                    await SendGlueViewSettingsToGame();
                    break;
            }


            SaveCompilerSettingsModel();

        }

        private async Task SendGlueViewSettingsToGame()
        {
            var dto = new Dtos.GlueViewSettingsDto
            {
                GridSize = GlueViewSettingsViewModel.GridSize,
                ShowScreenBoundsWhenViewingEntities = GlueViewSettingsViewModel.ShowScreenBoundsWhenViewingEntities,
                SetBackgroundColor = GlueViewSettingsViewModel.SetBackgroundColor,
                BackgroundRed = GlueViewSettingsViewModel.BackgroundRed,
                BackgroundGreen = GlueViewSettingsViewModel.BackgroundGreen,
                BackgroundBlue = GlueViewSettingsViewModel.BackgroundBlue,
                EnableSnapping = GlueViewSettingsViewModel.EnableSnapping,
                SnapSize = GlueViewSettingsViewModel.SnapSize
            };

            await CommandSender.Send(dto);
        }

        private async void HandleCompilerViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //////////Early Out////////////////////
            if (ignoreViewModelChanges)
            {
                return;
            }

            /////////End Early Out////////////////
            var propertyName = e.PropertyName;

            switch (propertyName)
            {

                case nameof(ViewModels.CompilerViewModel.CurrentGameSpeed):
                    var speedPercentage = int.Parse(CompilerViewModel.CurrentGameSpeed.Substring(0, CompilerViewModel.CurrentGameSpeed.Length - 1));
                    await CommandSender.Send(new SetSpeedDto
                    {
                        SpeedPercentage = speedPercentage
                    });
                    
                    break;
                case nameof(ViewModels.CompilerViewModel.EffectiveIsRebuildAndRestartEnabled):
                    RefreshManager.Self.IsExplicitlySetRebuildAndRestartEnabled = CompilerViewModel.EffectiveIsRebuildAndRestartEnabled;
                    break;
                case nameof(ViewModels.CompilerViewModel.IsToolbarPlayButtonEnabled):
                    ToolbarController.Self.SetEnabled(CompilerViewModel.IsToolbarPlayButtonEnabled);
                    break;
                case nameof(ViewModels.CompilerViewModel.IsRunning):
                    //CommandSender.CancelConnect();
                    break;
                case nameof(ViewModels.CompilerViewModel.PlayOrEdit):

                    await ReactToPlayOrEditSet();

                    break;
                case nameof(ViewModels.CompilerViewModel.ToolbarEntitiesAndStates):
                    if(CompilerViewModel.HasLoadedGlux)
                    {
                        SaveCompilerSettingsModel();
                    }
                    break;
            }


        }

        private async Task ReactToPlayOrEditSet()
        {
            var inEditMode = CompilerViewModel.PlayOrEdit == PlayOrEdit.Edit;
            var dto = new Dtos.SetEditMode 
            { 
                IsInEditMode = inEditMode ,
                AbsoluteGlueProjectFilePath = GlueState.Self.GlueProjectFileName.FullPath
            };
            var response = await CommandSending.CommandSender.Send<Dtos.GeneralCommandResponse>(dto);

            if (response?.Succeeded != true)
            {
                var message = "Failed to change game/edit mode. ";
                if (response == null)
                {
                    message += "Game sent no response back.";
                }
                else
                {
                    message += response.Message;
                }
                MainControl.PrintOutput(message);
            }
            else if (CommandSender.IsConnected == false)
            {

            }
            else if (inEditMode)
            {
                var currentEntity = GlueState.Self.CurrentEntitySave;
                if (currentEntity != null)
                {
                    await RefreshManager.Self.PushGlueSelectionToGame();
                }
                else
                {
                    var screenName = await CommandSending.CommandSender.GetScreenName();

                    if (!string.IsNullOrEmpty(screenName))
                    {
                        var glueScreenName =
                            string.Join('\\', screenName.Split('.').Skip(1).ToArray());

                        var screen = ObjectFinder.Self.GetScreenSave(glueScreenName);

                        if (screen != null)
                        {

                            if (GlueState.Self.CurrentElement != screen)
                            {
                                GlueState.Self.CurrentElement = screen;

                            }
                            else
                            {
                                // the screens are the same, so push the object selection from Glue to the game:
                                await RefreshManager.Self.PushGlueSelectionToGame();
                            }
                        }
                    }
                }

                await SendGlueViewSettingsToGame();
            }
            else
            {
                // the user is viewing an entity, so force the screen
                if (GlueState.Self.CurrentEntitySave != null)
                {
                    // push the selection to game
                    var startupScreen = ObjectFinder.Self.GetScreenSave(GlueState.Self.CurrentGlueProject.StartUpScreen);
                    await RefreshManager.Self.PushGlueSelectionToGame(forcedElement: startupScreen);
                }
            }
            var setCameraAspectRatioDto = new SetCameraAspectRatioDto();

            var displaySettings = GlueState.Self.CurrentGlueProject?.DisplaySettings;

            if (inEditMode)
            {
                setCameraAspectRatioDto.AspectRatio = null;
            }
            else
            {
                if(displaySettings != null &&
                    displaySettings.AspectRatioHeight > 0 &&
                    displaySettings.FixedAspectRatio == true
                    )
                {
                    setCameraAspectRatioDto.AspectRatio = GlueState.Self.CurrentGlueProject.DisplaySettings.AspectRatioWidth /
                        GlueState.Self.CurrentGlueProject.DisplaySettings.AspectRatioHeight;
                }
            }
                    
            await CommandSender.Send(setCameraAspectRatioDto);
        }

        private SetCameraSetupDto ToDto(DisplaySettings displaySettings)
        {
            var toReturn = new SetCameraSetupDto();
            toReturn.AllowWindowResizing = displaySettings.AllowWindowResizing;

            if(displaySettings.FixedAspectRatio)
            {
                toReturn.AspectRatio = displaySettings.AspectRatioWidth / displaySettings.AspectRatioHeight;
            }

            toReturn.DominantInternalCoordinates = displaySettings.DominantInternalCoordinates;
            toReturn.Is2D = displaySettings.Is2D;
            toReturn.IsFullScreen = displaySettings.RunInFullScreen;
            toReturn.IsGenerateCameraDisplayCodeEnabled = displaySettings.GenerateDisplayCode;
            toReturn.ResizeBehavior = displaySettings.ResizeBehavior;
            toReturn.ResizeBehaviorGum = displaySettings.ResizeBehaviorGum;
            toReturn.ResolutionHeight = displaySettings.ResolutionHeight;
            toReturn.ResolutionWidth = displaySettings.ResolutionWidth;
            toReturn.Scale = displaySettings.Scale;
            toReturn.ScaleGum = displaySettings.ScaleGum;
            toReturn.TextureFilter = (Microsoft.Xna.Framework.Graphics.TextureFilter)displaySettings.TextureFilter;

            return toReturn;



        }

        private async Task HandlePortOrGenerateCheckedChanged(string propertyName)
        {
            MainControl.PrintOutput("Applying changes");
            game1GlueControlGenerator.IsGlueControlManagerGenerationEnabled = GlueViewSettingsViewModel.EnableGameEditMode && IsFrbNewEnough();
            game1GlueControlGenerator.PortNumber = GlueViewSettingsViewModel.PortNumber;
            RefreshManager.Self.PortNumber = GlueViewSettingsViewModel.PortNumber;
            GlueCommands.Self.GenerateCodeCommands.GenerateGame1();
            if (IsFrbNewEnough())
            {
                TaskManager.Self.Add(() => EmbeddedCodeManager.EmbedAll(GlueViewSettingsViewModel.EnableGameEditMode), "Generate Glue Control Code");
                TaskManager.Self.Add(() => GlueCallsCodeGenerator.GenerateAll(), "Generate Glue Control Code New");
            }

            if (GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.NugetPackageInCsproj)
            {
                GlueCommands.Self.ProjectCommands.AddNugetIfNotAdded("Newtonsoft.Json", "12.0.3");
            }

            await RefreshManager.Self.StopAndRestartAsync($"{propertyName} changed");

            MainControl.PrintOutput("Waiting for tasks to finish...");
            await TaskManager.Self.WaitForAllTasksFinished();
            MainControl.PrintOutput("Finishined adding/generating code for GlueControlManager");
        }

        private void SaveCompilerSettingsModel()
        {
            var model = new CompilerSettingsModel();
            GlueViewSettingsViewModel.SetModel(model);

            foreach(var vm in CompilerViewModel.ToolbarEntitiesAndStates)
            {
                var toolbarModel = new ToolbarEntityAndState();
                vm.ApplyTo(toolbarModel);
                model.ToolbarEntitiesAndStates.Add(toolbarModel);
            }

            try
            {
                var text = JsonConvert.SerializeObject(model);
                GlueCommands.Self.TryMultipleTimes(() =>
                {
                    System.IO.Directory.CreateDirectory(JsonSettingsFilePath.GetDirectoryContainingThis().FullPath);
                    System.IO.File.WriteAllText(JsonSettingsFilePath.FullPath, text);
                });
            }
            catch
            {
                // no big deal if it fails
            }
        }

        private void AssignControlEvents()
        {
            MainControl.BuildClicked += async (not, used) =>
            {
                var succeeded = await GameHostController.Self.Compile();
                if(!succeeded)
                {
                    GlueCommands.Self.DialogCommands.FocusTab("Build");
                }
            };


            MainControl.RunClicked += async (not, used) =>
            {
                var succeeded = await GameHostController.Self.Compile();
                if (succeeded)
                {
                    if (succeeded)
                    {
                        CompilerViewModel.IsRunning = false;
                        await runner.Run(preventFocus: false);
                    }
                    else
                    {
                        var runAnywayMessage = "Your project has content errors. To fix them, see the Errors tab. You can still run the game but you may experience crashes. Run anyway?";

                        GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(runAnywayMessage, async () => await runner.Run(preventFocus: false));
                    }
                }
            };



        }


        private static bool GetIfHasErrors()
        {
            var errorPlugin = PluginManager.AllPluginContainers
                                .FirstOrDefault(item => item.Plugin is ErrorPlugin.MainErrorPlugin)?.Plugin as ErrorPlugin.MainErrorPlugin;

            var hasErrors = errorPlugin?.HasErrors == true;
            return hasErrors;
        }

        private void OutputSuccessOrFailure(bool succeeded)
        {
            if (succeeded)
            {
                MainControl.PrintOutput($"{DateTime.Now.ToLongTimeString()} Build succeeded");
            }
            else
            {
                MainControl.PrintOutput($"{DateTime.Now.ToLongTimeString()} Build failed");

            }
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            TryKillGame();
            return true;
        }


        public async void ShowState(string stateName, string categoryName)
        {
            await RefreshManager.Self.PushGlueSelectionToGame(categoryName, stateName);
        }




        private void HandleGrabbedTreeNodeChanged(ITreeNode treeNode, TreeNodeAction action)
        {


        }


        #region DLLImports
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, UInt32 dwNewLong);

        // from https://docs.microsoft.com/en-us/dotnet/desktop/wpf/advanced/walkthrough-hosting-a-win32-control-in-wpf?view=netframeworkdesktop-4.8
        internal const int
          WS_CHILD = 0x40000000,
          WS_VISIBLE = 0x10000000,
          LBS_NOTIFY = 0x00000001,
          HOST_ID = 0x00000002,
          LISTBOX_ID = 0x00000001,
          WS_VSCROLL = 0x00200000,
          WS_BORDER = 0x00800000;

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        #endregion

        #region Fields/Properties


        PluginTab pluginTab;
        GameHostView gameHostView;

        Process gameProcess;

        #endregion

        public async void MoveGameToHost()
        {
            gameProcess = Runner.Self.TryFindGameProcess();
            var handle = gameProcess?.MainWindowHandle;

            if (handle != null)
            {
                await gameHostView.EmbedHwnd(handle.Value);
            }
            else
            {
                if(gameProcess == null)
                {
                    GlueCommands.Self.PrintOutput("Failed to find game handle.");
                }
                else
                {
                    GlueCommands.Self.PrintOutput("Failed to find window handle.");
                }
                
            }
        }


        private void TryKillGame()
        {
            if (gameProcess != null)
            {
                try
                {
                    gameProcess?.Kill();
                }
                catch
                {
                    // no biggie, It hink
                }
            }
        }



    }

}
