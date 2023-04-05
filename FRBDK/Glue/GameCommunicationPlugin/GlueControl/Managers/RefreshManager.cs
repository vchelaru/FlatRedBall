using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Newtonsoft.Json;
using GameCommunicationPlugin.GlueControl.CommandSending;
using GameCommunicationPlugin.GlueControl.Dtos;
using GameCommunicationPlugin.GlueControl.ViewModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GeneralResponse = ToolsUtilities.GeneralResponse;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.CodeGeneration;
using Newtonsoft.Json.Linq;
using GameCommunicationPlugin.Common;
using CompilerLibrary.ViewModels;
using static FlatRedBall.Glue.Plugins.PluginManager;

namespace GameCommunicationPlugin.GlueControl.Managers
{
    #region Classes

    public class ExpiringFilePath
    {
        public DateTimeOffset? Expiration { get; set; }
        public FilePath FilePath { get; set; }
    }

    #endregion

    public class RefreshManager
    {
        public RefreshManager(Func<string, string, Task<string>> eventCallerWithReturn, CommandSender commandSender, Action<string, string> eventCaller)
        {
            _eventCallerWithReturn = eventCallerWithReturn;
            _commandSender = commandSender;
            _eventCaller = eventCaller;
        }

        internal VariableSendingManager VariableSendingManager { get; set; }

        #region Fields/Properties

        Action<string> printOutput;
        Action<string> printError;
        string screenToRestartOn = null;


        bool isExplicitlySetRebuildAndRestartEnabled;
        public bool IsExplicitlySetRebuildAndRestartEnabled
        {
            get => isExplicitlySetRebuildAndRestartEnabled;
            set
            {
                isExplicitlySetRebuildAndRestartEnabled = value;
                RefreshViewModelHotReload();

            }
        }
        bool failedToRebuildAndRestart { get; set; }

        public bool ShouldRestartOnChange
        {
            get
            {
                if (GlueState.Self.CurrentGlueProject != null)
                {
                    return
                        // This causes confusing behavior and can make the game restart over and over, so let's get rid of it:
                        //failedToRebuildAndRestart ||
                        IsExplicitlySetRebuildAndRestartEnabled ||
                        (ViewModel.IsRunning && ViewModel.IsEditChecked) ||
                        GlueViewSettingsViewModel.RestartScreenOnLevelContentChange;
                }
                return false;
            }
        }



        public int PortNumber { get; set; }

        public CompilerViewModel ViewModel
        {
            get; set;
        }
        public GlueViewSettingsViewModel GlueViewSettingsViewModel
        {
            get; set;
        }

        public bool IgnoreNextObjectSelect { get; set; }

        public SynchronizedCollection<ExpiringFilePath> FilePathsToIgnore { get; private set; }
            = new SynchronizedCollection<ExpiringFilePath>();

        #endregion

        #region Initialize

        public void InitializeEvents(Action<string> printOutput, Action<string> printError)
        {
            this.printOutput = printOutput;
            this.printError = printError;
        }

        #endregion

        #region Utilities

        public string GetGameTypeFor(GlueElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
            else if (element.Name == null)
            {
                throw new NullReferenceException(nameof(element.Name));
            }

            return
                GlueState.Self.ProjectNamespace + "." + element.Name.Replace("\\", ".");
        }

        #endregion

        #region File

        private void RemoveExpiredPaths()
        {
            var toRemove = FilePathsToIgnore.Where(item => item.Expiration < DateTime.Now).ToArray();

            foreach (var item in toRemove)
            {
                FilePathsToIgnore.Remove(item);
            }
        }

        public async void HandleFileChanged(FilePath fileName)
        {
            // always do this:
            RemoveExpiredPaths();
            var foundTempIgnoredFilePath =
                FilePathsToIgnore.FirstOrDefault(item => item.FilePath == fileName);
            if (foundTempIgnoredFilePath != null)
            {
                if (DateTime.Now > foundTempIgnoredFilePath.Expiration)
                {
                    FilePathsToIgnore.Remove(foundTempIgnoredFilePath);
                }
                else
                {
                    printOutput($"Ignoring file change {fileName}");
                }
            }

            var shouldReactToFileChange =
                foundTempIgnoredFilePath == null &&
                ShouldRestartOnChange &&
                GetIfShouldReactToFileChange(fileName);

            if (shouldReactToFileChange)
            {
                var rfses = GlueCommands.Self.FileCommands.GetReferencedFiles(fileName.FullPath);
                var firstRfs = rfses.FirstOrDefault();
                var isGlobalContent = rfses.Any(item => item.GetContainer() == null);

                bool canSendCommands = ViewModel.IsGenerateGlueControlManagerInGame1Checked;

                var handled = false;

                if (canSendCommands)
                {
                    string strippedName = null;
                    if (firstRfs != null)
                    {
                        strippedName = FileManager.RemovePath(FileManager.RemoveExtension(firstRfs.Name));
                    }
                    else
                    {
                        strippedName = fileName.NoPath;
                    }

                    var containerNames = rfses.Select(item => item.GetContainer()?.Name).Where(item => item != null).ToHashSet();

                    var shouldCopy = false;
                    shouldCopy = containerNames.Any() || GlueCommands.Self.FileCommands.IsContent(fileName);

                    if (shouldCopy)
                    {
                        // Right now we'll assume the screen owns this file, although it is possible that it's 
                        // global but not part of global content. That's a special case we'll have to handle later
                        printOutput($"Waiting for file to be copied: {strippedName}");
                        await Task.Delay(600);
                        try
                        {
                            if (ViewModel.IsRunning)
                            {
                                var extension = fileName.Extension;
                                var shouldReloadFile = extension == "csv";

                                var shouldReloadScreen = false;

                                if (shouldReloadFile)
                                {
                                    printOutput($"Sending force reload for file: {strippedName}");

                                    var dto = new Dtos.ForceReloadFileDto();
                                    dto.ElementsContainingFile = containerNames.ToList();
                                    dto.LoadInGlobalContent = GlueState.Self.CurrentGlueProject.GetAllReferencedFiles().Contains(firstRfs);
                                    dto.IsLocalizationDatabase = firstRfs.IsDatabaseForLocalizing;
                                    dto.FileRelativeToProject =
                                        ReferencedFileSaveCodeGenerator.GetFileToLoadForRfs(firstRfs);
                                    dto.StrippedFileName = fileName.NoPathNoExtension;
                                    await _commandSender.Send(dto);

                                    // Typically localization is applied in custom code, so we can't
                                    // apply these changes without reloading the screen
                                    shouldReloadScreen = dto.IsLocalizationDatabase;
                                }
                                else
                                {
                                    printOutput($"Telling game to restart screen");
                                    shouldReloadScreen = true;
                                }

                                if (shouldReloadScreen)
                                {
                                    var dto = new RestartScreenDto();
                                    dto.ReloadGlobalContent = isGlobalContent;
                                    await _commandSender.Send(dto);
                                }
                            }

                            handled = true;
                        }
                        catch (Exception e)
                        {
                            printError($"Error trying to send command:{e.ToString()}");
                        }
                    }

                    var isContentPipeline = firstRfs?.UseContentPipeline == true || firstRfs?.GetAssetTypeInfo()?.MustBeAddedToContentPipeline == true;
                    // the game should reload only after copying the file
                    if (isGlobalContent &&
                        // if it's using the content pipeline, it can't be reloaded individually. FRB Will throw an exception:
                        !isContentPipeline
                        // Why do we check if the CustomReloadFunc != null?
                        // If a file (like PNG) changes and it's in global content,
                        // then we want to reload global content
                        //&& firstRfs.GetAssetTypeInfo().CustomReloadFunc != null
                        )
                    {
                        printOutput($"Waiting for Glue to copy reload global file {strippedName}");

                        // just give the file time to copy:
                        await Task.Delay(500);

                        // it's part of global content and can be reloaded, so let's just tell
                        // it to reload:
                        await _commandSender.Send(new ReloadGlobalContentDto
                        {
                            StrippedGlobalContentFileName = strippedName
                        });

                        printOutput($"Reloading global file {strippedName}");

                        handled = true;
                    }
                }
                if (!handled && GlueCommands.Self.FileCommands.IsContent(fileName))
                {
                    CreateStopAndRestartTask($"File {fileName} changed");
                }
            }
        }

        internal bool HandleTreeNodeDoubleClicked(ITreeNode arg)
        {
            if (arg.Tag is NamedObjectSave asNos)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                PushGlueSelectionToGame(bringIntoFocus: true);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                return true;
            }
            return false;
        }

        private bool GetIfShouldReactToFileChange(FilePath filePath)
        {
            var noPath = filePath.NoPath;
            if (filePath.FullPath.Contains(".Generated.") && filePath.FullPath.EndsWith(".cs"))
            {
                return false;
            }
            if (filePath.FullPath.EndsWith(".Generated.xml"))
            {
                return false;
            }
            if (noPath == "CompilerSettings.json")
            {
                return false;
            }
            if(string.IsNullOrEmpty(filePath.Extension))
            {
                return false;
            }


            return true;
        }

        internal void HandleNewFile(ReferencedFileSave newFile)
        {
            GlueCommands.Self.ProjectCommands.CopyToBuildFolder(newFile);
        }

        #endregion

        #region Entity Created

        internal async void HandleNewEntityCreated(EntitySave newEntity)
        {
            if (ViewModel.IsRunning && ViewModel.IsEditChecked)
            {
                var filePath = GlueCommands.Self.FileCommands.GetCustomCodeFilePath(newEntity);


                IgnoreNextChange(filePath);

                var dto = new CreateNewEntityDto();
                dto.EntitySave = newEntity;

                await _commandSender.Send(dto);

                // selection happens before the entity is created, so let's force push the selection to the game
                await PushGlueSelectionToGame();
            }
        }

        public void IgnoreNextChange(FilePath filePath)
        {
            const int responseDelay = 5;
            var expiringFilePath =
                new ExpiringFilePath
                {
                    Expiration = DateTime.Now + TimeSpan.FromSeconds(responseDelay),
                    FilePath = filePath
                };

            printOutput($"Ignoring {expiringFilePath.FilePath} for {responseDelay} seconds");
            FilePathsToIgnore.Add(expiringFilePath);
        }

        #endregion

        #region Screen Created

        internal void HandleNewScreenCreated()
        {
            if (ShouldRestartOnChange)
            {
                // Don't await this because this could soft lock the app.

                CreateStopAndRestartTask($"New screen created");

            }
        }

        #endregion

        #region State Created

        internal async void HandleStateCreated(StateSave state, StateSaveCategory category)
        {
            if (category != null)
            {
                var container = ObjectFinder.Self.GetElementContaining(category);

                var dto = new CreateNewStateDto();
                var stateCopy = state.Clone();

                // cateorized states always set a value. That value will either
                // be explicitly set on the state, or it will be inherited from the
                // default value on the object.
                foreach (var variable in container.CustomVariables)
                {
                    if (category.ExcludedVariables.Contains(variable.Name))
                    {
                        continue;
                    }

                    stateCopy.InstructionSaves.Add(new FlatRedBall.Content.Instructions.InstructionSave
                    {
                        Member = variable.Name,
                        Type = variable.Type,
                        Value = variable.DefaultValue
                    });
                }
                dto.StateSave = stateCopy;
                dto.CategoryName = category?.Name;
                dto.ElementNameGame = GetGameTypeFor(container);

                await _commandSender.Send(dto);
            }
        }

        #endregion

        #region NamedObject Created


        internal async Task HandleNewObjectList(List<NamedObjectSave> newObjectList)
        {
            if (ViewModel.IsRunning && ViewModel.IsEditChecked)
            {
                var list = new Dtos.AddObjectDtoList();

                foreach (var newObject in newObjectList)
                {
                    var individualDto = CreateAddObjectDtoFor(newObject);

                    list.Data.Add(individualDto);
                }

                var response = await _commandSender.Send<AddObjectDtoListResponse>(list);

                if (response.Succeeded == false ||
                    response.Data.Data == null ||
                    response.Data.Data.Any(item => item.WasObjectCreated == false))
                {
                    CreateStopAndRestartTask("Restarting because the add object group failed");
                }
                // else do we want to position based on camera? This is likely a copy/paste so...maybe not?
            }
        }

        internal async void HandleNewObjectCreated(NamedObjectSave newNamedObject)
        {
            if (ViewModel.IsRunning && ViewModel.IsEditChecked)
            {
                AddObjectDto addObjectDto = CreateAddObjectDtoFor(newNamedObject);

                var sendResponse = await _commandSender.Send(addObjectDto);
                string addResponseAsString = null;
                if (sendResponse.Succeeded)
                {
                    addResponseAsString = sendResponse.Data;
                }

                AddObjectDtoResponse addResponse = null;
                if (!string.IsNullOrEmpty(addResponseAsString))
                {
                    try
                    {
                        addResponse = JsonConvert.DeserializeObject<AddObjectDtoResponse>(addResponseAsString);
                    }
                    catch (Exception e)
                    {
                        printOutput($"Error parsing string:\n\n{addResponseAsString}");
                    }
                }

                if (addResponse?.WasObjectCreated == true)
                {
                    var isPositionedObject = newNamedObject.SourceType == SourceType.Entity ||
                        (newNamedObject.GetAssetTypeInfo()?.IsPositionedObject == true);
                    if (isPositionedObject)
                    {
                        await AdjustNewObjectToCameraPosition(newNamedObject);
                    }
                }
                else
                {
                    CreateStopAndRestartTask($"Restarting because of added object {newNamedObject}");
                }
            }
        }

        private AddObjectDto CreateAddObjectDtoFor(NamedObjectSave newNamedObject)
        {
            var tempSerialized = JsonConvert.SerializeObject(newNamedObject);
            var nosCopy = JsonConvert.DeserializeObject<NamedObjectSave>(tempSerialized);

            foreach (var instruction in nosCopy.InstructionSaves)
            {
                // qualify here!
                instruction.Type = VariableSendingManager.GetQualifiedStateTypeName(instruction.Member, null, nosCopy, out bool isState, out StateSaveCategory category);
            }

            var addObjectDto = new AddObjectDto();
            addObjectDto.NamedObjectSave = nosCopy;
            var containerElement = ObjectFinder.Self.GetElementContaining(newNamedObject);
            NamedObjectSave nosList = null;
            if (containerElement != null)
            {
                addObjectDto.ElementNameGame = GetGameTypeFor(containerElement);
                nosList = containerElement.NamedObjects.FirstOrDefault(item => item.ContainedObjects.Contains(newNamedObject));
            }

            addObjectDto.NamedObjectsToUpdate.Add(new NamedObjectWithElementName
            {
                NamedObjectSave = nosCopy,
                GlueElementName = containerElement?.Name,
                ContainerName = nosList?.InstanceName
            });
            return addObjectDto;
        }

        public Vector2? ForcedNextObjectPosition { get; set; }
        private async Task AdjustNewObjectToCameraPosition(NamedObjectSave newNamedObject)
        {
            Vector2 newPosition = Vector2.Zero;

            if (ForcedNextObjectPosition != null)
            {
                newPosition = ForcedNextObjectPosition.Value;
                ForcedNextObjectPosition = null;
            }
            else
            {
                if (GlueState.Self.CurrentScreenSave != null)
                {
                    newPosition = await GetNewNosPositionFromCamera(newNamedObject);
                }
            }


            bool didSetValue = false;
            var gluxCommands = GlueCommands.Self.GluxCommands;

            if (newPosition.X != 0)
            {
                gluxCommands.SetVariableOn(newNamedObject, "X", newPosition.X, false, updateUi:false);
                didSetValue = true;
            }
            if (newPosition.Y != 0)
            {
                gluxCommands.SetVariableOn(newNamedObject, "Y", newPosition.Y, false, updateUi: false);

                didSetValue = true;
            }



            if (didSetValue)
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
                GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();
                GlueCommands.Self.GluxCommands.SaveGlux();
            }
        }

        private async Task<Vector2> GetNewNosPositionFromCamera(NamedObjectSave newNamedObject)
        {
            // If it's in a screen, then we position the object on the camera:

            var cameraPosition = Microsoft.Xna.Framework.Vector3.Zero;

            cameraPosition = await _commandSender.GetCameraPosition();

            var gluxCommands = GlueCommands.Self.GluxCommands;


            Vector2 newPosition = new Vector2(cameraPosition.X, cameraPosition.Y);

            var list = GlueState.Self.CurrentElement.NamedObjects.FirstOrDefault(item =>
                item.ContainedObjects.Contains(newNamedObject));

            var shouldIncreasePosition = false;
            do
            {
                shouldIncreasePosition = false;

                var listToLoopThrough = list?.ContainedObjects ?? GlueState.Self.CurrentElement.NamedObjects;

                const int incrementForNewObject = 16;
                const int minimumDistanceForObjects = 3;
                foreach (var item in listToLoopThrough)
                {
                    if (item != newNamedObject)
                    {
                        Vector2 itemPosition = new Vector2(
                            (item.GetCustomVariable("X")?.Value as float?) ?? 0,
                            (item.GetCustomVariable("Y")?.Value as float?) ?? 0);

                        var distance = (itemPosition - newPosition).Length();


                        if (distance < minimumDistanceForObjects)
                        {
                            shouldIncreasePosition = true;
                            break;
                        }

                    }
                }
                if (shouldIncreasePosition)
                {
                    newPosition.X += incrementForNewObject;
                }

            } while (shouldIncreasePosition);

            return newPosition;
        }

        #endregion

        #region Variable Created

        internal async void HandleVariableAdded(CustomVariable newVariable)
        {
            await HandleVariableAddedOrRenamedInternal(newVariable);
        }

        private async Task HandleVariableAddedOrRenamedInternal(CustomVariable newVariable)
        {
            // Vic says - When a new variable is added, we don't need to restart. However,
            // later that variable might get assigned on instances of an object, and if it is
            // then that would probably fail because it would attempt to assign through reflection.
            // Therefore, we could either restart here so that all future assignments work, or we could
            // restart on the variable set. While it might result in fewer restarts to restart when the
            // variable is assigned (since the variable may not actually get assigned in Glue), it could
            // also lead to confusion. Therefore, we'll just restart here:
            // Update August 21, 2021
            // Let's look at the possible variables that are added:
            // * New variables - which by default have no functionality until code is written for them
            // * Exposed variables - these do have functionality but they ultimately are just setting other variables
            // If it's a new variable, we are going to restart. Otherwise if it's expoed, send that to the game to use 
            // for assigning real values
            var isTunneled = !string.IsNullOrWhiteSpace(newVariable.SourceObject) &&
                !string.IsNullOrWhiteSpace(newVariable.SourceObjectProperty);

            if (isTunneled)
            {
                // send this down to the game
                var dto = new AddVariableDto();
                dto.CustomVariable = newVariable;

                var variableOwner = ObjectFinder.Self.GetElementContaining(newVariable);

                dto.ElementGameType = GetGameTypeFor(variableOwner ?? GlueState.Self.CurrentElement);

                await _commandSender.Send(dto);
            }
            else
            {
                // it's a brand new variable, so let's restart it...
                CreateStopAndRestartTask($"Restarting because of added variable {newVariable}");
            }
        }

        #endregion

        #region Variable Renamed

        internal async void HandleVariableRenamed(CustomVariable renamedVariable)
        {
            // When a variable is renamed, we treat it as if it's a new variable. 
            // I don't think we need to clean up old variables...or at least I can't
            // think of why it might be important to do so (yet), but if this becomes
            // an issue in the future we will have to send a "remove variable" command,
            // or modify the AddVariableDto DTO
            await HandleVariableAddedOrRenamedInternal(renamedVariable);
        }

        #endregion

        #region Selected Object

        internal async void HandleItemSelected(List<ITreeNode> selectedTreeNodes)
        {
            if (IgnoreNextObjectSelect)
            {
                IgnoreNextObjectSelect = false;
            }
            else if (ViewModel.IsEditChecked)
            {
                await PushGlueSelectionToGame();
            }

        }

        static bool IsAbstract(IElement element) => element.AllNamedObjects.Any(item => item.SetByDerived);

        // When the user selects a state, then
        // selects an object with in the same entity
        // as that state, we want to "undo" the state
        // so that any edits they make to the object doesn't
        // pull in state values. The easiest way to do this is
        // to force a reload of the screen. To do this, we need
        // to know if the user was viewing a state before, and is 
        // no longer viewing a state now. The LastDtoPushedToGame is
        // needed to determine this.
        SelectObjectDto LastDtoPushedToGame;
        private Func<string, string, Task<string>> _eventCallerWithReturn;
        private CommandSender _commandSender;
        private Action<string, string> _eventCaller;

        public async Task PushGlueSelectionToGame(string forcedCategoryName = null, string forcedStateName = null, GlueElement forcedElement = null, bool bringIntoFocus = false)
        {
            var element = forcedElement ?? GlueState.Self.CurrentElement;

            var dto = new SelectObjectDto();

            List<NamedObjectSave> namedObjects = new List<NamedObjectSave>();
            if (forcedElement == null)
            {
                namedObjects = GlueState.Self.CurrentNamedObjectSaves.ToList();
            }

            // Determine these values before resetting the LastDtoPushedToGame...
            var needsScreenReload = LastDtoPushedToGame?.GlueElement == element &&
                !string.IsNullOrEmpty(LastDtoPushedToGame?.StateName) &&
                string.IsNullOrEmpty(forcedStateName ?? GlueState.Self.CurrentStateSave?.Name);

            // ... now reset it
            LastDtoPushedToGame = null;

            if (needsScreenReload)
            {
                await _commandSender.Send(new Dtos.RestartScreenDto());
            }
            else if (element != null)
            {
                dto.ScreenSave = element as ScreenSave;
                dto.EntitySave = element as EntitySave;

                bool isAbstract = IsAbstract(element);

                if (isAbstract)
                {
                    var derived = ObjectFinder.Self.GetAllDerivedElementsRecursive(element)
                        .Where(item => !IsAbstract(item))
                        .OrderBy(item => item.Name)
                        .FirstOrDefault();

                    dto.BackupElementNameGlue = derived?.Name;
                }

                var canSend = !isAbstract || !string.IsNullOrEmpty(dto.BackupElementNameGlue);

                // If its abstract and there's no derived, don't try to select it
                if (canSend)
                {
                    dto.BringIntoFocus = bringIntoFocus;
                    dto.NamedObjects.Clear();
                    dto.NamedObjects.AddRange(namedObjects);
                    dto.ElementNameGlue = element.Name;
                    dto.StateName = forcedStateName ??
                        GlueState.Self.CurrentStateSave?.Name;

                    dto.StateCategoryName = forcedCategoryName ??
                        GlueState.Self.CurrentStateSaveCategory?.Name;

                    LastDtoPushedToGame = dto;


                    await _commandSender.Send(dto);
                }
            }
        }

        #endregion

        #region Variable Changed

        internal async void ReactToNamedObjectChangedValueList(List<VariableChangeArguments> variableList, AssignOrRecordOnly assignOrRecordOnly)
        {
            if (ViewModel.IsRunning && ViewModel.IsEditChecked)
            {
                await VariableSendingManager.HandleNamedObjectVariableListChanged(variableList, assignOrRecordOnly);
            }
        }

        internal async void HandleNamedObjectVariableOrPropertyChanged(string variableName, object oldValue, NamedObjectSave nos, AssignOrRecordOnly assignOrRecordOnly)
        {
            if (ViewModel.IsRunning && ViewModel.IsEditChecked)
            {
                await VariableSendingManager.HandleNamedObjectVariableChanged(variableName, oldValue, nos, assignOrRecordOnly);
            }
        }

        internal void HandleVariableChanged(IElement variableElement, CustomVariable variable)
        {
            if (ViewModel.IsRunning && ViewModel.IsEditChecked)
            {
                VariableSendingManager.HandleVariableChanged(variableElement as GlueElement, variable);
            }
        }


        #endregion

        #region State Variable Changed/Excluded

        internal async void HandleStateVariableChanged(StateSave state, StateSaveCategory category, string variableName)
        {
            var container = ObjectFinder.Self.GetElementContaining(category);

            ChangeStateVariableDto dto = null;
            if (container != null)
            {
                dto = new ChangeStateVariableDto();
                // Clone this...
                dto.StateSave = state.Clone();
                // ...because if the variable was deleted, we need to force the value to have the default:
                if (dto.StateSave.InstructionSaves.Any(item => item.Member == variableName) == false &&
                    !category.ExcludedVariables.Contains(variableName))
                {
                    var variable = container.GetCustomVariable(variableName);
                    // add it!
                    dto.StateSave.InstructionSaves.Add(new FlatRedBall.Content.Instructions.InstructionSave
                    {
                        Member = variable.Name,
                        Type = variable.Type,
                        Value = variable.DefaultValue
                    });
                }

                dto.CategoryName = category?.Name;
                dto.ElementNameGame = GetGameTypeFor(container);
                dto.VariableName = variableName;

            }
            if (dto != null)
            {
                await _commandSender.Send(dto);

                // This forces the game to refresh the view according to the current state.
                if (ViewModel.IsEditChecked &&
                    (state == GlueState.Self.CurrentStateSave || category == GlueState.Self.CurrentStateSaveCategory))
                {
                    await PushGlueSelectionToGame(
                        forcedCategoryName: category?.Name,
                        forcedStateName: state?.Name,
                        forcedElement: container);
                }
            }

        }

        internal void HandleStateCategoryExcludedVariablesChanged(StateSaveCategory category, string variableName, StateCategoryVariableAction excludedOrIncluded)
        {
            if (excludedOrIncluded == StateCategoryVariableAction.Included)
            {
                // If a new state variable is included, it won't be functional. This could be confusing, so let's restart
                CreateStopAndRestartTask($"New variable {variableName} added to category {category}");
            }
        }

        #endregion

        #region Object Container (List, Layer, ShapeCollection) changed

        internal async Task HandleObjectListContainerChanged(List<ObjectContainerChange> changeList)
        {
            if (ViewModel.IsRunning && ViewModel.IsEditChecked)
            {
                GeneralResponse generalResponse = GeneralResponse.UnsuccessfulWith("Unknown error");
                string responseAsString = null;
                MoveObjectToContainerListDto changesListDto = new MoveObjectToContainerListDto();

                foreach (var change in changeList)
                {
                    var objectMoving = change.ObjectMoved;
                    var newContainer = change.NewContainer;
                    var element = ObjectFinder.Self.GetElementContaining(objectMoving);

                    if (element == null)
                    {
                        generalResponse = GeneralResponse.UnsuccessfulWith("Could not find Glue Element containing {objectMoving}");
                    }
                    else
                    {
                        var dto = new MoveObjectToContainerDto
                        {
                            ElementName = element.Name,
                            ObjectName = objectMoving.InstanceName,
                            ContainerName = newContainer?.InstanceName

                        };

                        changesListDto.Changes.Add(dto);
                    }
                }



                var sendResponse = await _commandSender.Send(changesListDto);
                responseAsString = sendResponse.Succeeded ? sendResponse.Data : string.Empty;

                if (string.IsNullOrEmpty(responseAsString))
                {
                    generalResponse = GeneralResponse.UnsuccessfulWith($"Sent the command to move {changeList.Count} objects but never got a response from the game");
                }
                else
                {
                    try
                    {
                        var response = JsonConvert.DeserializeObject<MoveObjectToContainerDtoResponse>(responseAsString);
                        if (response.NumberFailedToMoved == 0)
                        {
                            // This could happen if there were no failures, or an empty list was sent to move. Do we care? For now, no
                            generalResponse = GeneralResponse.SuccessfulResponse;
                        }
                        else
                        {
                            generalResponse = GeneralResponse.UnsuccessfulWith($"Failed to move {response.NumberFailedToMoved} game objects to a list. " +
                                $"Successfully moved {response.NumberSuccessfullyMoved} game objects.");
                        }
                    }
                    catch (Exception e)
                    {
                        generalResponse = GeneralResponse.UnsuccessfulWith($"Failed to deserialie response:\n\n{responseAsString}\n\n{e}");
                    }
                }

                if (!generalResponse.Succeeded)
                {
                    CreateStopAndRestartTask($"Restarting due to changed container\n{generalResponse.Message}");
                }
            }
        }
        #endregion

        #region Object Removed

        internal async Task HandleObjectListRemoved(List<GlueElement> owners, List<NamedObjectSave> namedObjects)
        {
            var owner = owners.FirstOrDefault();
            // Assume all owners are the same, so just use the first. If we ever allow selection of multiple objects
            // in different screens, then we would want to include the lists.
            if (ViewModel.IsRunning && ViewModel.IsEditChecked && owner != null)
            {
                var dto = new Dtos.RemoveObjectDto();

                dto.ScreenSave = owner as ScreenSave;
                dto.EntitySave = owner as EntitySave;

                dto.ElementNameGlue = //ToGameType((GlueElement)owner);
                    owner.Name;

                var namedObjectNames = namedObjects.Select(item => item.InstanceName).ToList();

                dto.ObjectNames.AddRange(namedObjectNames);
                var timeBeforeSend = DateTime.Now;
                var sendResponse = await _commandSender.Send(dto);
                var responseAsstring = sendResponse.Succeeded ? sendResponse.Data : null;
                var timeAfterSend = DateTime.Now;
                printOutput($"Delete send took {timeAfterSend - timeBeforeSend}\n \n ");
                RemoveObjectDtoResponse response = null;
                try
                {
                    response = JsonConvert.DeserializeObject<RemoveObjectDtoResponse>(responseAsstring);
                }
                catch (Exception e)
                {
                    printOutput($"Error parsing response from game:\n\n{responseAsstring}");
                }
                if (response == null || (response.DidScreenMatch && response.WasObjectRemoved == false))
                {
                    CreateStopAndRestartTask(
                        $"Restarting because {namedObjects.Count} items were deleted from Glue but not from game");
                }
            }
        }

        internal Task HandleObjectRemoved(IElement owner, NamedObjectSave nos)
        {
            return HandleObjectListRemoved(
                new List<GlueElement> { owner as GlueElement }, new List<NamedObjectSave> { nos });
        }
        #endregion

        #region Stop/Restart


        bool CanRestart => ViewModel.IsGenerateGlueControlManagerInGame1Checked &&
            (
                ViewModel.DidRunnerStartProcess ||
                (ViewModel.IsRunning == false && failedToRebuildAndRestart) ||
                (ViewModel.IsRunning && ViewModel.IsEditChecked)
            );

        public void CreateStopAndRestartTask(string reason)
        {
            if (CanRestart)
            {
                TaskManager.Self.Add(async () =>
                {
                    if (!string.IsNullOrEmpty(reason))
                    {
                        printOutput($"Restarting because: {reason}. Waiting for tasks to finish...");
                    }
                    var wasInEditMode = ViewModel.IsEditChecked;
                    await StopAndRestartImmediately(PortNumber);
                    if (wasInEditMode)
                    {
                        ViewModel.IsEditChecked = true;
                    }

                }, $"{nameof(CreateStopAndRestartTask)} : {reason}" , TaskExecutionPreference.AddOrMoveToEnd);
            }
        }


        private async Task StopAndRestartImmediately(int portNumber)
        {
            bool DoesTaskManagerHaveAnotherRestartTask()
            {
                return TaskManager.Self.HasRestartTask;
            }

            if (CanRestart)
            {

                if (ViewModel.IsRunning)
                {
                    try
                    {
                        screenToRestartOn = await _commandSender.GetScreenName();
                    }
                    catch (AggregateException)
                    {
                        printOutput("Could not get the game's screen, restarting game from startup screen");

                    }
                    catch (SocketException)
                    {
                        // do nothing, may not have been able to communicate, just output
                        printOutput("Could not get the game's screen, restarting game from startup screen");
                    }

                    var response = await _eventCallerWithReturn("Runner_Kill", "");
                }

                bool compileSucceeded = false;
                if (!DoesTaskManagerHaveAnotherRestartTask())
                {
                    var compileResult = JObject.Parse(await _eventCallerWithReturn("Compiler_DoCompile", JsonConvert.SerializeObject(new
                    {
                        Configuration = "Debug",
                        PrintMsBuildCommand = false
                    })));

                    compileSucceeded = compileResult.Value<bool>("Succeeded");
                }

                if (compileSucceeded)
                {
                    if (!DoesTaskManagerHaveAnotherRestartTask())
                    {
                        // If we aren't generating Glue, then the game will not be embedded, so prevent focus
                        var preventFocus = ViewModel.IsGenerateGlueControlManagerInGame1Checked == false;
                        var response = JObject.Parse(await _eventCallerWithReturn("Runner_DoRun", JsonConvert.SerializeObject(new
                        {
                            PreventFocus = preventFocus,
                            RunArguments = screenToRestartOn
                        })));
                        if (response.Value<bool>("Succeeded") == false)
                        {
                            printError(response.Value<string>("Error"));
                        }
                        failedToRebuildAndRestart = response.Value<bool>("Succeeded") == false;
                    }
                }
                else
                {
                    failedToRebuildAndRestart = true;
                }
                RefreshViewModelHotReload();
            }

        }

        #endregion

        private void RefreshViewModelHotReload()
        {
            ViewModel.IsHotReloadAvailable = ShouldRestartOnChange;
        }
    }
}
