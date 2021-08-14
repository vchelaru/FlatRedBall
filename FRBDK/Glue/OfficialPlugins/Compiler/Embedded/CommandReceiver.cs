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
                .ToArray();
        }

        public static string Receive(string message, Func<object, bool> runPredicate = null)
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
                throw new Exception();
            }

            var matchingMethod =
                AllMethods
                .FirstOrDefault(item =>
                {
                    if (item.Name == nameof(HandleDto))
                    {
                        var parameters = item.GetParameters();
                        return parameters.Length == 1 && parameters[0].ParameterType.Name == dtoTypeName;
                    }
                    return false;
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

        public static object ReceiveDto(object dto)
        {
            var type = dto.GetType();

            var method = AllMethods
                .FirstOrDefault(item =>
                {
                    if (item.Name == nameof(HandleDto))
                    {
                        var parameters = item.GetParameters();
                        return parameters.Length == 1 && parameters[0].ParameterType == type;
                    }
                    return false;
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
            var currentScreenType = currentScreen.GetType();

            return currentScreenType == ownerType || ownerType?.IsAssignableFrom(currentScreenType) == true;
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

        private static GlueVariableSetDataResponse HandleDto(GlueVariableSetData dto)
        {
            GlueVariableSetDataResponse response = null;

            if (dto.AssignOrRecordOnly == AssignOrRecordOnly.Assign)
            {
                response = GlueControl.Editing.VariableAssignmentLogic.SetVariable(dto);
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
        }

        #endregion

        #region Set Camera Position

        private static void HandleDto(SetCameraPositionDto dto)
        {
            Camera.Main.Position = dto.Position;
        }

        #endregion

        #region Select Object

        private static void HandleDto(SelectObjectDto selectObjectDto)
        {
            bool matchesCurrentScreen =
                GetIfMatchesCurrentScreen(selectObjectDto.ElementNameGlue, out System.Type ownerType, out Screen currentScreen);

            var elementNameGlue = selectObjectDto.ElementNameGlue;
            string ownerTypeName = GlueToGameElementName(elementNameGlue);
            ownerType = typeof(CommandReceiver).Assembly.GetType(ownerTypeName);

            bool isOwnerScreen = false;


            if (matchesCurrentScreen)
            {
                Editing.EditingManager.Self.Select(selectObjectDto.NamedObject?.InstanceName);
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

                    void BeforeCustomInitializeLogic(Screen newScreen)
                    {
                        GlueControlManager.Self.ReRunAllGlueToGameCommands();
                        ScreenManager.BeforeScreenCustomInitialize -= BeforeCustomInitializeLogic;
                    }

                    void AfterInitializeLogic(Screen screen)
                    {
                        // Select this even if it's null so the EditingManager deselects 
                        EditingManager.Self.Select(selectObjectDto.NamedObject?.InstanceName);

                        if (!string.IsNullOrEmpty(selectObjectDto.StateName))
                        {
                            SelectState(selectObjectDto.StateName, selectObjectDto.StateCategoryName);
                        }

                        screen.ScreenDestroy += HandleScreenDestroy;
                        CameraLogic.SetCameraForScreen(screen);

                        CameraLogic.UpdateZoomLevelToCamera();

                        ScreenManager.ScreenLoaded -= AfterInitializeLogic;
                    }
                    FlatRedBall.Screens.ScreenManager.BeforeScreenCustomInitialize += BeforeCustomInitializeLogic;
                    ScreenManager.ScreenLoaded += AfterInitializeLogic;

                    ScreenManager.CurrentScreen.MoveToScreen(ownerType);

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
                        void BeforeCustomInitializeLogic(Screen newScreen)
                        {
                            GlueControlManager.Self.ReRunAllGlueToGameCommands();
                            ScreenManager.BeforeScreenCustomInitialize -= BeforeCustomInitializeLogic;
                        }

                        void AfterInitializeLogic(Screen newScreen)
                        {
                            newScreen.ScreenDestroy += HandleScreenDestroy;

                            FlatRedBall.Screens.ScreenManager.ScreenLoaded -= AfterInitializeLogic;
                        }

                        FlatRedBall.Screens.ScreenManager.ScreenLoaded += AfterInitializeLogic;
                        FlatRedBall.Screens.ScreenManager.BeforeScreenCustomInitialize += BeforeCustomInitializeLogic;


                        Screens.EntityViewingScreen.GameElementTypeToCreate = GlueToGameElementName(elementNameGlue);
                        Screens.EntityViewingScreen.InstanceToSelect = selectObjectDto.NamedObject?.InstanceName;
                        ScreenManager.CurrentScreen.MoveToScreen(typeof(Screens.EntityViewingScreen));
#endif
                    }
                    else
                    {
                        EditingManager.Self.Select(selectObjectDto.NamedObject?.InstanceName);
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
                }

                // what if a value has been nulled out?
                // Categories require all values to be set
                // so it won't matter there, and Vic thinks we
                // should phase out uncategorized states so maybe
                // there's no need to handle that here?
                foreach (var instruction in newStateSave.InstructionSaves)
                {
                    var fieldType = stateType.GetField(instruction.Member)?.FieldType;


                    var convertedValue = instruction.Value;
                    if (instruction.Value is string asString)
                    {
                        // not sure if this is a state, it could be and at some point we're going to handle that, but not for now...
                        convertedValue = VariableAssignmentLogic.ConvertStringToType(fieldType.ToString(), asString, false);
                    }

                    FlatRedBall.Instructions.Reflection.LateBinder.SetValueStatic(existingState, instruction.Member, convertedValue);
                }
            }
        }

        #endregion

        #region Edit vs Play

        private static void HandleDto(SetEditMode setEditMode)
        {
            var value = setEditMode.IsInEditMode;
#if SupportsEditMode
            if (value != FlatRedBall.Screens.ScreenManager.IsInEditMode)
            {
                CameraLogic.RecordCameraForCurrentScreen();

                FlatRedBall.Screens.ScreenManager.IsInEditMode = value;
                FlatRedBall.Gui.GuiManager.Cursor.RequiresGameWindowInFocus = !value;

                if (value)
                {
                    FlatRedBallServices.Game.IsMouseVisible = true;
                }

                FlatRedBall.TileEntities.TileEntityInstantiator.CreationFunction =
                    InstanceLogic.Self.CreateEntity;

                RestartScreenRerunCommands(applyRestartVariables: true, shouldRecordCameraPosition: false, forceCameraToPreviousState: true);
            }
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
                // happen because moving objects is done in the current screen, but I gues it's technically a possibility so I'll leave this
                // comment here.
            }

            CommandReceiver.GlobalGlueToGameCommands.Add(dto);


            return toReturn;
        }

        #endregion

        #region Restart Screen

        private static void HandleDto(RestartScreenDto dto)
        {
            RestartScreenRerunCommands(applyRestartVariables: true);
        }

        private static void RestartScreenRerunCommands(bool applyRestartVariables, bool shouldRecordCameraPosition = true, bool forceCameraToPreviousState = false)
        {
            var screen =
                FlatRedBall.Screens.ScreenManager.CurrentScreen;
            // user may go into edit mode after moving through a level and wouldn't want it to restart fully....or would they? What if they
            // want to change the Player start location. Need to think that through...

            // Vic says - We run all Glue commands before running custom initialize. The reason is - custom initialize
            // may make modifications to objects that are created by glue commands (such as assigning acceleration to objects
            // in a list), but it is unlikely that scripts will make modifications to objects created in CustomInitialize because
            // objects created in CustomInitialize cannot be modified by level editor.
            void BeforeCustomInitializeLogic(Screen newScreen)
            {
                GlueControlManager.Self.ReRunAllGlueToGameCommands();
                ScreenManager.BeforeScreenCustomInitialize -= BeforeCustomInitializeLogic;
            }

            void AfterInitializeLogic(Screen newScreen)
            {
                newScreen.ScreenDestroy += HandleScreenDestroy;

                if (FlatRedBall.Screens.ScreenManager.IsInEditMode || forceCameraToPreviousState)
                {
                    CameraLogic.SetCameraForScreen(screen, setZoom: FlatRedBall.Screens.ScreenManager.IsInEditMode);
                }

                CameraLogic.UpdateZoomLevelToCamera();

                FlatRedBall.Screens.ScreenManager.ScreenLoaded -= AfterInitializeLogic;

                EditingManager.Self.RefreshSelectionAfterScreenLoad();
            }

            FlatRedBall.Screens.ScreenManager.BeforeScreenCustomInitialize += BeforeCustomInitializeLogic;
            FlatRedBall.Screens.ScreenManager.ScreenLoaded += AfterInitializeLogic;

            if (shouldRecordCameraPosition)
            {
                CameraLogic.RecordCameraForCurrentScreen();
            }


            screen?.RestartScreen(reloadContent: true, applyRestartVariables: applyRestartVariables);
        }

        #endregion

        #region Reload Content

        private static void HandleDto(ReloadGlobalContentDto dto)
        {
            GlobalContent.Reload(GlobalContent.GetFile(dto.StrippedGlobalContentFileName));
        }

        #endregion

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
    }
}
