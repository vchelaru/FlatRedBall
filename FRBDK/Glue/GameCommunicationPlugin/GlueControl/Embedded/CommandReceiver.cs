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
using GlueControl.Models;
using GlueControl.Managers;


namespace GlueControl
{
    static class CommandReceiver
    {
        public const string ProjectNamespace = "{ProjectNamespace}";

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


                // This could be a response, so don't throw an exception anymore
                //if (matchingMethod == null)
                //{
                //    throw new InvalidOperationException(
                //        $"Could not find a HandleDto method for type {dtoTypeName}");
                //}

                var dtoType = matchingMethod?.GetParameters()[0].ParameterType;
                if (dtoType == null)
                {
                    // there may not be a matching method for this. That's okay, it may
                    // be a method that is only expected as a response
                    var possibleQualifiedType = $"GlueControl.Dtos.{dtoTypeName}";

                    dtoType = typeof(CommandReceiver).Assembly.GetType(possibleQualifiedType);
                }
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

            if (dto is RespondableDto respondableDto && respondableDto.OriginalDtoId > 0)
            {
                object content = null;

                if (respondableDto is ResponseWithContentDto dtoWithContent && !string.IsNullOrEmpty(dtoWithContent.Content))
                {
                    content = JsonConvert.DeserializeObject(dtoWithContent.Content);
                }
                GlueControlManager.Self.NotifyResponse(respondableDto.OriginalDtoId, content);
            }

            return toReturn;
        }

        public static bool GetIfMatchesCurrentScreen(string elementNameGlue)
        {
            return GetIfMatchesCurrentScreen(elementNameGlue, out _, out _);
        }

        private static bool GetIfMatchesCurrentScreen(string elementNameGlue, out System.Type ownerType, out Screen currentScreen)
        {
            var ownerTypeName = $"{ProjectNamespace}.{elementNameGlue.Replace("\\", ".")}";

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

            // Vic says - haven't figured out if NOS's should get set before applying runtimes or after...
            ApplyNewNamedObjects(dto);

            foreach (var setVariableDto in dto.Data)
            {
                var setResponse = HandleDto(setVariableDto);
                toReturn.Data.Add(setResponse);
            }

            return toReturn;
        }

        private static GlueVariableSetDataResponse HandleDto(GlueVariableSetData dto)
        {
            if (dto.GlueElement != null)
            {
                dto.GlueElement.FixAllTypes();
            }
            GlueVariableSetDataResponse response = null;

            if (dto.AssignOrRecordOnly == AssignOrRecordOnly.Assign)
            {
                if (ObjectFinder.Self.GlueProject == null)
                {
                    GlueCommands.Self.LoadProject(dto.AbsoluteGlueProjectFilePath);
                }

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
                    if (FlatRedBall.Screens.ScreenManager.IsInEditMode)
                    {
                        ObjectFinder.Self.Replace(dto.GlueElement);
                    }
                    if (oldName != null && newName != null)
                    {
                        var element = ObjectFinder.Self.GetElement(dto.GlueElement?.Name);
                        var renamedNos =
                            element?.AllNamedObjects.FirstOrDefault(item => item.InstanceName == oldName);

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

                ApplyNewNamedObjects(dto);
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

        #region SetEditMode

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
            try
            {
                if (value != FlatRedBall.Screens.ScreenManager.IsInEditMode)
                {
                    CameraLogic.RecordCameraForCurrentScreen();

                    FlatRedBall.Gui.GuiManager.Cursor.RequiresGameWindowInFocus = !value;

                    if (value)
                    {
                        FlatRedBallServices.Game.IsMouseVisible = true;

                        if(ObjectFinder.Self.GlueProject == null)
                        {
                            GlueCommands.Self.LoadProject(setEditMode.AbsoluteGlueProjectFilePath);
                        }

                        // If in edit mode, polygons can get sent over from Glue
                        // without points. We don't want to crash the game when this
                        // happens.
                        // Should we preserve the old value and reset it back? This adds
                        // complexity, and I don't know if there's any benefit because this
                        // property is usually false to catch coding errors, but code can't be
                        // added without restarting the app, which would then reset this value back
                        // to false. Let's keep it simple.
                        Polygon.TolerateEmptyPolygons = true;
#if SpriteHasTolerateMissingAnimations
                        Sprite.TolerateMissingAnimations = true;
#endif
                        if(!CameraSetup.Data.AllowWindowResizing)
                        {
                            CameraSetup.Data.AllowWindowResizing = true;
                            CameraSetup.ResetWindow();
                        }
                    }

                    FlatRedBall.TileEntities.TileEntityInstantiator.CreationFunction = (entityNameGameType) => InstanceLogic.Self.CreateEntity(entityNameGameType);


                    RestartScreenRerunCommands(applyRestartVariables: true, isInEditMode: value, shouldRecordCameraPosition: false, forceCameraToPreviousState: true);


                }

            }
            catch(Exception ex)
            {
                response.Succeeded = false;
                response.Message = $"Unexpected error:\n{ex.ToString()}";
            }

            return response;
#else
            return null;
#endif
        }

        #endregion

        #region SelectObjectDto / State

        static List<NamedObjectSave> GetSelectedNamedObjects(SelectObjectDto selectObjectDto)
        {
#if MONOGAME_381
            var names = selectObjectDto.NamedObjectNames.ToHashSet();
#else
            var names = selectObjectDto.NamedObjectNames;
#endif
            List<NamedObjectSave> selected = new List<NamedObjectSave>();
            foreach (var nos in GlueState.Self.CurrentElement.AllNamedObjects)
            {
                if (names.Contains(nos.InstanceName))
                {
                    selected.Add(nos);
                }
            }
            return selected;
        }

        private static void HandleDto(SelectObjectDto selectObjectDto)
        {
            //////////////// if the user has something grabbed, early out, we don't want any updates! //////////////////
            ////////////////The reason is - if selection changes while something is grabbed, then when the user
            //////////////continues to move the object, the movements will apply to the newly-selected object, rather
            ////////////than what the user originally grabbed. 
            if (EditingManager.Self.ItemGrabbed != null || EditingManager.Self.IsDraggingRectangle)
            {
                return;
            }
            ///////////////////////end early out//////////////////////////

            if (selectObjectDto.GlueElement != null)
            {
                selectObjectDto.GlueElement.FixAllTypes();
            }
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
            bool playBump = true;
            bool matchesCurrentSelection = false;

            // If the game does a copy/paste, the selection will echo back to the game. We don't want to play a bump
            // if the echoed selection is already active:
            if (matchesCurrentScreen)
            {
                matchesCurrentSelection = IsCurrentSelectionMatchingDto(selectObjectDto);
            }

            try
            {
                if (FlatRedBall.Screens.ScreenManager.IsInEditMode && selectObjectDto.GlueElement != null)
                {
                    ObjectFinder.Self.Replace(selectObjectDto.GlueElement);
                }
                if (selectObjectDto.GlueElement != null)
                {
                    Editing.EditingManager.Self.SetCurrentGlueElement(selectObjectDto.GlueElement);
                }
                else if (Editing.EditingManager.Self.CurrentGlueElement == null)
                {
                    var element = ObjectFinder.Self.GetElement(selectObjectDto.ElementNameGlue);
                    if (element != null)
                    {
                        Editing.EditingManager.Self.SetCurrentGlueElement(element);
                    }
                }
            }
            catch (ArgumentException e)
            {
                var message =
                    $"The command to select {selectObjectDto.NamedObjectNames.Count} in {selectObjectDto.GlueElement} " +
                    $"threw an exception because the Glue object has a null object.  Inner details:{e}";

                throw new ArgumentException(message);
            }

            ApplyNewNamedObjects(selectObjectDto);



            if (matchesCurrentScreen)
            {
                if (matchesCurrentSelection == false || selectObjectDto.BringIntoFocus)
                {
                    Editing.EditingManager.Self.Select(GetSelectedNamedObjects(selectObjectDto), playBump: playBump, focusCameraOnObject: selectObjectDto.BringIntoFocus);
                }
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
                        EditingManager.Self.Select(GetSelectedNamedObjects(selectObjectDto), playBump: playBump, focusCameraOnObject: true);

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
                    var entityScreen = FlatRedBall.Screens.ScreenManager.CurrentScreen as Screens.EntityViewingScreen;
                    var entity = (entityScreen?.CurrentEntity as PositionedObject);

                    var isAlreadyViewingThisEntity =
                        entityScreen != null &&
                        entity != null &&
                        DoTypesMatch(entity, ownerTypeName, ownerType);

                    if (!isAlreadyViewingThisEntity)
                    {
#if SupportsEditMode
                        ScreenManager.IsNextScreenInEditMode = ScreenManager.IsInEditMode;

                        void AfterInitializeLogic(Screen newScreen)
                        {
                            newScreen.ScreenDestroy += HandleScreenDestroy;

                            FlatRedBall.Screens.ScreenManager.ScreenLoaded -= AfterInitializeLogic;

                            CameraLogic.UpdateCameraValuesToScreenSavedValues(newScreen );

                            if (!string.IsNullOrEmpty(selectObjectDto.StateName))
                            {
                                SelectState(selectObjectDto.StateName, selectObjectDto.StateCategoryName);
                            }
                        }

                        FlatRedBall.Screens.ScreenManager.ScreenLoaded += AfterInitializeLogic;

                        EditorVisuals.DestroyContainedObjects();

                        Screens.EntityViewingScreen.GameElementTypeToCreate = GlueToGameElementName(elementNameGlue);
                        Screens.EntityViewingScreen.InstanceToSelect = GetSelectedNamedObjects(selectObjectDto).FirstOrDefault();
                        ScreenManager.CurrentScreen.MoveToScreen(typeof(Screens.EntityViewingScreen));
#endif
                    }
                    else
                    {
                        matchesCurrentSelection = IsCurrentSelectionMatchingDto(selectObjectDto);
                        if (!matchesCurrentSelection || selectObjectDto.BringIntoFocus)
                        {
                            EditingManager.Self.Select(GetSelectedNamedObjects(selectObjectDto), playBump: playBump, focusCameraOnObject: selectObjectDto.BringIntoFocus);
                        }
                    }
                }
            }
        }

        private static bool IsCurrentSelectionMatchingDto(SelectObjectDto selectObjectDto)
        {
            bool matchesSelection;
            var newSelectionCount = selectObjectDto.NamedObjectNames.Count;

            if (Editing.EditingManager.Self.CurrentNamedObjects.Count != newSelectionCount)
            {
                matchesSelection = false;
            }
            else
            {
                matchesSelection = true;
                for (int i = 0; i < Editing.EditingManager.Self.CurrentNamedObjects.Count; i++)
                {
                    var currentNamedObject = Editing.EditingManager.Self.CurrentNamedObjects[i];
                    if (currentNamedObject.InstanceName != selectObjectDto.NamedObjectNames[i])
                    {
                        matchesSelection = false;
                    }
                }
            }

            return matchesSelection;
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

            // always give dynamics preferential treatment:
            var wasSetByRuntimeState = SelectStateAddedAtRuntime(stateName, stateCategoryName, entity, GlueState.Self.CurrentElement);

            if (!wasSetByRuntimeState)
            {
                // if there is no runtime replacement for a state, then try to set the
                // normal state through reflection:

                var entityType = entity.GetType();
                var stateTypeName = entityType.FullName + "+" + stateCategoryName ?? "VariableState";
                var stateType = entityType.Assembly.GetType(stateTypeName);
                if (stateType != null)
                {
                    SelectStateByType(stateName, stateCategoryName, entity, stateType);
                }
            }
        }

        private static bool SelectStateAddedAtRuntime(string stateName, string stateCategoryName, PositionedObject entity, GlueElement owner)
        {
            var entityType = entity.GetType();

            var gameElementType = CommandReceiver.GlueToGameElementName(owner.Name);

            StateSaveCategory category = null;
            if (InstanceLogic.Self.StatesAddedAtRuntime.ContainsKey(entityType.FullName))
            {
                var categories = InstanceLogic.Self.StatesAddedAtRuntime[entityType.FullName];

                category = categories.FirstOrDefault(item => item.Name == stateCategoryName);
            }

            StateSave stateSave = null;

            if (category != null)
            {
                stateSave = category.GetState(stateName);
            }

            if (stateSave != null)
            {
                GlueVariableSetDataResponse throwawayResponse = new GlueVariableSetDataResponse();
                foreach (var instruction in stateSave.InstructionSaves)
                {
                    var value = instruction.Value;
                    string conversionReport = "";
                    bool convertFileNamesToObjects = true;
                    if (instruction.Value is string valueAsString)
                    {
                        value = VariableAssignmentLogic.ConvertStringToType(instruction.Type, valueAsString, false, out conversionReport, convertFileNamesToObjects);
                        //value = GlueControl.Editing.VariableAssignmentLogic.ConvertStringToValue(asString, instruction.Member.Type);
                    }

                    //InstanceLogic.Self.AssignVariable(entity, instruction, convertFileNamesToObjects: true, owner:owner);
                    GlueControl.Editing.VariableAssignmentLogic.SetVariable(
                        // prefix "this." so that the underlying system knows that it's a variable on the entity
                        // This is just how it's done...
                        "this." + instruction.Member,
                        value, null,
                        gameElementType, throwawayResponse);
                }
            }

            return stateSave != null;
        }

        private static void SelectStateByType(string stateName, string stateCategoryName, PositionedObject entity, Type stateType)
        {
            var entityType = entity.GetType();

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

        #region SelectSubIndex

        private static void HandleDto(SelectSubIndexDto dto)
        {
            GlueState.Self.SelectedSubIndex = dto.Index;
        }

        #endregion

        #region ObjectReorderedDto

        private static void HandleDto(NamedObjectReorderedDto dto)
        {
            if (FlatRedBall.Screens.ScreenManager.IsInEditMode && dto.GlueElement != null)
            {
                ObjectFinder.Self.Replace(dto.GlueElement);
            }

            var element = ObjectFinder.Self.GetElement(dto.GlueElement.Name);

            NamedObjectSave nos = null;
            if (element != null)
            {
                nos = element.GetNamedObjectRecursively(dto.NamedObjectName);
            }

            if (nos == null)
            {
                return;
            }

            if (nos.SourceType == SourceType.FlatRedBallType && nos.SourceClassType == "FlatRedBall.Graphics.Layer")
            {
                HandleLayerReordered(dto, nos, element);
            }
            //var namedObject = ObjectFinder.Self.GetNamedObjectRecursivelydto.ContainerName, dto.ObjectName);
        }

        private static void HandleLayerReordered(NamedObjectReorderedDto dto, NamedObjectSave layerMoved, GlueElement containerElement)
        {
            var layer = SpriteManager.Layers.FirstOrDefault(item => item.Name == dto.NamedObjectName);

            if (layer == null)
            {
                return;
            }

            var allLayerNamedObjects = containerElement.AllNamedObjects
                .Where(item => item.SourceType == SourceType.FlatRedBallType && item.SourceClassType == "FlatRedBall.Graphics.Layer")
                .ToList();


            var newIndex = allLayerNamedObjects.IndexOf(layerMoved);

#if SpriteManagerHasInsertLayer
            if (SpriteManager.Layers.IndexOf(layer) != newIndex)
            {
                SpriteManager.RemoveLayer(layer);
                SpriteManager.InsertLayer(layer, newIndex);
            }
#endif
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

        #region Get Camera Position

        private static object HandleDto(GetCameraPosition dto)
        {
            var toReturn = string.Empty;

            var getCameraPositionResponse = new GlueControl.Dtos.GetCameraPositionResponse();
            getCameraPositionResponse.X = Camera.Main.X;
            getCameraPositionResponse.Y = Camera.Main.Y;
            getCameraPositionResponse.Z = Camera.Main.Z;
            return getCameraPositionResponse;
        }

        #endregion

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

        #region ForceClientSizeUpdatesDto

        private static void HandleDto(ForceClientSizeUpdatesDto dto) => FlatRedBallServices.ForceClientSizeUpdates();

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

        public static string GlueToGameElementName(string elementName)
        {
            
            return $"{ProjectNamespace}.{elementName.Replace("\\", ".")}";
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
            if (removeObjectDto.GlueElement != null)
            {
                removeObjectDto.GlueElement.FixAllTypes();
            }
            var response = InstanceLogic.Self.HandleDeleteInstanceCommandFromGlue(removeObjectDto);

            if (FlatRedBall.Screens.ScreenManager.IsInEditMode)
            {
                ObjectFinder.Self.Replace(removeObjectDto.GlueElement);
            }

            // don't do this. When it re-runs, it can screw up state::
            //Editing.EditingManager.Self.SetCurrentGlueElement(removeObjectDto.GlueElement);

            CommandReceiver.GlobalGlueToGameCommands.Add(removeObjectDto);

            ApplyNewNamedObjects(removeObjectDto);

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

            ApplyNewNamedObjects(dto);

            var createdObject =
                GlueControl.InstanceLogic.Self.HandleCreateInstanceCommandFromGlue(dto, GlobalGlueToGameCommands.Count, forcedParent: null);

            var response = new OptionallyAttemptedGeneralResponse();
            response.Succeeded = createdObject != null;

            valueToReturn.CreationResponse = response;

            // internally this decides what to add to, so we don't have to sort the DTOs
            //CommandReceiver.EnqueueToOwner(dto, dto.ElementNameGame);
            GlobalGlueToGameCommands.Add(dto);

            return valueToReturn;
        }

        private static AddObjectDtoListResponse HandleDto(AddObjectDtoList dto)
        {
            AddObjectDtoListResponse dtoResponse = new AddObjectDtoListResponse();
            foreach (var dtoItem in dto.Data)
            {
                var response = HandleDto(dtoItem);
                dtoResponse.Data.Add(response);
            }
            return dtoResponse;
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

            foreach (var instruction in newStateSave.InstructionSaves)
            {
                instruction.Value = GlueControl.Models.IElementExtensionMethods.FixValue(instruction.Value, instruction.Type);
            }

            // If there is already a runtime State object, we need to update that object's properties
            // in case those are set through code directly. Direct assignments in game code do not use the InstanceLogic.Self.StatesAddedAtRuntime
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

                GlueVariableSetDataResponse throwawayResponse = new GlueVariableSetDataResponse();


                foreach (var instruction in newStateSave.InstructionSaves)
                {
                    var value = instruction.Value;
                    string conversionReport = "";
                    bool convertFileNamesToObjects = true;
                    if (instruction.Value is string valueAsString)
                    {
                        value = VariableAssignmentLogic.ConvertStringToType(instruction.Type, valueAsString, false, out conversionReport, convertFileNamesToObjects);
                        //value = GlueControl.Editing.VariableAssignmentLogic.ConvertStringToValue(asString, instruction.Member.Type);
                    }

                    GlueControl.Editing.VariableAssignmentLogic.SetVariable(
                        // prefix "this." so that the underlying system knows that it's a variable on the entity
                        // This is just how it's done...
                        "this." + instruction.Member,
                        value, null,
                        elementGameType, throwawayResponse);
                }
            }
        }

        #endregion

        #region Update Category

        private static void HandleDto(UpdateStateSaveCategory dto)
        {
            foreach (var state in dto.Category.States)
            {
                ReplaceStateWithNewState(dto.ElementNameGame, dto.Category.Name, state);
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


        #region Move to Container

        private static MoveObjectToContainerDtoResponse HandleDto(MoveObjectToContainerListDto dto)
        {
            MoveObjectToContainerDtoResponse toReturn = new MoveObjectToContainerDtoResponse();

            foreach (var item in dto.Changes)
            {
                var innerResult = HandleDto(item);

                toReturn.NumberSuccessfullyMoved += innerResult.NumberSuccessfullyMoved;
                toReturn.NumberFailedToMoved += innerResult.NumberFailedToMoved;
            }


            return toReturn;

        }

        private static MoveObjectToContainerDtoResponse HandleDto(MoveObjectToContainerDto dto)
        {
            var toReturn = new MoveObjectToContainerDtoResponse();

            var matchesCurrentScreen = GetIfMatchesCurrentScreen(
                dto.ElementName, out System.Type ownerType, out Screen currentScreen);
            bool wasMoved = false;
            if (matchesCurrentScreen)
            {
                wasMoved = GlueControl.Editing.MoveObjectToContainerLogic.TryMoveObjectToContainer(
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
                wasMoved = true;
            }

            if (wasMoved)
            {
                toReturn.NumberSuccessfullyMoved++;
            }
            else
            {
                toReturn.NumberFailedToMoved++;
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


                // this forces Gum layouts to refresh:
                FlatRedBall.FlatRedBallServices.GraphicsOptions.CallSizeOrOrientationChanged();
            }

            FlatRedBall.Screens.ScreenManager.ScreenLoaded += AfterInitializeLogic;

            if (shouldRecordCameraPosition)
            {
                CameraLogic.RecordCameraForCurrentScreen();
            }


            screen?.RestartScreen(true, applyRestartVariables);
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

            var file = GlobalContent.GetFile(dto.StrippedFileName);

            if (file != null)
            {
                GlobalContent.Reload(dto);
            }



            if (dto.IsLocalizationDatabase)
            {
                FlatRedBall.Localization.LocalizationManager.ClearDatabase();
                // assume this uses commas, not tabs. 
                FlatRedBall.Localization.LocalizationManager.AddDatabase(dto.FileRelativeToProject, ',');
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

        #region GetGlueToGameCommandRerunList

        private static GetCommandsDtoResponse HandleDto(GetGlueToGameCommandRerunList dto)
        {
            var responseDto = new GetCommandsDtoResponse();
#if SupportsEditMode

            var arrayCopy = GlobalGlueToGameCommands.ToArray();

            foreach(var item in arrayCopy)
            {
                var combined = item.GetType().Name + ":" + JsonConvert.SerializeObject(item);
                responseDto.Commands.Add(combined);
            }
#endif

            return responseDto;
        }

        #endregion

        #region SetBorderless

        private static void HandleDto(SetBorderlessDto dto)
        {
#if MONOGAME
            FlatRedBallServices.Game.Window.IsBorderless = dto.IsBorderless;
#elif FNA
            FlatRedBallServices.Game.Window.IsBorderlessEXT = dto.IsBorderless;

#endif
        }

        #endregion

        #region ForceGameResolution

        private static void HandleDto(ForceGameResolution dto)
        {
            CameraSetup.Data.ResolutionWidth = dto.Width;
            CameraSetup.Data.ResolutionHeight = dto.Height;
            CameraSetup.ResetWindow();
        }

        #endregion

        private static void HandleDto(GlueViewSettingsDto dto)
        {
            EditingManager.Self.ShowGrid = dto.ShowGrid;
            EditingManager.Self.GridAlpha = dto.GridAlpha;
            EditingManager.Self.GuidesGridSpacing = (float)dto.GridSize;
            Screens.EntityViewingScreen.ShowScreenBounds = dto.ShowScreenBoundsWhenViewingEntities;

            if (dto.SetBackgroundColor)
            {
                CameraLogic.BackgroundRed = dto.BackgroundRed / 255.0f;
                CameraLogic.BackgroundGreen = dto.BackgroundGreen / 255.0f;
                CameraLogic.BackgroundBlue = dto.BackgroundBlue / 255.0f;
            }
            else
            {
                CameraLogic.BackgroundRed = null;
                CameraLogic.BackgroundGreen = null;
                CameraLogic.BackgroundBlue = null;
            }

            EditingManager.Self.SnapSize = (float)dto.SnapSize;
            EditingManager.Self.PolygonPointSnapSize = (float)dto.PolygonPointSnapSize;
            EditingManager.Self.IsSnappingEnabled = dto.EnableSnapping;

        }

        private static void ApplyNewNamedObjects(UpdateCurrentElementDto dto)
        {
            foreach (var update in dto.NamedObjectsToUpdate)
            {
                EditingManager.Self.ReplaceNamedObjectSave(update.NamedObjectSave, update.GlueElementName, update.ContainerName);
            }
        }

        #region GetProfilingDataDto

        private static ProfilingDataDto HandleDto(GetProfilingDataDto dto)
        {
            var response = new ProfilingDataDto();

#if HasFrbServicesGraphicsDeviceManager || REFERENCES_FRB_SOURCE
            var isFixed = !dto.IsTimestepDisabled;


            FlatRedBallServices.Game.IsFixedTimeStep = isFixed;
            if(FlatRedBallServices.GraphicsDeviceManager.SynchronizeWithVerticalRetrace !=
                isFixed)
            {
                FlatRedBallServices.GraphicsDeviceManager.SynchronizeWithVerticalRetrace = isFixed;
                FlatRedBallServices.GraphicsDeviceManager.ApplyChanges();
            }
#endif

            response.SummaryData = FlatRedBall.Debugging.Debugger.GetFullPerformanceInformation();

            response.CollisionData = GetCollisionDataDto();

            return response;
        }

        private static List<CollisionRelationshipInfo> GetCollisionDataDto()
        {
            var collisionManager = FlatRedBall.Math.Collision.CollisionManager.Self;
            int numberOfCollisions = 0;
            int? maxCollisions = null;

            List<CollisionRelationshipInfo> toReturn = new List<CollisionRelationshipInfo>();
            foreach (var relationship in collisionManager.Relationships)
            {
                numberOfCollisions += relationship.DeepCollisionsThisFrame;

                var dto = new CollisionRelationshipInfo();
                dto.DeepCollisions = relationship.DeepCollisionsThisFrame;
                dto.RelationshipName = relationship.Name;

                var firstCollisionObject = relationship.FirstAsObject;
                var secondObject = relationship.SecondAsObject;

                System.Collections.IEnumerable listWithoutPartition = null;
                if (firstCollisionObject is System.Collections.IList firstAsIEnumerable)
                {
                    dto.FirstItemListCount = firstAsIEnumerable.Count;
                    var isPartitioned = false;
                    foreach (var item in CollisionManager.Self.Partitions)
                    {
                        if (item.PartitionedObject == firstCollisionObject)
                        {
                            dto.FirstPartitionAxis = item.axis;
                            isPartitioned = true;
                            break;
                        }
                    }

                    if (!isPartitioned)
                    {
                        listWithoutPartition = firstAsIEnumerable;
                    }
                }

                if (secondObject is System.Collections.IList secondAsIEnumerable)
                {
                    dto.SecondItemListCount = secondAsIEnumerable.Count;
                    var isPartitioned = false;
                    foreach (var item in CollisionManager.Self.Partitions)
                    {
                        if (item.PartitionedObject == secondObject)
                        {
                            dto.SecondPartitionAxis = item.axis;
                            isPartitioned = true;
                            break;
                        }
                    }

                    if (!isPartitioned)
                    {
                        listWithoutPartition = secondAsIEnumerable;
                    }
                }

                dto.IsPartitioned = listWithoutPartition == null;

                toReturn.Add(dto);
            }
            return toReturn;
        }

#endregion
    }


}
