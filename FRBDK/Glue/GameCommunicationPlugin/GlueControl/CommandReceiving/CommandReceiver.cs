using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Navigation;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Math;
using Glue;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using GameCommunicationPlugin.GlueControl.CommandSending;
using GameCommunicationPlugin.GlueControl.Dtos;
using GameCommunicationPlugin.GlueControl.Managers;
using GameCommunicationPlugin.GlueControl.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TMXGlueLib;
using ToolsUtilities;
using Xceed.Wpf.Toolkit.PropertyGrid;
using CompilerLibrary.ViewModels;

namespace OfficialPluginsCore.Compiler.CommandReceiving
{
    public class CommandReceiver
    {
        int _gamePortNumber;
        private RefreshManager _refreshManager;
        private VariableSendingManager _variableSendingManager;
        System.Reflection.MethodInfo[] AllMethods;
        public Action<string> PrintOutput { get; set; }

        public CompilerViewModel CompilerViewModel { get; set; }

        public CommandReceiver(RefreshManager refreshManager, VariableSendingManager variableSendingManager)
        {
            _refreshManager = refreshManager;
            _variableSendingManager = variableSendingManager;
            AllMethods = typeof(CommandReceiver).GetMethods(
                BindingFlags.NonPublic|BindingFlags.Instance)
                .Where(item => item.Name == nameof(HandleDto))
                .ToArray();
        }

        #region General Functions

        public async Task<object> HandleCommandsFromGame(string message, int gamePortNumber)
        {
            string dtoTypeName = null;
            string dtoSerialized = null;
            if (message.Contains(":"))
            {
                dtoTypeName = message.Substring(0, message.IndexOf(":"));
                dtoSerialized = message.Substring(message.IndexOf(":") + 1);
            }
            else
            {
                throw new Exception($"The command {message} does not contain a : (colon) separator");
            }


            _gamePortNumber = gamePortNumber;

            var matchingMethod =
                AllMethods
                .FirstOrDefault(item =>
                {
                    var parameters = item.GetParameters();
                    return parameters.Length == 1 && parameters[0].ParameterType.Name == dtoTypeName;
                });

            object dto = null;
            if (matchingMethod != null)
            {
                var dtoType = matchingMethod.GetParameters()[0].ParameterType;
                dto = JsonConvert.DeserializeObject(dtoSerialized, dtoType);

            }

            string outputText = dto is FacadeCommandBase facadeCommandBase
                ? $"Processing command of type {dtoTypeName}.{facadeCommandBase.Method ?? facadeCommandBase.GetPropertyName ?? facadeCommandBase.SetPropertyName}"
                : $"Processing command of type {dtoTypeName}";

            object response = null;

            await TaskManager.Self.AddAsync(async () =>
            {
                if(dto != null)
                {
                    response = await ReceiveDto(dto);
                }
                else
                {
                    switch(dtoTypeName)
                    {
                        case nameof(SetVariableDto):
                            await HandleSetVariable(JsonConvert.DeserializeObject<SetVariableDto>(dtoSerialized));
                            break;
                        case nameof(SetVariableDtoList):
                            await HandleSetVariableDtoList(JsonConvert.DeserializeObject<SetVariableDtoList>(dtoSerialized));
                            break;
                        //case nameof(SelectObjectDto):
                        //    HandleSelectObject(JsonConvert.DeserializeObject<SelectObjectDto>(dtoSerialized));
                        //    break;
                        case nameof(ModifyCollisionDto):
                            HandleDto(JsonConvert.DeserializeObject<ModifyCollisionDto>(dtoSerialized));
                            break;
                        default:
                            break;
                    }
                }
            },
            outputText);


            return response;
        }

        private async Task<object> ReceiveDto(object dto)
        {
            var type = dto.GetType();

            if (CompilerViewModel.IsPrintGameToEditorCheckboxChecked)
            {

                if (CompilerViewModel.IsShowParametersChecked && CompilerViewModel.CommandParameterCheckboxVisibility == System.Windows.Visibility.Visible)
                {
                    PrintOutput(JsonConvert.SerializeObject(dto));
                    PrintOutput("------------------------------------------");
                }
                else
                {
                    PrintOutput(DateTime.Now + " " + dto.ToString());
                }
            }




            var method = AllMethods
                .FirstOrDefault(item =>
                {
                    var parameters = item.GetParameters();
                    return parameters.Length == 1 && parameters[0].ParameterType == type;
                });


            object toReturn = null;

            if (method != null)
            {
                toReturn = method.Invoke(this, new object[] { dto });

                if(toReturn is Task asTask)
                {
                    await asTask;

                    Type taskType = asTask.GetType();
                    bool isGenericTask = taskType.IsGenericType;

                    if(isGenericTask)
                    {
                        var resultProperty = taskType.GetProperty("Result");
                        toReturn = resultProperty.GetValue(asTask);
                    }

                }

            }

            PrintOutput($"  {DateTime.Now} returning {toReturn ?? "<null>"}");

            return toReturn;
        }

        #endregion

        #region Remove Object

        //private async void HandleRemoveObject(RemoveObjectDto removeObjectDto)
        private async void HandleDto(RemoveObjectDto removeObjectDto)
        {
            GlueElement elementToRemoveFrom = null;

            if(removeObjectDto.ElementNamesGlue?.Count > 0)
            {
                elementToRemoveFrom = ObjectFinder.Self.GetElement(removeObjectDto.ElementNamesGlue[0]);
            }
            else
            {
                elementToRemoveFrom = GlueState.Self.CurrentElement;
            }

            if(elementToRemoveFrom != null)
            {
                await TaskManager.Self.AddAsync(() =>
                {
                    var objectsToRemove = removeObjectDto.ObjectNames
                        .Select(objectName => elementToRemoveFrom.GetNamedObjectRecursively(objectName))
                        .Where(item => item != null)
                        .ToList();

                    GlueCommands.Self.GluxCommands.RemoveNamedObjectListAsync(objectsToRemove);

                }, "Handling removing object from screen");
            }
        }

        #endregion

        #region Set Variable

        private async Task HandleSetVariable(SetVariableDto setVariableDto, bool regenerateAndSave = true, bool sendBackToGame = true)
        {

            await TaskManager.Self.AddAsync(() =>
            {
                var type = string.Join('\\', setVariableDto.InstanceOwner.Split('.').Skip(1));

                var element = ObjectFinder.Self.GetElement(type);

                NamedObjectSave nos = null;
                if(element != null)
                {
                    nos = element.GetNamedObjectRecursively(setVariableDto.ObjectName);
                }

                if (nos != null)
                {
                    object value = setVariableDto.VariableValue;
                    var typeName = setVariableDto.Type;

                    if(string.IsNullOrEmpty(typeName))
                    {
                        throw new InvalidOperationException($"Variable {setVariableDto.VariableName} came from glue with a value of {typeName} but didn't have a type");
                    }

                    value = ConvertVariable(value, ref typeName, setVariableDto.VariableName, nos, element);

                    // Calling nos.SetVariable rather than going through the GlueCommands prevents the PluginManager from being notified of the change.
                    // We want to manually push the change back using a batch command since it will be much faster when receiving a SetVariableDtoList
                    nos.SetVariable(setVariableDto.VariableName, value);

                    if(sendBackToGame)
                    {
                        // Send this back to the game so the game. When the game receives this, it will store it in
                        // a list and will re-run the commands as necessary (such as whenever a screen is reloaded).
                        GlueCommands.Self.DoOnUiThread(() =>
                            _refreshManager.HandleNamedObjectVariableOrPropertyChanged(setVariableDto.VariableName, null, nos, 
                            // record only - this variable change came from the game, we don't want to re-assign it and wipe other active edits
                            AssignOrRecordOnly.RecordOnly)
                        );
                    }

                    if(regenerateAndSave)
                    {

                        // this may not be the current screen:
                        var nosParent = ObjectFinder.Self.GetElementContaining(nos);

                        GlueCommands.Self.GluxCommands.SaveProjectAndElements();
                        GlueCommands.Self.DoOnUiThread(GlueCommands.Self.RefreshCommands.RefreshVariables);
                        GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(nosParent);
                    }

                }
            }, "Handling set variable from game", 
            // This is going to push the change back to the game, and we don't want to sit and wait for codegen to finish, etc. Do it immediately!
            // Update - if it's asap, then later commands can execute before earlier ones, so we need to still respect fifo. Fifo adds delays but avoids
            // the confusion.
            TaskExecutionPreference.Fifo);
        }

        private async Task HandleSetVariableDtoList(SetVariableDtoList setVariableDtoList)
        {
            HashSet<NamedObjectSave> modifiedObjects = new HashSet<NamedObjectSave>();

            var gameScreenName = await CommandSender.Self.GetScreenName();

            List<GlueVariableSetData> listOfVariables = new List<GlueVariableSetData>();
            HashSet<GlueElement> modifiedGlueElements = new HashSet<GlueElement>();
            foreach (var setVariableDto in setVariableDtoList.SetVariableList)
            {
                await HandleSetVariable(setVariableDto, sendBackToGame:false, regenerateAndSave: false);

                var type = string.Join('\\', setVariableDto.InstanceOwner.Split('.').Skip(1));
                var element = ObjectFinder.Self.GetElement(type);
                var nos = element.GetNamedObjectRecursively(setVariableDto.ObjectName);

                //GlueCommands.Self.DoOnUiThread(() =>
                //    RefreshManager.Self.HandleNamedObjectValueChanged(setVariableDto.VariableName, null, nos,
                //    // record only - this variable change came from the game, we don't want to re-assign it and wipe other active edits
                //    AssignOrRecordOnly.RecordOnly)
                //);
                if(nos != null)
                {
                    var foundVariable = nos.GetCustomVariable(setVariableDto.VariableName);
                    //await VariableSendingManager.Self.HandleNamedObjectValueChanged(setVariableDto.VariableName, null, nos, AssignOrRecordOnly.RecordOnly);
                    List<GlueVariableSetData> listOfInner = _variableSendingManager.GetNamedObjectValueChangedDtos(
                        setVariableDto.VariableName, null, nos, AssignOrRecordOnly.RecordOnly, gameScreenName);

                    listOfVariables.AddRange(listOfInner);

                    modifiedObjects.Add(nos);
                }
            }

            _variableSendingManager.PushVariableChangesToGame(listOfVariables, modifiedObjects.ToList());

            await TaskManager.Self.AddAsync(() =>
            {
                if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.SeparateJsonFilesForElements)
                {
                    foreach(var item in modifiedGlueElements)
                    {
                        GlueCommands.Self.GluxCommands.SaveElementAsync(item);
                    }
                }
                else
                {
                    GlueCommands.Self.GluxCommands.SaveProjectAndElements();
                }
                GlueCommands.Self.DoOnUiThread(GlueCommands.Self.RefreshCommands.RefreshVariables);

                HashSet<GlueElement> nosParents = new HashSet<GlueElement>();
                foreach(var nos in modifiedObjects)
                {
                    var nosParent = ObjectFinder.Self.GetElementContaining(nos);
                    if(nosParent != null)
                    {
                        nosParents.Add(nosParent);
                    }
                }

                foreach(var nosParent in nosParents)
                {
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(nosParent);
                }
            }, $"Wrapping up assignment of {setVariableDtoList.SetVariableList.Count} variables");

        }

        private object ConvertVariable(object value, ref string typeName, string variableName, NamedObjectSave owner,
            // We have to pass this because the NOS may not have yet been added to an element if it is a new one, but we still
            // need the element to assign file names
            GlueElement nosContainer)
        {
            if(typeName == typeof(List<FlatRedBall.Math.Geometry.Point>).ToString())
            {
                value = JsonConvert.DeserializeObject<List<FlatRedBall.Math.Geometry.Point>>(value.ToString());

            } 
            else
            {
                if(typeName.StartsWith($"{GlueState.Self.ProjectNamespace}."))
                {
                    typeName = typeName.Substring(typeName.IndexOf('.') + 1);
                }
                switch (typeName)
                {
                    case "float":
                    case nameof(Single):
                        {
                            if (value is double asDouble)
                            {
                                value = (float)asDouble;
                            }
                            else if(value is int asInt)
                            {
                                value = (float)asInt;
                            }
                        }
                        break;
                    case "decimal":
                    case nameof(Decimal):
                        {
                            if (value is double asDouble)
                            {
                                value = (decimal)asDouble;
                            }
                            else if (value is int asInt)
                            {
                                value = (decimal)asInt;
                            }
                            else if (value is float asFloat)
                            {
                                value = (decimal)asFloat;
                            }
                        }
                        break;
                    case "Microsoft.Xna.Framework.Graphics.Texture2D":
                    case nameof(Texture2D):
                    case "FlatRedBall.Graphics.Animation.AnimationChainList":
                    case nameof(AnimationChainList):
                        if (value is string asString && !string.IsNullOrEmpty(asString))
                        {
                            value =
                                FileManager.RemovePath(FileManager.RemoveExtension(asString));

                            ReferencedFileSave rfs = null;
                            if (nosContainer != null)
                            {
                                rfs = nosContainer.GetReferencedFileSaveByInstanceNameRecursively(value as string, caseSensitive:false);
                            }

                            if(rfs != null)
                            {
                                value = rfs.GetInstanceName();
                            }
                        }
                        break;
                    case nameof(TextureAddressMode):
                    case "Microsoft.Xna.Framework.Graphics.TextureAddressMode":
                        value = ToEnum<TextureAddressMode>(value);
                        break;
                    case nameof(FlatRedBall.Graphics.ColorOperation):
                    case "FlatRedBall.Graphics.ColorOperation":
                        value = ToEnum<FlatRedBall.Graphics.ColorOperation>(value);
                        break;
                    case nameof(FlatRedBall.Graphics.BlendOperation):
                    case "FlatRedBall.Graphics.BlendOperation":
                        value = ToEnum<FlatRedBall.Graphics.BlendOperation>(value);
                        break;
                    case nameof(FlatRedBall.Graphics.HorizontalAlignment):
                    case "FlatRedBall.Graphics.HorizontalAlignment":
                        value = ToEnum<FlatRedBall.Graphics.HorizontalAlignment>(value);
                        break;
                    case nameof(FlatRedBall.Graphics.VerticalAlignment):
                    case "FlatRedBall.Graphics.VerticalAlignment":
                        value = ToEnum<FlatRedBall.Graphics.VerticalAlignment>(value);
                        break;
                }
            }

            T ToEnum<T>(object toConvert)
            {
                T converted = default(T);
                if (toConvert is int asInt)
                {
                    converted = (T)(object)asInt;
                }
                else if (toConvert is long asLong)
                {
                    converted = (T)(object)(int)asLong;
                }
                return converted;
            }

            if(value is List<FlatRedBall.Math.Geometry.Point> pointList && owner.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Polygon &&
                variableName == "Points")
            {
                value = pointList.Select(item => new Vector2((float)item.X, (float)item.Y)).ToList();
                typeName = typeof(List<Vector2>).ToString();
            }

            return value;
        }

        #endregion

        #region Select Object

        //public async void HandleSelectObject(SelectObjectDto selectObjectDto)
        //{

        //    var screen = await CommandSender.Self.GetCurrentInGameScreen();
        //    TaskManager.Self.Add(() =>
        //    {
        //        //NamedObjectSave nos = null;
        //        List<NamedObjectSave> namedObjectSaves = new List<NamedObjectSave>();

        //        if(screen == null)
        //        {
        //            var entityName = selectObjectDto.ElementNameGlue;
        //            EntitySave currentEntity = ObjectFinder.Self.GetEntitySave(entityName);
                    
        //            if(currentEntity != null)
        //            {
        //                foreach(var selectedNamedObject in selectObjectDto.NamedObjects)
        //                {
        //                    var instanceName = selectedNamedObject.InstanceName;
        //                    var nos = currentEntity.GetNamedObjectRecursively(instanceName);
        //                    if(nos == null && instanceName?.StartsWith('m') == true && instanceName?.Length > 1)
        //                    {
        //                        nos = currentEntity.GetNamedObjectRecursively(selectObjectDto.NamedObjects.FirstOrDefault()?.InstanceName[1..]);
        //                    }
        //                    if(nos != null)
        //                    {
        //                        namedObjectSaves.Add(nos);
        //                    }
        //                }
        //            }
        //        }
        //        else
        //        {
        //            foreach(var selectedNamedObject in selectObjectDto.NamedObjects)
        //            {
        //                var nos = screen.GetNamedObjectRecursively(selectedNamedObject.InstanceName);
        //                if(nos != null)
        //                { 
        //                    namedObjectSaves.Add(nos);
        //                }
        //            }
        //        }

        //        if(namedObjectSaves.Count > 0)
        //        {
        //            GlueCommands.Self.DoOnUiThread(() =>
        //            {
        //                var existingSelectionIsSame = GlueState.Self.CurrentNamedObjectSaves.Count == selectObjectDto.NamedObjects.Count;
        //                if(existingSelectionIsSame)
        //                {
        //                    for(int i = 0; i < GlueState.Self.CurrentNamedObjectSaves.Count; i++)
        //                    {
        //                        if (GlueState.Self.CurrentNamedObjectSaves[i] != selectObjectDto.NamedObjects[i])
        //                        {
        //                            existingSelectionIsSame = false;
        //                        }
        //                    }   
        //                }
        //                if (!existingSelectionIsSame)
        //                {
        //                    _refreshManager.IgnoreNextObjectSelect = true;

        //                    GlueState.Self.CurrentNamedObjectSaves = namedObjectSaves;
        //                }

        //                var nos = namedObjectSaves.FirstOrDefault();
        //                var showVariables = true;
        //                var canShowPoints = nos?.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Polygon;
        //                var alreadyShownTabs = GlueState.Self.CurrentFocusedTabs;

        //                if(canShowPoints && alreadyShownTabs.Contains("Points"))
        //                {
        //                    showVariables = false;
        //                }

        //                if(showVariables && !alreadyShownTabs.Contains("Variables"))
        //                {
        //                    GlueCommands.Self.DialogCommands.FocusTab("Variables");
        //                }
        //            });
        //        }
        //        else
        //        {
        //            if(GlueState.Self.CurrentNamedObjectSave != null)
        //            {
        //                var element = GlueState.Self.CurrentElement;
        //                GlueState.Self.CurrentNamedObjectSave = null;

        //                if(element != null)
        //                {
        //                    GlueState.Self.CurrentElement = element;
        //                }
        //            }
        //        }
        //    }, "Selecting object from game command");
        //}

        #endregion

        #region Modify Collision (Tile)

        private void HandleDto(ModifyCollisionDto dto)
        {

            ///////////////Early Out///////////////////////////
            var hasAnything = dto.AddedPositions?.Count > 0 || dto.RemovedPositions?.Count > 0;
            if(!hasAnything)
            {
                return;
            }
            /////////////End Early Out/////////////////////////
            string collisionTileTypeName;
            FlatRedBall.IO.FilePath tmxFilePath;
            TiledMapSave tiledMapSave;
            MapLayer mapLayer;
            GetMapLayer(dto, out collisionTileTypeName, out tmxFilePath, out tiledMapSave, out mapLayer);

            var didChange = false;

            if(mapLayer == null)
            {
                GlueCommands.Self.PrintOutput("Tried to paint files, but could not find map layer");
            }
            if (mapLayer != null)
            {

                uint tileTypeGid = 0;
                foreach (var tileset in tiledMapSave.Tilesets)
                {
                    foreach (var kvp in tileset.TileDictionary)
                    {
                        var tilesetTile = kvp.Value;
                        if (tilesetTile.Type == collisionTileTypeName)
                        {
                            tileTypeGid = kvp.Key + tileset.Firstgid;
                            break;
                        }
                    }

                    if (tileTypeGid > 0)
                    {
                        break;
                    }
                }

                var ids = mapLayer.data[0].tiles;

                // todo - support seeds
                if (dto.AddedPositions != null)
                {
                    foreach (var newTile in dto.AddedPositions)
                    {
                        int absoluteIndex = GetTileIndexFromWorldPosition(newTile, mapLayer);

                        if (absoluteIndex >= 0 && absoluteIndex < ids.Length && ids[absoluteIndex] != tileTypeGid)
                        {
                            ids[absoluteIndex] = tileTypeGid;
                            didChange = true;
                        }
                    }
                }

                if (dto.RemovedPositions != null)
                {
                    foreach (var oldTile in dto.RemovedPositions)
                    {
                        int absoluteIndex = GetTileIndexFromWorldPosition(oldTile, mapLayer);

                        if (absoluteIndex < ids.Length && absoluteIndex > -1 && ids[absoluteIndex] != 0)
                        {
                            ids[absoluteIndex] = 0;
                            didChange = true;
                        }
                    }
                }

                if (didChange)
                {
                    mapLayer.data[0].SetTileData(ids, mapLayer.data[0].encoding, mapLayer.data[0].compression);

                    _refreshManager.IgnoreNextChange(tmxFilePath.FullPath);

                    GlueCommands.Self.TryMultipleTimes(() => tiledMapSave.Save(tmxFilePath.FullPath));

                    // Restart the screen *after* the TMX is saved, and after it has been copied too:
                    // The TMX needs to be copied which is a tasked operation:
                    if(dto.RequestRestart)
                    {
                        TaskManager.Self.Add(() =>
                        {
                            GlueCommands.Self.ProjectCommands.CopyToBuildFolder(tmxFilePath.FullPath);

                            var playBump = true;
                            // tell the game that it should restart the screen quietly
    #pragma warning disable CS4014 // Do not await in add calls this can cause problems
                            CommandSender.Self.Send(new RestartScreenDto { ShowSelectionBump = playBump });
    #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        },
                        "Copy TMX and restart screen",
                        executionPreference: TaskExecutionPreference.Asap);
                    }

                }
            }

            int GetTileIndexFromWorldPosition(Vector2 worldPosition, MapLayer mapLayer)
            {
                // todo - read tile size properties
                var xIndex = (int)(worldPosition.X / 16);
                var yIndex = (int)-(worldPosition.Y / 16);


                var mapWidth = mapLayer.width;
                var mapHeight = mapLayer.height;

                if (xIndex >= mapWidth || xIndex < 0 || yIndex < 0 || yIndex > mapHeight)
                {
                    return -1;
                }
                else
                {
                    var absoluteIndex = xIndex + yIndex * mapWidth;
                    return absoluteIndex;
                }

            }
        }

        private void GetMapLayer(ModifyCollisionDto dto, out string collisionTileTypeName, out FlatRedBall.IO.FilePath tmxFilePath, out TiledMapSave tiledMapSave, out MapLayer mapLayer)
        {
            // Maps can be in entities too for "rooms" so support both entities and screens
            var currentElement = GlueState.Self.CurrentElement;
            tmxFilePath = null;
            tiledMapSave = null;
            mapLayer = null;
            collisionTileTypeName = null;

            ////////////////////////////////////Early Out//////////////////////////////////////////////
            if (currentElement == null)
            {
                return;
            }
            /////////////////////////////////End Early Out/////////////////////////////////////////////

            var collisionNos = currentElement?.GetNamedObject(dto.TileShapeCollection);

            // this should use a file as a source, need to find that
            var sourceTmxObjectName = collisionNos.Properties.GetValue<string>("SourceTmxName");
            const int FromTypeCollisionCreationOption = 4;

            var isFromType = ObjectFinder.Self.GetPropertyValueRecursively<int>(
                collisionNos, "CollisionCreationOptions") == FromTypeCollisionCreationOption;

            collisionTileTypeName = collisionNos.Properties.GetValue<string>("CollisionTileTypeName");
            var isUsingTmx = !string.IsNullOrEmpty(sourceTmxObjectName) && isFromType && !string.IsNullOrEmpty(collisionTileTypeName);

            if (isUsingTmx)
            {
                var tmxObjectNos = currentElement.GetNamedObjectRecursively(sourceTmxObjectName);
                ReferencedFileSave rfs = null;
                if (tmxObjectNos.SourceType == SourceType.File)
                {
                    rfs = currentElement.GetReferencedFileSaveRecursively(tmxObjectNos.SourceFile);
                }

                if (rfs != null)
                {
                    tmxFilePath = GlueCommands.Self.GetAbsoluteFilePath(rfs);
                }
            }

            if (tmxFilePath?.Exists() == true)
            {
                tiledMapSave = TiledMapSave.FromFile(tmxFilePath.FullPath);
                // assume this for now...
                mapLayer = tiledMapSave.Layers.Find(item => item.Name == "GameplayLayer");
            }
        }

        #endregion

        #region GoToDefinitionDto

        private void HandleDto(GoToDefinitionDto dto)
        {
            GlueCommands.Self.DialogCommands.GoToDefinitionOfSelection();
        }


        #endregion

        #region SelectPrevious/Next

        private void HandleDto(SelectPreviousDto dto) => TreeNodeStackManager.Self.GoBack();
        private void HandleDto(SelectNextDto dto) => TreeNodeStackManager.Self.GoForward();

        #endregion

        #region CurrentDisplayInfoDto

        private void HandleDto(CurrentDisplayInfoDto dto)
        {
            var zoomValue = dto.ZoomPercentage;

            // If it's in game mode, then display the % using the destination height / display settings height
            if (CompilerViewModel.IsEditChecked == false && GlueState.Self.CurrentGlueProject?.DisplaySettings != null)
            {
                zoomValue = 100 * (decimal)(dto.DestinationRectangleHeight / (decimal)GlueState.Self.CurrentGlueProject.DisplaySettings.ResolutionHeight);
            }

            zoomValue = (int)zoomValue; // to prevent lots of trailing decimals

            GlueCommands.Self.DoOnUiThread(() =>
            {
                CompilerViewModel.CurrentZoomLevelDisplay = zoomValue.ToString() + "%";
                CompilerViewModel.ResolutionDisplayText = $"{dto.DestinationRectangleWidth}x{dto.DestinationRectangleHeight}";
            });

        }

        #endregion

        #region Glue/XXXX/CommandDto

        private async Task<string> HandleDto(GlueCommandDto dto) => await HandleFacadeCommand(GlueCommands.Self, dto);

        private async Task<string> HandleDto(GluxCommandDto dto)
        {
            if(dto.Method == nameof(GluxCommands.SetVariableOn) && !dto.EchoToGame)
            {
                // suppress this 
                var nos = (NamedObjectSave) Convert(dto.Parameters[0], typeof(NamedObjectSave));
                var memberName = (string)dto.Parameters[1];

                _variableSendingManager.AddOneTimeIgnore(nos, memberName);
            }
            if(dto.Method == nameof(GluxCommands.SetVariableOnList))
            {
                var list = (List<NosVariableAssignment>) Convert(dto.Parameters[0], typeof(List<NosVariableAssignment>));

                foreach(var item in list)
                {
                    var nos = item.NamedObjectSave;
                    var memberName = item.VariableName;
                    _variableSendingManager.AddOneTimeIgnore(nos, memberName);
                }
            }
            return await HandleFacadeCommand(GlueCommands.Self.GluxCommands, dto);
        }

        private async Task<string> HandleDto(GlueStateDto dto) => await HandleFacadeCommand(GlueState.Self, dto);
        private async Task<string> HandleDto(GenerateCodeCommandDto dto) => await HandleFacadeCommand(GlueCommands.Self.GenerateCodeCommands, dto);

        private async Task<string> HandleFacadeCommand(object target, FacadeCommandBase incomingDto)
        {
            string toReturn = null;
            var targetType = target.GetType();
            if(!string.IsNullOrEmpty( incomingDto.Method ))
            {
                MethodInfo method = targetType.GetMethod(incomingDto.Method);
                var dtoParameters = incomingDto.Parameters;
                List<object> parameters = new List<object>();
                var methodParameters = method.GetParameters();

                for (int i = 0; i < dtoParameters.Count; i++)
                {
                    var parameter = dtoParameters[i];
                    var parameterInfo = methodParameters[i];
                    var parameterType = parameterInfo.ParameterType;
                    if (incomingDto.CorrectTypeForParameters?.ContainsKey(parameterInfo.Name) == true)
                        parameterType = Type.GetType(incomingDto.CorrectTypeForParameters[parameterInfo.Name]);
                    var converted = Convert(parameter, parameterType);

                    parameters.Add(converted);
                }

                object methodResponse = null;
                try
                {
                    HandleBeforeRunningReceivedMethod(incomingDto);
                    methodResponse = method.Invoke(target, parameters.ToArray());
                }
                catch(TargetParameterCountException)
                {
                    var message =
                        $"Attempting to invoke method {incomingDto.Method} but the parameter count is wrong. Did this change in Glue without changing in the generated code for this method?";
                    throw new Exception(message);
                }

                if(methodResponse is Task asTask)
                {
                    var taskType = asTask.GetType();
                    await asTask;
                    if(taskType.IsGenericType())
                    {
                        var resultProperty = taskType.GetProperty("Result");
                        var taskResult = resultProperty.GetValue(asTask);

                        if(taskResult != null)
                        {
                            toReturn = JsonConvert.SerializeObject(taskResult);
                        }

                    }
                    else
                    {
                        toReturn = null;
                    }


                }
                else
                {
                    var contentToGame = methodResponse;
                    if(contentToGame != null)
                    {
                        toReturn = JsonConvert.SerializeObject(contentToGame);
                    }
                }
            }
            else if(!string.IsNullOrEmpty(incomingDto.SetPropertyName))
            {
                PropertyInfo property = targetType.GetProperty(incomingDto.SetPropertyName);
                var parameter = incomingDto.Parameters[0];
                var converted = Convert(parameter, property.PropertyType);

                if(incomingDto.SetPropertyName == "CurrentNamedObjectSaves")
                {
                    // ignore the next selection
                    _refreshManager.IgnoreNextObjectSelect = true;
                }

                property.SetValue(target, converted);

                toReturn = null;
            }
            return toReturn;
        }

        private void HandleBeforeRunningReceivedMethod(FacadeCommandBase incomingDto)
        {
            if (incomingDto.Method == nameof(GlueCommands.Self.GluxCommands.CopyNamedObjectListIntoElement))
            {
                _refreshManager.NextPositionValues = new RefreshManager.NewObjectListPositionValues
                {
                    SkipPositioningForNextGroup = true
                };
            }
        }

        //private async Task<ResponseWithContentDto> SendResponseBackToGame(FacadeCommandBase dto, object contentToGame)
        //{
        //    var response = new ResponseWithContentDto();
        //    response.Id = -1;
        //    response.OriginalDtoId = dto.Id;
        //    if (contentToGame != null)
        //    {
        //        response.Content = JsonConvert.SerializeObject(contentToGame);
        //    }
        //    await CommandSender.Self.Send(response, waitForResponse:false);
        //    return response;
        //}

        private object Convert(object parameter, Type reflectedParameterType)
        {

            return Convert(parameter, GetFriendlyName(reflectedParameterType));
        }

        string GetFriendlyName(Type type)
        {
            string friendlyName = type.Name;
            if (type.IsGenericType)
            {
                int iBacktick = friendlyName.IndexOf('`');
                if (iBacktick > 0)
                {
                    friendlyName = friendlyName.Remove(iBacktick);
                }
                friendlyName += "<";
                Type[] typeParameters = type.GetGenericArguments();
                for (int i = 0; i < typeParameters.Length; ++i)
                {
                    string typeParamName = GetFriendlyName(typeParameters[i]);
                    friendlyName += (i == 0 ? typeParamName : "," + typeParamName);
                }
                friendlyName += ">";
            }

            return friendlyName;
        }

        public class NosReferenceVariableAssignment
        {
#pragma warning disable CS0649 // These are set from json deserialization
            public NamedObjectSaveReference NamedObjectSave;
            public string VariableName;
            public TypedParameter Value;
#pragma warning restore CS0649 // Field 'CommandReceiver.NosReferenceVariableAssignment.NamedObjectSave' is never assigned to, and will always have its default value null
        }

        private object Convert(object parameter, string typeName)
        { 
            var converted = parameter;

            if(parameter is JObject asJObject)
            {
                if(typeName == typeof(NamedObjectSave).Name)
                {
                    var reference = asJObject.ToObject<NamedObjectSaveReference>();

                    var parentElement = ObjectFinder.Self.GetElement(reference.GlueElementReference.ElementNameGlue);
                    var nos = parentElement.GetAllNamedObjectsRecurisvely().FirstOrDefault(item => item.InstanceName == reference.NamedObjectName);

                    converted = nos;
                }
                else if(typeName == typeof(GlueElement).Name)
                {
                    var reference = asJObject.ToObject<GlueElementReference>();

                    var element = ObjectFinder.Self.GetElement(reference.ElementNameGlue);
                    converted = element;
                }
                else if(typeName == typeof(TypedParameter).Name)
                {
                    var typedParameter = asJObject.ToObject<TypedParameter>();

                    converted = Convert(typedParameter.Value, typedParameter.Type);
                }
                // I don't think this is possible:
                //else if(typeName == typeof(List<NosVariableAssignment>).Name)
                //{
                //    var assignmentList = asJObject.ToObject<List<NosReferenceVariableAssignment>>();
                //    List<NosVariableAssignment> convertedList = new List<NosVariableAssignment>();
                //    foreach(var assignment in assignmentList)
                //    {
                //        NosVariableAssignment individual = new NosVariableAssignment();

                //        var parentElement = ObjectFinder.Self.GetElement(assignment.NamedObjectSave.GlueElementReference.ElementNameGlue);
                //        var nos = parentElement.GetAllNamedObjectsRecurisvely().FirstOrDefault(item => item.InstanceName == assignment.NamedObjectSave.NamedObjectName);

                //        individual.NamedObjectSave = nos;
                //        individual.VariableName = assignment.VariableName;
                //        individual.Value = Convert(assignment.Value, nameof(TypedParameter));

                //        convertedList.Add(individual);
                //    }
                //    converted = convertedList;
                //}
                else if(typeName == typeof(object).Name)
                {
                    try
                    {
                        // Maybe we could parse this to see if it's a typed parameter?
                        var typedParameter = asJObject.ToObject<TypedParameter>();

                        converted = Convert(typedParameter.Value, typedParameter.Type);
                    }
                    catch { }

                }
            }
            else if(parameter is JArray asJArray)
            {
                if(typeName == GetFriendlyName(typeof(List<NosVariableAssignment>)) ||
                    typeName == GetFriendlyName(typeof(IReadOnlyList<NosVariableAssignment>)))
                {
                    var list = new List<NosVariableAssignment>();
                    foreach(JObject item in asJArray)
                    {
                        var itemReference = item.ToObject<NosReferenceVariableAssignment>();

                        var convertedItem = new NosVariableAssignment();

                        var parentElement = ObjectFinder.Self.GetElement(itemReference.NamedObjectSave.GlueElementReference.ElementNameGlue);
                        var nos = parentElement.GetAllNamedObjectsRecurisvely().FirstOrDefault(item => item.InstanceName == itemReference.NamedObjectSave.NamedObjectName);

                        convertedItem.NamedObjectSave = nos;
                        convertedItem.VariableName = itemReference.VariableName;

                        var typedParameter = (TypedParameter)Convert(itemReference.Value, typeof(TypedParameter));

                        convertedItem.Value = Convert(typedParameter.Value, typedParameter.Type);

                        list.Add(convertedItem);

                    }
                    converted = list;
                }
                else if(typeName == GetFriendlyName(typeof(List<NamedObjectSave>)) ||
                    typeName == GetFriendlyName(typeof(IReadOnlyList<NamedObjectSave>))
                    )
                {
                    var list = new List<NamedObjectSave>();
                    foreach (JObject item in asJArray)
                    {
                        var itemReference = item.ToObject<NamedObjectSaveReference>();

                        var parentElement = ObjectFinder.Self.GetElement(itemReference.GlueElementReference.ElementNameGlue);
                        var nos = parentElement.GetAllNamedObjectsRecurisvely().FirstOrDefault(item => item.InstanceName == itemReference.NamedObjectName);
                        list.Add(nos);
                    }
                    converted = list;
                }
                else if(typeName == GetFriendlyName(typeof(List<Vector2>)))
                {
                    var list = new List<Vector2>();
                    foreach (JToken item in asJArray)
                    {
                        var vector2 = item.ToObject<Vector2>();
                        list.Add(vector2);
                    }
                    converted = list;
                }
                else if (typeName == GetFriendlyName(typeof(List<Vector3>)))
                {
                    var list = new List<Vector3>();
                    foreach (JObject item in asJArray)
                    {
                        var vector3 = item.ToObject<Vector3>();
                        list.Add(vector3);
                    }
                    converted = list;
                }
            }
            else if(parameter is double asDouble)
            {
                if(typeName == typeof(float).Name)
                {
                    converted = (float)asDouble;
                }
            }
            else if(parameter is long asLong)
            {
                if(typeName == typeof(int).Name)
                {
                    converted = (int)asLong;
                }
                else if(typeName == nameof(TaskExecutionPreference))
                {
                    converted = (TaskExecutionPreference)asLong;
                }
                else if(typeName == "Nullable<Int32>")
                {
                    converted = (int?)asLong;
                }
            }
            else if(parameter is object)
            {
                // the method does not take a casted value, but we may have sent a casted value through a TypedParameter
                if(parameter is JObject)
                {

                }
            }
            return converted;
        }

        #endregion
    }
}
