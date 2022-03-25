#define SupportsEditMode
#define IncludeSetVariable
using GlueControl.Dtos;
using GlueControl.Editing;
using Microsoft.Xna.Framework;

using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Math.Collision;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Screens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using {ProjectNamespace};
using GlueControl.Models;


namespace GlueControl
{
    static class CommandReceiver
    {
        #region Supporting Methods/Properties

        /// <summary>
        /// Stores all commands that have been sent from Glue to game 
        /// which should always be re-run.
        /// </summary>
        public static List<object> GlobalGlueToGameCommands = new List<object>();

        static System.Reflection.MethodInfo[] AllMethods;

        static CommandReceiver()
        {
            AllMethods = typeof(CommandReceiver).GetMethods(
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.NonPublic)
                .Where(item => item.Name == nameof(HandleDto))
                .ToArray();
        }

        public static string Receive(string message, Func<object, bool> runPredicate = null)
        {
            try
            {
                string dtoTypeName = null;
                string dtoSerialized = null;
                if (message.Contains(":"))
                {
                    dtoSerialized = message.Substring(message.IndexOf(":") + 1);
                    dtoTypeName = message.Substring(0, message.IndexOf(":"));
                }
                else
                {
                    throw new Exception($"The command {message} does not contain a : (colon) separator");
                }

                var matchingMethod =
                    AllMethods
                    .FirstOrDefault(item =>
                    {
                        var parameters = item.GetParameters();
                        return parameters.Length == 1 && parameters[0].ParameterType.Name == dtoTypeName;
                    });

                if (matchingMethod == null)
                {
                    throw new InvalidOperationException(
                        $"Could not find a HandleDto method for type {dtoTypeName}");
                }

                var dtoType = matchingMethod.GetParameters()[0].ParameterType;

                var dto = JsonConvert.DeserializeObject(dtoSerialized, dtoType);

                if (runPredicate == null || runPredicate(dto))
                {
                    var response = ReceiveDto(dto);

                    if (response != null)
                    {
                        return JsonConvert.SerializeObject(response);
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex + "\nWith message:\n" + message);
                return null;
            }
        }

        public static object ReceiveDto(object dto)
        {
            var type = dto.GetType();

            var method = AllMethods
                .FirstOrDefault(item =>
                {
                    var parameters = item.GetParameters();
                    return parameters.Length == 1 && parameters[0].ParameterType == type;
                });


            object toReturn = null;

            if (method != null)
            {
                toReturn = method.Invoke(null, new object[] { dto });
            }

            return toReturn;
        }

        public static bool GetIfMatchesCurrentScreen(string elementNameGlue)
        {
            return GetIfMatchesCurrentScreen(elementNameGlue, out _, out _);
        }

        private static bool GetIfMatchesCurrentScreen(string elementNameGlue, out System.Type ownerType, out Screen currentScreen)
        {
            var game1FullName = typeof(Game1).FullName;
            var topNamespace = game1FullName.Substring(0, game1FullName.IndexOf('.'));
            //var ownerTypeName = "WhateverNamespace." + elementName.Replace("\\", ".");
            var ownerTypeName = $"{topNamespace}.{elementNameGlue.Replace("\\", ".")}";

            ownerType = typeof(CommandReceiver).Assembly.GetType(ownerTypeName);
            currentScreen = ScreenManager.CurrentScreen;
            var currentScreenType = currentScreen?.GetType();

            return currentScreenType == ownerType || (currentScreenType != null && ownerType?.IsAssignableFrom(currentScreenType) == true);
        }

        // todo - move this to some type manager
        public static bool DoTypesMatch(PositionedObject positionedObject, string qualifiedTypeName, Type possibleType = null)
        {
            if (possibleType == null)
            {
                possibleType = typeof(CommandReceiver).Assembly.GetType(qualifiedTypeName);
            }

            if (positionedObject.GetType() == possibleType)
            {
                return true;
            }
            else if (positionedObject is GlueControl.Runtime.DynamicEntity dynamicEntity)
            {
                return dynamicEntity.EditModeType == qualifiedTypeName;
            }
            else
            {
                // here we need to do reflection to get the EditModeType, but that's not implemented yet.
                // This is needed for inherited entities
                return false;
            }
        }

        #endregion

        #region Set Variable

        private static GlueVariableSetDataResponseList HandleDto(GlueVariableSetDataList dto)
        {
            GlueVariableSetDataResponseList toReturn = new GlueVariableSetDataResponseList();

            foreach (var setVariableDto in dto.Data)
            {
                var setResponse = HandleDto(setVariableDto);
                toReturn.Data.Add(setResponse);
            }

            return toReturn;
        }

        private static GlueVariableSetDataResponse HandleDto(GlueVariableSetData dto)
        {
            GlueVariableSetDataResponse response = null;

            if (dto.AssignOrRecordOnly == AssignOrRecordOnly.Assign)
            {
                var selectedNosNames = Editing.EditingManager.Self.CurrentNamedObjects.Select(item => item.InstanceName)
                    .ToArray();

                string oldName = null;
                string newName = null;
                if (selectedNosNames.Length > 0)
                {
                    var split = dto.VariableName.Split('.');

                    var isAssigningName = split.Length == 3 &&
                        split[2] == "Name";

                    if (isAssigningName)
                    {
                        oldName = split[1];
                        newName = dto.VariableValue;
                    }
                }

                response = GlueControl.Editing.VariableAssignmentLogic.SetVariable(dto);

                if (dto.GlueElement != null)
                {
                    Editing.EditingManager.Self.SetCurrentGlueElement(dto.GlueElement);
                    if (oldName != null && newName != null)
                    {
                        var renamedNos =
                            Editing.EditingManager.Self.CurrentGlueElement.AllNamedObjects.FirstOrDefault(item => item.InstanceName == oldName);

                        if (renamedNos != null)
                        {
                            renamedNos.InstanceName = newName;
                        }

                        var renamedNameable =
                            Editing.EditingManager.Self.ItemsSelected.FirstOrDefault(item => item.Name == oldName);

                        if (renamedNameable != null)
                        {
                            renamedNameable.Name = newName;
                        }
                    }
                }
            }
            else
            {
                // If it's a record-only, then we'll always want to enqueue it
                // need to change the record only back to assign so future re-runs will assign
                dto.AssignOrRecordOnly = AssignOrRecordOnly.Assign;
            }

            GlobalGlueToGameCommands.Add(dto);

            return response;
        }

        #endregion

        #region Set State Variable

        private static void HandleDto(ChangeStateVariableDto dto)
        {
            var elementGameType = dto.ElementNameGame;
            var categoryName = dto.CategoryName;
            var stateSave = dto.StateSave;

            ReplaceStateWithNewState(elementGameType, categoryName, stateSave);

            // stop all movement in case the state assigned movement
            foreach (var item in SpriteManager.ManagedPositionedObjects)
            {
                item.Velocity = Microsoft.Xna.Framework.Vector3.Zero;
                item.Acceleration = Microsoft.Xna.Framework.Vector3.Zero;
            }
        }

        #endregion

        private static object HandleDto(GetCameraPosition dto)
        {
            var toReturn = string.Empty;

            var getCameraPositionResponse = new GlueControl.Dtos.GetCameraPositionResponse();
            getCameraPositionResponse.X = Camera.Main.X;
            getCameraPositionResponse.Y = Camera.Main.Y;
            getCameraPositionResponse.Z = Camera.Main.Z;
            return getCameraPositionResponse;
        }

        #region Set Camera Position

        private static void HandleDto(SetCameraPositionDto dto)
        {
            Camera.Main.Position = dto.Position;
        }

        #endregion

        #region SetCameraSetupDto

        private static void HandleDto(SetCameraSetupDto dto)
        {
            CameraSetup.Data = dto;

            CameraSetup.ResetWindow();
        }

        #endregion

        #region SetCameraAspectRatioDto

        private static void HandleDto(SetCameraAspectRatioDto dto)
        {
            CameraSetup.Data.AspectRatio = dto.AspectRatio;
            
            CameraSetup.ResetCamera();

            if (FlatRedBall.Screens.ScreenManager.IsInEditMode)
            {
                CameraLogic.UpdateCameraToZoomLevel();
            }
        }

        #endregion

        #region Select Object

        private static void HandleDto(SelectObjectDto selectObjectDto)
        {

            // if it matches, don't fall back to the backup element
            bool matchesCurrentScreen =
                GetIfMatchesCurrentScreen(selectObjectDto.ElementNameGlue, out System.Type ownerType, out Screen currentScreen);

            string elementNameGlue = selectObjectDto.BackupElementNameGlue ?? selectObjectDto.ElementNameGlue;
            if (matchesCurrentScreen)
            {
                elementNameGlue = selectObjectDto.ElementNameGlue;
            }

            string ownerTypeName = GlueToGameElementName(elementNameGlue);
            ownerType = typeof(CommandReceiver).Assembly.GetType(ownerTypeName);

            bool isOwnerScreen = false;

            try
            {
                Editing.EditingManager.Self.SetCurrentGlueElement(selectObjectDto.GlueElement);
            }
            catch (ArgumentException e)
            {
                var message =
                    $"The command to select {selectObjectDto.NamedObject} in {selectObjectDto.GlueElement} " +
                    $"threw an exception because the Glue object has a null object.  Inner details:{e}";

                throw new ArgumentException(message);
            }

            if (matchesCurrentScreen)
            {
                Editing.EditingManager.Self.Select(selectObjectDto.NamedObject, playBump: true, focusCameraOnObject: selectObjectDto.BringIntoFocus);
                Editing.EditingManager.Self.ElementEditingMode = GlueControl.Editing.ElementEditingMode.EditingScreen;
                if (!string.IsNullOrEmpty(selectObjectDto.StateName))
                {
                    SelectState(selectObjectDto.StateName, selectObjectDto.StateCategoryName);
                }
                isOwnerScreen = true;
            }
            else
            {
                CameraLogic.RecordCameraForCurrentScreen();

                bool selectedNewScreen = ownerType != null && typeof(Screen).IsAssignableFrom(ownerType);
                if (selectedNewScreen)
                {
#if SupportsEditMode
                    ScreenManager.IsNextScreenInEditMode = ScreenManager.IsInEditMode;

                    void AfterInitializeLogic(Screen screen)
                    {
                        // Select this even if it's null so the EditingManager deselects 
                        EditingManager.Self.Select(selectObjectDto.NamedObject, playBump: true, focusCameraOnObject: true);

                        if (!string.IsNullOrEmpty(selectObjectDto.StateName))
                        {
                            SelectState(selectObjectDto.StateName, selectObjectDto.StateCategoryName);
                        }

                        screen.ScreenDestroy += HandleScreenDestroy;
                        CameraLogic.UpdateCameraValuesToScreenSavedValues(screen);

                        ScreenManager.ScreenLoaded -= AfterInitializeLogic;
                    }
                    ScreenManager.ScreenLoaded += AfterInitializeLogic;

                    ScreenManager.CurrentScreen.MoveToScreen(ownerType);
                    EditorVisuals.DestroyContainedObjects();

                    isOwnerScreen = true;
                    EditingManager.Self.ElementEditingMode = GlueControl.Editing.ElementEditingMode.EditingScreen;
#endif
                }
                else
                {
                    if (!string.IsNullOrEmpty(selectObjectDto.StateName))
                    {
                        SelectState(selectObjectDto.StateName, selectObjectDto.StateCategoryName);
                    }
                }
            }

            if (!isOwnerScreen)
            {
                var isEntity = typeof(PositionedObject).IsAssignableFrom(ownerType) ||
                    InstanceLogic.Self.CustomGlueElements.ContainsKey(ownerTypeName);

                if (isEntity)
                {
                    var isAlreadyViewingThisEntity = ScreenManager.CurrentScreen.GetType().Name == "EntityViewingScreen" &&
                        SpriteManager.ManagedPositionedObjects.Count > 0 &&
                        DoTypesMatch(SpriteManager.ManagedPositionedObjects[0], ownerTypeName, ownerType);

                    if (!isAlreadyViewingThisEntity)
                    {
#if SupportsEditMode
                        ScreenManager.IsNextScreenInEditMode = ScreenManager.IsInEditMode;

                        void AfterInitializeLogic(Screen newScreen)
                        {
                            newScreen.ScreenDestroy += HandleScreenDestroy;

                            FlatRedBall.Screens.ScreenManager.ScreenLoaded -= AfterInitializeLogic;

                            if (!string.IsNullOrEmpty(selectObjectDto.StateName))
                            {
                                SelectState(selectObjectDto.StateName, selectObjectDto.StateCategoryName);
                            }
                        }

                        FlatRedBall.Screens.ScreenManager.ScreenLoaded += AfterInitializeLogic;

                        EditorVisuals.DestroyContainedObjects();

                        Screens.EntityViewingScreen.GameElementTypeToCreate = GlueToGameElementName(elementNameGlue);
                        Screens.EntityViewingScreen.InstanceToSelect = selectObjectDto.NamedObject;
                        ScreenManager.CurrentScreen.MoveToScreen(typeof(Screens.EntityViewingScreen));
#endif
                    }
                    else
                    {
                        EditingManager.Self.Select(selectObjectDto.NamedObject, playBump: true, focusCameraOnObject: true);
                    }
                }
            }
        }



        private static void SelectState(string stateName, string stateCategoryName)
        {
            var currentScreen = ScreenManager.CurrentScreen;
            var entity = SpriteManager.ManagedPositionedObjects.FirstOrDefault();
            ////////////////Early Out//////////////////////
            if (currentScreen.GetType().Name != "EntityViewingScreen" ||
                entity == null)
            {
                return;
            }
            /////////////End Early Out/////////////////////

            var entityType = entity.GetType();

            var stateTypeName = entityType.FullName + "+" + stateCategoryName ?? "VariableState";

            var stateType = entityType.Assembly.GetType(stateTypeName);

            var dictionary = stateType.GetField("AllStates").GetValue(null) as System.Collections.IDictionary;

            if (dictionary.Contains(stateName))
            {
                // got the state, gotta apply:
                var stateInstance = dictionary[stateName];

                string propertyName = "VariableState";

                if (!string.IsNullOrEmpty(stateCategoryName))
                {
                    propertyName = $"Current{stateCategoryName}State";
                }

                var stateProperty = entityType.GetProperty(propertyName);

                stateProperty.SetValue(entity, stateInstance);

                // stop all movement in case the state assigned movement
                foreach (var item in SpriteManager.ManagedPositionedObjects)
                {
                    item.Velocity = Microsoft.Xna.Framework.Vector3.Zero;
                    item.Acceleration = Microsoft.Xna.Framework.Vector3.Zero;
                }
            }
        }

        #endregion


        #region ChangeZoomDto

        private static void HandleDto(ChangeZoomDto changeZoomDto)
        {
            if (changeZoomDto.PlusOrMinus == PlusOrMinus.Plus)
            {
                CameraLogic.DoZoomMinus();
            }
            else
            {
                CameraLogic.DoZoomPlus();
            }
        }

        #endregion

        #region Rename

        static string topNamespace = null;
        public static string GlueToGameElementName(string elementName)
        {
            if (topNamespace == null)
            {
                var game1FullName = typeof(Game1).FullName;
                topNamespace = game1FullName.Substring(0, game1FullName.IndexOf('.'));
            }
            return $"{topNamespace}.{elementName.Replace("\\", ".")}";
        }

        public static string GameElementTypeToGlueElement(string gameType)
        {
            var strings = gameType.Split('.');

            return string.Join("\\", strings.Skip(1).ToArray());
        }

        #endregion

        #region Destroy Screen

        private static void HandleScreenDestroy()
        {
            GlueControl.InstanceLogic.Self.DestroyDynamicallyAddedInstances();
        }

        #endregion

        #region Destroy NamedObjectSave

        private static RemoveObjectDtoResponse HandleDto(RemoveObjectDto removeObjectDto)
        {
            var response = InstanceLogic.Self.HandleDeleteInstanceCommandFromGlue(removeObjectDto);

            Editing.EditingManager.Self.SetCurrentGlueElement(removeObjectDto.GlueElement);

            CommandReceiver.GlobalGlueToGameCommands.Add(removeObjectDto);

            return response;
        }

        #endregion

        #region Add Entity

        private static void HandleDto(CreateNewEntityDto createNewEntityDto)
        {
            var entitySave = createNewEntityDto.EntitySave;

            // convert the entity save name (which is the glue name) to a type name:
            string elementName = GlueToGameElementName(entitySave.Name);


            InstanceLogic.Self.CustomGlueElements[elementName] = entitySave;
        }

        #endregion

        #region Add Object

        private static AddObjectDtoResponse HandleDto(AddObjectDto dto)
        {
            AddObjectDtoResponse valueToReturn = new AddObjectDtoResponse();

            var createdObject =
                GlueControl.InstanceLogic.Self.HandleCreateInstanceCommandFromGlue(dto, GlobalGlueToGameCommands.Count, forcedItem: null);
            valueToReturn.WasObjectCreated = createdObject != null;

            // internally this decides what to add to, so we don't have to sort the DTOs
            //CommandReceiver.EnqueueToOwner(dto, dto.ElementNameGame);
            GlobalGlueToGameCommands.Add(dto);

            return valueToReturn;
        }

        #endregion

        #region Add State

        private static void HandleDto(CreateNewStateDto dto)
        {
            var elementGameType = dto.ElementNameGame;
            var categoryName = dto.CategoryName;
            var stateSave = dto.StateSave;

            ReplaceStateWithNewState(elementGameType, categoryName, stateSave);
        }

        private static void ReplaceStateWithNewState(string elementGameType, string categoryName, StateSave newStateSave)
        {
            List<StateSaveCategory> statesForThisElement = null;
            if (!InstanceLogic.Self.StatesAddedAtRuntime.ContainsKey(elementGameType))
            {
                InstanceLogic.Self.StatesAddedAtRuntime[elementGameType] =
                    new List<StateSaveCategory>();
            }
            statesForThisElement = InstanceLogic.Self.StatesAddedAtRuntime[elementGameType];

            // does this category exist?
            var category = statesForThisElement.FirstOrDefault(item => item.Name == categoryName);
            if (category == null)
            {
                category = new StateSaveCategory();
                category.Name = categoryName;
                statesForThisElement.Add(category);
            }

            var existingWithMatchingName = category.States.FirstOrDefault(item => item.Name == newStateSave.Name);
            if (existingWithMatchingName != null)
            {
                category.States.Remove(existingWithMatchingName);
            }

            category.States.Add(newStateSave);

            // Now create the runtime object and 
            var stateType = VariableAssignmentLogic.TryGetStateType(elementGameType + "." + (categoryName ?? "VariableState"));
            if (stateType != null)
            {
                var allStates = stateType.GetField("AllStates").GetValue(null) as System.Collections.IDictionary;

                object existingState = null;

                if (allStates.Contains(newStateSave.Name))
                {
                    existingState = allStates[newStateSave.Name];
                }
                else
                {
                    existingState = Activator.CreateInstance(stateType);
                    FlatRedBall.Instructions.Reflection.LateBinder.SetValueStatic(existingState, "Name", newStateSave.Name);
                    allStates[newStateSave.Name] = existingState;
                }

                // what if a value has been nulled out?
                // Categories require all values to be set
                // so it won't matter there, and Vic thinks we
                // should phase out uncategorized states so maybe
                // there's no need to handle that here?
                foreach (var instruction in newStateSave.InstructionSaves)
                {
                    InstanceLogic.Self.AssignVariable(existingState, instruction, convertFileNamesToObjects: false);
                }
            }
        }

        #endregion

        #region Add Variable

        private static void HandleDto(AddVariableDto dto)
        {
            var newVariable = dto.CustomVariable;

            if (!InstanceLogic.Self.CustomVariablesAddedAtRuntime.ContainsKey(dto.ElementGameType))
            {
                var newList = new List<CustomVariable>();

                InstanceLogic.Self.CustomVariablesAddedAtRuntime.Add(dto.ElementGameType, newList);
            }

            List<CustomVariable> listToAddTo = InstanceLogic.Self.CustomVariablesAddedAtRuntime[dto.ElementGameType];

            var existingVariable = listToAddTo.FirstOrDefault(item => item.Name == dto.CustomVariable.Name);

            if (existingVariable != null)
            {
                listToAddTo.Remove(existingVariable);
            }

            listToAddTo.Add(dto.CustomVariable);
        }

        #endregion

        #region GetCameraSave

        private static object HandleDto(GetCameraSave dto)
        {
            return FlatRedBall.Content.Scene.CameraSave.FromCamera(Camera.Main);
        }

        #endregion

        #region EditMode vs Play

        private static object HandleDto(SetEditMode setEditMode)
        {
            var value = setEditMode.IsInEditMode;
#if SupportsEditMode

            var response = new Dtos.GeneralCommandResponse
            {
                Succeeded = true,
            };

            if (ScreenManager.CurrentScreen == null)
            {
                response.Succeeded = false;
                response.Message = "The ScreenManager.CurrentScreen is null, so the screen cannot be restarted. Is the screen you are viewing abstract (such as GameScreen)? If so, this may be why the Screen hasn't been created.";

            }

            if (value != FlatRedBall.Screens.ScreenManager.IsInEditMode)
            {
                CameraLogic.RecordCameraForCurrentScreen();

                FlatRedBall.Gui.GuiManager.Cursor.RequiresGameWindowInFocus = !value;

                if (value)
                {
                    FlatRedBallServices.Game.IsMouseVisible = true;
                    // If in edit mode, polygons can get sent over from Glue
                    // without points. We don't want to crash the game when this
                    // happens.
                    // Should we preserve the old value and reset it back? This adds
                    // complexity, and I don't know if there's any benefit because this
                    // property is usually false to catch coding errors, but code can't be
                    // added without restarting the app, which would then reset this value back
                    // to false. Let's keep it simple.
                    Polygon.TolerateEmptyPolygons = true;
                }

                FlatRedBall.TileEntities.TileEntityInstantiator.CreationFunction = (entityNameGameType) => InstanceLogic.Self.CreateEntity(entityNameGameType);


                RestartScreenRerunCommands(applyRestartVariables: true, isInEditMode: value, shouldRecordCameraPosition: false, forceCameraToPreviousState: true);
            }

            return response;
#endif
        }

        #endregion


        #region Move to Container

        private static MoveObjectToContainerDtoResponse HandleDto(MoveObjectToContainerDto dto)
        {
            var toReturn = new MoveObjectToContainerDtoResponse();

            var matchesCurrentScreen = GetIfMatchesCurrentScreen(
                dto.ElementName, out System.Type ownerType, out Screen currentScreen);
            if (matchesCurrentScreen)
            {
                toReturn.WasObjectMoved = GlueControl.Editing.MoveObjectToContainerLogic.TryMoveObjectToContainer(
                    dto.ObjectName, dto.ContainerName, EditingManager.Self.ElementEditingMode);
            }
            else
            {
                // we don't know if it can be moved. We'll assume it can, and when that screen is loaded, it will re-run that and...if it 
                // fails, then I guess we'll figure out a way to communicate back to Glue that it needs to restart. Actually this may never
                // happen because moving objects is done in the current screen, but I guess it's technically a possibility so I'll leave this
                // comment here.
                // Update October 12, 2021
                // Actually we can move an object
                // without selecting the current screen.
                // This can be done through the "Add XXXX
                // to GameScreen" option in Quick Actions. In
                // this case, we assume all is okay, so let's return
                // that the object was in fact moved.
                toReturn.WasObjectMoved = true;
            }

            CommandReceiver.GlobalGlueToGameCommands.Add(dto);


            return toReturn;
        }

        #endregion

        #region Restart Screen

        private static void HandleDto(RestartScreenDto dto)
        {
            RestartScreenRerunCommands(applyRestartVariables: true,
                isInEditMode: FlatRedBall.Screens.ScreenManager.IsInEditMode,
                playBump: dto.ShowSelectionBump,
                shouldReloadGlobalContent: dto.ReloadGlobalContent);
        }

        private static void RestartScreenRerunCommands(bool applyRestartVariables, 
            bool isInEditMode,
            bool shouldRecordCameraPosition = true,
            bool forceCameraToPreviousState = false,
            bool playBump = true,
            bool shouldReloadGlobalContent = false)
        {
            ScreenManager.IsNextScreenInEditMode = isInEditMode;

            var screen =
                FlatRedBall.Screens.ScreenManager.CurrentScreen;
            
            void AfterInitializeLogic(Screen newScreen)
            {
                newScreen.ScreenDestroy += HandleScreenDestroy;

                if (FlatRedBall.Screens.ScreenManager.IsInEditMode || forceCameraToPreviousState)
                {
                    CameraLogic.UpdateCameraValuesToScreenSavedValues(screen, setZoom: FlatRedBall.Screens.ScreenManager.IsInEditMode);
                }


                // Even though the camera is reset properly, Gum zoom isn't. Calling this fixes Gum zoom:
                if (CameraSetup.Data.IsGenerateCameraDisplayCodeEnabled)
                {
                    if (FlatRedBall.Screens.ScreenManager.IsInEditMode)
                    {
                        CameraLogic.UpdateCameraToZoomLevel(zoomAroundCursorPosition: false, forceTo100: !isInEditMode);
                    }
                    CameraLogic.PushZoomLevelToEditor();
                }

                if (FlatRedBall.Screens.ScreenManager.IsInEditMode)
                {
                    Camera.Main.Velocity = Vector3.Zero;
                    Camera.Main.Acceleration = Vector3.Zero;
                }

                FlatRedBall.Screens.ScreenManager.ScreenLoaded -= AfterInitializeLogic;

                EditingManager.Self.RefreshSelectionAfterScreenLoad(playBump);
            }

            FlatRedBall.Screens.ScreenManager.ScreenLoaded += AfterInitializeLogic;

            if (shouldRecordCameraPosition)
            {
                CameraLogic.RecordCameraForCurrentScreen();
            }


            screen?.RestartScreen(reloadScreenContent: true, applyRestartVariables: applyRestartVariables);
            EditorVisuals.DestroyContainedObjects();

            if (shouldReloadGlobalContent)
            {
                FlatRedBallServices.Unload(FlatRedBallServices.GlobalContentManager);
                GlobalContent.Initialize();
            }
        }

        #endregion

        #region Reload Content

        private static void HandleDto(ReloadGlobalContentDto dto)
        {
            GlobalContent.Reload(GlobalContent.GetFile(dto.StrippedGlobalContentFileName));
        }


        private static void HandleDto(Dtos.ForceReloadFileDto dto)
        {
            var gameType = FlatRedBallServices.Game.GetType();
            var gameAssembly = gameType.Assembly;
            var namespacePrefix = gameType.FullName.Split('.').First();

            Type elementType = null;
            foreach (var element in dto.ElementsContainingFile)
            {
                var qualifiedName = $"{namespacePrefix}.{element.Replace('\\', '.')}";

                elementType = gameAssembly.GetType(qualifiedName);

                if (elementType != null)
                {
                    // invoke the ReloadFile method:
                    var reloadMethod = elementType.GetMethod("Reload");

                    var field = elementType.GetField(dto.StrippedFileName);

                    var fileObjectReference = field?.GetValue(null);

                    if (reloadMethod != null && fileObjectReference != null)
                    {
                        reloadMethod.Invoke(null, new object[] { fileObjectReference });
                    }
                }

            }

        }

        #endregion

        #region Pause/speed/frame methods

        private static void HandleDto(TogglePauseDto dto)
        {
            var screen = ScreenManager.CurrentScreen;

            if (screen.IsPaused)
            {
                screen.UnpauseThisScreen();
            }
            else
            {
                screen.PauseThisScreen();
            }
        }

        private static void HandleDto(AdvanceOneFrameDto dto)
        {
            var screen = ScreenManager.CurrentScreen;

            screen.UnpauseThisScreen();
            var delegateInstruction = new FlatRedBall.Instructions.DelegateInstruction(() =>
            {
                screen.PauseThisScreen();
            });
            delegateInstruction.TimeToExecute = FlatRedBall.TimeManager.CurrentTime + .001;

            FlatRedBall.Instructions.InstructionManager.Instructions.Add(delegateInstruction);
        }

        private static void HandleDto(SetSpeedDto dto)
        {
            FlatRedBall.TimeManager.TimeFactor = dto.SpeedPercentage / 100.0f;
        }

        #endregion

        #region Get Commands

        private static GetCommandsDtoResponse HandleDto(GetCommandsDto dto)
        {
            var responseDto = new GetCommandsDtoResponse();
#if SupportsEditMode
            if (GlueControlManager.GameToGlueCommands.Count != 0)
            {
                while (GlueControlManager.GameToGlueCommands.TryDequeue(out GlueControlManager.GameToGlueCommand gameToGlueCommand))
                {
                    responseDto.Commands.Add(gameToGlueCommand.Command);
                }
            }
#endif

            return responseDto;
        }

        #endregion

        #region SetBorderless

        private static void HandleDto(SetBorderlessDto dto)
        {
            FlatRedBallServices.Game.Window.IsBorderless = dto.IsBorderless;
        }

        #endregion

        private static void HandleDto(GlueViewSettingsDto dto)
        {
            EditingManager.Self.GuidesGridSpacing = (float)dto.GridSize;
            Screens.EntityViewingScreen.ShowScreenBounds = dto.ShowScreenBoundsWhenViewingEntities;
        }
    }
}
